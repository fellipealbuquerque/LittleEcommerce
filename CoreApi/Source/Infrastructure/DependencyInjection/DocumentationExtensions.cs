using LittleEcommerce.Source.Shared.Helpers;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class DocumentationExtensions
{
    public const string DocumentUrl = "/openapi/v1.json";
    public const string SwaggerPrefix = "swagger";
    private const string Title = "LittleEcommerce API";

    private const string Description =
        "API de venda de produtos. Sucesso responde `{ status, message, data }` e erro responde " +
        "`{ status, code, title, message, traceId, errors }`. Envie `X-Correlation-Id` para rastrear a requisição " +
        "nos logs; sem ele a API gera um e devolve no mesmo header. O `traceId` do erro é esse mesmo valor.";

    public const string BearerScheme = "Bearer";

    public static IServiceCollection AddDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = Title,
                    Version = AppInfo.Version,
                    Description = Description
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Cole o `accessToken` devolvido por `/api/v1/auth/login`."
                };

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var requiresAuth = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();

                if (!requiresAuth)
                    return Task.CompletedTask;

                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = []
                });

                operation.Responses ??= new OpenApiResponses();
                operation.Responses.TryAdd(
                    StatusCodes.Status401Unauthorized.ToString(),
                    new OpenApiResponse { Description = "Token ausente, inválido, expirado ou revogado" });

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication UseDocumentation(this WebApplication app)
    {
        var options = app.Configuration.GetSection(DocumentationOptions.Section).Get<DocumentationOptions>();

        if (options is not { Enabled: true })
            return app;

        app.MapOpenApi();

        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint(DocumentUrl, Title);
            o.RoutePrefix = SwaggerPrefix;
            o.DocumentTitle = Title;
            o.DisplayRequestDuration();
            o.EnableTryItOutByDefault();
            o.EnablePersistAuthorization();
        });

        app.MapScalarApiReference(o => o.Title = Title);

        app.MapGet("/", () => Results.Redirect($"/{SwaggerPrefix}")).ExcludeFromDescription();

        return app;
    }
}
