using System.ComponentModel.DataAnnotations;
using RaceDay.Models;

namespace RaceDay.DTOs.Users;

public class UpdateProfileRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}

public class UserProfileResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public static UserProfileResponse FromEntity(User u) => new()
    {
        UserId = u.UserID,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role.RoleName,
        ProfilePictureUrl = u.ProfilePictureURL,
        CreatedAt = u.CreatedAt
    };
}