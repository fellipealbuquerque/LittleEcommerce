using System.Text.Json;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LittleEcommerce.Source.Shared.Extensions;

public static class HttpResponseExtensions
{
    public static Task WriteErrorAsync(this HttpResponse response, APIErrorResponse error)
    {
        response.StatusCode = error.Status;
        return response.WriteAsJsonAsync(error, SerializerOptions(response.HttpContext));
    }

    private static JsonSerializerOptions SerializerOptions(HttpContext context) =>
        context.RequestServices?.GetService<IOptions<JsonOptions>>()?.Value.JsonSerializerOptions
        ?? JsonDefaults.Options;
}
