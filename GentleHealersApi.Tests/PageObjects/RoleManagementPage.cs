using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests.PageObjects;

/// <summary>Page object for the role and account management screen.</summary>
public sealed class RoleManagementPage : BasePage
{
    public const string Path = "/rol/management";

    private static readonly By PageTitle = By.CssSelector("h1#page-title");
    private static readonly By UserRows = By.CssSelector(".user-row");
    private static readonly By SuccessMessage = By.CssSelector("[role='status']");
    private static readonly By ErrorMessage = By.CssSelector("[role='alert']");

    public RoleManagementPage(IWebDriver driver) : base(driver)
    {
    }

    public void Open(string baseUrl)
    {
        Driver.Navigate().GoToUrl(baseUrl.TrimEnd('/') + Path);
        CreateWait().Until(d => d.FindElement(PageTitle).Text.Contains("Rollen en rechten", StringComparison.OrdinalIgnoreCase));
    }

    public bool HasUser(string email) => FindUserRow(email, waitForUser: false) is not null;

    public string GetRoleValue(string email)
    {
        var row = FindUserRow(email) ?? throw new NoSuchElementException($"Gebruiker {email} staat niet in het overzicht.");
        return row.FindElement(By.CssSelector("select[aria-label^='Rol voor']")).GetAttribute("value") ?? string.Empty;
    }

    public void SetRole(string email, string roleName)
    {
        var row = FindUserRow(email) ?? throw new NoSuchElementException($"Gebruiker {email} staat niet in het overzicht.");
        var selector = row.FindElement(By.CssSelector("select[aria-label^='Rol voor']"));
        selector.Click();
        selector.SendKeys(roleName);
        row.FindElement(By.CssSelector("button[aria-label^='Rol wijzigen']")).Click();
    }

    public void DeactivateAccount(string email)
    {
        var row = FindUserRow(email) ?? throw new NoSuchElementException($"Gebruiker {email} staat niet in het overzicht.");
        row.FindElement(By.XPath(".//button[contains(., 'Account deactiveren')]")).Click();
    }

    public string GetSuccessMessage() => GetVisibleMessage(SuccessMessage);

    public string GetErrorMessage() => GetVisibleMessage(ErrorMessage);

    private IWebElement? FindUserRow(string email, bool waitForUser = true)
    {
        Func<IWebDriver, IWebElement?> find = driver => driver.FindElements(UserRows)
            .FirstOrDefault(row => row.Text.Contains(email, StringComparison.OrdinalIgnoreCase));

        return waitForUser
            ? CreateWait().Until(find)
            : find(Driver);
    }

    private string GetVisibleMessage(By locator) => Driver.FindElements(locator)
        .FirstOrDefault(element => element.Displayed)?.Text.Trim() ?? string.Empty;
}
