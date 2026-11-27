using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData;
using GentleHealersApi.Tests.TestData.Factories;
using Microsoft.Extensions.Configuration;

namespace GentleHealersApi.Tests.Epic4.US_4_1;

/// <summary>Selenium system tests for selecting a patient and creating a referral.</summary>
[TestClass]
[TestCategory("Selenium")]
public sealed class US_4_1_PatientReferralSeleniumTests : TestBase
{
    private static string ConnectionString => TestConfiguration.Configuration
        .GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection ontbreekt in de Testing-configuratie.");

    private ReferralTestScenarioFactory _scenarioFactory = null!;
    private ReferralTestScenario _scenario = null!;
    private LoginPage _loginPage = null!;

    [TestInitialize]
    public void CreateScenarioAndSignIn()
    {
        _scenarioFactory = new ReferralTestScenarioFactory(ConnectionString);
        _loginPage = new LoginPage(_driver);
    }

    [TestMethod]
    [TestCategory("TC-1.1.1-01")]
    public void HuisartsKanPatientViaZoekbalkVindenEnDoorverwijzen()
    {
        _scenario = _scenarioFactory.Create(
            "Pieter",
            "Post",
            new DateOnly(1998, 4, 10));
        SignInAsPractitioner();

        var searchPage = new PatientSearchPage(_driver);
        searchPage.NavigateTo(BaseUrl);
        searchPage.SearchFor("Pieter Post");

        string result = searchPage.WaitForPatientByBsn(_scenario.Patient!.Bsn);
        Assert.IsTrue(result.Contains("Pieter Post", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains(_scenario.Patient.Bsn, StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("1998-04-10", StringComparison.Ordinal),
            "De geboortedatum van de gevonden patiënt hoort bij de zoekresultaten te staan.");

        searchPage.OpenReferralForPatient(_scenario.Patient.Bsn);
        CreateReferralForSelectedPatient();
    }

    [TestMethod]
    [TestCategory("TC-1.1.1-02")]
    public void HuisartsKanPatientUitDePatientenlijstSelecteren()
    {
        _scenario = _scenarioFactory.Create(
            "Pieter",
            "Post",
            new DateOnly(1990, 4, 29));
        SignInAsPractitioner();

        var searchPage = new PatientSearchPage(_driver);
        searchPage.NavigateTo(BaseUrl);

        string result = searchPage.WaitForPatientByBsn(_scenario.Patient!.Bsn);
        Assert.IsTrue(result.Contains("Pieter Post", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains(_scenario.Patient.Bsn, StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("1990-04-29", StringComparison.Ordinal));

        searchPage.OpenReferralForPatient(_scenario.Patient.Bsn);
        CreateReferralForSelectedPatient();
    }

    [TestMethod]
    [TestCategory("TC-1.1.1-03")]
    public void HuisartsKanPatientMetAlternatieveSchrijfwijzeVinden()
    {
        _scenario = _scenarioFactory.Create(
            "Sanne",
            "van der Berg",
            new DateOnly(1985, 6, 14));
        SignInAsPractitioner();

        var searchPage = new PatientSearchPage(_driver);
        searchPage.NavigateTo(BaseUrl);
        searchPage.SearchFor("Sanne Berg");

        string result = searchPage.WaitForPatientByBsn(_scenario.Patient!.Bsn);
        Assert.IsTrue(result.Contains("Sanne van der Berg", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("1985-06-14", StringComparison.Ordinal));

        searchPage.OpenReferralForPatient(_scenario.Patient.Bsn);
        CreateReferralForSelectedPatient();
    }

    [TestMethod]
    [TestCategory("TC-1.1.1-04")]
    public void HuisartsKanGeenNietBestaandePatientDoorverwijzen()
    {
        _scenario = _scenarioFactory.Create(
            "Bram",
            "van den Berg",
            new DateOnly(1988, 2, 12),
            createPatient: false);
        SignInAsPractitioner();

        var searchPage = new PatientSearchPage(_driver);
        searchPage.NavigateTo(BaseUrl);
        searchPage.SearchFor("Bram van den Berg");

        Assert.AreEqual("Geen patiënten gevonden.", searchPage.WaitForNoResults());
        Assert.IsFalse(searchPage.HasVisiblePatientNamed("Bram van den Berg"));
        Assert.AreEqual(0, _scenarioFactory.CountReferralsForPractitioner(_scenario.Practitioner.UserId),
            "Er mag geen doorverwijzing worden opgeslagen als de patiënt niet bestaat.");
    }

    private void SignInAsPractitioner()
    {
        _loginPage.Login(_scenario.Practitioner.Email, _scenario.Practitioner.Password);
        Assert.IsFalse(_loginPage.HasErrorMessage(), _loginPage.GetErrorMessage());
    }

    private void CreateReferralForSelectedPatient()
    {
        var referralPage = new PatientReferralPage(_driver);
        Assert.AreEqual("Doorverwijzing aanmaken", referralPage.WaitForTitle());
        Assert.AreEqual(_scenario.Patient!.PatientId, referralPage.GetSelectedPatientId(),
            "De patiënt uit de zoekresultaten moet in de doorverwijsroute geselecteerd zijn.");

        referralPage.AddTreatmentCode(_scenario.TreatmentCodeId);
        referralPage.SubmitReferral();

        int? referredPatientId = _scenarioFactory.FindReferralPatientId(
            _scenario.Patient.PatientId,
            _scenario.Practitioner.UserId,
            _scenario.TreatmentCodeId);

        Assert.AreEqual(_scenario.Patient.PatientId, referredPatientId,
            "De aangemaakte doorverwijzing moet aan de geselecteerde patiënt gekoppeld zijn.");
    }
}
