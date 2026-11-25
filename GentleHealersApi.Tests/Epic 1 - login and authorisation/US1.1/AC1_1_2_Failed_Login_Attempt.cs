using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData.Factories;
using GentleHealersApi.Tests.TestData.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests;

[TestClass]
public class AC1_1_2_Failed_Login_Attempt : TestBase
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
    public void TC01_Login_Invalid_Email()
    {
        _logger.Info("Starting login test with an unknown email address.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));

        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );

        EmailInput.SendKeys("doesnotexist@example.com");
        passwordInput.SendKeys(TestAccount.Password);
        loginButton.Click();

        var errorMessage = wait.Until(d => d.FindElement(By.CssSelector("[role='alert']")));

        Assert.IsTrue(errorMessage.Displayed);
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    public void TC02_Login_Invalid_Password()
    {
        _logger.Info("Starting login test with an incorrect password.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys(TestAccount.Email);
        passwordInput.SendKeys("WrongPassword123!");
        loginButton.Click();
        var errorMessage = wait.Until(d => d.FindElement(By.CssSelector("[role='alert']")));
        Assert.IsTrue(errorMessage.Displayed);
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    public void TC03_Login_Invalid_Email_And_Password()
    {
        _logger.Info("Starting login test with an unknown email and incorrect password.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys("doesnotexist@example.com");
        passwordInput.SendKeys("WrongPassword123!");
        loginButton.Click();
        var errorMessage = wait.Until(d => d.FindElement(By.CssSelector("[role='alert']")));
        Assert.IsTrue(errorMessage.Displayed);
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    public void TC04_Login_Empty_Email()
    {
        _logger.Info("Starting login test with an empty email address.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys("");
        passwordInput.SendKeys(TestAccount.Password);
        loginButton.Click();
        var errorMessage = wait.Until(d => d.FindElement(By.CssSelector("[role='alert']")));
        Assert.IsTrue(errorMessage.Displayed);
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    public void TC05_Login_Empty_Password()
    {
        _logger.Info("Starting login test with an empty password.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys(TestAccount.Email);
        passwordInput.SendKeys("");
        loginButton.Click();
        var errorMessage = wait.Until(d => d.FindElement(By.CssSelector("[role='alert']")));
        Assert.IsTrue(errorMessage.Displayed);
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    public void TC06_Login_Empty_Email_And_Password()
    {
        _logger.Info("Starting login test with empty email and password fields.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys("");
        passwordInput.SendKeys("");
        loginButton.Click();
        var errorMessage = wait.Until(d => d.FindElement(By.CssSelector("[role='alert']")));
        Assert.IsTrue(errorMessage.Displayed);
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }
}
