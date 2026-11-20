using OpenQA.Selenium;

namespace GentleHealersApi.Tests.PageModels;

/// <summary>Page model for managing user roles and account statuses.</summary>
internal sealed class RoleManagementPage(IWebDriver driver)
{
    private IWebDriver Driver { get; } = driver;
    private IReadOnlyCollection<IWebElement> Rows => Driver.FindElements(By.CssSelector(".user-row"));

    public IWebElement UserRow(string email) => Rows.SingleOrDefault(row => row.Text.Contains(email, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"No role-management row was found for '{email}'.");

    public void ChooseRole(string email, int roleId)
    {
        var select = UserRow(email).FindElement(By.TagName("select"));
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));",
            select,
            roleId.ToString());
    }

    public string SelectedRole(string email) =>
        UserRow(email).FindElement(By.TagName("select")).FindElement(By.CssSelector("option:checked")).Text.Trim();

    public string AccountStatus(string email) => UserRow(email).FindElement(By.CssSelector(".status-badge")).Text.Trim();

    public void ChangeRole(string email, string fullName) =>
        UserRow(email).FindElement(By.CssSelector($"button[aria-label='Rol wijzigen voor {fullName}']")).Click();

    public void ToggleAccount(string email, string actionText) =>
        UserRow(email).FindElements(By.TagName("button")).Single(button => button.Text.Contains(actionText, StringComparison.OrdinalIgnoreCase)).Click();

    public string SuccessMessage => Driver.FindElement(By.CssSelector("[role='status']")).Text.Trim();
    public string ErrorMessage => Driver.FindElement(By.CssSelector("[role='alert']")).Text.Trim();

    public bool HasUser(string email) => Rows.Any(row => row.Text.Contains(email, StringComparison.OrdinalIgnoreCase));
}
