using LittleEcommerce.Source.Middlewares;
using Microsoft.AspNetCore.Http;

namespace LittleEcommerce.UnitTests.Middlewares;

public class CorrelationIdMiddlewareTests
{
    [Theory]
    [InlineData("pedido-123")]
    [InlineData("0HN1:00000001")]
    [InlineData("a.b_c-d")]
    public void Resolve_ShouldKeepValidIncomingId(string incoming)
    {
        Assert.Equal(incoming, CorrelationIdMiddleware.Resolve(incoming));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tem espaço")]
    [InlineData("<script>")]
    public void Resolve_ShouldGenerateId_WhenIncomingIsInvalid(string? incoming)
    {
        var resolved = CorrelationIdMiddleware.Resolve(incoming);

        Assert.NotEqual(incoming, resolved);
        Assert.Equal(32, resolved.Length);
    }

    [Fact]
    public void Resolve_ShouldGenerateId_WhenIncomingIsTooLong()
    {
        var incoming = new string('a', 65);

        Assert.NotEqual(incoming, CorrelationIdMiddleware.Resolve(incoming));
    }

    [Fact]
    public async Task InvokeAsync_ShouldUseCorrelationIdAsTraceIdentifier()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "abc-123";
        string? seen = null;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            seen = ctx.TraceIdentifier;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal("abc-123", seen);
    }
}
