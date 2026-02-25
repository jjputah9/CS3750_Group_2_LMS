using LMS.Data;
using LMS.Models;
using LMS.Pages.Assignments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LMS_Test
{
    [TestClass]
    public class SubmitAssignmentUnitTest
    {
        [TestMethod]
        public async Task CanSubmitAssignment_FileUpload_ValidStudent()
        {
            // Arrange - Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_SubmitFile")
                .Options;

            using var context = new ApplicationDbContext(options);

            // Create test data
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

            var submissionType = new SubmissionType
            {
                SubmissionTypeId = 1,
                TypeName = "File Upload"
            };
            context.Set<SubmissionType>().Add(submissionType);

            var assignment = new Assignment
            {
                AssignmentId = 1,
                Title = "Test Assignment",
                Description = "Submit a file",
                Points = 100,
                DueDate = DateTime.Today.AddDays(7),
                CourseId = course.Id,
                SubmissionTypeId = submissionType.SubmissionTypeId
            };
            context.Assignment.Add(assignment);
            await context.SaveChangesAsync();

            // Mock student user
            var fakeStudent = new ApplicationUser
            {
                Id = "student-123",
                Email = "student@test.com",
                UserType = "Student"
            };

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );
            mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(fakeStudent);

            // Mock IWebHostEnvironment
            var mockWebHostEnv = new Mock<IWebHostEnvironment>();
            var testPath = Path.Combine(Path.GetTempPath(), "wwwroot");
            mockWebHostEnv.Setup(m => m.WebRootPath).Returns(testPath);

            // Create page model
            var pageModel = new AssignmentSubmitModel(context, mockUserManager.Object, mockWebHostEnv.Object);

            // Setup PageContext and TempData
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            var tempData = new TempDataDictionary(pageModel.PageContext.HttpContext, Mock.Of<ITempDataProvider>());
            pageModel.TempData = tempData;

            // Create mock file
            var content = "Test file content";
            var fileName = "testfile.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream target, CancellationToken token) =>
                {
                    stream.Position = 0;
                    return stream.CopyToAsync(target, token);
                });

            pageModel.SubmittedFile = mockFile.Object;

            // Act - Submit the assignment
            var result = await pageModel.OnPostAsync(assignment.AssignmentId);

            // Assert - Check redirect
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));
            var redirectResult = result as RedirectToPageResult;
            Assert.AreEqual("/Assignments/StudentAssignments", redirectResult.PageName);

            // Verify submission was saved to database
            var submission = await context.submittedAssignments
                .FirstOrDefaultAsync(s => s.AssignmentId == assignment.AssignmentId && s.StudentId == fakeStudent.Id);

            Assert.IsNotNull(submission);
            Assert.AreEqual(assignment.AssignmentId, submission.AssignmentId);
            Assert.AreEqual(fakeStudent.Id, submission.StudentId);
            Assert.AreEqual(submissionType.SubmissionTypeId, submission.submissionTypeId);
            Assert.IsTrue(submission.filePath.Contains($"/submissions/{assignment.AssignmentId}/"));
            Assert.AreEqual(0, submission.grade);

            // Cleanup
            Directory.Delete(testPath, true);
        }

        [TestMethod]
        public async Task CanSubmitAssignment_TextEntry_ValidStudent()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_SubmitText")
                .Options;

            using var context = new ApplicationDbContext(options);

            // Create test data
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

            var submissionType = new SubmissionType
            {
                SubmissionTypeId = 2,
                TypeName = "Text Entry"
            };
            context.Set<SubmissionType>().Add(submissionType);

            var assignment = new Assignment
            {
                AssignmentId = 2,
                Title = "Essay Assignment",
                Description = "Write an essay",
                Points = 100,
                DueDate = DateTime.Today.AddDays(7),
                CourseId = course.Id,
                SubmissionTypeId = submissionType.SubmissionTypeId
            };
            context.Assignment.Add(assignment);
            await context.SaveChangesAsync();

            // Mock student
            var fakeStudent = new ApplicationUser
            {
                Id = "student-456",
                Email = "student2@test.com",
                UserType = "Student"
            };

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );
            mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(fakeStudent);

            // Mock environment
            var mockWebHostEnv = new Mock<IWebHostEnvironment>();
            var testPath = Path.Combine(Path.GetTempPath(), "wwwroot2");
            mockWebHostEnv.Setup(m => m.WebRootPath).Returns(testPath);

            // Create page model
            var pageModel = new AssignmentSubmitModel(context, mockUserManager.Object, mockWebHostEnv.Object);

            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            var tempData = new TempDataDictionary(pageModel.PageContext.HttpContext, Mock.Of<ITempDataProvider>());
            pageModel.TempData = tempData;

            // Set text submission
            pageModel.TextSubmission = "This is my essay submission. It contains important information.";

            // Act
            var result = await pageModel.OnPostAsync(assignment.AssignmentId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            // Verify submission in database
            var submission = await context.submittedAssignments
                .FirstOrDefaultAsync(s => s.AssignmentId == assignment.AssignmentId && s.StudentId == fakeStudent.Id);

            Assert.IsNotNull(submission);
            Assert.AreEqual(assignment.AssignmentId, submission.AssignmentId);
            Assert.AreEqual(fakeStudent.Id, submission.StudentId);
            Assert.AreEqual("This is my essay submission. It contains important information.", submission.textSubmission);
            Assert.AreEqual(0, submission.grade);

            // Cleanup
            if (Directory.Exists(testPath))
                Directory.Delete(testPath, true);
        }

        [TestMethod]
        public async Task CannotSubmitAssignment_DuplicateSubmission()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_DuplicateSubmit")
                .Options;

            using var context = new ApplicationDbContext(options);

            var course = new Course
            {
                Id = 3,
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

            var submissionType = new SubmissionType
            {
                SubmissionTypeId = 3,
                TypeName = "File Upload"
            };
            context.Set<SubmissionType>().Add(submissionType);

            var assignment = new Assignment
            {
                AssignmentId = 3,
                Title = "Test Assignment",
                Description = "Submit a file",
                Points = 100,
                DueDate = DateTime.Today.AddDays(7),
                CourseId = course.Id,
                SubmissionTypeId = submissionType.SubmissionTypeId
            };
            context.Assignment.Add(assignment);

            // Add existing submission
            var existingSubmission = new submittedAssignment
            {
                submittedAssignmentId = 1,
                AssignmentId = assignment.AssignmentId,
                StudentId = "student-789",
                submissionTypeId = submissionType.SubmissionTypeId,
                filePath = "/submissions/3/student-789_3_20260225.txt",
                submissionDate = DateTime.Now,
                textSubmission = "",
                grade = 0
            };
            context.submittedAssignments.Add(existingSubmission);
            await context.SaveChangesAsync();

            // Mock student (same student trying to submit again)
            var fakeStudent = new ApplicationUser
            {
                Id = "student-789",
                Email = "student3@test.com",
                UserType = "Student"
            };

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );
            mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(fakeStudent);

            var mockWebHostEnv = new Mock<IWebHostEnvironment>();
            mockWebHostEnv.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

            var pageModel = new AssignmentSubmitModel(context, mockUserManager.Object, mockWebHostEnv.Object);
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            var tempData = new TempDataDictionary(pageModel.PageContext.HttpContext, Mock.Of<ITempDataProvider>());
            pageModel.TempData = tempData;

            // Mock file
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.Length).Returns(100);
            pageModel.SubmittedFile = mockFile.Object;

            // Act - Try to submit again
            var result = await pageModel.OnPostAsync(assignment.AssignmentId);

            // Assert - Should redirect with error message
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));
            Assert.IsTrue(tempData.ContainsKey("ErrorMessage"));
            Assert.AreEqual("You have already submitted this assignment.", tempData["ErrorMessage"]);

            // Verify only one submission exists
            var submissionCount = await context.submittedAssignments
                .CountAsync(s => s.AssignmentId == assignment.AssignmentId && s.StudentId == fakeStudent.Id);
            Assert.AreEqual(1, submissionCount);
        }

        [TestMethod]
        public async Task CannotSubmitAssignment_NonStudent()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_NonStudentSubmit")
                .Options;

            using var context = new ApplicationDbContext(options);

            var assignment = new Assignment
            {
                AssignmentId = 4,
                Title = "Test",
                Points = 100,
                DueDate = DateTime.Today,
                CourseId = 1,
                SubmissionTypeId = 1
            };
            context.Assignment.Add(assignment);
            await context.SaveChangesAsync();

            // Mock instructor trying to submit
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

            var mockWebHostEnv = new Mock<IWebHostEnvironment>();
            mockWebHostEnv.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

            var pageModel = new AssignmentSubmitModel(context, mockUserManager.Object, mockWebHostEnv.Object);
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act - Try to submit as instructor
            var result = await pageModel.OnPostAsync(assignment.AssignmentId);

            // Assert - Should return ForbidResult
            Assert.IsInstanceOfType(result, typeof(ForbidResult));
        }

        [TestMethod]
        public async Task CannotSubmitAssignment_MissingFile()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_MissingFile")
                .Options;

            using var context = new ApplicationDbContext(options);

            var course = new Course
            {
                Id = 5,
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

            var submissionType = new SubmissionType
            {
                SubmissionTypeId = 5,
                TypeName = "File Upload"
            };
            context.Set<SubmissionType>().Add(submissionType);

            var assignment = new Assignment
            {
                AssignmentId = 5,
                Title = "File Required",
                Points = 100,
                DueDate = DateTime.Today.AddDays(7),
                CourseId = course.Id,
                SubmissionTypeId = submissionType.SubmissionTypeId
            };
            context.Assignment.Add(assignment);
            await context.SaveChangesAsync();

            var fakeStudent = new ApplicationUser
            {
                Id = "student-999",
                Email = "student@test.com",
                UserType = "Student"
            };

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );
            mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(fakeStudent);

            var mockWebHostEnv = new Mock<IWebHostEnvironment>();
            mockWebHostEnv.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

            var pageModel = new AssignmentSubmitModel(context, mockUserManager.Object, mockWebHostEnv.Object);
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };
            var tempData = new TempDataDictionary(pageModel.PageContext.HttpContext, Mock.Of<ITempDataProvider>());
            pageModel.TempData = tempData;

            // No file provided (SubmittedFile is null)
            pageModel.SubmittedFile = null;

            // Act
            var result = await pageModel.OnPostAsync(assignment.AssignmentId);

            // Assert - Should return Page with error
            Assert.IsInstanceOfType(result, typeof(PageResult));
            Assert.IsFalse(pageModel.ModelState.IsValid);
            Assert.IsTrue(pageModel.ModelState.ContainsKey("SubmittedFile"));
        }
    }
}
