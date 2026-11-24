# ASP.NET Core (.NET 10) DDD skeleton
#
#   make init        generate files, create solution, restore
#   make docker-up   Postgres + MongoDB + Redis + RabbitMQ
#
# Repo giữ đúng 2 file: Makefile và docker-compose.yml. Mọi file project được
# sinh từ các define bên dưới. File đã tồn tại sẽ không bị ghi đè.

SHELL := /bin/bash
.SHELLFLAGS := -eu -o pipefail -c
.ONESHELL:
.SILENT:
.DELETE_ON_ERROR:
.DEFAULT_GOAL := help

PROJECT ?= Kasio
DOTNET ?= dotnet
CONFIGURATION ?= Debug
SDK_MAJOR ?= 10
SDK_PACKAGE ?= dotnet-sdk-10.0
EF_VERSION ?= 10.0.15
COMPOSE ?= docker compose

DOMAIN := src/$(PROJECT).Domain
CONTRACTS := src/$(PROJECT).Contracts
APPLICATION := src/$(PROJECT).Application
PERSISTENCE := src/$(PROJECT).Infrastructure.Persistence
READ_MODEL := src/$(PROJECT).Infrastructure.Persistence.ReadModel
CACHE := src/$(PROJECT).Infrastructure.Cache
API := host/$(PROJECT).Api
TESTS := tests/$(PROJECT).UnitTests
SLN := $(PROJECT).sln

PROJECTS := \
	$(API)/$(PROJECT).Api.csproj \
	$(APPLICATION)/$(PROJECT).Application.csproj \
	$(CONTRACTS)/$(PROJECT).Contracts.csproj \
	$(DOMAIN)/$(PROJECT).Domain.csproj \
	$(PERSISTENCE)/$(PROJECT).Infrastructure.Persistence.csproj \
	$(READ_MODEL)/$(PROJECT).Infrastructure.Persistence.ReadModel.csproj \
	$(CACHE)/$(PROJECT).Infrastructure.Cache.csproj \
	$(TESTS)/$(PROJECT).UnitTests.csproj

define GITIGNORE :=
# Build output
bin/
obj/
artifacts/
[Dd]ebug/
[Rr]elease/
*.trx
TestResults/
*.coverage
*.coveragexml

# IDE
.idea/
.vs/
.vscode/
*.user
*.suo
*.DotSettings.user

# NuGet
*.nupkg
*.snupkg
.nuget/

# Local overrides and secrets
appsettings.*.local.json
*.pfx
*.snk
.env
.env.*

# Logs and OS noise
logs/
*.log
.DS_Store
Thumbs.db
endef

define BUILD_PROPS :=
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>14.0</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <ContinuousIntegrationBuild Condition="'$$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Meziantou.Analyzer" Version="3.0.342" PrivateAssets="all" />
  </ItemGroup>
</Project>
endef

define EDITORCONFIG :=
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 4

[*.{csproj,props,targets,json,yml,yaml,xml,sln}]
indent_size = 2

[*.cs]
dotnet_sort_system_directives_first = true
csharp_style_namespace_declarations = file_scoped:warning
dotnet_style_qualification_for_field = false:silent
dotnet_style_qualification_for_property = false:silent
dotnet_style_qualification_for_method = false:silent
dotnet_style_qualification_for_event = false:silent
endef

define GLOBAL_JSON :=
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
endef

define ARCHITECTURE_DOC :=
# Architecture rules

## Dependency direction

- `Domain` depends on nothing.
- `Application` depends on `Domain` only. It owns commands, queries, handlers,
  validators, internal DTOs and Mapperly mappers. It must NOT reference `Contracts`.
- `Contracts` holds the public API surface (request/response DTOs) and depends on nothing.
- `Api` depends on `Application`, `Contracts`, `Persistence`, `Persistence.ReadModel`,
  `Cache`, maps Application results to Contracts DTOs (Mapperly) and owns transport concerns
  (ProblemDetails, auth, health, OpenAPI).

Do not add `Application` to `Contracts` references: Contracts is the public contract;
changing it is a breaking change for consumers.

## Storage

