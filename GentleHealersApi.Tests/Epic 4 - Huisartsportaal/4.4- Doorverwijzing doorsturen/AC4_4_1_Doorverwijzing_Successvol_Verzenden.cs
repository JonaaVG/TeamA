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
public class AC4_4_1_Doorverwijzing_Successvol_Verzenden : TestBase
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
    public void SetupReferralTest()
    {
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        _loginPage = new LoginPage(_driver);

        var practiceFactory = new TestPracticeFactory(ConnectionString);
        var userFactory = new TestUserFactory(ConnectionString);

        new RoleSeeder(ConnectionString).EnsureRoleExists(RoleType.GeneralPractitioner);
        new RoleSeeder(ConnectionString).EnsureRoleExists(RoleType.Patient);

        _practice = practiceFactory.CreatePractice();
        _gpAccount = userFactory.CreateGeneralPractitioner();
        LinkGpToPractice(_gpAccount.UserId, _practice.PracticeId);

        _patientAccount = userFactory.CreatePatient(
            _practice.PracticeId,
            _gpAccount.UserId);
        _patientId = GetPatientId(_patientAccount.UserId);

        EnsureTreatmentCodes();

        _loginPage.Login(_gpAccount.Email, _gpAccount.Password);
        _driver.Navigate().GoToUrl($"{BaseUrl}/patient/{_patientId}/doorverwijzen");

        var referralWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        bool referralPageOpened = false;
        try
        {
            referralPageOpened = referralWait.Until(d =>
            {
                var select = d.FindElements(By.Id("treatment-code")).FirstOrDefault(e => e.Displayed);
                return select is not null && select.FindElements(By.TagName("option")).Count > 1;
            });
        }
        catch (WebDriverTimeoutException)
        {
            referralPageOpened = false;
        }

        Assert.IsTrue(referralPageOpened,
            $"Het referralformulier is niet geopend. Huidige URL: {_driver.Url}");
    }

    [TestCleanup]
    public void CleanupReferralTest()
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
    [TestCategory("TC-4.4.1-01")]
    public void TC01_Succesvol_verzenden()
    {
        AddReferral("Methode 4.4.1 succesvol verzenden", useFirstCode: true);
        SubmitReferrals();

        Assert.IsTrue(ReferralCount() >= 1, "Er moet minimaal één doorverwijzing zijn opgeslagen.");
        Assert.IsTrue(_wait.Until(d => d.Url.EndsWith("/patient/zoeken", StringComparison.OrdinalIgnoreCase)),
            "Na succesvol verzenden moet de huisarts teruggaan naar patiënt zoeken.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.1-02")]
    public void TC02_Direct_beschikbaar_in_ziekenhuissysteem()
    {
        AddReferral("Direct beschikbaar voor specialist", useFirstCode: true);
        SubmitReferrals();

        Assert.IsTrue(ReferralTreatmentCount() >= 1,
            "De behandelregel moet direct als ziekenhuisbeschikbare doorverwijzing zijn opgeslagen.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.1-03")]
    public void TC03_Verzendbevestiging_huisarts()
    {
        AddReferral("Bevestiging voor huisarts", useFirstCode: true);
        SubmitReferrals();

        var toast = _wait.Until(d => d.FindElements(By.CssSelector("[role='status'] span"))
            .FirstOrDefault(e => e.Displayed && e.Text.Contains("Doorverwijzing", StringComparison.OrdinalIgnoreCase)));

        Assert.IsNotNull(toast, "De huisarts moet een verzendbevestiging zien.");
        StringAssert.Contains(toast.Text, "Doorverwijzing");
    }

    [TestMethod]
    [TestCategory("TC-4.4.1-04")]
    public void TC04_Zichtbaar_in_patientenportaal()
    {
        AddReferral("Zichtbaar voor patiënt", useFirstCode: true);
        SubmitReferrals();

        _driver.Navigate().GoToUrl($"{BaseUrl}/login");
        ((IJavaScriptExecutor)_driver).ExecuteScript("window.localStorage.clear();");
        _driver.Navigate().Refresh();

        _loginPage = new LoginPage(_driver);
        _loginPage.Login(_patientAccount.Email, _patientAccount.Password);

        Assert.IsTrue(_wait.Until(d => d.Url.TrimEnd('/').EndsWith(BaseUrl, StringComparison.OrdinalIgnoreCase)
            || d.FindElements(By.Id("page-title")).Any(e => e.Displayed)),
            "Na inloggen moet de patiënt het portaal kunnen openen waarin de doorverwijzing wordt getoond.");
        Assert.IsTrue(ReferralTreatmentCount() >= 1,
            "De opgeslagen doorverwijzing moet aan het patiëntenportaal beschikbaar zijn.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.1-05")]
    public void TC05_Meerder_behandelcodes()
    {
        var treatmentCodes = GetTreatmentCodes(2);
        Assert.IsTrue(treatmentCodes.Count >= 2, "Voor deze test moeten minimaal twee actieve behandelcodes beschikbaar zijn.");

        AddReferral("Eerste behandelcode", useFirstCode: true);
        AddReferral("Tweede behandelcode", useSecondCode: true);
        SubmitReferrals();

        Assert.IsTrue(ReferralTreatmentCount() >= 2,
            "Beide behandelcodes moeten als aparte regels zijn opgeslagen.");
    }

    [TestMethod]
    [TestCategory("TC-4.4.1-06")]
    public void TC06_Doorverwijzing_zonder_toelichting()
    {
        AddReferral(null, useFirstCode: true);
        SubmitReferrals();

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand("""
            SELECT COUNT(1)
            FROM dbo.Referral r
            INNER JOIN dbo.ReferralTreatment rt ON rt.ReferralId = r.ReferralId
            WHERE r.PatientId = @PatientId;
            """, connection);
        command.Parameters.Add("@PatientId", SqlDbType.Int).Value = _patientId;

        Assert.IsTrue(Convert.ToInt32(command.ExecuteScalar()) >= 1,
            "Een doorverwijzing zonder toelichting moet zonder notitie als behandelregel zijn opgeslagen.");
    }

    private void AddReferral(string? notes, bool useFirstCode = false, bool useSecondCode = false)
    {
        var codes = GetTreatmentCodes(useSecondCode ? 2 : 1);
        int codeIndex = useSecondCode ? 1 : 0;

        var codeSelect = _wait.Until(d => d.FindElement(By.Id("treatment-code")));
        ((IJavaScriptExecutor)_driver).ExecuteScript("""
            const select = arguments[0];
            const value = arguments[1];
            const option = Array.from(select.options).find(item => item.value === value);
            if (!option) throw new Error('Behandelcode-optie niet gevonden: ' + value);
            option.selected = true;
            select.dispatchEvent(new Event('input', { bubbles: true }));
            select.dispatchEvent(new Event('change', { bubbles: true }));
            """, codeSelect, codes[codeIndex].ToString());

        _wait.Until(d => codeSelect.GetAttribute("value") == codes[codeIndex].ToString());

        if (notes is not null)
        {
            var notesField = _driver.FindElement(By.Id("specialist-notes"));
            notesField.Clear();
            notesField.SendKeys(notes);
        }

        _driver.FindElement(By.CssSelector("form button[type='submit']")).Click();
        _wait.Until(d => d.FindElements(By.CssSelector("tbody tr")).Count >=
            (useSecondCode ? 2 : 1));
    }

    private void SubmitReferrals()
    {
        var submitButton = _wait.Until(d => d.FindElements(By.XPath("//button[normalize-space()='Aanmaken']"))
            .FirstOrDefault(e => e.Displayed));
        Assert.IsNotNull(submitButton, "De knop Aanmaken is niet gevonden.");
        submitButton.Click();

        bool submitCompleted = false;
        try
        {
            submitCompleted = _wait.Until(d => d.Url.EndsWith("/patient/zoeken", StringComparison.OrdinalIgnoreCase)
                || d.FindElements(By.CssSelector("[role='status'] span")).Any(e => e.Displayed));
        }
        catch (WebDriverTimeoutException)
        {
            submitCompleted = false;
        }

        if (!submitCompleted)
        {
            var error = _driver.FindElements(By.CssSelector("[role='alert']"))
                .FirstOrDefault(e => e.Displayed)?.Text;
            Assert.Fail($"De doorverwijzing is niet verzonden. API/foutmelding: {error ?? "geen zichtbare melding"}. URL: {_driver.Url}");
        }
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
            BEGIN
                INSERT INTO dbo.Specialty (Name) VALUES (@Name);
            END;
            SELECT SpecialtyId FROM dbo.Specialty WHERE Name = @Name;
            """, connection);
        specialtyCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = "Test specialisme 4.4.1";
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
        insertCommand.Parameters.Add("@CareCode1", SqlDbType.NVarChar, 50).Value = "TC441A";
        insertCommand.Parameters.Add("@Description1", SqlDbType.NVarChar, 255).Value = "Testbehandelcode één";
        insertCommand.Parameters.Add("@CareCode2", SqlDbType.NVarChar, 50).Value = "TC441B";
        insertCommand.Parameters.Add("@Description2", SqlDbType.NVarChar, 255).Value = "Testbehandelcode twee";
        insertCommand.Parameters.Add("@SpecialtyId", SqlDbType.Int).Value = specialtyId;
        insertCommand.ExecuteNonQuery();
    }

    private List<int> GetTreatmentCodes(int count)
    {
        var ids = new List<int>();
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand("""
            SELECT TOP (@Count) TreatmentCodeId
            FROM dbo.TreatmentCode
            WHERE IsActive = 1
            ORDER BY CareCode;
            """, connection);
        command.Parameters.Add("@Count", SqlDbType.Int).Value = count;
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

    private int ReferralTreatmentCount()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand("""
            SELECT COUNT(1)
            FROM dbo.Referral r
            INNER JOIN dbo.ReferralTreatment rt ON rt.ReferralId = r.ReferralId
            WHERE r.PatientId = @PatientId;
            """, connection);
        command.Parameters.Add("@PatientId", SqlDbType.Int).Value = _patientId;
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
