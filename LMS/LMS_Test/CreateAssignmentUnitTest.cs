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
    public class CreateAssignmentUnitTest
    {
        [TestMethod]
        public async Task CanCreateAssignment_ValidInstructor()
        {
            // Arrange - Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_CreateAssignment")
                .Options;

            using var context = new ApplicationDbContext(options);

            // Create a test course
            var course = new Course
            {
                Id = 1,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 3750,
                CourseTitle = "Software Engineering",
                CreditHours = 3,
                Capacity = 30,
                Location = "Room 201",
                MeetDays = new bool[5] { true, false, true, false, true },
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10)
            };
            context.Course.Add(course);
            
            // Add a submission type
            context.Set<SubmissionType>().Add(new SubmissionType 
            { 
                SubmissionTypeId = 1, 
                TypeName = "Online" 
            });
            
            await context.SaveChangesAsync();

            // Mock UserManager with authorized instructor
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

            // Create the page model
            var pageModel = new CreateModel(context, mockUserManager.Object);

            // Setup PageContext and Session
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            pageModel.PageContext.HttpContext.Session = new TestSession();
            pageModel.PageContext.HttpContext.Session.SetInt32("ActiveCourseId", course.Id);

            // Create a new assignment
            pageModel.Assignment = new Assignment
            {
                Title = "Unit Test Assignment",
                Description = "Test Description",
                Points = 100,
                DueDate = DateTime.Today.AddDays(7),
                CourseId = course.Id,
                SubmissionTypeId = 1
            };

            // Act - Call OnPostAsync
            var result = await pageModel.OnPostAsync(course.Id);

            // Assert - Check redirect
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));
            var redirectResult = result as RedirectToPageResult;
            Assert.AreEqual("/Assignments/Index", redirectResult.PageName);

            // Verify assignment was saved to database
            var savedAssignment = await context.Assignment
                .FirstOrDefaultAsync(a => a.Title == "Unit Test Assignment");
            
            Assert.IsNotNull(savedAssignment);
            Assert.AreEqual("Unit Test Assignment", savedAssignment.Title);
            Assert.AreEqual("Test Description", savedAssignment.Description);
            Assert.AreEqual(100, savedAssignment.Points);
            Assert.AreEqual(course.Id, savedAssignment.CourseId);
        }

        [TestMethod]
        public async Task CannotCreateAssignment_UnauthorizedUser()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_CreateAssignment_Unauthorized")
                .Options;

            using var context = new ApplicationDbContext(options);

            var course = new Course
            {
                Id = 2,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 3750,
                CourseTitle = "Software Engineering",
                CreditHours = 3,
                Capacity = 30,
                Location = "Room 201",
                MeetDays = new bool[5] { true, false, true, false, true },
                StartTime = DateTime.Today.AddHours(9),
                EndTime = DateTime.Today.AddHours(10)
            };
            context.Course.Add(course);
            await context.SaveChangesAsync();

            // Mock student user (not instructor)
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

            var pageModel = new CreateModel(context, mockUserManager.Object);
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            pageModel.PageContext.HttpContext.Session = new TestSession();
            pageModel.PageContext.HttpContext.Session.SetInt32("ActiveCourseId", course.Id);

            // Act
            var result = await pageModel.OnPostAsync(course.Id);

            // Assert - Should return ForbidResult
            Assert.IsInstanceOfType(result, typeof(ForbidResult));
        }

        [TestMethod]
        public async Task CannotCreateAssignment_InvalidCourseId()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_CreateAssignment_InvalidCourse")
                .Options;

            using var context = new ApplicationDbContext(options);

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

            var pageModel = new CreateModel(context, mockUserManager.Object);
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            pageModel.PageContext.HttpContext.Session = new TestSession();
            pageModel.PageContext.HttpContext.Session.SetInt32("ActiveCourseId", 999);

            // Act - Try to create assignment for non-existent course
            var result = await pageModel.OnPostAsync(999);

            // Assert - Should return ForbidResult
            Assert.IsInstanceOfType(result, typeof(ForbidResult));
        }
    }
}
