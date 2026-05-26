namespace Micros.Api.Domains.Common.Exceptions;

public class ForbiddenException() : DomainException("you do not have access to this resource")
{
    public override string Code => "forbidden";
    public override int StatusCode => StatusCodes.Status403Forbidden;
}