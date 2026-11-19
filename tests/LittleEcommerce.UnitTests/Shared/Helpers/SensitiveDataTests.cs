using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.AspNetCore.Http;

namespace LittleEcommerce.UnitTests.Shared.Helpers;

public class SensitiveDataTests
{
    [Theory]
    [InlineData("Password", true)]
    [InlineData("newPassword", true)]
    [InlineData("senha", true)]
    [InlineData("refresh_token", true)]
    [InlineData("X-Api-Key", true)]
    [InlineData("Authorization", true)]
    [InlineData("cardNumber", true)]
    [InlineData("Email", false)]
    [InlineData("ShippingAddress", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSensitive_ShouldDetectSecretNames(string? name, bool expected)
    {
        Assert.Equal(expected, SensitiveData.IsSensitive(name));
    }

    [Fact]
    public void MaskQueryString_ShouldMaskOnlySensitiveKeys()
    {
        var masked = SensitiveData.MaskQueryString(new QueryString("?page=2&token=abc&search=camisa"));

        Assert.Contains("page=2", masked);
        Assert.Contains("search=camisa", masked);
        Assert.DoesNotContain("abc", masked);
        Assert.Contains("token=", masked);
    }

    [Fact]
    public void MaskQueryString_ShouldReturnOriginal_WhenNothingIsSensitive()
    {
        Assert.Equal("?page=1&limit=10", SensitiveData.MaskQueryString(new QueryString("?page=1&limit=10")));
    }

    [Fact]
    public void MaskQueryString_ShouldReturnEmpty_WhenThereIsNoQuery()
    {
        Assert.Equal(string.Empty, SensitiveData.MaskQueryString(QueryString.Empty));
    }
}
