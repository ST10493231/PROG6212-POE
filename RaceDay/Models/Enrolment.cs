using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models;

public class Enrolment
{
    public int EnrolmentID { get; set; }

    public int UserID { get; set; }
    public User User { get; set; } = null!;

    public int CategoryID { get; set; }
    public Category Category { get; set; } = null!;

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Confirmed";

    public Result? Result { get; set; }
}