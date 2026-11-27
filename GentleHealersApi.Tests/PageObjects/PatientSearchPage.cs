using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests.PageObjects;

/// <summary>Page object for the huisarts patient search and patient list.</summary>
public sealed class PatientSearchPage : BasePage
{
    private const string SearchPath = "/patient/zoeken";

    private static readonly By SearchInput =
        By.CssSelector("input[type='text'][aria-label]");

    private static readonly By SearchButton = By.CssSelector("form button[type='submit']");

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
        var wait = CreatePageWait();
        var input = wait.Until(d =>
        {
            var element = d.FindElement(SearchInput);
            return element.Displayed && element.Enabled ? element : null;
        });

        var button = wait.Until(d =>
        {
            var element = d.FindElement(SearchButton);
            return element.Displayed && element.Enabled ? element : null;
        });

        input.Clear();
        input.SendKeys(query);
        button.Click();
    }

    public string WaitForPatientByBsn(string bsn)
    {
        string? rowText = CreatePageWait().Until(d =>
        {
            try
            {
                var alert = d.FindElements(By.CssSelector("[role='alert']"))
                    .FirstOrDefault(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));
                if (alert is not null)
                {
                    throw new InvalidOperationException(
                        $"De patiëntzoekopdracht gaf een API-fout: {alert.Text.Trim()}");
                }

                var row = d.FindElements(PatientRows)
                    .FirstOrDefault(e => e.Displayed && RowMatchesBsn(e, bsn));
                if (row is not null)
                {
                    return $"{row.GetDomProperty("textContent")} {GetAccessibleActionLabels(row)} {bsn}";
                }

                var noResults = d.FindElements(NoResultsMessage).FirstOrDefault(e => e.Displayed);
                if (noResults is not null)
                {
                    throw new NoSuchElementException(
                        $"De zoekopdracht is afgerond, maar BSN {bsn} ontbreekt. {noResults.Text.Trim()}");
                }

                return null;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        });

        return rowText ?? throw new NoSuchElementException(
            $"Geen zichtbare patiënt gevonden met test-BSN {bsn}.");
    }

    public void OpenReferralForPatient(string bsn)
    {
        var row = CreateWait().Until(d =>
        {
            try
            {
                return d.FindElements(PatientRows)
                    .FirstOrDefault(e => e.Displayed && RowMatchesBsn(e, bsn));
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

        row.FindElement(By.CssSelector("a[aria-label]")).Click();
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

    private static bool RowMatchesBsn(IWebElement row, string bsn)
    {
        // BSN is a unique test-data key. Read DOM text because the visual number is
        // aria-hidden while its screen-reader equivalent contains spaces between digits.
        string rowContent = row.GetDomProperty("textContent") ?? row.Text;
        if (rowContent.Contains(bsn, StringComparison.Ordinal))
        {
            return true;
        }

        string accessibleBsn = string.Join(" ", bsn.Select(digit => digit.ToString()));
        return row.FindElements(By.CssSelector("a[aria-label]"))
            .Any(link => (link.GetAttribute("aria-label") ?? string.Empty)
                .Contains(accessibleBsn, StringComparison.Ordinal));
    }

    private static string GetAccessibleActionLabels(IWebElement row) =>
        string.Join(" ", row.FindElements(By.CssSelector("a[aria-label]"))
            .Select(link => link.GetAttribute("aria-label"))
            .Where(label => !string.IsNullOrWhiteSpace(label)));

    private WebDriverWait CreatePageWait() => new(Driver, TimeSpan.FromSeconds(30));
}
