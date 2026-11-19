using LittleEcommerce.Source.Infrastructure.Security;
using LittleEcommerce.Source.Shared.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace LittleEcommerce.UnitTests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new(MsOptions.Create(new PasswordHashingOptions { WorkFactor = 4 }));

    [Fact]
    public void Verify_ShouldAcceptOriginalPassword()
    {
        var hash = _hasher.Hash("Senha12345");

        Assert.NotEqual("Senha12345", hash);
        Assert.True(_hasher.Verify("Senha12345", hash));
        Assert.False(_hasher.Verify("Senha12346", hash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("isto-nao-e-um-hash")]
    public void Verify_ShouldReturnFalse_WhenHashIsMissingOrInvalid(string? hash)
    {
        Assert.False(_hasher.Verify("Senha12345", hash));
    }
}
