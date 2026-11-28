namespace GentleHealersApi.DTOs.Referral;

public record BookableReferralTreatment(int ReferralTreatmentId, string CareCode, string Description, int DurationMinutes);

public record ReferralAppointmentSlot(
    int SpecialistId, int LocationId, DateTime StartTime, string LocationName, string SpecialistName);

public record BookReferralAppointmentRequest(int SpecialistId, int LocationId, DateTime StartTime);
