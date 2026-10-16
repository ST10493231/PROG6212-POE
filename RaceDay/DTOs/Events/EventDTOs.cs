using System.ComponentModel.DataAnnotations;
using RaceDay.Models;
using CategoryResponse = RaceDay.DTOs.Categories.CategoryResponse;

namespace RaceDay.DTOs.Events;

public class CreateEventRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required, Range(0.1, 999.99)]
    public decimal DistanceKm { get; set; }

    // "Run", "Walk", or "Cycle"
    [Required]
    public string EventType { get; set; } = string.Empty;
}

public class UpdateEventRequest : CreateEventRequest { }

public class EventResponse
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public List<CategoryResponse> Categories { get; set; } = new();

    public static EventResponse FromEntity(Event e, bool includeCategories = false) => new()
    {
        EventId = e.EventID,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        BannerImageUrl = e.BannerImageURL,
        OrganiserId = e.OrganiserID,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        Categories = includeCategories
            ? e.Categories.Select(CategoryResponse.FromEntity).ToList()
            : new List<CategoryResponse>()
    };
}