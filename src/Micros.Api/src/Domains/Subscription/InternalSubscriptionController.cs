using Micros.Api.Infrastructure.Authorize;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Micros.Api.Domains.Subscription;

[ApiController]
[Route("/api/internal/subscriptions")]
[Authorize(AuthenticationSchemes = AppAuthorize.S2sScheme)]
public class InternalSubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public InternalSubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }


    [HttpGet]
    [ProducesResponseType<List<SubscriptionWithTelegramIdResponseDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionsWithTelegramId()
    {
        var subscriptions = await _subscriptionService.GetAllWithTelegramIdAsync();

        return Ok(subscriptions.Select(s => new SubscriptionWithTelegramIdResponseDTO(
            s.Id,
            s.UserId,
            s.User.TelegramId,
            s.Url,
            s.CreatedAt))
        );
    }
}