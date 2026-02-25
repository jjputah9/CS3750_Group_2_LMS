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
        private static DbContextOptions<ApplicationDbContext> BuildOptions(string dbName)
            => new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

        private static async Task SeedCourseAsync(DbContextOptions<ApplicationDbContext> options, Course course)
        {
            using var seed = new ApplicationDbContext(options);
            seed.Course.Add(course);
            await seed.SaveChangesAsync();
        }

        private static async Task<Course> ReadCourseAsync(DbContextOptions<ApplicationDbContext> options, int id)
        {
            using var read = new ApplicationDbContext(options);
            return await read.Course.AsNoTracking().FirstAsync(c => c.Id == id);
        }

        // ----------------------------
        // 1) Happy path: valid edit
        // ----------------------------
        [TestMethod]
        public async Task EditCourse_WithValidChanges_PersistsUpdates_AndRedirects()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = BuildOptions(dbName);

            await SeedCourseAsync(options, new Course
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

            using (var act = new ApplicationDbContext(options))
            {
                var page = new EditModel(act)
                {
                    Course = new Course
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
                    }
                };

                var result = await page.OnPostAsync();
                Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));
                Assert.IsTrue(string.IsNullOrWhiteSpace(page.MeetDayWarning));
            }

            var updated = await ReadCourseAsync(options, 500);
            Assert.AreEqual("After", updated.CourseTitle);
            Assert.AreEqual(4, updated.CreditHours);
            Assert.AreEqual(40, updated.Capacity);
            Assert.AreEqual("New", updated.Location);
            Assert.IsTrue(updated.MeetDays != null && updated.MeetDays.Contains(true));
        }

        // ---------------------------------------------------------
        // 2) Validation: MeetDays invalid -> Page + warning + no save
        // ---------------------------------------------------------
        [TestMethod]
        public async Task EditCourse_WithNoMeetDaysSelected_ReturnsPage_SetsWarning_AndDoesNotUpdate()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = BuildOptions(dbName);

            await SeedCourseAsync(options, new Course
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

            using (var act = new ApplicationDbContext(options))
            {
                var page = new EditModel(act)
                {
                    Course = new Course
                    {
                        Id = 501,
                        InstructorEmail = "instructor@test.com",
                        DeptName = "CS",
                        CourseNum = 2600,
                        CourseTitle = "Should Not Save",
                        CreditHours = 3,
                        Capacity = 25,
                        Location = "Room X",
                        MeetDays = new bool[] { false, false, false, false, false }, // invalid
                        StartTime = DateTime.Today.AddHours(10),
                        EndTime = DateTime.Today.AddHours(11)
                    }
                };

                var result = await page.OnPostAsync();
                Assert.IsInstanceOfType(result, typeof(PageResult));
                Assert.AreEqual("At least one day needs to be selected.", page.MeetDayWarning);
            }

            var notUpdated = await ReadCourseAsync(options, 501);
            Assert.AreEqual("Original Title", notUpdated.CourseTitle);
        }

        // ---------------------------------------------------------
        // 3) Validation: ModelState invalid -> Page + no save
        //    (Only valid if your EditModel checks ModelState.IsValid)
        // ---------------------------------------------------------
        [TestMethod]
        public async Task EditCourse_WithInvalidModelState_ReturnsPage_AndDoesNotUpdate()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = BuildOptions(dbName);

            await SeedCourseAsync(options, new Course
            {
                Id = 502,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 2700,
                CourseTitle = "Original",
                CreditHours = 3,
                Capacity = 25,
                Location = "Loc",
                MeetDays = new[] { true, false, false, false, false },
                StartTime = DateTime.Today.AddHours(10),
                EndTime = DateTime.Today.AddHours(11)
            });

            using (var act = new ApplicationDbContext(options))
            {
                var page = new EditModel(act)
                {
                    Course = new Course
                    {
                        Id = 502,
                        InstructorEmail = "instructor@test.com",
                        DeptName = "CS",
                        CourseNum = 2700,
                        CourseTitle = "Attempted Update",
                        CreditHours = 4,
                        Capacity = 30,
                        Location = "New Loc",
                        MeetDays = new[] { true, false, false, false, false },
                        StartTime = DateTime.Today.AddHours(12),
                        EndTime = DateTime.Today.AddHours(13)
                    }
                };

                // Force invalid ModelState to simulate validation failure
                page.ModelState.AddModelError("Course.CourseTitle", "Required");

                var result = await page.OnPostAsync();

                // If this fails (redirect instead), your EditModel likely doesn’t check ModelState.
                Assert.IsInstanceOfType(result, typeof(PageResult));
            }

            var notUpdated = await ReadCourseAsync(options, 502);
            Assert.AreEqual("Original", notUpdated.CourseTitle);
        }

        // ---------------------------------------------------------
        // 4) OnGet: id null -> NotFound (if OnGetAsync(int? id) exists)
        // ---------------------------------------------------------
        [TestMethod]
        public async Task EditCourse_OnGet_NullId_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = BuildOptions(dbName);

            using var context = new ApplicationDbContext(options);
            var page = new EditModel(context);

            // Remove/adjust if your method signature differs
            var result = await page.OnGetAsync(null);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        // ---------------------------------------------------------
        // 5) OnGet: non-existent course id -> NotFound
        // ---------------------------------------------------------
        [TestMethod]
        public async Task EditCourse_OnGet_NonExistentId_ReturnsNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = BuildOptions(dbName);

            using var context = new ApplicationDbContext(options);
            var page = new EditModel(context);

            var result = await page.OnGetAsync(999999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        // ---------------------------------------------------------
        // 6) Valid edit updates multiple fields (mapping completeness)
        // ---------------------------------------------------------
        [TestMethod]
        public async Task EditCourse_ValidEdit_UpdatesMultipleFields()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = BuildOptions(dbName);

            await SeedCourseAsync(options, new Course
            {
                Id = 503,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 2800,
                CourseTitle = "Before",
                CreditHours = 1,
                Capacity = 10,
                Location = "A",
                MeetDays = new[] { true, false, false, false, false },
                StartTime = DateTime.Today.AddHours(8),
                EndTime = DateTime.Today.AddHours(9)
            });

            using (var act = new ApplicationDbContext(options))
            {
                var page = new EditModel(act)
                {
                    Course = new Course
                    {
                        Id = 503,
                        InstructorEmail = "instructor@test.com",
                        DeptName = "CS",
                        CourseNum = 2800,
                        CourseTitle = "After",
                        CreditHours = 5,
                        Capacity = 55,
                        Location = "B",
                        MeetDays = new[] { false, true, true, false, false },
                        StartTime = DateTime.Today.AddHours(14),
                        EndTime = DateTime.Today.AddHours(15)
                    }
                };

                var result = await page.OnPostAsync();
                Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));
            }

            var updated = await ReadCourseAsync(options, 503);
            Assert.AreEqual("After", updated.CourseTitle);
            Assert.AreEqual(5, updated.CreditHours);
            Assert.AreEqual(55, updated.Capacity);
            Assert.AreEqual("B", updated.Location);
            Assert.IsTrue(updated.MeetDays[1] && updated.MeetDays[2]);
        }
    }
}