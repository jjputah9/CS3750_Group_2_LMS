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
    public class CreateCourseUnitTests
    {
        private ApplicationDbContext GetTestContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("UnitTestDB")
                .Options;

            return new ApplicationDbContext(options);
        }

        private ClaimsPrincipal FakeInstructor()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "instructor@test.com")
            }, "TestAuth"));
        }

        [TestMethod]
        public async Task Instructor_Can_Create_Course()
        {
            // Arrange
            var context = GetTestContext();
            var pageModel = new CreateModel(context);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = FakeInstructor()
                }
            };

            pageModel.Course = new Course
            {
                DeptName = "CS",
                CourseNum = 9999,
                CourseTitle = "Unit Test Course",
                CreditHours = 3,
                Capacity = 20,
                Location = "Test Room",
                MeetDays = new bool[] { true, false, false, false, false },
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10)
            };


            // Act
            IActionResult result = await pageModel.OnPostAsync();

            // Assert redirect
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            // Assert course saved
            Assert.IsTrue(context.Course.Any(c => c.CourseNum == 9999));

            // Assert instructor assigned
            var course = context.Course.First(c => c.CourseNum == 9999);
            Assert.AreEqual("instructor@test.com", course.InstructorEmail);
        }
        // when title is missing, the model state should be invalid and the course should not be created
        [TestMethod]
        public async Task Create_Fails_When_Title_Missing()
        {
            var context = GetTestContext();
            var pageModel = new CreateModel(context);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = FakeInstructor()
                }
            };

            pageModel.Course = new Course
            {
                DeptName = "CS",
                CourseNum = 1234,
                CreditHours = 3,
                Capacity = 20,
                MeetDays = new bool[] { true, false, false, false, false },
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10)
                // Missing CourseTitle
            };

            pageModel.ModelState.AddModelError("Course.CourseTitle", "Required");

            var result = await pageModel.OnPostAsync();

            Assert.IsInstanceOfType(result, typeof(PageResult));
            Assert.IsFalse(context.Course.Any(c => c.CourseNum == 1234));
        }

        [TestMethod]
        public async Task Instructor_Email_Is_Set_Automatically()
        {
            var context = GetTestContext();
            var pageModel = new CreateModel(context);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = FakeInstructor()
                }
            };

            pageModel.Course = new Course
            {
                DeptName = "CS",
                CourseNum = 5678,
                CourseTitle = "Email Test",
                CreditHours = 3,
                Capacity = 25,
                MeetDays = new bool[] { true, true, false, false, false },
                StartTime = DateTime.Today.AddHours(11),
                EndTime = DateTime.Today.AddHours(12)
            };

            await pageModel.OnPostAsync();

            var course = context.Course.First(c => c.CourseNum == 5678);

            Assert.AreEqual("instructor@test.com", course.InstructorEmail);
        }

        [TestMethod]
        public async Task Create_Fails_When_Not_Logged_In()
        {
            var context = GetTestContext();
            var pageModel = new CreateModel(context);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext() // no user
            };

            pageModel.Course = new Course
            {
                DeptName = "CS",
                CourseNum = 4321,
                CourseTitle = "Auth Test",
                CreditHours = 3,
                Capacity = 20,
                MeetDays = new bool[] { true, false, false, false, false },
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10)
            };

            var result = await pageModel.OnPostAsync();

            Assert.IsFalse(context.Course.Any(c => c.CourseNum == 4321));
        }


        [TestMethod]
        public async Task Course_Data_Saved_Correctly()
        {
            var context = GetTestContext();
            var pageModel = new CreateModel(context);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = FakeInstructor()
                }
            };

            pageModel.Course = new Course
            {
                DeptName = "CS",
                CourseNum = 1111,
                CourseTitle = "Data Check",
                CreditHours = 4,
                Capacity = 30,
                Location = "Room 101",
                MeetDays = new bool[] { false, true, false, true, false },
                StartTime = DateTime.Today.AddHours(14),
                EndTime = DateTime.Today.AddHours(15)
            };

            await pageModel.OnPostAsync();

            var course = context.Course.First(c => c.CourseNum == 1111);

            Assert.AreEqual("Data Check", course.CourseTitle);
            Assert.AreEqual(4, course.CreditHours);
        }



    }

}
