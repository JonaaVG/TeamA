using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;
using GentleHealersApi.Controllers;
using GentleHealersApi.DTOs;
using GentleHealersApi.Services;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GentleHealersApi.Tests;

[TestClass]
public sealed class TreatmentCodeImportTests
{
    [TestMethod]
    public void TC_10_1_1_01_ReadsAllRequiredFieldsFromAnExcelWorkbook()
    {
        using var workbook = CreateWorkbook(
            ["Zorgcode", "Omschrijving", "Specialisme", "Tijdsduur (kwartieren)", "Kosten (€)"],
            ["Z0001", "Consult", "Chirurgie", 2, 125.50m],
            ["Z0002", "Onderzoek", "Cardiologie", 4, 320m]);

        var rows = new TreatmentCodeExcelReader().Read(workbook);

        Assert.HasCount(2, rows);
        Assert.AreEqual("Z0001", rows[0].CareCode);
        Assert.AreEqual("Consult", rows[0].Description);
        Assert.AreEqual("Chirurgie", rows[0].Specialty);
        Assert.AreEqual(2, rows[0].DurationQuarters);
        Assert.AreEqual(125.50m, rows[0].Cost);
        Assert.AreEqual("Z0002", rows[1].CareCode);
    }

    [TestMethod]
    public void TC_10_1_1_02_ReadsDutchCostTextAndOptionalPreparation()
    {
        using var workbook = CreateWorkbook(
            ["CareCode", "Description", "Specialty", "DurationQuarters", "Cost", "PreparationNote"],
            ["Z0001", "Onderzoek", "Chirurgie", "3", "€ 1.234,56", "Nuchter blijven"]);

        var row = new TreatmentCodeExcelReader().Read(workbook).Single();

        Assert.AreEqual(1234.56m, row.Cost);
        Assert.AreEqual(3, row.DurationQuarters);
        Assert.AreEqual("Nuchter blijven", row.PreparationNote);
    }

    [TestMethod]
    public async Task TC_10_1_1_03_ImportPassesAllRowsAndAuthenticatedUserToRepository()
    {
        var expectedRows = new[] { new TreatmentCodeImportRow("Z0001", "Consult", "Chirurgie", 2, 125.50m, null) };
        var repository = new RecordingRepository();
        var service = new TreatmentCodeImportService(new StubReader(expectedRows), repository);

        var result = await service.ImportAsync(CreateFile("behandelingen.XLSX"), 42);

        Assert.AreEqual(1, result.ImportedCount);
        Assert.AreEqual("Bestand is succesvol geïmporteerd.", result.Message);
        Assert.AreEqual(42, repository.ImportedByUserId);
        Assert.AreEqual("behandelingen.XLSX", repository.FileName);
        CollectionAssert.AreEqual(expectedRows, repository.Rows!.ToArray());
        Assert.AreEqual(1, repository.ImportCalls);
    }

    [TestMethod]
    public async Task TC_10_1_2_01_RejectsNonExcelBeforeReadingOrSavingAnything()
    {
        foreach (var fileName in new[] { "behandelingen.csv", "behandelingen.pdf", "behandelingen.xlsx.exe", "behandelingen" })
        {
            var reader = new StubReader([]);
            var repository = new RecordingRepository();
            var service = new TreatmentCodeImportService(reader, repository);

            var exception = await Assert.ThrowsAsync<TreatmentCodeImportException>(
                () => service.ImportAsync(CreateFile(fileName), 42));

            StringAssert.Contains(exception.Message, "Excel");
            StringAssert.Contains(exception.Message, ".xlsx");
            StringAssert.Contains(exception.Message, ".xls");
            Assert.AreEqual(0, reader.ReadCalls);
            Assert.AreEqual(0, repository.ImportCalls);
        }
    }

