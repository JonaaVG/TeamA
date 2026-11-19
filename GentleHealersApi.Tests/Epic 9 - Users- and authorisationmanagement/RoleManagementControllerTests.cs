using System.Reflection;
using GentleHealersApi.Controllers;
using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GentleHealersApi.Tests;

[TestClass]
public sealed class RoleManagementControllerTests
{
    private Mock<IRoleService> _roleService = null!;
    private RoleMangementController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _roleService = new Mock<IRoleService>(MockBehavior.Strict);
        _controller = new RoleMangementController(_roleService.Object);
    }

    [TestMethod]
    public async Task GetRoles_ReturnsRolesFromService()
    {
        var roles = new List<RoleDto>
        {
            new() { RoleId = 2, Name = "Specialist" },
            new() { RoleId = 1, Name = "Patient" },
        };
        _roleService.Setup(service => service.GetAllRolesAsync()).ReturnsAsync(roles);

        var action = await _controller.GetRoles();

        var result = action.Result as OkObjectResult;
        Assert.IsNotNull(result);
        Assert.AreSame(roles, result.Value);
        _roleService.Verify(service => service.GetAllRolesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task GetUsers_ReturnsUsersFromService()
    {
        var users = new List<UserRoleDto>
        {
            new() { UserId = 7, FirstName = "Hendrik", LastName = "Jan", Email = "hendrik.jan@example.com", RoleId = 2, RoleName = "Specialist", Status = "Active" },
        };
        _roleService.Setup(service => service.GetAllUsersAsync()).ReturnsAsync(users);

        var action = await _controller.GetUsers();

        var result = action.Result as OkObjectResult;
        Assert.IsNotNull(result);
        Assert.AreSame(users, result.Value);
        _roleService.Verify(service => service.GetAllUsersAsync(), Times.Once);
    }

    [TestMethod]
    public async Task ChangeUserRole_WhenServiceChangesRole_ReturnsOk()
    {
        var request = new RoleDto { RoleId = 3, Name = "SystemAdministrator" };
        _roleService.Setup(service => service.ChangeRoleAsync(7, 3)).ReturnsAsync(true);

        var result = await _controller.ChangeUserRole(7, request);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual("Rol succesvol gewijzigd.", ok.Value);
        _roleService.Verify(service => service.ChangeRoleAsync(7, 3), Times.Once);
    }

    [TestMethod]
    public async Task ChangeUserRole_WhenUserOrRoleDoesNotExist_ReturnsNotFound()
    {
        _roleService.Setup(service => service.ChangeRoleAsync(7, 999)).ReturnsAsync(false);

        var result = await _controller.ChangeUserRole(7, new RoleDto { RoleId = 999 });

        var notFound = result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual("Gebruiker of rol niet gevonden.", notFound.Value);
        _roleService.Verify(service => service.ChangeRoleAsync(7, 999), Times.Once);
    }

    [TestMethod]
    public async Task DeactivateUser_WhenServiceDeactivatesAccount_ReturnsOk()
    {
        _roleService.Setup(service => service.DeactivateUserAsync(7)).ReturnsAsync(true);

        var result = await _controller.DeactivateUser(7);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual("Account gedeactiveerd.", ok.Value);
        _roleService.Verify(service => service.DeactivateUserAsync(7), Times.Once);
    }

    [TestMethod]
    public async Task DeactivateUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        _roleService.Setup(service => service.DeactivateUserAsync(999)).ReturnsAsync(false);

        var result = await _controller.DeactivateUser(999);

        var notFound = result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual("Gebruiker niet gevonden.", notFound.Value);
    }

    [TestMethod]
    public async Task ActivateUser_WhenServiceActivatesAccount_ReturnsOk()
    {
        _roleService.Setup(service => service.ActivateUserAsync(7)).ReturnsAsync(true);

        var result = await _controller.ActivateUser(7);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual("Account geactiveerd.", ok.Value);
        _roleService.Verify(service => service.ActivateUserAsync(7), Times.Once);
    }

    [TestMethod]
    public async Task ActivateUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        _roleService.Setup(service => service.ActivateUserAsync(999)).ReturnsAsync(false);

        var result = await _controller.ActivateUser(999);

        var notFound = result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual("Gebruiker niet gevonden.", notFound.Value);
    }

    [TestMethod]
    public void Controller_RequiresSystemAdministratorRole()
    {
        var attribute = typeof(RoleMangementController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.IsNotNull(attribute);
        Assert.AreEqual("SystemAdministrator", attribute.Roles);
    }
}
