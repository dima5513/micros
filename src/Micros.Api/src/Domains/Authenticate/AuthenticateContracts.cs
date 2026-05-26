namespace Micros.Api.Domains.Authenticate;

public record RegisterContract(
    string Username, 
    string Email, 
    string Password);

public record LoginContract(
    string Email,
    string Password);