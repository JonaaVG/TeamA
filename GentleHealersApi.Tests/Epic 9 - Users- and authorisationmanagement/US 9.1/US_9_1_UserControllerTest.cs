// Unit tests for UserController (account creation + activation email)

using System.Security.Claims;
using GentleHealersApi.DTOs;
using GentleHealersApi.DTOs.User;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using GentleHealersApi.Controllers;
using GentleHealersApi.Exceptions;
namespace GentleHealersApi.Tests;

[TestClass]
public class UserControllerTests
{
    private const int GpUserId = 42;

    private Mock<IUserService> _users = null!;
    private Mock<IEmailService> _email = null!;
    private Mock<ILogger<UserController>> _logger = null!;

    private static CreateUserDto MakeDto(string roleName = "Patient") => new()
    {
        FirstName = "John",
        LastName = "Smith",
        Email = "john.smith@example.com",
        RoleName = roleName
    };

    private static UserCreatedDto MakeCreated() => new()
    {
        UserId = 99,
        FirstName = "John",
        LastName = "Smith",
        Email = "john.smith@example.com",
        ActivationLink = "https://portal.test/activeer?token=raw&email=john.smith%40example.com"
    };

    private UserCreatedDto _created = null!;

    [TestInitialize]
    public void Setup()
    {
        _users = new Mock<IUserService>();
        _email = new Mock<IEmailService>();
        _logger = new Mock<ILogger<UserController>>();
        _created = MakeCreated();

        _users.Setup(u => u.CreateUserAsync(It.IsAny<CreateUserDto>(), GpUserId)).ReturnsAsync(_created);
        _email.Setup(e => e.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
    }

    private UserController CreateController(string? userId = "42")
    {
        var claims = new List<Claim>();
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        return new UserController(_users.Object, _email.Object, _logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            }
        };
    }

    private void VerifyNoEmailSent()
        => _email.Verify(e => e.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()), Times.Never);

    [TestMethod]
    public async Task CreateUser_Success_Returns201WithLocation()
    {
        var dto = MakeDto();

        var result = await CreateController().CreateUser(dto);

        var created = result.Result as CreatedResult;
        Assert.IsNotNull(created);
        Assert.AreEqual("/api/users/99", created.Location);
        Assert.AreSame(_created, created.Value);
        _users.Verify(u => u.CreateUserAsync(dto, GpUserId), Times.Once);
    }

    [TestMethod]
    [DataRow("Patient")]
    [DataRow("GeneralPractitioner")]
    public async Task CreateUser_Success_SendsActivationEmailForTheCreatedUser(string roleName)
    {
        EmailRequest? sent = null;
        _email.Setup(e => e.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
              .Callback<EmailRequest, CancellationToken>((r, _) => sent = r)
              .Returns(Task.CompletedTask);

        await CreateController().CreateUser(MakeDto(roleName));

        _email.Verify(e => e.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsNotNull(sent);
        Assert.AreEqual(EmailType.Activation, sent.Type);
        Assert.AreEqual(_created.Email, sent.ToEmail);
        Assert.AreEqual(_created.FirstName, sent.FirstName);
        Assert.AreEqual(_created.ActivationLink, sent.Link);
        Assert.AreEqual(roleName, sent.RoleName);
    }

    [TestMethod]
    public async Task CreateUser_EmailDeliveryFails_Returns503()
    {
        _users.Setup(u => u.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<int>()))
              .ThrowsAsync(new EmailDeliveryException("De activatiemail kon niet worden verstuurd.", new Exception()));

        var result = await CreateController().CreateUser(MakeDto());

        var status = result.Result as ObjectResult;
        Assert.IsNotNull(status);
        Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
    }

    [TestMethod]
    public async Task CreateUser_UnknownRole_Returns400_AndSendsNoEmail()
    {
        _users.Setup(u => u.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<int>()))
              .ThrowsAsync(new ArgumentException("De rol 'X' bestaat niet."));

        var result = await CreateController().CreateUser(MakeDto("X"));

        Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        VerifyNoEmailSent();
    }

    [TestMethod]
    public async Task CreateUser_EmailAlreadyExists_Returns409_AndSendsNoEmail()
    {
        _users.Setup(u => u.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<int>()))
              .ThrowsAsync(new InvalidOperationException("Er bestaat al een account met dit e-mailadres."));

        var result = await CreateController().CreateUser(MakeDto());

        Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));
        VerifyNoEmailSent();
    }

    [TestMethod]
    public async Task CreateUser_MissingUserIdClaim_ThrowsUnauthorizedAccessException()
    {
        var controller = CreateController(userId: null);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => controller.CreateUser(MakeDto()));

        Assert.AreEqual("Invalid or missing user identity claim.", ex.Message);
        VerifyNoEmailSent();
    }

    [TestMethod]
    public async Task CreateUser_EmailDeliveryFailsInService_Returns503()
    {
        // Email delivery failure originates from IUserService.CreateUserAsync
        _users.Setup(u => u.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<int>()))
              .ThrowsAsync(new EmailDeliveryException("De activatiemail kon niet worden verstuurd.", new Exception()));

        var result = await CreateController().CreateUser(MakeDto());

        var status = result.Result as ObjectResult;
        Assert.IsNotNull(status);
        Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
    }
}