- `Persistence` = write model: EF Core on PostgreSQL, migrations via `dotnet ef`.
- `Persistence.ReadModel` = read model: MongoDB via the driver, no EF.
  Bootstrap collections with code-first `$$jsonSchema` validators plus indexes; see
  README of that project when the first read model lands.
- `Cache` wraps `IDistributedCache` over Redis with a fixed `InstanceName`.

## Configuration

- `appsettings.json` contains no credentials and no endpoints.
- Local values live in `appsettings.Development.json`; production values come from
  environment variables (`ConnectionStrings__WriteDb`, `Mongo__ConnectionString`,
  `Jwt__SigningKey`, ...). A missing required value fails startup with
  `InvalidOperationException` - never a silent fallback.

## Errors

- Application handlers return `ErrorOr<T>`; validation failures come from the
  FluentValidation pipeline behavior and become HTTP 400 ProblemDetails in the API.
- Unexpected exceptions become HTTP 500 ProblemDetails (`AddProblemDetails`).
endef

define DOMAIN_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk" />
endef

define CONTRACTS_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk" />
endef

define APPLICATION_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="../$(PROJECT).Domain/$(PROJECT).Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.5.0" />
    <PackageReference Include="FluentValidation" Version="12.2.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.2.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.15" />
    <PackageReference Include="ErrorOr" Version="2.1.1" />
    <PackageReference Include="Riok.Mapperly" Version="4.3.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
endef

define APPLICATION_DI :=
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace $(PROJECT).Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(Behaviors.ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
endef

define VALIDATION_BEHAVIOR :=
using FluentValidation;
using MediatR;

namespace $(PROJECT).Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task
            .WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);
        var failures = results.SelectMany(result => result.Errors).ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken).ConfigureAwait(false);
    }
}
endef

define PERSISTENCE_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="../$(PROJECT).Application/$(PROJECT).Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.15" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.4" />
    <PackageReference Include="EFCore.NamingConventions" Version="10.0.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.15" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.15" />
  </ItemGroup>
</Project>
endef

define PERSISTENCE_DI :=
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace $(PROJECT).Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WriteDb")
            ?? throw new InvalidOperationException("Connection string 'WriteDb' is not configured.");

        services.AddDbContext<$(PROJECT)DbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention());

        return services;
    }
}
endef

define DB_CONTEXT :=
using Microsoft.EntityFrameworkCore;

namespace $(PROJECT).Infrastructure.Persistence;

public class $(PROJECT)DbContext(DbContextOptions<$(PROJECT)DbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof($(PROJECT)DbContext).Assembly);
    }
}
endef

define READ_MODEL_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="../$(PROJECT).Application/$(PROJECT).Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MongoDB.Driver" Version="3.14.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.15" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.15" />
  </ItemGroup>
</Project>
endef

define READ_MODEL_DI :=
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace $(PROJECT).Infrastructure.Persistence.ReadModel;

public static class DependencyInjection
{
    public static IServiceCollection AddReadModel(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["Mongo:ConnectionString"]
            ?? throw new InvalidOperationException("Configuration 'Mongo:ConnectionString' is not set.");
        var databaseName = configuration["Mongo:Database"]
            ?? throw new InvalidOperationException("Configuration 'Mongo:Database' is not set.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

        return services;
    }
}
endef

define CACHE_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="../$(PROJECT).Application/$(PROJECT).Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.15" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.15" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.15" />
  </ItemGroup>
</Project>
endef

define CACHE_DI :=
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace $(PROJECT).Infrastructure.Cache;

public static class DependencyInjection
{
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        var instanceName = configuration["Redis:InstanceName"]
            ?? throw new InvalidOperationException("Configuration 'Redis:InstanceName' is not set.");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = instanceName;
        });

        return services;
    }
}
endef

