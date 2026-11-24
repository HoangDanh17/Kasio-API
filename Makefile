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
QUERY := src/$(PROJECT).Infrastructure.Persistence.Query
CACHE := src/$(PROJECT).Infrastructure.Cache
HEALTH := src/$(PROJECT).Infrastructure.Health
API := host/$(PROJECT).Api
TESTS := tests/$(PROJECT).UnitTests
INTEGRATION_TESTS := tests/$(PROJECT).IntegrationTests
SLN := $(PROJECT).sln

PROJECTS := \
	$(API)/$(PROJECT).Api.csproj \
	$(APPLICATION)/$(PROJECT).Application.csproj \
	$(CONTRACTS)/$(PROJECT).Contracts.csproj \
	$(DOMAIN)/$(PROJECT).Domain.csproj \
	$(PERSISTENCE)/$(PROJECT).Infrastructure.Persistence.csproj \
	$(QUERY)/$(PROJECT).Infrastructure.Persistence.Query.csproj \
	$(CACHE)/$(PROJECT).Infrastructure.Cache.csproj \
	$(HEALTH)/$(PROJECT).Infrastructure.Health.csproj \
	$(TESTS)/$(PROJECT).UnitTests.csproj \
	$(INTEGRATION_TESTS)/$(PROJECT).IntegrationTests.csproj

define GITIGNORE :=
bin/
obj/
TestResults/
*.user
.vs/
.idea/
endef

define BUILD_PROPS :=
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
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
    <ProjectReference Include="../$(PROJECT).Contracts/$(PROJECT).Contracts.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.5.0" />
    <PackageReference Include="FluentValidation" Version="12.2.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.2.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.15" />
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
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(result => result.Errors).ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
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

        services.AddDbContext<$(PROJECT)DbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
endef

define DB_CONTEXT :=
using Microsoft.EntityFrameworkCore;

namespace $(PROJECT).Infrastructure.Persistence;

public sealed class $(PROJECT)DbContext(DbContextOptions<$(PROJECT)DbContext> options) : DbContext(options)
{
}
endef

define QUERY_CSPROJ :=
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

define QUERY_DI :=
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace $(PROJECT).Infrastructure.Persistence.Query;

public static class DependencyInjection
{
    public static IServiceCollection AddQuery(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ReadDb")
            ?? throw new InvalidOperationException("Connection string 'ReadDb' is not configured.");
        var databaseName = configuration["ReadDb:Database"] ?? "kasio-read";

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
    <PackageReference Include="StackExchange.Redis" Version="2.13.23" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.15" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.15" />
  </ItemGroup>
</Project>
endef

define CACHE_DI :=
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace $(PROJECT).Infrastructure.Cache;

public static class DependencyInjection
{
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));

        return services;
    }
}
endef

define HEALTH_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="AspNetCore.HealthChecks.MongoDb" Version="9.0.0" />
    <PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="10.0.15" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.15" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.15" />
  </ItemGroup>
</Project>
endef

define HEALTH_DI :=
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace $(PROJECT).Infrastructure.Health;

public static class DependencyInjection
{
    public static IServiceCollection AddPlatformHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var writeDb = configuration.GetConnectionString("WriteDb")
            ?? throw new InvalidOperationException("Connection string 'WriteDb' is not configured.");

        services.AddHealthChecks()
            .AddNpgSql(
                writeDb,
                name: "postgres",
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(2))
            .AddMongoDb(
                provider => provider.GetRequiredService<IMongoDatabase>(),
                name: "mongodb",
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(2));

        return services;
    }
}
endef

define API_CSPROJ :=
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <ProjectReference Include="../../$(APPLICATION)/$(PROJECT).Application.csproj" />
    <ProjectReference Include="../../$(PERSISTENCE)/$(PROJECT).Infrastructure.Persistence.csproj" />
    <ProjectReference Include="../../$(QUERY)/$(PROJECT).Infrastructure.Persistence.Query.csproj" />
    <ProjectReference Include="../../$(CACHE)/$(PROJECT).Infrastructure.Cache.csproj" />
    <ProjectReference Include="../../$(HEALTH)/$(PROJECT).Infrastructure.Health.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="11.0.1" />
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

define HEALTH_RESPONSE_WRITER :=
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace $(PROJECT).Api.Health;

internal static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.Headers["Cache-Control"] = "no-store";

        var body = JsonSerializer.Serialize(new { status = report.Status.ToString() });
        return context.Response.WriteAsync(body);
    }
}
endef

define API_PROGRAM :=
using $(PROJECT).Api;
using $(PROJECT).Api.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync,
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync,
}).AllowAnonymous();

app.Run();

public partial class Program;
endef

define API_DI :=
using $(PROJECT).Application;
using $(PROJECT).Infrastructure.Cache;
using $(PROJECT).Infrastructure.Health;
using $(PROJECT).Infrastructure.Persistence;
using $(PROJECT).Infrastructure.Persistence.Query;

namespace $(PROJECT).Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddPersistence(configuration);
        services.AddQuery(configuration);
        services.AddCache(configuration);
        services.AddPlatformHealthChecks(configuration);

        return services;
    }
}
endef

