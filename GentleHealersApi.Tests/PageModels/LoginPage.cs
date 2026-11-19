namespace MmaTests.PageModels;

using GentleHealersApi.PageModels;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

internal class LogInPage : BasePage
{
    private readonly IWebDriver _driver;
    public LogInPage(IWebDriver driver) : base(driver)
    {
        _driver = driver;
    }

    public void EnterCredentials(string username, string password)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));

        wait.Until(d => d.FindElements(By.Id("login-heading")).Count > 0);

        // find the username and password fields and enter valid credentials
        var emailField = _driver.FindElement(By.Id("email"));
        var passwordField = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.Name("btn-login"));
        new Actions(driver: _driver)
                .ScrollToElement(loginButton)
                .Click()
                .Perform();

        emailField.SendKeys(username);
        passwordField.SendKeys(password);
        loginButton.Click();
    }
}