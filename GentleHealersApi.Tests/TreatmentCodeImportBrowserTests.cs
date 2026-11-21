using System.Globalization;
using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace GentleHealersApi.Tests;

[TestClass]
[DoNotParallelize]
public sealed class TreatmentCodeImportBrowserTests
{
    private static readonly By FileInput = By.Id("treatment-code-file");
    private static readonly By SubmitButton = By.CssSelector("form button[type='submit']");
    private ChromeDriver? driver;
    private DirectoryInfo? testFiles;

    [TestInitialize]
    public void OpenImportPage()
    {
        var url = Environment.GetEnvironmentVariable("GENTLEHEALERS_BROWSER_TEST_URL");
        if (string.IsNullOrWhiteSpace(url))
        {
            Assert.Inconclusive("Set GENTLEHEALERS_BROWSER_TEST_URL to run the browser tests against a disposable local test database.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var baseUri) ||
            !baseUri.IsLoopback || (baseUri.Scheme != "http" && baseUri.Scheme != "https"))
        {
            throw new InvalidOperationException("GENTLEHEALERS_BROWSER_TEST_URL must be a local HTTP(S) address.");
        }

        var email = RequireSetting("GENTLEHEALERS_BROWSER_TEST_EMAIL");
        var password = RequireSetting("GENTLEHEALERS_BROWSER_TEST_PASSWORD");
        testFiles = Directory.CreateTempSubdirectory("gentlehealers-import-tests-");

        var options = new ChromeOptions();
        options.AddArguments("--headless", "--window-size=1440,1000");
        driver = new ChromeDriver(options);
        var timeouts = driver.Manage().Timeouts();
        timeouts.ImplicitWait = TimeSpan.FromSeconds(12);
        timeouts.AsynchronousJavaScript = TimeSpan.FromSeconds(12);

        driver.Navigate().GoToUrl(new Uri(baseUri, "/login"));
        driver.FindElement(By.Id("email")).SendKeys(email);
        driver.FindElement(By.Id("password")).SendKeys(password);
        driver.FindElement(SubmitButton).Click();
        driver.FindElement(By.LinkText("Zorgbehandelingenbestand importeren")).Click();
        driver.FindElement(FileInput);
    }

    [TestCleanup]
    public void CloseBrowserAndRemoveFiles()
    {
        try
        {
            driver?.Dispose();
        }
        finally
        {
            if (testFiles is not null)
            {
                foreach (var file in testFiles.EnumerateFiles())
                {
                    file.Delete();
                }
                testFiles.Delete();
            }
        }
    }

    [TestMethod]
    [TestCategory("Browser")]
    [DataRow("XLSX")]
    [DataRow("CSV")]
    public void TC_10_1_1_12_ImportsAnExcelOrCsvFileAndPersistsTheTreatmentData(string extension)
    {
        var fileName = $"behandelingen.{extension}";
        const string code = "ZT001";
        const string description = "Selenium testconsult";
        const string specialty = "Chirurgie";
        const int quarters = 2;
        const decimal price = 125.50m;

        var path = Path.Combine(testFiles!.FullName, fileName);
        if (extension == "CSV")
        {
            var amount = price.ToString(CultureInfo.InvariantCulture);
            File.WriteAllText(path, $"Zorgcode,Omschrijving,Specialisme,Tijdsduur (kwartieren),Kosten (€)\r\n{code},{description},{specialty},{quarters},{amount}\r\n");
        }
        else
        {
            using var workbook = TreatmentCodeImportTests.CreateWorkbook(
                ["Zorgcode", "Omschrijving", "Specialisme", "Tijdsduur (kwartieren)", "Kosten (€)"],
                [code, description, specialty, quarters, price]);
            using var file = File.Create(path);
            workbook.CopyTo(file);
        }

        var input = driver!.FindElement(FileInput);
        CollectionAssert.AreEquivalent(
            new[] { ".xlsx", ".xls", ".csv" },
            input.GetDomAttribute("accept")!.Split(','));

        // The file name check should accept the legacy extension regardless of case.
        var legacySelection = Path.Combine(testFiles.FullName, "behandelingen.XLS");
        File.WriteAllText(legacySelection, "Nonempty file for extension selection, never uploaded.");
        input.SendKeys(legacySelection);
        Assert.IsTrue(driver.FindElement(SubmitButton).Enabled);

        input.SendKeys(path);
        Assert.AreEqual(fileName, driver.FindElement(By.Id("selected-treatment-code-file")).Text);

        driver.FindElement(SubmitButton).Click();
        Assert.AreEqual("Import geslaagd", driver.FindElement(By.Id("import-success-heading")).Text);

        using var catalog = ReadActiveCatalog(driver);
        var records = catalog.RootElement.EnumerateArray().ToArray();
        Assert.HasCount(1, records);
        var record = records.Single();
        Assert.AreEqual(code, record.GetProperty("careCode").GetString());
        Assert.AreEqual(description, record.GetProperty("description").GetString());
        Assert.AreEqual(specialty, record.GetProperty("specialty").GetString());
        Assert.AreEqual(quarters, record.GetProperty("durationQuarters").GetInt32());
        Assert.AreEqual(price, record.GetProperty("cost").GetDecimal());
    }

    [TestMethod]
    [TestCategory("Browser")]
    public void TC_10_1_2_04_RejectsFilesWithUnsupportedExtensions()
    {
        using var catalogBefore = ReadActiveCatalog(driver!);
        foreach (var name in new[] { "behandelingen.pdf", "behandelingen.txt", "behandelingen.xlsx.txt", "behandelingen" })
        {
            var path = Path.Combine(testFiles!.FullName, name);
            File.WriteAllText(path, "Invalid browser test file.");
            driver!.FindElement(FileInput).SendKeys(path);

            Assert.AreEqual(name, driver.FindElement(By.Id("selected-treatment-code-file")).Text);
            Assert.IsFalse(driver.FindElement(SubmitButton).Enabled, $"The import button was enabled for {name}.");
            var feedback = driver.FindElement(By.CssSelector("#import-feedback[role='alert']")).Text;
            StringAssert.Contains(feedback, "Excel- of CSV-bestand (.xlsx, .xls of .csv)");
        }

        using var catalogAfter = ReadActiveCatalog(driver!);
        Assert.AreEqual(catalogBefore.RootElement.GetRawText(), catalogAfter.RootElement.GetRawText());
    }

    private static string RequireSetting(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Set {name} for the test administrator.")
            : value;
    }

    private static JsonDocument ReadActiveCatalog(ChromeDriver browser)
    {
        var json = browser.ExecuteAsyncScript("""
            const complete = arguments[arguments.length - 1];
            fetch('/api/treatment-codes', {
              headers: { Authorization: 'Bearer ' + localStorage.getItem('token') }
            })
              .then(response => response.ok ? response.text() : Promise.reject(response.status))
              .then(complete, error => complete(null));
            """) as string;
        Assert.IsNotNull(json, "The active treatment catalogue could not be read.");
        return JsonDocument.Parse(json);
    }
}
