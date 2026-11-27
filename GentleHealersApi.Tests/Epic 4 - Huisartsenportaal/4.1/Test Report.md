# Epic 4.1 Test Report

**Date:** 2026-11-27  
**Result:** Passed

## Executed tests

- `PatientControllerSearchTests`: **4 passed, 0 failed**
  - Coverage: matching patients, no matching patients, invalid search input, and forbidden access.
  - Uses mocked services; does not access the API or database.
- `US_4_1_PatientReferralSeleniumTests`: **4 passed, 0 failed**
  - Coverage: searching by name, selecting from the patient list, searching with an alternative spelling, and handling a patient that does not exist.
  - Runs through the browser against the local application and test database.

**Controller test command:**

```powershell
dotnet test GentleHealersApi.Tests\GentleHealersApi.Tests.csproj --filter FullyQualifiedName~PatientControllerSearchTests
```

**Selenium test command:**

```powershell
dotnet test GentleHealersApi.Tests\GentleHealersApi.Tests.csproj --filter FullyQualifiedName~US_4_1_PatientReferralSeleniumTests
```
