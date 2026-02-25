using LMS.Data;
using LMS.Models;
using LMS.Pages.Courses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LMS_Test
{
    [TestClass]
    public class InstructorEditCourseTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        [TestMethod]
        public async Task EditCourse_WithValidChanges_PersistsUpdates_AndRedirects()
        {
            using var context = CreateContext(nameof(EditCourse_WithValidChanges_PersistsUpdates_AndRedirects));

            context.Course.Add(new Course
            {
                Id = 500,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 2500,
                CourseTitle = "Before",
                CreditHours = 3,
                Capacity = 25,
                Location = "Old",
                MeetDays = new[] { true, false, false, false, false },
                StartTime = DateTime.Today.AddHours(10),
                EndTime = DateTime.Today.AddHours(11)
            });
            await context.SaveChangesAsync();

            var page = new EditModel(context);
            page.Course = new Course
            {
                Id = 500,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 2500,
                CourseTitle = "After",
                CreditHours = 4,
                Capacity = 40,
                Location = "New",
                MeetDays = new[] { true, true, false, false, false },
                StartTime = DateTime.Today.AddHours(12),
                EndTime = DateTime.Today.AddHours(13)
            };

            var result = await page.OnPostAsync();

            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            var updated = await context.Course.FirstAsync(c => c.Id == 500);
            Assert.AreEqual("After", updated.CourseTitle);
            Assert.AreEqual(4, updated.CreditHours);
            Assert.AreEqual(40, updated.Capacity);
            Assert.AreEqual("New", updated.Location);
            Assert.IsTrue(updated.MeetDays.Any(d => d));
        }

        [TestMethod]
        public async Task EditCourse_WithNoMeetDaysSelected_ReturnsPage_AndDoesNotUpdate()
        {
            using var context = CreateContext(nameof(EditCourse_WithNoMeetDaysSelected_ReturnsPage_AndDoesNotUpdate));

            context.Course.Add(new Course
            {
                Id = 501,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 2600,
                CourseTitle = "Original Title",
                CreditHours = 3,
                Capacity = 25,
                Location = "Room X",
                MeetDays = new[] { true, false, false, false, false },
                StartTime = DateTime.Today.AddHours(10),
                EndTime = DateTime.Today.AddHours(11)
            });
            await context.SaveChangesAsync();

            var page = new EditModel(context);
            page.Course = new Course
            {
                Id = 501,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 2600,
                CourseTitle = "Should Not Save",
                CreditHours = 3,
                Capacity = 25,
                Location = "Room X",
                MeetDays = new[] { false, false, false, false, false }, // invalid
                StartTime = DateTime.Today.AddHours(10),
                EndTime = DateTime.Today.AddHours(11)
            };

            var result = await page.OnPostAsync();

            Assert.IsInstanceOfType(result, typeof(PageResult));

            var notUpdated = await context.Course.FirstAsync(c => c.Id == 501);
            Assert.AreEqual("Original Title", notUpdated.CourseTitle);
        }
    }
}