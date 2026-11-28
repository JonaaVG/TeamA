using GentleHealersApi.Tests.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests;

[TestClass]
public class US1_1_BasisLogin
{
    [TestMethod]
    [TestCategory("Browser")]
    public void User_Can_Successfully_LogIn_Valid_Credentials()
    {
        var url = Environment.GetEnvironmentVariable("GENTLEHEALERS_BROWSER_TEST_URL");
        var email = Environment.GetEnvironmentVariable("GENTLEHEALERS_BROWSER_TEST_EMAIL");
        var password = Environment.GetEnvironmentVariable("GENTLEHEALERS_BROWSER_TEST_PASSWORD");

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            Assert.Inconclusive("Configure the local browser test URL and test account credentials.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var baseUri) ||
            !baseUri.IsLoopback || (baseUri.Scheme != "http" && baseUri.Scheme != "https"))
        {
            throw new InvalidOperationException("The browser test URL must be a local HTTP(S) address.");
        }

        var options = new ChromeOptions();
        options.AddArguments("--headless", "--window-size=1440,1000");
        using var driver = new ChromeDriver(options);
        driver.Navigate().GoToUrl(new Uri(baseUri, "/login"));

        var loginPage = new LoginPage(driver);
        loginPage.Login(email!, password!);
        Assert.IsFalse(loginPage.HasErrorMessage(), "A valid test account must be able to sign in.");

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(browser =>
            new Uri(browser.Url).AbsolutePath == "/" &&
            browser.FindElements(By.Id("page-title")).Any(heading => heading.Displayed));
    }
}
