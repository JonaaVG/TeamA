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
        ActivateAccount();
        Assert.IsNotNull(Data.GetTokenUsedAt(), "The activation token must be marked as used.");
    }

    [TestMethod]
    [TestCategory("TC-1.4.3-03")]
    public void TC03_SetsAccountStatusToActive()
    {
        ActivateAccount();
        Assert.AreEqual("Active", Data.GetUserStatus());
    }

    [TestMethod]
    [TestCategory("TC-1.4.3-04")]
    public void TC04_DoesNotActivateAccountWithInvalidPassword()
    {
        OpenActivationPage();
        _page.SetPassword("weak", "weak");
        AssertAccountRemainsInvited();
    }

    [TestMethod]
    [TestCategory("TC-1.4.3-05")]
    public void TC05_DoesNotActivateAccountWithMismatchedPasswords()
    {
        OpenActivationPage();
        _page.SetPassword(ValidPassword, "DifferentPassword!2026");
        AssertAccountRemainsInvited();
    }

    private ActivationTestData Data => _data
        ?? throw new InvalidOperationException("Activation test data was not created.");

    private void OpenActivationPage() => _page.Open(BaseUrl, Data.Email, Data.RawToken);

    private void ActivateAccount()
    {
        OpenActivationPage();
        _page.SetPassword(ValidPassword, ValidPassword);
        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='status']")).Displayed);
    }

    private void AssertAccountRemainsInvited()
    {
        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='alert']")).Displayed);
        Assert.AreEqual("Invited", Data.GetUserStatus());
        Assert.IsNull(Data.GetTokenUsedAt(), "A rejected password must not consume the activation token.");
    }
}
