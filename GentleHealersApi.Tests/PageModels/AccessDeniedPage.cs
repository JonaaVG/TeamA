using OpenQA.Selenium;

namespace GentleHealersApi.Tests.PageModels;

/// <summary>Page model for the Access Denied response shown to non-administrators.</summary>
internal sealed class AccessDeniedPage(IWebDriver driver)
{
    private IWebDriver Driver { get; } = driver;
    public string Heading => Driver.FindElement(By.TagName("h1")).Text.Trim();
    public bool HasNoUserSettings => Driver.FindElements(By.CssSelector(".user-row, .user-list, #users-heading")).Count == 0;
}
