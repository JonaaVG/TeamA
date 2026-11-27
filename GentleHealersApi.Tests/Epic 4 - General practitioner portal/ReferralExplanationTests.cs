using OpenQA.Selenium;

namespace GentleHealersApi.Tests.Epic4;

[TestClass]
public sealed class ReferralExplanationTests : ReferralTreatmentBrowserTest
{
    [TestMethod]
    [TestCategory("TC-4.3.1-01")]
    public void ReferralSavesTheExplanationForTheTreatingSpecialist()
    {
        const string note = "Pijn in rechterknie sinds 3 weken. Graag beoordeling.";
        var treatment = Codes[0];

        OpenReferral();
        AddTreatment(treatment, note: note);
        EditDraft(treatment);

        Assert.AreEqual(note, CurrentNote, "Reopening the draft should show the original explanation.");

        var savedTreatment = SubmitReferral().Single();
        Assert.AreEqual(treatment.Code, savedTreatment.Code);
        Assert.AreEqual(note, savedTreatment.Note, "Sending should persist the explanation with its treatment code.");
        CheckSpecialistNote(savedTreatment, note);
    }

    [TestMethod]
    [TestCategory("TC-4.3.1-02")]
    public void ReferralCanBeSentWithoutAnExplanation()
    {
        var treatment = Codes[0];

        OpenReferral();
        AddTreatment(treatment);

        var savedTreatment = SubmitReferral().Single();
        Assert.AreEqual(treatment.Code, savedTreatment.Code);
        Assert.IsNull(savedTreatment.Note, "An optional explanation left blank should not be stored.");
        CheckSpecialistNote(savedTreatment, null);
    }

    [TestMethod]
    [TestCategory("TC-4.3.1-03")]
    public void ReferralKeepsTheEntireFiveHundredCharacterExplanation()
    {
        const string fragment = "De patiënt bespreekt de klachten tijdens de afspraak. ";
        var note = string.Concat(Enumerable.Repeat(fragment, 12))[..499] + "!";
        var treatment = Codes[0];

        OpenReferral();
        AddTreatment(treatment, note: note);
        EditDraft(treatment);

        Assert.AreEqual(note, CurrentNote, "The last character should remain visible when reopening the explanation.");

        var savedTreatment = SubmitReferral().Single();
        Assert.AreEqual(500, savedTreatment.Note?.Length);
        Assert.AreEqual(note, savedTreatment.Note, "No characters should be truncated or replaced.");
        CheckSpecialistNote(savedTreatment, note);
    }

    [TestMethod]
    [TestCategory("TC-4.3.1-04")]
    public void ReferralDisplaysAccentsLineBreaksAndInertMarkupAsText()
    {
        const string note = "Patiënt meldt pijn bij de knie.\nRegel twee: café en coöperatie.\n<script type=\"text/plain\">Voorbeeldtekst</script>";
        var treatment = Codes[0];

        OpenReferral();
        AddTreatment(treatment, note: note);

        Assert.AreEqual(note, Drafts().Single().Note.ReplaceLineEndings("\n"),
            "The draft should preserve the accents, line breaks and literal markup.");
        Assert.AreEqual(0, Browser.FindElements(By.CssSelector("tbody td script")).Count,
            "The explanation should render as text without inserting a script element.");

        var savedTreatment = SubmitReferral().Single();
        Assert.AreEqual(note, savedTreatment.Note?.ReplaceLineEndings("\n"),
            "Persistence should preserve the accents, line breaks and literal markup.");
        CheckSpecialistNote(savedTreatment, note);
    }

    [TestMethod]
    [TestCategory("TC-4.3.1-05")]
    public void ReferralTreatsAWhitespaceOnlyExplanationAsEmpty()
    {
        var treatment = Codes[0];

        OpenReferral();
        AddTreatment(treatment, note: "     ");

        var savedTreatment = SubmitReferral().Single();
        Assert.AreEqual(treatment.Code, savedTreatment.Code);
        Assert.IsNull(savedTreatment.Note, "An explanation with only spaces should not be stored.");
        CheckSpecialistNote(savedTreatment, null);
    }
}
