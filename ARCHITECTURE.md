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
  Bootstrap collections with code-first `$jsonSchema` validators plus indexes; see
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
