using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Exceptions;
using TickerQ.DependencyInjection;
using LoungeClubsApi.Data;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Filters;
using LoungeClubsApi.OpenApi;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application");

    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails());

    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.AddDocumentTransformer<ServerUrlTransformer>();
    });
    builder.Services.AddOpenApi("v2", options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.AddDocumentTransformer<ServerUrlTransformer>();
    });

    builder.Services.ConfigureSqlContext(builder.Configuration);
    builder.Services.ConfigureDbConnection(builder.Configuration);
    builder.Services.ConfigureDatabaseSettings(builder.Configuration);
    builder.Services.ConfigureIdentity();
    builder.Services.ConfigureJwt(builder.Configuration);
    builder.Services.ConfigureMail(builder.Configuration);
    builder.Services.ConfigureSms(builder.Configuration);
    builder.Services.ConfigureAppSettings(builder.Configuration);
    builder.Services.ConfigurePaymentGateway(builder.Configuration);
    builder.Services.ConfigureFileStorage(builder.Configuration);
    builder.Services.AddHttpClient();
    builder.Services.ConfigureServiceScanning();
    builder.Services.ConfigureApiVersioning();
    builder.Services.ConfigureValidation();
    builder.Services.ConfigureAuthorization();
    builder.Services.ConfigureHttpContextAccessor();
    builder.Services.ConfigureProblemDetails();
    builder.Services.ConfigureCarter();
    builder.Services.ConfigureTickerQ(builder.Configuration);
    builder.Services.ConfigureCortexMediator(builder.Configuration);
    builder.Services.ConfigureCors(builder.Configuration);
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
        options.AddPolicy("public-read", httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
        // Anonymous complaint intake — strict cap; residents legitimately file
        // a handful of reports, bots file hundreds.
        options.AddPolicy("complaint", httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
    });
    builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
    {
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });


    var app = builder.Build();

    // Configure the HTTP request pipeline.
    // Configure the HTTP request pipeline.
    app.UseStatusCodePages();
    app.UseExceptionHandler();

    // Honour X-Forwarded-For from the reverse proxy (Koyeb, Nginx, …) so
    // RemoteIpAddress is the real client — required for per-IP rate limiting,
    // audit IPs, and correct forwarded-scheme redirects.
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    // In dev the client hits the API directly so XFF must be trusted from
    // anywhere. In production only trust the configured proxy addresses —
    // otherwise a client can spoof X-Forwarded-For to defeat IP rate limits.
    if (app.Environment.IsDevelopment())
    {
        forwardedOptions.KnownNetworks.Clear();
        forwardedOptions.KnownProxies.Clear();
    }
    else
    {
        var trusted = builder.Configuration.GetSection("Proxy:TrustedIps").Get<string[]>() ?? [];
        foreach (var ip in trusted)
            if (System.Net.IPAddress.TryParse(ip, out var addr))
                forwardedOptions.KnownProxies.Add(addr);
    }
    app.UseForwardedHeaders(forwardedOptions);

    app.UseHttpsRedirection();

    // Baseline security headers on every response.
    app.Use(async (context, next) =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        await next();
    });

    // CORS must run before the rate limiter so 429 responses still carry
    // Access-Control-Allow-* headers (otherwise browsers see an opaque error).
    app.UseCors("CorsPolicy");
    app.UseRateLimiter();
    app.MapStaticAssets();

    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
    }
    app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription().AllowAnonymous();

    // Migrations must run before UseTickerQ — it touches ticker.* tables on startup.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var tickerDb = scope.ServiceProvider.GetService<TickerQ.EntityFrameworkCore.DbContextFactory.TickerQDbContext>();
        if (tickerDb is not null)
            await tickerDb.Database.MigrateAsync();

        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }

    app.UseTickerQ();

    var versionSet = app.NewApiVersionSet()
        .HasApiVersion(new Asp.Versioning.ApiVersion(1))
        .HasApiVersion(new Asp.Versioning.ApiVersion(2))
        .ReportApiVersions()
        .Build();

    app.MapGroup("api/v{version:apiVersion}")
        .WithApiVersionSet(versionSet)
        .AddEndpointFilter<ValidationFilter>()
        .MapCarter();

    await app.RunAsync();
}
catch (Microsoft.Extensions.Hosting.HostAbortedException)
{
    // Ignore this exception as it is thrown by EF Core tools when generating migrations
    Log.Information("Host aborted (likely by EF Core tools).");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
