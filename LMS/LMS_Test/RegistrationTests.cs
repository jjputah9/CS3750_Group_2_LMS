using LMS.Data;
using LMS.Models;
using LMS.Pages.Registrations;
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
    public class RegistrationTests
    {
        /// <summary>
        /// Test registering a student for a course
        /// </summary>
        [TestMethod]
        public async Task CanRegisterForCourse_InMemory()
        {
            //Setup in-memory DB
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_RegisterCourse")
                .Options;

            using var context = new ApplicationDbContext(options);

            //Add a sample course
            var course = new Course
            {
                Id = 1,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 101,
                CourseTitle = "Intro to Unit Testing",
                CreditHours = 3,
                Capacity = 20,
                Location = "Room 101",
                MeetDays = new bool[5] { true, true, true, true, true },
                StartTime = DateTime.Today.AddHours(8),
                EndTime = DateTime.Today.AddHours(9)
            };
            context.Course.Add(course);
            await context.SaveChangesAsync();

            //Mock UserManager for student
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

            //Create PageModel
            var pageModel = new CreateModel(mockUserManager.Object, context)
            {
                Registration = new Registration
                {
                    StudentID = fakeStudent.Id,
                    CourseID = course.Id
                }
            };

            //Setup HttpContext (optional, required if PageModel uses it)
            pageModel.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
            };

            //Call OnPostAsync() to register
            var result = await pageModel.OnPostAsync();

            //Assert redirect to same page
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            //Verify registration exists in DB
            var reg = await context.Registration.FirstOrDefaultAsync(r => r.StudentID == fakeStudent.Id && r.CourseID == course.Id);
            Assert.IsNotNull(reg, "Registration should be added to the database");
            Assert.AreEqual(fakeStudent.Id, reg.StudentID);
            Assert.AreEqual(course.Id, reg.CourseID);
        }

        /// <summary>
        /// Test dropping a course (student already registered)
        /// </summary>
        [TestMethod]
        public async Task CanDropCourse_InMemory()
        {
            //Setup in-memory DB
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb_DropCourse")
                .Options;

            using var context = new ApplicationDbContext(options);

            //Add a sample course
            var course = new Course
            {
                Id = 2,
                InstructorEmail = "instructor@test.com",
                DeptName = "CS",
                CourseNum = 102,
                CourseTitle = "Advanced Testing",
                CreditHours = 3,
                Capacity = 20,
                Location = "Room 102",
                MeetDays = new bool[5] { true, true, true, true, true },
                StartTime = DateTime.Today.AddHours(10),
                EndTime = DateTime.Today.AddHours(11)
            };
            context.Course.Add(course);

            //Add a registration for student
            var regToDrop = new Registration
            {
                StudentID = "student-2",
                CourseID = course.Id,
                RegistrationDateTime = DateTime.Now
            };
            context.Registration.Add(regToDrop);
            await context.SaveChangesAsync();

            //Mock UserManager
            var fakeStudent = new ApplicationUser
            {
                Id = "student-2",
                Email = "student2@test.com",
                UserType = "Student"
            };
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );
            mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(fakeStudent);

            //Create PageModel
            var pageModel = new CreateModel(mockUserManager.Object, context)
            {
                Registration = new Registration
                {
                    StudentID = fakeStudent.Id,
                    CourseID = course.Id
                }
            };

            //Call OnPostAsync() to drop the course
            var result = await pageModel.OnPostAsync();

            //Assert redirect to same page
            Assert.IsInstanceOfType(result, typeof(RedirectToPageResult));

            //Verify registration removed from DB
            var reg = await context.Registration.FirstOrDefaultAsync(r => r.StudentID == fakeStudent.Id && r.CourseID == course.Id);
            Assert.IsNull(reg, "Registration should be removed from the database");
        }
    }
}