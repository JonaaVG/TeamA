using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData;
using GentleHealersApi.Tests.TestData.Factories;
using Microsoft.Extensions.Configuration;

namespace GentleHealersApi.Tests.Epic4.US_4_1;

/// <summary>Selenium system tests for patient visibility across GP practices.</summary>
[TestClass]
[TestCategory("Selenium")]
public sealed class US_4_1_PracticeScopeSeleniumTests : TestBase
{
    private static string ConnectionString => TestConfiguration.Configuration
        .GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection ontbreekt in de Testing-configuratie.");

    private ReferralTestScenarioFactory _scenarioFactory = null!;
    private ReferralTestScenario _scenario = null!;
    private LoginPage _loginPage = null!;

    [TestInitialize]
    public void CreateScenario()
    {
        _scenarioFactory = new ReferralTestScenarioFactory(ConnectionString);
        _loginPage = new LoginPage(_driver);
    }

    [TestMethod]
    [TestCategory("TC-1.1.1-01-Eigen-praktijk")]
    public void HuisartsKanPatientUitEigenPraktijkDoorverwijzen()
    {
        _scenario = _scenarioFactory.Create(
            "Pieter",
            "Post",
            new DateOnly(1990, 4, 29));
        SignInAsPractitioner();

        var patient = _scenario.Patient!;
        Assert.AreEqual(_scenario.Practice.PracticeId, patient.PracticeId,
            "De testpatiënt hoort bij de eigen praktijk van de huisarts.");

        var searchPage = new PatientSearchPage(_driver);
        searchPage.NavigateTo(BaseUrl);
        searchPage.SearchFor("Pieter Post");

        string result = searchPage.WaitForPatientByBsn(patient.Bsn);
        Assert.IsTrue(result.Contains("Pieter Post", StringComparison.Ordinal));
        Assert.IsTrue(result.Contains("1990-04-29", StringComparison.Ordinal));

        searchPage.OpenReferralForPatient(patient.Bsn);
        var referralPage = new PatientReferralPage(_driver);
        Assert.AreEqual("Doorverwijzing aanmaken", referralPage.WaitForTitle());
        Assert.AreEqual(patient.PatientId, referralPage.GetSelectedPatientId());
        referralPage.AddTreatmentCode(_scenario.TreatmentCodeId);
        referralPage.SubmitReferral();

        Assert.AreEqual(
            patient.PatientId,
            _scenarioFactory.FindReferralPatientId(
                patient.PatientId,
                _scenario.Practitioner.UserId,
                _scenario.TreatmentCodeId),
            "Een doorverwijzing voor een patiënt van de eigen praktijk moet worden opgeslagen.");
    }

    [TestMethod]
    [TestCategory("TC-1.1.1-02-Andere-praktijk")]
    public void HuisartsZietPatientUitAnderePraktijkNietInZoekresultaten()
    {
        _scenario = CreateForeignPatientScenario();
        SignInAsPractitioner();

        var searchPage = new PatientSearchPage(_driver);
        searchPage.NavigateTo(BaseUrl);
        searchPage.SearchFor("Rina Silke");

        Assert.AreEqual("Geen patiënten gevonden.", searchPage.WaitForNoResults());
        Assert.IsFalse(searchPage.HasVisiblePatientNamed("Rina Silke"));
        Assert.AreNotEqual(
            _scenario.Practice.PracticeId,
            _scenario.Patient!.PracticeId,
            "De testpatiënt moet aan een andere praktijk gekoppeld zijn.");
    }

    [TestMethod]
    [TestCategory("TC-1.1.1-03-Andere-praktijk-weigeren")]
    public void HuisartsKanPatientUitAnderePraktijkNietDoorverwijzenViaDirecteLink()
    {
        _scenario = CreateForeignPatientScenario();
        SignInAsPractitioner();

        var referralPage = new PatientReferralPage(_driver);
        referralPage.NavigateToPatientReferral(BaseUrl, _scenario.Patient!.PatientId);
        Assert.AreEqual(_scenario.Patient.PatientId, referralPage.GetSelectedPatientId());

        referralPage.AddTreatmentCode(_scenario.TreatmentCodeId);
        var outcome = referralPage.SubmitReferralAndWaitForOutcome();

        int referralCount = _scenarioFactory.CountReferralsForPractitioner(_scenario.Practitioner.UserId);
        Assert.AreEqual(0, referralCount,
            "Er mag geen doorverwijzing voor een patiënt van een andere praktijk worden opgeslagen.");
        Assert.IsFalse(outcome.Succeeded,
            "De verwijspoging voor een patiënt uit een andere praktijk mag niet slagen.");
        Assert.AreEqual("Onbekende patiënt.", outcome.ErrorMessage,
            "De API hoort de patiënt buiten de praktijk als niet beschikbaar te behandelen.");
    }

    private ReferralTestScenario CreateForeignPatientScenario() =>
        _scenarioFactory.CreatePatientAtOtherPractice(
            "Rina",
            "Silke",
            new DateOnly(1990, 4, 29),
            ownPracticeName: "Praktijk De Linde",
            otherPracticeName: "Praktijk De Roos");

    private void SignInAsPractitioner()
    {
        _loginPage.Login(_scenario.Practitioner.Email, _scenario.Practitioner.Password);
        Assert.IsFalse(_loginPage.HasErrorMessage(), _loginPage.GetErrorMessage());
    }
}
