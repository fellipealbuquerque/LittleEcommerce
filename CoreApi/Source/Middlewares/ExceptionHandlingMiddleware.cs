using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.Extensions;

namespace LittleEcommerce.Source.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public const int ClientClosedRequest = 499;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex,
                "AppException | TraceId: {TraceId} | Method: {Method} | Path: {Path} | StatusCode: {StatusCode} | Code: {Code} | Message: {Message}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                ex.StatusCode,
                ex.Code,
                ex.Message);

            await WriteAsync(context, APIErrors.Create(ex.StatusCode, ex.Message, context.TraceIdentifier, ex.Code, ex.Errors));
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                "RequestAborted | TraceId: {TraceId} | Method: {Method} | Path: {Path}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path);

            if (!context.Response.HasStarted)
                context.Response.StatusCode = ClientClosedRequest;
        }
        catch (BadHttpRequestException ex)
        {
            logger.LogWarning(ex,
                "BadHttpRequest | TraceId: {TraceId} | Method: {Method} | Path: {Path} | StatusCode: {StatusCode}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                ex.StatusCode);

            await WriteAsync(context, APIErrors.Create(ex.StatusCode, traceId: context.TraceIdentifier));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "UnhandledException | TraceId: {TraceId} | Method: {Method} | Path: {Path} | Query: {QueryString} | ExceptionType: {ExceptionType}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString.ToString(),
                ex.GetType().FullName);

            await WriteAsync(context, APIErrors.Create(StatusCodes.Status500InternalServerError, traceId: context.TraceIdentifier));
        }
    }

    private async Task WriteAsync(HttpContext context, APIErrorResponse error)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning(
                "ResponseAlreadyStarted | TraceId: {TraceId} | StatusCode: {StatusCode}",
                context.TraceIdentifier,
                error.Status);
            return;
        }

        context.Response.Clear();
        await context.Response.WriteErrorAsync(error);
    }
}
