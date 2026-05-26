using Micros.Api.Domains.Common;
using Micros.Api.Domains.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Micros.Api.Infrastructure.ExceptionHandlers;

public class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainEx)
            return false;   // не наш — пропусти дальше

        logger.LogWarning(exception, "Domain exception: {Code}", domainEx.Code);

        var problem = new ProblemDetails()
        {
            Status = domainEx.StatusCode,
            Title = domainEx.Message,
            Type = $"https://errors.micros.api/{domainEx.Code}",
            Extensions = { ["code"] = domainEx.Code }
        };

        httpContext.Response.StatusCode = domainEx.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;   // обработали
    }
}
