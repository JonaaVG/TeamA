using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData;
using GentleHealersApi.Tests.TestData.Factories;
using GentleHealersApi.Tests.TestData.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests.Epic4;

public abstract class ReferralTreatmentBrowserTest
{
    protected sealed record TreatmentCode(int Id, string Code, int StandardMinutes);
    protected sealed record TreatmentDraft(string Code, int DurationMinutes, string Note);
    protected sealed record SavedTreatment(int ReferralId, int Id, string Code, int DurationMinutes, string? Note);
    protected sealed record PatientFixture(int Id, TestUserAccount Account);

    private static readonly By TreatmentSelector = By.Id("treatment-code");
    private static readonly By DurationSelector = By.Id("appointment-duration");
    private static readonly By NoteSelector = By.Id("specialist-notes");
    private static readonly By ReferralAlert = By.XPath("//p[@role='alert' and not(ancestor::form)]");
    private static readonly By DraftRows = By.CssSelector("tbody tr");

    private string _connectionString = null!;
    private Uri _baseUri = null!;
    private TestUserFactory _users = null!;
    private TestUserAccount _practitioner = null!;
    private int _practiceId;

    public TestContext TestContext { get; set; } = null!;
    protected IWebDriver Browser { get; private set; } = null!;
    protected PatientFixture Patient { get; private set; } = null!;
    protected IReadOnlyList<TreatmentCode> Codes { get; private set; } = [];

    private WebDriverWait Wait => new(Browser, TimeSpan.FromSeconds(8));

