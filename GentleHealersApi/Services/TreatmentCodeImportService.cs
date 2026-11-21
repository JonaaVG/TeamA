using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;

namespace GentleHealersApi.Services;

public sealed class TreatmentCodeImportService(ITreatmentCodeReader reader, ITreatmentCodeRepository repository) : ITreatmentCodeImportService
{
    public const long MaximumFileSize = 10 * 1024 * 1024;
    private const string AcceptedFileTypes = "Kies een Excel- of CSV-bestand (.xlsx, .xls of .csv).";

    public async Task<TreatmentCodeImportResult> ImportAsync(IFormFile? file, int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedAccessException("Invalid user identity.");
        }

        if (file is null)
        {
            throw new TreatmentCodeImportException(AcceptedFileTypes);
        }

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        var extension = Path.GetExtension(fileName);
        var isCsv = string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase);
        if (!isCsv
            && !string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase))
        {
            throw new TreatmentCodeImportException(AcceptedFileTypes);
        }

        if (fileName.Length > 255)
        {
            throw new TreatmentCodeImportException("De bestandsnaam mag maximaal 255 tekens bevatten.");
        }

        if (file.Length == 0)
        {
            throw new TreatmentCodeImportException("Het bestand is leeg. Kies een bestand met behandelcodes.");
        }

        if (file.Length > MaximumFileSize)
        {
            throw new TreatmentCodeImportException("Het bestand mag maximaal 10 MB groot zijn.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = file.OpenReadStream();
        var rows = reader.Read(stream, isCsv);
        if (rows.Count == 0)
        {
            throw new TreatmentCodeImportException("Het bestand bevat geen behandelcodes.");
        }

        // Parse the entire file first. A rejected file must never change existing codes.
        cancellationToken.ThrowIfCancellationRequested();
        await repository.ImportAsync(rows, fileName, userId, cancellationToken);

        return new TreatmentCodeImportResult(rows.Count, "Bestand is succesvol geïmporteerd.");
    }
}
