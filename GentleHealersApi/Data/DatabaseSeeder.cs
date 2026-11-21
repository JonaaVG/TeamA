using BCrypt.Net;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Data
{
    /// <summary>
    /// Handles database seeding by inserting default health insurers, practices, users, and role-specific profiles.
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseSeeder"/> class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public DatabaseSeeder(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Seeds the database with initial test data for GentleHealers.
        /// </summary>
        public void SeedDatabase()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using (var disablePolicyCmd = new SqlCommand("ALTER SECURITY POLICY Security.PatientPolicy WITH (STATE = OFF);", connection))
            {
                try { disablePolicyCmd.ExecuteNonQuery(); } catch {  }
            }
            string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Welkom123!");
            DateTime now = DateTime.UtcNow;

            // 1. Fetch existing Role IDs
            Console.WriteLine("Fetching existing roles...");
            int patientRoleId = GetRoleId(connection, "Patient");
            int gpRoleId = GetRoleId(connection, "GeneralPractitioner");
            int specialistRoleId = GetRoleId(connection, "Specialist");
            int insurerRoleId = GetRoleId(connection, "HealthInsurer");
            int adminRoleId = GetRoleId(connection, "SystemAdministrator");
            int nurseRoleId = GetRoleId(connection, "Nurse");
            int adminStaffRoleId = GetRoleId(connection, "AdministrativeStaff");

            // 2. Seed Health Insurer
            Console.WriteLine("Seeding health insurer...");
            int healthInsurerId = EnsureHealthInsurer(connection, "Zorg & Zekerheid Nederland");

            // 3. Seed General Practitioner Practice
            Console.WriteLine("Seeding general practitioner practice...");
            int practiceId = EnsurePractice(
                connection,
                practiceName: "Huisartsenpraktijk De Linde",
                email: "info@praktijkdelinde.nl",
                street: "Dorpsstraat",
                houseNumber: "12",
                houseNumberAddition: "A",
                postalCode: "5684AA",
                city: "Best",
                phone: "0499123456",
                createdAt: now
            );

            // 4. Seed Users & Linked Profiles
            Console.WriteLine("Seeding users and profiles...");

            // User 1: Patient
            int patientUserId = EnsureUser(connection, patientRoleId, "jan.jansen@example.com", defaultPasswordHash, "Jan", "Jansen", now);
            EnsurePatient(
                connection,
                userId: patientUserId,
                practiceId: practiceId,
                bsn: "123456782",
                dateOfBirth: new DateTime(1985, 5, 20),
                street: "Kerkstraat",
                houseNumber: "45",
                houseNumberAddition: null,
                postalCode: "5684BB",
                city: "Best",
                phone: "0612345678",
                healthInsurerId: healthInsurerId,
                policyNumber: "POL-987654321"
            );

            // User 2: General Practitioner
            int gpUserId = EnsureUser(connection, gpRoleId, "s.devries@praktijkdelinde.nl", defaultPasswordHash, "Sanne", "de Vries", now);
            EnsureGeneralPractitioner(connection, gpUserId, practiceId);

            // User 3: Specialist
            int specialistUserId = EnsureUser(connection, specialistRoleId, "m.bakker@gentlehealers.nl", defaultPasswordHash, "Mark", "Bakker", now);
            EnsureSpecialist(connection, specialistUserId);

            // User 4: Health Insurer User
            int insurerUserId = EnsureUser(connection, insurerRoleId, "l.koster@zorgzekerheid.nl", defaultPasswordHash, "Lisa", "Koster", now);
            EnsureHealthInsurerUser(connection, insurerUserId, healthInsurerId);

            // User 5: System Administrator
            EnsureUser(connection, adminRoleId, "admin@gentlehealers.nl", defaultPasswordHash, "Admin", "Beheer", now);

            // User 6: Nurse
            EnsureUser(connection, nurseRoleId, "e.visser@gentlehealers.nl", defaultPasswordHash, "Emma", "Visser", now);

            // User 7: Administrative Staff
            EnsureUser(connection, adminStaffRoleId, "balie@gentlehealers.nl", defaultPasswordHash, "Sophie", "Balie", now);

            Console.WriteLine("Base seed completed successfully!");
            using (var enablePolicyCmd = new SqlCommand("ALTER SECURITY POLICY Security.PatientPolicy WITH (STATE = ON);", connection))
            {
                try { enablePolicyCmd.ExecuteNonQuery(); } catch {  }
            }
        }

        private int GetRoleId(SqlConnection connection, string roleName)
        {
            string query = "SELECT RoleId FROM dbo.Role WHERE Name = @Name";
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Name", roleName);

            object? result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException($"Role '{roleName}' was not found in dbo.Role.");
            }

            return Convert.ToInt32(result);
        }

        private int EnsureHealthInsurer(SqlConnection connection, string name)
        {
            string selectQuery = "SELECT HealthInsurerId FROM dbo.HealthInsurer WHERE Name = @Name";
            using (var selectCmd = new SqlCommand(selectQuery, connection))
            {
                selectCmd.Parameters.AddWithValue("@Name", name);
                object? existingId = selectCmd.ExecuteScalar();
                if (existingId != null && existingId != DBNull.Value)
                {
                    return Convert.ToInt32(existingId);
                }
            }

            string insertQuery = "INSERT INTO dbo.HealthInsurer (Name) OUTPUT INSERTED.HealthInsurerId VALUES (@Name)";
            using var insertCmd = new SqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@Name", name);
            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        private int EnsurePractice(
            SqlConnection connection,
            string practiceName,
            string email,
            string street,
            string houseNumber,
            string? houseNumberAddition,
            string postalCode,
            string city,
            string phone,
            DateTime createdAt)
        {
            string selectQuery = "SELECT PracticeId FROM dbo.GeneralPractitionerPractice WHERE PracticeName = @PracticeName";
            using (var selectCmd = new SqlCommand(selectQuery, connection))
            {
                selectCmd.Parameters.AddWithValue("@PracticeName", practiceName);
                object? existingId = selectCmd.ExecuteScalar();
                if (existingId != null && existingId != DBNull.Value)
                {
                    return Convert.ToInt32(existingId);
                }
            }

            string insertQuery = @"
                INSERT INTO dbo.GeneralPractitionerPractice 
                    (PracticeName, Email, Street, HouseNumber, HouseNumberAddition, PostalCode, City, Phone, CreatedAt)
                OUTPUT INSERTED.PracticeId
                VALUES 
                    (@PracticeName, @Email, @Street, @HouseNumber, @HouseNumberAddition, @PostalCode, @City, @Phone, @CreatedAt)";

            using var insertCmd = new SqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@PracticeName", practiceName);
            insertCmd.Parameters.AddWithValue("@Email", email);
            insertCmd.Parameters.AddWithValue("@Street", street);
            insertCmd.Parameters.AddWithValue("@HouseNumber", houseNumber);
            insertCmd.Parameters.AddWithValue("@HouseNumberAddition", (object?)houseNumberAddition ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@PostalCode", postalCode);
            insertCmd.Parameters.AddWithValue("@City", city);
            insertCmd.Parameters.AddWithValue("@Phone", phone);
            insertCmd.Parameters.AddWithValue("@CreatedAt", createdAt);

            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        private int EnsureUser(
            SqlConnection connection,
            int roleId,
            string email,
            string passwordHash,
            string firstName,
            string lastName,
            DateTime createdAt)
        {
            string selectQuery = "SELECT UserId FROM dbo.AppUser WHERE Email = @Email";
            using (var selectCmd = new SqlCommand(selectQuery, connection))
            {
                selectCmd.Parameters.AddWithValue("@Email", email);
                object? existingId = selectCmd.ExecuteScalar();
                if (existingId != null && existingId != DBNull.Value)
                {
                    return Convert.ToInt32(existingId);
                }
            }

            string insertQuery = @"
                INSERT INTO dbo.AppUser 
                    (RoleId, Email, PasswordHash, FirstName, LastName, Status, CreatedAt, LastLoginAt)
                OUTPUT INSERTED.UserId
                VALUES 
                    (@RoleId, @Email, @PasswordHash, @FirstName, @LastName, 'Active', @CreatedAt, NULL)";

            using var insertCmd = new SqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@RoleId", roleId);
            insertCmd.Parameters.AddWithValue("@Email", email);
            insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            insertCmd.Parameters.AddWithValue("@FirstName", firstName);
            insertCmd.Parameters.AddWithValue("@LastName", lastName);
            insertCmd.Parameters.AddWithValue("@CreatedAt", createdAt);

            return Convert.ToInt32(insertCmd.ExecuteScalar());
        }

        private void EnsureGeneralPractitioner(SqlConnection connection, int userId, int practiceId)
        {
            string query = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.GeneralPractitioner WHERE UserId = @UserId)
                BEGIN
                    INSERT INTO dbo.GeneralPractitioner (UserId, PracticeId)
                    VALUES (@UserId, @PracticeId)
                END";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@PracticeId", practiceId);
            cmd.ExecuteNonQuery();
        }

        private void EnsureSpecialist(SqlConnection connection, int userId)
        {
            string query = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.Specialist WHERE UserId = @UserId)
                BEGIN
                    INSERT INTO dbo.Specialist (UserId)
                    VALUES (@UserId)
                END";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.ExecuteNonQuery();
        }

        private void EnsureHealthInsurerUser(SqlConnection connection, int userId, int healthInsurerId)
        {
            string query = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.HealthInsurerUser WHERE UserId = @UserId)
                BEGIN
                    INSERT INTO dbo.HealthInsurerUser (UserId, HealthInsurerId)
                    VALUES (@UserId, @HealthInsurerId)
                END";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@HealthInsurerId", healthInsurerId);
            cmd.ExecuteNonQuery();
        }

        private void EnsurePatient(
            SqlConnection connection,
            int userId,
            int practiceId,
            string bsn,
            DateTime dateOfBirth,
            string street,
            string houseNumber,
            string? houseNumberAddition,
            string postalCode,
            string city,
            string phone,
            int healthInsurerId,
            string policyNumber)
        {
            string query = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.Patient WHERE UserId = @UserId)
                BEGIN
                    INSERT INTO dbo.Patient 
                        (UserId, PracticeId, Bsn, DateOfBirth, Street, HouseNumber, HouseNumberAddition, PostalCode, City, Phone, HealthInsurerId, PolicyNumber)
                    VALUES 
                        (@UserId, @PracticeId, @Bsn, @DateOfBirth, @Street, @HouseNumber, @HouseNumberAddition, @PostalCode, @City, @Phone, @HealthInsurerId, @PolicyNumber)
                END";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@PracticeId", practiceId);
            cmd.Parameters.AddWithValue("@Bsn", bsn);
            cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
            cmd.Parameters.AddWithValue("@Street", street);
            cmd.Parameters.AddWithValue("@HouseNumber", houseNumber);
            cmd.Parameters.AddWithValue("@HouseNumberAddition", (object?)houseNumberAddition ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PostalCode", postalCode);
            cmd.Parameters.AddWithValue("@City", city);
            cmd.Parameters.AddWithValue("@Phone", phone);
            cmd.Parameters.AddWithValue("@HealthInsurerId", healthInsurerId);
            cmd.Parameters.AddWithValue("@PolicyNumber", policyNumber);
            cmd.ExecuteNonQuery();
        }
    }
}