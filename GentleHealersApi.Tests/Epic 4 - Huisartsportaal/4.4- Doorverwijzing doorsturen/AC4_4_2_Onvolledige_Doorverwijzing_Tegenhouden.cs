using System.Data;
using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData;
using GentleHealersApi.Tests.TestData.Factories;
using GentleHealersApi.Tests.TestData.Infrastructure;
using GentleHealersApi.Tests.TestData.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests;

[TestClass]
public class AC4_4_2_Onvolledige_Doorverwijzing_Tegenhouden : TestBase
{
    private static string ConnectionString =>
        TestConfiguration.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    private TestPracticeInfo _practice = null!;
    private TestUserAccount _gpAccount = null!;
    private TestUserAccount _patientAccount = null!;
    private int _patientId;
    private LoginPage _loginPage = null!;
    private WebDriverWait _wait = null!;

    [TestInitialize]
    public void SetupIncompleteReferralTest()
    {
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        _loginPage = new LoginPage(_driver);

        var practiceFactory = new TestPracticeFactory(ConnectionString);
        var userFactory = new TestUserFactory(ConnectionString);

        new RoleSeeder(ConnectionString).EnsureRoleExists(RoleType.GeneralPractitioner);
        new RoleSeeder(ConnectionString).EnsureRoleExists(RoleType.Patient);

        _practice = practiceFactory.CreatePractice();
        _gpAccount = userFactory.CreateGeneralPractitioner();
        LinkGpToPractice(_gpAccount.UserId, _practice.PracticeId);

        _patientAccount = userFactory.CreatePatient(_practice.PracticeId, _gpAccount.UserId);
        _patientId = GetPatientId(_patientAccount.UserId);
        EnsureTreatmentCodes();

        _loginPage.Login(_gpAccount.Email, _gpAccount.Password);
        OpenReferralPage(_patientId);
    }

    [TestCleanup]
    public void CleanupIncompleteReferralTest()
    {
        if (_patientId == 0)
            return;

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using (var contextCommand = new SqlCommand(
            "EXEC sys.sp_set_session_context @key = N'UserId', @value = @UserId;",
            connection))
        {
            contextCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = _gpAccount?.UserId ?? 0;
            contextCommand.ExecuteNonQuery();
        }

        using var command = new SqlCommand("""
            DELETE rt
            FROM dbo.ReferralTreatment rt
            INNER JOIN dbo.Referral r ON r.ReferralId = rt.ReferralId
            WHERE r.PatientId = @PatientId;

            DELETE FROM dbo.Referral WHERE PatientId = @PatientId;
            DELETE FROM dbo.Patient WHERE PatientId = @PatientId;
            DELETE FROM dbo.GeneralPractitioner WHERE PracticeId = @PracticeId;
            DELETE FROM dbo.GeneralPractitionerPractice WHERE PracticeId = @PracticeId;
            DELETE FROM dbo.AppUser WHERE UserId IN (@GpUserId, @PatientUserId);
            """, connection);
        command.Parameters.Add("@PatientId", SqlDbType.Int).Value = _patientId;
        command.Parameters.Add("@GpUserId", SqlDbType.Int).Value = _gpAccount?.UserId ?? 0;
        command.Parameters.Add("@PatientUserId", SqlDbType.Int).Value = _patientAccount?.UserId ?? 0;
        command.Parameters.Add("@PracticeId", SqlDbType.Int).Value = _practice?.PracticeId ?? 0;
        command.ExecuteNonQuery();
    }

