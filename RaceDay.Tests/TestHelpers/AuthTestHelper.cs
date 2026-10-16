using System.Net.Http.Json;
using RaceDay.DTOs.Authentication;


namespace RaceDay.Tests.TestHelpers;

public static class AuthTestHelper
{
    /// Registers a new user with the given role and logs in on the given HttpClient.
    /// Because WebApplicationFactory clients handle cookies by default, the session
    /// cookie set by /api/auth/login is stored on this client and sent automatically
    /// on every later request made with it -- exactly like a browser.
    ///
    /// Each test identity needs its own HttpClient instance (via factory.CreateClient())
    /// so sessions for different users don't collide on the same cookie jar.

    public static async Task<AuthResponse> RegisterAndLoginAsync(HttpClient client, string role, string? emailPrefix = null)
    {
        var email = $"{emailPrefix ?? role.ToLower()}-{Guid.NewGuid():N}@test.raceday.co.za";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = $"Test {role}",
            Email = email,
            Password = "P@ssw0rd123!",
            Role = role
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "P@ssw0rd123!"
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authResult!;
    }
}