define APPSETTINGS :=
{
  "ConnectionStrings": {
    "WriteDb": "Host=localhost;Port=5432;Database=kasio;Username=postgres",
    "ReadDb": "mongodb://localhost:27017",
    "Redis": "localhost:6379"
  },
  "ReadDb": {
    "Database": "kasio-read"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
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
      "launchUrl": "swagger",
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
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
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

define INTEGRATION_TESTS_CSPROJ :=
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
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.15" />
    <PackageReference Include="Testcontainers.MongoDb" Version="4.17.0" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.17.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../$(API)/$(PROJECT).Api.csproj" />
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
	mkdir -p $(API)/Properties $(API)/Health $(APPLICATION)/Behaviors $(PERSISTENCE) $(QUERY) $(CACHE) $(HEALTH) $(DOMAIN) $(CONTRACTS) $(TESTS) $(INTEGRATION_TESTS)

files: dirs
	$(if $(wildcard .gitignore),,$(file >.gitignore,$(GITIGNORE)))
	$(if $(wildcard Directory.Build.props),,$(file >Directory.Build.props,$(BUILD_PROPS)))
	$(if $(wildcard $(DOMAIN)/$(PROJECT).Domain.csproj),,$(file >$(DOMAIN)/$(PROJECT).Domain.csproj,$(DOMAIN_CSPROJ)))
	$(if $(wildcard $(CONTRACTS)/$(PROJECT).Contracts.csproj),,$(file >$(CONTRACTS)/$(PROJECT).Contracts.csproj,$(CONTRACTS_CSPROJ)))
	$(if $(wildcard $(APPLICATION)/$(PROJECT).Application.csproj),,$(file >$(APPLICATION)/$(PROJECT).Application.csproj,$(APPLICATION_CSPROJ)))
	$(if $(wildcard $(APPLICATION)/DependencyInjection.cs),,$(file >$(APPLICATION)/DependencyInjection.cs,$(APPLICATION_DI)))
	$(if $(wildcard $(APPLICATION)/Behaviors/ValidationBehavior.cs),,$(file >$(APPLICATION)/Behaviors/ValidationBehavior.cs,$(VALIDATION_BEHAVIOR)))
	$(if $(wildcard $(PERSISTENCE)/$(PROJECT).Infrastructure.Persistence.csproj),,$(file >$(PERSISTENCE)/$(PROJECT).Infrastructure.Persistence.csproj,$(PERSISTENCE_CSPROJ)))
	$(if $(wildcard $(PERSISTENCE)/DependencyInjection.cs),,$(file >$(PERSISTENCE)/DependencyInjection.cs,$(PERSISTENCE_DI)))
	$(if $(wildcard $(PERSISTENCE)/$(PROJECT)DbContext.cs),,$(file >$(PERSISTENCE)/$(PROJECT)DbContext.cs,$(DB_CONTEXT)))
	$(if $(wildcard $(QUERY)/$(PROJECT).Infrastructure.Persistence.Query.csproj),,$(file >$(QUERY)/$(PROJECT).Infrastructure.Persistence.Query.csproj,$(QUERY_CSPROJ)))
	$(if $(wildcard $(QUERY)/DependencyInjection.cs),,$(file >$(QUERY)/DependencyInjection.cs,$(QUERY_DI)))
	$(if $(wildcard $(CACHE)/$(PROJECT).Infrastructure.Cache.csproj),,$(file >$(CACHE)/$(PROJECT).Infrastructure.Cache.csproj,$(CACHE_CSPROJ)))
	$(if $(wildcard $(CACHE)/DependencyInjection.cs),,$(file >$(CACHE)/DependencyInjection.cs,$(CACHE_DI)))
	$(if $(wildcard $(HEALTH)/$(PROJECT).Infrastructure.Health.csproj),,$(file >$(HEALTH)/$(PROJECT).Infrastructure.Health.csproj,$(HEALTH_CSPROJ)))
	$(if $(wildcard $(HEALTH)/DependencyInjection.cs),,$(file >$(HEALTH)/DependencyInjection.cs,$(HEALTH_DI)))
	$(if $(wildcard $(API)/$(PROJECT).Api.csproj),,$(file >$(API)/$(PROJECT).Api.csproj,$(API_CSPROJ)))
	$(if $(wildcard $(API)/Program.cs),,$(file >$(API)/Program.cs,$(API_PROGRAM)))
	$(if $(wildcard $(API)/Health/HealthResponseWriter.cs),,$(file >$(API)/Health/HealthResponseWriter.cs,$(HEALTH_RESPONSE_WRITER)))
	$(if $(wildcard $(API)/DependencyInjection.cs),,$(file >$(API)/DependencyInjection.cs,$(API_DI)))
	$(if $(wildcard $(API)/appsettings.json),,$(file >$(API)/appsettings.json,$(APPSETTINGS)))
	$(if $(wildcard $(API)/appsettings.Development.json),,$(file >$(API)/appsettings.Development.json,$(APPSETTINGS_DEV)))
	$(if $(wildcard $(API)/Properties/launchSettings.json),,$(file >$(API)/Properties/launchSettings.json,$(LAUNCH_SETTINGS)))
	$(if $(wildcard $(TESTS)/$(PROJECT).UnitTests.csproj),,$(file >$(TESTS)/$(PROJECT).UnitTests.csproj,$(TESTS_CSPROJ)))
	$(if $(wildcard $(TESTS)/ValidationBehaviorTests.cs),,$(file >$(TESTS)/ValidationBehaviorTests.cs,$(TESTS_CODE)))
	$(if $(wildcard $(INTEGRATION_TESTS)/$(PROJECT).IntegrationTests.csproj),,$(file >$(INTEGRATION_TESTS)/$(PROJECT).IntegrationTests.csproj,$(INTEGRATION_TESTS_CSPROJ)))
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
