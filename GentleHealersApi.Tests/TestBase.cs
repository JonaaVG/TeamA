using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using GentleHealersApi.Tests.Helpers;
namespace GentleHealersApi.Tests
{
    public class TestBase
    {
        protected IWebDriver _driver = null!;
        protected TestLogger _logger = null!;

        public TestContext TestContext { get; set; } = null!;

        protected const string BaseUrl = "http://localhost:5173";

        [TestInitialize]
        public void Setup()
        {
            _logger = new TestLogger(
                TestContext,
                GetType()
            );

            try
            {
                _logger.Info("Testomgeving wordt gestart.");

                _driver = new ChromeDriver();
                _driver.Manage().Window.Maximize();

                _logger.Info("Chrome browser succesvol gestart.");

                _driver.Navigate().GoToUrl(BaseUrl);

                _logger.Info(
                    $"Applicatie succesvol geopend: {BaseUrl}"
                );
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Fout tijdens het starten van de test."
                );

                try
                {
                    _driver?.Quit();
                    _driver?.Dispose();
                }
                catch (Exception cleanupException)
                {
                    _logger.Error(
                        cleanupException,
                        "Fout tijdens het afsluiten van de browser na een mislukte Setup."
                    );
                }

                throw;
            }
        }

        [TestCleanup]
public void Cleanup()
{
    try
    {
        if (_logger != null && TestContext != null)
        {
            _logger.LogResult(
                TestContext.CurrentTestOutcome == UnitTestOutcome.Passed
            );
        }

        _driver?.Quit();

        _logger?.Info("Chrome browser succesvol afgesloten.");
    }
    catch (Exception ex)
    {
        _logger?.Error(ex, "Fout tijdens het opruimen van de test.");
    }
    finally
    {
        try
        {
            _driver?.Dispose();
        }
        finally
        {
            _logger?.Close();
        }
    }
}
    }
}