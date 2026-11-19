using LittleEcommerce.Source.Shared.Helpers;
using LittleEcommerce.Source.Shared.Options;
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
        "`{ status, code, title, message, traceId, errors }`.";

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
