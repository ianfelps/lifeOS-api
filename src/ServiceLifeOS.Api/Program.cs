using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using ServiceLifeOS.Api.Adapters;
using ServiceLifeOS.Application;
using ServiceLifeOS.Application.Options;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Infrastructure;
using ServiceLifeOS.Infrastructure.Options;
using ServiceLifeOS.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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
    if (string.IsNullOrWhiteSpace(connectionString) || allowedOrigins.Length == 0 ||
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

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton(bootstrapUserOptions);

if (jwtOptions is null)
{
    throw new InvalidOperationException("Jwt configuration was not found.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret must have at least 32 characters.");
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
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe apenas o accessToken retornado por /auth/login."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [
            new OpenApiSecuritySchemeReference("Bearer", document, null)
        ] = []
    });

});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    await next(context);
});

app.UseCors("DefaultCors");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await DbSeeder.SeedAsync(db, bootstrapUserOptions, passwordHasher);
}

app.Run();
