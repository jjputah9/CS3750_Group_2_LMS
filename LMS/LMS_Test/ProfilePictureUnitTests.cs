using LMS.Data;
using LMS.Models;
using LMS.Pages.Profile;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Tests
{
    [TestClass]
    public class ProfilePictureUnitTests
    {
        private ApplicationDbContext GetTestContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private ClaimsPrincipal FakeUser(string userId = "user1")
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "TestAuth"));
        }

        private IFormFile CreateFakeImage()
        {
            var content = "fake image data";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            return new FormFile(stream, 0, stream.Length, "ProfilePicture", "test.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };
        }

        private UserProfile CreateProfile(string userId)
        {
            return new UserProfile
            {
                UserId = userId,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Today,
                Phone = "1234567890",
                Description = "Test description"
            };
        }

        private EditModel CreatePageModel(ApplicationDbContext context, ClaimsPrincipal user)
        {
            var logger = new FakeLogger();
            var env = new FakeEnvironment();

            var pageModel = new EditModel(context, logger, env);

            pageModel.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            return pageModel;
        }

        [TestMethod]
        public async Task User_Can_Upload_Profile_Picture()
        {
            var context = GetTestContext();

            var profile = CreateProfile("user1");
            context.Add(profile);
            await context.SaveChangesAsync();

            var pageModel = CreatePageModel(context, FakeUser("user1"));
            pageModel.ProfilePicture = CreateFakeImage();

            await pageModel.OnPostAsync();

            var updated = await context.Set<UserProfile>().FirstAsync();

            Assert.IsNotNull(updated.ProfilePictureData);
            Assert.AreEqual("image/jpeg", updated.ProfilePictureContentType);
        }

        [TestMethod]
        public async Task User_Can_Remove_Profile_Picture()
        {
            var context = GetTestContext();

            var profile = CreateProfile("user1");
            profile.ProfilePictureData = Encoding.UTF8.GetBytes("existing");

            context.Add(profile);
            await context.SaveChangesAsync();

            var pageModel = CreatePageModel(context, FakeUser("user1"));
            pageModel.RemovePhoto = true;

            await pageModel.OnPostAsync();

            var updated = await context.Set<UserProfile>().FirstAsync();

            Assert.IsNull(updated.ProfilePictureData);
        }

        [TestMethod]
        public async Task Cannot_Update_Profile_If_Not_Logged_In()
        {
            var context = GetTestContext();

            var profile = CreateProfile("user1");
            context.Add(profile);
            await context.SaveChangesAsync();

            var pageModel = CreatePageModel(context, new ClaimsPrincipal());
            pageModel.ProfilePicture = CreateFakeImage();

            await pageModel.OnPostAsync();

            var updated = await context.Set<UserProfile>().FirstAsync();

            Assert.IsNull(updated.ProfilePictureData);
        }

        [TestMethod]
        public async Task Different_Users_Cannot_Edit_Others_Profile()
        {
            var context = GetTestContext();

            var profile = CreateProfile("ownerUser");
            context.Add(profile);
            await context.SaveChangesAsync();

            var pageModel = CreatePageModel(context, FakeUser("anotherUser"));
            pageModel.ProfilePicture = CreateFakeImage();

            await pageModel.OnPostAsync();

            var updated = await context.Set<UserProfile>().FirstAsync();

            Assert.IsNull(updated.ProfilePictureData);
        }

        [TestMethod]
        public async Task Upload_Replaces_Existing_Picture()
        {
            var context = GetTestContext();

            var profile = CreateProfile("user1");
            profile.ProfilePictureData = Encoding.UTF8.GetBytes("old");

            context.Add(profile);
            await context.SaveChangesAsync();

            var pageModel = CreatePageModel(context, FakeUser("user1"));
            pageModel.ProfilePicture = CreateFakeImage();

            await pageModel.OnPostAsync();

            var updated = await context.Set<UserProfile>().FirstAsync();

            Assert.AreNotEqual("old", Encoding.UTF8.GetString(updated.ProfilePictureData));
        }

        [TestMethod]
        public async Task No_File_Upload_Does_Not_Change_Profile()
        {
            var context = GetTestContext();

            var profile = CreateProfile("user1");
            context.Add(profile);
            await context.SaveChangesAsync();

            var pageModel = CreatePageModel(context, FakeUser("user1"));

            await pageModel.OnPostAsync();

            var updated = await context.Set<UserProfile>().FirstAsync();

            Assert.IsNull(updated.ProfilePictureData);
        }

        [TestMethod]
        public async Task Multiple_Users_Only_Update_Their_Own_Profile()
        {
            var context = GetTestContext();

            var profile1 = CreateProfile("user1");
            var profile2 = CreateProfile("user2");

            context.AddRange(profile1, profile2);
            await context.SaveChangesAsync();

            var pageModel = CreatePageModel(context, FakeUser("user1"));
            pageModel.ProfilePicture = CreateFakeImage();

            await pageModel.OnPostAsync();

            var updated1 = await context.Set<UserProfile>().FirstAsync(p => p.UserId == "user1");
            var updated2 = await context.Set<UserProfile>().FirstAsync(p => p.UserId == "user2");

            Assert.IsNotNull(updated1.ProfilePictureData);
            Assert.IsNull(updated2.ProfilePictureData);
        }

        [TestMethod]
        public async Task Instructor_Profile_Can_Update_Picture()
        {
            var context = GetTestContext();

            var profile = CreateProfile("inst1");
            profile.Description = "Instructor";

            context.Add(profile);
            await context.SaveChangesAsync();

            var pageModel = CreatePageModel(context, FakeUser("inst1"));
            pageModel.ProfilePicture = CreateFakeImage();

            await pageModel.OnPostAsync();

            var updated = await context.Set<UserProfile>().FirstAsync();

            Assert.IsNotNull(updated.ProfilePictureData);
        }
    }

    public class FakeLogger : ILogger<EditModel>
    {
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        { }
    }

    public class FakeEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}