using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Authentication;
using RaceDay.Common;
using RaceDay.Database;
using RaceDay.DTOs.Authentication;
using RaceDay.Models;

namespace RaceDay.Controllers
{// start of namespace
    [ApiController]
    [Route("api/auth")]

    /// Registration, login, and logout. Login establishes a server-side session.

    public class AuthenticationController : ControllerBase
    {// start of class authentication controller
        private readonly RaceDayDbContext _db;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthenticationController(RaceDayDbContext db)
        {
            _db = db;
        }

        // Registers a new Organiser or Participant account. Public.
        // <response code="201">User created.</response>
        // <response code="400">Validation failed, or Role was not "Organiser"/"Participant".</response>
        // <response code="409">A user with this email is already registered.</response>

        [HttpPost("register")]
        [ProducesResponseType(typeof(UserSummaryResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {// start of Register
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (request.Role != Role.Organiser && request.Role != Role.Participant)
            {
                return BadRequest(new ApiError($"Role must be either '{Role.Organiser}' or '{Role.Participant}'."));
            }

            var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                return Conflict(new ApiError("A user with this email is already registered."));
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role);
            if (role is null)
            {
                return BadRequest(new ApiError("Invalid role."));
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                RoleID = role.RoleID,
                Role = role
            };
            // Password is hashed before it ever touches the database -- never stored in plain text.
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { id = user.UserID }, UserSummaryResponse.FromEntity(user));
        }// end of Register

        /// Authenticates a user and establishes a server-side session. The session stores
        /// the user's ID and role; the client only receives an opaque HttpOnly session
        /// cookie and must send it back (automatically, if it's a browser or a cookie-aware
        /// HTTP client) on every subsequent request to stay authenticated.
        // <response code="200">Login successful; session cookie set.</response>
        // <response code="401">Invalid email or password.</response>

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {// start of Login
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user is null)
            {// start of if (null user)
                return Unauthorized(new ApiError("Invalid email or password."));
            }// end of if

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verification == PasswordVerificationResult.Failed)
            {// start of if (verification)
                return Unauthorized(new ApiError("Invalid email or password."));
            }// end of if (verification)

            // Store identity server-side in session -- this IS the authenticated state.
            HttpContext.Session.SetInt32(SessionAuthenticationHandler.SessionKeyUserId, user.UserID);
            HttpContext.Session.SetString(SessionAuthenticationHandler.SessionKeyRole, user.Role.RoleName);
            HttpContext.Session.SetString(SessionAuthenticationHandler.SessionKeyEmail, user.Email);
            HttpContext.Session.SetString(SessionAuthenticationHandler.SessionKeyFullName, user.FullName);

            return Ok(new AuthResponse
            {// start of return: returning the user info in the response body, but not the password hash
                UserId = user.UserID,
                Role = user.Role.RoleName,
                FullName = user.FullName
            }// end of return 
            );
        }// end of Login

        /// Ends the current user's session. Any authenticated user.
        // <response code="204">Session cleared.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Logout()
        {// start of Logout

            HttpContext.Session.Clear();
            return NoContent();

        }// end of Logout
    }// end of class authentication controller
}// end of namespace