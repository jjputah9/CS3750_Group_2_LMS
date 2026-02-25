using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LMS.Data;
using LMS.Models;
using LMS.Pages;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
namespace LMS_Test;

[TestClass]
public class RoleAccessTests
{
    private ApplicationDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private UserManager<ApplicationUser> GetUserManager(string role)
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var uManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        uManager.Setup(userManager => userManager.FindByIdAsync(It.IsAny<string>()))
        .ReturnsAsync(new ApplicationUser{});
        uManager.Setup(userManager => userManager.IsInRoleAsync(It.IsAny<ApplicationUser>(), role))
        .ReturnsAsync(true);

        return uManager.Object;
    }

    private async void SetupCourses(ApplicationDbContext context)
    {
        //Add test courses
        context.Course.Add(new Course
            {
                DeptName = "ART",
                CourseNum = 101,
                CourseTitle = "Test course A",
                CreditHours = 3,
                Capacity = 20,
                InstructorEmail = "instructor@test.com",
                MeetDays = new bool[] { true, false, false, false, false },
                StartTime = System.DateTime.Today.AddHours(9),
                EndTime = System.DateTime.Today.AddHours(10)
            });

        await context.SaveChangesAsync();
    }

    private void CleanupDatabase(ApplicationDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    // Tests that check if a student is denied access to create, delete, and edit courses

    [TestMethod]
    public async Task StudentCannotCreateCourse()
    {
        //Arrange
        var context = GetContext();
        
        LMS.Pages.Courses.CreateModel model = new LMS.Pages.Courses.CreateModel(context);

        model.PageContext = new PageContext(){
            HttpContext = new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "student@test.com"),
                    new Claim(ClaimTypes.Role, "Student")
                }
                , "TestAuth")
                )
            }
        };

        //Attempt to access 
        var receivedPage = model.OnGet();

        //Assert that the attempt returned a forbidden result
        Assert.IsInstanceOfType(receivedPage, typeof(ForbidResult));

        //Clean up
        CleanupDatabase(context);
    }

    [TestMethod]
    public async Task StudentCannotEditCourse()
    {
        //Arrange
        var context = GetContext();
        SetupCourses(context);
        
        LMS.Pages.Courses.EditModel model = new LMS.Pages.Courses.EditModel(context);

        model.PageContext = new PageContext(){
            HttpContext = new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "student@test.com"),
                    new Claim(ClaimTypes.Role, "Student")
                }
                , "TestAuth")
                )
            }
        };

        //Attempt to access 
        var receivedPage = model.OnGetAsync(context.Course.First().Id).Result;

        //Assert that the attempt returned a forbidden result
        Assert.IsInstanceOfType(receivedPage, typeof(ForbidResult));

        //Clean up
        CleanupDatabase(context);
    }

    [TestMethod]
    public async Task StudentCannotDeleteCourse()
    {
        //Arrange
        var context = GetContext();
        SetupCourses(context);
        
        LMS.Pages.Courses.DeleteModel model = new LMS.Pages.Courses.DeleteModel(context);

        model.PageContext = new PageContext(){
            HttpContext = new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "student@test.com"),
                    new Claim(ClaimTypes.Role, "Student")
                }
                , "TestAuth")
                )
            }
        };

        //Attempt to access 
        var receivedPage = model.OnGetAsync(context.Course.First().Id).Result;

        //Assert that the attempt returned a forbidden result
        Assert.IsInstanceOfType(receivedPage, typeof(ForbidResult));

        //Clean up
        CleanupDatabase(context);
    }

    // Tests if an instructor can view the registration page
    [TestMethod]
    public async Task InstructorCannotRegisterForCourse()
    {
        //Arrange
        var context = GetContext();
        var userManager = GetUserManager("Instructor");
        
        LMS.Pages.Registrations.CreateModel model = new LMS.Pages.Registrations.CreateModel(userManager, context);

        //Attempt to access 
        var receivedPage = model.OnGetAsync();

        //Assert that the attempt returned a forbidden result
        Assert.IsInstanceOfType(receivedPage, typeof(ForbidResult));

        //Clean up
        CleanupDatabase(context);
    }

    // Tests to check if a student can create, edit, or delete assignments
    [TestMethod]
    public async Task StudentCannotCreateAssignment()
    {
        //Arrange
        var context = GetContext();
        SetupCourses(context);
        var userManager = GetUserManager("Student");
        
        LMS.Pages.Assignments.CreateModel model = new LMS.Pages.Assignments.CreateModel(context, userManager);

        int courseId = context.Course.First().Id;

        model.PageContext = new PageContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                Session = new Mock<ISession>().Object
            }
        };

        model.HttpContext.Session.SetInt32("ActiveCourseId", courseId);

        //Attempt to access 
        var receivedPage = model.OnGetAsync(courseId).Result;

        //Assert that the attempt returned a forbidden result
        Assert.IsInstanceOfType(receivedPage, typeof(ForbidResult));

        //Clean up
        CleanupDatabase(context);
    }

    [TestMethod]
    public async Task StudentCannotEditAssignment()
    {
        //Arrange
        var context = GetContext();
        SetupCourses(context);
        var userManager = GetUserManager("Student");
        
        LMS.Pages.Assignments.EditModel model = new LMS.Pages.Assignments.EditModel(context, userManager);

        int courseId = context.Course.First().Id;

        model.PageContext = new PageContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                Session = new Mock<ISession>().Object
            }
        };

        model.HttpContext.Session.SetInt32("ActiveCourseId", courseId);

        //Attempt to access 
        var receivedPage = model.OnGetAsync(courseId).Result;

        //Assert that the attempt returned a forbidden result
        Assert.IsInstanceOfType(receivedPage, typeof(ForbidResult));

        //Clean up
        CleanupDatabase(context);
    }

    [TestMethod]
    public async Task StudentCannotDeleteAssignment()
    {
        //Arrange
        var context = GetContext();
        SetupCourses(context);
        var userManager = GetUserManager("Student");
        
        LMS.Pages.Assignments.DeleteModel model = new LMS.Pages.Assignments.DeleteModel(context, userManager);

        int courseId = context.Course.First().Id;

        model.PageContext = new PageContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                Session = new Mock<ISession>().Object
            }
        };

        model.HttpContext.Session.SetInt32("ActiveCourseId", courseId);

        //Attempt to access 
        var receivedPage = model.OnGetAsync(context.Course.First().Id).Result;

        //Assert that the attempt returned a forbidden result
        Assert.IsInstanceOfType(receivedPage, typeof(ForbidResult));

        //Clean up
        CleanupDatabase(context);
    }
}