    [TestMethod]
    [TestCategory("TC-4.4.2-01")]
    public void TC01_Meerdere_verplichte_velden_ontbreken()
    {
        ClickSubmit();

        var alert = GetVisibleAlert();
        Assert.IsNotNull(alert, "Er moet een foutmelding worden getoond wanneer verplichte velden ontbreken.");
        StringAssert.Contains(alert.Text, "behandelcode");
        Assert.IsTrue(ReferralCount() == 0, "Er mag geen doorverwijzing worden opgeslagen.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.2-02")]
    public void TC02_Geen_patient_geselecteerd()
    {
        OpenReferralPage(0);
        AddReferral("Geen patiënt geselecteerd");
        ClickSubmit();

        var alert = GetVisibleAlert();
        Assert.IsNotNull(alert, "Er moet een foutmelding verschijnen bij een ontbrekende patiënt.");
        Assert.IsTrue(ReferralCount() == 0, "Een doorverwijzing zonder bestaande patiënt mag niet worden opgeslagen.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.2-03")]
    public void TC03_Geen_behandelcode_toegevoegd()
    {
        SetNotes("Geen behandelcode gekozen");
        ClickSubmit();

        var alert = GetVisibleAlert();
        Assert.IsNotNull(alert, "Er moet een foutmelding worden getoond als geen behandelcode is toegevoegd.");
        Assert.IsTrue(ReferralCount() == 0, "Er mag geen doorverwijzing zonder behandelcode worden opgeslagen.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.2-04")]
    public void TC04_Geen_tijdsduur_ingevuld()
    {
        AddReferral("Tijdsduur ontbreekt");
        EditPendingReferral();
        SetDuration("");
        SaveReferralEntry();
        ClickSubmit();

        var alert = GetVisibleAlert();
        Assert.IsNotNull(alert, "Een ontbrekende tijdsduur moet worden tegengehouden.");
        Assert.IsTrue(ReferralCount() == 0, "Er mag geen doorverwijzing zonder tijdsduur worden opgeslagen.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.2-05")]
    public void TC05_Ongeldige_tijdsduur()
    {
        AddReferral("Ongeldige tijdsduur");
        EditPendingReferral();
        SetDuration("20");
        SaveReferralEntry();
        ClickSubmit();

        var alert = GetVisibleAlert();
        Assert.IsNotNull(alert, "Een ongeldige tijdsduur moet worden tegengehouden.");
        Assert.IsTrue(ReferralCount() == 0, "Er mag geen doorverwijzing met een ongeldige duur worden opgeslagen.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.2-06")]
    public void TC06_Veld_alleen_met_spaties()
    {
        SetNotes("   ");
        AddReferral(null);

        var pendingRows = _driver.FindElements(By.CssSelector("tbody tr")).Count;
        Assert.IsTrue(pendingRows == 1, "Een spatieveld moet worden behandeld als een lege optionele toelichting.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.2-07")]
    public void TC07_Gegevens_blijven_behouden_na_foutmelding()
    {
        SetNotes("Deze gegevens moeten behouden blijven");
        SetDuration("30");
        ClickSubmit();

        Assert.IsNotNull(GetVisibleAlert(), "Na de foutmelding moet een melding zichtbaar blijven.");
        Assert.AreEqual("Deze gegevens moeten behouden blijven",
            _driver.FindElement(By.Id("specialist-notes")).GetAttribute("value"));
        Assert.AreEqual("30", _driver.FindElement(By.Id("appointment-duration")).GetAttribute("value"));
    }

