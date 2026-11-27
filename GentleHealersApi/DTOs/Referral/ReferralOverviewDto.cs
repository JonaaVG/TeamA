namespace GentleHealersApi.DTOs.Referral
{
    public class ReferralOverviewDto
    {
        public int ReferralTreatmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Department { get; set; } = string.Empty;
        public string TreatmentDescription { get; set; } = string.Empty;
        public string GeneralPractitionerName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}