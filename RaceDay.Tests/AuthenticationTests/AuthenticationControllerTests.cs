using System.Net;
using System.Net.Http.Json;
using RaceDay.Tests.TestHelpers;
using RaceDay.DTOs.Authentication;
using RaceDay.Models;
using RaceDay.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Tests.AuthTests;

public class AuthenticationControllerTests : IClassFixture<RaceDayApiFactory>
{
    private readonly HttpClient _client;

    public AuthenticationControllerTests(RaceDayApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidParticipantData_ReturnsCreated()
    {
        var request = new RegisterRequest
        {
            FullName = "Naledi Molefe",
            Email = $"naledi-{Guid.NewGuid():N}@test.raceday.co.za",
            Password = "P@ssw0rd123!",
            Role = Role.Participant
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        Assert.NotNull(body);
        Assert.Equal(request.Email, body!.Email);
        Assert.Equal(Role.Participant, body.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@test.raceday.co.za";
        var request = new RegisterRequest
        {
            FullName = "First User",
            Email = email,
            Password = "P@ssw0rd123!",
            Role = Role.Organiser
        };

        var first = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Second User",
            Email = email,
            Password = "P@ssw0rd123!",
            Role = Role.Organiser
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        var request = new RegisterRequest
        {
            FullName = "Bad Role User",
            Email = $"badrole-{Guid.NewGuid():N}@test.raceday.co.za",
            Password = "P@ssw0rd123!",
            Role = "SuperAdmin"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndCorrectRole()
    {
        var email = $"login-{Guid.NewGuid():N}@test.raceday.co.za";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Login Test",
            Email = email,
            Password = "P@ssw0rd123!",
            Role = Role.Organiser
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "P@ssw0rd123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal(Role.Organiser, body!.Role);

        // A session cookie should have been set for this client.
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith("RaceDay.Session"));
    }

    [Fact]
    public async Task Login_ThenAccessProtectedEndpoint_SessionPersists()
    {
        // Confirms the server-side session actually authenticates the next request
        // on the same client, without any token being attached manually.
        var email = $"session-{Guid.NewGuid():N}@test.raceday.co.za";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Session Persistence Test",
            Email = email,
            Password = "P@ssw0rd123!",
            Role = Role.Participant
        });
        await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "P@ssw0rd123!"
        });

        var profileResponse = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"wrongpw-{Guid.NewGuid():N}@test.raceday.co.za";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Wrong Password Test",
            Email = email,
            Password = "P@ssw0rd123!",
            Role = Role.Participant
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "TotallyWrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = $"doesnotexist-{Guid.NewGuid():N}@test.raceday.co.za",
            Password = "Whatever123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}