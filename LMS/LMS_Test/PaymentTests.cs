using LMS.Data;
using LMS.Models;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LMS.Pages.StudentAccount;
using Microsoft.Extensions.Logging;


namespace LMS_Test;

[TestClass]
public class PaymentTests
{
    [TestMethod]
    public async Task SavePayment_AddsPaymentToDatabase()
    {
        // create in memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDb_SavePayment")
            .Options;

        using var context = new ApplicationDbContext(options);

        // mock user manager
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null, null, null, null, null, null, null, null);

        var mockConfig = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<IndexModel>>();

        // create the pageModel
        var pageModel = new IndexModel(
            mockUserManager.Object,
            context,
            mockConfig.Object, 
            mockLogger.Object
        );

        await pageModel.SavePaymentRecord(
            "testuser",
            300,
            "partial_tuition",
            "fake_session_id"
        );

        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.StudentId == "testuser");

        Assert.IsNotNull(payment);
        Assert.AreEqual(300, payment.Amount);
        Assert.AreEqual("Completed", payment.Status);
    }
}
