using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Tests.TestData.Factories
{
    public sealed class TestPracticeInfo
    {
        public int PracticeId { get; init; }
        public string PracticeName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }

    public sealed class TestPracticeFactory
    {
        private readonly string _connectionString;

        public TestPracticeFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public TestPracticeInfo CreatePractice(string? practiceName = null)
        {
            string uniqueId = Guid.NewGuid().ToString("N")[..8];
            string name = practiceName ?? $"Test Practice {uniqueId}";
            string email = $"practice_{uniqueId}@gentlehealers-test.nl";

            const string insertSql = """
                INSERT INTO dbo.GeneralPractitionerPractice
                    (PracticeName, Email, Street, HouseNumber, HouseNumberAddition, PostalCode, City, Phone, CreatedAt)
                OUTPUT INSERTED.PracticeId
                VALUES
                    (@PracticeName, @Email, @Street, @HouseNumber, @HouseNumberAddition, @PostalCode, @City, @Phone, SYSUTCDATETIME());
                """;

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("@PracticeName", name);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@Street", "Dokterstraat");
            command.Parameters.AddWithValue("@HouseNumber", "10");
            command.Parameters.AddWithValue("@HouseNumberAddition", DBNull.Value);
            command.Parameters.AddWithValue("@PostalCode", "5683AA");
            command.Parameters.AddWithValue("@City", "Best");
            command.Parameters.AddWithValue("@Phone", "0612345678");

            int practiceId = Convert.ToInt32(command.ExecuteScalar());

            return new TestPracticeInfo
            {
                PracticeId = practiceId,
                PracticeName = name,
                Email = email
            };
        }

        public void DeletePractice(int practiceId)
        {
            // Clean up dependent records referencing PracticeId before deleting the practice
            const string deleteSql = """
                DELETE FROM dbo.GeneralPractitioner WHERE PracticeId = @PracticeId;
                DELETE FROM dbo.Patient WHERE PracticeId = @PracticeId;
                DELETE FROM dbo.GeneralPractitionerPractice WHERE PracticeId = @PracticeId;
                """;

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(deleteSql, connection);
            command.Parameters.AddWithValue("@PracticeId", practiceId);
            command.ExecuteNonQuery();
        }
    }
}