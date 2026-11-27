using System.Globalization;
using OpenQA.Selenium;

namespace GentleHealersApi.Tests.PageObjects;

/// <summary>Page object for adding and saving a referral for the selected patient.</summary>
public sealed class PatientReferralPage : BasePage
{
    private static readonly By PageTitle = By.Id("page-title");
    private static readonly By TreatmentCodeSelect = By.Id("treatment-code");
    private static readonly By AddTreatmentButton = By.CssSelector("form button[type='submit']");
    private static readonly By PendingTreatmentRows = By.CssSelector("tbody tr");
    private static readonly By SubmitReferralButton =
        By.XPath("//button[@type='button' and normalize-space()='Aanmaken']");

    public PatientReferralPage(IWebDriver driver) : base(driver)
    {
    }

    public string WaitForTitle()
    {
        var title = CreateWait().Until(d =>
        {
            var element = d.FindElement(PageTitle);
            return element.Displayed && element.Text.Contains("Doorverwijzing aanmaken", StringComparison.Ordinal)
                ? element
                : null;
        });

        return title?.Text.Trim() ?? string.Empty;
    }

    public int GetSelectedPatientId()
    {
        var segments = new Uri(Driver.Url).AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 3 || segments[0] != "patient" || segments[2] != "doorverwijzen" ||
            !int.TryParse(segments[1], NumberStyles.None, CultureInfo.InvariantCulture, out var patientId))
        {
            throw new InvalidOperationException($"De verwachte patiënt-route staat niet open: {Driver.Url}");
        }

        return patientId;
    }

    public void AddTreatmentCode(int treatmentCodeId)
    {
        var select = CreateWait().Until(d =>
        {
            var element = d.FindElement(TreatmentCodeSelect);
            return element.Displayed && element.Enabled ? element : null;
        });

        string value = treatmentCodeId.ToString(CultureInfo.InvariantCulture);
        var option = select.FindElements(By.TagName("option"))
            .FirstOrDefault(element => element.GetAttribute("value") == value)
            ?? throw new NoSuchElementException($"Behandelcode met ID {value} staat niet in de keuzelijst.");
        option.Click();
        Driver.FindElement(AddTreatmentButton).Click();

        CreateWait().Until(d => d.FindElements(PendingTreatmentRows).Any(e => e.Displayed));
    }

    public void SubmitReferral()
    {
        var button = CreateWait().Until(d =>
        {
            var element = d.FindElement(SubmitReferralButton);
            return element.Displayed && element.Enabled ? element : null;
        });

        button.Click();
        CreateWait().Until(d => new Uri(d.Url).AbsolutePath == "/patient/zoeken");
    }
}
