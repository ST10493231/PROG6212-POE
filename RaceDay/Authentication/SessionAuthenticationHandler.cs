using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RaceDay.Authentication;

/// Reads the authenticated identity (UserId, Role, Email, FullName) straight out of
/// server-side ASP.NET Core session state. The session is created on login by
/// AuthenticationController and persists on the server, keyed by a session-ID cookie the
/// browser/client sends back on every subsequent request, this is the
/// "server-side session stores user ID and role" mechanism, as opposed to a
/// stateless, client-held JWT.

public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "RaceDaySession";

    public const string SessionKeyUserId = "UserId";
    public const string SessionKeyRole = "Role";
    public const string SessionKeyEmail = "Email";
    public const string SessionKeyFullName = "FullName";

#pragma warning disable CS0618 // ISystemClock is obsolete in net8 but still the supported ctor shape for AuthenticationHandler
    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }
#pragma warning restore CS0618

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Session middleware must run before authentication middleware
        // so Context.Session is populated by the time this executes.
        var userId = Context.Session.GetInt32(SessionKeyUserId);
        var role = Context.Session.GetString(SessionKeyRole);

        if (userId is null || string.IsNullOrEmpty(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new(ClaimTypes.Role, role)
        };

        var email = Context.Session.GetString(SessionKeyEmail);
        if (!string.IsNullOrEmpty(email)) claims.Add(new Claim(ClaimTypes.Email, email));

        var fullName = Context.Session.GetString(SessionKeyFullName);
        if (!string.IsNullOrEmpty(fullName)) claims.Add(new Claim(ClaimTypes.Name, fullName));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// Called by [Authorize] when there is no valid session — returns 401.
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    /// Called by [Authorize(Roles = ...)] when the session's role doesn't match — returns 403.
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
