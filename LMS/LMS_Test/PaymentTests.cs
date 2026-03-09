using LMS.Data;
using LMS.Models;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LMS.Pages.StudentAccount; // make sure IndexModel is public
using System.Threading.Tasks;
using System.Linq;

namespace LMS_Test
{
    [TestClass]
    public class PaymentTests
    {
        // Helper to create a fresh in-memory database
        private ApplicationDbContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_SavePayment")
                .Options;

            return new ApplicationDbContext(options);
        }

        // Helper to mock UserManager
        private UserManager<ApplicationUser> GetMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new UserManager<ApplicationUser>(
                store.Object,
                null, null, null, null, null, null, null, null
            );
        }

        [TestMethod]
        public async Task SavePayment_AddsPaymentToDatabase()
        {
            // Arrange
            using var context = GetInMemoryDb();
            var mockUserManager = GetMockUserManager();
            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<IndexModel>>();

            // Ensure IndexModel constructor matches these parameters
            var pageModel = new IndexModel(
                mockUserManager,
                context,
                mockConfig.Object,
                mockLogger.Object
            );

            // Act
            await pageModel.SavePaymentRecord(
                "testuser",
                300m, // decimal
                "partial_tuition",
                "fake_session_id"
            );

            // Assert
            var payment = await context.Payments
                .FirstOrDefaultAsync(p => p.StudentId == "testuser");

            Assert.IsNotNull(payment, "Payment was not saved in the database.");
            Assert.AreEqual(300m, payment.Amount, "Payment amount mismatch.");
            Assert.AreEqual("Completed", payment.Status, "Payment status mismatch.");
        }
    }
}