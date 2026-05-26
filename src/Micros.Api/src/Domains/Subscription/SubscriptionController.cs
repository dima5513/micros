using Micros.Api.Domains.Common.Exceptions;
using Micros.Api.Infrastructure.Authorize;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Micros.Api.Domains.Subscription;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }


    [Authorize]
    [HttpPost]
    [ProducesResponseType<SubscriptionResponseDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateSubscriptionDTO dto,
        [FromServices] ICurrentUser currentUser)
    {
        var subscription = await _subscriptionService.CreateAsync(
            new CreateSubscriptionContract(dto.Url, currentUser.Id)
        );

        return StatusCode(StatusCodes.Status201Created, new SubscriptionResponseDTO(
            subscription.Id,
            subscription.UserId,
            subscription.Url,
            subscription.CreatedAt)
        );
    }

    [Authorize]
    [HttpDelete("{subscriptionId}")]
    [ProducesResponseType<SubscriptionResponseDTO>(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAsync(Guid subscriptionId, [FromServices] ICurrentUser currentUser)
    {
        await _subscriptionService.DeleteAsync(
            subscriptionId,
            currentUser.Id
        );

        return NoContent();
    }


    [Authorize]
    [HttpGet]
    [ProducesResponseType<List<SubscriptionResponseDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromServices] ICurrentUser currentUser)
    {
        var subscriptions = await _subscriptionService.GetForCurrentUserAsync(currentUser.Id);

        return Ok(subscriptions.Select(s => new SubscriptionResponseDTO(
            s.Id,
            s.UserId,
            s.Url,
            s.CreatedAt))
        );
    }
}