define API_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <ProjectReference Include="../../$(APPLICATION)/$(PROJECT).Application.csproj" />
    <ProjectReference Include="../../$(CONTRACTS)/$(PROJECT).Contracts.csproj" />
    <ProjectReference Include="../../$(PERSISTENCE)/$(PROJECT).Infrastructure.Persistence.csproj" />
    <ProjectReference Include="../../$(READ_MODEL)/$(PROJECT).Infrastructure.Persistence.ReadModel.csproj" />
    <ProjectReference Include="../../$(CACHE)/$(PROJECT).Infrastructure.Cache.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.15" />
    <PackageReference Include="Scalar.AspNetCore" Version="3.2.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="11.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.15" />
    <PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="9.0.0" />
    <PackageReference Include="AspNetCore.HealthChecks.MongoDb" Version="9.0.0" />
    <PackageReference Include="AspNetCore.HealthChecks.Redis" Version="9.0.0" />
    <!-- Hangfire.Core chi co target netstandard2.0, keo Newtonsoft.Json 11.0.1 (NU1903); pin ban va -->
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
    <!-- dotnet ef can package nay o startup project; PrivateAssets de khong copy ra output -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.15">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="DotNetCore.CAP" Version="10.0.2" />
    <PackageReference Include="DotNetCore.CAP.RabbitMQ" Version="10.0.2" />
    <PackageReference Include="DotNetCore.CAP.PostgreSql" Version="10.0.2" />
    <PackageReference Include="Hangfire.AspNetCore" Version="1.8.25" />
    <PackageReference Include="Hangfire.PostgreSql" Version="1.21.1" />
  </ItemGroup>
</Project>
endef

define API_PROGRAM :=
using $(PROJECT).Api;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
    builder.Services.AddOpenApi();
    builder.Services.AddApi(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.MapControllers();
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready", StringComparer.Ordinal),
    });

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
endef

define API_DI :=
using System.Text;
using $(PROJECT).Application;
using $(PROJECT).Infrastructure.Cache;
using $(PROJECT).Infrastructure.Persistence;
using $(PROJECT).Infrastructure.Persistence.ReadModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace $(PROJECT).Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddPersistence(configuration);
        services.AddReadModel(configuration);
        services.AddCache(configuration);
        services.AddApiAuthentication(configuration);
        services.AddApiHealthChecks(configuration);

        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();

        return services;
    }

    private static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var signingKey = Required(configuration, "Jwt:SigningKey");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Required(configuration, "Jwt:Issuer"),
                    ValidateAudience = true,
                    ValidAudience = Required(configuration, "Jwt:Audience"),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();

        return services;
    }

    private static IServiceCollection AddApiHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(Required(configuration, "ConnectionStrings:WriteDb"), name: "postgres",
                tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(3))
            .AddMongoDb(provider => provider.GetRequiredService<IMongoClient>(), name: "mongo",
                tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(3))
            .AddRedis(Required(configuration, "ConnectionStrings:Redis"), name: "redis",
                tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(3));

        return services;
    }

    private static string Required(IConfiguration configuration, string key)
        => configuration[key]
           ?? throw new InvalidOperationException("Configuration '" + key + "' is not set.");
}
endef

define VALIDATION_EXCEPTION_HANDLER :=
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace $(PROJECT).Api;

internal sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var errors = validationException.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problem = new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return true;
    }
}
endef

define APPSETTINGS :=
{
  "Redis": {
    "InstanceName": "kasio:"
  },
  "Jwt": {
    "Issuer": "kasio",
    "Audience": "kasio-api"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  }
}
endef

define LAUNCH_SETTINGS :=
{
  "$$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "scalar",
      "applicationUrl": "http://localhost:5080",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
endef

define APPSETTINGS_DEV :=
{
  "ConnectionStrings": {
    "WriteDb": "Host=localhost;Port=5432;Database=kasio;Username=postgres",
    "Redis": "localhost:6379"
  },
  "Mongo": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "kasio-read"
  },
  "Jwt": {
    "SigningKey": "kasio-dev-signing-key-change-me-32-bytes"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  }
}
endef

define TESTS_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.2" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../$(APPLICATION)/$(PROJECT).Application.csproj" />
  </ItemGroup>
</Project>
endef


define TESTS_CODE :=
using FluentValidation;
using $(PROJECT).Application.Behaviors;
using MediatR;
using Xunit;

namespace $(PROJECT).UnitTests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_throws_when_a_validation_rule_fails()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new Ping(string.Empty), next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_calls_next_when_all_rules_pass()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var response = await behavior.Handle(new Ping("value"), next, CancellationToken.None);

        Assert.Equal("ok", response);
    }

    public sealed record Ping(string Value) : IRequest<string>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(request => request.Value).NotEmpty();
    }
}
endef

