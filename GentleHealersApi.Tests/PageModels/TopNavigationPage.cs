using OpenQA.Selenium;

namespace GentleHealersApi.Tests.PageModels;

/// <summary>Page model for the role-sensitive top navigation.</summary>
internal sealed class TopNavigationPage(IWebDriver driver)
{
    private IWebDriver Driver { get; } = driver;
    public void OpenMenu() => Driver.FindElement(By.CssSelector(".menu-trigger")).Click();

    public bool HasRoleManagementLink => Driver.FindElements(By.CssSelector("a[href='/rol/management']")).Count > 0;

    public string ProfileRole => Driver.FindElement(By.CssSelector(".profile-role")).Text.Trim();
}
