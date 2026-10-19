# StackDuel Server technology stack

Inventory from repository manifests, source registration, Compose and workflow files, inspected October 16, 2026. Versions below are declared/pinned values, not verified deployed versions. See [STUDY_GUIDE.md](STUDY_GUIDE.md) for patterns, exercises and a learning path.

## Platform and application libraries

| Technology | Declared version | Role and evidence |
| --- | --- | --- |
| C# | No solution-wide language version pinned; some tests use `latest` | Backend language; nullable types and implicit usings enabled in projects |
| .NET / ASP.NET Core | `net10.0`; CI SDK `10.0.x` | API host, DI, middleware, controllers, WebSockets and SignalR; project files and `ApiServiceRegistration.cs` |
| MediatR | 14.1.0 | Commands, queries, notifications, logging/audit behaviors |
| FluentValidation | 12.1.1 | Input validation; DI extensions also 12.1.1 |
| Ardalis.Result / AspNetCore integration | 10.1.0 | Expected use-case outcomes and HTTP mapping |
| Asp.Versioning.Mvc | 10.0.0 | API versioning; default API version 1 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.9 | JWT authentication against Auth0 configuration |
| Auth0 | Hosted service; not pinned here | Identity/token provider; no Auth0 NuGet SDK needed for JWT validation |
| Microsoft.AspNetCore.OpenApi | 10.0.8 | OpenAPI document generation |
| Microsoft.OpenApi | 2.7.5 | OpenAPI models/support |
| Scalar.AspNetCore | 2.16.3 | API reference UI; mapped in Development |
| Microsoft.ApplicationInsights.AspNetCore | 3.1.2 | Configurable Azure Application Insights telemetry |
| Microsoft.Extensions.Caching.Memory | 10.0.9 | In-process memory cache; not a distributed cache |
| Bogus | 35.6.5 | Generated data, including test-case generation and username-related utilities |
| Microsoft.Extensions.Http | 10.0.9 | HTTP client factory for external adapters such as Judge0 |
| Microsoft.AspNetCore.WebUtilities | 10.0.10 | HTTP/web utility functions in Infrastructure |

SignalR, `System.Text.Json`, logging, DI, hosted services, rate limiting and configuration facilities are also used through the .NET/ASP.NET platform. They are not all separate direct package dependencies.

## Database and persistence

| Technology | Version | Role |
| --- | --- | --- |
| PostgreSQL | Compose image `postgres:17` | Primary relational database |
| Microsoft.EntityFrameworkCore | 10.0.9 | ORM, tracking and database operations |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 | Design-time EF tooling |
| Microsoft.EntityFrameworkCore.Relational | 10.0.9 | Relational support explicitly referenced by test/seeder projects |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.2 | PostgreSQL EF Core provider |
| Npgsql | 10.0.3 | Explicit PostgreSQL driver dependency in integration tests |
| EF migrations / migrations bundle | Tool version not pinned by the workflow install | Schema evolution in `StackDuel.Infrastructure/Migrations`; deployment migration artifact |

Persistence uses a rich domain context and a narrower row-based read context, configuration classes, repositories, global query filters, uniqueness constraints and database-backed worker leases. These are implementation patterns rather than additional services.

## Messaging, scheduling and storage

| Technology | Version | Role |
| --- | --- | --- |
| RabbitMQ.Client | 6.8.1 | RabbitMQ producer/consumer integration |
| RabbitMQ local image | `heidiks/rabbitmq-delayed-message-exchange:4.0.7-management` | Local broker including delayed-message exchange support and management UI |
| Azure.Messaging.ServiceBus | 7.18.4 | Alternative broker transport selected by configuration |
| Quartz / Quartz.Extensions.Hosting | 3.18.2 | Scheduled processing, sweeps and cleanup jobs in the host |
| Azure.Storage.Blobs | 12.29.2 | Avatar blob storage through `AzureAvatarBlobStorage` |
| Azurite local image | `mcr.microsoft.com/azure-storage/azurite:3.35.0` | Local Azure Blob Storage emulator |

RabbitMQ and Azure Service Bus are configurable transport alternatives. They are not both required for a local run. Quartz job registration uses cron schedules; `StoreDurably` in Quartz registration alone does not demonstrate a persistent clustered Quartz store.

## Code execution and language tooling

