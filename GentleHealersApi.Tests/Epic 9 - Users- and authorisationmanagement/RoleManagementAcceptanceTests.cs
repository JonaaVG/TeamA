using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GentleHealersApi.Tests.PageModels;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace GentleHealersApi.Tests;

/// <summary>Runs US 9.4 acceptance scenarios through the Vue app and protected API.</summary>
[TestClass]
[DoNotParallelize]
public sealed class RoleManagementAcceptanceTests
{
    private const string PatientEmail = "hendrik.jan@example.com";
    private const string OtherUserEmail = "alice@example.com";
    private RoleManagementTestContext _testData = null!;
    private TestAccount _admin = null!;
    private TestAccount _patient = null!;
    private TestAccount _otherUser = null!;
    private IWebDriver _browser = null!;

    [TestInitialize]
    public void SetUpScenarioDataAndBrowser()
    {
        _testData = new RoleManagementTestContext();
        _admin = _testData.EnsureAccount("admin@gentlehealers.nl", "Admin", "Beheer", "SystemAdministrator", "Active");
        _patient = _testData.EnsureAccount(PatientEmail, "Hendrik", "Jan", "Patient", "Active");
        _otherUser = _testData.EnsureAccount(OtherUserEmail, "Alice", "Bakker", "Specialist", "Active");

        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1440,1000");
        options.AddArgument("--no-sandbox");
        _browser = new ChromeDriver(options);
        _browser.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(20);
        _browser.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
    }

    [TestCleanup]
    public void RestoreTestDataAndCloseBrowser()
    {
        try
        {
            _browser?.Quit();
            _browser?.Dispose();
        }
        finally
        {
            _testData?.Dispose();
        }
    }

    [TestMethod]
    public void TC_9_4_1_01_AdminChangesRoleAndSeesItSaved()
    {
        OpenAsAdmin("/rol/management");
        var page = new RoleManagementPage(_browser);
        WaitFor(() => page.HasUser(PatientEmail), "Role-management users did not load.");

        page.ChooseRole(PatientEmail, _testData.RoleId("Specialist"));
        page.ChangeRole(PatientEmail, "Hendrik Jan");

        WaitFor(() => page.SuccessMessage.Contains("Hendrik Jan") && page.SuccessMessage.Contains("succesvol"),
            "The role-change success message did not appear.");
        Assert.AreEqual("Specialist", page.SelectedRole(PatientEmail));
        Assert.AreEqual("Specialist", _testData.ReadAccount(PatientEmail).RoleName,
            "The selected role should also be stored in SQL Server.");
    }

    [TestMethod]
    public void TC_9_4_1_02_ReassigningCurrentRoleKeepsRoleAndShowsSuccess()
    {
        _testData.EnsureAccount(PatientEmail, "Hendrik", "Jan", "Specialist", "Active");
        OpenAsAdmin("/rol/management");
        var page = new RoleManagementPage(_browser);
        WaitFor(() => page.HasUser(PatientEmail), "Role-management users did not load.");

        page.ChooseRole(PatientEmail, _testData.RoleId("Specialist"));
        page.ChangeRole(PatientEmail, "Hendrik Jan");

        WaitFor(() => page.SuccessMessage.Contains("succesvol"), "The repeated role assignment was not confirmed.");
        Assert.AreEqual("Specialist", page.SelectedRole(PatientEmail));
        Assert.AreEqual("Specialist", _testData.ReadAccount(PatientEmail).RoleName);
    }

    [TestMethod]
    public void TC_9_4_1_03_SaveFailureShowsErrorAndKeepsCurrentRole()
    {
        OpenAsAdmin("/rol/management");
        var page = new RoleManagementPage(_browser);
        WaitFor(() => page.HasUser(PatientEmail), "Role-management users did not load.");

        page.ChooseRole(PatientEmail, _testData.RoleId("Specialist"));
        FailNextApiPut("/role");
        page.ChangeRole(PatientEmail, "Hendrik Jan");

        WaitFor(() => !string.IsNullOrWhiteSpace(page.ErrorMessage), "The role-update error message did not appear.");
        Assert.AreEqual("Patient", page.SelectedRole(PatientEmail));
        Assert.AreEqual("Patient", _testData.ReadAccount(PatientEmail).RoleName,
            "A failed role update must not change the stored role.");
    }

