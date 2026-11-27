using System.Security.Claims;
using GentleHealersApi.Controllers;
using GentleHealersApi.DTOs.Patient;
using GentleHealersApi.Services.Interfaces;
using GentleHealersApi.Services.Interfaces.Patient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace GentleHealersApi.Tests.Epic4.US_4_1.ControllerTests;

/// <summary>Unit tests for the patient search endpoint on PatientController.</summary>
[TestClass]
public sealed class PatientControllerSearchTests
{
    private const int PractitionerUserId = 42;

    private Mock<IPatientService> _patientService = null!;
    private PatientController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _patientService = new Mock<IPatientService>(MockBehavior.Strict);

        var registrationService = new Mock<IPatientRegistrationService>(MockBehavior.Strict);
        var emailService = new Mock<IEmailService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<PatientController>>();

        _controller = new PatientController(
            _patientService.Object,
            registrationService.Object,
            emailService.Object,
            logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, PractitionerUserId.ToString())],
                    authenticationType: "UnitTest"))
                }
            }
        };
    }

    [TestMethod]
    public async Task Search_WhenPatientsMatch_ReturnsPatientsAndPassesQueryAndUserIdToService()
    {
        const string query = "Sanne Berg";
        IEnumerable<PatientSearchResultDto> patients =
        [
            new PatientSearchResultDto
            {
                PatientId = 17,
                FirstName = "Sanne",
                LastName = "van der Berg",
                Bsn = "111222333",
                DateOfBirth = new DateOnly(1985, 6, 14)
            }
        ];
        _patientService
            .Setup(service => service.SearchPatientsAsync(query, PractitionerUserId))
            .ReturnsAsync(patients);

        var action = await _controller.Search(query);

        var result = action.Result as OkObjectResult;
        Assert.IsNotNull(result);
        Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
        Assert.AreSame(patients, result.Value);
        _patientService.Verify(
            service => service.SearchPatientsAsync(query, PractitionerUserId),
            Times.Once);
    }

    [TestMethod]
    public async Task Search_WhenNoPatientsMatch_ReturnsAnEmptyList()
    {
        const string query = "Niet bestaande patiënt";
        IEnumerable<PatientSearchResultDto> patients = Array.Empty<PatientSearchResultDto>();
        _patientService
            .Setup(service => service.SearchPatientsAsync(query, PractitionerUserId))
            .ReturnsAsync(patients);

        var action = await _controller.Search(query);

        var result = action.Result as OkObjectResult;
        Assert.IsNotNull(result);
        Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
        Assert.AreSame(patients, result.Value);
        _patientService.Verify(
            service => service.SearchPatientsAsync(query, PractitionerUserId),
            Times.Once);
    }

    [TestMethod]
    public async Task Search_WhenServiceRejectsQuery_ReturnsBadRequest()
    {
        const string query = "invalid";
        _patientService
            .Setup(service => service.SearchPatientsAsync(query, PractitionerUserId))
            .ThrowsAsync(new ArgumentException("De zoekopdracht is ongeldig."));

        var action = await _controller.Search(query);

        var result = action.Result as BadRequestObjectResult;
        Assert.IsNotNull(result);
        Assert.AreEqual(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.AreEqual(
            "De zoekopdracht is ongeldig.",
            result.Value?.GetType().GetProperty("message")?.GetValue(result.Value));
    }

    [TestMethod]
    public async Task Search_WhenServiceDeniesAccess_ReturnsForbidden()
    {
        const string query = "Pieter";
        _patientService
            .Setup(service => service.SearchPatientsAsync(query, PractitionerUserId))
            .ThrowsAsync(new UnauthorizedAccessException("Geen toegang tot patiënten."));

        var action = await _controller.Search(query);

        var result = action.Result as ObjectResult;
        Assert.IsNotNull(result);
        Assert.AreEqual(StatusCodes.Status403Forbidden, result.StatusCode);
    }
}
