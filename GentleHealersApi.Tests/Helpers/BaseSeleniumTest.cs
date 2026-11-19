namespace GentleHealersApi.Helpers;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome; // Of OpenQA.Selenium.Firefox ;
// 1. Base Class uses MSTest [TestInitialize] and [TestCleanup] for test lifecyclemanagement
public abstract class BaseSeleniumTest
{
    // Protected property to allow derived test classes access to the driver
    protected IWebDriver Driver { get; private set; }
    // [TestInitialize] method runs before every test method
    [TestInitialize]
    public void SetUp()
    {
        Driver = new ChromeDriver();
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }
    // [TestCleanup] method runs after every test method
    [TestCleanup]
    public void TearDown()
    {
        if (Driver != null)
        {
            Driver.Quit();
            Driver.Dispose();
        }
    }
}