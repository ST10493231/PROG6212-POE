using System.ComponentModel.DataAnnotations;
using RaceDay.Models;

namespace RaceDay.DTOs.Categories;

public class CreateCategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Leave null for age-based categories (e.g. "Under 20")
    public decimal? DistanceKm { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;
}

public class UpdateCategoryRequest : CreateCategoryRequest { }

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public int MaxParticipants { get; set; }

    public static CategoryResponse FromEntity(Category c) => new()
    {
        CategoryId = c.CategoryID,
        EventId = c.EventID,
        Name = c.Name,
        DistanceKm = c.DistanceKm,
        MaxParticipants = c.MaxParticipants
    };
}