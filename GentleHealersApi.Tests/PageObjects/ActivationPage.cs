using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests.PageObjects;

public sealed class ActivationPage : BasePage
{
    public ActivationPage(IWebDriver driver) : base(driver)
    {
    }

    public void Open(string baseUrl, string email, string? token = null, bool includeToken = true)
    {
        string query = includeToken
            ? $"?token={Uri.EscapeDataString(token ?? throw new ArgumentNullException(nameof(token)))}&email={Uri.EscapeDataString(email)}"
            : $"?email={Uri.EscapeDataString(email)}";
        Driver.Navigate().GoToUrl($"{baseUrl}/activeer{query}");
    }

    public void SetPassword(string password, string confirmation)
    {
        WaitFor(By.Id("password"));
        Driver.FindElement(By.Id("password")).SendKeys(password);
        Driver.FindElement(By.Id("confirmPassword")).SendKeys(confirmation);
        Driver.FindElement(By.CssSelector("button[type='submit']")).Click();
    }

    public void Login(string baseUrl, string email, string password)
    {
        Driver.Navigate().GoToUrl($"{baseUrl}/login");
        WaitFor(By.Id("email"));
        Driver.FindElement(By.Id("email")).SendKeys(email);
        Driver.FindElement(By.Id("password")).SendKeys(password);
        Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        CreateWait().Until(driver =>
        {
            var token = ((IJavaScriptExecutor)driver)
                .ExecuteScript("return window.localStorage.getItem('token');") as string;
            return !string.IsNullOrWhiteSpace(token)
                || driver.FindElements(By.CssSelector("[role='alert']"))
                    .Any(element => element.Displayed);
        });
    }

    public IWebElement WaitFor(By selector)
    {
        return CreateWait()
            .Until(driver => driver.FindElements(selector).FirstOrDefault(element => element.Displayed))
            ?? throw new WebDriverTimeoutException($"Element '{selector}' was not displayed.");
    }

    public string? GetLoginToken()
    {
        return ((IJavaScriptExecutor)Driver)
            .ExecuteScript("return window.localStorage.getItem('token');") as string;
    }
}
