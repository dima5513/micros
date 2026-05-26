using Micros.Api.Infrastructure.Authorize;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Micros.Api.Domains.User;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUser _currentUser;

    public UserController(IUserService userService, ICurrentUser currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [Route("me")]
    [HttpGet]
    [ProducesResponseType<UserResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        var user = await _userService.GetById(_currentUser.Id);

        return Ok(new UserResponseDTO(user.Id, user.Username, user.Email, user.TelegramId, user.CreatedAt, user.UpdatedAt));
    }

    [Route("me")]
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize]
    public async Task<IActionResult> UpdateCurrentUserAsync([FromBody] UpdateMeRequestDTO dto)
    {
        await _userService.UpdateMeAsync(new UpdateMeContract(_currentUser.Id, dto.TelegramId));

        return NoContent();
    }
}