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
    /// The TestCategory numbers match the test case document (TC 9.1.1 - 01 ... TC 9.1.2 - 07).
    /// All test data is generated; fixed values are only used for deliberately invalid input.
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
        public void Valid_details_are_accepted_without_validation_errors()
        {
            var patient = NewPatient();

            FillForm(patient);
            SubmitForm();

            WaitForSuccessMessage("Patiëntaccount succesvol aangemaakt!");

            Assert.IsFalse(IsDisplayed("patient-error"),
                "No validation error should be shown for valid input.");

            _logger.Info("Check passed: valid details were accepted.");
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-02")]
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
        [TestCategory("TC-9.1.1-03")]
        public void BSN_with_too_few_digits_is_rejected()
        {
            var patient = NewPatient(bsn: "11122233"); // 8 digits.

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-04")]
        public void BSN_with_too_many_digits_is_rejected()
        {
            var patient = NewPatient(bsn: "1112223334"); // 10 digits.

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-05")]
        public void BSN_with_a_letter_is_rejected()
        {
            var patient = NewPatient(bsn: "11122233A");

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-06")]
        public void Empty_BSN_is_rejected()
        {
            var patient = NewPatient(bsn: "");

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-07")]
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
        [TestCategory("TC-9.1.1-08")]
        public void Empty_name_is_rejected()
        {
            var patient = NewPatient(firstName: "", lastName: "");

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-09")]
        public void Empty_email_is_rejected()
        {
            var patient = NewPatient(email: "");

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
            Assert.IsFalse(PatientExistsByBsn(patient.Bsn),
                "No patient should be created when the email address is empty.");
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-10")]
        public void Invalid_email_format_is_rejected()
        {
            var patient = NewPatient(email: "jan.jansen.example.com"); // No '@'.

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
            Assert.IsFalse(PatientExistsByBsn(patient.Bsn),
                "No patient should be created with an invalid email address.");
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-11")]
        public void Empty_phone_number_is_rejected()
        {
            var patient = NewPatient(phone: "");

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-12")]
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

        [TestMethod]
        [TestCategory("TC-9.1.1-13")]
        public void Empty_date_of_birth_is_rejected()
        {
            var patient = NewPatient(dateOfBirth: "");

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-14")]
        public void Date_of_birth_in_the_future_is_rejected()
        {
            var futureDate = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
            var patient = NewPatient(dateOfBirth: futureDate);

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-15")]
        public void Empty_form_is_rejected()
        {
            // Nothing is filled in; submit the empty form straight away.
            SubmitForm();

            Assert.IsFalse(IsDisplayed("patient-success"),
                "No success message should be shown for an empty form.");
            Assert.IsTrue(_driver.Url.Contains(NewPatientPath),
                "The user should stay on the add patient page.");

            _logger.Info("Check passed: empty form was not accepted.");
        }

        [TestMethod]
        [TestCategory("TC-9.1.1-16")]
        public void Add_patient_page_is_not_accessible_when_logged_out()
        {
            // Log out by removing the session (cookies and browser storage) of the GP.
            _driver.Manage().Cookies.DeleteAllCookies();
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "window.localStorage.clear(); window.sessionStorage.clear();");

            // Go straight to the add patient page via the URL (full page load,
            // so the router guard runs again without an authenticated user).
            _driver.Navigate().GoToUrl(BaseUrl.TrimEnd('/') + NewPatientPath);

            // The router redirects to the login route; the form is never shown.
            _wait.Until(driver => !driver.Url.Contains(NewPatientPath));

            Assert.IsFalse(_driver.Url.Contains(NewPatientPath),
                "A logged out user must be redirected away from the add patient page.");

            _logger.Info($"Check passed: redirected to {_driver.Url}");
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

            var tokenCount = ActivationTokenCount(patient.Email);

            _logger.Info($"Activation token count for {patient.Email}: {tokenCount}");

            Assert.AreEqual(1, tokenCount);
        }

        [TestMethod]
        [TestCategory("TC-9.1.2-05")]
        public void Invalid_details_create_no_account_and_no_token()
        {
            var patient = NewPatient(bsn: "123456789"); // Fails the elfproef check.

            FillForm(patient);
            SubmitForm();

            AssertRejected(patient);

            Assert.AreEqual(0, ActivationTokenCount(patient.Email),
                "No activation token may be created for invalid details.");

            _logger.Info("Check passed: no account and no token were created.");
        }

        [TestMethod]
        [TestCategory("TC-9.1.2-06")]
        public void Double_click_on_save_creates_only_one_account()
        {
            var patient = NewPatient();

            FillForm(patient);

            // Click twice in quick succession. The second click may hit a disabled
            // or re-rendered button, which is fine.
            SubmitForm();
            try
            {
                _addPatientPage.SubmitForm();
            }
            catch (WebDriverException ex)
            {
                _logger.Info($"Second click could not be performed (expected): {ex.GetType().Name}");
            }

            WaitForSuccessMessage("Patiëntaccount succesvol aangemaakt!");

            Assert.AreEqual(1, CountPatientsByBsn(patient.Bsn),
                "Exactly one patient with this BSN must exist.");
            Assert.AreEqual(1, ActivationTokenCount(patient.Email),
                "Exactly one invitation (activation token) must exist.");

            _logger.Info("Check passed: only one account was created.");
        }

        [TestMethod]
        [TestCategory("TC-9.1.2-07")]
        public void Gp_from_another_practice_cannot_see_the_new_patient()
        {
            // Practice B with its own GP.
            var practiceB = _practiceFactory.CreatePractice();
            var gpB = _userFactory.CreateGeneralPractitioner();
            LinkGpToPractice(gpB.UserId, practiceB.PracticeId);

            // GP of practice A (logged in during setup) creates the patient.
            var patient = NewPatient();
            FillForm(patient);
            SubmitForm();
            WaitForSuccessMessage("Patiëntaccount succesvol aangemaakt!");

            // Control: the GP of practice A can see the patient.
            Assert.IsTrue(PatientExists(patient.Email),
                "The GP of the own practice must be able to see the patient.");

            // The GP of practice B must not see the patient (row-level security).
            var visibleForGpB = Convert.ToInt32(Scalar(@"
                SELECT COUNT(1)
                FROM dbo.Patient p
                INNER JOIN dbo.AppUser u ON u.UserId = p.UserId
                WHERE u.Email = @Email",
                patient.Email,
                sessionUserId: gpB.UserId));

            _logger.Info(
                $"Patients with email {patient.Email} visible for GP of practice B: {visibleForGpB}");

            Assert.AreEqual(0, visibleForGpB,
                "A GP from another practice must not see the patient.");
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

        // Everything is generated unless a value is passed in. Passing an empty string
        // ("") makes that field deliberately empty; null means "generate a value".
        private static PatientTestData NewPatient(
            string? bsn = null,
            string? phone = null,
            string? firstName = null,
            string? lastName = null,
            string? email = null,
            string? dateOfBirth = null)
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];

            return new PatientTestData(
                firstName ?? "Selenium",
                lastName ?? $"Testpatient{uniqueId}",
                email ?? $"selenium.{Guid.NewGuid():N}@gentlehealers-test.nl",
                bsn ?? GenerateValidBsn(),
                phone ?? $"06{RandomGenerator.Next(10000000, 100000000)}",
                dateOfBirth ?? GenerateDateOfBirth());
        }

        // Random date of birth in the past, formatted as yyyy-MM-dd.
        private static string GenerateDateOfBirth()
        {
            var date = new DateTime(
                RandomGenerator.Next(1940, 2006),
                RandomGenerator.Next(1, 13),
                RandomGenerator.Next(1, 29));

            return date.ToString("yyyy-MM-dd");
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

        private bool IsDisplayed(string elementId) =>
            _driver.FindElements(By.Id(elementId)).Any(element => element.Displayed);

        // Generic check for rejected input: use this when the exact error text is not
        // known (or when the browser itself blocks the submit). To also check the text,
        // pass expectedError.
        private void AssertRejected(PatientTestData patient, string? expectedError = null)
        {
            if (expectedError is not null)
            {
                WaitForErrorMessage(expectedError);
            }
            else
            {
                try
                {
                    new WebDriverWait(_driver, TimeSpan.FromSeconds(3)).Until(driver =>
                        driver.FindElements(By.Id("patient-error")).Any(element =>
                            element.Displayed && !string.IsNullOrWhiteSpace(element.Text)));

                    var message = _driver.FindElement(By.Id("patient-error")).Text;
                    _logger.Info($"Error message shown: \"{message}\"");
                }
                catch (WebDriverTimeoutException)
                {
                    _logger.Info(
                        "No message in #patient-error (possibly browser or field validation).");
                }
            }

            Assert.IsFalse(IsDisplayed("patient-success"),
                "No success message may be shown for rejected input.");
            Assert.IsTrue(_driver.Url.Contains(NewPatientPath),
                "The user should stay on the add patient page.");
            Assert.IsFalse(PatientExists(patient.Email),
                "No patient should be created for rejected input.");

            _logger.Info("Check passed: input was rejected and no patient was created.");
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

        // Number of patients with this BSN (as seen by the logged in GP).
        // The BSN is generated digits only, so it is safe to put in the SQL text.
        private int CountPatientsByBsn(string bsn) =>
            Convert.ToInt32(Scalar($@"
                SELECT COUNT(1)
                FROM dbo.Patient p
                WHERE p.Bsn = '{bsn}'",
                email: string.Empty,
                sessionUserId: _gpAccount.UserId));

        private bool PatientExistsByBsn(string bsn) => CountPatientsByBsn(bsn) > 0;

        private int ActivationTokenCount(string email) =>
            Convert.ToInt32(Scalar(@"
                SELECT COUNT(1)
                FROM dbo.UserToken t
                INNER JOIN dbo.AppUser u ON u.UserId = t.UserId
                WHERE u.Email = @Email AND t.Type = 'Activation'",
                email,
                sessionUserId: _gpAccount.UserId));

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