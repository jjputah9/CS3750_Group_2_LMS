using LMS.Data;
using LMS.Models;
using LMS.Pages.Assignments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace LMS_Test
{
    [TestClass]
    public class AssignmentTests
    {
        [TestMethod]
        public async Task CanEditAssignment_InMemory()
        {
            // Setup in-memory DB
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_EditAssignment")
                .Options;

            using var context = new ApplicationDbContext(options);

            // Add a course
            var course = new Course
            {
                Id = 1,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 101,
                CourseTitle = "Test Course",
                CreditHours = 3,
                Capacity = 20,
                Location = "Room 101",
                MeetDays = new bool[5] { true, true, true, true, true },
                StartTime = DateTime.Today.AddHours(8),
                EndTime = DateTime.Today.AddHours(9)
            };
            context.Course.Add(course);

            // Add an assignment
            var assignment = new Assignment
            {
                AssignmentId = 1,
                CourseId = course.Id,
                Title = "Original Title",
                Description = "Original Description",
                Points = 10,
                DueDate = DateTime.Today,
                SubmissionTypeId = 1
            };
            context.Assignment.Add(assignment);
            await context.SaveChangesAsync();

            // Mock UserManager and authorized instructor
            var fakeInstructor = new ApplicationUser
            {
                Id = "instructor-1",
                Email = "instructor@test.com",
                UserType = "Instructor"
            };

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );
            mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(fakeInstructor);

            // Create EditModel
            var pageModel = new EditModel(context, mockUserManager.Object)
            {
                Assignment = assignment
            };

            // Setup PageContext & Session
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            pageModel.PageContext.HttpContext.Session = new TestSession();
            pageModel.PageContext.HttpContext.Session.SetInt32("ActiveCourseId", course.Id);

            // Simulate editing
            pageModel.Assignment.Title = "Updated Title";
            pageModel.Assignment.Description = "Updated Description";
            pageModel.Assignment.Points = 20;
            pageModel.Assignment.DueDate = DateTime.Today.AddDays(7);

            // Call OnPostAsync
            var result = await pageModel.OnPostAsync();

            // Check redirect result
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            // Verify changes in database
            var updated = await context.Assignment.FindAsync(assignment.AssignmentId);
            Assert.AreEqual("Updated Title", updated.Title);
            Assert.AreEqual("Updated Description", updated.Description);
            Assert.AreEqual(20, updated.Points);
            Assert.AreEqual(DateTime.Today.AddDays(7), updated.DueDate);
        }

        [TestMethod]
        public async Task CannotEditAssignment_UnauthorizedUser()
        {
            // Setup in-memory DB
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_UnauthorizedEdit")
                .Options;

            using var context = new ApplicationDbContext(options);

            // Add a course
            var course = new Course
            {
                Id = 2,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 102,
                CourseTitle = "Another Course",
                CreditHours = 3,
                Capacity = 20,
                Location = "Room 102",
                MeetDays = new bool[5] { true, true, true, true, true },
                StartTime = DateTime.Today.AddHours(10),
                EndTime = DateTime.Today.AddHours(11)
            };
            context.Course.Add(course);

            // Add an assignment
            var assignment = new Assignment
            {
                AssignmentId = 2,
                CourseId = course.Id,
                Title = "Original Title",
                Description = "Original Description",
                Points = 10,
                DueDate = DateTime.Today,
                SubmissionTypeId = 1
            };
            context.Assignment.Add(assignment);
            await context.SaveChangesAsync();

            // Mock UserManager and unauthorized user (Student)
            var fakeStudent = new ApplicationUser
            {
                Id = "student-1",
                Email = "student@test.com",
                UserType = "Student"
            };

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );
            mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(fakeStudent);

            // Create EditModel
            var pageModel = new EditModel(context, mockUserManager.Object)
            {
                Assignment = new Assignment
                {
                    AssignmentId = assignment.AssignmentId,
                    Title = "Hacked Title",
                    Description = "Hacked Description",
                    Points = 100,
                    DueDate = DateTime.Today.AddDays(10),
                    SubmissionTypeId = 1,
                    CourseId = course.Id
                }
            };

            // Setup PageContext & Session
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            pageModel.PageContext.HttpContext.Session = new TestSession();
            pageModel.PageContext.HttpContext.Session.SetInt32("ActiveCourseId", course.Id);

            // Call OnPostAsync
            var result = await pageModel.OnPostAsync();

            // Check that unauthorized user is forbidden
            Assert.IsInstanceOfType(result, typeof(ForbidResult));

            // Verify assignment was NOT changed in DB
            var unchanged = await context.Assignment.FindAsync(assignment.AssignmentId);
            Assert.AreEqual("Original Title", unchanged.Title);
            Assert.AreEqual("Original Description", unchanged.Description);
            Assert.AreEqual(10, unchanged.Points);
            Assert.AreEqual(DateTime.Today, unchanged.DueDate);
        }
    }

    // Simple session mock
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _storage = new();
        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _storage.Keys;
        public void Clear() => _storage.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _storage.Remove(key);
        public void Set(string key, byte[] value) => _storage[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _storage.TryGetValue(key, out value);
    }
}