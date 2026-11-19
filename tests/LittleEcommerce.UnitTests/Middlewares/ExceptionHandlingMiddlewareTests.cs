using System.Text.Json;
using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace LittleEcommerce.UnitTests.Middlewares;

public class ExceptionHandlingMiddlewareTests
{
    private const string TraceId = "trace-123";

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext { TraceIdentifier = TraceId };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenNoExceptionIsThrown()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_ShouldWriteNotFoundEnvelope_WhenNotFoundExceptionIsThrown()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Produto não encontrado"));

        await middleware.InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", body.GetProperty("code").GetString());
        Assert.Equal("Produto não encontrado", body.GetProperty("message").GetString());
        Assert.Equal(TraceId, body.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldWriteFieldErrors_WhenValidationExceptionIsThrown()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ValidationException("email", "E-mail inválido"));

        await middleware.InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.Equal("E-mail inválido", body.GetProperty("errors").GetProperty("email")[0].GetString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldWriteBusinessRuleEnvelope_WhenDomainExceptionIsThrown()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new DomainException("Estoque insuficiente"));

        await middleware.InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, context.Response.StatusCode);
        Assert.Equal("BUSINESS_RULE", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldHideInternalDetails_WhenUnhandledExceptionIsThrown()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("connection string vazou"));

        await middleware.InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("INTERNAL_ERROR", body.GetProperty("code").GetString());
        Assert.DoesNotContain("vazou", body.GetRawText());
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnClientClosedRequest_WhenRequestIsAborted()
    {
        var context = CreateContext();
        using var cts = new CancellationTokenSource();
        context.RequestAborted = cts.Token;
        await cts.CancelAsync();
        var middleware = CreateMiddleware(ctx => throw new OperationCanceledException(ctx.RequestAborted));

        await middleware.InvokeAsync(context);

        Assert.Equal(ExceptionHandlingMiddleware.ClientClosedRequest, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }
}
