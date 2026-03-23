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
            // Go to login page
            driver.Navigate().GoToUrl(baseUrl + "Identity/Account/Login");

            //Set login fields
            wait.Until(d => d.FindElement(By.Id("Input_Email"))).SendKeys("register@drop.com");
            driver.FindElement(By.Id("Input_Password")).SendKeys("NewPassword123!");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // Navigate directly to register page
            driver.Navigate().GoToUrl(baseUrl + "Registrations/Create");

            // Wait for page to load
            wait.Until(d => d.FindElements(By.CssSelector(".card")).Count > 0);
        }

        public void DropCourse(string courseName)
        {
            driver.Navigate().GoToUrl(baseUrl + "Registrations/Create");
            // Find the card with the course name
            var courseCard = wait.Until(d => d.FindElements(By.CssSelector(".card"))
                .FirstOrDefault(c => c.FindElement(By.CssSelector(".card-title")).Text.Contains(courseName)));
            Assert.IsNotNull(courseCard, $"Course card with name containing '{courseName}' not found.");
            // Click the "Drop Course" button within that card
            var dropButton = courseCard.FindElement(By.XPath(".//button[contains(text(),'Drop Course')]"));
            dropButton.Click();
            // Wait for "Add Course" button to confirm action finished
            wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Add Course')]")));
        }

        [TestMethod]  // MSTest requires this
        public void StudentCanAddCourse()
        {
            // always trys to add Test Course
            try
            {
                LoginAndGoToRegistration();

                // Find the card with "Test Course" title
                var courseCard = wait.Until(d => d.FindElements(By.CssSelector(".card"))
                    .FirstOrDefault(c => c.FindElement(By.CssSelector(".card-title")).Text.Contains("Test Course")));

                Assert.IsNotNull(courseCard, "Test Course not found on registration page!");

                // Find "Add Course" button inside that card and click
                var addButton = courseCard.FindElement(By.XPath(".//button[contains(text(),'Add Course')]"));
                addButton.Click();

                // Wait for "Drop Course" button to confirm registration
                wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Drop Course')]")));

                // Go to dashboard to confirm course is listed
                driver.Navigate().GoToUrl(baseUrl + "Dashboard");
                wait.Until(d => d.FindElement(By.TagName("body"))); // wait page load

                // Grab all course titles from dashboard
                var courses = driver.FindElements(By.XPath("//h5[@class='card-title']"));
                bool courseAdded = courses.Any(c => c.Text.Contains("Test Course")); 

                Assert.IsTrue(courseAdded, "Course not found on dashboard! Add failed!");

                // rest test by dropping course again
                DropCourse("Test Course");
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