using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Common;
using RaceDay.Database;
using RaceDay.DTOs.Users;
using RaceDay.Extensions;

namespace RaceDay.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // Any authenticated user (Organiser or Participant)
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IWebHostEnvironment _env;

    public UsersController(RaceDayDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// Retrieves the profile of the currently logged-in user.
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.GetUserId();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null) return Unauthorized(new ApiError("User no longer exists."));

        return Ok(UserProfileResponse.FromEntity(user));
    }

    /// Updates the profile details of the currently logged-in user.
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.GetUserId();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null) return Unauthorized(new ApiError("User no longer exists."));

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email && u.UserID != userId);
        if (emailTaken) return BadRequest(new ApiError("That email is already in use by another account."));

        user.FullName = request.FullName;
        user.Email = request.Email;
        await _db.SaveChangesAsync();

        return Ok(UserProfileResponse.FromEntity(user));
    }

    /// Uploads/replaces the logged-in user's profile picture.
    /// NOTE: For Part 2 this stores the file locally under wwwroot/uploads and
    /// returns its URL. Part 3 swaps this implementation for Azure Blob Storage
    /// without changing the endpoint's route or response shape.

    [HttpPost("me/profile-picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiError("No file was uploaded."));
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new ApiError("Only .jpg, .jpeg, .png, or .gif files are allowed."));
        }

        var userId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null) return Unauthorized(new ApiError("User no longer exists."));

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "profile-pictures");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"user-{userId}-{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/profile-pictures/{fileName}";
        user.ProfilePictureURL = url;
        await _db.SaveChangesAsync();

        return Ok(new { profilePictureUrl = url });
    }
}