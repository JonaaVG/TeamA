using GentleHealersApi.DTOs.TreatmentCode;

namespace GentleHealersApi.Services.Interfaces.TreatmentCode;

public interface ITreatmentCodeRepository
{
    Task ImportAsync(IReadOnlyList<TreatmentCodeImportRow> rows, string fileName, int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TreatmentCodeDto>> GetActiveAsync(int userId, CancellationToken cancellationToken = default);
}
