using System.Data;
using GentleHealersApi.Tests.PageObjects;
using GentleHealersApi.Tests.TestData;
using GentleHealersApi.Tests.TestData.Factories;
using GentleHealersApi.Tests.TestData.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests
{
    /// <summary>
    /// User Story 9.1: As a general practitioner, I want to individually create patient accounts.
    /// AC 9.1.1: Enter patient details (elfproef + unique BSN)
    /// AC 9.1.2: Create patient account (account, practice link, invitation)
    ///
    /// Configuration is loaded primarily from testing.json (via TestConfiguration),
    /// with an optional fallback to environment variables.
    /// </summary>
    [TestClass]
    public class US_9_1_CreatePatientTest : TestBase
    {
        private const string NewPatientPath = "/patient/aanmaken";

        private static string ConnectionString =>
            TestConfiguration.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in appsettings.Testing.json.");


        private WebDriverWait _wait = null!;
        private LoginPage _loginPage = null!;
        private AddPatientPage _addPatientPage = null!;

        private TestPracticeFactory _practiceFactory = null!;
        private TestUserFactory _userFactory = null!;
        private TestPatientFactory _patientFactory = null!;

        private TestPracticeInfo _practice = null!;
        private TestUserAccount _gpAccount = null!;

        [TestInitialize]
        public void SetupTestAndLogin()
        {
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _loginPage = new LoginPage(_driver);
            _addPatientPage = new AddPatientPage(_driver);

            _patientFactory = new TestPatientFactory(ConnectionString);
            _userFactory = new TestUserFactory(ConnectionString, _patientFactory);
            _practiceFactory = new TestPracticeFactory(ConnectionString);

            // Create a practice and a GP account, then link them.
            _practice = _practiceFactory.CreatePractice();
            _gpAccount = _userFactory.CreateGeneralPractitioner();

            LinkGpToPractice(_gpAccount.UserId, _practice.PracticeId);

            _logger.Info(
                $"Test GP created: {_gpAccount.Email} " +
                $"(UserId={_gpAccount.UserId}, PracticeId={_practice.PracticeId})");

            // Log in through the LoginPage Page Object.
            _logger.Info($"Logging in as GP: {_gpAccount.Email}");
            _loginPage.Login(_gpAccount.Email, _gpAccount.Password);
            _logger.Info("Logged in as GP.");

            // Open the patient form through the AddPatientPage Page Object.
            _addPatientPage.NavigateTo(BaseUrl, NewPatientPath);
            _logger.Info("Opened Add Patient page.");
        }

        // ---------------------------------------------------------------
        // AC 9.1.1: Enter patient details
        // ---------------------------------------------------------------

        [TestMethod]
        [TestCategory("TC-9.1.1-01")]
        public void Invalid_BSN_is_rejected_by_elfproef()
        {
            var patient = NewPatient(bsn: "123456789"); // Fails the elfproef check.

            FillForm(patient);
            SubmitForm();

            WaitForErrorMessage("Het BSN is ongeldig volgens de elfproef.");

            Assert.IsFalse(PatientExists(patient.Email),
                "No patient should be created when the BSN is invalid.");

            _logger.Info("Check passed: patient was not created.");
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-02")]
        public void Duplicate_BSN_is_rejected()
        {
            var bsn = GenerateValidBsn();

            _logger.Info("Creating first patient (should succeed).");
            FillForm(NewPatient(bsn));
            SubmitForm();
            WaitForSuccessMessage("Patiëntaccount succesvol aangemaakt!");

            _logger.Info("Creating second patient with the same BSN (should fail).");
            var secondPatient = NewPatient(bsn);
            FillForm(secondPatient);
            SubmitForm();

            WaitForErrorMessage("Er bestaat al een patiënt met dit BSN.");

            Assert.IsFalse(PatientExists(secondPatient.Email),
                "The second patient with a duplicate BSN must not be created.");

            _logger.Info("Check passed: duplicate BSN was rejected.");
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-03")]
        public void Invalid_phone_number_is_rejected()
        {
            // The generated BSN is valid; only the phone number should fail validation.
            var patient = NewPatient(phone: "abcdefgh");

            FillForm(patient);
            SubmitForm();

            WaitForErrorMessage("Vul een geldig telefoonnummer in.");

            Assert.IsFalse(PatientExists(patient.Email),
                "No patient should be created with an invalid phone number.");

            _logger.Info("Check passed: patient was not created.");
        }

        // ---------------------------------------------------------------
        // AC 9.1.2: Create patient account
        // ---------------------------------------------------------------

        [TestMethod]
        [TestCategory("TC-9.1.2-01")]
        public void Valid_details_create_patient_account()
        {
            var patient = NewPatient();

            FillForm(patient);
            SubmitForm();

            WaitForSuccessMessage("Patiëntaccount succesvol aangemaakt!");
            _logger.Info("Success message displayed.");

            Assert.AreEqual("", _addPatientPage.GetFirstName(),
                "Form should be cleared after success (first name).");
            Assert.AreEqual("", _addPatientPage.GetBsn(),
                "Form should be cleared after success (BSN).");

            _logger.Info("Form fields were cleared.");

            Assert.IsTrue(PatientExists(patient.Email),
                $"Patient must exist in the database with role {RoleType.Patient}.");

            _logger.Info("Patient found in the database.");
        }

        [TestMethod]
        [TestCategory("TC-9.1.2-02")]
        public void Patient_is_linked_to_the_gp_practice()
        {
            var patient = NewPatient();

            // Log input data and GP context before submitting the form.
            TestContext?.WriteLine(
                $"Creating patient with Email='{patient.Email}', GP UserId={_gpAccount.UserId}, GP Email='{_gpAccount.Email}'");
            _logger?.Info(
                $"Creating patient with Email='{patient.Email}', GP UserId={_gpAccount.UserId}, GP Email='{_gpAccount.Email}'");

            FillForm(patient);
            SubmitForm();
            WaitForSuccessMessage("Patiëntaccount succesvol aangemaakt!");

            // Log the GP UserId passed into SESSION_CONTEXT before running RLS-protected lookups.
            TestContext?.WriteLine($"Using GP UserId={_gpAccount.UserId} in SESSION_CONTEXT for lookups.");
            _logger?.Info($"Using GP UserId={_gpAccount.UserId} in SESSION_CONTEXT for lookups.");

            var gpPracticeId = Scalar(@"
                SELECT gp.PracticeId
                FROM dbo.GeneralPractitioner gp
                INNER JOIN dbo.AppUser u ON u.UserId = gp.UserId
                WHERE u.Email = @Email",
                _gpAccount.Email,
                sessionUserId: _gpAccount.UserId);

            var patientPracticeId = Scalar(@"
                SELECT p.PracticeId
                FROM dbo.Patient p
                INNER JOIN dbo.AppUser u ON u.UserId = p.UserId
                WHERE u.Email = @Email",
                patient.Email,
                sessionUserId: _gpAccount.UserId);

            // Log query results to see whether RLS or email matching caused a null value.
            TestContext?.WriteLine(
                $"Query result -> GP PracticeId: {gpPracticeId ?? "NULL"}, Patient PracticeId: {patientPracticeId ?? "NULL"}");
            _logger?.Info(
                $"GP PracticeId: {gpPracticeId ?? "NULL"}, Patient PracticeId: {patientPracticeId ?? "NULL"}");

            if (patientPracticeId is null or DBNull)
            {
                var appUserId = Scalar(@"
                    SELECT u.UserId
                    FROM dbo.AppUser u
                    WHERE u.Email = @Email",
                    patient.Email,
                    sessionUserId: _gpAccount.UserId);

                TestContext?.WriteLine(
                    $"Fallback AppUser lookup for '{patient.Email}' with GP session -> UserId: {appUserId ?? "NULL"}");
                _logger?.Info(
                    $"Fallback AppUser lookup for '{patient.Email}' with GP session -> UserId: {appUserId ?? "NULL"}");
            }

            Assert.IsNotNull(gpPracticeId, "The GP must have a linked practice.");
            Assert.IsNotNull(patientPracticeId, "The patient must have a linked practice.");
            Assert.AreEqual(Convert.ToInt32(gpPracticeId), Convert.ToInt32(patientPracticeId));
        }

        [TestMethod]
        [TestCategory("TC-9.1.2-03")]
        public void Patient_receives_activation_token_for_invitation_link()
        {
            var patient = NewPatient();

            FillForm(patient);
            SubmitForm();
            WaitForSuccessMessage("Patiëntaccount succesvol aangemaakt!");

            // The token used by the invitation link in the email is stored in the database.
            // Verifying the email itself can be done with a mail catcher (e.g., Papercut or MailHog).
            var tokenCount = Scalar(@"
                SELECT COUNT(1)
                FROM dbo.UserToken t
                INNER JOIN dbo.AppUser u ON u.UserId = t.UserId
                WHERE u.Email = @Email AND t.Type = 'Activation'",
                patient.Email,
                sessionUserId: _gpAccount.UserId);

            _logger.Info($"Activation token count for {patient.Email}: {tokenCount}");

            Assert.AreEqual(1, Convert.ToInt32(tokenCount));
        }

        // ---------------------------------------------------------------
        // Helpers: test data
        // ---------------------------------------------------------------

        private sealed class PatientTestData
        {
            public string FirstName { get; }
            public string LastName { get; }
            public string Email { get; }
            public string Bsn { get; }
            public string Phone { get; }
            public string DateOfBirth { get; }

            public PatientTestData(
                string firstName,
                string lastName,
                string email,
                string bsn,
                string phone,
                string dateOfBirth)
            {
                FirstName = firstName;
                LastName = lastName;
                Email = email;
                Bsn = bsn;
                Phone = phone;
                DateOfBirth = dateOfBirth;
            }
        }

        private static readonly Random RandomGenerator = new();

        private static PatientTestData NewPatient(
            string? bsn = null,
            string phone = "0612345678")
        {
            return new PatientTestData(
                "Selenium",
                "Testpatient",
                $"selenium.{Guid.NewGuid():N}@gentlehealers-test.nl",
                bsn ?? GenerateValidBsn(),
                phone,
                "1985-04-12");
        }

        // Generates a random BSN passing the elfproef (must be unique per test run).
        private static string GenerateValidBsn()
        {
            while (true)
            {
                var digits = new int[9];
                var sum = 0;

                for (var i = 0; i < 8; i++)
                {
                    digits[i] = RandomGenerator.Next(0, 10);
                    sum += digits[i] * (9 - i);
                }

                var checkDigit = sum % 11;
                if (checkDigit > 9 || sum - checkDigit <= 0)
                    continue;

                digits[8] = checkDigit;
                return string.Concat(digits);
            }
        }

        // ---------------------------------------------------------------
        // Helpers: browser (delegated to Page Objects)
        // ---------------------------------------------------------------

        private void FillForm(PatientTestData patient)
        {
            _logger.Info(
                $"Filling form: {patient.FirstName} {patient.LastName}, {patient.Email}, " +
                $"BSN {patient.Bsn}, phone {patient.Phone}, DOB {patient.DateOfBirth}");

            _addPatientPage.FillForm(
                patient.FirstName,
                patient.LastName,
                patient.Email,
                patient.Bsn,
                patient.Phone,
                patient.DateOfBirth);
        }

        private void SubmitForm()
        {
            _logger.Info("Clicking submit button to create patient.");
            _addPatientPage.SubmitForm();
        }

        private void WaitForErrorMessage(string expectedText)
        {
            WaitForText("patient-error", expectedText);
            _logger.Info($"Error message found: {expectedText}");
        }

        private void WaitForSuccessMessage(string expectedText)
        {
            WaitForText("patient-success", expectedText);
            _logger.Info($"Success message found: {expectedText}");
        }

        private void WaitForText(string elementId, string expectedText)
        {
            try
            {
                _wait.Until(driver =>
                    driver.FindElements(By.Id(elementId))
                        .Any(element =>
                            element.Displayed &&
                            element.Text.Contains(expectedText, StringComparison.Ordinal)));
            }
            catch (WebDriverTimeoutException)
            {
                var actualText = _driver.FindElements(By.Id(elementId))
                    .Where(element => element.Displayed)
                    .Select(element => element.Text)
                    .FirstOrDefault() ?? "(element niet gevonden of niet zichtbaar)";

                _logger.Error(
                    $"Expected text in element '{elementId}': \"{expectedText}\", " +
                    $"but found: \"{actualText}\"");

                throw;
            }
        }

        // ---------------------------------------------------------------
        // Helpers: database
        // ---------------------------------------------------------------

        private static void LinkGpToPractice(int userId, int practiceId)
        {
            const string insertGpSql = """
                INSERT INTO dbo.GeneralPractitioner (UserId, PracticeId)
                VALUES (@UserId, @PracticeId);
                """;

            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            using var command = new SqlCommand(insertGpSql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@PracticeId", practiceId);
            command.ExecuteNonQuery();
        }

        private bool PatientExists(string email) =>
            Convert.ToInt32(Scalar(@"
                SELECT COUNT(1)
                FROM dbo.Patient p
                INNER JOIN dbo.AppUser u ON u.UserId = p.UserId
                INNER JOIN dbo.Role r ON r.RoleId = u.RoleId
                WHERE u.Email = @Email AND r.Name = @RoleName",
                email,
                roleName: RoleType.Patient.ToString(),
                sessionUserId: _gpAccount.UserId)) > 0;

        private static object? Scalar(string sql, string email, string? roleName = null, int? sessionUserId = null)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            if (sessionUserId.HasValue)
            {
                using var ctxCmd = connection.CreateCommand();
                ctxCmd.CommandText = "EXEC sys.sp_set_session_context @key = N'UserId', @value = @UserId;";
                ctxCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = sessionUserId.Value;
                ctxCmd.ExecuteNonQuery();
            }

            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = email.Trim();
            if (roleName is not null)
            {
                command.Parameters.Add("@RoleName", SqlDbType.NVarChar, 100).Value = roleName;
            }

            return command.ExecuteScalar();
        }
    }
}