using LMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace LMS.Models;

//Registration Association class handles relationship between student and courses
public class Registration
{
    public int Id {get; set; } //Primary Key

    [Required]
    public string? StudentID {get; set;} //foreign key for student table

    public int CourseID {get; set; } //foreign key for course table

    [DataType(DataType.DateTime)]
    public DateTime RegistrationDateTime {get; set;} //date and time of registration

}