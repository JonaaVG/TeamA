namespace GentleHealersApi.Tests.Epic4;

[TestClass]
public sealed class ReferralTreatmentCodeTests : ReferralTreatmentBrowserTest
{
    [TestMethod]
    [TestCategory("TC-4.2.1-01")]
    public void SaveOneTreatmentCode()
    {
        var code = Codes[0];
        OpenReferral();
        AddTreatment(code);

        var saved = SubmitReferral();

        Assert.AreEqual(1, saved.Count, "The referral should contain exactly one treatment.");
        Assert.AreEqual(code.Code, saved.Single().Code);
    }

    [TestMethod]
    [TestCategory("TC-4.2.1-02")]
    public void SaveThreeDifferentCodesOnOneReferral()
    {
        OpenReferral();
        foreach (var code in Codes)
        {
            AddTreatment(code);
        }

        var saved = SubmitReferral();

        CollectionAssert.AreEquivalent(
            Codes.Select(code => code.Code).ToArray(),
            saved.Select(treatment => treatment.Code).ToArray());
        Assert.AreEqual(1, saved.Select(treatment => treatment.ReferralId).Distinct().Count(),
            "All selected treatment codes should be saved on one referral.");
    }

    [TestMethod]
    [TestCategory("TC-4.2.1-03")]
    public void PreventARepeatedTreatmentCode()
    {
        var code = Codes[0];
        OpenReferral();
        AddTreatment(code);

        Assert.IsFalse(AvailableTreatmentCodes.Contains(code.Code),
            "A treatment that is already on the referral should be unavailable for a second selection.");
        Assert.AreEqual(1, Drafts().Count);

        var saved = SubmitReferral();

        Assert.AreEqual(1, saved.Count, "The treatment must be saved only once.");
        Assert.AreEqual(code.Code, saved.Single().Code);
    }

    [TestMethod]
    [TestCategory("TC-4.2.1-04")]
    public void RejectAnUnavailableTreatmentCode()
    {
        const string unavailableCode = "NIE03";
        var validCode = Codes[0];
        OpenReferral();

        Assert.IsFalse(AvailableTreatmentCodes.Contains(unavailableCode),
            "An unavailable code should never be offered as a valid selection.");
        SaveDraft();
        Assert.IsEmpty(Drafts(), "An unavailable code should not create a draft row.");
        AssertEmptyReferralRejected();

        AddTreatment(validCode);
        var saved = SubmitReferral();

        Assert.AreEqual(1, saved.Count);
        Assert.AreEqual(validCode.Code, saved.Single().Code);
    }

    [TestMethod]
    [TestCategory("TC-4.2.2-01")]
    public void StoreAnAdjustedDuration()
    {
        var code = Codes[0];
        OpenReferral();
        ChooseTreatment(code);
        Assert.AreEqual(30, CurrentDuration, "The selected code should initially use its catalogue duration.");
        SetDuration(45);
        SaveDraft();

        var treatment = SubmitReferral().Single();

        Assert.AreEqual(code.Code, treatment.Code);
        Assert.AreEqual(45, treatment.DurationMinutes);
    }

    [TestMethod]
    [TestCategory("TC-4.2.2-02")]
    public void StoreSeparateDurationsForSeparateTreatments()
    {
        var first = Codes[0];
        var second = Codes[1];
        OpenReferral();

        ChooseTreatment(first);
        Assert.AreEqual(30, CurrentDuration);
        SetDuration(45);
        SaveDraft();

        ChooseTreatment(second);
        Assert.AreEqual(45, CurrentDuration);
        SetDuration(60);
        SaveDraft();

        var saved = SubmitReferral();

        Assert.AreEqual(2, saved.Count);
        Assert.AreEqual(45, saved.Single(treatment => treatment.Code == first.Code).DurationMinutes);
        Assert.AreEqual(60, saved.Single(treatment => treatment.Code == second.Code).DurationMinutes);
    }

    [TestMethod]
    [TestCategory("TC-4.2.2-03")]
    public void PreserveAValidDurationWhenInvalidInputIsUnavailable()
    {
        var first = Codes[0];
        var second = Codes[1];
        OpenReferral();
        AddTreatment(first, minutes: 45);
        AddTreatment(second, minutes: 45);

        // A selection-only field prevents typing text or choosing zero minutes.
        foreach (var code in new[] { first, second })
        {
            EditDraft(code);
            Assert.IsTrue(AvailableDurations.Count > 0);
            Assert.IsTrue(AvailableDurations.All(minutes => minutes > 0),
                "Only positive numeric durations may appear in the selection field.");
            Assert.AreEqual(45, CurrentDuration);
            SaveDraft();
        }

        var saved = SubmitReferral();

        Assert.AreEqual(2, saved.Count);
        Assert.IsTrue(saved.All(treatment => treatment.DurationMinutes == 45),
            "The valid durations should remain in the saved referral.");
    }

    [TestMethod]
    [TestCategory("TC-4.2.2-04")]
    public void KeepAnAdjustedDurationOnItsOwnReferral()
    {
        var code = Codes[0];
        OpenReferral();
        AddTreatment(code, minutes: 45);
        var first = SubmitReferral().Single();

        var secondPatient = NewPatient();
        OpenReferral(secondPatient);
        ChooseTreatment(code);
        Assert.AreEqual(code.StandardMinutes, CurrentDuration,
            "The code's catalogue duration should still apply to a separate referral.");
        SaveDraft();
        var second = SubmitReferral(secondPatient).Single();

        Assert.AreNotEqual(first.ReferralId, second.ReferralId);
        Assert.AreEqual(code.Code, first.Code);
        Assert.AreEqual(code.Code, second.Code);
        Assert.AreEqual(45, first.DurationMinutes);
        Assert.AreEqual(code.StandardMinutes, second.DurationMinutes);
    }

    [TestMethod]
    [TestCategory("TC-4.2.3-01")]
    public void PatientCanBookOnlyTheSelectedTreatment()
    {
        var first = Codes[0];
        var second = Codes[1];
        OpenReferral();
        AddTreatment(first);
        AddTreatment(second);
        var saved = SubmitReferral();

        Assert.AreEqual(2, saved.Count);
        CheckPatientBooking(
            saved.Single(treatment => treatment.Code == first.Code),
            saved.Single(treatment => treatment.Code == second.Code));
    }
}
