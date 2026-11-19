namespace GentleHealersApi.Helpers;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;

public class MsSqlDatabaseHelper
{

    private readonly string connectionString =
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build()
        .GetConnectionString("DefaultConnection");


    /// <summary>
    /// Executes a SQL query (SELECT, INSERT, UPDATE, DELETE).
    /// </summary>
    /// <param name="query">The SQL command to execute.</param>
    /// <returns>A DataTable containing results for SELECT queries, otherwise empty.</returns>
    public DataTable ExecuteQuery(string query)
    {
        DataTable dataTable = new DataTable();
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    // For SELECT queries, use a DataAdapter to fetch results
                    if (query.Trim().ToUpper().StartsWith("SELECT"))
                    {
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                    // For modification queries (INSERT, UPDATE, DELETE)
                    else
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Implement proper error logging here
                Console.WriteLine($"Database Error: {ex.Message}");
                throw; // Re-throw the exception to fail the test
            }
        }
        return dataTable;
    }
}