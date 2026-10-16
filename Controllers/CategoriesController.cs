using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Common;
using RaceDay.Database;
using RaceDay.DTOs.Categories;
using RaceDay.Extensions;
using RaceDay.Models;

namespace RaceDay.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// Lists all categories for a specific event. Public.
    [HttpGet("api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.EventID == eventId);
        if (!eventExists) return NotFound(new ApiError("Event does not exist."));

        var categories = await _db.Categories.Where(c => c.EventID == eventId).ToListAsync();
        return Ok(categories.Select(CategoryResponse.FromEntity));
    }

    /// Adds a new category to a specific event. Organiser (owner) only.
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = Role.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CreateCategoryRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null) return NotFound(new ApiError("Event does not exist."));

        if (ev.OrganiserID != User.GetUserId())
            return Forbid();

        var category = new Category
        {
            EventID = eventId,
            Name = request.Name,
            DistanceKm = request.DistanceKm,
            MaxParticipants = request.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForEvent), new { eventId }, CategoryResponse.FromEntity(category));
    }

    /// Updates a category's details. Organiser (owner of the parent event) only.
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = Role.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFound(new ApiError("Category does not exist."));

        if (category.Event.OrganiserID != User.GetUserId())
            return Forbid();

        category.Name = request.Name;
        category.DistanceKm = request.DistanceKm;
        category.MaxParticipants = request.MaxParticipants;

        await _db.SaveChangesAsync();
        return Ok(CategoryResponse.FromEntity(category));
    }

    /// Deletes a category from an event. Organiser (owner of the parent event) only.
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = Role.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category is null) return NotFound(new ApiError("Category does not exist."));

        if (category.Event.OrganiserID != User.GetUserId())
            return Forbid();

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
