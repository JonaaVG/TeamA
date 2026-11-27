namespace GentleHealersApi.DTOs.TreatmentCode;

public sealed record TreatmentCodeImportRow(
    string CareCode,
    string Description,
    string Specialty,
    int DurationQuarters,
    decimal Cost,
    string? PreparationNote);
