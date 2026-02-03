using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models;

public class Course
{
    public int Id {get; set;} //Internal ID and unique primary key

    [Required]
    public string? InstructorEmail {get; set;} //Foreign key for the course instructor

    [Required]
    [StringLength(5)]
    public string? DeptName {get; set;} //Department Name

    [Range(0,9999)]
    public int CourseNum {get; set;} //Course Number, nonnegative

    [Required]
    [StringLength(50)]
    public string? CourseTitle {get; set;} //Course Title

    [Range(0,20)]
    public int CreditHours {get; set;} //Credit Hours, nonnegative

    [Range(1,100)]
    public int Capacity {get; set;} //Capacity, at least 1

    [StringLength(100)]
    public string? Location {get; set;} //Location name

    [Length(5,5)]
    [Required]
    public bool[]? MeetDays {get; set;} //Meeting Days - Array of 5 booleans, monday-friday.

    [DataType(DataType.Time)]
    public DateTime StartTime {get; set;} //Starting Time

    [DataType(DataType.Time)]
    public DateTime EndTime {get; set;} //End Time
}
