// // Unit tests for user story 9.1

// using System.Reflection;
// using System.Security.Claims;
// using GentleHealersApi;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Moq;
// using GentleHealersApi.Controllers;
// using GentleHealersApi.DTOs;

// namespace GentleHealersApi.Tests;

// // =====================================================================
// // AC 9.1.1 - Elfproef / eleven-test (BsnValidator, mirrors dbo.checkBsn)
// // =====================================================================
// [TestClass]
// public class BsnValidatorTests
// {
//     [TestMethod]                   // UT-9.1.1-01
//     [DataRow("111222333")]
//     [DataRow("123456782")]
//     public void IsValid_ValidBsn_ReturnsTrue(string bsn)
//         => Assert.IsTrue(BsnValidator.IsValid(bsn));

//     [TestMethod]                   // UT-9.1.1-02
//     [DataRow("123456789")]         // sum 147, not divisible by 11
//     [DataRow("111222334")]         // one digit off
//     [DataRow("000000000")]         // dbo.checkBsn explicitly rejects this BSN
//     public void IsValid_FailsElevenTest_ReturnsFalse(string bsn)
//         => Assert.IsFalse(BsnValidator.IsValid(bsn));

//     [TestMethod]                   // UT-9.1.1-03
//     [DataRow((string?)null)]
//     [DataRow("")]
//     [DataRow("   ")]
//     [DataRow("12345678")]          // 8 digits (the column is CHAR(9))
//     [DataRow("1234567890")]        // 10 digits
//     [DataRow("12345678a")]         // contains a letter
//     public void IsValid_InvalidFormat_ReturnsFalse(string? bsn)
//         => Assert.IsFalse(BsnValidator.IsValid(bsn));
// }

// // =====================================================================
// // AC 9.1.1 + 9.1.2 - PatientService
// // =====================================================================
// [TestClass]
// public class PatientServiceTests
// {
//     private const int GpUserId = 42;
//     private const int PracticeId = 7;
//     private const string RawToken = "raw-token";
//     private const string HashedToken = "hashed-token";

//     private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

//     private static readonly CreatePatientDto Valid = new(
//         "John", "Smith", "john.smith@example.com", "111222333", "0612345678", new DateOnly(1980, 5, 17));

//     // The service uses GetLocalNow() (the database works with local time), so the local time zone is fixed too.
//     private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
//     {
//         public override DateTimeOffset GetUtcNow() => now;
//         public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
//     }

//     private Mock<IPatientRepository> _patients = null!;
//     private Mock<IUserRepository> _users = null!;
//     private Mock<IGeneralPractitionerRepository> _generalPractitioners = null!;
//     private Mock<IInvitationService> _invitations = null!;
//     private Mock<ITokenGenerator> _tokens = null!;
//     private Mock<ITokenHasher> _hasher = null!;
//     private PatientService _sut = null!;

//     [TestInitialize]
//     public void Setup()
//     {
//         _patients = new Mock<IPatientRepository>();
//         _users = new Mock<IUserRepository>();
//         _generalPractitioners = new Mock<IGeneralPractitionerRepository>();
//         _invitations = new Mock<IInvitationService>();
//         _tokens = new Mock<ITokenGenerator>();
//         _hasher = new Mock<ITokenHasher>();

//         _patients.Setup(p => p.BsnExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
//         _patients.Setup(p => p.AddPatientAccountAsync(It.IsAny<NewPatientAccount>())).ReturnsAsync(99);
//         _users.Setup(u => u.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
//         _generalPractitioners.Setup(g => g.GetPracticeIdByUserIdAsync(GpUserId)).ReturnsAsync(PracticeId);
//         _tokens.Setup(t => t.Generate()).Returns(RawToken);
//         _hasher.Setup(h => h.Hash(RawToken)).Returns(HashedToken);

//         _sut = new PatientService(
//             _patients.Object, _users.Object, _generalPractitioners.Object, _invitations.Object,
//             _tokens.Object, _hasher.Object, new FixedTimeProvider(Now));
//     }

//     private void AssertNothingSavedOrSent()
//     {
//         _patients.Verify(p => p.AddPatientAccountAsync(It.IsAny<NewPatientAccount>()), Times.Never);
//         _invitations.Verify(i => i.SendInvitationAsync(
//             It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
//     }

