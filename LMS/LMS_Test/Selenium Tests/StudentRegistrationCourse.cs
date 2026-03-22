using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace LMS_Test.Selenium_Tests
{
    [TestClass]  // MSTest requires this
    public class StudentRegistrationCourse
    {
        private IWebDriver driver;
        private WebDriverWait wait;
        private string baseUrl = "https://localhost:5181/";

        [TestInitialize]  // Runs before each test
        public void Setup()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddArgument("--ignore-certificate-errors");

            driver = new ChromeDriver(options);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        [TestCleanup]  // Runs after each test
        public void Teardown()
        {
            try
            {
                driver.Quit();
            }
            catch { }
        }

        /// <summary>
        /// Logs in and navigates to the course registration page
        /// </summary>
        public void LoginAndGoToRegistration()
        {
            driver.Navigate().GoToUrl(baseUrl + "Identity/Account/Login");

            var emailInput = wait.Until(d => d.FindElement(By.Id("Input_Email")));
            emailInput.SendKeys("raff2@gmail.com");

            driver.FindElement(By.Id("Input_Password")).SendKeys("P@ss0rd");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            wait.Until(d => d.Url.Contains("Home") || d.Url.Contains("Dashboard"));

            driver.Navigate().GoToUrl(baseUrl + "Registrations/Create");
            wait.Until(d => d.FindElements(By.CssSelector(".card")).Count > 0);
        }

        [TestMethod]  // MSTest requires this
        public void StudentCanAddCourse()
        {
            try
            {
                LoginAndGoToRegistration();

                // Find first "Add Course" button
                var addButton = wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Add Course')]")));
                addButton.Click();

                // Wait for "Drop Course" button to confirm registration
                var dropButton = wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Drop Course')]")));

                Assert.IsNotNull(dropButton, "Course was not added successfully.");
            }
            catch (WebDriverTimeoutException ex)
            {
                Assert.Fail("Test failed due to timeout: " + ex.Message);
            }
            catch (Exception ex)
            {
                Assert.Fail("Test failed with exception: " + ex.Message);
            }
        }
    }
}