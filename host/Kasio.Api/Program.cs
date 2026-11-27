using System.Text;
using Kasio.Api.Authorization;
using Kasio.Api.Health;
using Kasio.Application;
using Kasio.Infrastructure.Cache;
using Kasio.Infrastructure.Persistence;
using Kasio.Infrastructure.Persistence.ReadModel;
using Kasio.Infrastructure.Persistence.Security;
using Kasio.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Driver;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.AddProblemDetails();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "Kasio API";

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT access token: Authorization: Bearer <token>",
            };

            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document, null)] = [],
            });

            return Task.CompletedTask;
        });
    });

    builder.Services.AddApplication();
    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddPermissionAuthorization();
    builder.Services.AddCache(builder.Configuration);
    builder.Services.AddReadModel(builder.Configuration);

    // Readiness kiểm tra hạ tầng thật; liveness không phụ thuộc gì (xem MapHealthChecks bên dưới).
    var writeDbConnection = builder.Configuration.GetConnectionString("WriteDb")
        ?? throw new InvalidOperationException("Thiếu ConnectionStrings:WriteDb.");
    var redisConnection = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Redis.");

    builder.Services.AddHealthChecks()
        .AddNpgSql(writeDbConnection, name: "postgres", timeout: TimeSpan.FromSeconds(5))
        .AddMongoDb(
            provider => provider.GetRequiredService<IMongoClient>(),
            name: "mongo",
            timeout: TimeSpan.FromSeconds(5))
        .AddRedis(redisConnection, name: "redis", timeout: TimeSpan.FromSeconds(5));

    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Thiếu cấu hình Jwt.");

    var jwtSecret = jwtOptions.Secret;
    if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
        throw new InvalidOperationException("Jwt:Secret phải dài ít nhất 32 ký tự.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateLifetime = true
            };
        });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowCors", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                          ?? ["http://localhost:3000"];
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    var app = builder.Build();

    if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
        await app.MigrateAndSeedAsync();

    app.UseCors("AllowCors");

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseAuthentication();
    app.UseAuthorization();

    // Liveness: process còn sống, không chạm hạ tầng → không bị restart vì Redis/DB chết.
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

    // Readiness: Postgres + Mongo + Redis. Hỏng một cái → 503 để ngừng nhận traffic.
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteAsync,
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options => options.WithTitle("Kasio API"));
        app.MapGet("/", () => Results.Redirect("/scalar/index.html")).ExcludeFromDescription();
    }

    app.MapControllers();

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

public partial class Program;