//     private async Task<NewPatientAccount> CreateAccountAsync(CreatePatientDto? dto = null)
//     {
//         NewPatientAccount? saved = null;
//         _patients.Setup(p => p.AddPatientAccountAsync(It.IsAny<NewPatientAccount>()))
//                  .Callback<NewPatientAccount>(a => saved = a)
//                  .ReturnsAsync(99);

//         await _sut.CreatePatientAsync(dto ?? Valid, GpUserId);

//         Assert.IsNotNull(saved);
//         return saved;
//     }

//     // ---------- AC 9.1.1 ----------

//     [TestMethod]                   // UT-9.1.1-04
//     [DataRow("", "Smith", "john.smith@example.com")]
//     [DataRow("   ", "Smith", "john.smith@example.com")]
//     [DataRow("John", "", "john.smith@example.com")]
//     [DataRow("John", "Smith", "")]
//     public async Task CreatePatient_RequiredFieldEmpty_ThrowsValidationException(
//         string firstName, string lastName, string email)
//     {
//         var dto = Valid with { FirstName = firstName, LastName = lastName, Email = email };

//         await Assert.ThrowsAsync<PatientValidationException>(() => _sut.CreatePatientAsync(dto, GpUserId));

//         AssertNothingSavedOrSent();
//     }

//     [TestMethod]                   // UT-9.1.1-05
//     [DataRow("not-an-email")]
//     [DataRow("name@")]
//     [DataRow("@domain.nl")]
//     [DataRow("John <john@example.com>")]   // a display name is not a plain email address
//     public async Task CreatePatient_InvalidEmail_ThrowsValidationException(string email)
//     {
//         var dto = Valid with { Email = email };

//         await Assert.ThrowsAsync<PatientValidationException>(() => _sut.CreatePatientAsync(dto, GpUserId));

//         AssertNothingSavedOrSent();
//     }

//     [TestMethod]                   // UT-9.1.1-06 (date of birth not provided)
//     public async Task CreatePatient_DateOfBirthNotProvided_ThrowsValidationException()
//     {
//         var dto = Valid with { DateOfBirth = default };

//         await Assert.ThrowsAsync<PatientValidationException>(() => _sut.CreatePatientAsync(dto, GpUserId));

//         AssertNothingSavedOrSent();
//     }

//     [TestMethod]                   // UT-9.1.1-06 (date of birth in the future)
//     [DataRow(1)]
//     [DataRow(365)]
//     public async Task CreatePatient_DateOfBirthInFuture_ThrowsValidationException(int daysFromToday)
//     {
//         var dto = Valid with { DateOfBirth = DateOnly.FromDateTime(Now.DateTime).AddDays(daysFromToday) };

//         await Assert.ThrowsAsync<PatientValidationException>(() => _sut.CreatePatientAsync(dto, GpUserId));

//         AssertNothingSavedOrSent();
//     }

//     [TestMethod]                   // UT-9.1.1-07
//     public async Task CreatePatient_BsnFailsElevenTest_ThrowsValidationException_WithoutUniquenessCheck()
//     {
//         var dto = Valid with { Bsn = "123456789" };

//         await Assert.ThrowsAsync<PatientValidationException>(() => _sut.CreatePatientAsync(dto, GpUserId));

//         _patients.Verify(p => p.BsnExistsAsync(It.IsAny<string>()), Times.Never); // path: eleven-test first
//         AssertNothingSavedOrSent();
//     }

//     [TestMethod]                   // UT-9.1.1-08 (UQ_Patient_Bsn)
//     public async Task CreatePatient_BsnAlreadyExists_ThrowsDuplicateBsnException()
//     {
//         _patients.Setup(p => p.BsnExistsAsync(Valid.Bsn)).ReturnsAsync(true);

//         await Assert.ThrowsAsync<DuplicateBsnException>(() => _sut.CreatePatientAsync(Valid, GpUserId));

//         _patients.Verify(p => p.BsnExistsAsync(Valid.Bsn), Times.Once);
//         AssertNothingSavedOrSent();
//     }

//     [TestMethod]                   // UT-9.1.1-11 (extra, not an AC: UQ_AppUser_Email)
//     public async Task CreatePatient_EmailAlreadyExists_ThrowsDuplicateEmailException()
//     {
//         _users.Setup(u => u.EmailExistsAsync(Valid.Email)).ReturnsAsync(true);

//         await Assert.ThrowsAsync<DuplicateEmailException>(() => _sut.CreatePatientAsync(Valid, GpUserId));

//         AssertNothingSavedOrSent();
//     }

