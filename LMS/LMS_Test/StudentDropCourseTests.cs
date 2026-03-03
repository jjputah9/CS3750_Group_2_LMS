using System;
using System.Linq;
using System.Threading.Tasks;
using LMS.Data;
using LMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LMS_Test
{
    [TestClass]
    public class StudentDropCourseTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private static async Task SeedStudentCourseAndRegistration(
            ApplicationDbContext context,
            string studentId,
            int courseId)
        {
            context.Users.Add(new ApplicationUser
            {
                Id = studentId,
                Email = $"{studentId}@test.com",
                UserName = $"{studentId}@test.com",
                fName = "Test",
                lName = "Student",
                DOB = new DateTime(2000, 1, 1),
                UserType = "Student"
            });

            context.Course.Add(new Course
            {
                Id = courseId,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 1400,
                CourseTitle = "Test Course",
                CreditHours = 3,
                Capacity = 30,
                Location = "Room 1",
                MeetDays = new[] { true, false, false, false, false },
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10)
            });

            context.Registration.Add(new Registration
            {
                StudentID = studentId,
                CourseID = courseId
            });

            await context.SaveChangesAsync();
        }

        // Helper that represents the drop behavior (data-layer equivalent).
        // If your app uses a PageModel handler instead, I’ll rewrite these to call it once you re-upload it.
        private static async Task<bool> DropCourseAsync(ApplicationDbContext context, string studentId, int courseId)
        {
            var reg = await context.Registration
                .FirstOrDefaultAsync(r => r.StudentID == studentId && r.CourseID == courseId);

            if (reg == null) return false;

            context.Registration.Remove(reg);
            await context.SaveChangesAsync();
            return true;
        }

        [TestMethod]
        public async Task DropCourse_WhenStudentIsRegistered_RemovesRegistration_ReturnsTrue()
        {
            using var context = CreateContext(nameof(DropCourse_WhenStudentIsRegistered_RemovesRegistration_ReturnsTrue));
            await SeedStudentCourseAndRegistration(context, "student-1", 101);

            var result = await DropCourseAsync(context, "student-1", 101);

            Assert.IsTrue(result);
            Assert.AreEqual(0, await context.Registration.CountAsync());
        }

        [TestMethod]
        public async Task DropCourse_WhenNotRegistered_DoesNotChangeDb_ReturnsFalse()
        {
            using var context = CreateContext(nameof(DropCourse_WhenNotRegistered_DoesNotChangeDb_ReturnsFalse));
            // seed student + course but no registration
            context.Users.Add(new ApplicationUser
            {
                Id = "student-1",
                Email = "student-1@test.com",
                UserName = "student-1@test.com",
                fName = "Test",
                lName = "Student",
                DOB = new DateTime(2000, 1, 1),
                UserType = "Student"
            });
            context.Course.Add(new Course
            {
                Id = 101,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 1400,
                CourseTitle = "Test Course",
                CreditHours = 3,
                Capacity = 30,
                Location = "Room 1",
                MeetDays = new[] { true, false, false, false, false },
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10)
            });
            await context.SaveChangesAsync();

            var result = await DropCourseAsync(context, "student-1", 101);

            Assert.IsFalse(result);
            Assert.AreEqual(0, await context.Registration.CountAsync());
        }

        [TestMethod]
        public async Task DropCourse_Twice_SecondAttemptReturnsFalse_AndDbStaysConsistent()
        {
            using var context = CreateContext(nameof(DropCourse_Twice_SecondAttemptReturnsFalse_AndDbStaysConsistent));
            await SeedStudentCourseAndRegistration(context, "student-1", 101);

            var first = await DropCourseAsync(context, "student-1", 101);
            var second = await DropCourseAsync(context, "student-1", 101);

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.AreEqual(0, await context.Registration.CountAsync());
        }

        [TestMethod]
        public async Task DropCourse_WithWrongCourseId_DoesNotAffectOtherRegistrations()
        {
            using var context = CreateContext(nameof(DropCourse_WithWrongCourseId_DoesNotAffectOtherRegistrations));

            // seed two registrations for same student, different courses
            await SeedStudentCourseAndRegistration(context, "student-1", 101);

            context.Course.Add(new Course
            {
                Id = 202,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 1410,
                CourseTitle = "Other Course",
                CreditHours = 3,
                Capacity = 30,
                Location = "Room 2",
                MeetDays = new[] { false, true, false, false, false },
                StartTime = DateTime.Today.AddHours(11),
                EndTime = DateTime.Today.AddHours(12)
            });
            context.Registration.Add(new Registration { StudentID = "student-1", CourseID = 202 });
            await context.SaveChangesAsync();

            var result = await DropCourseAsync(context, "student-1", 999); // wrong courseId

            Assert.IsFalse(result);
            Assert.AreEqual(2, await context.Registration.CountAsync(r => r.StudentID == "student-1"));
        }

        [TestMethod]
        public async Task DropCourse_WrongStudentId_DoesNotRemoveOtherStudentsRegistration()
        {
            using var context = CreateContext(nameof(DropCourse_WrongStudentId_DoesNotRemoveOtherStudentsRegistration));

            await SeedStudentCourseAndRegistration(context, "student-1", 101);
            // another student registered in same course
            context.Users.Add(new ApplicationUser
            {
                Id = "student-2",
                Email = "student-2@test.com",
                UserName = "student-2@test.com",
                fName = "Test",
                lName = "Student2",
                DOB = new DateTime(2000, 1, 1),
                UserType = "Student"
            });
            context.Registration.Add(new Registration { StudentID = "student-2", CourseID = 101 });
            await context.SaveChangesAsync();

            var result = await DropCourseAsync(context, "student-999", 101);

            Assert.IsFalse(result);
            Assert.AreEqual(2, await context.Registration.CountAsync());
        }
    }
}