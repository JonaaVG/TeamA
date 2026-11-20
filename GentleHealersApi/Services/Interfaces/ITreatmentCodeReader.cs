using GentleHealersApi.DTOs;

namespace GentleHealersApi.Services.Interfaces;

public interface ITreatmentCodeReader
{
    IReadOnlyList<TreatmentCodeImportRow> Read(Stream stream);
}
