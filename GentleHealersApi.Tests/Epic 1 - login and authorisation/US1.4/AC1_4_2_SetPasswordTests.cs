using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData.Infrastructure;
using OpenQA.Selenium;

namespace GentleHealersApi.Tests;

[TestClass]
public class AC1_4_2_SetPasswordTests : TestBase
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
    [TestCategory("TC-1.4.2-01")]
    public void TC01_ActivationPageOpensWithValidLink()
    {
        OpenActivationPage();
        Assert.IsTrue(_page.WaitFor(By.Id("password")).Displayed,
            "A valid activation link should display the password form.");
    }

    [TestMethod]
    [TestCategory("TC-1.4.2-02")]
    public void TC02_SetsValidPassword()
    {
        OpenActivationPage();
        _page.SetPassword(ValidPassword, ValidPassword);

        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='status']")).Text
            .Contains("succesvol ingesteld", StringComparison.Ordinal));
        Assert.AreEqual("Active", Data.GetUserStatus());
    }

    [TestMethod]
    [TestCategory("TC-1.4.2-03")]
    public void TC03_RejectsInvalidPassword()
    {
        OpenActivationPage();
        _page.SetPassword("weak", "weak");

        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='alert']")).Text
            .Contains("minimaal 12 tekens", StringComparison.Ordinal));
        AssertPasswordWasNotAccepted();
    }

    [TestMethod]
    [TestCategory("TC-1.4.2-04")]
    public void TC04_RejectsMismatchedPasswords()
    {
        OpenActivationPage();
        _page.SetPassword(ValidPassword, "DifferentPassword!2026");

        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='alert']")).Text
            .Contains("komen niet overeen", StringComparison.Ordinal));
        AssertPasswordWasNotAccepted();
    }

    [TestMethod]
    [TestCategory("TC-1.4.2-05")]
    public void TC05_AcceptsValidActivationToken()
    {
        OpenActivationPage();

        Assert.IsTrue(_page.WaitFor(By.Id("password")).Displayed);
        Assert.AreEqual("Invited", Data.GetUserStatus());
    }

    [TestMethod]
    [TestCategory("TC-1.4.2-06")]
    public void TC06_RejectsExpiredActivationToken()
    {
        Data.ExpireToken();
        OpenActivationPage();
        AssertInvalidActivationLink();
    }

    [TestMethod]
    [TestCategory("TC-1.4.2-07")]
    public void TC07_RejectsPreviouslyUsedActivationToken()
    {
        Data.MarkTokenUsed();
        OpenActivationPage();
        AssertInvalidActivationLink();
    }

    private ActivationTestData Data => _data
        ?? throw new InvalidOperationException("Activation test data was not created.");

    private void OpenActivationPage() => _page.Open(BaseUrl, Data.Email, Data.RawToken);

    private void AssertInvalidActivationLink()
    {
        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='alert']")).Text
            .Contains("niet geldig of verlopen", StringComparison.Ordinal));
        Assert.AreEqual(0, _driver.FindElements(By.Id("password")).Count,
            "An invalid activation link must not show the password form.");
    }

    private void AssertPasswordWasNotAccepted()
    {
        Assert.AreEqual("Invited", Data.GetUserStatus());
        Assert.IsNull(Data.GetTokenUsedAt(), "A rejected password must not consume the activation token.");
    }
}
