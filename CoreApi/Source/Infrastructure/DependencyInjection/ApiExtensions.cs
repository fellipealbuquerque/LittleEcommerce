using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.Extensions;
using LittleEcommerce.Source.Shared.Helpers;
using LittleEcommerce.Source.Shared.Options;
using LittleEcommerce.Source.Shared.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class ApiExtensions
{
    public const string CorsPolicy = "AllowFrontend";

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddRouting(o => o.LowercaseUrls = true);

        services
            .AddControllers(o =>
                o.Filters.Add(new ProducesResponseTypeAttribute<APIErrorResponse>(StatusCodes.Status500InternalServerError)))
            .AddJsonOptions(o => JsonDefaults.Apply(o.JsonSerializerOptions))
            .ConfigureApiBehaviorOptions(o =>
            {
                o.InvalidModelStateResponseFactory = context =>
                    APIResponse.ValidationError(ModelStateErrors.From(context.ModelState), context.HttpContext.TraceIdentifier);
            });

        services.ConfigureHttpJsonOptions(o => JsonDefaults.Apply(o.SerializerOptions));

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(CorsPolicyOptions.Section).Get<CorsPolicyOptions>()?.AllowedOrigins ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, policy =>
                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        return services;
    }

    public static WebApplication UseStatusCodeEnvelope(this WebApplication app)
    {
        app.UseStatusCodePages(async context =>
        {
            var http = context.HttpContext;
            await http.Response.WriteErrorAsync(APIErrors.Create(http.Response.StatusCode, traceId: http.TraceIdentifier));
        });

        return app;
    }
}
