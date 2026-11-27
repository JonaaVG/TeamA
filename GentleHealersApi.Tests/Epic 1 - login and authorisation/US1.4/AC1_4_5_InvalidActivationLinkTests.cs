using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData.Infrastructure;
using OpenQA.Selenium;

namespace GentleHealersApi.Tests;

[TestClass]
public class AC1_4_5_InvalidActivationLinkTests : TestBase
{
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
    [TestCategory("TC-1.4.5-01")]
    public void TC01_RejectsActivationLinkWithInvalidToken()
    {
        _logger.Info("Starting rejection test for an invalid activation token.");
        _page.Open(BaseUrl, Data.Email, "tampered-token");
        AssertInvalidActivationLink();
    }

    [TestMethod]
    [TestCategory("TC-1.4.5-02")]
    public void TC02_RejectsActivationLinkWithExpiredToken()
    {
        _logger.Info("Starting rejection test for an expired activation link.");
        Data.ExpireToken();
        OpenActivationPage();
        AssertInvalidActivationLink();
    }

    [TestMethod]
    [TestCategory("TC-1.4.5-03")]
    public void TC03_RejectsActivationLinkWithPreviouslyUsedToken()
    {
        _logger.Info("Starting rejection test for a previously used activation link.");
        Data.MarkTokenUsed();
        OpenActivationPage();
        AssertInvalidActivationLink();
    }

    [TestMethod]
    [TestCategory("TC-1.4.5-04")]
    public void TC04_RejectsActivationLinkWithoutToken()
    {
        _logger.Info("Starting rejection test for an activation link without a token.");
        _page.Open(BaseUrl, Data.Email, includeToken: false);
        AssertInvalidActivationLink();
    }

    [TestMethod]
    [TestCategory("TC-1.4.5-05")]
    public void TC05_RejectsModifiedActivationLink()
    {
        _logger.Info("Starting rejection test for a modified activation link.");
        _page.Open(BaseUrl, Data.Email, Data.RawToken + "-modified");
        AssertInvalidActivationLink();
    }

    private ActivationTestData Data => _data
        ?? throw new InvalidOperationException("Activation test data was not created.");

    private void OpenActivationPage()
    {
        _logger.Info("Opening the account activation page with the test link.");
        _page.Open(BaseUrl, Data.Email, Data.RawToken);
    }

    private void AssertInvalidActivationLink()
    {
        Assert.IsTrue(_page.WaitFor(By.CssSelector("[role='alert']")).Text
            .Contains("niet geldig of verlopen", StringComparison.Ordinal));
        Assert.AreEqual(0, _driver.FindElements(By.Id("password")).Count,
            "An invalid activation link must not show the password form.");
        _logger.Info("Check passed: invalid activation link was rejected.");
    }
}
