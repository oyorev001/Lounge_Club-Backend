using System.Security.Claims;
using System.Text;
using Cortex.Mediator.DependencyInjection;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using LoungeClubsApi.Attributes;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using TickerQ.DependencyInjection;
using LoungeClubsApi.Interfaces;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.Dashboard.DependencyInjection;

namespace LoungeClubsApi.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public void ConfigureIdentity()
        {
            services.AddIdentity<User, IdentityRole>(o =>
                {
                    o.Password.RequireDigit = true;
                    o.Password.RequireLowercase = false;
                    o.Password.RequireUppercase = false;
                    o.Password.RequireNonAlphanumeric = false;
                    o.Password.RequiredLength = 10;
                    o.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
        }

        public void ConfigureHttpContextAccessor()
        {
            services.AddHttpContextAccessor();
        }

        public void ConfigureProblemDetails()
        {
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                    context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);

                    if (context.Exception is BadHttpRequestException badRequestEx)
                    {
                        context.ProblemDetails.Title = "Invalid Request";
                        context.ProblemDetails.Status = StatusCodes.Status400BadRequest;
                        context.ProblemDetails.Detail = badRequestEx.Message;
                        context.ProblemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                        context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }

                    // Map common domain exceptions so handlers don't each need
                    // a try/catch just to produce correct status codes.
                    switch (context.Exception)
                    {
                        case KeyNotFoundException knf:
                            context.ProblemDetails.Title = "Not Found";
                            context.ProblemDetails.Status = StatusCodes.Status404NotFound;
                            context.ProblemDetails.Detail = knf.Message;
                            context.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                            break;
                        case UnauthorizedAccessException unauth:
                            context.ProblemDetails.Title = "Forbidden";
                            context.ProblemDetails.Status = StatusCodes.Status403Forbidden;
                            context.ProblemDetails.Detail = unauth.Message;
                            context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                            break;
                        case InvalidOperationException invalid:
                            context.ProblemDetails.Title = "Invalid Operation";
                            context.ProblemDetails.Status = StatusCodes.Status400BadRequest;
                            context.ProblemDetails.Detail = invalid.Message;
                            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                            break;
                        case FluentValidation.ValidationException valEx:
                            context.ProblemDetails.Title = "Validation Error";
                            context.ProblemDetails.Status = StatusCodes.Status400BadRequest;
                            context.ProblemDetails.Detail = string.Join(" ", valEx.Errors.Select(e => e.ErrorMessage));
                            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                            break;
                    }
                };
            });
        }

        public void ConfigureCarter()
        {
            services.AddCarter();
        }

        public void ConfigureCortexMediator(IConfiguration configuration)
        {
            services.AddCortexMediator(configuration, [typeof(Program)], options => options.AddDefaultBehaviors());
        }

        public void ConfigureJwt(IConfiguration configuration)
        {
            // Register and validate JwtSettings
            services.AddOptionsWithFluentValidation<JwtSettings>("JwtSettings");

            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

            services.AddAuthentication(opt =>
                {
                    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.ValidIssuer,
                        ValidAudience = jwtSettings.ValidAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                    };

                    // Deactivation sets LockoutEnd but doesn't revoke issued
                    // tokens — reject them here so a deactivated account stops
                    // working immediately instead of at token expiry.
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userId = context.Principal?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                            if (string.IsNullOrEmpty(userId))
                            {
                                context.Fail("Invalid token.");
                                return;
                            }

                            var userManager = context.HttpContext.RequestServices
                                .GetRequiredService<UserManager<User>>();
                            var user = await userManager.FindByIdAsync(userId);
                            if (user is null || (user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow))
                            {
                                context.Fail("Account is deactivated.");
                                return;
                            }

                            // Security-stamp check — role changes, deactivation and password
                            // resets bump the stamp, which instantly invalidates live tokens.
                            var stamp = context.Principal?.FindFirstValue("sec_stamp");
                            if (stamp != (user.SecurityStamp ?? ""))
                                context.Fail("Token is stale — sign in again.");
                        }
                    };
                });
        }

        public void ConfigureSqlContext(IConfiguration configuration)
        {
            var databaseSettings = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>();
            var connectionString = configuration.GetConnectionString("sqlConnection");

            services.AddDbContextFactory<AppDbContext>((sp, opts) =>
            {
                switch (databaseSettings?.DBProvider?.ToLowerInvariant())
                {
                    case "postgres":
                    case "postgresql":
                    case "npgsql":
                        opts.UseNpgsql(connectionString);
                        break;
                    case "mysql":
                        opts.UseMySQL(connectionString!);
                        break;
                    case "mssql":
                    default:
                        opts.UseSqlServer(connectionString);
                        break;
                }
                opts.AddInterceptors(sp.GetRequiredService<Interceptors.AuditInterceptor>());
            });
        }

        public void ConfigureDbConnection(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("sqlConnection");
            var provider = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>()?.DBProvider?.ToLowerInvariant();
            services.AddScoped<System.Data.IDbConnection>(_ => provider switch
            {
                "postgres" or "postgresql" or "npgsql" => new Npgsql.NpgsqlConnection(connectionString),
                _ => new Microsoft.Data.SqlClient.SqlConnection(connectionString)
            });
        }

        public void ConfigureDatabaseSettings(IConfiguration configuration)
        {
            services.AddOptionsWithFluentValidation<DatabaseSettings>("DatabaseSettings");
        }

        public void ConfigureAppSettings(IConfiguration configuration)
        {
            services.AddOptionsWithFluentValidation<AppSettings>("AppSettings");
        }

        public void ConfigurePaymentGateway(IConfiguration configuration)
        {
            services.Configure<PaystackSettings>(configuration.GetSection("Paystack"));
            var baseUrl = configuration.GetSection("Paystack").Get<PaystackSettings>()?.BaseUrl
                ?? "https://api.paystack.co";
            services.AddHttpClient("Paystack", client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddTransient<Interfaces.IPaymentGateway, Services.PaystackGateway>();
        }

        public void ConfigureMail(IConfiguration configuration)
        {
            services.AddOptionsWithFluentValidation<MailSettings>("MailSettings");
        }

        public void ConfigureSms(IConfiguration configuration)
        {
            services.Configure<SmsSettings>(configuration.GetSection("SmsSettings"));
            services.AddHttpClient<Services.TermiiSmsService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            services.AddTransient<Interfaces.ISmsService, Services.TermiiSmsService>();
        }

        public void ConfigureServiceScanning()
        {
            services.Scan(scan => scan
                .FromAssemblyOf<IEmailService>()
                // Register Scoped Services
                .AddClasses(classes => classes.WithAttribute<ScopedServiceAttribute>())
                .AsImplementedInterfaces()
                .AsSelf()
                .WithScopedLifetime()
                // Register Singleton Services
                .AddClasses(classes => classes.WithAttribute<SingletonServiceAttribute>())
                .AsImplementedInterfaces()
                .AsSelf()
                .WithSingletonLifetime()
                // Register Transient Services (Explicit Attribute)
                .AddClasses(classes => classes.WithAttribute<TransientServiceAttribute>())
                .AsImplementedInterfaces()
                .AsSelf()
                .WithTransientLifetime()
                // Register Default Services (Ends with "Service") - Exclude decorated services
                .AddClasses(classes => classes.Where(type =>
                    type.Name.EndsWith("Service") &&
                    !type.IsDefined(typeof(ScopedServiceAttribute), false) &&
                    !type.IsDefined(typeof(SingletonServiceAttribute), false) &&
                    !type.IsDefined(typeof(TransientServiceAttribute), false)))
                .AsImplementedInterfaces()
                .AsSelf()
                .WithTransientLifetime());
        }

        public void ConfigureFileStorage(IConfiguration configuration)
        {
            services.Configure<StorageSettings>(configuration.GetSection("Storage"));
            var provider = configuration.GetSection("Storage").Get<StorageSettings>()?.Provider;
            if (provider?.Equals("s3", StringComparison.OrdinalIgnoreCase) == true)
                services.AddSingleton<Interfaces.IFileStorage, Services.S3FileStorage>();
            else
                services.AddSingleton<Interfaces.IFileStorage, Services.LocalFileStorage>();
        }

        public void ConfigureApiVersioning()
        {
            services.AddEndpointsApiExplorer();
            services.AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                        new Asp.Versioning.UrlSegmentApiVersionReader(),
                        new Asp.Versioning.HeaderApiVersionReader("X-Api-Version"));
                })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'V";
                    options.SubstituteApiVersionInUrl = true;
                })
                .EnableApiVersionBinding();
        }

        public void ConfigureValidation()
        {
            services.AddValidation();
            services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        }

        public void ConfigureAuthorization()
        {
            services.AddSingleton<IAuthorizationPolicyProvider, Authorization.Providers.CustomAuthorizationPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, Authorization.Handlers.RoleAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, Authorization.Handlers.PermissionAuthorizationHandler>();

            services.AddAuthorization();
        }

        public void ConfigureCors(IConfiguration configuration)
        {
            var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();
            var origins = new[] { appSettings?.PortalUrl, appSettings?.AppUrl }
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Distinct()
                .ToArray();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    if (origins.Length == 0)
                    {
                        // No origins configured (misconfiguration) — deny cross-origin
                        // rather than silently allowing every site on the internet.
                        builder.WithOrigins("about:blank");
                    }
                    else
                    {
                        builder.WithOrigins(origins.Select(o => o.TrimEnd('/')).ToArray());
                    }

                    builder.AllowAnyMethod().AllowAnyHeader();
                });
            });
        }

        public void ConfigureTickerQ(IConfiguration configuration)
        {
            services.AddTickerQ(options =>
            {
                // Dashboard only mounts when credentials are configured —
                // never ship a default-password admin surface.
                var dashUser = configuration["TickerQ:DashboardUser"];
                var dashPass = configuration["TickerQ:DashboardPassword"];
                if (!string.IsNullOrWhiteSpace(dashUser) && !string.IsNullOrWhiteSpace(dashPass))
                {
                    options.AddDashboard(dashboardOptions =>
                    {
                        dashboardOptions.WithBasicAuth(dashUser, dashPass);
                    });
                }

                options.ConfigureScheduler(scheduler =>
                {
                    scheduler.MaxConcurrency = 8;
                    scheduler.NodeIdentifier = Environment.MachineName;
                });

                options.AddOperationalStore(efOptions =>
                {
                    var connectionString = configuration.GetConnectionString("sqlConnection");

                    efOptions.UseTickerQDbContext<TickerQDbContext>(optionsBuilder =>
                    {
                        var provider = configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>()?.DBProvider?.ToLowerInvariant();
                        if (provider is "postgres" or "postgresql" or "npgsql")
                        {
                            optionsBuilder.UseNpgsql(connectionString, cfg =>
                            {
                                cfg.MigrationsAssembly(typeof(Program).Assembly.GetName().Name!);
                                cfg.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                            });
                        }
                        else
                        {
                            optionsBuilder.UseSqlServer(connectionString, cfg =>
                            {
                                cfg.MigrationsAssembly(typeof(Program).Assembly.GetName().Name!);
                                cfg.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                            });
                        }
                    }, schema: "ticker");

                    efOptions.SetDbContextPoolSize(34);
                });
            });
        }
    }
}
