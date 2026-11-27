using GentleHealersApi.DTOs.Referral;

namespace GentleHealersApi.Services.Interfaces
{
    public interface IReferralService
    {
        Task<CreateReferralsResult> CreateReferralsAsync(int patientId, IReadOnlyList<CreateReferralItemDto> items, int practitionerUserId);
        Task<List<ReferralOverviewDto>> GetOpenReferralsAsync(int patientId, int userId, bool isStaff);
    }
}