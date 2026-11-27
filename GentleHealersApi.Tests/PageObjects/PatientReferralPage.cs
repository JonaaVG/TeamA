using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

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
    private static readonly By ErrorMessage = By.CssSelector("[role='alert']");

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

    public void NavigateToPatientReferral(string baseUrl, int patientId)
    {
        Driver.Navigate().GoToUrl(
            $"{baseUrl.TrimEnd('/')}/patient/{patientId}/doorverwijzen");
        WaitForTitle();
    }

    public void AddTreatmentCode(int treatmentCodeId)
    {
        string value = treatmentCodeId.ToString(CultureInfo.InvariantCulture);
        var select = CreateWait().Until(d =>
        {
            try
            {
                var element = d.FindElement(TreatmentCodeSelect);
                bool codeIsLoaded = element.FindElements(By.TagName("option"))
                    .Any(option => option.GetAttribute("value") == value);
                return element.Displayed && element.Enabled && codeIsLoaded ? element : null;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        });

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

    public ReferralSubmissionOutcome SubmitReferralAndWaitForOutcome()
    {
        var button = CreateWait().Until(d =>
        {
            var element = d.FindElement(SubmitReferralButton);
            return element.Displayed && element.Enabled ? element : null;
        });

        button.Click();
        var outcome = new WebDriverWait(Driver, TimeSpan.FromSeconds(30)).Until(d =>
        {
            if (new Uri(d.Url).AbsolutePath == "/patient/zoeken")
            {
                return new ReferralSubmissionOutcome(Succeeded: true, ErrorMessage: null);
            }

            var element = d.FindElements(ErrorMessage).FirstOrDefault(e => e.Displayed);
            return string.IsNullOrWhiteSpace(element?.Text)
                ? null
                : new ReferralSubmissionOutcome(Succeeded: false, ErrorMessage: element.Text.Trim());
        });

        return outcome ?? throw new InvalidOperationException("De verwijzing gaf geen zichtbaar resultaat.");
    }
}

public sealed record ReferralSubmissionOutcome(bool Succeeded, string? ErrorMessage);
