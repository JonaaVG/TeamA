# Epic 4.1 Test Report

**Updated:** 2026-11-27

## Test coverage

### AC 4.1.1 – Patiënt opzoeken

`US_4_1_PatientReferralSeleniumTests` covers the four requested cases:

- `TC-1.1.1-01-Zoekbalk`: search for a patient by name, verify identifying details, and save a referral linked to that patient.
- `TC-1.1.1-02-Patientenlijst`: select a patient from the list and verify the saved referral belongs to that patient.
- `TC-1.1.1-03-Alternatieve-schrijfwijze`: search for “Sanne Berg” and verify the “Sanne van der Berg” result and referral linkage.
- `TC-1.1.1-04-Niet-bestaande-patient`: verify an unknown patient returns no results and creates no referral.

### AC 4.1.2 – Alleen eigen praktijk

`US_4_1_PracticeScopeSeleniumTests` covers the three requested cases:

- `TC-1.1.1-01-Eigen-praktijk`: find and refer a patient from the doctor's own practice.
- `TC-1.1.1-02-Andere-praktijk`: verify a patient from another practice is absent from search results.
- `TC-1.1.1-03-Andere-praktijk-weigeren`: attempt a direct referral for a patient from another practice and verify the API rejects it and stores no referral.

`PatientControllerSearchTests` separately covers the search endpoint response for matching patients, no matches, invalid queries, and forbidden access.

## Latest verification

The test data is retained between runs. The page object identifies a patient using its generated unique BSN, while the tests still verify name and date of birth. The API filters GP searches and referral creation to the authenticated GP's practice, so records from other practices cannot contaminate later runs or be used for a referral.

Both API and test projects compile successfully. Restart the API before rerunning Selenium so it loads the practice-scope checks. The updated end-to-end result still needs confirmation.

Earlier Selenium attempts in this environment did not complete consistently: one was blocked by a running API file lock and Chrome startup failures. The latest user-provided run reached assertions and exposed duplicate name/date matches plus missing practice checks; the changes above address those failures. Selenium has not been rerun after these changes because each run adds persistent test data.

To rerun when ChromeDriver can start and the test environment is available:

```powershell
dotnet test GentleHealersApi.Tests\GentleHealersApi.Tests.csproj --filter "FullyQualifiedName~US_4_1_PatientReferralSeleniumTests|FullyQualifiedName~US_4_1_PracticeScopeSeleniumTests"
```
