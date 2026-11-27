using GentleHealersApi.DTOs.TreatmentCode;

namespace GentleHealersApi.Services.Interfaces.TreatmentCode;

public interface ITreatmentCodeImportService
{
    Task<TreatmentCodeImportResult> ImportAsync(IFormFile? file, int userId, CancellationToken cancellationToken = default);
}
