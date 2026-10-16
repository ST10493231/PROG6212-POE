using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class Role
{
    public int RoleID { get; set; }

    [Required, MaxLength(20)]
    public string RoleName { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();

    // Well-known role names used throughout the app and in [Authorize(Roles = ...)]
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}