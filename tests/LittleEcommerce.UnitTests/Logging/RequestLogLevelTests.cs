using LittleEcommerce.Source.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace LittleEcommerce.UnitTests.Logging;

public class RequestLogLevelTests
{
    [Theory]
    [InlineData("/api/v1/products", 200, LogEventLevel.Information)]
    [InlineData("/health/live", 200, LogEventLevel.Debug)]
    [InlineData("/swagger/index.html", 200, LogEventLevel.Debug)]
    [InlineData("/api/v1/products", 404, LogEventLevel.Warning)]
    [InlineData("/health/ready", 503, LogEventLevel.Error)]
    [InlineData("/api/v1/checkout", 500, LogEventLevel.Error)]
    public void RequestLevel_ShouldDependOnStatusAndPath(string path, int status, LogEventLevel expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.StatusCode = status;

        Assert.Equal(expected, LoggingExtensions.RequestLevel(context, 1, null));
    }

    [Fact]
    public void RequestLevel_ShouldBeError_WhenExceptionEscapes()
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = 200;

        Assert.Equal(LogEventLevel.Error, LoggingExtensions.RequestLevel(context, 1, new Exception()));
    }
}