    [TestMethod]
    public void TC_9_4_1_04_UpdatedRoleIsShownInNavigationForThatRole()
    {
        _patient = _testData.EnsureAccount(PatientEmail, "Hendrik", "Jan", "Specialist", "Active");
        OpenAsAdmin("/rol/management");
        var managementPage = new RoleManagementPage(_browser);
        WaitFor(() => managementPage.HasUser(PatientEmail), "Role-management users did not load.");

        managementPage.ChooseRole(PatientEmail, _testData.RoleId("Patient"));
        managementPage.ChangeRole(PatientEmail, "Hendrik Jan");
        WaitFor(() => managementPage.SuccessMessage.Contains("succesvol"), "The role change did not complete.");
        Assert.AreEqual("Patient", _testData.ReadAccount(PatientEmail).RoleName);

        OpenAs(_patient, "Patient", "/");
        var navigation = new TopNavigationPage(_browser);
        WaitFor(() => navigation.ProfileRole == "Patiënt", "The navigation did not show the user's assigned role.");
        Assert.AreEqual("Patiënt", navigation.ProfileRole);
    }

    [TestMethod]
    public void TC_9_4_2_01_NonAdministratorDoesNotSeeRoleManagementMenuLink()
    {
        OpenAs(_patient, "Patient", "/");
        var navigation = new TopNavigationPage(_browser);
        navigation.OpenMenu();

        Assert.IsFalse(navigation.HasRoleManagementLink,
            "A user without administrator rights must not see the role-management menu item.");
    }

    [TestMethod]
    public void TC_9_4_2_02_DirectRoleManagementUrlOpensAccessDenied()
    {
        OpenAs(_patient, "Patient", "/rol/management");
        var page = new AccessDeniedPage(_browser);

        WaitFor(() => page.Heading == "Geen toegang", "The Access Denied page did not open.");
        Assert.AreEqual("Geen toegang", page.Heading);
        Assert.IsTrue(page.HasNoUserSettings, "User and role settings must not be rendered for a non-administrator.");
    }

    [TestMethod]
    public void TC_9_4_2_03_SavedRoleManagementUrlStillOpensAccessDenied()
    {
        // Opening a previously saved URL has the same route-guard behavior as entering it directly.
        OpenAs(_patient, "Patient", "/rol/management");
        var page = new AccessDeniedPage(_browser);

        WaitFor(() => page.Heading == "Geen toegang", "The saved management link was not denied.");
        Assert.IsTrue(page.HasNoUserSettings);
    }

