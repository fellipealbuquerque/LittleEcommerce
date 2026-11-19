using System.Text.Json.Serialization;
using LittleEcommerce.Source.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Shared.DTOs;

public abstract record APIResponseBase(
    [property: JsonPropertyOrder(-10)] int Status,
    [property: JsonPropertyOrder(0)] string Message
);

public record APIErrorResponse(
    int Status,
    [property: JsonPropertyOrder(-5)] string Code,
    [property: JsonPropertyOrder(-4)] string Title,
    string Message,
    [property: JsonPropertyOrder(5)] string? TraceId = null,
    [property: JsonPropertyOrder(6)] IReadOnlyDictionary<string, string[]>? Errors = null
) : APIResponseBase(Status, Message);

public record APISuccessResponse<T>(
    int Status,
    string Message,
    [property: JsonPropertyOrder(5)] T Data
) : APIResponseBase(Status, Message);

public class PaginatedResponse<T>
{
    public int Status { get; }
    public string Message { get; }
    public int Page { get; }
    public int Total { get; }
    public int TotalPages { get; }
    public int Limit { get; }
    public int Offset { get; }
    public IEnumerable<T> Data { get; }

    public PaginatedResponse(int status, string message, IEnumerable<T> data, int total, int limit, int offset)
    {
        Status = status;
        Message = message;
        Data = data;
        Total = total;
        Limit = limit;
        Offset = offset;
        TotalPages = limit <= 0 ? 0 : (int)Math.Ceiling(total / (double)limit);
        Page = limit <= 0 ? 1 : offset / limit + 1;
    }
}

public static class APIErrors
{
    public const string ValidationCode = "VALIDATION_ERROR";

    public static APIErrorResponse Create(
        int status,
        string? message = null,
        string? traceId = null,
        string? code = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var (defaultCode, title) = HttpErrorCatalog.Describe(status);
        return new APIErrorResponse(status, code ?? defaultCode, title, message ?? title, traceId, errors);
    }
}

public static class APIResponse
{
    public static ObjectResult Success<T>(T data, string message = "Success", int status = StatusCodes.Status200OK) =>
        new(new APISuccessResponse<T>(status, message, data)) { StatusCode = status };

    public static ObjectResult Created<T>(T data, string message = "Created") =>
        Success(data, message, StatusCodes.Status201Created);

    public static ObjectResult CreatedAtAction<T>(
        ControllerBase controller,
        string actionName,
        object? routeValues,
        T data,
        string message = "Created")
    {
        var response = new APISuccessResponse<T>(StatusCodes.Status201Created, message, data);
        return controller.CreatedAtAction(actionName, routeValues, response);
    }

    public static ObjectResult Paginated<T>(
        IEnumerable<T> data,
        int total,
        int limit,
        int offset,
        string message = "Success") =>
        new(new PaginatedResponse<T>(StatusCodes.Status200OK, message, data, total, limit, offset))
            { StatusCode = StatusCodes.Status200OK };

    public static ObjectResult Error(APIErrorResponse error) =>
        new(error) { StatusCode = error.Status };

    public static ObjectResult BadRequest(string message, string? traceId = null) =>
        Error(APIErrors.Create(StatusCodes.Status400BadRequest, message, traceId));

    public static ObjectResult Unauthorized(string message, string? traceId = null) =>
        Error(APIErrors.Create(StatusCodes.Status401Unauthorized, message, traceId));

    public static ObjectResult Forbidden(string message, string? traceId = null) =>
        Error(APIErrors.Create(StatusCodes.Status403Forbidden, message, traceId));

    public static ObjectResult NotFound(string message, string? traceId = null) =>
        Error(APIErrors.Create(StatusCodes.Status404NotFound, message, traceId));

    public static ObjectResult Conflict(string message, string? traceId = null) =>
        Error(APIErrors.Create(StatusCodes.Status409Conflict, message, traceId));

    public static ObjectResult ValidationError(IReadOnlyDictionary<string, string[]> errors, string? traceId = null) =>
        Error(APIErrors.Create(StatusCodes.Status400BadRequest, "Dados inválidos", traceId, APIErrors.ValidationCode, errors));

    public static ObjectResult InternalError(string message = "Erro interno inesperado", string? traceId = null) =>
        Error(APIErrors.Create(StatusCodes.Status500InternalServerError, message, traceId));
}
