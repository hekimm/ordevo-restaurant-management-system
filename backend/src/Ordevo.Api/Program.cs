using System.Diagnostics;
using System.Data.Common;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Ordevo.Api.Modules;
using Ordevo.BuildingBlocks;
using Ordevo.BuildingBlocks.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddBuildingBlocks(builder.Configuration);

builder.Services.AddOpenApi();

var otelEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Ordevo.Api"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otelEndpoint))
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otelEndpoint));
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otelEndpoint))
            m.AddOtlpExporter(o => o.Endpoint = new Uri(otelEndpoint));
    });

builder.Services.AddHealthChecks()
    .AddCheck<OracleHealthCheck>("oracle", tags: ["ready"]);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("default", o =>
    {
        o.PermitLimit = 100;
        o.Window = TimeSpan.FromSeconds(10);
        o.QueueLimit = 0;
    });
});

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        if (builder.Environment.IsDevelopment())
            policy.SetIsOriginAllowed(_ => true);
        else if (corsOrigins.Length > 0)
            policy.WithOrigins(corsOrigins);
    });
});

var modules = ModuleRegistry.DiscoverModules();
foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        Log.ForContext("RequestPath", feature?.Path)
            .Error(feature?.Error, "Unhandled API exception");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            title = "server.error",
            detail = "İşlem şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.",
            status = StatusCodes.Status500InternalServerError,
            traceId = Activity.Current?.Id ?? context.TraceIdentifier
        });
    });
});

app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    service = "Ordevo.Api",
    status = "up",
    modules = modules.Select(m => m.Name).ToArray()
}));

foreach (var module in modules)
    module.MapEndpoints(app);

app.Run();

public sealed class OracleHealthCheck(IDbConnectionFactory factory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using DbConnection connection = await factory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM DUAL";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("Oracle reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Oracle unreachable.", ex);
        }
    }
}

public partial class Program;
