using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData.Factories;
using GentleHealersApi.Tests.TestData.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests;

[TestClass]
public class AC1_1_3_Log_Out : TestBase
{
    private LoginTestData? _loginTestData;
    private TestUserAccount TestAccount => _loginTestData?.Account
        ?? throw new InvalidOperationException("Test login account was not created.");

    [TestInitialize]
    public void CreateLoginTestData()
    {
        _loginTestData = new LoginTestData();
    }

    [TestCleanup]
    public void DeleteLoginTestData()
    {
        _loginTestData?.Dispose();
    }

    [TestMethod]
    public void TC01_Log_Out_Successful()
    {
        _logger.Info("Starting successful logout test.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));

        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );

        EmailInput.SendKeys(TestAccount.Email);
        passwordInput.SendKeys(TestAccount.Password);
        loginButton.Click();

        wait.Until(d => _driver.FindElement(By.Id("page-title")).Displayed);
        var logoutButton = _driver.FindElement(By.XPath("//button[normalize-space()='Uitloggen']"));
        logoutButton.Click();

        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    public void TC02_Check_If_Session_Ended()
    {
        _logger.Info("Starting session-expiration check after logout.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys(TestAccount.Email);
        passwordInput.SendKeys(TestAccount.Password);
        loginButton.Click();
        wait.Until(d => _driver.FindElement(By.Id("page-title")).Displayed);
        var logoutButton = _driver.FindElement(By.XPath("//button[normalize-space()='Uitloggen']"));
        logoutButton.Click();
        // Attempt to navigate to a protected page after logging out
        _driver.Navigate().GoToUrl($"{BaseUrl}/patient/aanmaken");
        // Check if the user is redirected to the login page
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    public void TC03_Direct_Back_After_Logout()
    {
        _logger.Info("Starting browser-back navigation check after logout.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys(TestAccount.Email);
        passwordInput.SendKeys(TestAccount.Password);
        loginButton.Click();
        wait.Until(d => _driver.FindElement(By.Id("page-title")).Displayed);
        var logoutButton = _driver.FindElement(By.XPath("//button[normalize-space()='Uitloggen']"));
        logoutButton.Click();
        // Attempt to navigate back in the browser after logging out
        _driver.Navigate().Back();
        // Check if the user is redirected to the login page
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    public void TC04_Open_Protected_Page_After_Logout()
    {
        _logger.Info("Starting protected-page access check after logout.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys(TestAccount.Email);
        passwordInput.SendKeys(TestAccount.Password);
        loginButton.Click();
        wait.Until(d => _driver.FindElement(By.Id("page-title")).Displayed);
        var logoutButton = _driver.FindElement(By.XPath("//button[normalize-space()='Uitloggen']"));
        logoutButton.Click();
        // Attempt to open a protected page directly after logging out
        _driver.Navigate().GoToUrl($"{BaseUrl}/patient/aanmaken");
        // Check if the user is redirected to the login page
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    public void TC05_Login_After_Logout()
    {
        _logger.Info("Starting login-again-after-logout test.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys(TestAccount.Email);
        passwordInput.SendKeys(TestAccount.Password);
        loginButton.Click();
        wait.Until(d => _driver.FindElement(By.Id("page-title")).Displayed);
        var logoutButton = _driver.FindElement(By.XPath("//button[normalize-space()='Uitloggen']"));
        logoutButton.Click();
        // Log in again after logging out
        _driver.FindElement(By.Id("email")).SendKeys(TestAccount.Email);
        _driver.FindElement(By.Id("password")).SendKeys(TestAccount.Password);
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        wait.Until(d => _driver.FindElement(By.Id("page-title")).Displayed);
        // Check if the user is redirected to the home page after logging in again
        Assert.AreEqual($"{BaseUrl}/", _driver.Url);
    }
}
