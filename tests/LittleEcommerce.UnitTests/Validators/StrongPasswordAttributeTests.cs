using LittleEcommerce.Source.Shared.Validators;

namespace LittleEcommerce.UnitTests.Validators;

public class StrongPasswordAttributeTests
{
    [Theory]
    [InlineData("Senha123", true)]
    [InlineData("abc12345", true)]
    [InlineData("Ação2026x", true)]
    [InlineData("curta1", false)]
    [InlineData("somenteletras", false)]
    [InlineData("1234567890", false)]
    [InlineData(null, false)]
    public void IsValid_ShouldRequireLengthLetterAndDigit(string? password, bool expected)
    {
        Assert.Equal(expected, new StrongPasswordAttribute().IsValid(password));
    }

    [Fact]
    public void IsValid_ShouldRejectPasswordsLongerThanBcryptLimit()
    {
        Assert.False(new StrongPasswordAttribute().IsValid(new string('a', 72) + "1"));
    }
}
