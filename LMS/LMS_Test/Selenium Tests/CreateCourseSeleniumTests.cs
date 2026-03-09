using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace LMS.Tests.SeleniumTests
{
    [TestClass]
    public class CreateCourseSeleniumTests
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

        private void LoginAndGoToCreate()
        {
            // Go to login page
            driver.Navigate().GoToUrl(baseUrl + "Identity/Account/Login");

            // Fill in login
            wait.Until(d => d.FindElement(By.Id("Input_Email"))).SendKeys("instructor@test1.com");
            driver.FindElement(By.Id("Input_Password")).SendKeys("NewPassword123!");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // Navigate directly to Create page
            driver.Navigate().GoToUrl(baseUrl + "Courses/Create");

            // Wait for form to load
            wait.Until(d => d.FindElement(By.Id("Course_DeptName")));
        }

        [TestMethod]
        public void Create_Fails_When_Title_Missing_UI()
        {
            LoginAndGoToCreate();

            // Select DeptName
            var deptSelect = new SelectElement(driver.FindElement(By.Id("Course_DeptName")));
            deptSelect.SelectByValue("CS");

            // Fill CourseNum
            driver.FindElement(By.Id("Course_CourseNum")).SendKeys("1234");

            // Do NOT fill CourseTitle (testing validation)

            // Fill other required fields
            driver.FindElement(By.Id("Course_CreditHours")).SendKeys("3");
            driver.FindElement(By.Id("Course_Capacity")).SendKeys("20");

            // Check at least one meeting day
            driver.FindElement(By.Id("Course_MeetDays_0")).Click(); // Monday

            // Fill times
            driver.FindElement(By.Id("Course_StartTime")).SendKeys("09:00");
            driver.FindElement(By.Id("Course_EndTime")).SendKeys("10:00");

            // Submit form
            driver.FindElement(By.CssSelector("input[type='submit']")).Click();

            // Wait for validation error
            var errorSpan = wait.Until(d => d.FindElement(By.CssSelector("span[data-valmsg-for='Course.CourseTitle']")));

            // Assert that the validation message is displayed
            Assert.IsTrue(errorSpan.Displayed);
            StringAssert.Contains(errorSpan.Text, "The CourseTitle field is required");
        }
    }
}