//     // --- Column lengths from the schema (extra, not an AC) ---

//     [TestMethod]                   // UT-9.1.1-12 (FirstName/LastName NVARCHAR(100))
//     public async Task CreatePatient_NameLongerThan100Characters_ThrowsValidationException()
//     {
//         var longName = new string('a', DbLimits.NameLength + 1);

//         await Assert.ThrowsAsync<PatientValidationException>(
//             () => _sut.CreatePatientAsync(Valid with { FirstName = longName }, GpUserId));
//         await Assert.ThrowsAsync<PatientValidationException>(
//             () => _sut.CreatePatientAsync(Valid with { LastName = longName }, GpUserId));

//         AssertNothingSavedOrSent();
//     }

//     [TestMethod]                   // UT-9.1.1-13 (Email NVARCHAR(256))
//     public async Task CreatePatient_EmailLongerThan256Characters_ThrowsValidationException()
//     {
//         var longEmail = new string('a', DbLimits.EmailLength) + "@example.com";

//         await Assert.ThrowsAsync<PatientValidationException>(
//             () => _sut.CreatePatientAsync(Valid with { Email = longEmail }, GpUserId));

//         AssertNothingSavedOrSent();
//     }

//     [TestMethod]                   // UT-9.1.1-14 (Phone NVARCHAR(30))
//     public async Task CreatePatient_PhoneLongerThan30Characters_ThrowsValidationException()
//     {
//         var longPhone = new string('1', DbLimits.PhoneLength + 1);

//         await Assert.ThrowsAsync<PatientValidationException>(
//             () => _sut.CreatePatientAsync(Valid with { Phone = longPhone }, GpUserId));

//         AssertNothingSavedOrSent();
//     }

//     // ---------- AC 9.1.2 ----------

//     [TestMethod]                   // UT-9.1.2-01
//     public async Task CreatePatient_AllValid_CreatesAppUserAndPatientLinkedToPractice()
//     {
//         NewPatientAccount? saved = null;
//         _patients.Setup(p => p.AddPatientAccountAsync(It.IsAny<NewPatientAccount>()))
//                  .Callback<NewPatientAccount>(a => saved = a)
//                  .ReturnsAsync(99);

//         var patientId = await _sut.CreatePatientAsync(Valid, GpUserId);

//         _patients.Verify(p => p.AddPatientAccountAsync(It.IsAny<NewPatientAccount>()), Times.Once);
//         Assert.AreEqual(99, patientId);
//         Assert.IsNotNull(saved);

//         // AppUser
//         var user = saved.User;
//         Assert.AreEqual(RoleIds.Patient, user.RoleId);
//         Assert.AreEqual(Valid.Email, user.Email);
//         Assert.AreEqual(Valid.FirstName, user.FirstName);
//         Assert.AreEqual(Valid.LastName, user.LastName);
//         Assert.AreEqual("Invited", user.Status);             // CK_AppUser_Status; becomes 'Active' after activation
//         Assert.IsNull(user.PasswordHash);                    // the patient sets their own password
//         Assert.IsNull(user.LastLoginAt);
//         // CreatedAt is set by the database (DF_AppUser_CreatedAt = dbo.GetLocalTime())

//         // Patient
//         var patient = saved.Patient;
//         Assert.AreEqual(PracticeId, patient.PracticeId);     // the practice of the general practitioner
//         Assert.AreEqual(Valid.Bsn, patient.Bsn);
//         Assert.AreEqual(Valid.DateOfBirth, patient.DateOfBirth);
//         Assert.AreEqual(Valid.Phone, patient.Phone);
//     }

//     [TestMethod]                   // UT-9.1.2-02
//     public async Task CreatePatient_AllValid_CreatesActivationTokenWithHashAndExpiry()
//     {
//         var account = await CreateAccountAsync();

//         var token = account.Token;
//         Assert.AreEqual("Activation", token.Type);           // CK_UserToken_Type
//         Assert.AreEqual(HashedToken, token.TokenHash);
//         Assert.AreNotEqual(RawToken, token.TokenHash);       // the raw token is never stored
//         Assert.AreEqual(Now.DateTime + PatientService.ActivationTokenLifetime, token.ExpiresAt);
//         Assert.IsNull(token.UsedAt);
//     }

//     [TestMethod]                   // UT-9.1.2-03
//     public async Task CreatePatient_AllValid_SendsInvitationWithRawToken()
//     {
//         await _sut.CreatePatientAsync(Valid, GpUserId);

