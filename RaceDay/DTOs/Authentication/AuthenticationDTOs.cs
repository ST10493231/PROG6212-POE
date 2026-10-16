using System.ComponentModel.DataAnnotations;
using RaceDay.Models;

namespace RaceDay.DTOs.Authentication;

// Body for POST /api/auth/register.
public class RegisterRequest
{
    // The user's full name.
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// A unique email address, used as the login identifier.
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    // At least 8 characters. Hashed before storage -- never stored in plain text.
    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    // Must be exactly "Organiser" or "Participant".
    [Required]
    public string Role { get; set; } = string.Empty;
}

// Body for POST /api/auth/login.
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

/// Returned on successful login. There is no token: authentication state (UserId,
/// Role) is stored server-side in session and tracked via an HttpOnly session
/// cookie the browser/HTTP client sends automatically on every later request.

public class AuthResponse
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Message { get; set; } = "Login successful. Session established.";
}

// Returned on successful registration.
public class UserSummaryResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public static UserSummaryResponse FromEntity(User u) => new()
    {
        UserId = u.UserID,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role.RoleName
    };
}