using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ServiceLifeOS.Api.Adapters;
using ServiceLifeOS.Application;
using ServiceLifeOS.Application.Options;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Infrastructure;
using ServiceLifeOS.Infrastructure.Options;
using ServiceLifeOS.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var meter = new Meter("ServiceLifeOS.Api");
var requestDuration = meter.CreateHistogram<double>("http.server.request.duration", unit: "ms");
var requestCount = meter.CreateCounter<long>("http.server.request.count");
var errorCount = meter.CreateCounter<long>("http.server.error.count");

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var bootstrapUserOptions = builder.Configuration
    .GetSection("BootstrapUser")
    .Get<BootstrapUserOptions>() ?? new BootstrapUserOptions();
var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>();
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (builder.Environment.IsProduction())
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString) ||
        allowedOrigins.Length == 0 ||
        string.IsNullOrWhiteSpace(jwtOptions?.Issuer) ||
        string.IsNullOrWhiteSpace(jwtOptions?.Audience) ||
        string.IsNullOrWhiteSpace(bootstrapUserOptions.UserId) ||
        string.IsNullOrWhiteSpace(bootstrapUserOptions.UserName) ||
        string.IsNullOrWhiteSpace(bootstrapUserOptions.DisplayName) ||
        string.IsNullOrWhiteSpace(bootstrapUserOptions.Password))
    {
        throw new InvalidOperationException("Production configuration is incomplete.");
    }
}

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton(bootstrapUserOptions);
builder.Services.AddSingleton(builder.Configuration.GetSection("PasswordPolicy").Get<PasswordPolicyOptions>() ?? new PasswordPolicyOptions());

if (jwtOptions is null)
{
    throw new InvalidOperationException("Jwt configuration was not found.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret must have at least 32 characters.");
}

if (jwtOptions.AccessTokenExpirationMinutes <= 0 || jwtOptions.RefreshTokenExpirationDays <= 0)
{
    throw new InvalidOperationException("Jwt token expiration settings must be greater than zero.");
}

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "username"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)?.Trim();
                var tokenId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti)?.Trim();
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tokenId))
                {
                    context.Fail("Session token was not found.");
                    return;
                }

                var sessions = context.HttpContext.RequestServices
                    .GetRequiredService<IUserSessionRepository>();
                var now = DateTime.UtcNow;
                if (!await sessions.IsActiveAsync(
                        userId,
                        tokenId,
                        now,
                        context.HttpContext.RequestAborted))
                {
                    context.Fail("Session is no longer active.");
                    return;
                }

                await sessions.TouchAsync(
                    tokenId,
                    now,
                    context.HttpContext.RequestAborted);
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var loginPermitLimit = builder.Configuration.GetValue("RateLimiting:LoginPermitLimit", 10);
var loginWindowMinutes = builder.Configuration.GetValue("RateLimiting:LoginWindowMinutes", 15);
var apiPermitLimit = builder.Configuration.GetValue("RateLimiting:ApiPermitLimit", 300);
var apiWindowMinutes = builder.Configuration.GetValue("RateLimiting:ApiWindowMinutes", 1);
var refreshPermitLimit = builder.Configuration.GetValue("RateLimiting:RefreshPermitLimit", 30);
var refreshWindowMinutes = builder.Configuration.GetValue("RateLimiting:RefreshWindowMinutes", 1);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = apiPermitLimit,
                Window = TimeSpan.FromMinutes(apiWindowMinutes),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = loginPermitLimit;
        limiterOptions.Window = TimeSpan.FromMinutes(loginWindowMinutes);
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
    options.AddFixedWindowLimiter("refresh", limiterOptions =>
    {
        limiterOptions.PermitLimit = refreshPermitLimit;
        limiterOptions.Window = TimeSpan.FromMinutes(refreshWindowMinutes);
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Informe apenas o accessToken retornado por /auth/login."
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
        });
        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
}

app.UseForwardedHeaders();

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] =
        context.Request.Path.StartsWithSegments("/scalar")
        ? "default-src 'self'; script-src 'self' 'unsafe-inline' blob:; " +
          "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
          "font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; " +
          "connect-src 'self'; frame-ancestors 'none'; base-uri 'none'"
        : "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    await next(context);
});

app.Use(async (context, next) =>
{
    var startedAt = Stopwatch.GetTimestamp();
    try
    {
        await next(context);
    }
    catch (ArgumentException exception)
    {
        await WriteErrorAsync(context, StatusCodes.Status400BadRequest, exception.Message);
    }
    catch (KeyNotFoundException exception)
    {
        await WriteErrorAsync(context, StatusCodes.Status404NotFound, exception.Message);
    }
    catch (InvalidOperationException exception)
    {
        await WriteErrorAsync(context, StatusCodes.Status409Conflict, exception.Message);
    }
    catch (Exception exception)
    {
        context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("UnhandledException")
            .LogError(exception, "Unhandled request failure for {Method} {Path}.", context.Request.Method, context.Request.Path);
        errorCount.Add(1);
        await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
    }
    finally
    {
        requestCount.Add(1);
        requestDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
    }
});

app.UseCors("DefaultCors");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { AllowCachingResponses = false });

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    if (app.Environment.IsDevelopment())
    {
        await db.Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(db, bootstrapUserOptions, passwordHasher);
}

app.Run();

static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
{
    context.Response.StatusCode = statusCode;
    return context.Response.WriteAsJsonAsync(new { message });
}
