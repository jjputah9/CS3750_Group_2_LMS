using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LMS.Data;
using LMS.Models;
using LMS.Pages.Registrations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
namespace LMS_Test;

[TestClass]
public class SearchBarUnitTests
{
    private ApplicationDbContext GetTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
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

        context.Course.Add(new Course
            {
                DeptName = "HIST",
                CourseNum = 101,
                CourseTitle = "Test course B",
                CreditHours = 6,
                Capacity = 20,
                InstructorEmail = "instructor@test.com",
                MeetDays = new bool[] { true, false, false, false, false },
                StartTime = System.DateTime.Today.AddHours(9),
                EndTime = System.DateTime.Today.AddHours(10)
            });

        context.Course.Add(new Course
            {
                DeptName = "ART",
                CourseNum = 101,
                CourseTitle = "Test course C",
                CreditHours = 6,
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

    private CreateModel SetupPageModel(ApplicationDbContext context)
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var uManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        uManager.Setup(userManager => userManager.FindByIdAsync(It.IsAny<string>()))
        .ReturnsAsync(new ApplicationUser{});
        uManager.Setup(userManager => userManager.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Student"))
        .ReturnsAsync(true);
        
        return new CreateModel(uManager.Object, context);
    }

    [TestMethod]
    // If the search bar is given an empty string and the search button is pressed, all courses should appear
    public async Task EmptySearchBarTest()
    {
        //Arrange
        var context = GetTestContext();
        SetupCourses(context);
        var pageModel = SetupPageModel(context);

        await pageModel.OnGetAsync();
        
        //Get default list of displayed courses
        var defaultCourses = pageModel.Courses;

        //Act
        pageModel.SearchTerm = "";
        await pageModel.OnGetAsync();

        var searchedCourses = pageModel.Courses;

        //Assert all courses remain
        Assert.HasCount(defaultCourses.Count, searchedCourses);

        //Clean up database
        CleanupDatabase(context);
    }

    [TestMethod]
    // The search query removes irrelevant courses
    public async Task SearchQueryRemovesCoursesTest()
    {
        //Arrange
        var context = GetTestContext();
        SetupCourses(context);
        var pageModel = SetupPageModel(context);

        await pageModel.OnGetAsync();
        
        //Get default list of displayed courses
        var defaultCourses = pageModel.Courses;

        //Act
        pageModel.SearchTerm = "A"; //One course does not match this string
        await pageModel.OnGetAsync();

        var searchedCourses = pageModel.Courses;

        //Assert one course has been removed
        Assert.HasCount(defaultCourses.Count - 1, searchedCourses);

        //Assert the remaining course matches the pattern
        Assert.IsTrue((searchedCourses.First().CourseTitle ?? "").Contains('A') || (searchedCourses.First().DeptName ?? "").Contains('A'));

        //Clean up database
        CleanupDatabase(context);
    }

    [TestMethod]
    // The department dropdown removes irrelevant courses
    public async Task DepartmentDropdownRemovesCoursesTest()
    {
        //Arrange
        var context = GetTestContext();
        SetupCourses(context);
        var pageModel = SetupPageModel(context);

        await pageModel.OnGetAsync();
        
        //Get default list of displayed courses
        var defaultCourses = pageModel.Courses;

        //Act
        pageModel.SelectedDepartment = "HIST"; //only one course is in HIST
        await pageModel.OnGetAsync();

        var searchedCourses = pageModel.Courses;

        //Assert one course has been removed
        Assert.HasCount(1, searchedCourses);

        //Assert the remaining course matches the pattern
        Assert.AreEqual("HIST", searchedCourses.First().DeptName ?? "");

        //Clean up database
        CleanupDatabase(context);
    }

    [TestMethod]
    // The credit hours removes irrelevant courses
    public async Task CreditHoursRemoveCoursesTest()
    {
        //Arrange
        var context = GetTestContext();
        SetupCourses(context);
        var pageModel = SetupPageModel(context);

        await pageModel.OnGetAsync();
        
        //Get default list of displayed courses
        var defaultCourses = pageModel.Courses;

        //Act
        pageModel.SelectedCredits = 6; //one course does not have 6 credits
        await pageModel.OnGetAsync();

        var searchedCourses = pageModel.Courses;

        //Assert one course has been removed
        Assert.HasCount(defaultCourses.Count - 1, searchedCourses);

        //Assert the remaining course matches the pattern
        Assert.AreEqual(6, searchedCourses.First().CreditHours);

        //Clean up database
        CleanupDatabase(context);
    }

    [TestMethod]
    // Several fields work at once
    public async Task SeveralFieldsRemoveCoursesTest()
    {
        //Arrange
        var context = GetTestContext();
        SetupCourses(context);
        var pageModel = SetupPageModel(context);

        await pageModel.OnGetAsync();
        
        //Get default list of displayed courses
        var defaultCourses = pageModel.Courses;

        //Act
        //Only one course matches all three fields, but each individually matches more than one
        pageModel.SearchTerm = "C";
        pageModel.SelectedDepartment = "ART";
        pageModel.SelectedCredits = 6; 
        await pageModel.OnGetAsync();

        var searchedCourses = pageModel.Courses;

        //Assert all but one course has been removed
        Assert.HasCount(1, searchedCourses);

        //Assert the remaining course matches the pattern
        Assert.IsTrue((searchedCourses.First().CourseTitle ?? "").Contains('C') || (searchedCourses.First().DeptName ?? "").Contains('C'));
        Assert.AreEqual("ART", searchedCourses.First().DeptName ?? "");
        Assert.AreEqual(6, searchedCourses.First().CreditHours);

        //Clean up database
        CleanupDatabase(context);
    }
}