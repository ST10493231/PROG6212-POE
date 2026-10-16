using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class Category
{
    public int CategoryID { get; set; }

    public int EventID { get; set; }
    public Event Event { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Nullable: distance-based categories (e.g. "10km") set this;
    // age-based categories (e.g. "Under 20") leave it null.
    public decimal? DistanceKm { get; set; }

    public int MaxParticipants { get; set; } = 100;

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}