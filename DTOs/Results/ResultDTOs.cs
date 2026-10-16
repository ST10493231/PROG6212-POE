using System.ComponentModel.DataAnnotations;
using RaceDay.Models;

namespace RaceDay.DTOs.Results;

public class CaptureResultRequest
{
    // Format "hh:mm:ss", e.g. "00:24:12"
    [Required]
    public string FinishTime { get; set; } = string.Empty;

    [Required, Range(1, 100000)]
    public int Position { get; set; }

    public string Status { get; set; } = "Finished";
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;

    public static ResultResponse FromEntity(Result r) => new()
    {
        ResultId = r.ResultID,
        EnrolmentId = r.EnrolmentID,
        ParticipantName = r.Enrolment?.User?.FullName ?? string.Empty,
        EventName = r.Enrolment?.Category?.Event?.Name ?? string.Empty,
        CategoryName = r.Enrolment?.Category?.Name ?? string.Empty,
        FinishTime = r.FinishTime?.ToString(@"hh\:mm\:ss"),
        Position = r.Position,
        Status = r.Status
    };
}