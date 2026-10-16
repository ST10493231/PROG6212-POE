using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Common;
using RaceDay.Database;
using RaceDay.DTOs.Events;
using RaceDay.Extensions;
using RaceDay.Models;
using EnrolmentResponse = RaceDay.DTOs.Enrolments.EnrolmentResponse;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static readonly string[] ValidEventTypes = { "Run", "Walk", "Cycle" };

    private readonly RaceDayDbContext _db;
    private readonly IWebHostEnvironment _env;

    public EventsController(RaceDayDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// Lists all events. Public — no authentication required.
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? location, [FromQuery] DateTime? afterDate)
    {
        var query = _db.Events.Include(e => e.Organiser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(e => e.Location.Contains(location));

        if (afterDate.HasValue)
            query = query.Where(e => e.EventDate >= afterDate.Value);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => EventResponse.FromEntity(e)));
    }

    /// Retrieves full details of a single event, including its categories. Public.
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev is null) return NotFound(new ApiError("Event does not exist."));

        return Ok(EventResponse.FromEntity(ev, includeCategories: true));
    }

    /// Creates a new event. The logged-in Organiser becomes the owner.
    [HttpPost]
    [Authorize(Roles = Role.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (!ValidEventTypes.Contains(request.EventType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiError("EventType must be one of: Run, Walk, Cycle."));
        }

        var organiserId = User.GetUserId();

        var ev = new Event
        {
            Name = request.Name,
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location,
            DistanceKm = request.DistanceKm,
            EventType = request.EventType,
            OrganiserID = organiserId
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, EventResponse.FromEntity(ev));
    }

    /// Updates an event owned by the logged-in Organiser.
    [HttpPut("{id:int}")]
    [Authorize(Roles = Role.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFound(new ApiError("Event does not exist."));

        if (ev.OrganiserID != User.GetUserId())
            return Forbid();

        if (!ValidEventTypes.Contains(request.EventType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiError("EventType must be one of: Run, Walk, Cycle."));
        }

        ev.Name = request.Name;
        ev.Description = request.Description;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location;
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType;

        await _db.SaveChangesAsync();
        return Ok(EventResponse.FromEntity(ev));
    }

    /// Deletes an event owned by the logged-in Organiser.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Role.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFound(new ApiError("Event does not exist."));

        if (ev.OrganiserID != User.GetUserId())
            return Forbid();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// Uploads/replaces the event's banner image.
    /// NOTE: For Part 2 this stores the file locally under wwwroot/uploads. Part 3
    /// swaps this implementation for Azure Blob Storage without changing the route.

    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = Role.Organiser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile? file)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFound(new ApiError("Event does not exist."));

        if (ev.OrganiserID != User.GetUserId())
            return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest(new ApiError("No file was uploaded."));

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new ApiError("Only .jpg, .jpeg, .png, or .gif files are allowed."));

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "event-banners");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"event-{id}-{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/event-banners/{fileName}";
        ev.BannerImageURL = url;
        await _db.SaveChangesAsync();

        return Ok(new { bannerImageUrl = url });
    }

    /// Lists all participants enrolled in a specific event (all categories). Organiser (owner) only.
    [HttpGet("{id:int}/enrolments")]
    [Authorize(Roles = Role.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null) return NotFound(new ApiError("Event does not exist."));

        if (ev.OrganiserID != User.GetUserId())
            return Forbid();

        var enrolments = await _db.Enrolments
            .Include(en => en.User)
            .Include(en => en.Category).ThenInclude(c => c.Event)
            .Include(en => en.Result)
            .Where(en => en.Category.EventID == id)
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentResponse.FromEntity));
    }
}