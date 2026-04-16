# Project Architecture

## Solution Structure

The solution uses the **`.slnx` format** (XML-based, not legacy `.sln`). File: `Questionaire.slnx`. Projects listed:

| Project | Purpose |
|---------|---------|
| `AppHost` | Aspire orchestrator |
| `WebApi` | ASP.NET Core Minimal API |
| `DataAccess` | Data access library |
| `ServiceDefaults` | Shared Aspire infrastructure (OpenTelemetry, health checks, resilience) |
| `WebApiTests` | Integration tests (Aspire TestingBuilder) |
| `DataAccessTests` | Unit tests for DataAccess |

The Angular frontend (`Frontend/`) is **not** a .csproj project; it is added via `AddNpmApp` in the AppHost.

## Aspire AppHost Wiring

**File**: `AppHost/AppHost.cs`

```csharp
var webapi = builder.AddProject<Projects.WebApi>("webapi");

builder.AddNpmApp("frontend", "../Frontend")
    .WithReference(webapi)
    .WaitFor(webapi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();
```

Key details:

- **Resource name `"webapi"`** — Aspire service discovery exposes this as `services__webapi__https__0` and `services__webapi__http__0` environment variables to dependent resources.
- **Resource name `"frontend"`** — the Angular app, served via `pnpm start`.
- `.WithReference(webapi)` injects the WebApi URL into the frontend's environment.
- `.WaitFor(webapi)` ensures WebApi is healthy before starting the frontend.
- `.WithHttpEndpoint(env: "PORT")` tells Aspire to assign a dynamic port and pass it via the `PORT` env var, which the Angular dev server reads.
- `.WithExternalHttpEndpoints()` marks the frontend as externally reachable in the Aspire dashboard.
- **Aspire SDK version**: `13.2.2` (set in `AppHost/AppHost.csproj` via `Aspire.AppHost.Sdk`).

## Frontend-to-API URL Discovery

The Angular app discovers the WebApi URL at runtime through a build-time code generation step.

**File**: `Frontend/build/set-env.js`

This script runs as part of `pnpm start` (before `ng serve`). It:

1. Reads `process.env.services__webapi__https__0` (falls back to `services__webapi__http__0`, then empty string).
2. Writes `Frontend/src/environments/environment.ts` and `environment.development.ts` with the resolved URL.

**Generated file structure**:
```typescript
export const environment = {
  apiBaseUrl: 'https://localhost:7085',  // or whatever Aspire injects
};
```

**Consumed in**: `Frontend/src/app/app.config.ts` via `provideApiConfiguration(environment.apiBaseUrl)`, which sets the root URL for all generated API client calls.

**Important**: `environment.ts` and `environment.development.ts` are **generated files** (listed in `.gitignore`). Do not edit them manually.

## OpenAPI Client Generation Pipeline

End-to-end flow:

1. **WebApi.csproj** has `<OpenApiDocumentsDirectory>.</OpenApiDocumentsDirectory>` — this tells the build to emit the OpenAPI spec to `WebApi/WebApi.json` during build.
2. Run `pnpm run generate-web-api` in `Frontend/`, which executes:
   ```
   ng-openapi-gen --input ../WebApi/WebApi.json --output src/app/api
   ```
3. Generated client code lands in `Frontend/src/app/api/`:
   - `api-configuration.ts` — injectable config with `rootUrl`
   - `api.ts` — main `Api` service with `invoke()` / `invoke$Response()`
   - `fn/` — per-endpoint function files (e.g., `fn/health/ping.ts`)
   - `models.ts` — TypeScript interfaces from OpenAPI schemas
   - `request-builder.ts`, `strict-http-response.ts` — internal utilities

**The `src/app/api/` directory is excluded from ESLint** (see `eslint.config.js`), and `WebApi.json` plus generated API files are in `.gitignore`.

**Tool**: `ng-openapi-gen` v1.0.5 (dev dependency).

## Non-Default Configuration

### Angular (`angular.json`)

- **All schematics skip test file generation** (`"skipTests": true` for component, directive, pipe, service, guard, interceptor, resolver).
- Component style defaults to `css`.

