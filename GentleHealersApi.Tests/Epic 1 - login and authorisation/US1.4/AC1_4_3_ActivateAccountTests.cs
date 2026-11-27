using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData.Infrastructure;
using OpenQA.Selenium;

namespace GentleHealersApi.Tests;

[TestClass]
public class AC1_4_3_ActivateAccountTests : TestBase
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
    [TestCategory("TC-1.4.3-01")]
    public void TC01_ActivatesAccountAndLogsInWithNewPassword()
    {
        _logger.Info("Starting account activation and subsequent login test.");
        ActivateAccount();
        _page.Login(BaseUrl, Data.Email, ValidPassword);

        Assert.IsTrue(_page.WaitFor(By.Id("page-title")).Displayed,
            "The activated user should be able to log in with the newly set password.");
        Assert.AreEqual("Active", Data.GetUserStatus());
    }

    [TestMethod]
    [TestCategory("TC-1.4.3-02")]
    public void TC02_MarksActivationTokenAsUsed()
    {
        _logger.Info("Starting activation-token consumption test.");
        ActivateAccount();
        Assert.IsNotNull(Data.GetTokenUsedAt(), "The activation token must be marked as used.");
    }

    [TestMethod]
    [TestCategory("TC-1.4.3-03")]
    public void TC03_SetsAccountStatusToActive()
    {
        _logger.Info("Starting account status activation test.");
        ActivateAccount();
        Assert.AreEqual("Active", Data.GetUserStatus());
    }

    [TestMethod]
    [TestCategory("TC-1.4.3-04")]
    public void TC04_DoesNotActivateAccountWithInvalidPassword()
    {
        _logger.Info("Starting check that an invalid password cannot activate an account.");
        OpenActivationPage();
        _page.SetPassword("weak", "weak");
        AssertAccountRemainsInvited();
    }

    [TestMethod]
    [TestCategory("TC-1.4.3-05")]
    public void TC05_DoesNotActivateAccountWithMismatchedPasswords()
    {
        _logger.Info("Starting check that mismatched passwords cannot activate an account.");
        OpenActivationPage();
        _page.SetPassword(ValidPassword, "DifferentPassword!2026");
        AssertAccountRemainsInvited();
    }

    private ActivationTestData Data => _data
        ?? throw new InvalidOperationException("Activation test data was not created.");

    private void OpenActivationPage()
    {
        _logger.Info("Opening the account activation page.");
        _page.Open(BaseUrl, Data.Email, Data.RawToken);
    }

    private void ActivateAccount()
    {
        OpenActivationPage();
        _logger.Info("Submitting a valid password to activate the account.");
        _page.SetPassword(ValidPassword, ValidPassword);
        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='status']")).Displayed);
        _logger.Info("Account activation succeeded.");
    }

    private void AssertAccountRemainsInvited()
    {
        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='alert']")).Displayed);
        Assert.AreEqual("Invited", Data.GetUserStatus());
        Assert.IsNull(Data.GetTokenUsedAt(), "A rejected password must not consume the activation token.");
        _logger.Info("Check passed: invalid password left the account invited and token unused.");
    }
}
