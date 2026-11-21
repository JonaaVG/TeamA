using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace GentleHealersApi.Tests.PageObjects
{
    public abstract class BasePage
    {
        protected readonly IWebDriver Driver;

        protected BasePage(IWebDriver driver)
        {
            Driver = driver;
        }

        protected WebDriverWait CreateWait()
        {
            return new WebDriverWait(
                Driver,
                TimeSpan.FromSeconds(10)
            );
        }
    }
}
