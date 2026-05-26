using Micros.Api.Domains.Common;
using Micros.Api.Domains.Common.Exceptions;

namespace Micros.Api.Domains.User;

public class EmailAlreadyExistsException(string email)
    : DomainException($"User with email '{email}' already exists")
{
    public override string Code => "user.email_taken";
    public override int StatusCode => StatusCodes.Status409Conflict;
}

// src/Domains/User/Exceptions/UserNotFoundException.cs
public class UserNotFoundException(string email)
    : DomainException($"User with email '{email}' not found")
{
    public override string Code => "user.not_found";
    public override int StatusCode => StatusCodes.Status404NotFound;
}

public class TelegramIdAlreadyExistsException(long telegramId)
    : DomainException($"User with telegram id '{telegramId}' already exists")
{
    public override string Code => "user.telegram_id_taken";
    public override int StatusCode => StatusCodes.Status409Conflict;
}

// src/Domains/Authenticate/Exceptions/InvalidCredentialsException.cs
public class InvalidCredentialsException()
    : DomainException("Invalid email or password")
{
    public override string Code => "auth.invalid_credentials";
    public override int StatusCode => StatusCodes.Status401Unauthorized;
}