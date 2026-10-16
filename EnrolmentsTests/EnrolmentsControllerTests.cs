using System.Net;
using System.Net.Http.Json;
using RaceDay.DTOs.Categories;
using RaceDay.DTOs.Enrolments;
using RaceDay.DTOs.Events;
using RaceDay.Models;
using RaceDay.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Tests.EnrolmentsTests;

public class EnrolmentsControllerTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EnrolmentsControllerTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Creates an event with one category, owned by a fresh Organiser (on their own client), and returns the category ID.</summary>
    private async Task<int> CreateEventWithCategoryAsync()
    {
        var organiserClient = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(organiserClient, Role.Organiser);

        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            Name = "Soweto Marathon",
            Description = "42.2km road race.",
            EventDate = DateTime.UtcNow.AddMonths(3),
            Location = "Soweto, Gauteng",
            DistanceKm = 42.2m,
            EventType = "Run"
        });
        var ev = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();

        var categoryResponse = await organiserClient.PostAsJsonAsync($"/api/events/{ev!.EventId}/categories", new CreateCategoryRequest
        {
            Name = "Open",
            DistanceKm = 42.2m,
            MaxParticipants = 2
        });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        return category!.CategoryId;
    }

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreatedAndRecordsEnrolment()
    {
        var categoryId = await CreateEventWithCategoryAsync();

        var participantClient = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(participantClient, Role.Participant);

        var enrolResponse = await participantClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);

        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.NotNull(enrolment);
        Assert.Equal(categoryId, enrolment!.CategoryId);
        Assert.Equal("Confirmed", enrolment.Status);

        // Verify it shows up in the participant's own enrolment list (same client/session).
        var myEnrolments = await participantClient.GetFromJsonAsync<List<EnrolmentResponse>>("/api/users/me/enrolments");
        Assert.Contains(myEnrolments!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        // Organisers manage events; enrolling is Participant-only functionality.
        var categoryId = await CreateEventWithCategoryAsync();

        var organiserClient = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(organiserClient, Role.Organiser, "organiser2");

        var response = await organiserClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_InSameCategory_ReturnsConflict()
    {
        var categoryId = await CreateEventWithCategoryAsync();

        var participantClient = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(participantClient, Role.Participant);

        var first = await participantClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await participantClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_Unauthenticated_ReturnsUnauthorized()
    {
        var categoryId = await CreateEventWithCategoryAsync();

        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_AsDifferentParticipant_ReturnsForbidden()
    {
        var categoryId = await CreateEventWithCategoryAsync();

        var participant1Client = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(participant1Client, Role.Participant, "p1");
        var enrolResponse = await participant1Client.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>();

        var participant2Client = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(participant2Client, Role.Participant, "p2");

        var cancelResponse = await participant2Client.DeleteAsync($"/api/enrolments/{enrolment!.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, cancelResponse.StatusCode);
    }
}