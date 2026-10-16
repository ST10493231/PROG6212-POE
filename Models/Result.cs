using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class Result
{
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }
    public Enrolment Enrolment { get; set; } = null!;

    public TimeSpan? FinishTime { get; set; }

    public int? Position { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Finished";
}