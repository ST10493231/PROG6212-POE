using RaceDay.Models;

namespace RaceDay.DTOs.Enrolments;

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasResult { get; set; }

    public static EnrolmentResponse FromEntity(Enrolment en) => new()
    {
        EnrolmentId = en.EnrolmentID,
        UserId = en.UserID,
        ParticipantName = en.User?.FullName ?? string.Empty,
        CategoryId = en.CategoryID,
        CategoryName = en.Category?.Name ?? string.Empty,
        EventId = en.Category?.EventID ?? 0,
        EventName = en.Category?.Event?.Name ?? string.Empty,
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status,
        HasResult = en.Result != null
    };
}