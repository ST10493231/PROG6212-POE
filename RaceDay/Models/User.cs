using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class User
{
    public int UserID { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleID { get; set; }
    public Role Role { get; set; } = null!;

    [MaxLength(500)]
    public string? ProfilePictureURL { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Event> EventsOrganised { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}