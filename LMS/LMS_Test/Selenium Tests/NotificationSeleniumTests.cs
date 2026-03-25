using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace LMS.Tests.SeleniumTests
{
    [TestClass]
    public class NotificationSeleniumTests
    {
        private IWebDriver driver;
        private WebDriverWait wait;
        private string baseUrl = "https://localhost:7270/"; // Your running project URL

        [TestInitialize]
        public void Setup()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddArgument("--ignore-certificate-errors"); // For localhost HTTPS

            driver = new ChromeDriver(options);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        [TestCleanup]
        public void Teardown()
        {
            driver.Quit();
        }

        private void LoginAsStudent()
        {
            driver.Navigate().GoToUrl(baseUrl + "Identity/Account/Login");

            wait.Until(d => d.FindElement(By.Id("Input_Email"))).SendKeys("student@test1.com");
            driver.FindElement(By.Id("Input_Password")).SendKeys("NewPassword123!");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            wait.Until(d => d.FindElement(By.Id("notificationBtn")));
        }

        [TestMethod]
        public void NotificationBell_Shows_No_More_Than_Five_Notifications_UI()
        {
            LoginAsStudent();

            driver.FindElement(By.Id("notificationBtn")).Click();

            wait.Until(d =>
            {
                var popovers = d.FindElements(By.ClassName("popover"));
                return popovers.Count > 0 && popovers[0].Displayed;
            });

            var notificationRows = driver.FindElements(By.CssSelector(".popover .notification-row"));

            Assert.IsTrue(notificationRows.Count <= 5,
                $"Expected 5 or fewer notifications, but found {notificationRows.Count}.");
        }

        [TestMethod]
        public void NotificationBell_Delete_Removes_Visible_Notification_UI()
        {
            LoginAsStudent();

            driver.FindElement(By.Id("notificationBtn")).Click();

            wait.Until(d =>
            {
                var popovers = d.FindElements(By.ClassName("popover"));
                return popovers.Count > 0 && popovers[0].Displayed;
            });

            var initialRows = driver.FindElements(By.CssSelector(".popover .notification-row")).Count;

            if (initialRows == 0)
            {
                Assert.Inconclusive("No notifications were available for this student.");
            }

            var deleteButtons = driver.FindElements(By.CssSelector(".popover .delete-notification"));
            Assert.IsTrue(deleteButtons.Count > 0, "Expected at least one delete button.");

            deleteButtons[0].Click();

            wait.Until(d =>
                d.FindElements(By.CssSelector(".popover .notification-row")).Count == initialRows - 1
            );

            var finalRows = driver.FindElements(By.CssSelector(".popover .notification-row")).Count;

            Assert.AreEqual(initialRows - 1, finalRows,
                "Expected one visible notification to be removed after clicking delete.");
        }
    }
}