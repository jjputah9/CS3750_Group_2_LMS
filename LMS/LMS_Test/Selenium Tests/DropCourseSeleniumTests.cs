using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace LMS.Tests.SeleniumTests
{
    [TestClass]
    public class DropCourseSeleniumTests
    {
        private IWebDriver driver;
        private WebDriverWait wait;
        private string baseUrl = "https://localhost:44332/"; // Your running project URL

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
            wait.Until(d => d.FindElement(By.Id("Input_Email"))).SendKeys("register@drop.com");
            driver.FindElement(By.Id("Input_Password")).SendKeys("NewPassword123!");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // Navigate directly to register page
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
            var addButton = courseCard.FindElement(By.XPath(".//button[contains(text(),'Add Course')]"));
            addButton.Click();
            // Wait for "Drop Course" button to confirm action finished
            wait.Until(d => courseCard.FindElement(By.XPath(".//button[contains(text(),'Drop Course')]")));
        }

        [TestMethod]
        public void Drop_Course_Student_Is_Registered_For()
        {
            // always trys to drop Best course

            LoginAndGoToRegistration();

            // Find the card with "Best Course" title
            var courseCard = wait.Until(d => d.FindElements(By.CssSelector(".card"))
                .FirstOrDefault(c => c.FindElement(By.CssSelector(".card-title")).Text.Contains("Best Course")));

            Assert.IsNotNull(courseCard, "Best Course not found on registration page!");

            // Find "Drop Course" button inside that card and click
            var dropButton = courseCard.FindElement(By.XPath(".//button[contains(text(),'Drop Course')]"));
            dropButton.Click();

            // Wait for "Add Course" button to confirm action finished
            wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'Add Course')]")));

            // go to dashboard to confirm course is gone
            driver.Navigate().GoToUrl(baseUrl + "Dashboard/Index");
            wait.Until(d => d.FindElement(By.TagName("body")));

            // look for all course titles
            var courses = driver.FindElements(By.XPath("//h5[@class='card-title']"));
            bool courseStillThere = courses.Any(c => c.Text.Contains("Best Course"));

            Assert.IsFalse(courseStillThere, "Course still there. Drop failed!");

            // add course again
            AddCourse("Best Course");
        }
    }
}
