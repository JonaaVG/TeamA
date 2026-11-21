using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests.PageObjects
{
    public class LoginPage : BasePage
    {
        private readonly By _email = By.Id("email");
        private readonly By _password = By.Id("password");
        private readonly By _submitButton = By.CssSelector("button[type='submit']");
        private readonly By _errorAlert = By.CssSelector(".alert-danger, .error-message, [role='alert']");

        public LoginPage(IWebDriver driver) : base(driver)
        {
        }

        public void Login(string identifier, string password)
        {
            var wait = CreateWait();
            wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));

            // Wait until the email input is visible on the login form
            var emailField = wait.Until(d =>
            {
                var elements = d.FindElements(_email);
                return elements.FirstOrDefault(e => e.Displayed);
            }) ?? throw new NoSuchElementException("Email input (#email) was not visible.");

            var passwordField = Driver.FindElement(_password);
            var submitBtn = Driver.FindElement(_submitButton);

            emailField.Clear();
            emailField.SendKeys(identifier);

            passwordField.Clear();
            passwordField.SendKeys(password);

            submitBtn.Click();

            // The form may be replaced while the router navigates after login.
            // Do not inspect its elements here; wait only for a stable login outcome.
            wait.Until(driver =>
            {
                try
                {
                    return HasStoredJwtToken(driver) || HasErrorMessage();
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });
        }

        public bool HasErrorMessage()
        {
            return Driver.FindElements(_errorAlert).Any(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));
        }

        public string GetErrorMessage()
        {
            var element = Driver.FindElements(_errorAlert).FirstOrDefault(e => e.Displayed);
            return element?.Text.Trim() ?? string.Empty;
        }

        private static bool HasStoredJwtToken(IWebDriver driver)
        {
            if (driver is not IJavaScriptExecutor js)
            {
                return false;
            }

            var token = js.ExecuteScript("return window.localStorage.getItem('token');") as string;
            return !string.IsNullOrWhiteSpace(token);
        }
    }
}
