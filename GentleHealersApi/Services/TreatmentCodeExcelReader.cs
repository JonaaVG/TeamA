using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using ExcelDataReader.Exceptions;
using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;

namespace GentleHealersApi.Services;

public sealed class TreatmentCodeExcelReader : ITreatmentCodeReader
{
    public const int MaximumRows = 10_000;
    private static readonly CultureInfo DutchCulture = CultureInfo.GetCultureInfo("nl-NL");
    private static readonly string[] RequiredColumns = ["code", "description", "specialty", "duration", "cost"];
    private static readonly Dictionary<string, string> ColumnAliases = CreateColumnAliases();

    public IReadOnlyList<TreatmentCodeImportRow> Read(Stream stream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            using var excel = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration { LeaveOpen = true });
            var rows = new List<TreatmentCodeImportRow>();
            var careCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var foundHeader = false;

            do
            {
                var columns = FindColumns(excel);
                if (columns is null)
                {
                    continue;
                }

                foundHeader = true;
                while (excel.Read())
                {
                    if (excel.Depth > 100_000)
                    {
                        throw new TreatmentCodeImportException("Het Excel-werkblad bevat te veel rijen.");
                    }

                    if (IsEmptyRow(excel))
                    {
                        continue;
                    }

                    if (rows.Count >= MaximumRows)
                    {
                        throw new TreatmentCodeImportException("Het Excel-bestand mag maximaal 10.000 behandelcodes bevatten.");
                    }

                    var rowNumber = excel.Depth + 1;
                    var code = RequiredText(excel, columns["code"], "zorgcode", 30, rowNumber);
                    if (!careCodes.Add(code))
                    {
                        throw new TreatmentCodeImportException($"Rij {rowNumber}: de zorgcode komt dubbel voor in het Excel-bestand.");
                    }

                    var description = RequiredText(excel, columns["description"], "omschrijving", 255, rowNumber);
                    var specialty = RequiredText(excel, columns["specialty"], "specialisme", 100, rowNumber);
                    var duration = ReadNumber(excel.GetValue(columns["duration"]), "tijdsduur", rowNumber);
                    if (duration <= 0 || duration > int.MaxValue || decimal.Truncate(duration) != duration)
                    {
                        throw new TreatmentCodeImportException($"Rij {rowNumber}: de tijdsduur moet een positief, heel aantal kwartieren zijn.");
                    }

                    var cost = ReadNumber(excel.GetValue(columns["cost"]), "kosten", rowNumber);
                    if (cost < 0 || cost > 99_999_999.99m || decimal.Round(cost, 2) != cost)
                    {
                        throw new TreatmentCodeImportException($"Rij {rowNumber}: de kosten moeten een geldig bedrag met maximaal twee decimalen zijn.");
                    }

                    string? preparation = null;
                    if (columns.TryGetValue("preparation", out var preparationIndex))
                    {
                        preparation = Text(excel.GetValue(preparationIndex));
                        if (preparation.Length > 500)
                        {
                            throw new TreatmentCodeImportException($"Rij {rowNumber}: de voorbereiding mag maximaal 500 tekens bevatten.");
                        }
                        if (preparation.Length == 0) preparation = null;
                    }

                    rows.Add(new TreatmentCodeImportRow(code, description, specialty, (int)duration, cost, preparation));
                }
            } while (excel.NextResult());

            if (!foundHeader)
            {
                throw new TreatmentCodeImportException("De benodigde kolommen ontbreken. Gebruik Zorgcode, Omschrijving, Specialisme, Tijdsduur (kwartieren) en Kosten.");
            }

            if (rows.Count == 0)
            {
                throw new TreatmentCodeImportException("Het Excel-bestand bevat geen behandelcodes.");
            }