//         _invitations.Verify(i => i.SendInvitationAsync(Valid.Email, Valid.FirstName, RawToken), Times.Once);
//         _invitations.Verify(i => i.SendInvitationAsync(
//             It.IsAny<string>(), It.IsAny<string>(), HashedToken), Times.Never); // never email the hash
//     }

//     [TestMethod]                   // UT-9.1.2-04
//     public async Task CreatePatient_GeneralPractitionerWithoutPractice_ThrowsPracticeNotFoundException()
//     {
//         _generalPractitioners.Setup(g => g.GetPracticeIdByUserIdAsync(GpUserId)).ReturnsAsync((int?)null);

//         await Assert.ThrowsAsync<PracticeNotFoundException>(() => _sut.CreatePatientAsync(Valid, GpUserId));

//         AssertNothingSavedOrSent();
//     }

//     [TestMethod]                   // UT-9.1.2-08 (Patient.Phone is nullable in the schema)
//     [DataRow((string?)null)]
//     [DataRow("")]
//     [DataRow("   ")]
//     public async Task CreatePatient_PhoneEmpty_CreatesPatientWithoutPhone(string? phone)
//     {
//         var account = await CreateAccountAsync(Valid with { Phone = phone });

//         Assert.IsNull(account.Patient.Phone);
//     }
// }

// // =====================================================================
// // AC 9.1.1 + 9.1.2 - PatientsController
// // =====================================================================
// [TestClass]
// public class PatientsControllerTests
// {
//     private Mock<IPatientService> _service = null!;

//     private static readonly CreatePatientDto Dto = new(
//         "John", "Smith", "john.smith@example.com", "111222333", "0612345678", new DateOnly(1980, 5, 17));

//     [TestInitialize]
//     public void Setup() => _service = new Mock<IPatientService>();

//     private PatientsController CreateController(string? userId = "42")
//     {
//         var claims = new List<Claim>();
//         if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

//         return new PatientsController(_service.Object)
//         {
//             ControllerContext = new ControllerContext
//             {
//                 HttpContext = new DefaultHttpContext
//                 {
//                     User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
//                 }
//             }
//         };
//     }

//     [TestMethod]                   // UT-9.1.2-05
//     public async Task Create_Success_Returns201WithLocation()
//     {
//         _service.Setup(s => s.CreatePatientAsync(Dto, 42)).ReturnsAsync(99);

//         var result = await CreateController().Create(Dto);

//         var created = result as CreatedResult;
//         Assert.IsNotNull(created);
//         Assert.AreEqual("/api/patients/99", created.Location);
//         _service.Verify(s => s.CreatePatientAsync(Dto, 42), Times.Once);
//     }

//     [TestMethod]                   // UT-9.1.1-09
//     public async Task Create_ValidationError_Returns400()
//     {
//         _service.Setup(s => s.CreatePatientAsync(It.IsAny<CreatePatientDto>(), It.IsAny<int>()))
//                 .ThrowsAsync(new PatientValidationException("BSN is invalid (fails the elfproef check)."));

//         var result = await CreateController().Create(Dto);

//         Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
//     }

//     [TestMethod]                   // UT-9.1.1-10
//     public async Task Create_BsnAlreadyExists_Returns409()
//     {
//         _service.Setup(s => s.CreatePatientAsync(It.IsAny<CreatePatientDto>(), It.IsAny<int>()))
//                 .ThrowsAsync(new DuplicateBsnException());

//         var result = await CreateController().Create(Dto);

//         Assert.IsInstanceOfType(result, typeof(ConflictObjectResult));
//     }

//     [TestMethod]                   // UT-9.1.2-06
//     public async Task Create_WithoutValidUserIdClaim_Returns401_AndDoesNotCallService()
//     {
//         var result = await CreateController(userId: null).Create(Dto);

//         Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
//         _service.Verify(s => s.CreatePatientAsync(It.IsAny<CreatePatientDto>(), It.IsAny<int>()), Times.Never);
//     }

//     [TestMethod]                   // UT-9.1.2-07 (only a general practitioner may do this; role name from the Role table)
//     public void Controller_RequiresGeneralPractitionerRole()
//     {
//         var attribute = typeof(PatientsController).GetCustomAttribute<AuthorizeAttribute>();

//         Assert.IsNotNull(attribute);
//         Assert.AreEqual("GeneralPractitioner", attribute.Roles);
//     }
// }