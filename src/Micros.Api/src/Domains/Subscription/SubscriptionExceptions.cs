using Micros.Api.Domains.Common;
using Micros.Api.Domains.Common.Exceptions;

namespace Micros.Api.Domains.Subscription;

public class SubscriptionUrlAlreadyExistsException(string url) 
    : DomainException($"Subscription with url: {url} already exist")
{
    public override string Code => "subscription.url_taken";
    public override int StatusCode => StatusCodes.Status409Conflict;
}

public class SubscriptionNotFoundException(Guid subscriptionId)
    : DomainException($"Subscription with {subscriptionId} not found")
{
    public override string Code => "subscription.not_found";
    public override int StatusCode => StatusCodes.Status404NotFound;
}