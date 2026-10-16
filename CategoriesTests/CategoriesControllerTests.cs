using System.Net;
using System.Net.Http.Json;
using RaceDay.DTOs.Categories;
using RaceDay.DTOs.Events;
using RaceDay.Models;
using RaceDay.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Tests.CategoriesTests;

public class CategoriesControllerTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public CategoriesControllerTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient OwnerClient, int EventId)> CreateEventAsync(string emailPrefix = "catowner")
    {
        var client = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(client, Role.Organiser, emailPrefix);

        var response = await client.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            Name = "Benoni Park Run",
            Description = "A relaxed weekend park run.",
            EventDate = DateTime.UtcNow.AddMonths(1),
            Location = "Benoni, Gauteng",
            DistanceKm = 5.0m,
            EventType = "Run"
        });
        var ev = await response.Content.ReadFromJsonAsync<EventResponse>();

        return (client, ev!.EventId);
    }

    [Fact]
    public async Task CreateCategory_AsOwningOrganiser_ReturnsCreated()
    {
        var (client, eventId) = await CreateEventAsync();

        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new CreateCategoryRequest
        {
            Name = "5km",
            DistanceKm = 5.0m,
            MaxParticipants = 200
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(body);
        Assert.Equal("5km", body!.Name);
    }

    [Fact]
    public async Task CreateCategory_AsNonOwningOrganiser_ReturnsForbidden()
    {
        var (_, eventId) = await CreateEventAsync("catowner2");

        var otherOrganiser = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(otherOrganiser, Role.Organiser, "otherorganiser");

        var response = await otherOrganiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CreateCategoryRequest
        {
            Name = "10km",
            DistanceKm = 10.0m,
            MaxParticipants = 100
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_ReturnsForbidden()
    {
        var (_, eventId) = await CreateEventAsync("catowner3");

        var participant = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(participant, Role.Participant);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", new CreateCategoryRequest
        {
            Name = "10km",
            DistanceKm = 10.0m,
            MaxParticipants = 100
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCategoriesForEvent_Unauthenticated_ReturnsOk()
    {
        var (client, eventId) = await CreateEventAsync("catowner4");
        await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new CreateCategoryRequest
        {
            Name = "Under 20",
            MaxParticipants = 50
        });

        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        Assert.Contains(categories!, c => c.Name == "Under 20");
    }

    [Fact]
    public async Task DeleteCategory_AsOwningOrganiser_ReturnsNoContent()
    {
        var (client, eventId) = await CreateEventAsync("catowner5");
        var createResponse = await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new CreateCategoryRequest
        {
            Name = "21km",
            DistanceKm = 21.0m,
            MaxParticipants = 150
        });
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/categories/{category!.CategoryId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}