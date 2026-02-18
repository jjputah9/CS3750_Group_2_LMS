using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LMS.Data;
using LMS.Models;
using LMS.Pages.Courses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        private ClaimsPrincipal FakeInstructor(string email = "instructor@test.com")
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, email)
            }, "TestAuth"));
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

            var pageModel = new DeleteModel(context)
            {
                Course = course,
                PageContext = new PageContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = FakeInstructor()
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

            var pageModel = new DeleteModel(context)
            {
                Course = course,
                PageContext = new PageContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = FakeInstructor("instructor@test.com") // different instructor
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

            var pageModel = new DeleteModel(context)
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
