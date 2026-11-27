using System.Data;
using GentleHealersApi.Tests.TestData.Infrastructure;
using GentleHealersApi.Tests.TestData.Models;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Tests.TestData.Factories;

/// <summary>
/// Creates isolated, repeatable referral scenarios. Rows are intentionally retained in the
/// test database, so BSNs, accounts, practices, specialties, and care codes are unique per run.
/// </summary>
public sealed class ReferralTestScenarioFactory
{
    private readonly string _connectionString;
    private readonly TestPracticeFactory _practiceFactory;
    private readonly TestUserFactory _userFactory;
    private readonly TestPatientFactory _patientFactory;

    public ReferralTestScenarioFactory(string connectionString)
    {
        _connectionString = connectionString;
        _practiceFactory = new TestPracticeFactory(connectionString);
        _patientFactory = new TestPatientFactory(connectionString);
        _userFactory = new TestUserFactory(connectionString, _patientFactory);
    }

    public ReferralTestScenario Create(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        bool createPatient = true)
    {
        EnsureRequiredRolesExist();

        var practice = _practiceFactory.CreatePractice();
        var practitioner = _userFactory.CreateGeneralPractitioner();
        int practitionerProfileId = LinkPractitionerToPractice(practitioner.UserId, practice.PracticeId);

        var treatmentCode = CreateTreatmentCode();

        if (!createPatient)
        {
            return new ReferralTestScenario(
                practice,
                practitioner,
                practitionerProfileId,
                null,
                null,
                treatmentCode.TreatmentCodeId);
        }

        var patientUser = _userFactory.CreateUser(RoleType.Patient, prefix: "referralpatient");
        var patient = CreatePatientForScenario(
            patientUser.UserId,
            practice.PracticeId,
            practitioner.UserId,
            dateOfBirth);

        SetPatientName(patientUser.UserId, firstName, lastName);

        return new ReferralTestScenario(
            practice,
            practitioner,
            practitionerProfileId,
            patientUser,
            patient,
            treatmentCode.TreatmentCodeId);
    }

    public int CountReferralsForPractitioner(int practitionerUserId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Referral r
            INNER JOIN dbo.GeneralPractitioner gp ON gp.GeneralPractitionerId = r.GeneralPractitionerId
            WHERE gp.UserId = @UserId;
            """;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        SetSessionUser(connection, practitionerUserId);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = practitionerUserId;
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int? FindReferralPatientId(int patientId, int practitionerUserId, int treatmentCodeId)
    {
        const string sql = """
            SELECT TOP (1) r.PatientId
            FROM dbo.Referral r
            INNER JOIN dbo.GeneralPractitioner gp ON gp.GeneralPractitionerId = r.GeneralPractitionerId
            INNER JOIN dbo.ReferralTreatment rt ON rt.ReferralId = r.ReferralId
            WHERE r.PatientId = @PatientId
              AND gp.UserId = @UserId
              AND rt.TreatmentCodeId = @TreatmentCodeId
            ORDER BY r.ReferralId DESC;
            """;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        SetSessionUser(connection, practitionerUserId);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@PatientId", SqlDbType.Int).Value = patientId;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = practitionerUserId;
        command.Parameters.Add("@TreatmentCodeId", SqlDbType.Int).Value = treatmentCodeId;

        object? result = command.ExecuteScalar();
        return result is null or DBNull ? null : Convert.ToInt32(result);
    }

    private void EnsureRequiredRolesExist()
    {
        var seeder = new RoleSeeder(_connectionString);
        seeder.EnsureRoleExists(RoleType.GeneralPractitioner);
        seeder.EnsureRoleExists(RoleType.Patient);
    }

    private static void SetSessionUser(SqlConnection connection, int userId)
    {
        using var command = new SqlCommand(
            "EXEC sys.sp_set_session_context @key = N'UserId', @value = @UserId;",
            connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.ExecuteNonQuery();
    }

    private int LinkPractitionerToPractice(int userId, int practiceId)
    {
        const string sql = """
            INSERT INTO dbo.GeneralPractitioner (UserId, PracticeId)
            OUTPUT INSERTED.GeneralPractitionerId
            VALUES (@UserId, @PracticeId);
            """;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@PracticeId", SqlDbType.Int).Value = practiceId;
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private void SetPatientName(int userId, string firstName, string lastName)
    {
        const string sql = """
            UPDATE dbo.AppUser
            SET FirstName = @FirstName, LastName = @LastName
            WHERE UserId = @UserId;
            """;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = firstName;
        command.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = lastName;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.ExecuteNonQuery();
    }

    private CreatedTestPatient CreatePatientForScenario(
        int userId,
        int practiceId,
        int actingUserId,
        DateOnly dateOfBirth)
    {
        string bsn = _patientFactory.GenerateUniqueBsnAsync().GetAwaiter().GetResult();

        const string sql = """
            EXEC sys.sp_set_session_context @key = N'UserId', @value = @ActingUserId;
            INSERT INTO dbo.Patient (UserId, PracticeId, Bsn, DateOfBirth)
            VALUES (@UserId, @PracticeId, @Bsn, @DateOfBirth);
            SELECT CONVERT(int, SCOPE_IDENTITY());
            """;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ActingUserId", SqlDbType.Int).Value = actingUserId;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@PracticeId", SqlDbType.Int).Value = practiceId;
        command.Parameters.Add("@Bsn", SqlDbType.NVarChar, 20).Value = bsn;
        command.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = dateOfBirth.ToDateTime(TimeOnly.MinValue);

        int patientId = Convert.ToInt32(command.ExecuteScalar());
        return new CreatedTestPatient
        {
            PatientId = patientId,
            UserId = userId,
            PracticeId = practiceId,
            Bsn = bsn,
            DateOfBirth = dateOfBirth
        };
    }

    private (int TreatmentCodeId, string CareCode) CreateTreatmentCode()
    {
        string unique = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        string specialtyName = $"Selenium {unique}";
        string careCode = $"SEL-{unique}";

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        int specialtyId;
        using (var specialtyCommand = new SqlCommand(
            "INSERT INTO dbo.Specialty (Name) OUTPUT INSERTED.SpecialtyId VALUES (@Name);", connection))
        {
            specialtyCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = specialtyName;
            specialtyId = Convert.ToInt32(specialtyCommand.ExecuteScalar());
        }

        const string treatmentSql = """
            INSERT INTO dbo.TreatmentCode
                (CareCode, Description, SpecialtyId, DurationQuarters, Cost, IsActive)
            OUTPUT INSERTED.TreatmentCodeId
            VALUES (@CareCode, @Description, @SpecialtyId, 3, 0, 1);
            """;

        using var treatmentCommand = new SqlCommand(treatmentSql, connection);
        treatmentCommand.Parameters.Add("@CareCode", SqlDbType.NVarChar, 30).Value = careCode;
        treatmentCommand.Parameters.Add("@Description", SqlDbType.NVarChar, 255).Value = "Selenium doorverwijzingstest";
        treatmentCommand.Parameters.Add("@SpecialtyId", SqlDbType.Int).Value = specialtyId;
        int treatmentCodeId = Convert.ToInt32(treatmentCommand.ExecuteScalar());

        return (treatmentCodeId, careCode);
    }
}

public sealed record ReferralTestScenario(
    TestPracticeInfo Practice,
    TestUserAccount Practitioner,
    int PractitionerProfileId,
    TestUserAccount? PatientUser,
    CreatedTestPatient? Patient,
    int TreatmentCodeId);
