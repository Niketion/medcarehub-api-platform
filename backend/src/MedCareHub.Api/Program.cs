using MedCareHub.Api.Auth;
using MedCareHub.Api.Data;
using MedCareHub.Api.Health;
using MedCareHub.Api.Middleware;
using MedCareHub.Api.Services;
using MedCareHub.Api.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Load base configuration, optional environment-specific overrides and environment variables.
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Configure Serilog as the application logger.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Register EF Core with PostgreSQL.
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var authSection = builder.Configuration.GetSection("Auth");
var authority = authSection.GetValue<string>("Authority")
    ?? throw new InvalidOperationException("Auth:Authority missing");

var validateAudience = authSection.GetValue<bool>("ValidateAudience");
var validateIssuer = authSection.GetValue<bool>("ValidateIssuer");
var audience = authSection.GetValue<string>("Audience");

// Configure JWT bearer authentication against Keycloak.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new()
        {
            ValidateAudience = validateAudience,
            ValidAudience = validateAudience ? audience : null,
            ValidateIssuer = validateIssuer,
            ValidIssuer = validateIssuer ? authority : null,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    });

// Flatten Keycloak role claims into the role claim type expected by ASP.NET authorization.
builder.Services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

// Define authorization policies used across the API.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Patient, p => p.RequireRole(Roles.Patient));
    options.AddPolicy(Policies.Operator, p => p.RequireRole(Roles.Operator, Roles.Admin));
    options.AddPolicy(Policies.Doctor, p => p.RequireRole(Roles.Doctor, Roles.Admin));
    options.AddPolicy(Policies.Staff, p => p.RequireRole(Roles.Operator, Roles.Doctor, Roles.Admin));
});

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Configure browser-origin access for the SPA frontend.
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("default", p =>
    {
        p.WithOrigins(origins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    });
});

// Register storage services for report persistence in MinIO.
builder.Services.AddSingleton<IMinioClientFactory, MinioClientFactory>();
builder.Services.AddScoped<IReportStorage, MinioReportStorage>();

// Register application services.
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// Register middleware and health checks.
builder.Services.AddTransient<ApiExceptionMiddleware>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!, name: "postgres")
    .AddCheck<MinioHealthCheck>("minio");

// Startup hosted services:
// - database migrations
// - MinIO bucket bootstrap
builder.Services.AddHostedService<DatabaseMigrationHostedService>();
builder.Services.AddHostedService<MinioBootstrapHostedService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MedCareHub API", Version = "v1" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Inserisci: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Request logging and centralized exception handling.
app.UseSerilogRequestLogging();
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseStatusCodePages();

// Expose OpenAPI and Swagger UI in the current MVP environment.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Basic liveness endpoint.
app.MapHealthChecks("/health");

// Readiness endpoint with a JSON payload describing each registered check.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description
            })
        };

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.Run();