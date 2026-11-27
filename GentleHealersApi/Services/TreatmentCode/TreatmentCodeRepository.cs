using System.Data;
using GentleHealersApi.DTOs.TreatmentCode;
using GentleHealersApi.Services.Interfaces.TreatmentCode;
using GentleHealersApi.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Services.TreatmentCode;

public sealed class TreatmentCodeRepository(ISqlConnectionFactory connectionFactory) : ITreatmentCodeRepository
{
    public async Task ImportAsync(IReadOnlyList<TreatmentCodeImportRow> rows, string fileName, int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.GetAuthenticatedConnectionAsync(userId);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        // Existing DATETIME2 columns use the hospital's local time throughout the schema.
        await using var timestampCommand = new SqlCommand("SELECT dbo.GetLocalTime();", connection, transaction);
        var importedAt = (DateTime)(await timestampCommand.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("The database timestamp is unavailable."));

        // Serialise imports without deleting historical versions used by referrals and invoices.
        await using (var importLock = new SqlCommand("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = N'GentleHealers.TreatmentCodeImport',
                @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
            SELECT @result;
            """, connection, transaction))
        {
            var result = Convert.ToInt32(await importLock.ExecuteScalarAsync(cancellationToken));
            if (result < 0) throw new InvalidOperationException("Another treatment-code import is running.");
        }

        await using var batchCommand = new SqlCommand("""
            INSERT INTO dbo.ImportBatch (Type, FileName, ImportedByUserId, ImportedAt, [RowCount], Status)
            OUTPUT INSERTED.ImportBatchId
            VALUES (N'TreatmentCodes', @FileName, @UserId, @ImportedAt, @RowCount, N'Succeeded');
            """, connection, transaction);
        batchCommand.Parameters.Add("@FileName", SqlDbType.NVarChar, 255).Value = fileName;
        batchCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        batchCommand.Parameters.Add("@ImportedAt", SqlDbType.DateTime2).Value = importedAt;
        batchCommand.Parameters.Add("@RowCount", SqlDbType.Int).Value = rows.Count;
        var batchId = Convert.ToInt32(await batchCommand.ExecuteScalarAsync(cancellationToken));

        await using (var deactivate = new SqlCommand("""
            UPDATE dbo.TreatmentCode
            SET IsActive = 0, DeactivatedAt = @ImportedAt
            WHERE IsActive = 1;
            """, connection, transaction))
        {
            deactivate.Parameters.Add("@ImportedAt", SqlDbType.DateTime2).Value = importedAt;
            await deactivate.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var row in rows)
        {
            await using var insert = new SqlCommand("""
                DECLARE @SpecialtyId int;
                SELECT @SpecialtyId = SpecialtyId FROM dbo.Specialty WHERE Name = @Specialty;
                IF @SpecialtyId IS NULL
                BEGIN
                    INSERT INTO dbo.Specialty (Name) VALUES (@Specialty);
                    SET @SpecialtyId = CONVERT(int, SCOPE_IDENTITY());
                END;

                INSERT INTO dbo.TreatmentCode
                    (CareCode, Description, PreparationNote, SpecialtyId, DurationQuarters, Cost, IsActive, ImportBatchId)
                VALUES (@CareCode, @Description, @PreparationNote, @SpecialtyId, @DurationQuarters, @Cost, 1, @BatchId);
                """, connection, transaction);
            insert.Parameters.Add("@Specialty", SqlDbType.NVarChar, 100).Value = row.Specialty;
            insert.Parameters.Add("@CareCode", SqlDbType.NVarChar, 30).Value = row.CareCode;
            insert.Parameters.Add("@Description", SqlDbType.NVarChar, 255).Value = row.Description;
            insert.Parameters.Add("@PreparationNote", SqlDbType.NVarChar, 500).Value = (object?)row.PreparationNote ?? DBNull.Value;
            insert.Parameters.Add("@DurationQuarters", SqlDbType.Int).Value = row.DurationQuarters;
            var costParameter = insert.Parameters.Add("@Cost", SqlDbType.Decimal);
            costParameter.Precision = 10;
            costParameter.Scale = 2;
            costParameter.Value = row.Cost;
            insert.Parameters.Add("@BatchId", SqlDbType.Int).Value = batchId;
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var audit = new SqlCommand("""
            INSERT INTO dbo.AuditLog (Timestamp, UserId, ActionType, Entity, EntityId, Details)
            VALUES (@Timestamp, @UserId, N'Create', N'ImportBatch', @BatchId, @Details);
            """, connection, transaction))
        {
            audit.Parameters.Add("@Timestamp", SqlDbType.DateTime2).Value = importedAt;
            audit.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            audit.Parameters.Add("@BatchId", SqlDbType.BigInt).Value = batchId;
            audit.Parameters.Add("@Details", SqlDbType.NVarChar, -1).Value = $"Treatment-code import completed: {rows.Count} codes.";
            await audit.ExecuteNonQueryAsync(cancellationToken);
        }

        // Disposing an uncommitted transaction rolls everything back, including the audit entry.
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TreatmentCodeDto>> GetActiveAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.GetAuthenticatedConnectionAsync(userId);
        await using var command = new SqlCommand("""
            SELECT t.TreatmentCodeId, t.CareCode, t.Description, s.Name AS Specialty,
                   t.DurationQuarters, t.Cost, t.PreparationNote
            FROM dbo.TreatmentCode AS t
            INNER JOIN dbo.Specialty AS s ON s.SpecialtyId = t.SpecialtyId
            WHERE t.IsActive = 1
            ORDER BY t.CareCode;
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<TreatmentCodeDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new TreatmentCodeDto(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetInt32(4), reader.GetDecimal(5), reader.IsDBNull(6) ? null : reader.GetString(6)));
        }
        return rows;
    }
}
