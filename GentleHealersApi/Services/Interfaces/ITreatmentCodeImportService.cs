using GentleHealersApi.DTOs;

namespace GentleHealersApi.Services.Interfaces;

public interface ITreatmentCodeImportService
{
    Task<TreatmentCodeImportResult> ImportAsync(IFormFile? file, int userId, CancellationToken cancellationToken = default);
}