    [TestMethod]
    public void TC_9_4_2_04_NonAdministratorSeesNoSettingsWhenDirectAccessIsDenied()
    {
        OpenAs(_patient, "Patient", "/rol/management");
        var page = new AccessDeniedPage(_browser);

        WaitFor(() => page.Heading == "Geen toegang", "Access was not denied.");
        Assert.IsTrue(page.HasNoUserSettings);
        Assert.IsFalse(_browser.PageSource.Contains(PatientEmail, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(_browser.PageSource.Contains(OtherUserEmail, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task TC_9_4_3_01_NonAdministratorCannotChangeOwnRoleThroughApi()
    {
        var stateBefore = _testData.ReadAccount(PatientEmail);
        using var response = await SendRoleChangeAsPatient(_patient.UserId, _testData.RoleId("SystemAdministrator"));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(stateBefore.RoleName, _testData.ReadAccount(PatientEmail).RoleName);
    }

    [TestMethod]
    public async Task TC_9_4_3_02_NonAdministratorCannotChangeAnotherUsersRoleThroughApi()
    {
        var stateBefore = _testData.ReadAccount(OtherUserEmail);
        using var response = await SendRoleChangeAsPatient(_otherUser.UserId, _testData.RoleId("SystemAdministrator"));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(stateBefore.RoleName, _testData.ReadAccount(OtherUserEmail).RoleName);
    }

    [TestMethod]
    public void TC_9_4_5_01_AdminDeactivatesAccountAndSeesInactiveStatus()
    {
        OpenAsAdmin("/rol/management");
        var page = new RoleManagementPage(_browser);
        WaitFor(() => page.HasUser(PatientEmail), "Role-management users did not load.");

        page.ToggleAccount(PatientEmail, "Account deactiveren");
        WaitFor(() => page.AccountStatus(PatientEmail) == "Inactief", "The inactive account status did not appear.");

        Assert.IsTrue(page.SuccessMessage.Contains("gedeactiveerd"));
        Assert.AreEqual("Inactive", _testData.ReadAccount(PatientEmail).Status);
    }

    [TestMethod]
    public void TC_9_4_5_03_DeactivatingAccountPreservesItsRole()
    {
        var roleBefore = _testData.ReadAccount(PatientEmail).RoleName;
        OpenAsAdmin("/rol/management");
        var page = new RoleManagementPage(_browser);
        WaitFor(() => page.HasUser(PatientEmail), "Role-management users did not load.");

        page.ToggleAccount(PatientEmail, "Account deactiveren");
        WaitFor(() => page.AccountStatus(PatientEmail) == "Inactief", "The account did not become inactive.");

        var stored = _testData.ReadAccount(PatientEmail);
        Assert.AreEqual("Inactive", stored.Status);
        Assert.AreEqual(roleBefore, stored.RoleName);
    }

    [TestMethod]
    public void TC_9_4_5_04_DeactivatingOneAccountLeavesAnotherAccountActive()
    {
        OpenAsAdmin("/rol/management");
        var page = new RoleManagementPage(_browser);
        WaitFor(() => page.HasUser(PatientEmail) && page.HasUser(OtherUserEmail), "Both test accounts did not load.");

        page.ToggleAccount(PatientEmail, "Account deactiveren");
        WaitFor(() => page.AccountStatus(PatientEmail) == "Inactief", "The selected account did not become inactive.");

        Assert.AreEqual("Inactive", _testData.ReadAccount(PatientEmail).Status);
        Assert.AreEqual("Active", _testData.ReadAccount(OtherUserEmail).Status);
        Assert.AreEqual("Actief", page.AccountStatus(OtherUserEmail));
    }

    [TestMethod]
    public void TC_9_4_5_05_DeactivationRequestFailureShowsErrorAndKeepsAccountActive()
    {
        OpenAsAdmin("/rol/management");
        var page = new RoleManagementPage(_browser);
        WaitFor(() => page.HasUser(PatientEmail), "Role-management users did not load.");

        FailNextApiPut("/deactivate");
        page.ToggleAccount(PatientEmail, "Account deactiveren");

        WaitFor(() => !string.IsNullOrWhiteSpace(page.ErrorMessage), "The deactivation error message did not appear.");
        Assert.AreEqual("Actief", page.AccountStatus(PatientEmail));
        Assert.AreEqual("Active", _testData.ReadAccount(PatientEmail).Status);
    }

    private void OpenAsAdmin(string path) => OpenAs(_admin, "SystemAdministrator", path);

    private void OpenAs(TestAccount account, string roleName, string path)
    {
        var token = _testData.CreateToken(account, roleName);
        _browser.Navigate().GoToUrl($"{_testData.ClientBaseUrl}/login");
        ((IJavaScriptExecutor)_browser).ExecuteScript("localStorage.setItem('token', arguments[0]);", token);
        _browser.Navigate().GoToUrl($"{_testData.ClientBaseUrl}{path}");
    }

    private async Task<HttpResponseMessage> SendRoleChangeAsPatient(int targetUserId, int roleId)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_testData.ApiBaseUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", _testData.CreateToken(_patient, "Patient"));
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/roles/user/{targetUserId}/role")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { roleId }), Encoding.UTF8, "application/json"),
        };
        return await client.SendAsync(request);
    }

    private void FailNextApiPut(string urlPart)
    {
        ((IJavaScriptExecutor)_browser).ExecuteScript("""
            const target = arguments[0];
            const originalOpen = XMLHttpRequest.prototype.open;
            const originalSend = XMLHttpRequest.prototype.send;
            XMLHttpRequest.prototype.open = function(method, url, ...rest) {
              this.__failThisRequest = method.toUpperCase() === 'PUT' && String(url).includes(target);
              return originalOpen.call(this, method, url, ...rest);
            };
            XMLHttpRequest.prototype.send = function(...args) {
              if (this.__failThisRequest) {
                setTimeout(() => {
                  this.dispatchEvent(new ProgressEvent('error'));
                  this.dispatchEvent(new ProgressEvent('loadend'));
                }, 0);
                return;
              }
              return originalSend.apply(this, args);
            };
            """, urlPart);
    }

    private static void WaitFor(Func<bool> condition, string failureMessage)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            try
            {
                if (condition()) return;
            }
            catch (NoSuchElementException) { }
            catch (StaleElementReferenceException) { }
            Thread.Sleep(100);
        }

        Assert.Fail(failureMessage);
    }
}
