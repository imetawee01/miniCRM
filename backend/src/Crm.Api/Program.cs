using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Crm.Api.Middleware;
using Crm.Application;
using Crm.Application.Common;
using Crm.Infrastructure;
using Crm.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/crm-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, _, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/crm-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 26 * 1024 * 1024);

    var jwt = builder.Configuration.GetSection("Authentication:Jwt");
    var key = Encoding.UTF8.GetBytes(jwt["Key"] ?? "DEV-ONLY-CHANGE-ME-32CHARS-MINIMUM-KEY!!");

    var auth = builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

    // TODO(SSO): if Authentication:EntraId:Enabled, register OpenIdConnect / additional JWT bearer for Entra ID.
    // Stub only — do not implement provisioning, group-to-role mapping, or the frontend SSO login button yet.
    // if (builder.Configuration.GetValue("Authentication:EntraId:Enabled", false))
    // {
    //     auth.AddJwtBearer("EntraId", options => { options.Authority = builder.Configuration["Authentication:EntraId:Authority"]; });
    // }
    _ = auth;

    builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, Crm.Infrastructure.Security.PermissionPolicyProvider>();
    builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Crm.Infrastructure.Security.PermissionAuthorizationHandler>();
    builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, ForbiddenAuditHandler>();
    builder.Services.AddAuthorization();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Opportunity Lifecycle CRM", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme."
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"];
    builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0
                }));
    });

    var cs = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    var useInMemory = cs.Equals("InMemory", StringComparison.OrdinalIgnoreCase)
                      || cs.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase)
                      || builder.Configuration.GetValue("Database:UseInMemory", false);
    var health = builder.Services.AddHealthChecks();
    if (!builder.Environment.IsEnvironment("Testing") && !useInMemory && !string.IsNullOrWhiteSpace(cs))
        health.AddSqlServer(cs, name: "sql", tags: ["ready"]);

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers();
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
    app.MapFallback(() => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found"));

    if (!app.Environment.IsEnvironment("Testing"))
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
        {
            Log.Fatal(ex,
                "Database unavailable. For local dev without Docker, install SQL Server LocalDB " +
                "(winget install Microsoft.SQLServer.2019.LocalDB) or start SQL Server with " +
                "'docker compose up sqlserver' from the repo root, then set ConnectionStrings:DefaultConnection in appsettings.");
            throw;
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
