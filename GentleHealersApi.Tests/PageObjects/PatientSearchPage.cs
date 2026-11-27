using OpenQA.Selenium;

namespace GentleHealersApi.Tests.PageObjects;

/// <summary>Page object for the huisarts patient search and patient list.</summary>
public sealed class PatientSearchPage : BasePage
{
    private const string SearchPath = "/patient/zoeken";

    private static readonly By SearchInput =
        By.CssSelector("input[aria-label='Zoek patiënt op naam of BSN']");

    private static readonly By SearchButton =
        By.XPath("//form//button[@type='submit' and normalize-space()='Zoeken']");

    private static readonly By PatientRows = By.CssSelector("ul li");
    private static readonly By NoResultsMessage =
        By.XPath("//p[normalize-space()='Geen patiënten gevonden.']");

    public PatientSearchPage(IWebDriver driver) : base(driver)
    {
    }

    public void NavigateTo(string baseUrl)
    {
        Driver.Navigate().GoToUrl($"{baseUrl.TrimEnd('/')}{SearchPath}");
        CreateWait().Until(d => d.FindElements(SearchInput).Any(e => e.Displayed));
    }

    public void SearchFor(string query)
    {
        var input = CreateWait().Until(d =>
        {
            var element = d.FindElement(SearchInput);
            return element.Displayed && element.Enabled ? element : null;
        });

        input.Clear();
        input.SendKeys(query);
        Driver.FindElement(SearchButton).Click();
    }

    public string WaitForPatientByBsn(string bsn)
    {
        var row = CreateWait().Until(d =>
        {
            try
            {
                return d.FindElements(PatientRows)
                    .FirstOrDefault(e => e.Displayed && e.Text.Contains(bsn, StringComparison.Ordinal));
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        });

        return row?.Text ?? throw new NoSuchElementException(
            $"Geen zichtbare patiënt gevonden met test-BSN {bsn}.");
    }

    public void OpenReferralForPatient(string bsn)
    {
        var row = CreateWait().Until(d =>
        {
            try
            {
                return d.FindElements(PatientRows)
                    .FirstOrDefault(e => e.Displayed && e.Text.Contains(bsn, StringComparison.Ordinal));
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        });

        if (row is null)
        {
            throw new NoSuchElementException($"Patiëntrij met test-BSN {bsn} is niet zichtbaar.");
        }

        row.FindElement(By.XPath(".//a[normalize-space()='Doorverwijzen']")).Click();
    }

    public string WaitForNoResults()
    {
        var message = CreateWait().Until(d =>
        {
            var element = d.FindElements(NoResultsMessage).FirstOrDefault(e => e.Displayed);
            return element;
        });

        return message?.Text.Trim() ?? string.Empty;
    }

    public bool HasVisiblePatientNamed(string fullName)
    {
        try
        {
            return Driver.FindElements(PatientRows)
                .Any(e => e.Displayed && e.Text.Contains(fullName, StringComparison.Ordinal));
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }
}
