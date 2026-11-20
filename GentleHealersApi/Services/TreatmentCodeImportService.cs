using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;

namespace GentleHealersApi.Services;

public sealed class TreatmentCodeImportService(ITreatmentCodeReader reader, ITreatmentCodeRepository repository) : ITreatmentCodeImportService
{
    public const long MaximumFileSize = 10 * 1024 * 1024;
    private const string AcceptedFileTypes = "Kies een Excel-bestand (.xlsx of .xls).";

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
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
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
            throw new TreatmentCodeImportException("Het Excel-bestand is leeg. Kies een bestand met behandelcodes.");
        }

        if (file.Length > MaximumFileSize)
        {
            throw new TreatmentCodeImportException("Het Excel-bestand mag maximaal 10 MB groot zijn.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = file.OpenReadStream();
        var rows = reader.Read(stream);
        if (rows.Count == 0)
        {
            throw new TreatmentCodeImportException("Het Excel-bestand bevat geen behandelcodes.");
        }

        // Parse the entire workbook first. A rejected file must never change existing codes.
        cancellationToken.ThrowIfCancellationRequested();
        await repository.ImportAsync(rows, fileName, userId, cancellationToken);

        return new TreatmentCodeImportResult(rows.Count, "Bestand is succesvol geïmporteerd.");
    }
}