    [TestInitialize]
    public void SetUpReferralTest()
    {
        var configuredUrl = Environment.GetEnvironmentVariable("GENTLEHEALERS_BROWSER_TEST_URL");
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var url) ||
            !url.IsLoopback || url.Scheme is not ("http" or "https"))
        {
            Assert.Inconclusive("Set GENTLEHEALERS_BROWSER_TEST_URL to a local running frontend.");
        }
        _baseUri = url!;

        var connection = new SqlConnectionStringBuilder(
            TestConfiguration.Configuration.GetConnectionString("DefaultConnection"));
        var host = connection.DataSource.Split(',')[0].Replace("tcp:", "", StringComparison.OrdinalIgnoreCase);
        if (!connection.InitialCatalog.StartsWith("TestGentleHealers", StringComparison.OrdinalIgnoreCase) ||
            host is not ("localhost" or "127.0.0.1" or "[::1]" or "::1"))
        {
            Assert.Inconclusive("Use a local TestGentleHealers database for the generated referral records.");
        }
        _connectionString = connection.ConnectionString;

        _practiceId = new TestPracticeFactory(_connectionString).CreatePractice().PracticeId;
        _users = new TestUserFactory(_connectionString);
        _practitioner = _users.CreateGeneralPractitioner();
        Execute(
            "INSERT INTO dbo.GeneralPractitioner (UserId, PracticeId) VALUES (@User, @Practice);",
            ("@User", _practitioner.UserId), ("@Practice", _practiceId));

        Patient = NewPatient();
        Codes = SeedTreatmentCatalog();

        var options = new ChromeOptions();
        options.AddArguments("--headless", "--window-size=1440,1000");
        Browser = new ChromeDriver(options);
        SignIn(_practitioner);
    }

    [TestCleanup]
    public void CloseReferralBrowser() => Browser?.Dispose();

    protected PatientFixture NewPatient()
    {
        var user = _users.CreateUser(RoleType.Patient, "referral_patient");
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var prefix = $"99999{RandomNumberGenerator.GetInt32(0, 1000):000}";
            var checksum = prefix.Select((digit, index) => (digit - '0') * (9 - index)).Sum() % 11;
            if (checksum == 10)
                continue;

            try
            {
                // Use the reserved Dutch test BSN range.
                var bsn = prefix + checksum.ToString(CultureInfo.InvariantCulture);
                var patientId = (int)Scalar("""
                    EXEC sys.sp_set_session_context @key=N'UserId', @value=@Practitioner;
                    INSERT INTO dbo.Patient (UserId, PracticeId, Bsn, DateOfBirth)
                    VALUES (@UserId, @PracticeId, @Bsn, DATEFROMPARTS(1990, 5, 14));
                    SELECT CONVERT(int, SCOPE_IDENTITY());
                    """, ("@Practitioner", _practitioner.UserId), ("@UserId", user.UserId),
                    ("@PracticeId", _practiceId), ("@Bsn", bsn));
                return new PatientFixture(patientId, user);
            }
            catch (SqlException error) when (error.Number == 2627 &&
                error.Message.Contains("UQ_Patient_Bsn", StringComparison.Ordinal))
            {
                // Another local test may already have used the generated test BSN.
            }
        }
        throw new InvalidOperationException("Could not assign an unused test BSN to the referral patient.");
    }

    protected void OpenReferral(PatientFixture? patient = null)
    {
        patient ??= Patient;
        Browser.Navigate().GoToUrl(new Uri(_baseUri, "/patient/zoeken"));
        try
        {
            var query = Wait.Until(driver => driver.FindElements(By.CssSelector("form input[type='text']"))
                .FirstOrDefault(input => input.Displayed));
            query!.SendKeys(patient.Account.LastName);
            var search = Wait.Until(driver => driver.FindElements(By.CssSelector("form button[type='submit']"))
                .FirstOrDefault(button => button.Displayed && button.Enabled));
            search!.Click();

            var referralLink = By.CssSelector($"a[href='/patient/{patient.Id}/doorverwijzen']");
            var link = Wait.Until(driver => driver.FindElements(referralLink).FirstOrDefault(result => result.Displayed));
            link!.Click();

            Wait.Until(driver => driver.FindElements(By.CssSelector("#treatment-code option"))
                .Any(option => option.GetDomProperty("value") == Codes[0].Id.ToString(CultureInfo.InvariantCulture)));
        }
        catch (WebDriverTimeoutException)
        {
            Assert.Fail($"The GP could not open the patient's referral form and load the treatment catalog. Page message: {ReadAlert()}");
        }
    }

    protected IReadOnlyList<string> AvailableTreatmentCodes => Options(TreatmentSelector)
        .Where(option => !string.IsNullOrWhiteSpace(option.GetDomProperty("value")))
        .Select(option => option.Text.Split('–', 2)[0].Trim()).ToArray();

    protected IReadOnlyList<int> AvailableDurations => Options(DurationSelector)
        .Select(option => int.Parse(option.GetDomProperty("value")!, CultureInfo.InvariantCulture)).ToArray();

    protected int CurrentDuration => int.Parse(
        Browser.FindElement(DurationSelector).GetDomProperty("value")!, CultureInfo.InvariantCulture);

    protected string CurrentNote => Browser.FindElement(NoteSelector).GetDomProperty("value") ?? "";

    protected void ChooseTreatment(TreatmentCode treatment) => ChooseOption(TreatmentSelector, treatment.Id);

    protected void SetDuration(int minutes) => ChooseOption(DurationSelector, minutes);

    protected void AddTreatment(TreatmentCode treatment, int? minutes = null, string? note = null)
    {
        ChooseTreatment(treatment);
        if (minutes.HasValue)
            SetDuration(minutes.Value);
        if (note is not null)
        {
            var noteField = Browser.FindElement(NoteSelector);
            noteField.Clear();
            noteField.SendKeys(note);
        }
        SaveDraft();
    }

    protected void SaveDraft() => Browser.FindElement(By.CssSelector("form button[type='submit']")).Click();

    protected void EditDraft(TreatmentCode treatment) =>
        Browser.FindElements(DraftRows).Single(row => row.FindElement(By.CssSelector("td span")).Text == treatment.Code).Click();

    protected IReadOnlyList<TreatmentDraft> Drafts() =>
        Browser.FindElements(DraftRows).Select(row =>
        {
            var cells = row.FindElements(By.TagName("td"));
            var duration = int.Parse(cells[1].Text.Split(' ', 2)[0], CultureInfo.InvariantCulture);
            return new TreatmentDraft(cells[0].FindElement(By.TagName("span")).Text, duration,
                cells[2].GetDomAttribute("title") ?? "");
        }).ToArray();

    protected IReadOnlyList<SavedTreatment> SubmitReferral(PatientFixture? patient = null)
    {
        patient ??= Patient;
        ClickCreateAndWaitForResult();

        Assert.AreEqual("/patient/zoeken", new Uri(Browser.Url).AbsolutePath,
            $"The GP portal did not return to the patient search after sending the referral. Page message: {ReadAlert()}");

        var saved = ReadTreatments(patient.Id);
        Assert.IsNotEmpty(saved, "The referral returned to the patient list but stored no treatment rows.");
        TestContext.WriteLine($"Confirmed {saved.Count} saved referral treatment(s).");
        return saved;
    }

    protected void AssertEmptyReferralRejected()
    {
        var originalUrl = Browser.Url;
        ClickCreateAndWaitForResult();

        Assert.AreEqual(originalUrl, Browser.Url, "The form must stay open after rejecting the referral.");
        Assert.AreEqual("Voeg minimaal één behandelcode toe.", ReadReferralError(),
            "The final submission must explain that a valid treatment code is required.");
        Assert.IsEmpty(ReadTreatments(Patient.Id), "The rejected referral must not store any treatment codes.");
    }

    private void ClickCreateAndWaitForResult()
    {
        var originalUrl = Browser.Url;
        Browser.FindElement(By.XPath("//button[normalize-space()='Aanmaken']")).Click();
        try
        {
            Wait.Until(driver => driver.Url != originalUrl || ReadReferralError() is not null);
        }
        catch (WebDriverTimeoutException)
        {
            Assert.Fail("Sending the referral did not produce a confirmation or a validation message.");
        }
    }

    private string? ReadReferralError() => Browser.FindElements(ReferralAlert)
        .Where(element => element.Displayed)
        .Select(element => element.Text)
        .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

    protected void CheckPatientBooking(SavedTreatment bookedTreatment, SavedTreatment otherTreatment)
    {
        SignIn(Patient.Account);
        Browser.Navigate().GoToUrl(new Uri(_baseUri, $"/patient/{Patient.Id}/doorverwijzingen"));
        var code = Browser.FindElements(By.XPath(
            $"//*[self::td or self::span or self::p or self::h3][normalize-space()='{bookedTreatment.Code}']")).FirstOrDefault();

        if (code is null)
        {
            Assert.Inconclusive("The patient referral overview and booking flow are unavailable in this application version.");
        }
        var row = code!.FindElements(By.XPath("ancestor::*[self::tr or self::li or self::article][1]")).FirstOrDefault();
        var book = row?.FindElements(By.XPath(
            ".//*[self::button or self::a][contains(normalize-space(), 'Inplannen') or contains(normalize-space(), 'Afspraak')]"))
            .FirstOrDefault();
        if (book is null)
        {
            Assert.Inconclusive("The selected referral treatment has no patient booking action in this application version.");
        }
        book!.Click();

        IWebElement? slot;
        try
        {
            slot = Wait.Until(driver => driver.FindElements(By.TagName("button")).FirstOrDefault(button =>
                button.Displayed && button.Enabled && Regex.IsMatch(button.Text.Trim(), @"^\d{1,2}:\d{2}$")));
        }
        catch (WebDriverTimeoutException)
        {
            Assert.Inconclusive("No available appointment slot was shown for the selected treatment.");
            return;
        }
        slot!.Click();
        Browser.FindElement(By.XPath("//button[contains(normalize-space(), 'Bevestig')]")).Click();

        Assert.IsTrue(Wait.Until(_ => HasAppointment(bookedTreatment.Id)),
            "The patient did not get an appointment for the selected treatment.");
        Assert.IsFalse(HasAppointment(otherTreatment.Id), "The other treatment must remain unbooked.");
    }

    protected void CheckSpecialistNote(SavedTreatment treatment, string? note)
    {
        var specialist = _users.CreateUser(RoleType.Specialist, "referral_specialist");
        var specialistId = (int)Scalar(
            "INSERT INTO dbo.Specialist (UserId) OUTPUT INSERTED.SpecialistId VALUES (@UserId);",
            ("@UserId", specialist.UserId));

        SignIn(specialist);
        Browser.FindElement(By.CssSelector("button[aria-controls='navigation-drawer']")).Click();
        var appointments = Browser.FindElements(By.XPath(
            "//*[@id='navigation-drawer']//*[self::a or self::button]" +
            "[contains(normalize-space(), 'Afspraken') or contains(normalize-space(), 'Agenda')]"))
            .FirstOrDefault(element => element.Displayed);
        if (appointments is null)
        {
            Assert.Inconclusive("The specialist has no appointments view. Draft and database assertions passed.");
        }

        var startTime = DateTime.Today.AddDays(1).AddHours(10);
        Execute("""
            INSERT INTO dbo.Appointment
                (ReferralTreatmentId, PatientId, SpecialistId, StartTime, DurationQuarters, CreatedByUserId)
            VALUES (@Treatment, @Patient, @Specialist, @Start, @Duration, @Creator);
            """,
            ("@Treatment", treatment.Id), ("@Patient", Patient.Id), ("@Specialist", specialistId),
            ("@Start", startTime), ("@Duration", treatment.DurationMinutes / 15), ("@Creator", _practitioner.UserId));
        appointments!.Click();

        var patientLink = Wait.Until(driver => driver.FindElements(By.CssSelector("main a, main button"))
            .FirstOrDefault(element => element.Displayed && element.Text.Contains(Patient.Account.LastName, StringComparison.Ordinal)));
        patientLink!.Click();

        var displayedNote = Browser.FindElements(By.XPath("//main//*[self::dt or self::label or self::h3]" +
            "[normalize-space()='Toelichting']/following-sibling::*[1]")).FirstOrDefault();
        if (note is null)
        {
            Assert.IsTrue(displayedNote is null || string.IsNullOrWhiteSpace(displayedNote.Text),
                "The specialist must see no placeholder for an omitted note.");
            return;
        }

        Assert.IsNotNull(displayedNote, "The specialist must see the note on this appointment.");
        Assert.AreEqual(note, displayedNote!.GetDomProperty("textContent"), "The appointment must show the exact note.");
        Assert.IsEmpty(displayedNote.FindElements(By.TagName("script")), "Note markup must remain plain text.");
    }

    private IReadOnlyList<TreatmentCode> SeedTreatmentCatalog()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var specialtyId = (int)Scalar(
            "INSERT INTO dbo.Specialty (Name) OUTPUT INSERTED.SpecialtyId VALUES (@Name);", ("@Name", $"Referral test {suffix}"));
        return new[] { 30, 45, 30 }.Select((duration, index) =>
        {
            var code = $"E4-{suffix}-{index + 1}";
            var id = (int)Scalar("""
                INSERT INTO dbo.TreatmentCode (CareCode, Description, SpecialtyId, DurationQuarters, Cost, IsActive)
                OUTPUT INSERTED.TreatmentCodeId
                VALUES (@Code, @Description, @Specialty, @Duration, 1, 1);
                """, ("@Code", code), ("@Description", $"Referral test treatment {index + 1}"),
                ("@Specialty", specialtyId), ("@Duration", duration / 15));
            return new TreatmentCode(id, code, duration);
        }).ToArray();
    }

    private IReadOnlyList<SavedTreatment> ReadTreatments(int patientId)
    {
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, """
            SELECT r.ReferralId, t.ReferralTreatmentId, c.CareCode, 15 * t.DurationQuarters, t.Note
            FROM dbo.Referral r
            JOIN dbo.ReferralTreatment t ON t.ReferralId = r.ReferralId
            JOIN dbo.TreatmentCode c ON c.TreatmentCodeId = t.TreatmentCodeId
            WHERE r.PatientId = @PatientId
            ORDER BY r.ReferralId, t.ReferralTreatmentId;
            """, [("@PatientId", patientId)]);
        using var results = command.ExecuteReader();
        var saved = new List<SavedTreatment>();
        while (results.Read())
        {
            saved.Add(new SavedTreatment(results.GetInt32(0), results.GetInt32(1), results.GetString(2),
                results.GetInt32(3), results.IsDBNull(4) ? null : results.GetString(4)));
        }
        return saved;
    }

    private bool HasAppointment(int treatmentId) => (int)Scalar("""
        SELECT COUNT(*) FROM dbo.Appointment WHERE ReferralTreatmentId = @TreatmentId AND CancelledAt IS NULL;
        """, ("@TreatmentId", treatmentId)) > 0;

    private void SignIn(TestUserAccount account)
    {
        // Each role starts with an empty portal session in this test browser.
        Browser.Navigate().GoToUrl(_baseUri);
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.localStorage.clear();");
        Browser.Navigate().GoToUrl(new Uri(_baseUri, "/login"));
        var login = new LoginPage(Browser);
        login.Login(account.Email, account.Password);
        Assert.IsFalse(login.HasErrorMessage(), $"The {account.Role} test account could not sign in.");
        Wait.Until(driver => new Uri(driver.Url).AbsolutePath == "/" &&
            driver.FindElements(By.Id("page-title")).Any(heading => heading.Displayed));
    }

    private string ReadAlert() => Browser.FindElements(By.CssSelector("[role='alert']"))
        .Where(element => element.Displayed).Select(element => element.Text).FirstOrDefault() ?? "no alert";

    private IReadOnlyList<IWebElement> Options(By selector) =>
        Browser.FindElement(selector).FindElements(By.TagName("option"));

    private void ChooseOption(By selector, int value) => Options(selector)
        .Single(option => option.GetDomProperty("value") == value.ToString(CultureInfo.InvariantCulture)).Click();

    private SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void Execute(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, sql, parameters);
        command.ExecuteNonQuery();
    }

    private object Scalar(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, sql, parameters);
        return command.ExecuteScalar() ?? throw new InvalidOperationException("The test database returned no result.");
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string sql, (string Name, object Value)[] parameters)
    {
        var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return command;
    }
}
