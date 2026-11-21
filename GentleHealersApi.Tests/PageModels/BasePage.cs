using OpenQA.Selenium;

namespace GentleHealersApi.Tests.PageModels;

/// <summary>Provides the WebDriver shared by page-specific Selenium page models.</summary>
internal abstract class BasePage
{
    protected IWebDriver Driver { get; }

    protected BasePage(IWebDriver driver)
    {
        Driver = driver;
    }
}