.PHONY: help init files dirs ensure-dotnet solution restore ef-tool test \
	docker-up docker-down docker-logs clean

help:
	printf 'ASP.NET Core DDD skeleton - project: %s\n\n' '$(PROJECT)'
	printf 'Usage: make <target>\n\n'
	printf '  %-20s %s\n' 'init' 'Generate files + solution + restore'
	printf '  %-20s %s\n' 'files' 'Generate the project files only'
	printf '  %-20s %s\n' 'ensure-dotnet' 'Install .NET SDK 10 with dnf when missing'
	printf '  %-20s %s\n' 'ef-tool' 'Install local dotnet-ef (needed by Rider)'
	printf '  %-20s %s\n' 'test' 'dotnet test'
	printf '  %-20s %s\n' 'docker-up' 'Start Postgres, MongoDB, Redis, RabbitMQ'
	printf '  %-20s %s\n' 'docker-down' 'Stop the local infrastructure'
	printf '  %-20s %s\n' 'docker-logs' 'Follow container logs'
	printf '  %-20s %s\n' 'clean' 'Remove bin/obj output'
	printf '\nDoi ten project: make init PROJECT=MyService\n'

init: ensure-dotnet solution restore ef-tool
	printf '\n%s\n' 'Scaffold complete.'

ensure-dotnet:
	if $(DOTNET) --list-sdks 2>/dev/null | cut -d. -f1 | grep -qx '$(SDK_MAJOR)'; then
		printf 'dotnet SDK %s present: %s\n' '$(SDK_MAJOR)' "$$($(DOTNET) --version)"
	else
		printf 'dotnet SDK %s missing - installing %s\n' '$(SDK_MAJOR)' '$(SDK_PACKAGE)'
		sudo dnf install -y $(SDK_PACKAGE)
		$(DOTNET) --list-sdks | cut -d. -f1 | grep -qx '$(SDK_MAJOR)' || { printf 'error: dotnet %s is still not installed\n' '$(SDK_MAJOR)'; exit 1; }
	fi

dirs:
	mkdir -p $(API)/Properties $(APPLICATION)/Behaviors $(PERSISTENCE) $(READ_MODEL) $(CACHE) $(DOMAIN) $(CONTRACTS) $(TESTS)

