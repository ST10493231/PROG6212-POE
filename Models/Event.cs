using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class Event
{
    public int EventID { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    // Constrained to "Run", "Walk", "Cycle" — enforced in DbContext (CHECK constraint)
    // and validated again in the controller/DTO.
    [Required, MaxLength(10)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? BannerImageURL { get; set; }

    public int OrganiserID { get; set; }
    public User Organiser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}