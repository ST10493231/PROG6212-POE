using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Common;
using RaceDay.Database;
using RaceDay.DTOs.Enrolments;
using RaceDay.DTOs.Results;
using RaceDay.Extensions;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// Enrols the logged-in Participant into the specified category.
    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = Role.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

        if (category is null) return NotFound(new ApiError("Category does not exist."));

        var userId = User.GetUserId();

        var alreadyEnrolled = await _db.Enrolments.AnyAsync(en => en.UserID == userId && en.CategoryID == categoryId);
        if (alreadyEnrolled) return Conflict(new ApiError("You are already enrolled in this category."));

        if (category.Enrolments.Count >= category.MaxParticipants)
            return Conflict(new ApiError("This category is full."));

        var enrolment = new Enrolment
        {
            UserID = userId,
            CategoryID = categoryId
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        await _db.Entry(enrolment).Reference(en => en.User).LoadAsync();
        enrolment.Category = category;

        return CreatedAtAction(nameof(GetResult), new { enrolmentId = enrolment.EnrolmentID },
            EnrolmentResponse.FromEntity(enrolment));
    }

    /// Lists all enrolments (past and upcoming) for the logged-in Participant.
    [HttpGet("api/users/me/enrolments")]
    [Authorize(Roles = Role.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = User.GetUserId();

        var enrolments = await _db.Enrolments
            .Include(en => en.User)
            .Include(en => en.Category).ThenInclude(c => c.Event)
            .Include(en => en.Result)
            .Where(en => en.UserID == userId)
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentResponse.FromEntity));
    }

    /// Cancels an enrolment belonging to the logged-in Participant.
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = Role.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(en => en.EnrolmentID == id);
        if (enrolment is null) return NotFound(new ApiError("Enrolment does not exist."));

        if (enrolment.UserID != User.GetUserId())
            return Forbid();

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// Captures the finish-line result for a specific enrolment. Organiser (owner of the event) only.
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = Role.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureResult(int enrolmentId, [FromBody] CaptureResultRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var enrolment = await _db.Enrolments
            .Include(en => en.User)
            .Include(en => en.Category).ThenInclude(c => c.Event)
            .Include(en => en.Result)
            .FirstOrDefaultAsync(en => en.EnrolmentID == enrolmentId);

        if (enrolment is null) return NotFound(new ApiError("Enrolment does not exist."));

        if (enrolment.Category.Event.OrganiserID != User.GetUserId())
            return Forbid();

        if (enrolment.Result is not null)
            return Conflict(new ApiError("A result has already been captured for this enrolment."));

        if (!TimeSpan.TryParse(request.FinishTime, out var finishTime))
            return BadRequest(new ApiError("FinishTime must be in the format hh:mm:ss."));

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = finishTime,
            Position = request.Position,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Finished" : request.Status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        result.Enrolment = enrolment;
        return CreatedAtAction(nameof(GetResult), new { enrolmentId }, ResultResponse.FromEntity(result));
    }

    /// Retrieves the result for a specific enrolment. Any authenticated user (ownership enforced for Participants).
    [HttpGet("api/enrolments/{enrolmentId:int}/result")]
    [Authorize]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResult(int enrolmentId)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.User)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.EnrolmentID == enrolmentId);

        if (result is null) return NotFound(new ApiError("No result has been captured for this enrolment yet."));

        var isOwningParticipant = result.Enrolment.UserID == User.GetUserId();
        var isOwningOrganiser = result.Enrolment.Category.Event.OrganiserID == User.GetUserId();

        if (!isOwningParticipant && !isOwningOrganiser)
            return Forbid();

        return Ok(ResultResponse.FromEntity(result));
    }

    /// Retrieves the personal performance history of the logged-in Participant across all events.
    [HttpGet("api/users/me/results")]
    [Authorize(Roles = Role.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = User.GetUserId();

        var results = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.User)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category).ThenInclude(c => c.Event)
            .Where(r => r.Enrolment.UserID == userId)
            .ToListAsync();

        return Ok(results.Select(ResultResponse.FromEntity));
    }
}