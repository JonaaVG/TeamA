namespace GentleHealersApi.DTOs.TreatmentCode;

public sealed record TreatmentCodeDto(
    int TreatmentCodeId,
    string CareCode,
    string Description,
    string Specialty,
    int DurationQuarters,
    decimal Cost,
    string? PreparationNote);
