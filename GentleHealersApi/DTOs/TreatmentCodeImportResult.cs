namespace GentleHealersApi.DTOs;

public sealed record TreatmentCodeImportResult(int ImportedCount, string Message);

public sealed record TreatmentCodeImportError(string Message);
