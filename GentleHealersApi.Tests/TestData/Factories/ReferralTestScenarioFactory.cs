using System.Data;
using GentleHealersApi.Tests.TestData.Infrastructure;
using GentleHealersApi.Tests.TestData.Models;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Tests.TestData.Factories;

/// <summary>
/// Creates isolated, repeatable referral scenarios. Test accounts, practices, and patients are
/// retained in the test database; referrals use an active treatment code from the imported list.
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

        var treatmentCodeId = GetImportedTreatmentCodeId(practitioner.UserId);

        if (!createPatient)
        {
            return new ReferralTestScenario(
                practice,
                practitioner,
                practitionerProfileId,
                null,
                null,
                treatmentCodeId);
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
            treatmentCodeId);
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

    private int GetImportedTreatmentCodeId(int practitionerUserId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        SetSessionUser(connection, practitionerUserId);

        const string sql = """
            SELECT TOP (1) TreatmentCodeId
            FROM dbo.TreatmentCode
            WHERE IsActive = 1
              AND ImportBatchId IS NOT NULL
            ORDER BY CareCode, TreatmentCodeId DESC;
            """;

        using var command = new SqlCommand(sql, connection);
        object? result = command.ExecuteScalar();
        if (result is null or DBNull)
        {
            throw new InvalidOperationException(
                "Er is geen actieve, geïmporteerde behandelcode gevonden in de testdatabase. " +
                "Importeer eerst de zorgbehandelingscodes in de database uit appsettings.Testing.json.");
        }

        return Convert.ToInt32(result);
    }
}

public sealed record ReferralTestScenario(
    TestPracticeInfo Practice,
    TestUserAccount Practitioner,
    int PractitionerProfileId,
    TestUserAccount? PatientUser,
    CreatedTestPatient? Patient,
    int TreatmentCodeId);
