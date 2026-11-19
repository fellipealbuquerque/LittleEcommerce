using LittleEcommerce.Source.Common.Errors;

namespace LittleEcommerce.UnitTests.Common.Errors;

public class HttpErrorCatalogTests
{
    [Theory]
    [InlineData(400, "BAD_REQUEST")]
    [InlineData(401, "UNAUTHORIZED")]
    [InlineData(403, "FORBIDDEN")]
    [InlineData(404, "NOT_FOUND")]
    [InlineData(409, "CONFLICT")]
    [InlineData(422, "BUSINESS_RULE")]
    [InlineData(500, "INTERNAL_ERROR")]
    public void Describe_ShouldReturnKnownCode(int status, string expectedCode)
    {
        var (code, title) = HttpErrorCatalog.Describe(status);

        Assert.Equal(expectedCode, code);
        Assert.False(string.IsNullOrWhiteSpace(title));
    }

    [Fact]
    public void Describe_ShouldFallbackToReasonPhrase_WhenStatusIsNotMapped()
    {
        var (code, title) = HttpErrorCatalog.Describe(418);

        Assert.Equal("HTTP_418", code);
        Assert.Equal("I'm a teapot", title);
    }
}
