using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LMS.Data;
using LMS.Models;
using LMS.Pages.Courses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LMS.Tests
{
    [TestClass]
    public class DeleteCourseUnitTests
    {
        private ApplicationDbContext GetTestContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UnitTestDB_Delete")
                .Options;

            return new ApplicationDbContext(options);
        }

        private UserManager<ApplicationUser> FakeInstructor(string email = "instructor@test.com")
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var uManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        uManager.Setup(userManager => userManager.FindByIdAsync(It.IsAny<string>()))
        .ReturnsAsync(new ApplicationUser{});
        uManager.Setup(userManager => userManager.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Instructor"))
        .ReturnsAsync(true);
        uManager.Setup(userManager => userManager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
        .ReturnsAsync(new ApplicationUser{UserType="Instructor", Email=email});

        return uManager.Object;
        }

        [TestMethod]
        public async Task Instructor_Can_Delete_Course()
        {
            // Arrange
            var context = GetTestContext();
            var course = new Course
            {
                DeptName = "CS",
                CourseNum = 101,
                CourseTitle = "Delete Test",
                CreditHours = 3,
                Capacity = 20,
                InstructorEmail = "instructor@test.com",
                MeetDays = new bool[] { true, false, false, false, false },
                StartTime = System.DateTime.Today.AddHours(9),
                EndTime = System.DateTime.Today.AddHours(10)
            };
            context.Course.Add(course);
            await context.SaveChangesAsync();

            var user = FakeInstructor();

            var pageModel = new DeleteModel(context, user)
            {
                Course = course,
                PageContext = new PageContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.Name, "instructor@test.com")
                        }, "TestAuth"))
                    }
                }
            };

            // Act
            IActionResult result = await pageModel.OnPostAsync(course.Id);

            // Assert redirect
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            // Assert course removed
            Assert.IsFalse(context.Course.Any(c => c.Id == course.Id));
        }

        [TestMethod]
        public async Task Cannot_Delete_Course_If_Not_Instructor()
        {
            // Arrange
            var context = GetTestContext();
            var course = new Course
            {
                DeptName = "CS",
                CourseNum = 102,
                CourseTitle = "Unauthorized Delete",
                CreditHours = 3,
                Capacity = 20,
                InstructorEmail = "otherinstructor@test.com",
                MeetDays = new bool[] { true, false, false, false, false },
                StartTime = System.DateTime.Today.AddHours(9),
                EndTime = System.DateTime.Today.AddHours(10)
            };
            context.Course.Add(course);
            await context.SaveChangesAsync();

            var user = FakeInstructor();

            var pageModel = new DeleteModel(context, user)
            {
                Course = course,
                PageContext = new PageContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        
                    }
                }
            };

            // Act
            IActionResult result = await pageModel.OnPostAsync(course.Id);

            // Assert course still exists
            Assert.IsTrue(context.Course.Any(c => c.Id == course.Id));
        }

        [TestMethod]
        public async Task Cannot_Delete_Course_If_Not_Logged_In()
        {
            // Arrange
            var context = GetTestContext();
            var course = new Course
            {
                DeptName = "CS",
                CourseNum = 103,
                CourseTitle = "Anonymous Delete",
                CreditHours = 3,
                Capacity = 20,
                InstructorEmail = "instructor@test.com",
                MeetDays = new bool[] { true, false, false, false, false },
                StartTime = System.DateTime.Today.AddHours(9),
                EndTime = System.DateTime.Today.AddHours(10)
            };
            context.Course.Add(course);
            await context.SaveChangesAsync();

            var uManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            var pageModel = new DeleteModel(context, uManager.Object)
            {
                Course = course,
                PageContext = new PageContext
                {
                    HttpContext = new DefaultHttpContext() // no user
                }
            };

            // Act
            IActionResult result = await pageModel.OnPostAsync(course.Id);

            // Assert course still exists
            Assert.IsTrue(context.Course.Any(c => c.Id == course.Id));
        }
    }
}
