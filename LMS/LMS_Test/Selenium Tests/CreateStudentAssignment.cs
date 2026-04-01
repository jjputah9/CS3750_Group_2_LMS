using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;
using System.Threading;

namespace LMS_Test.Selenium_Tests
{
    [TestClass]
    public class CreateStudentAssignment
    {
        private IWebDriver driver;
        private WebDriverWait wait;
        private string baseUrl = "https://localhost:7270/"; // Your running project URL
        private const int DelayMs = 2000; // 2 second delay between actions

        [TestInitialize]
        public void Setup()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddArgument("--ignore-certificate-errors"); // For localhost HTTPS

            driver = new ChromeDriver(options);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15)); // Increased timeout
        }

        [TestCleanup]
        public void Teardown()
        {
            driver.Quit();
        }

        /// <summary>
        /// Scrolls element into view and clicks using JavaScript to avoid interception issues
        /// </summary>
        private void ScrollAndClick(IWebElement element)
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            // Scroll element into center of viewport
            jsExecutor.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
            Thread.Sleep(300); // Brief pause for scroll to complete
            // Use JS click to avoid interception
            jsExecutor.ExecuteScript("arguments[0].click();", element);
        }

        private void LoginAsInstructor()
        {
            // Go to login page
            driver.Navigate().GoToUrl(baseUrl + "Identity/Account/Login");
            Thread.Sleep(DelayMs);

            // Set login fields for instructor
            wait.Until(d => d.FindElement(By.Id("Input_Email"))).SendKeys("instructor@test.com");
            Thread.Sleep(500);
            driver.FindElement(By.Id("Input_Password")).SendKeys("NewPassword123!");
            Thread.Sleep(500);
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();
            Thread.Sleep(DelayMs);

            // Wait for login to complete
            wait.Until(d => d.Url.Contains("Dashboard") || d.Url.EndsWith("/"));
            Thread.Sleep(DelayMs);
        }

        private int NavigateToCourseAssignments()
        {
            // Go to dashboard
            driver.Navigate().GoToUrl(baseUrl + "Dashboard/Index");
            Thread.Sleep(DelayMs);

            // Wait for course cards to load
            var courseCard = wait.Until(d => d.FindElements(By.CssSelector(".card"))
                .FirstOrDefault(c => c.FindElements(By.CssSelector(".card-title")).Any()));
            Thread.Sleep(DelayMs);

            Assert.IsNotNull(courseCard, "No courses found for instructor!");

            // Click "Manage Assignments" button
            var manageButton = courseCard.FindElement(By.XPath(".//a[contains(text(),'Manage Assignments')]"));
            ScrollAndClick(manageButton);
            Thread.Sleep(DelayMs);

            // Debug: Check current URL after click
            var currentUrl = driver.Url;
            Console.WriteLine($"URL after clicking Manage Assignments: {currentUrl}");
            
            // Check if we got a Forbid/Access Denied page
            if (driver.PageSource.Contains("Access Denied") || driver.PageSource.Contains("Forbid"))
            {
                Assert.Fail("Access denied - instructor may not own this course or user type is incorrect");
            }

            // Wait for assignments page to load (handle both URL patterns)
            wait.Until(d => d.Url.Contains("Assignments/Index") || d.Url.Contains("Assignments/"));
            Thread.Sleep(DelayMs);

            // Extract courseId from URL - handle route parameter pattern /Assignments/Index/123
            var url = driver.Url;
            int courseId;
            if (url.Contains("courseId="))
            {
                var courseIdParam = url.Split("courseId=").Last().Split('&').First();
                courseId = int.Parse(courseIdParam);
            }
            else
            {
                // URL pattern: /Assignments/Index/123 or /Assignments/123
                var segments = new Uri(url).Segments;
                courseId = int.Parse(segments.Last().TrimEnd('/'));
            }
            
            return courseId;
        }

        /// <summary>
        /// Sets a datetime-local input field using JavaScript for reliable cross-browser support
        /// </summary>
        private void SetDateTimeInput(string elementId, DateTime dateTime)
        {
            // Format for datetime-local input: yyyy-MM-ddTHH:mm
            var formattedDate = dateTime.ToString("yyyy-MM-ddTHH:mm");
            
            // Use JavaScript to set the value directly (more reliable than SendKeys for date inputs)
            var jsExecutor = (IJavaScriptExecutor)driver;
            jsExecutor.ExecuteScript($"document.getElementById('{elementId}').value = '{formattedDate}';");
            Thread.Sleep(500);
        }

        [TestMethod]
        public void Professor_Can_Create_Assignment_FileUpload()
        {
            LoginAsInstructor();

            int courseId = NavigateToCourseAssignments();

            // Click "Create New" button
            var createButton = wait.Until(d => d.FindElement(By.XPath("//a[contains(text(),'+ New Assignment')]")));
            ScrollAndClick(createButton);
            Thread.Sleep(DelayMs);

            // Wait for create page to load
            wait.Until(d => d.Url.Contains("Assignments/Create"));
            Thread.Sleep(DelayMs);

            // Fill in assignment form
            var titleInput = wait.Until(d => d.FindElement(By.Id("Assignment_Title")));
            titleInput.SendKeys("Selenium Test Assignment");
            Thread.Sleep(500);

            var descriptionInput = driver.FindElement(By.Id("Assignment_Description"));
            descriptionInput.SendKeys("This is a test assignment created by Selenium.");
            Thread.Sleep(500);

            var pointsInput = driver.FindElement(By.Id("Assignment_Points"));
            pointsInput.Clear();
            pointsInput.SendKeys("100");
            Thread.Sleep(500);

            // Set due date to 7 days from now at 11:59 PM
            var dueDate = DateTime.Now.AddDays(7).Date.AddHours(23).AddMinutes(59);
            SetDateTimeInput("Assignment_DueDate", dueDate);

            // Select submission type (File Upload)
            var submissionTypeSelect = new SelectElement(driver.FindElement(By.Id("Assignment_SubmissionTypeId")));
            submissionTypeSelect.SelectByText("File Upload");
            Thread.Sleep(DelayMs);

            // Submit the form
            var submitButton = driver.FindElement(By.CssSelector("input[type='submit'][value='Create']"));
            ScrollAndClick(submitButton);
            Thread.Sleep(DelayMs);

            // Wait for redirect to assignments index
            wait.Until(d => d.Url.Contains("Assignments/Index") || d.Url.Contains("Assignments/"));
            Thread.Sleep(DelayMs);

            // Verify assignment appears in the list
            var pageSource = driver.PageSource;
            Assert.IsTrue(pageSource.Contains("Selenium Test Assignment"), "Created assignment not found in assignments list!");
        }

        [TestMethod]
        public void Professor_Can_Create_Assignment_TextEntry()
        {
            LoginAsInstructor();

            int courseId = NavigateToCourseAssignments();

            // Click "+ New Assignment" button (matches the actual button text)
            var createButton = wait.Until(d => d.FindElement(By.XPath("//a[contains(text(),'+ New Assignment')]")));
            ScrollAndClick(createButton);
            Thread.Sleep(DelayMs);

            // Wait for create page to load
            wait.Until(d => d.Url.Contains("Assignments/Create"));
            Thread.Sleep(DelayMs);

            // Fill in assignment form
            var titleInput = wait.Until(d => d.FindElement(By.Id("Assignment_Title")));
            titleInput.SendKeys("Text Entry Assignment Test");
            Thread.Sleep(500);

            var descriptionInput = driver.FindElement(By.Id("Assignment_Description"));
            descriptionInput.SendKeys("Submit your essay here.");
            Thread.Sleep(500);

            var pointsInput = driver.FindElement(By.Id("Assignment_Points"));
            pointsInput.Clear();
            pointsInput.SendKeys("50");
            Thread.Sleep(500);

            // Set due date to 14 days from now at 11:59 PM
            var dueDate = DateTime.Now.AddDays(14).Date.AddHours(23).AddMinutes(59);
            SetDateTimeInput("Assignment_DueDate", dueDate);

            // Select submission type (Text Entry)
            var submissionTypeSelect = new SelectElement(driver.FindElement(By.Id("Assignment_SubmissionTypeId")));
            submissionTypeSelect.SelectByText("Text Entry");
            Thread.Sleep(DelayMs);

            // Submit the form
            var submitButton = driver.FindElement(By.CssSelector("input[type='submit'][value='Create']"));
            ScrollAndClick(submitButton);
            Thread.Sleep(DelayMs);

            // Wait for redirect to assignments index
            wait.Until(d => d.Url.Contains("Assignments/Index") || d.Url.Contains("Assignments/"));
            Thread.Sleep(DelayMs);

            // Verify assignment appears in the list
            var pageSource = driver.PageSource;
            Assert.IsTrue(pageSource.Contains("Text Entry Assignment Test"), "Created assignment not found in assignments list!");
        }

        [TestMethod]
        public void Professor_Cannot_Create_Assignment_Without_Required_Fields()
        {
            LoginAsInstructor();

            int courseId = NavigateToCourseAssignments();

            // Click "Create New" button
            var createButton = wait.Until(d => d.FindElement(By.XPath("//a[contains(text(),'+ New Assignment')]")));
            ScrollAndClick(createButton);
            Thread.Sleep(DelayMs);

            // Wait for create page to load
            wait.Until(d => d.Url.Contains("Assignments/Create"));
            Thread.Sleep(DelayMs);

            // Try to submit without filling required fields
            var submitButton = driver.FindElement(By.CssSelector("input[type='submit'][value='Create']"));
            ScrollAndClick(submitButton);
            Thread.Sleep(DelayMs);

            // Verify we're still on the create page (validation failed)
            Assert.IsTrue(driver.Url.Contains("Assignments/Create"), "Should remain on create page when validation fails!");

            // Verify validation errors appear
            var validationErrors = driver.FindElements(By.CssSelector(".text-danger"));
            Assert.IsTrue(validationErrors.Count > 0, "Validation errors should be displayed!");
        }

        [TestMethod]
        public void Professor_Can_Cancel_Assignment_Creation()
        {
            LoginAsInstructor();

            int courseId = NavigateToCourseAssignments();

            // Click "Create New" button
            var createButton = wait.Until(d => d.FindElement(By.XPath("//a[contains(text(),'+ New Assignment')]")));
            ScrollAndClick(createButton);
            Thread.Sleep(DelayMs);

            // Wait for create page to load
            wait.Until(d => d.Url.Contains("Assignments/Create"));
            Thread.Sleep(DelayMs);

            // Fill in some fields
            var titleInput = wait.Until(d => d.FindElement(By.Id("Assignment_Title")));
            titleInput.SendKeys("Assignment to be Cancelled");
            Thread.Sleep(DelayMs);

            // Click cancel button - use partial match for flexibility
            var cancelButton = driver.FindElement(By.XPath("//a[contains(text(),'Cancel') or contains(text(),'Back')]"));
            ScrollAndClick(cancelButton);
            Thread.Sleep(DelayMs);

            // Verify we're back at assignments index
            wait.Until(d => d.Url.Contains("Assignments/Index") || d.Url.Contains("Assignments/"));
            Thread.Sleep(DelayMs);

            // Verify the cancelled assignment was not created
            var pageSource = driver.PageSource;
            Assert.IsFalse(pageSource.Contains("Assignment to be Cancelled"), "Cancelled assignment should not be in the list!");
        }
    }
}
