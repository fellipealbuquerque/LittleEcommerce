using LittleEcommerce.Source.Infrastructure.DependencyInjection;
using LittleEcommerce.Source.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddStructuredLogging(builder.Configuration)
    .AddApi()
    .AddDocumentation()
    .AddCorsPolicy(builder.Configuration)
    .AddDataStores(builder.Configuration)
    .AddHealth();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodeEnvelope();
app.UseCors(ApiExtensions.CorsPolicy);
app.UseDocumentation();
app.MapControllers();
app.MapHealth();

app.Run();

public partial class Program;