### Angular (`app.config.ts`)

- **Zoneless change detection**: `provideZonelessChangeDetection()` — no `zone.js`, all components must use signals or explicit change detection.
- **Fetch-based HTTP**: `provideHttpClient(withFetch())`.
- **Component input binding from router**: `withComponentInputBinding()`.

### TypeScript (`tsconfig.json`)

- `strict: true` with additional strictness: `noImplicitOverride`, `noImplicitReturns`, `noFallthroughCasesInSwitch`, `isolatedModules`.
- `module: "preserve"` (not CommonJS or ESNext).
- `target: "ES2022"`.

### WebApi (`WebApi.csproj`)

- `<OpenApiDocumentsDirectory>.</OpenApiDocumentsDirectory>` — non-default; causes OpenAPI spec to be written to the project root as `WebApi.json` at build time.
- References both `ServiceDefaults` and `DataAccess`.

### ESLint (`eslint.config.js`)

- Flat config format (not `.eslintrc`).
- Ignores `src/app/api/**` (generated code).
- Enforces `app-` prefix for component selectors (kebab-case) and `app` prefix for directive selectors (camelCase).

### Test runner

- **Vitest** (not Karma/Jasmine) for Angular unit tests. Config in `tsconfig.spec.json` uses `vitest/globals` types.

## xUnit Integration Test Setup

### Test Fixture (`WebApiTests/WebApiTestFixture.cs`)

```csharp
public class WebApiTestFixture : IAsyncLifetime
```

- Implements `IAsyncLifetime` for async setup/teardown.
- `InitializeAsync()`:
  1. Creates `DistributedApplicationTestingBuilder` from `Projects.AppHost`.
  2. Calls `builder.BuildAsync()` then `app.StartAsync()`.
  3. Waits up to **60 seconds** for the `"webapi"` resource to reach `KnownResourceStates.Running` via `app.ResourceNotifications`.
  4. Creates `HttpClient` via `app.CreateHttpClient("webapi")` — this resolves the Aspire service discovery URL automatically.
- `DisposeAsync()`: disposes the `DistributedApplication`.

### Test Pattern (`WebApiTests/PingIntegrationTest.cs`)

```csharp
public class PingIntegrationTest(WebApiTestFixture fixture) : IClassFixture<WebApiTestFixture>
```

- Uses **primary constructor** to receive the fixture.
- `IClassFixture<WebApiTestFixture>` shares the fixture (and thus the running Aspire app) across all tests in the class.
- `fixture.HttpClient` is pre-configured to hit the WebApi resource — no manual URL construction needed.

### Test Project References

- `WebApiTests.csproj` references `AppHost.csproj` (not `WebApi.csproj` directly) — the Aspire TestingBuilder spins up the entire AppHost graph.
- NuGet: `Aspire.Hosting.Testing 13.2.2`, `xunit 2.9.3`, `xunit.runner.visualstudio 3.1.4`.

## WebApi Endpoints and Middleware

**File**: `WebApi/Program.cs`

- `builder.AddServiceDefaults()` — registers OpenTelemetry, health checks, service discovery, resilience.
- CORS: fully open (`AllowAnyOrigin`, `AllowAnyHeader`, `AllowAnyMethod`) via default policy.
- OpenAPI doc served at `/openapi/v1.json`.
- Swagger UI at `/swagger` (endpoint configured as `/openapi/v1.json`).
- Default launch URLs: `http://localhost:5195` / `https://localhost:7085` (from `launchSettings.json`).

## ServiceDefaults

**File**: `ServiceDefaults/Extensions.cs`

Provides `AddServiceDefaults()` and `MapDefaultEndpoints()` extension methods used by all service projects:

- **Health checks**: `/health` (all checks) and `/alive` (self only).
- **OpenTelemetry**: metrics (ASP.NET Core, HTTP client, runtime), tracing (ASP.NET Core excluding `/health`+`/alive`, HTTP client).
- **Resilience**: standard resilience handler for `HttpClient` via `AddStandardResilienceHandler()`.
- **Service discovery**: `AddServiceDiscovery()` on all HTTP clients.
