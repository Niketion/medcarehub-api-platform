using System.Security.Claims;
using MedCareHub.Api.Auth;
using MedCareHub.Api.Data;
using MedCareHub.Api.Middleware;
using MedCareHub.Api.Storage;
using MedCareHub.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var authSection = builder.Configuration.GetSection("Auth");
var authority = authSection.GetValue<string>("Authority") ?? throw new InvalidOperationException("Auth:Authority missing");

var validateAudience = authSection.GetValue<bool>("ValidateAudience");
var validateIssuer = authSection.GetValue<bool>("ValidateIssuer");
var audience = authSection.GetValue<string>("Audience");

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

builder.Services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Patient, p => p.RequireRole(Roles.Patient));
    options.AddPolicy(Policies.Operator, p => p.RequireRole(Roles.Operator, Roles.Admin));
    options.AddPolicy(Policies.Doctor, p => p.RequireRole(Roles.Doctor, Roles.Admin));
    options.AddPolicy(Policies.Staff, p => p.RequireRole(Roles.Operator, Roles.Doctor, Roles.Admin));
});

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
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

builder.Services.AddSingleton<IMinioClientFactory, MinioClientFactory>();
builder.Services.AddScoped<IReportStorage, MinioReportStorage>();

builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddTransient<ApiExceptionMiddleware>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!, name: "postgres");

builder.Services.AddHostedService<DatabaseMigrationHostedService>();
builder.Services.AddHostedService<MinioBootstrapHostedService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MedCareHub API", Version = "v1" });

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
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();