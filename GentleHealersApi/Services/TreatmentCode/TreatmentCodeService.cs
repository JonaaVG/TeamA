using System.Data;
using System.Text;
using GentleHealersApi.DTOs.TreatmentCode;
using GentleHealersApi.Extensions;
using GentleHealersApi.Services.Interfaces.TreatmentCode;
using GentleHealersApi.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Services.TreatmentCode
{
    public class TreatmentCodeService : ITreatmentCodeService
    {


        private readonly ISqlConnectionFactory _connectionFactory;

        public TreatmentCodeService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<GetTreatmentCodeDto>> GetActiveTreatmentCodesAsync(int userId)
        {
            using var connection = await _connectionFactory.GetAuthenticatedConnectionAsync(userId);
            using var cmd = new SqlCommand(@"
        SELECT TreatmentCodeId, CareCode, Description, SpecialtyId, DurationQuarters
        FROM dbo.TreatmentCode
        WHERE IsActive = 1
        ORDER BY CareCode", connection);

            var results = new List<GetTreatmentCodeDto>();
            using var reader = await cmd.ExecuteReaderAsync();

            var idOrdinal = reader.GetOrdinal("TreatmentCodeId");
            var codeOrdinal = reader.GetOrdinal("CareCode");
            var descOrdinal = reader.GetOrdinal("Description");
            var specOrdinal = reader.GetOrdinal("SpecialtyId");
            var durOrdinal = reader.GetOrdinal("DurationQuarters");

            while (await reader.ReadAsync())
            {
                results.Add(new GetTreatmentCodeDto
                {
                    TreatmentCodeId = reader.GetInt32(idOrdinal),
                    CareCode = reader.GetString(codeOrdinal),
                    Description = reader.GetString(descOrdinal),
                    SpecialtyId = reader.GetInt32(specOrdinal),
                    DurationQuarters = reader.GetInt32(durOrdinal)
                });
            }

            return results;
        }

    }
}