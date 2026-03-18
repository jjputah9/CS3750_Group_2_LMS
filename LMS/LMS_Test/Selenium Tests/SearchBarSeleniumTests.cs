using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace LMS.Tests.SeleniumTests
{
    [TestClass]
    public class SearchBarSeleniumTests
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
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        }

        [TestCleanup]
        public void Teardown()
        {
            driver.Quit();
        }

        public void InitLogin()
        {
            //Go to login
            driver.Navigate().GoToUrl(baseUrl + "Identity/Account/Login");

            //Set login fields
            wait.Until(d => d.FindElement(By.Id("Input_Email"))).SendKeys("student@test1.com");
            driver.FindElement(By.Id("Input_Password")).SendKeys("NewPassword123!");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            driver.Navigate().GoToUrl(baseUrl + "Registrations/Create"); // Go to registration
            wait.Until(d => d.FindElement(By.Id("Registration_Title"))); // Wait for registration
        }

        [TestMethod]
        public void Search_Returns_Empty_When_No_Results_Matched_Test()
        {
            InitLogin();

            //Find search bar
            var searchBar = driver.FindElement(By.Id("Registration_Search_Bar"));
            searchBar.SendKeys("~~~~~~~~~~~ A LONG STRING THAT WON'T BE MATCHED IN ANY CLASSES ~~~~~~~~~~~~~~");

            var submitBtn = driver.FindElement(By.Id("Registration_Submit_Button"));
            submitBtn.Click();

            var failureNotice = wait.Until(d => d.FindElement(By.Id("Search_Failure_Notice"))); // Wait for error notice

            Assert.IsNotNull(failureNotice);
            Assert.IsTrue(failureNotice.Displayed);
        }

        [TestMethod]
        public void Search_Returns_One_Text_Result()
        {
            InitLogin();

            //Find search bar
            var searchBar = driver.FindElement(By.Id("Registration_Search_Bar"));

            //First, remove all cards
            searchBar.SendKeys("~~~~~~~~~~~ A LONG STRING THAT WON'T BE MATCHED IN ANY CLASSES ~~~~~~~~~~~~~~");

            var submitBtn = driver.FindElement(By.Id("Registration_Submit_Button"));
            submitBtn.Click();

            wait.Until(d => d.FindElement(By.Id("Search_Failure_Notice"))); // Wait for error notice

            //Next, search for one card
            searchBar.Clear();
            searchBar.SendKeys("TEST 0"); // Should only be matched by one class

            wait.Until(d => d.FindElement(By.CssSelector(".card"))); //Wait for cards to appear

            var returnedCards = driver.FindElements(By.CssSelector(".card"));

            Assert.HasCount(1, returnedCards);
        }

        [TestMethod]
        public void Search_Returns_One_Dept_Result()
        {
            InitLogin();

            //Find search bar
            var searchBar = driver.FindElement(By.Id("Registration_Search_Bar"));
            var deptSelect = new SelectElement(driver.FindElement(By.Id("Registration_Dept_Select")));
            var submitBtn = driver.FindElement(By.Id("Registration_Submit_Button"));

            //First, remove all cards
            searchBar.SendKeys("~~~~~~~~~~~ A LONG STRING THAT WON'T BE MATCHED IN ANY CLASSES ~~~~~~~~~~~~~~");

            submitBtn.Click();

            wait.Until(d => d.FindElement(By.Id("Search_Failure_Notice"))); // Wait for error notice

            //Next, search for one card
            searchBar.Clear();

            deptSelect.SelectByValue("TEST"); // Should only be matched by one class

            submitBtn.Click();

            wait.Until(d => d.FindElement(By.CssSelector(".card"))); //Wait for cards to appear

            var returnedCards = driver.FindElements(By.CssSelector(".card"));

            Assert.HasCount(1, returnedCards);
        }

        [TestMethod]
        public void Search_Returns_One_Credits_Result()
        {
            InitLogin();

            //Find search bar
            var searchBar = driver.FindElement(By.Id("Registration_Search_Bar"));
            var advToggle = driver.FindElement(By.Id("toggleAdvanced"));
            var submitBtn = driver.FindElement(By.Id("Registration_Submit_Button"));

            //First, remove all cards
            searchBar.SendKeys("~~~~~~~~~~~ A LONG STRING THAT WON'T BE MATCHED IN ANY CLASSES ~~~~~~~~~~~~~~");

            submitBtn.Click();

            wait.Until(d => d.FindElement(By.Id("Search_Failure_Notice"))); // Wait for error notice

            //Next, search for one card
            searchBar.Clear();

            advToggle.Click();

            var creditHours = new SelectElement(wait.Until(d => d.FindElement(By.Id("Registration_Credit_Hours"))));
            creditHours.SelectByValue("20"); // Only the test course should have 20 credits

            submitBtn.Click();

            wait.Until(d => d.FindElement(By.CssSelector(".card"))); //Wait for cards to appear

            var returnedCards = driver.FindElements(By.CssSelector(".card"));

            Assert.HasCount(1, returnedCards);
        }
    }
}