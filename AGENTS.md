# Agentic Coding Guidelines - Itau Compra Programada

This document provides essential instructions for AI agents working on the Itau Compra Programada repository.

---

## 🗂 Project Overview

**Itaú Compra Programada ("Top Five")** is a Brazilian stock automated-purchase platform. Clients join the product, set a monthly contribution, and the system periodically buys a curated basket of 5 stocks on their behalf. An admin manages the recommendation basket; the purchase motor runs on days 5, 15, and 25 of each month. IR (income tax) events are published to Kafka for downstream reporting.

---

## 📁 Folder Structure

```
/
├── docker-compose.yml              # MySQL + Kafka + Zookeeper + Kafka-UI
├── requirements/                   # Product requirements (read before coding)
│   ├── desafio-tecnico-compra-programada.md
│   ├── regras-negocio-detalhadas.md
│   ├── layout-cotahist-b3.md
│   ├── glossario-compra-programada.md
│   ├── exemplos-contratos-api.md
│   └── diagrama-*.drawio
├── src/
│   ├── ItauCompraProgramada.Domain/            # Core domain (no external deps)
│   │   ├── Entities/
│   │   │   ├── Client.cs
│   │   │   ├── GraphicAccount.cs               # AddBalance / SubtractBalance
│   │   │   ├── Custody.cs                      # SubtractQuantity / UpdateAveragePrice
│   │   │   ├── PurchaseOrder.cs                # negative Quantity = sell
│   │   │   ├── RecommendationBasket.cs
│   │   │   ├── BasketItem.cs                   # Percentage (e.g. 25m = 25%)
│   │   │   ├── Distribution.cs
│   │   │   ├── IREvent.cs
│   │   │   ├── Rebalancing.cs
│   │   │   ├── StockQuote.cs
│   │   │   ├── StoredEvent.cs                  # Idempotency / event sourcing lite
│   │   │   └── ContributionUpdate.cs
│   │   ├── Enums/
│   │   │   ├── AccountType.cs
│   │   │   ├── IREventType.cs
│   │   │   ├── MarketType.cs
│   │   │   └── RebalancingType.cs
│   │   ├── Interfaces/                         # Repository contracts
│   │   │   ├── IClientRepository.cs
│   │   │   ├── ICustodyRepository.cs
│   │   │   ├── IDistributionRepository.cs
│   │   │   ├── IPurchaseOrderRepository.cs
│   │   │   ├── IRecommendationBasketRepository.cs
│   │   │   └── IStockQuoteRepository.cs
│   │   └── Repositories/
│   │       ├── IEventLogRepository.cs
│   │       └── IIREventRepository.cs
│   │
│   ├── ItauCompraProgramada.Application/       # Use cases (CQRS via MediatR)
│   │   ├── Behaviors/
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── ValidationBehavior.cs           # FluentValidation pipeline
│   │   │   └── ResiliencyBehavior.cs           # Idempotency via StoredEvent
│   │   ├── Common/Interfaces/
│   │   │   └── ICorrelatedRequest.cs
│   │   ├── Interfaces/
│   │   │   ├── ICotahistParser.cs
│   │   │   ├── IKafkaProducer.cs
│   │   │   └── IQuoteService.cs
│   │   ├── Admin/
│   │   │   ├── Commands/CreateBasket/          # CreateBasketCommand + Handler + Validator + Response
│   │   │   └── Queries/
│   │   │       ├── GetCurrentBasket/
│   │   │       └── GetBasketHistory/
│   │   ├── Clients/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateClient/               # CreateClientCommand + Handler + Validator
│   │   │   │   ├── DeactivateClient/
│   │   │   │   └── UpdateClientContribution/
│   │   │   └── Queries/
│   │   │       ├── GetClientWallet/
│   │   │       ├── GetClientPortfolio/
│   │   │       └── GetDetailedPerformance/
│   │   ├── Purchases/Commands/ExecutePurchaseMotor/
│   │   │   ├── ExecutePurchaseMotorCommand.cs
│   │   │   └── ExecutePurchaseMotorCommandHandler.cs   # Purchase motor logic
│   │   ├── Services/
│   │   │   └── QuoteService.cs
│   │   ├── Taxes/Services/
│   │   │   └── TaxService.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── ItauCompraProgramada.Infrastructure/    # EF Core, Kafka, parsers
│   │   ├── ExternalServices/
│   │   │   └── CotahistParser.cs               # B3 COTAHIST fixed-width parser
│   │   ├── Messaging/
│   │   │   └── KafkaProducer.cs
│   │   ├── Migrations/                         # EF Core migrations
│   │   ├── Persistence/
│   │   │   ├── ItauDbContext.cs
│   │   │   ├── Mappings/                       # One IEntityTypeConfiguration per entity
│   │   │   └── Repositories/                   # EF Core implementations
│   │   ├── Services/
│   │   │   └── PurchaseScheduler.cs            # IHostedService — runs on days 5, 15, 25
│   │   └── DependencyInjection.cs
│   │
│   └── ItauCompraProgramada.Api/               # Entry point
│       ├── Controllers/
│       │   ├── AdminController.cs
│       │   ├── ClientsController.cs
│       │   └── PurchasesController.cs
│       ├── Models/                             # Request/response models (JsonPropertyName in PT-BR)
│       ├── Program.cs
│       ├── appsettings.json
│       └── appsettings.Development.json
│
└── tests/
    ├── ItauCompraProgramada.UnitTests/
    │   ├── Application/
    │   │   ├── Admin/Commands/CreateBasket/
    │   │   ├── Admin/Queries/
    │   │   ├── Clients/Commands/{CreateClient,DeactivateClient,UpdateClientContribution}/
    │   │   ├── Clients/Queries/{GetClientWallet,GetDetailedPerformance}/
    │   │   ├── Purchases/Commands/ExecutePurchaseMotor/
    │   │   ├── Taxes/Services/
    │   │   └── QuoteServiceTests.cs
    │   └── Infrastructure/
    │       └── CotahistParserTests.cs
    └── ItauCompraProgramada.IntegrationTests/
        └── UnitTest1.cs                        # Placeholder (not yet implemented)
```

