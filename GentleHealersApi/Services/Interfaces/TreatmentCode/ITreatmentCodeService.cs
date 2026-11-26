using GentleHealersApi.DTOs.TreatmentCode;

namespace GentleHealersApi.Services.Interfaces.TreatmentCode
{
    public interface ITreatmentCodeService
    {
        Task<IEnumerable<GetTreatmentCodeDto>> GetActiveTreatmentCodesAsync(int userId);
    }
}