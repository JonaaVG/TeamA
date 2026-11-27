using System.ComponentModel.DataAnnotations;

namespace GentleHealersApi.DTOs.Referral
{

    public record CreateReferralItemDto
    {
        [Required] public int TreatmentCodeId { get; init; }
        [Range(15, 480)] public int DurationMinutes { get; init; }
        [MaxLength(1000)] public string? Notes { get; init; }
    }

    public record CreateReferralsRequest
    {
        [Required, MinLength(1)]
        public List<CreateReferralItemDto> Referrals { get; init; } = new();
    }
}