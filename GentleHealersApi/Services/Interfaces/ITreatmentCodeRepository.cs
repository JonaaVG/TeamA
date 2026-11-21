using GentleHealersApi.DTOs;

namespace GentleHealersApi.Services.Interfaces;

public interface ITreatmentCodeRepository
{
    Task ImportAsync(IReadOnlyList<TreatmentCodeImportRow> rows, string fileName, int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TreatmentCodeDto>> GetActiveAsync(int userId, CancellationToken cancellationToken = default);
}