| Technology | Version/status | Role |
| --- | --- | --- |
| Judge0 CE via RapidAPI | External API version not pinned here | Executes submitted code through the Judge0 adapter |
| JavaScript, TypeScript, Python, SQLite, Java, C++ | Strategies registered; exact executor versions depend on language data/external runtime | Submission code templates, not backend implementation languages |
| React and vanilla JavaScript harnesses | Embedded assets | Execute/evaluate frontend-oriented submissions |
| React | 19.0.0 | Bundled React execution asset |
| react-reconciler | 0.31.0 | Bundled custom React runtime support |
| scheduler | 0.25.0 | Bundled React runtime dependency |
| esbuild | Executable included for API publish; version not established here | JSX transformation through `EsbuildJsxTranspiler` |
| Vitest | Batch strategy present; version not established here | JavaScript batch test harness generation |
| Eclipse JDT Language Server | Optional; download script uses a snapshot source | Java diagnostics/completion through raw WebSocket bridge |
| JDK for jdtls | README requires 21+ | Runs the language-server process |
| JDK for Java analysis | README specifies 17 | Aligns Java analysis with the compiler selected in the Java execution template |

Source: `InfrastructureServiceRegistration.cs`, `ExecutionEngine/CodeTemplates`, bundled package manifests and README. A registered template does not establish that a particular production Judge0 runtime is enabled or installed. This repository contains backend execution assets; it does not establish the stack of the separate web client.

## Tests and quality tools

| Technology | Version | Role |
| --- | --- | --- |
| xUnit | 2.9.3 | Unit, integration, architecture and specification test runner |
| xunit.runner.visualstudio | 3.1.4 | Test discovery/execution integration |
| Microsoft.NET.Test.Sdk | 17.14.0 | .NET test tooling |
| Moq | 4.20.72 | Test doubles in unit-test projects |
| NSubstitute | 5.3.0 | Test doubles in specifications |
| Reqnroll.xUnit | 3.3.4 | Gherkin behavior specifications |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.9 | In-process application hosting for HTTP integration tests |
| Testcontainers.PostgreSql | 4.15.0 | Disposable PostgreSQL for integration tests |
| NetArchTest.Rules | 1.3.2 | Assembly dependency rules |
| coverlet.collector | 6.0.4 | Coverage collection; intentionally omitted from architecture tests |
| CSharpier | 1.3.0 | Local tool manifest for formatting |
| dotnet format / EditorConfig | SDK tool / repository rules | CI format verification and C# style configuration |
| SonarCloud | Hosted service | Quality analysis and coverage reporting workflow |
| CodeQL | GitHub workflow | Static security analysis |
| Dependency security audit | GitHub workflow | Package vulnerability auditing |

## Configuration, development and deployment

| Tool or service | Role / evidence |
| --- | --- |
| Git / GitHub | Source control and repository automation |
| GitHub Actions | Reusable restore/build/format/test workflow; Scrum/Production deployment workflows; release tagging |
| Azure App Service | README deployment target; framework-dependent publish through Kudu zip deployment |
| Docker / Docker Compose | Local PostgreSQL, broker and storage emulator; Docker also needed by Testcontainers |
| Bruno | API request collection under `bruno` |
| PowerShell and shell scripts | Secrets setup, language-server setup, deployment automation |
| .NET user secrets | Local configuration; API and Seeder share a secrets identifier |
| Environment variables | Deployment/runtime configuration |
| Microsoft.Extensions.Configuration packages | Binder, EnvironmentVariables, Json and UserSecrets declared at 10.0.9 |
| NuGet Central Package Management | `Directory.Packages.props` owns dependency versions |
| `StackDuel.slnx` | Solution definition |

## Local service ports

| Service | Compose host port | Purpose |
| --- | --- | --- |
| PostgreSQL | 5432 | Database access |
| RabbitMQ | 5672 | Broker protocol |
| RabbitMQ management | 15672 | Broker administration UI |
| Azurite Blob | 10000 | Local blob endpoint |

API URLs/ports depend on launch/runtime configuration. Language-server setup is optional and documented as disabled by default. Local development needs the .NET 10 SDK and configured service settings; full integration tests need Docker. See [README.md](README.md) for setup commands.

## Source-of-truth checklist

- Package versions: [Directory.Packages.props](Directory.Packages.props). A central declaration becomes a direct dependency only when referenced by a project.
- Project dependencies and target frameworks: individual `*.csproj` files.
- Runtime registrations: [ApplicationServiceRegistration.cs](StackDuel.Application/ApplicationServiceRegistration.cs), [InfrastructureServiceRegistration.cs](StackDuel.Infrastructure/InfrastructureServiceRegistration.cs), [ApiServiceRegistration.cs](StackDuel.Api/ApiServiceRegistration.cs).
- Local service versions/ports: [docker-compose.yml](docker-compose.yml).
- Tool pinning: [.config/dotnet-tools.json](.config/dotnet-tools.json).
- Delivery behavior: [.github/workflows/build.yml](.github/workflows/build.yml), deployment workflows and `scripts`.
- Embedded JavaScript package versions: package manifests under `StackDuel.Infrastructure/ExecutionEngine/CodeTemplates/Assets/React/node_modules`.
