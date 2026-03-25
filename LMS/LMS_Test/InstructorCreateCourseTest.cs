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
using Moq;

using Microsoft.AspNetCore.Identity;

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

        private UserManager<ApplicationUser> FakeInstructor()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var uManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        uManager.Setup(userManager => userManager.FindByIdAsync(It.IsAny<string>()))
        .ReturnsAsync(new ApplicationUser{});
        uManager.Setup(userManager => userManager.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Instructor"))
        .ReturnsAsync(true);
        uManager.Setup(userManager => userManager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
        .ReturnsAsync(new ApplicationUser{UserType="Instructor"});

        return uManager.Object;
        }

        [TestMethod]
        public async Task Instructor_Can_Create_Course()
        {
            // Arrange
            var context = GetTestContext();
            var user = FakeInstructor();
            var pageModel = new CreateModel(context, user);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "instructor@test.com")
                    }, "TestAuth"))
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
                EndTime = DateTime.Today.AddHours(10),
                InstructorName = "Instructor, Test"
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
            var user = FakeInstructor();
            var pageModel = new CreateModel(context, user);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "instructor@test.com")
                    }, "TestAuth"))
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
                EndTime = DateTime.Today.AddHours(10),
                InstructorName = "Instructor, Test"
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
            var user = FakeInstructor();
            var pageModel = new CreateModel(context, user);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "instructor@test.com")
                    }, "TestAuth"))
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
                EndTime = DateTime.Today.AddHours(12),
                InstructorName = "Instructor, Test"
            };

            await pageModel.OnPostAsync();

            var course = context.Course.First(c => c.CourseNum == 5678);

            Assert.AreEqual("instructor@test.com", course.InstructorEmail);
        }

        [TestMethod]
        public async Task Create_Fails_When_Not_Logged_In()
        {
            var context = GetTestContext();
            var user = FakeInstructor();
            var pageModel = new CreateModel(context, user);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
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
                EndTime = DateTime.Today.AddHours(10),
                InstructorName = "Instructor, Test"
            };

            var result = await pageModel.OnPostAsync();

            Assert.IsFalse(context.Course.Any(c => c.CourseNum == 4321));
        }


        [TestMethod]
        public async Task Course_Data_Saved_Correctly()
        {
            var context = GetTestContext();
            var user = FakeInstructor();
            var pageModel = new CreateModel(context, user);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "instructor@test.com")
                    }, "TestAuth"))
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
                EndTime = DateTime.Today.AddHours(15),
                InstructorName = "Instructor, Test"
            };

            await pageModel.OnPostAsync();

            var course = context.Course.First(c => c.CourseNum == 1111);

            Assert.AreEqual("Data Check", course.CourseTitle);
            Assert.AreEqual(4, course.CreditHours);
        }



    }

}