files: dirs
	$(if $(wildcard .gitignore),,$(file >.gitignore,$(GITIGNORE)))
	$(if $(wildcard Directory.Build.props),,$(file >Directory.Build.props,$(BUILD_PROPS)))
	$(if $(wildcard .editorconfig),,$(file >.editorconfig,$(EDITORCONFIG)))
	$(if $(wildcard global.json),,$(file >global.json,$(GLOBAL_JSON)))
	$(if $(wildcard ARCHITECTURE.md),,$(file >ARCHITECTURE.md,$(ARCHITECTURE_DOC)))
	$(if $(wildcard $(DOMAIN)/$(PROJECT).Domain.csproj),,$(file >$(DOMAIN)/$(PROJECT).Domain.csproj,$(DOMAIN_CSPROJ)))
	$(if $(wildcard $(CONTRACTS)/$(PROJECT).Contracts.csproj),,$(file >$(CONTRACTS)/$(PROJECT).Contracts.csproj,$(CONTRACTS_CSPROJ)))
	$(if $(wildcard $(APPLICATION)/$(PROJECT).Application.csproj),,$(file >$(APPLICATION)/$(PROJECT).Application.csproj,$(APPLICATION_CSPROJ)))
	$(if $(wildcard $(APPLICATION)/DependencyInjection.cs),,$(file >$(APPLICATION)/DependencyInjection.cs,$(APPLICATION_DI)))
	$(if $(wildcard $(APPLICATION)/Behaviors/ValidationBehavior.cs),,$(file >$(APPLICATION)/Behaviors/ValidationBehavior.cs,$(VALIDATION_BEHAVIOR)))
	$(if $(wildcard $(PERSISTENCE)/$(PROJECT).Infrastructure.Persistence.csproj),,$(file >$(PERSISTENCE)/$(PROJECT).Infrastructure.Persistence.csproj,$(PERSISTENCE_CSPROJ)))
	$(if $(wildcard $(PERSISTENCE)/DependencyInjection.cs),,$(file >$(PERSISTENCE)/DependencyInjection.cs,$(PERSISTENCE_DI)))
	$(if $(wildcard $(PERSISTENCE)/$(PROJECT)DbContext.cs),,$(file >$(PERSISTENCE)/$(PROJECT)DbContext.cs,$(DB_CONTEXT)))
	$(if $(wildcard $(READ_MODEL)/$(PROJECT).Infrastructure.Persistence.ReadModel.csproj),,$(file >$(READ_MODEL)/$(PROJECT).Infrastructure.Persistence.ReadModel.csproj,$(READ_MODEL_CSPROJ)))
	$(if $(wildcard $(READ_MODEL)/DependencyInjection.cs),,$(file >$(READ_MODEL)/DependencyInjection.cs,$(READ_MODEL_DI)))
	$(if $(wildcard $(CACHE)/$(PROJECT).Infrastructure.Cache.csproj),,$(file >$(CACHE)/$(PROJECT).Infrastructure.Cache.csproj,$(CACHE_CSPROJ)))
	$(if $(wildcard $(CACHE)/DependencyInjection.cs),,$(file >$(CACHE)/DependencyInjection.cs,$(CACHE_DI)))
	$(if $(wildcard $(API)/$(PROJECT).Api.csproj),,$(file >$(API)/$(PROJECT).Api.csproj,$(API_CSPROJ)))
	$(if $(wildcard $(API)/Program.cs),,$(file >$(API)/Program.cs,$(API_PROGRAM)))
	$(if $(wildcard $(API)/ValidationExceptionHandler.cs),,$(file >$(API)/ValidationExceptionHandler.cs,$(VALIDATION_EXCEPTION_HANDLER)))
	$(if $(wildcard $(API)/DependencyInjection.cs),,$(file >$(API)/DependencyInjection.cs,$(API_DI)))
	$(if $(wildcard $(API)/appsettings.json),,$(file >$(API)/appsettings.json,$(APPSETTINGS)))
	$(if $(wildcard $(API)/appsettings.Development.json),,$(file >$(API)/appsettings.Development.json,$(APPSETTINGS_DEV)))
	$(if $(wildcard $(API)/Properties/launchSettings.json),,$(file >$(API)/Properties/launchSettings.json,$(LAUNCH_SETTINGS)))
	$(if $(wildcard $(TESTS)/$(PROJECT).UnitTests.csproj),,$(file >$(TESTS)/$(PROJECT).UnitTests.csproj,$(TESTS_CSPROJ)))
	$(if $(wildcard $(TESTS)/ValidationBehaviorTests.cs),,$(file >$(TESTS)/ValidationBehaviorTests.cs,$(TESTS_CODE)))
	printf '  files ready\n'

solution: files
	if [ ! -f $(SLN) ]; then
		$(DOTNET) new sln --name $(PROJECT) --format sln
	fi
	for project in $(PROJECTS); do
		if ! $(DOTNET) sln $(SLN) list | grep -qF "$$project"; then
			$(DOTNET) sln $(SLN) add "$$project"
		fi
	done

restore: solution
	$(DOTNET) restore $(SLN) --nologo

ef-tool:
	if [ ! -f dotnet-tools.json ]; then $(DOTNET) new tool-manifest >/dev/null; fi
	if ! $(DOTNET) tool list | grep -q 'dotnet-ef'; then $(DOTNET) tool install dotnet-ef --version $(EF_VERSION); fi
	printf 'dotnet-ef %s ready\n' '$(EF_VERSION)'

test: solution
	$(DOTNET) test $(SLN) -c $(CONFIGURATION) --nologo

docker-up:
	$(COMPOSE) up -d --wait

docker-down:
	$(COMPOSE) down

docker-logs:
	$(COMPOSE) logs -f --tail=100

clean:
	$(DOTNET) clean $(SLN) --nologo || true
	find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
	printf 'clean done\n'
