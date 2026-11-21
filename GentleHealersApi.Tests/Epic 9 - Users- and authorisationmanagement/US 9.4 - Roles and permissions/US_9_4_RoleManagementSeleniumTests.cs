using System.Data;
using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData;
using GentleHealersApi.Tests.TestData.Factories;
using GentleHealersApi.Tests.TestData.Infrastructure;
using GentleHealersApi.Tests.TestData.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests.US_9_4;

/// <summary>Browser-driven acceptance tests for US 9.4. All created accounts are retained in the test database.</summary>
[TestClass]
[TestCategory("Selenium")]
public sealed class US_9_4_RoleManagementSeleniumTests : TestBase
{
    private static readonly TimeSpan WaitTime = TimeSpan.FromSeconds(15);

    private static IConfiguration Configuration => TestConfiguration.Configuration;
    private static string ConnectionString => Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection ontbreekt in GentleHealersApi/appsettings.json of de omgevingsvariabelen.");

    private WebDriverWait Wait => new(_driver, WaitTime);
    private RoleManagementPage RoleManagement => new(_driver);

    [TestMethod]
    public void TC_9_4_1_01_AdminCanAssignRoleAndSaveIt()
    {
        var user = CreateUser(RoleType.Patient);
        LoginAsAdministrator();
        OpenManagementPage();

        RoleManagement.SetRole(user.Email, RoleType.Specialist.ToDisplayString());

        Assert.IsTrue(Wait.Until(_ => RoleManagement.GetSuccessMessage().Contains("succesvol gewijzigd", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(RoleType.Specialist, ReadRole(user.UserId));
    }

    [TestMethod]
    public void TC_9_4_1_02_AssigningTheSameRoleKeepsTheRole()
    {
        var user = CreateUser(RoleType.Specialist);
        LoginAsAdministrator();
        OpenManagementPage();

        RoleManagement.SetRole(user.Email, RoleType.Specialist.ToDisplayString());

        Assert.IsTrue(Wait.Until(_ => RoleManagement.GetSuccessMessage().Contains("succesvol gewijzigd", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(RoleType.Specialist, ReadRole(user.UserId));
    }

    [TestMethod]
    public void TC_9_4_1_03_RoleRemainsUnchangedWhenSaveRequestFails()
    {
        var user = CreateUser(RoleType.Specialist);
        LoginAsAdministrator();
        OpenManagementPage();
        BlockManagementRequest($"*/api/admin/roles/user/{user.UserId}/role*");

        RoleManagement.SetRole(user.Email, RoleType.Patient.ToDisplayString());

        Assert.IsTrue(Wait.Until(_ => !string.IsNullOrWhiteSpace(RoleManagement.GetErrorMessage())));
        Assert.AreEqual(RoleType.Specialist, ReadRole(user.UserId));
    }

    [TestMethod]
    public void TC_9_4_1_04_NewRoleIsShownAfterTheUserSignsIn()
    {
        var user = CreateUser(RoleType.Specialist);
        LoginAsAdministrator();
        OpenManagementPage();
        RoleManagement.SetRole(user.Email, RoleType.Patient.ToDisplayString());
        Wait.Until(_ => RoleManagement.GetSuccessMessage().Contains("succesvol gewijzigd", StringComparison.OrdinalIgnoreCase));

        Login(user.Email, user.Password);

        var profile = Wait.Until(d => d.FindElement(By.CssSelector(".profile-trigger")));
        Assert.IsTrue(profile.Text.Contains("Patiënt", StringComparison.OrdinalIgnoreCase), "De navigatie toont de rol Patiënt niet.");
        Assert.AreEqual(RoleType.Patient, ReadRole(user.UserId));
    }

    [TestMethod]
    public void TC_9_4_2_01_NonAdministratorDoesNotSeeManagementMenuLink()
    {
        var user = CreateUser(RoleType.Patient);
        Login(user.Email, user.Password);

        _driver.FindElement(By.CssSelector(".menu-trigger")).Click();
        Wait.Until(d => d.FindElement(By.CssSelector("#navigation-drawer")));

        Assert.IsFalse(_driver.FindElements(By.CssSelector("#navigation-drawer a[href*='rol/management']")).Any(e => e.Displayed));
    }

    [TestMethod]
    public void TC_9_4_2_02_NonAdministratorIsDeniedOnDirectManagementUrl()
    {
        var user = CreateUser(RoleType.Patient);
        Login(user.Email, user.Password);

        _driver.Navigate().GoToUrl(BaseUrl + RoleManagementPage.Path);

        AssertAccessDenied();
        Assert.IsFalse(_driver.PageSource.Contains("Hendrik Jan", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TC_9_4_2_03_NonAdministratorIsDeniedThroughSavedManagementUrl()
    {
        var user = CreateUser(RoleType.Patient);
        Login(user.Email, user.Password);
        var bookmark = BaseUrl + RoleManagementPage.Path;

        _driver.Navigate().GoToUrl(bookmark);

        AssertAccessDenied();
        Assert.IsFalse(_driver.FindElements(By.CssSelector(".user-row, select[aria-label^='Rol voor']")).Any(e => e.Displayed));
    }

    [TestMethod]
    public void TC_9_4_2_04_NoManagementSettingsAreShownWhenLoadingFails()
    {
        LoginAsAdministrator();
        BlockManagementRequest("*/api/admin/roles/roles*", "*/api/admin/roles/users*");
        OpenManagementPage();

        Assert.IsTrue(Wait.Until(_ => !string.IsNullOrWhiteSpace(RoleManagement.GetErrorMessage())));
        Assert.IsFalse(_driver.FindElements(By.CssSelector(".user-row, select[aria-label^='Rol voor']")).Any(e => e.Displayed));
    }

    [TestMethod]
    public async Task TC_9_4_3_01_NonAdministratorCannotPromoteOwnAccount()
    {
        var user = CreateUser(RoleType.Patient);
        Login(user.Email, user.Password);
        var response = await TryChangeRoleAsCurrentUser(user.UserId, RoleType.SystemAdministrator);

        Assert.IsTrue(response is 401 or 403, $"De API gaf onverwacht HTTP {response} terug.");
        Assert.AreEqual(RoleType.Patient, ReadRole(user.UserId));
    }

    [TestMethod]
    public async Task TC_9_4_3_02_NonAdministratorCannotChangeAnotherUsersRoleFromManagementUrl()
    {
        var actor = CreateUser(RoleType.Patient);
        var target = CreateUser(RoleType.Specialist);
        Login(actor.Email, actor.Password);
        _driver.Navigate().GoToUrl(BaseUrl + RoleManagementPage.Path);
        AssertAccessDenied();

        var response = await TryChangeRoleAsCurrentUser(target.UserId, RoleType.SystemAdministrator);

        Assert.IsTrue(response is 401 or 403, $"De API gaf onverwacht HTTP {response} terug.");
        Assert.AreEqual(RoleType.Specialist, ReadRole(target.UserId));
    }

    [TestMethod]
    public async Task TC_9_4_3_03_FailedUnauthorizedChangeDoesNotChangeTargetRole()
    {
        var actor = CreateUser(RoleType.Patient);
        var target = CreateUser(RoleType.Specialist);
        Login(actor.Email, actor.Password);
        BlockManagementRequest($"*/api/admin/roles/user/{target.UserId}/role*");

        var response = await TryChangeRoleAsCurrentUser(target.UserId, RoleType.SystemAdministrator);

        Assert.IsTrue(response is null or 401 or 403, $"De onbevoegde wijziging werd onverwacht geaccepteerd (HTTP {response}).");
        Assert.AreEqual(RoleType.Specialist, ReadRole(target.UserId));
    }

    [TestMethod]
    public void TC_9_4_5_01_AdministratorCanDeactivateActiveAccount()
    {
        var user = CreateUser(RoleType.Patient);
        LoginAsAdministrator();
        OpenManagementPage();

        RoleManagement.DeactivateAccount(user.Email);

        Assert.IsTrue(Wait.Until(_ => RoleManagement.GetSuccessMessage().Contains("gedeactiveerd", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual("Inactive", ReadStatus(user.UserId));
    }

    [TestMethod]
    public void TC_9_4_5_02_DeactivatedAccountCannotSignIn()
    {
        var user = CreateUser(RoleType.Patient, status: "Inactive");
        Login(user.Email, user.Password, expectSuccess: false);

        Assert.IsTrue(Wait.Until(_ => new LoginPage(_driver).HasErrorMessage()), "Er werd geen afwijzing getoond voor een gedeactiveerd account.");
        Assert.IsTrue(new LoginPage(_driver).GetErrorMessage().Length > 0);
    }

    [TestMethod]
    public void TC_9_4_5_03_DeactivationKeepsTheUsersRole()
    {
        var user = CreateUser(RoleType.Patient);
        LoginAsAdministrator();
        OpenManagementPage();

        RoleManagement.DeactivateAccount(user.Email);

        Assert.IsTrue(Wait.Until(_ => RoleManagement.GetSuccessMessage().Contains("gedeactiveerd", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual("Inactive", ReadStatus(user.UserId));
        Assert.AreEqual(RoleType.Patient, ReadRole(user.UserId));
    }

    [TestMethod]
    public void TC_9_4_5_04_DeactivationOnlyChangesTheSelectedAccount()
    {
        var selected = CreateUser(RoleType.Patient);
        var other = CreateUser(RoleType.Specialist);
        LoginAsAdministrator();
        OpenManagementPage();

        RoleManagement.DeactivateAccount(selected.Email);

        Assert.IsTrue(Wait.Until(_ => RoleManagement.GetSuccessMessage().Contains("gedeactiveerd", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual("Inactive", ReadStatus(selected.UserId));
        Assert.AreEqual("Active", ReadStatus(other.UserId));
    }

    [TestMethod]
    public void TC_9_4_5_05_AccountRemainsActiveWhenDeactivationRequestFails()
    {
        var user = CreateUser(RoleType.Patient);
        LoginAsAdministrator();
        OpenManagementPage();
        BlockManagementRequest($"*/api/admin/roles/user/{user.UserId}/deactivate*");

        RoleManagement.DeactivateAccount(user.Email);

        Assert.IsTrue(Wait.Until(_ => !string.IsNullOrWhiteSpace(RoleManagement.GetErrorMessage())));
        Assert.AreEqual("Active", ReadStatus(user.UserId));
    }

    private void LoginAsAdministrator()
    {
        var administrator = CreateUser(RoleType.SystemAdministrator);
        Login(administrator.Email, administrator.Password);
    }

    private void Login(string email, string password, bool expectSuccess = true)
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript("localStorage.clear(); sessionStorage.clear();");
        _driver.Navigate().GoToUrl(BaseUrl + "/login");
        new LoginPage(_driver).Login(email, password);
        if (expectSuccess)
        {
            Wait.Until(d => !string.IsNullOrWhiteSpace((string?)((IJavaScriptExecutor)d).ExecuteScript("return localStorage.getItem('token');")));
        }
    }

    private void OpenManagementPage()
    {
        RoleManagement.Open(BaseUrl);
    }

    private void AssertAccessDenied()
    {
        Wait.Until(d => d.Url.Contains("/access-denied", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("Geen toegang", Wait.Until(d => d.FindElement(By.CssSelector("h1#page-title")).Text));
    }

    private async Task<int?> TryChangeRoleAsCurrentUser(int targetUserId, RoleType role)
    {
        var token = (string?)((IJavaScriptExecutor)_driver).ExecuteScript("return localStorage.getItem('token');");
        Assert.IsFalse(string.IsNullOrWhiteSpace(token), "De testgebruiker heeft geen geldige sessie.");
        var script = """
            const [url, token, roleId, done] = arguments;
            fetch(url, { method: 'PUT', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify({ roleId }) })
              .then(response => done(response.status)).catch(() => done(null));
            """;
        var result = ((IJavaScriptExecutor)_driver).ExecuteAsyncScript(script, $"{BaseUrl}/api/admin/roles/user/{targetUserId}/role", token, (int)role);
        return result is null ? null : Convert.ToInt32(result);
    }

    private void BlockManagementRequest(params string[] urlPatterns)
    {
        var chrome = _driver as ChromeDriver ?? throw new InvalidOperationException("Deze Selenium-suite vereist ChromeDriver.");
        chrome.ExecuteCdpCommand("Network.enable", new Dictionary<string, object?>());
        chrome.ExecuteCdpCommand("Network.setBlockedURLs", new Dictionary<string, object?> { ["urls"] = urlPatterns });
    }

    private static TestUserAccount CreateUser(RoleType role, string status = "Active")
    {
        new RoleSeeder(ConnectionString).EnsureRoleExists(role);
        return new TestUserFactory(ConnectionString)
            .CreateUser(role, $"us94_{role.ToString().ToLowerInvariant()}", status);
    }

    private static RoleType ReadRole(int userId) => ReadUserField(userId, "r.Name") switch
    {
        "Patient" => RoleType.Patient,
        "Specialist" => RoleType.Specialist,
        "SystemAdministrator" => RoleType.SystemAdministrator,
        _ => throw new AssertFailedException($"Onverwachte rol in de database: user {userId}.")
    };

    private static string ReadStatus(int userId) => ReadUserField(userId, "u.Status");

    private static string ReadUserField(int userId, string field)
    {
        if (field is not ("r.Name" or "u.Status")) throw new ArgumentOutOfRangeException(nameof(field));
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand($"SELECT {field} FROM dbo.AppUser u LEFT JOIN dbo.Role r ON r.RoleId = u.RoleId WHERE u.UserId = @UserId", connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        return Convert.ToString(command.ExecuteScalar()) ?? throw new AssertFailedException($"Testgebruiker {userId} ontbreekt in de database.");
    }
}