    private void OpenReferralPage(int patientId)
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/patient/{patientId}/doorverwijzen");
        _wait.Until(d =>
        {
            var select = d.FindElements(By.Id("treatment-code")).FirstOrDefault(e => e.Displayed);
            return select is not null && select.FindElements(By.TagName("option")).Count > 1;
        });
    }

    private void AddReferral(string? notes)
    {
        var codeId = GetTreatmentCodes().First();
        var codeSelect = _wait.Until(d => d.FindElement(By.Id("treatment-code")));
        ((IJavaScriptExecutor)_driver).ExecuteScript("""
            const select = arguments[0];
            const value = arguments[1];
            const option = Array.from(select.options).find(item => item.value === value);
            if (!option) throw new Error('Behandelcode-optie niet gevonden: ' + value);
            option.selected = true;
            select.dispatchEvent(new Event('input', { bubbles: true }));
            select.dispatchEvent(new Event('change', { bubbles: true }));
            """, codeSelect, codeId.ToString());

        if (notes is not null)
            SetNotes(notes);

        _driver.FindElement(By.CssSelector("form button[type='submit']")).Click();
    }

    private void EditPendingReferral()
    {
        var row = _wait.Until(d => d.FindElements(By.CssSelector("tbody tr")).FirstOrDefault(e => e.Displayed));
        Assert.IsNotNull(row, "Er moet een pending doorverwijzingsregel bestaan.");
        row.Click();
    }

    private void SaveReferralEntry()
    {
        var saveButton = _wait.Until(d => d.FindElements(By.CssSelector("form button[type='submit']"))
            .FirstOrDefault(e => e.Displayed));
        Assert.IsNotNull(saveButton, "De opslagknop voor de regel is niet gevonden.");
        saveButton.Click();
    }

    private void SetNotes(string notes)
    {
        var notesField = _wait.Until(d => d.FindElement(By.Id("specialist-notes")));
        notesField.Clear();
        notesField.SendKeys(notes);
    }

    private void SetDuration(string duration)
    {
        var durationSelect = _wait.Until(d => d.FindElement(By.Id("appointment-duration")));
        ((IJavaScriptExecutor)_driver).ExecuteScript("""
            const select = arguments[0];
            const duration = arguments[1];
            if (duration && !Array.from(select.options).some(item => item.value === duration)) {
                select.add(new Option(duration + ' min', duration));
            }
            select.value = duration;
            select.dispatchEvent(new Event('input', { bubbles: true }));
            select.dispatchEvent(new Event('change', { bubbles: true }));
            """, durationSelect, duration);
    }

    private void ClickSubmit()
    {
        var submit = _wait.Until(d => d.FindElements(By.XPath("//button[normalize-space()='Aanmaken']"))
            .FirstOrDefault(e => e.Displayed));
        Assert.IsNotNull(submit, "De knop Aanmaken is niet gevonden.");
        submit.Click();
    }

    private IWebElement? GetVisibleAlert()
    {
        return _wait.Until(d => d.FindElements(By.CssSelector("[role='alert']"))
            .FirstOrDefault(e => e.Displayed));
    }

    private void LinkGpToPractice(int userId, int practiceId)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand("""
            INSERT INTO dbo.GeneralPractitioner (UserId, PracticeId)
            VALUES (@UserId, @PracticeId);
            """, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@PracticeId", SqlDbType.Int).Value = practiceId;
        command.ExecuteNonQuery();
    }

    private int GetPatientId(int userId)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using (var contextCommand = new SqlCommand(
            "EXEC sys.sp_set_session_context @key = N'UserId', @value = @ActingUserId;",
            connection))
        {
            contextCommand.Parameters.Add("@ActingUserId", SqlDbType.Int).Value = _gpAccount.UserId;
            contextCommand.ExecuteNonQuery();
        }

        using var command = new SqlCommand("SELECT PatientId FROM dbo.Patient WHERE UserId = @UserId;", connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private void EnsureTreatmentCodes()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var countCommand = new SqlCommand(
            "SELECT COUNT(1) FROM dbo.TreatmentCode WHERE IsActive = 1;", connection);
        if (Convert.ToInt32(countCommand.ExecuteScalar()) >= 2)
            return;

        using var specialtyCommand = new SqlCommand("""
            IF NOT EXISTS (SELECT 1 FROM dbo.Specialty WHERE Name = @Name)
                INSERT INTO dbo.Specialty (Name) VALUES (@Name);
            SELECT SpecialtyId FROM dbo.Specialty WHERE Name = @Name;
            """, connection);
        specialtyCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = "Test specialisme 4.4.2";
        int specialtyId = Convert.ToInt32(specialtyCommand.ExecuteScalar());

        using var insertCommand = new SqlCommand("""
            IF NOT EXISTS (SELECT 1 FROM dbo.TreatmentCode WHERE CareCode = @CareCode1)
                INSERT INTO dbo.TreatmentCode
                    (CareCode, Description, SpecialtyId, DurationQuarters, Cost, PreparationNote, IsActive)
                VALUES (@CareCode1, @Description1, @SpecialtyId, 4, 125.50, NULL, 1);

            IF NOT EXISTS (SELECT 1 FROM dbo.TreatmentCode WHERE CareCode = @CareCode2)
                INSERT INTO dbo.TreatmentCode
                    (CareCode, Description, SpecialtyId, DurationQuarters, Cost, PreparationNote, IsActive)
                VALUES (@CareCode2, @Description2, @SpecialtyId, 4, 150.00, NULL, 1);
            """, connection);
        insertCommand.Parameters.Add("@CareCode1", SqlDbType.NVarChar, 50).Value = "TC442A";
        insertCommand.Parameters.Add("@Description1", SqlDbType.NVarChar, 255).Value = "Testbehandelcode één";
        insertCommand.Parameters.Add("@CareCode2", SqlDbType.NVarChar, 50).Value = "TC442B";
        insertCommand.Parameters.Add("@Description2", SqlDbType.NVarChar, 255).Value = "Testbehandelcode twee";
        insertCommand.Parameters.Add("@SpecialtyId", SqlDbType.Int).Value = specialtyId;
        insertCommand.ExecuteNonQuery();
    }

    private List<int> GetTreatmentCodes()
    {
        var ids = new List<int>();
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand("""
            SELECT TOP 2 TreatmentCodeId
            FROM dbo.TreatmentCode
            WHERE IsActive = 1
            ORDER BY CareCode;
            """, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
            ids.Add(reader.GetInt32(0));
        return ids;
    }

    private int ReferralCount()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand(
            "SELECT COUNT(1) FROM dbo.Referral WHERE PatientId = @PatientId;", connection);
        command.Parameters.Add("@PatientId", SqlDbType.Int).Value = _patientId;
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