    [TestMethod]
    public void TC_10_1_2_02_RejectsAFileThatOnlyHasAnExcelExtension()
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("Dit is geen Excel-bestand."));

        var exception = Assert.Throws<TreatmentCodeImportException>(
            () => new TreatmentCodeExcelReader().Read(content));

        StringAssert.Contains(exception.Message, "Excel");
    }

    [TestMethod]
    public void TC_10_1_1_04_RejectsMissingColumnsWithoutSilentlyDroppingAField()
    {
        using var workbook = CreateWorkbook(
            ["Zorgcode", "Omschrijving", "Specialisme", "Tijdsduur (kwartieren)"],
            ["Z0001", "Consult", "Chirurgie", 2]);

        var exception = Assert.Throws<TreatmentCodeImportException>(
            () => new TreatmentCodeExcelReader().Read(workbook));

        StringAssert.Contains(exception.Message, "kolommen");
    }

    [TestMethod]
    public void TC_10_1_1_05_RejectsDuplicateCareCodesInTheSameFile()
    {
        using var workbook = CreateWorkbook(
            ["Zorgcode", "Omschrijving", "Specialisme", "Tijdsduur", "Kosten"],
            ["Z0001", "Consult", "Chirurgie", 2, 50m],
            ["z0001", "Ander consult", "Chirurgie", 2, 75m]);

        var exception = Assert.Throws<TreatmentCodeImportException>(
            () => new TreatmentCodeExcelReader().Read(workbook));

        StringAssert.Contains(exception.Message, "dubbel");
    }

    [TestMethod]
    public void TC_10_1_1_06_RejectsInvalidDurationOrCostBeforeImport()
    {
        foreach (var values in new object[][] { [0, 10m], [1.5m, 10m], [2, -1m], [2, 12.345m] })
        {
            using var workbook = CreateWorkbook(
                ["Zorgcode", "Omschrijving", "Specialisme", "Tijdsduur", "Kosten"],
                ["Z0001", "Consult", "Chirurgie", values[0], values[1]]);

            Assert.Throws<TreatmentCodeImportException>(() => new TreatmentCodeExcelReader().Read(workbook));
        }
    }

    [TestMethod]
    public void TC_10_1_1_11_DoesNotSilentlyConvertMalformedThousandsSeparators()
    {
        foreach (var cost in new[] { "1.2.3,45", "1,23,4.56" })
        {
            using var workbook = CreateWorkbook(
                ["Zorgcode", "Omschrijving", "Specialisme", "Tijdsduur", "Kosten"],
                ["Z0001", "Consult", "Chirurgie", 2, cost]);

            Assert.Throws<TreatmentCodeImportException>(() => new TreatmentCodeExcelReader().Read(workbook));
        }
    }

    [TestMethod]
    public async Task TC_10_1_1_07_ParseFailureLeavesExistingDataUntouched()
    {
        var repository = new RecordingRepository();
        var service = new TreatmentCodeImportService(new RejectingReader(), repository);

        await Assert.ThrowsAsync<TreatmentCodeImportException>(
            () => service.ImportAsync(CreateFile("behandelingen.xlsx"), 42));

        Assert.AreEqual(0, repository.ImportCalls);
    }

    [TestMethod]
    public async Task TC_10_1_2_03_EmptyOrOversizedUploadIsRejectedWithoutDatabaseAccess()
    {
        foreach (var length in new long[] { 0, TreatmentCodeImportService.MaximumFileSize + 1 })
        {
            var repository = new RecordingRepository();
            var service = new TreatmentCodeImportService(new StubReader([]), repository);
            var file = new FormFile(new MemoryStream([1]), 0, length, "file", "behandelingen.xlsx");

            await Assert.ThrowsAsync<TreatmentCodeImportException>(() => service.ImportAsync(file, 42));

            Assert.AreEqual(0, repository.ImportCalls);
        }
    }

    [TestMethod]
    public async Task TC_10_1_1_08_ImportControllerReturnsAHelpfulValidationMessage()
    {
        var controller = CreateController(new TreatmentCodeImportService(new StubReader([]), new RecordingRepository()));

        var response = await controller.Import(CreateFile("behandelingen.pdf"), CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(response.Result);
        var error = (TreatmentCodeImportError)((BadRequestObjectResult)response.Result!).Value!;
        StringAssert.Contains(error.Message, "Excel");
    }

    [TestMethod]
    public void TC_10_1_1_09_OnlyTheSystemAdministratorIsAuthorizedToImport()
    {
        var action = typeof(TreatmentCodesController).GetMethod(nameof(TreatmentCodesController.Import))!;
        var authorization = action.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();

        Assert.AreEqual("SystemAdministrator", authorization.Roles);
    }

    [TestMethod]
    public async Task TC_10_1_1_10_ControllerReturnsAvailableTreatmentFields()
    {
        var repository = new RecordingRepository();
        var controller = CreateController(new TreatmentCodeImportService(new StubReader([]), repository), repository);

        var response = await controller.GetActive(CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(response.Result);
        Assert.AreEqual(42, repository.ReadByUserId);
    }

    private static TreatmentCodesController CreateController(ITreatmentCodeImportService service, ITreatmentCodeRepository? repository = null)
    {
        var controller = new TreatmentCodesController(service, repository ?? new RecordingRepository())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim(ClaimTypes.NameIdentifier, "42"),
                        new Claim(ClaimTypes.Role, "SystemAdministrator")], "Test"))
                }
            }
        };
        return controller;
    }

    private static FormFile CreateFile(string name) => new(new MemoryStream([1, 2, 3]), 0, 3, "file", name);

    private sealed class RecordingRepository : ITreatmentCodeRepository
    {
        public int ImportCalls { get; private set; }
        public int ImportedByUserId { get; private set; }
        public int ReadByUserId { get; private set; }
        public string? FileName { get; private set; }
        public IReadOnlyList<TreatmentCodeImportRow>? Rows { get; private set; }

        public Task ImportAsync(IReadOnlyList<TreatmentCodeImportRow> rows, string fileName, int userId, CancellationToken cancellationToken = default)
        {
            ImportCalls++;
            ImportedByUserId = userId;
            FileName = fileName;
            Rows = rows;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TreatmentCodeDto>> GetActiveAsync(int userId, CancellationToken cancellationToken = default)
        {
            ReadByUserId = userId;
            return Task.FromResult<IReadOnlyList<TreatmentCodeDto>>([]);
        }
    }

    private sealed class StubReader(IReadOnlyList<TreatmentCodeImportRow> rows) : ITreatmentCodeReader
    {
        public int ReadCalls { get; private set; }
        public IReadOnlyList<TreatmentCodeImportRow> Read(Stream stream)
        {
            ReadCalls++;
            return rows;
        }
    }

    private sealed class RejectingReader : ITreatmentCodeReader
    {
        public IReadOnlyList<TreatmentCodeImportRow> Read(Stream stream) => throw new TreatmentCodeImportException("Het Excel-bestand kan niet worden gelezen.");
    }

    // A real minimal OpenXML workbook, so parsing tests exercise ExcelDataReader rather than mocks.
    internal static MemoryStream CreateWorkbook(params object[][] rows)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            WriteEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="utf-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            WriteEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="utf-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Behandelingen" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="utf-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
                """);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var sheetData = new XElement(ns + "sheetData");
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = new XElement(ns + "row", new XAttribute("r", rowIndex + 1));
                for (var column = 0; column < rows[rowIndex].Length; column++)
                {
                    var value = rows[rowIndex][column];
                    var cell = new XElement(ns + "c", new XAttribute("r", $"{(char)('A' + column)}{rowIndex + 1}"));
                    if (value is string text)
                    {
                        cell.SetAttributeValue("t", "inlineStr");
                        cell.Add(new XElement(ns + "is", new XElement(ns + "t", text)));
                    }
                    else
                    {
                        cell.Add(new XElement(ns + "v", Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)));
                    }
                    row.Add(cell);
                }
                sheetData.Add(row);
            }
            WriteEntry(archive, "xl/worksheets/sheet1.xml", new XDocument(new XElement(ns + "worksheet", sheetData)).ToString());
        }
        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string name, string contents)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(contents);
    }
}