            return rows;
        }
        catch (TreatmentCodeImportException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ExcelReaderException or IOException or ArgumentException or InvalidOperationException or NotSupportedException or System.Xml.XmlException)
        {
            throw new TreatmentCodeImportException("Het bestand is geen leesbaar Excel-bestand. Kies een geldig .xlsx- of .xls-bestand.");
        }
    }

    private static Dictionary<string, int>? FindColumns(IExcelDataReader excel)
    {
        // Allow a title or empty lines above the actual header, and ignore non-data worksheets.
        for (var attempt = 0; attempt < 20 && excel.Read(); attempt++)
        {
            var columns = new Dictionary<string, int>();
            for (var column = 0; column < excel.FieldCount; column++)
            {
                var normalized = NormalizeHeader(Text(excel.GetValue(column)));
                if (ColumnAliases.TryGetValue(normalized, out var field))
                {
                    columns.TryAdd(field, column);
                }
            }
            if (RequiredColumns.All(columns.ContainsKey)) return columns;
        }
        return null;
    }

    private static bool IsEmptyRow(IExcelDataReader excel)
    {
        for (var column = 0; column < excel.FieldCount; column++)
        {
            if (!string.IsNullOrWhiteSpace(Text(excel.GetValue(column)))) return false;
        }
        return true;
    }

    private static string RequiredText(IExcelDataReader excel, int column, string label, int maximumLength, int rowNumber)
    {
        var value = Text(excel.GetValue(column));
        if (value.Length == 0 || value.Length > maximumLength)
        {
            throw new TreatmentCodeImportException($"Rij {rowNumber}: {label} is verplicht en mag maximaal {maximumLength} tekens bevatten.");
        }
        return value;
    }

    private static string Text(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

    private static decimal ReadNumber(object? value, string label, int rowNumber)
    {
        if (value is not null and not string and not bool and not DateTime)
        {
            try { return Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
            catch (Exception exception) when (exception is FormatException or OverflowException or InvalidCastException) { }
        }

        var text = Text(value).Replace("€", "").Replace("\u00a0", "").Replace(" ", "");
        var culture = CultureInfo.InvariantCulture;
        if (text.LastIndexOf(',') > text.LastIndexOf('.')
            || Regex.IsMatch(text, @"^[+-]?\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?$"))
        {
            culture = DutchCulture;
        }

        // Decimal.TryParse accepts incorrectly grouped thousands (for example 1.2.3,45).
        // Reject those strings instead of silently importing a different financial amount.
        var numberPattern = culture == DutchCulture
            ? @"^[+-]?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$"
            : @"^[+-]?(?:\d+|\d{1,3}(?:,\d{3})+)(?:\.\d+)?$";
        if (Regex.IsMatch(text, numberPattern) && decimal.TryParse(text, NumberStyles.Number, culture, out var number)) return number;
        throw new TreatmentCodeImportException($"Rij {rowNumber}: {label} moet een geldig getal zijn.");
    }

    private static string NormalizeHeader(string value) => new(
        value.Normalize(NormalizationForm.FormD)
            .Where(character => char.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
            .Select(char.ToLowerInvariant).ToArray());

    private static Dictionary<string, string> CreateColumnAliases()
    {
        var aliases = new Dictionary<string, string>();
        Add("code", "zorgcode", "behandelcode", "carecode", "treatmentcode");
        Add("description", "omschrijving", "beschrijving", "description");
        Add("specialty", "specialisme", "specialiteit", "specialty", "specialism");
        Add("duration", "tijdsduur", "tijdsduurkwartieren", "tijdsduurinkwartieren", "duur", "duurkwartieren", "duurinkwartieren", "behandelduur", "aantalkwartieren", "kwartieren", "durationquarters", "durationinquarters");
        Add("cost", "kosten", "kosteneur", "kostenineuro", "cost", "prijs", "price", "tarief");
        Add("preparation", "voorbereiding", "voorbereidingsinstructie", "voorbereidingsnotitie", "preparationnote", "preparation", "instructies");
        return aliases;

        void Add(string field, params string[] names)
        {
            foreach (var name in names) aliases[name] = field;
        }
    }
}
