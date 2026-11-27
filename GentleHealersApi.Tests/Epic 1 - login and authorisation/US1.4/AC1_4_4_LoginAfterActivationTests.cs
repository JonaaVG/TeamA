using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData.Infrastructure;
using GentleHealersApi.Tests.TestData.Models;
using OpenQA.Selenium;

namespace GentleHealersApi.Tests;

[TestClass]
public class AC1_4_4_LoginAfterActivationTests : TestBase
{
    private const string ValidPassword = "ValidPassword!2026";
    private ActivationTestData? _data;
    private ActivationPage _page = null!;

    [TestInitialize]
    public void CreateTestData()
    {
        _data = new ActivationTestData();
        _page = new ActivationPage(_driver);
    }

    [TestCleanup]
    public void DeleteTestData() => _data?.Dispose();

    [TestMethod]
    [TestCategory("TC-1.4.4-01")]
    public void TC01_PatientCanLogInAfterActivation()
    {
        _logger.Info("Starting patient login test after account activation.");
        ActivateAccount();
        _page.Login(BaseUrl, Data.Email, ValidPassword);

        Assert.IsTrue(_page.WaitFor(By.Id("page-title")).Displayed,
            "The activated patient should reach the dashboard.");
    }

    [TestMethod]
    [TestCategory("TC-1.4.4-03")]
    public void TC02_RejectsIncorrectPasswordAfterActivation()
    {
        _logger.Info("Starting incorrect-password rejection test after account activation.");
        ActivateAccount();
        _page.Login(BaseUrl, Data.Email, "WrongPassword!2026");

        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='alert']")).Text
            .Contains("Inloggegevens onjuist", StringComparison.Ordinal));
        Assert.AreEqual($"{BaseUrl}/login", _driver.Url);
    }

    [TestMethod]
    [TestCategory("TC-1.4.4-04")]
    public void TC03_IssuesPatientRoleAfterActivation()
    {
        _logger.Info("Starting patient-role claim verification after activation.");
        ActivateAccount();
        _page.Login(BaseUrl, Data.Email, ValidPassword);

        string token = _page.GetLoginToken()
            ?? throw new InvalidOperationException("The login token was not stored.");
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;
        string? role = claims.FirstOrDefault(claim =>
            claim.Type == ClaimTypes.Role || claim.Type == "role")?.Value;

        Assert.AreEqual(RoleType.Patient.ToString(), role);
        Assert.AreEqual(RoleType.Patient.ToString(), Data.GetRoleName());
    }

    private ActivationTestData Data => _data
        ?? throw new InvalidOperationException("Activation test data was not created.");

    private void ReplaceTestData(RoleType role)
    {
        _data?.Dispose();
        _data = new ActivationTestData(role);
    }

    private void ActivateAccount()
    {
        _logger.Info("Opening the activation page and submitting a valid password.");
        _page.Open(BaseUrl, Data.Email, Data.RawToken);
        _page.SetPassword(ValidPassword, ValidPassword);
        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='status']")).Displayed);
        _logger.Info("Account activation succeeded.");
    }
}
