# StackDuel Server

[![Quality gate status](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=stackduel_server) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=coverage)](https://sonarcloud.io/summary/new_code?id=stackduel_server) [![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=stackduel_server) [![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=stackduel_server) [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=stackduel_server) [![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=stackduel_server) [![Security issues](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=software_quality_security_issues)](https://sonarcloud.io/summary/new_code?id=stackduel_server) [![Maintainability issues](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=software_quality_maintainability_issues)](https://sonarcloud.io/summary/new_code?id=stackduel_server) [![Reliability issues](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=software_quality_reliability_issues)](https://sonarcloud.io/summary/new_code?id=stackduel_server) [![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=stackduel_server&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=stackduel_server)

Backend for StackDuel, a competitive programming platform. Built with .NET.

## Requirements

- .NET 10 SDK
- docker
- dotnet-ef CLI

```
dotnet tool install --global dotnet-ef
```

### Getting started

To get started with the project. You need a postgresql server and a message broker. You can use the docker compose file to spin this up. To do so run the command:

```
docker-compose up
```

### Configuration (user secrets)

The API reads its config from `dotnet user-secrets` locally — never put real credentials in `appsettings*.json`. Run the setup script for your platform and answer the prompts (press Enter to accept the shown default, or type `skip` to leave a key untouched):

```
./scripts/setup-user-secrets.sh       # macOS/Linux/Git Bash
./scripts/setup-user-secrets.ps1      # Windows PowerShell
```

Where each value comes from:

- **Auth0 Domain / Audience** — create an API in your Auth0 tenant (Applications → APIs); the API's Identifier is the Audience, and your tenant domain (e.g. `your-tenant.us.auth0.com`) is the Domain. This needs to match whatever `stackduel/web`'s `AUTH0_AUDIENCE` / `AUTH0_DOMAIN` are pointed at — this API validates tokens issued for that Audience.
- **Connection string** — defaults to the docker-compose Postgres above; only change it if you're pointing at a different database.
- **Judge0 BaseUrl / Host / ApiKey** — code execution runs through the Judge0 CE API via RapidAPI. Subscribe at [RapidAPI's Judge0 CE listing](https://rapidapi.com/judge0-official/api/judge0-ce), then use your RapidAPI key as `ApiKey`. The default `BaseUrl`/`Host` already match Judge0 CE's RapidAPI endpoint — you shouldn't need to change them.
- **MessageBus** — defaults to the docker-compose RabbitMQ above; only switch to `AzureServiceBus` if you're deliberately pointing at a real Service Bus namespace.

Java language-server support is separate and optional — `scripts/download-jdtls.sh` (or `.ps1`) downloads the language server and prints the additional `LanguageServer:*` user-secrets commands to run.

### Migrations

Migrations are managed in `StackDuel.Infrastructure`. To add a new migration:

```
dotnet ef migrations add <MigrationName> --project StackDuel.Infrastructure --context StackDuelDbContext --startup-project StackDuel.Api
```

To apply migrations against the local database (requires the docker-compose Postgres to be running):

```
dotnet ef database update --project StackDuel.Infrastructure --context StackDuelDbContext --startup-project StackDuel.Api
```

To apply migrations against an external database:

```
dotnet ef database update --project StackDuel.Infrastructure --context StackDuelDbContext --connection "<connection string>"
```

### Seeding

Seed everything (languages + demo problems with tags):

```
dotnet run --project StackDuel.Seeder -- --all
```

Seed only static data (languages):

```
dotnet run --project StackDuel.Seeder -- --static
```

Seed only demo data (problems, test suites, tags):

```
dotnet run --project StackDuel.Seeder -- --demo
```

### Testing

Two kinds of test project, covering different layers:

- **Unit tests** — `StackDuel.Application.Tests`, `StackDuel.Domain.Tests`
- **Integration tests** — `StackDuel.IntegrationTests`

#### Unit tests

```
dotnet test StackDuel.Application.Tests
dotnet test StackDuel.Domain.Tests
```

#### Integration tests

Requires Docker running — nothing else. `StackDuel.IntegrationTests` spins up a disposable Postgres container via [Testcontainers](https://testcontainers.com/), runs the app's own EF Core migrations against it, and tears it down when the run finishes. You don't need `docker-compose up` first, and there's no manual migration or seeding step.

```
dotnet test StackDuel.IntegrationTests
```

Authentication is swapped for a test scheme that trusts an `X-Test-Sub` header instead of a real Auth0 token, so tests can authenticate as any user (created through the real `PUT /api/v1/user` endpoint, or seeded directly for RBAC fixtures like roles/permissions/groups that have no HTTP surface) without minting JWTs. One real external dependency is stubbed out: the RabbitMQ-backed message consumer, which would otherwise try to open a connection to a broker at startup — everything else (Quartz, SignalR, Judge0's HTTP client, Azure Blob storage) starts up as it would in production.

#### Running everything

```
dotnet test Server.slnx
```

runs both unit and integration tests together — Docker must be running for the integration project to pass.

### Feature flags

Feature flags live in `StackDuel.Domain/FeatureFlags` (evaluation logic in `StackDuel.Application/FeatureFlags`), as their own bounded context separate from RBAC — they're operational toggles, not permanent access grants, though group targeting reuses the existing `Group` entity. `dotnet run --project StackDuel.Seeder -- --static` (or `--all`) creates the seeded `leaderboards` flag; without it, the flag row doesn't exist and resolves as disabled everywhere (evaluation fails closed on an unknown key).

There's no admin UI yet — flags are managed entirely through the API below.

**Check what's resolved for the current caller** (public, no auth — this is what the frontend reads):

```
GET /api/v1/feature-flag
→ { "leaderboards": true }
```

**Admin endpoints** — all require `[RequireUser]` + the `feature-flag:manage:admin` permission (already granted to the seeded `admin` group/role):

| Method | Route                                                   | Body                        | Does                              |
| ------ | -------------------------------------------------------- | ---------------------------- | ---------------------------------- |
| GET    | `/api/v1/feature-flag/admin`                             | —                             | List all flags with their overrides |
| GET    | `/api/v1/feature-flag/admin/{key}`                       | —                             | Get one flag by key                |
| POST   | `/api/v1/feature-flag/admin`                             | `{ key, name, description, defaultEnabled }` | Create a new flag |
| PUT    | `/api/v1/feature-flag/admin/{id}/default`                | `{ defaultEnabled }`         | **Global kill-switch** — on/off for everyone |
| PUT    | `/api/v1/feature-flag/admin/{id}/rollout`                | `{ rolloutPercentage }` (0-100) | **Canary rollout** to a deterministic % of users |
| PUT    | `/api/v1/feature-flag/admin/{id}/user-overrides/{userId}`  | `{ effect: "Allow" \| "Deny" }` | **Per-user override** |
| DELETE | `/api/v1/feature-flag/admin/{id}/user-overrides/{userId}`  | —                             | Remove a per-user override         |
| PUT    | `/api/v1/feature-flag/admin/{id}/group-overrides/{groupId}` | `{ effect: "Allow" \| "Deny" }` | **Per-group override** |
| DELETE | `/api/v1/feature-flag/admin/{id}/group-overrides/{groupId}` | —                             | Remove a per-group override        |

**Evaluation precedence** (first match wins; `Deny` beats `Allow` within a layer):

1. Explicit user override
2. Explicit group override (find group ids via `GET /api/v1/group`)
3. Percentage rollout bucket — deterministic hash of `flagKey:userId`, so a user stays in/out consistently across requests without storing a per-user assignment. Anonymous callers never participate in rollout.
4. The flag's global default (`defaultEnabled`)

Group targeting reuses the existing RBAC groups. Only `default-user` and `admin` are seeded today — there's no "create group" endpoint yet, so targeting a new cohort (e.g. `beta-testers`) means adding it to `WellKnownAuthorization` and seeding it in `AuthorizationSeeder`, then assigning users to it via `PUT /api/v1/user/{userId}/groups` (which replaces a user's group list, not additive).

**Adding a new flag for a new feature:**

1. Add a key constant to `WellKnownFeatureFlags` (`StackDuel.Domain/FeatureFlags`) and re-export it in `StackDuel.Api/Authorization/WellKnownFeatures.cs`.
2. Create the row via `POST /api/v1/feature-flag/admin`, or add it to `FeatureFlagSeeder` if it should exist automatically after every deploy.
3. Guard the endpoint with `[RequireFeature(WellKnownFeatures.YourFlag)]`.
4. Guard the frontend with `useFeatureFlag(FeatureFlags.YOUR_FLAG)` (`web/src/domains/feature-flags`).

No code changes are needed to flip an existing flag, change its rollout percentage, or add/remove a user or group override — only adding a brand-new flag touches code, and only in the two spots above.

### Language server (Java code intelligence)

Live diagnostics, hover, and completion in the Java editor are powered by a self-hosted [Eclipse JDT Language Server](https://github.com/eclipse-jdtls/eclipse.jdt.ls) (jdtls), bridged to the browser over a raw WebSocket at `/hubs/language-server`. **Currently disabled everywhere** — locally and in production (`LanguageServer:Enabled = false` by default). The feature degrades gracefully when disabled or misconfigured: the Java editor still works, it just has syntax highlighting only, no live diagnostics.

It needs **two separate JDKs**, easy to get backwards:

- **JDK 21+** to run jdtls itself — this build of jdtls refuses to start on JDK 17 (`UnsupportedClassVersionError`).
- **JDK 17** for jdtls to *analyze* the user's code with — should match whatever JDK Judge0 actually compiles with (`JavaCodeTemplateStrategy` pins `/usr/local/jdk17/bin/javac`), so live diagnostics agree with what happens at submit time.

#### Local setup

1. Download jdtls:
   ```
   ./scripts/download-jdtls.ps1   # or download-jdtls.sh
   ```
   Extracts jdtls into a gitignored `.tools/jdtls/` and prints the config values you need below.

2. Install a JDK 21+ and a JDK 17 locally (any distribution, e.g. [Eclipse Temurin](https://adoptium.net/)).

3. Set user secrets on `StackDuel.Api`:
   ```
   dotnet user-secrets set --project StackDuel.Api LanguageServer:Enabled true
   dotnet user-secrets set --project StackDuel.Api LanguageServer:Java:JdtlsRuntimeJavaHome "<path to JDK 21+>"
   dotnet user-secrets set --project StackDuel.Api LanguageServer:Java:JavaHome "<path to JDK 17>"
   dotnet user-secrets set --project StackDuel.Api LanguageServer:Java:JdtlsLauncherJarPath "<path from step 1>"
   dotnet user-secrets set --project StackDuel.Api LanguageServer:Java:JdtlsConfigDirectory "<path from step 1>"
   ```

4. Restart `StackDuel.Api`. Open the Java editor for any problem — typing something with an undefined symbol should show a live diagnostic without hitting Run.

Other `LanguageServer` options: `MaxConcurrentSessions` (default 10 — each jdtls process is ~200-400MB, size to available memory), `IdleTimeoutMinutes` (default 10 — idle sessions get reaped and their jdtls process killed).

#### Azure App Service (Windows)

App Service deploys are a Kudu zip-deploy of a framework-dependent build (see `scripts/deploy-production.sh`), not a container — there's no package manager to install a JDK with, so the JDKs and jdtls have to be extracted onto the instance's disk directly.

`D:\home` is persistent **and shared across every scaled-out instance** of the plan, so this only needs doing once, not per-instance or per-deploy. `D:\local` is fast local disk but per-instance and ephemeral — that's what `Path.GetTempPath()` already resolves to for per-session jdtls workspaces, which is correct as-is and needs no configuration.

1. Open the Kudu console: `https://<app-name>.scm.azurewebsites.net/DebugConsole` → PowerShell tab.
2. Run:
   ```powershell
   cd D:\home
   New-Item -ItemType Directory -Force -Path tools | Out-Null
   cd tools

   Invoke-WebRequest -Uri "https://api.adoptium.net/v3/binary/latest/21/ga/windows/x64/jdk/hotspot/normal/eclipse" -OutFile jdk21.zip
   Expand-Archive jdk21.zip -DestinationPath jdk21-raw
   Move-Item (Get-ChildItem jdk21-raw)[0].FullName jdk21
   Remove-Item jdk21.zip, jdk21-raw -Recurse -Force

   Invoke-WebRequest -Uri "https://api.adoptium.net/v3/binary/latest/17/ga/windows/x64/jdk/hotspot/normal/eclipse" -OutFile jdk17.zip
   Expand-Archive jdk17.zip -DestinationPath jdk17-raw
   Move-Item (Get-ChildItem jdk17-raw)[0].FullName jdk17
   Remove-Item jdk17.zip, jdk17-raw -Recurse -Force

   New-Item -ItemType Directory -Force -Path jdtls | Out-Null
   Invoke-WebRequest -Uri "https://download.eclipse.org/jdtls/snapshots/jdt-language-server-latest.tar.gz" -OutFile jdtls.tar.gz
   tar -xzf jdtls.tar.gz -C jdtls
   Remove-Item jdtls.tar.gz

   (Get-ChildItem "jdtls\plugins\org.eclipse.equinox.launcher_*.jar")[0].FullName
   ```
   The last line prints the launcher jar path (filename has a version suffix that changes per jdtls release).
3. Set these as **Application settings** (Portal → App Service → Configuration), not user secrets — Azure uses `__` instead of `:` for nested keys:

   | Name | Value |
   | --- | --- |
   | `LanguageServer__Enabled` | `true` |
   | `LanguageServer__Java__JdtlsRuntimeJavaHome` | `D:\home\tools\jdk21` |
   | `LanguageServer__Java__JavaHome` | `D:\home\tools\jdk17` |
   | `LanguageServer__Java__JdtlsConfigDirectory` | `D:\home\tools\jdtls\config_win` |
   | `LanguageServer__Java__JdtlsLauncherJarPath` | (path printed in step 2) |

4. **Enable WebSockets** on the App Service (Configuration → General settings → Web sockets) — off by default, and unlike the SignalR hubs this endpoint has no long-polling fallback if it's off.
5. Restart the App Service.

Known limitation: a restart, deploy, or scale event kills every active jdtls process along with the rest of the sandbox — sessions just die and reconnect on next use (degrades gracefully, no crash), there's no cross-restart session persistence.

### Deployment environments

The deployment workflows use GitHub Environments named `Scrum` and `Production`.
Each environment should define the same settings, with values appropriate for that target.

Environment variables:

- `APP_URL`: Public URL for the deployed API. Used for the GitHub deployment environment link.
- `AZURE_WEBAPP_NAME`: Name of the Azure App Service to deploy to.
- `CORS_ALLOWED_ORIGINS`: Comma-separated list of allowed frontend origins.
- `DEPLOY_SCRIPT_PATH`: Repo-relative path to the deployment script to execute, for example `scripts/deploy-scrum.sh`.
- `MESSAGEBUS_TRANSPORT`: `RabbitMQ` or `AzureServiceBus`.
- `QUARTZ_SUBMISSIONCLEANUPJOB_CRONEXPRESSION`: Cron expression for the submission cleanup job.

Environment secrets:

- `AUTH0_AUDIENCE`: Auth0 API audience.
- `AUTH0_DOMAIN`: Auth0 domain.
- `AZURE_SERVICEBUS_CONNECTION_STRING`: Required when `MESSAGEBUS_TRANSPORT` is `AzureServiceBus`.
- `AZURE_WEBAPP_PUBLISH_PROFILE`: Publish profile XML for the Azure App Service (same content used by the `azure/webapps-deploy` action), used to authenticate the Kudu zip deploy.
- `DB_CONNECTION_STRING`: Database connection string used for EF Core migrations and app runtime configuration.
- `RABBITMQ_HOST`: Required when `MESSAGEBUS_TRANSPORT` is `RabbitMQ`.
- `RABBITMQ_PASSWORD`: Required when `MESSAGEBUS_TRANSPORT` is `RabbitMQ`.
- `RABBITMQ_USERNAME`: Required when `MESSAGEBUS_TRANSPORT` is `RabbitMQ`.
- `RABBITMQ_VIRTUALHOST`: Required when `MESSAGEBUS_TRANSPORT` is `RabbitMQ`.
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Optional, but recommended for production telemetry.

The deploy workflows publish the API, generate an EF migrations bundle, run the migrations against `DB_CONNECTION_STRING`, and then execute `DEPLOY_SCRIPT_PATH`.
The deployment script receives the following environment variables:

- `APP_URL`
- `ASPNETCORE_ENVIRONMENT`
- `AUTH0_AUDIENCE`
- `AUTH0_DOMAIN`
- `AZURE_SERVICEBUS_CONNECTION_STRING`
- `AZURE_WEBAPP_NAME`
- `AZURE_WEBAPP_PUBLISH_PROFILE`
- `CORS_ALLOWED_ORIGINS`
- `DB_CONNECTION_STRING`
- `MESSAGEBUS_TRANSPORT`
- `MIGRATION_BUNDLE_PATH`
- `PUBLISH_DIR`
- `QUARTZ_SUBMISSIONCLEANUPJOB_CRONEXPRESSION`
- `RABBITMQ_HOST`
- `RABBITMQ_PASSWORD`
- `RABBITMQ_USERNAME`
- `RABBITMQ_VIRTUALHOST`
- `APPLICATIONINSIGHTS_CONNECTION_STRING`

### Project structure

| Project                    | Description                         |
| -------------------------- | ----------------------------------- |
| `StackDuel.Api`             | HTTP API                            |
| `StackDuel.Application`     | Application logic and handlers      |
| `StackDuel.Domain`          | Domain models                       |
| `StackDuel.Infrastructure`  | EF Core, repositories, persistence  |
| `StackDuel.Application.Tests` | Unit tests — handlers, mocked repositories |
| `StackDuel.Domain.Tests`    | Unit tests — domain models              |
| `StackDuel.IntegrationTests` | Integration tests — real API + Postgres |
| `StackDuel.Seeder`          | CLI for seeding static/demo data         |
