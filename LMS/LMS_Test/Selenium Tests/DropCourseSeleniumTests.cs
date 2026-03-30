using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;

namespace LMS.Tests.SeleniumTests
{
    [TestClass]
    public class DropCourseSeleniumTests
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

        private void LoginAndGoToRegistration()
        {
            // Go to login page
            driver.Navigate().GoToUrl(baseUrl + "Identity/Account/Login");

            //Set login fields
            wait.Until(d => d.FindElement(By.Id("Input_Email"))).SendKeys("student@test1.com");
            driver.FindElement(By.Id("Input_Password")).SendKeys("NewPassword123!");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // Navigate directly to register page
            wait.Until(d => d.FindElement(By.Id("welcome-title")));
            driver.Navigate().GoToUrl(baseUrl + "Registrations/Create");

            // Wait for page to load
            wait.Until(d => d.FindElements(By.CssSelector(".card")).Count > 0);
        }

        private void AddCourse(string courseName)
        {
            driver.Navigate().GoToUrl(baseUrl + "Registrations/Create");
            // Find the card with the course name
            var courseCard = wait.Until(d => d.FindElements(By.CssSelector(".card"))
                .FirstOrDefault(c => c.FindElement(By.CssSelector(".card-title")).Text.Contains(courseName)));
            Assert.IsNotNull(courseCard, $"Course card with name containing '{courseName}' not found.");
            // Click the "Add Course" button within that card
            var addButton = courseCard.FindElement(By.CssSelector(".btn"));

            new Actions(driver)
                .ScrollToElement(addButton)
                .Perform();

            addButton.Click();
            // Wait for "Drop Course" button to confirm action finished
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

        [TestMethod]
        public void Drop_Course_Student_Is_Registered_For()
        {
            try
            {
                LoginAndGoToRegistration();

                // Find the card with "Best Course" title
                var courseCard = wait.Until(d => d.FindElements(By.CssSelector(".card"))
                    .FirstOrDefault(c => c.FindElement(By.CssSelector(".card-title")).Text.Contains("Best Course")));

                Assert.IsNotNull(courseCard, "Best Course not found on registration page!");

                // Find "Add Course" button inside that card and click
                var addButton = courseCard.FindElement(By.CssSelector(".btn"));

                // Scroll to find button
                new Actions(driver)
                    .ScrollToElement(addButton)
                    .Perform();

                addButton.Click();

                // Wait for "Drop Course" button to confirm registration
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

                // Go to dashboard to confirm course is listed
                driver.Navigate().GoToUrl(baseUrl + "Dashboard");
                wait.Until(d => d.FindElement(By.TagName("body"))); // wait page load

                // Grab all course titles from dashboard
                var courses = driver.FindElements(By.CssSelector(".card-title"));
                bool courseExists = courses.Any(c => c.Text.Contains("Best Course")); 

                Assert.IsFalse(courseExists, "Course found on dashboard! Drop failed!");
            }
            catch (WebDriverTimeoutException ex)
            {
                Assert.Fail("Test failed due to timeout: " + ex.Message);
            }
            catch (Exception ex)
            {
                Assert.Fail("Test failed with exception: " + ex.Message);
            }
            finally
            {
                // reset test by adding course again
                AddCourse("Best Course");
            }
        }
    }
}
