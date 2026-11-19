using GentleHealersApi.Helpers;
using MmaTests.PageModels;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Reflection;

namespace GentleHealersApi.Tests;

[TestClass]
public class US1_1_BasisLogin : BaseSeleniumTest
{
    [TestMethod]
    public void User_Can_Successfully_LogIn_Valid_Credentials()
    {

        // navigate to the login page
        Driver.Navigate().GoToUrl("http://localhost:5173");

        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));

        // find the username and password fields and enter valid credentials
        var loginPage = new LogInPage(Driver);
        loginPage.EnterCredentials("jan.jansen@example.com", "Welkom123!");

        var username_span = Driver.FindElement(By.Name("user_name"));
        wait.Until(d => d.FindElements(By.XPath("//span[text()='De Zachte Heelmeesters']")).Count > 0);
        var page_title = Driver.FindElement(By.Id("page-title"));
        Assert.IsNotEmpty(page_title.Text);
    }
}
