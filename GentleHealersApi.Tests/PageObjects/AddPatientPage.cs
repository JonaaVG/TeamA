
using OpenQA.Selenium;

namespace GentleHealersApi.Tests.PageObjects
{
    public class AddPatientPage : BasePage
    {
        private readonly By _firstName = By.Id("patient-firstname");
        private readonly By _lastName = By.Id("patient-lastname");
        private readonly By _email = By.Id("patient-email");
        private readonly By _bsn = By.Id("patient-bsn");
        private readonly By _phone = By.Id("patient-phone");
        private readonly By _dateOfBirth = By.Id("patient-date-of-birth");
        private readonly By _submitButton = By.Id("patient-submit");
        private readonly By _errorMessage = By.Id("patient-error");
        private readonly By _successMessage = By.Id("patient-success");


        public AddPatientPage(IWebDriver driver) : base(driver)
        {
        }

        public void NavigateTo(
            string baseUrl,
            string path = "/patienten/nieuw")
        {
            Driver.Navigate().GoToUrl(
                $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}"
            );

            CreateWait().Until(d =>
                d.FindElements(_firstName)
                    .Any(e => e.Displayed)
            );
        }

        public void FillForm(
            string firstName,
            string lastName,
            string email,
            string bsn,
            string phone,
            string dateOfBirth)
        {
            EnterText(_firstName, firstName);
            EnterText(_lastName, lastName);
            EnterText(_email, email);
            EnterText(_bsn, bsn);
            EnterText(_phone, phone);

            SetDateOfBirth(dateOfBirth);
        }

        public void SubmitForm()
        {
            var button = CreateWait().Until(d =>
            {
                var element = d.FindElement(_submitButton);
                return element.Displayed && element.Enabled
                    ? element
                    : null;
            });

            button.Click();
        }

        public void WaitForSuccessMessage(string expectedMessage)
        {
            WaitForMessage(_successMessage, expectedMessage);
        }

        public void WaitForErrorMessage(string expectedMessage)
        {
            WaitForMessage(_errorMessage, expectedMessage);
        }

        public string GetFirstName()
        {
            return Driver.FindElement(_firstName)
                .GetAttribute("value") ?? "";
        }

        public string GetBsn()
        {
            return Driver.FindElement(_bsn)
                .GetAttribute("value") ?? "";
        }

        private void EnterText(By locator, string value)
        {
            var element = CreateWait().Until(d =>
            {
                var found = d.FindElement(locator);

                return found.Displayed && found.Enabled
                    ? found
                    : null;
            });

            element.Clear();
            element.SendKeys(value);
        }

        private void SetDateOfBirth(string isoDate)
        {
            var element = Driver.FindElement(_dateOfBirth);

            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "arguments[0].value = arguments[1];" +
                "arguments[0].dispatchEvent(" +
                "new Event('input', { bubbles: true }));" +
                "arguments[0].dispatchEvent(" +
                "new Event('change', { bubbles: true }));",
                element,
                isoDate
            );
        }

        private void WaitForMessage(By locator, string expectedMessage)
        {
            CreateWait().Until(d =>
                d.FindElements(locator).Any(e =>
                    e.Displayed &&
                    e.Text.Contains(expectedMessage)
                )
            );
        }
    }
}
