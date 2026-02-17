using LMS.Data;
using LMS.Models;
using LMS.Pages.Registrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LMS_Test;

[TestClass]
public class StudentTests
{
    [TestMethod]
    public async Task CanStudentRegisterForCourse()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Data Source=titan.cs.weber.edu,10433;Initial Catalog=3750_Group2_Spr26;User ID=3750_Group2_Spr26;Password=Group2!;TrustServerCertificate=True;")
            .Options;

        using var context = new ApplicationDbContext(options);

        // define student to use
        var testStudentId = "be24302f-247c-409e-884a-bb6b19f0d260";
        // define course to use (26)
        var testCourseId = 26;

        // test if student exists (it should)
        if (!context.Users.Any(c => c.Id == testStudentId))
        { 
            throw new Exception($"Test setup error: student with ID {testStudentId} does not exist.");
        }

        // test if course exists (it should)
        if (!context.Course.Any(c => c.Id == testCourseId))
        {
            throw new Exception($"Test setup error: course with ID {testCourseId} does not exist.");
        }

        // make sure student is not already registered
        var existing = context.Registration
            .FirstOrDefault(r => r.StudentID == testStudentId
                      && r.CourseID == testCourseId);

        // if is registered, remove it
        if (existing != null)
        {
            context.Registration.Remove(existing);
            context.SaveChanges();
        }

        // create page model
        var pageModel = new CreateModel(null, context);

        // simulate a form submission
        pageModel.Registration = new Registration
        {
            StudentID = testStudentId,
            CourseID = testCourseId
        };

        // get courses registerd in
        var beforeCount = await context.Registration.CountAsync(r => r.StudentID == testStudentId);


        // call the registration method
        await pageModel.OnPostAsync();


        // count registed courses after registering
        var afterCount = await context.Registration.CountAsync(r => r.StudentID == testStudentId);
        Assert.AreEqual(beforeCount + 1, afterCount);

        // remove the registration just added so test can be rerun
        var newReg = await context.Registration
                .FirstAsync(r => r.StudentID == testStudentId && r.CourseID == testCourseId);

        context.Registration.Remove(newReg);
        context.SaveChanges();
    }
}
