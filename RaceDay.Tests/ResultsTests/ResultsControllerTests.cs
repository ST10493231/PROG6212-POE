using System.Net;
using System.Net.Http.Json;
using RaceDay.DTOs.Categories;
using RaceDay.DTOs.Enrolments;
using RaceDay.DTOs.Events;
using RaceDay.DTOs.Results;
using RaceDay.Models;
using RaceDay.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Tests.ResultsTests;

public class ResultsControllerTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public ResultsControllerTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient OrganiserClient, HttpClient ParticipantClient, int EnrolmentId)> CreateEnrolledParticipantAsync(string prefix)
    {
        var organiserClient = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(organiserClient, Role.Organiser, $"{prefix}-organiser");

        var eventResponse = await organiserClient.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            Name = "Centurion 10km",
            Description = "Flat, fast 10km road race.",
            EventDate = DateTime.UtcNow.AddMonths(1),
            Location = "Centurion, Gauteng",
            DistanceKm = 10.0m,
            EventType = "Run"
        });
        var ev = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();

        var categoryResponse = await organiserClient.PostAsJsonAsync($"/api/events/{ev!.EventId}/categories", new CreateCategoryRequest
        {
            Name = "Open 10km",
            DistanceKm = 10.0m,
            MaxParticipants = 100
        });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var participantClient = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(participantClient, Role.Participant, $"{prefix}-participant");

        var enrolResponse = await participantClient.PostAsync($"/api/categories/{category!.CategoryId}/enrolments", null);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>();

        return (organiserClient, participantClient, enrolment!.EnrolmentId);
    }

    [Fact]
    public async Task CaptureResult_AsOwningOrganiser_ReturnsCreated()
    {
        var (organiserClient, _, enrolmentId) = await CreateEnrolledParticipantAsync("capture1");

        var response = await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new CaptureResultRequest
        {
            FinishTime = "00:45:30",
            Position = 3,
            Status = "Finished"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.NotNull(body);
        Assert.Equal(3, body!.Position);
        Assert.Equal("00:45:30", body.FinishTime);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_ReturnsForbidden()
    {
        var (_, participantClient, enrolmentId) = await CreateEnrolledParticipantAsync("capture2");

        var response = await participantClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new CaptureResultRequest
        {
            FinishTime = "00:45:30",
            Position = 3
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var (organiserClient, _, enrolmentId) = await CreateEnrolledParticipantAsync("capture3");

        var first = await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new CaptureResultRequest
        {
            FinishTime = "00:45:30",
            Position = 3
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new CaptureResultRequest
        {
            FinishTime = "00:46:00",
            Position = 4
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_AsParticipant_IncludesCapturedResult()
    {
        var (organiserClient, participantClient, enrolmentId) = await CreateEnrolledParticipantAsync("capture4");

        await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new CaptureResultRequest
        {
            FinishTime = "00:45:30",
            Position = 3
        });

        var results = await participantClient.GetFromJsonAsync<List<ResultResponse>>("/api/users/me/results");

        Assert.NotNull(results);
        Assert.Contains(results!, r => r.EnrolmentId == enrolmentId);
    }

    [Fact]
    public async Task GetResult_AsUnrelatedParticipant_ReturnsForbidden()
    {
        var (organiserClient, _, enrolmentId) = await CreateEnrolledParticipantAsync("capture5");
        await organiserClient.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new CaptureResultRequest
        {
            FinishTime = "00:45:30",
            Position = 3
        });

        var unrelatedParticipant = _factory.CreateClient();
        await AuthTestHelper.RegisterAndLoginAsync(unrelatedParticipant, Role.Participant, "unrelated");

        var response = await unrelatedParticipant.GetAsync($"/api/enrolments/{enrolmentId}/result");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}