using Microsoft.AspNetCore.Mvc;
using GentleHealersApi.Services.Interfaces;
using GentleHealersApi.DTOs.User;
using GentleHealersApi.Extensions;

namespace RevaliNowApi.Controllers
{
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(UserCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserCreatedDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        try
        {
            int userId = User.GetUserIdFromClaims();
            var created = await _userService.CreateUserAsync(dto, userId);
            return Created($"/api/users/{created.UserId}", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
}