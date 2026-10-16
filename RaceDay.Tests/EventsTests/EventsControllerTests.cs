using System.Net;
using System.Net.Http.Json;
using RaceDay.DTOs.Events;
using RaceDay.Models;
using RaceDay.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Tests.EventsTests;

public class EventsControllerTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EventsControllerTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private static CreateEventRequest SampleEvent() => new()
    {
        Name = "Pretoria Night Run",
        Description = "A 10km evening run through the city.",
        EventDate = DateTime.UtcNow.AddMonths(2),
        Location = "Pretoria, Gauteng",
        DistanceKm = 10.0m,
        EventType = "Run"
    };

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        // Each identity gets its own client so session cookies don't collide.
        var client = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(client, Role.Organiser);

        var response = await client.PostAsJsonAsync("/api/events", SampleEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(body);
        Assert.Equal("Pretoria Night Run", body!.Name);
        Assert.Equal("Run", body.EventType);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        // Verifies role-based access: Organiser functionality must not be
        // accessible to a Participant, even with a valid session.
        var client = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(client, Role.Participant);

        var response = await client.PostAsJsonAsync("/api/events", SampleEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", SampleEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsDifferentOrganiser_ReturnsForbidden()
    {
        // An Organiser must not be able to edit another Organiser's event.
        var ownerClient = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(ownerClient, Role.Organiser, "owner");
        var createResponse = await ownerClient.PostAsJsonAsync("/api/events", SampleEvent());
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        var otherClient = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(otherClient, Role.Organiser, "other");

        var updateResponse = await otherClient.PutAsJsonAsync($"/api/events/{created!.EventId}", SampleEvent());

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsOwningOrganiser_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(client, Role.Organiser);

        var createResponse = await client.PostAsJsonAsync("/api/events", SampleEvent());
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/events/{created!.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession_SubsequentProtectedCallIsUnauthorized()
    {
        // Confirms the session is genuinely server-side: after logout, the same
        // client (same cookie) can no longer reach a protected endpoint.
        var client = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(client, Role.Organiser);

        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var postLogoutResponse = await client.PostAsJsonAsync("/api/events", SampleEvent());
        Assert.Equal(HttpStatusCode.Unauthorized, postLogoutResponse.StatusCode);
    }
}