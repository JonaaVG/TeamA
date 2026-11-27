using GentleHealersApi.DTOs.TreatmentCode;

namespace GentleHealersApi.Services.Interfaces.TreatmentCode;

public interface ITreatmentCodeReader
{
    IReadOnlyList<TreatmentCodeImportRow> Read(Stream stream, bool isCsv = false);
}
