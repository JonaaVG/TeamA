using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData.Factories;
using GentleHealersApi.Tests.TestData.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests;

[TestClass]
public class AC1_1_1_Succesfull_Login : TestBase
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
    public void TC01_LoginWithValidCredentials()
    {
        _logger.Info("Starting valid login test.");
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

        Assert.AreEqual($"{BaseUrl}/", _driver.Url);
    }

    [TestMethod]
    public void TC02_LoginWithCapitalizationInEmail()
    {
        _logger.Info("Starting case-insensitive email login test.");
        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var EmailInput = _driver.FindElement(By.Id("email"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
        wait.Until(
            d => EmailInput.Displayed && passwordInput.Displayed && loginButton.Displayed
        );
        EmailInput.SendKeys(TestAccount.Email.ToUpperInvariant());
        passwordInput.SendKeys(TestAccount.Password);
        loginButton.Click();

        wait.Until(d => _driver.FindElement(By.Id("page-title")).Displayed);
        Assert.AreEqual($"{BaseUrl}/", _driver.Url);
    }
}