---

## 🧰 Tech Stack

| Concern | Library / Version |
|---|---|
| Runtime | .NET 8 / C# 12 |
| Web framework | ASP.NET Core 8 (`Microsoft.AspNetCore.OpenApi`) |
| ORM | EF Core 9 (`Pomelo.EntityFrameworkCore.MySql 9`) |
| CQRS / Mediator | MediatR 12 |
| Validation | FluentValidation 11 (`FluentValidation.AspNetCore`) |
| Messaging | Confluent.Kafka 2 |
| Logging | Serilog + `Serilog.Sinks.Console` + `Serilog.AspNetCore` |
| Serialization | `System.Text.Json` (built-in) |
| API docs | Swashbuckle / Swagger |
| Unit testing | xUnit + Moq + FluentAssertions |
| Integration tests | xUnit (placeholder, no tests yet) |

---

## ⚙️ Environment Variables & Configuration

Configuration is driven by `appsettings.json`. Override per-environment using `appsettings.{Environment}.json` or actual environment variables (ASP.NET Core standard).

### `appsettings.json` keys

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=itau_compra_programada;Uid=root;Pwd=root;"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  }
}
```

**Never commit real credentials.** Use placeholder values and override with environment variables in production:

```
ConnectionStrings__DefaultConnection=...
Kafka__BootstrapServers=...
```

### Docker Compose services

| Service | Port | Purpose |
|---|---|---|
| `mysql` | 3306 | Primary database |
| `kafka` | 9092 | Message broker |
| `zookeeper` | 2181 | Kafka coordination |
| `kafka-ui` | 8080 | Kafka web UI |

---

## ✅ Implementation Checklist

### User Stories

| ID | Story | Status |
|---|---|---|
| US01 | Client adhesion (`POST /api/clientes`) | Done |
| US01 | Client exit (`DELETE /api/clientes/{id}`) | Done |
| US01 | Update monthly contribution (`PUT /api/clientes/{id}/contribuicao`) | Done |
| US02 | Create recommendation basket (`POST /api/admin/cesta`) | Done |
| US02 | Get current basket (`GET /api/admin/cesta/atual`) | Done |
| US02 | Get basket history (`GET /api/admin/cesta/historico`) | Done |
| US03 | Purchase motor endpoint (`POST /api/motor/executar-compra`) | Done |
| US03 | Scheduled motor (days 5, 15, 25) via `PurchaseScheduler` | Done |
| US04 | Rebalancing: sell tickers removed from basket (RN-046/047) | Done |
| US04 | Rebalancing: stayed-ticker proportion drift RN-049 | **Pending** |
| US04 | Rebalancing: >5% drift trigger RN-050 | Postponed |
| US05 | B3 COTAHIST file parser | Done |
| US06 | IR dedo-duro Kafka events | Done |
| US06 | Profit tax Kafka events | Done |
| US07 | Client wallet (`GET /api/clientes/{id}/carteira`) | Done |
| US07 | Client performance (`GET /api/clientes/{id}/rentabilidade`) | Done |

### Technical Tasks

| Task | Status |
|---|---|
| Domain entities (all 13 entities) | Done |
| EF Core migrations (4 migrations) | Done |
| MediatR pipeline (Logging → Validation → Resiliency) | Done |
| Idempotency via `StoredEvent` + `ResiliencyBehavior` | Done |
| Kafka producer (`IKafkaProducer`) | Done |
| Serilog JSON logging | Done |
| Swagger / OpenAPI | Done |
| Global exception middleware (error envelope) | **Pending** |
| Unit tests — Application layer (26 tests, all green) | Done |
| Integration tests | Not started |

### Known Gaps (next work items, in priority order)

1. **RN-049** — Add `ProcessStayedTickerRebalancingAsync` to `ExecutePurchaseMotorCommandHandler` (compares current custody quantities against basket target %, sells excess / buys deficit). Include unit tests.
2. **Global exception middleware** — `src/ItauCompraProgramada.Api/Middleware/GlobalExceptionMiddleware.cs`: map `ValidationException` → 400, `KeyNotFoundException` → 404, `InvalidOperationException` → 400/409/500 using the error-code table below. Register in `Program.cs`.
3. **Enrich motor response** — `ExecutePurchaseMotorCommand` currently returns no data; the API contract expects `ordensCompra`, `distribuicoes`, `residuosCustMaster`, `eventosIRPublicados`.
4. **`GET /api/admin/conta-master/custodia`** — endpoint defined in the API contract but not yet implemented.

### Error envelope format & codes

Response body: `{ "erro": "<human message>", "codigo": "<MACHINE_CODE>" }`

| HTTP | Code | Trigger |
|---|---|---|
| 400 | `CLIENTE_CPF_DUPLICADO` | CPF already registered |
| 400 | `VALOR_MENSAL_INVALIDO` | Monthly value below minimum |
| 400 | `PERCENTUAIS_INVALIDOS` | Basket percentages do not sum to 100% |
| 400 | `QUANTIDADE_ATIVOS_INVALIDA` | Basket does not have exactly 5 assets |
| 400 | `CLIENTE_JA_INATIVO` | Client is already inactive |
| 404 | `CLIENTE_NAO_ENCONTRADO` | Client not found |
| 404 | `CESTA_NAO_ENCONTRADA` | No active basket found |
| 404 | `COTACAO_NAO_ENCONTRADA` | COTAHIST quote not found for date |
| 409 | `COMPRA_JA_EXECUTADA` | Purchase already executed for this date |
| 500 | `KAFKA_INDISPONIVEL` | Error publishing to Kafka |

---

## 🛠 Build, Lint, and Test Commands

All commands should be executed from the root of the repository.

### Build and Restore

- **Restore dependencies:** `dotnet restore`
- **Build solution:** `dotnet build`
- **Build specific project:** `dotnet build src/ItauCompraProgramada.Api`

### Running Tests

- **Run all tests:** `dotnet test`
- **Run a specific project's tests:** `dotnet test tests/ItauCompraProgramada.UnitTests`
- **Run a single test method:**
  `dotnet test --filter "FullyQualifiedName=ItauCompraProgramada.UnitTests.UnitTest1.Test1"`
- **Run tests with coverage:** `dotnet test /p:CollectCoverage=true`

### Linting and Formatting

- **Check formatting:** `dotnet format --verify-no-changes`
- **Fix formatting:** `dotnet format`
- **Run analyzers (linting):** `dotnet build /p:EnforceCodeStyleInBuild=true`

## 🧪 Test-Driven Development (TDD) Guide

AI agents MUST follow the TDD cycle for all new features and bug fixes:

1.  **RED**: Write a failing unit test in the `tests/` project that defines the expected behavior.
2.  **GREEN**: Implement the minimum amount of code in the `src/` project to make the test pass.
3.  **REFACTOR**: Clean up the code while ensuring tests remain green.
4.  **VERIFY**: Run `dotnet test` to confirm everything is working correctly.

Always prioritize unit tests for `Domain` and `Application` layers. Use `Moq` or `NSubstitute` for mocking dependencies.

---

## 🛰 Git Commit Conventions

- **Branching Strategy**: You are allowed to commit directly to the `main` branch.
- **Atomic Commits**: Each commit MUST have a single, clear purpose. NEVER mix different contexts (e.g., feature development with bug fixes, or refactoring with documentation updates) in the same commit. Guideline updates (like AGENTS.md) must be committed separately.
- **Commit after changes**: ALWAYS commit your code immediately after completing a logical unit of work or finishing a specific task.
- **Correlated Changes**: If the new changes are directly related to the previous commit and it hasn't been pushed yet, you may use `git commit --amend` to keep the history clean.
- **Commit Format**: Follow the **Conventional Commits** specification:
- **Format**: `<type>(<scope>): <description>`
- **Types**:
  - `feat`: A new feature
  - `fix`: A bug fix
  - `docs`: Documentation only changes
  - `style`: Changes that do not affect the meaning of the code (white-space, formatting, etc)
  - `refactor`: A code change that neither fixes a bug nor adds a feature
  - `perf`: A code change that improves performance
  - `test`: Adding missing tests or correcting existing tests
  - `chore`: Changes to the build process or auxiliary tools and libraries
- **Scope**: (optional) The project or module being changed (e.g., `api`, `infra`, `domain`).
- **Description**: Use imperative, present tense: "change" not "changed" nor "changes".

Example: `feat(domain): add Client entity with validation rules`

---

## 📋 Requirements Alignment

All development MUST align with the documents in the `requirements/` directory. **You MUST validate and verify the requirements during the planning phase, before starting any development task.**

- **`desafio-tecnico-compra-programada.md`**: Main challenge overview.
- **`regras-negocio-detalhadas.md`**: Critical business rules (RN-XXX). Refer to these by ID in commit messages or code comments if logic is complex.
- **`layout-cotahist-b3.md`**: Technical specification for the B3 file parser.
- **`glossario-compra-programada.md`**: Ubiquitous language reference.
- **`diagrama-*.drawio`**: Visual guides for ER, Business, and Sequence flows.

**Note on Draw.io**: These files are XML-based. AI agents can read and interpret their structure (entities, relationships, flows) to ensure architectural alignment.

---

## 🎨 Code Style Guidelines

### 1. Naming Conventions

- **Classes, Methods, Properties:** `PascalCase` (e.g., `PurchaseService`, `ExecutePurchase`)
- **Private Fields:** `_camelCase` with underscore prefix (e.g., `_httpClient`)
- **Local Variables & Parameters:** `camelCase` (e.g., `monthlyContribution`)
- **Interfaces:** Prefix with `I` (e.g., `IBasketRepository`)
- **Constants:** `PascalCase` or `UPPER_SNAKE_CASE` (PascalCase preferred for public constants)

### 2. File Structure & Imports

- **Namespaces:** Match the directory structure.
- **Imports:**
  - Place `using` statements at the top of the file, outside the namespace.
  - Group System namespaces first, then third-party libraries, then internal projects.
  - Alphabetize within groups.
- **File Scoped Namespaces:** Use `namespace MyNamespace;` instead of curly braces to reduce indentation.

### 3. Clean Architecture Implementation

- **CQRS & MediatR Pipeline:**
  - All operations MUST use MediatR `IRequest`.
  - Commands and Queries are handled by specific Handlers.
  - The pipeline follows: `LoggingBehavior` -> `ValidationBehavior` -> `ResiliencyBehavior`.
- **Resiliency & Idempotency:**
  - A `StoredEvent` table (Event Sourcing lite) tracks completed commands.
  - `ResiliencyBehavior` intercepts commands with a `CorrelationId`.
  - If a command was already processed, it returns the cached `ResponsePayload` (JSON) from `StoredEvent` instead of re-executing logic.
- **Domain Layer:**
  - The core of the application.
  - Contains Entities, Value Objects, Domain Exceptions, and Repository Interfaces.
  - Zero dependencies on other projects or external libraries (except for essential ones like `FluentValidation` if used for domain rules).
- **Application Layer:**
  - Contains Use Case implementations (Commands/Queries).
  - Defined Service Interfaces and DTOs.
  - Dependencies: Only on the Domain layer.
  - Logic for the Purchase Motor and Rebalancing logic resides here.
- **Infrastructure Layer:**
  - Implementation of external concerns.
  - EF Core DbContext, Migrations, and Repository implementations.
  - Kafka Producer/Consumer implementations.
  - B3 COTAHIST File Parser.
  - Dependencies: Application and Domain layers.
- **Api Layer:**
  - Entry point of the application.
  - Controllers, Middleware, and DI Registration.
  - Dependencies: Application and Infrastructure (only for DI).

### 4. Error Handling and Results

- **Structured Logging:**
  - Use Serilog for all logging.
  - Production logs MUST be in JSON format for observability.
  - Include relevant context (CorrelationId, ClientId, Ticker) in log messages.
- **Business Failures:** Use a `Result<T>` or `OneOf` pattern to return success or failure without throwing exceptions for expected business rule violations.
- **Exceptions:** Throw exceptions only for truly exceptional cases (e.g., database connection failure, unexpected nulls).
- **Validation:** Use `FluentValidation` for request validation in the Application layer.
- **Global Handler:** A middleware in the Api layer should catch all unhandled exceptions and return a standard RFC 7807 Problem Details response.

### 5. Types and Modern C# Features

- **Records:** Use `public record MyDto(...)` for immutable data transfer objects.
- **Primary Constructors:** Use primary constructors for dependency injection: `public class MyService(IDependency dep) { ... }`.
- **Nullable Reference Types:** Enabled by default. Always use `?` for optional properties and handle nulls explicitly.
- **Collection Expressions:** Use `[item1, item2]` instead of `new List<T> { item1, item2 }` where appropriate (C# 12).
- **Interpolated Strings:** Prefer `$"Value: {variable}"` over concatenation.

### 6. Dependency Injection & Configuration

- **Registration:** Group registrations by concern in static methods like `AddApplicationServices(this IServiceCollection services)`.
- **Options Pattern:** Use `IOptions<MySettings>` for strongly-typed configuration.
- **Lifecycle:**
  - `Scoped`: Repositories, DbContext, Services handling a single request.
  - `Singleton`: Kafka Producers, Caches, Background Services.
  - `Transient`: Lightweight, stateless utilities.

---

## 🏗 Infrastructure (Docker)

- **Start Infra:** `docker-compose up -d`
- **Stop Infra:** `docker-compose down`
- **MySQL Connection:** `Server=localhost;Database=itau_compra_programada;Uid=root;Pwd=root;`
- **Kafka Bootstrap:** `localhost:9092`

---

## 📝 Documentation

- Maintain `README.md` for high-level overview.
- Use XML comments (`///`) for public API methods and complex domain logic.
- Keep `AGENTS.md` updated with new tooling or patterns.

---

## 🤖 AI Instructions

- **Proactiveness:** If you add a new service, remember to register it in the DI container.
- **Migrations:** When changing Domain Entities, generate a new EF Core migration in the Infrastructure project.
- **Tests:** Every new feature in `Application` or `Domain` MUST have corresponding unit tests.
- **Logging:** You MUST add detailed logs to all application flows (Handlers, Services, Background Tasks) to ensure full observability.
- **Task Sequentiality & Approval:** Do not switch to a new task until the current one is either completed or explicitly pivoted/cancelled by the user. If a new request is made while another task is in progress, ask for explicit permission to switch or confirm if the current task should be finished first.
- **Clean Code:** Adhere to SOLID principles and DRY. Prefer composition over inheritance. Avoid "super methods" (methods that perform too many distinct actions). Prefer splitting logic into focused private methods, using the main method as an orchestrator.
- **Safety:** Never commit secrets or connection strings; use `appsettings.json` with placeholders or environment variables.
