namespace GentleHealersApi.DTOs.Referral
{
    public record CreateReferralsResult(
        string PatientEmail,
        string PatientFirstName,
        IReadOnlyList<CreatedReferralTreatment> Treatments);
}