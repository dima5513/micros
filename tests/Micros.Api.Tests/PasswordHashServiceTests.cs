using Micros.Api.Infrastructure.PasswordHash;

namespace Micros.Api.Tests;

public class PasswordHashServiceTests
{
    private readonly PasswordHashService _sut = new();


    [Theory]
    [InlineData("cfjF)!KSFOLSJru2389urnfjcsnFJHAjfsjfoG$so")]
    [InlineData("123456")]
    public void Verify_WithCorrectPassword_ReturnsTrue(string password)
    {
        var hashedPassword = _sut.Hash(password);

        var isCorrectPassword = _sut.Verify(password, hashedPassword);
        
        Assert.True(isCorrectPassword);
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hashedPassword = _sut.Hash("12345678");
        
        var isCorrectPassword = _sut.Verify("87654321", hashedPassword);
        
        Assert.False(isCorrectPassword);
    }

    [Fact]
    public void Hash_CalledTwiceForSamePassword_ProducesDifferentHashes()
    {
        var hashedPassword1 = _sut.Hash("12345678");
        var hashedPassword2 = _sut.Hash("12345678");
        
        Assert.NotEqual(hashedPassword1, hashedPassword2);
    }
}