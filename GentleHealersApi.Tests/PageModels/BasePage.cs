namespace GentleHealersApi.PageModels;

using OpenQA.Selenium;
internal class BasePage
{
    protected IWebDriver Driver { get; }
    // Constructor Injection: Driver instance is passed from the test class
    public BasePage(IWebDriver driver)
    {
        Driver = driver;
        // Selenium's PageFactory initialization is often done here too
    }
}