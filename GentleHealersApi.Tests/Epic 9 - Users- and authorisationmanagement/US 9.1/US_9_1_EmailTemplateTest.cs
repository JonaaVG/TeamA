// Unit tests for EmailTemplates (activation / password reset, patient / staff)

using System.Net;
using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Email;

namespace GentleHealersApi.Tests;

[TestClass]
public class EmailTemplatesTests
{
    private const string Link = "https://portal.test/activeer?token=raw&email=a%40b.nl";

    private static EmailRequest Request(EmailType type, string role = "Patient", string name = "John")
        => new(type, "john@example.com", name, Link, role);

    [TestMethod]
    public void Activation_Patient_UsesPatientPortalSubject_AndContainsLink()
    {
        var (subject, html, text) = EmailTemplates.Build(Request(EmailType.Activation));

        Assert.AreEqual("Uitnodiging voor het patiëntenportaal", subject);
        Assert.IsTrue(html.Contains(WebUtility.HtmlEncode(Link)));
        Assert.IsTrue(text.Contains(Link));
    }

    [TestMethod]
    public void Activation_Staff_UsesStaffSubject()
    {
        var (subject, _, _) = EmailTemplates.Build(Request(EmailType.Activation, role: "GeneralPractitioner"));

        Assert.AreEqual("Activeer uw account", subject);
    }

    [TestMethod]
    public void Activation_RoleNameIsCaseInsensitive()
    {
        var (subject, _, _) = EmailTemplates.Build(Request(EmailType.Activation, role: "patient"));

        Assert.AreEqual("Uitnodiging voor het patiëntenportaal", subject);
    }

    [TestMethod]
    public void PasswordReset_UsesResetSubject_AndContainsLink()
    {
        var (subject, html, text) = EmailTemplates.Build(Request(EmailType.PasswordReset));

        Assert.AreEqual("Wachtwoord wijzigen", subject);
        Assert.IsTrue(html.Contains(WebUtility.HtmlEncode(Link)));
        Assert.IsTrue(text.Contains(Link));
    }

    [TestMethod]
    public void Build_GreetsRecipientByFirstName()
    {
        var (_, html, text) = EmailTemplates.Build(Request(EmailType.Activation, name: "Verena"));

        Assert.IsTrue(html.Contains("Beste Verena"));
        Assert.IsTrue(text.Contains("Beste Verena"));
    }

    [TestMethod]
    public void Build_HtmlEncodesName()
    {
        var (_, html, _) = EmailTemplates.Build(Request(EmailType.Activation, name: "<script>x</script>"));

        Assert.IsFalse(html.Contains("<script>"));
    }

    [TestMethod]
    public void Build_UnknownType_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => EmailTemplates.Build(Request((EmailType)999)));

    // Extra tests to add to EmailTemplatesTests

    [TestMethod]
    public void PasswordReset_HtmlEncodesAmpersandInLink()
    {
        var (_, html, text) = EmailTemplates.Build(Request(EmailType.PasswordReset));

        Assert.IsTrue(html.Contains(WebUtility.HtmlEncode(Link)));
        Assert.IsTrue(text.Contains(Link));
    }

    [TestMethod]
    public void Activation_OtherStaffRole_UsesStaffSubject()
    {
        var (subject, _, _) = EmailTemplates.Build(Request(EmailType.Activation, role: "Admin"));

        Assert.AreEqual("Activeer uw account", subject);
    }

    [TestMethod]
    public void Build_BlankFirstName_DoesNotThrow()
    {
        var (_, html, text) = EmailTemplates.Build(Request(EmailType.Activation, name: " "));

        Assert.IsFalse(string.IsNullOrWhiteSpace(html));
        Assert.IsFalse(string.IsNullOrWhiteSpace(text));
    }
}
