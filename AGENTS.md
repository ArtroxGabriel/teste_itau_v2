# Agentic Coding Guidelines - Itau Compra Programada

This document provides essential instructions for AI agents working on the Itau Compra Programada repository.

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

- **Branching Strategy**: ALWAYS create a new branch from `main` before starting any feature implementation, bug fix, or refactoring.
- **Atomic Commits**: Avoid mixing different contexts in the same commit. For example, guideline updates (AGENTS.md) should be committed separately from feature code implementation.
- **Commit after changes**: ALWAYS commit your code after completing a logical unit of work (feature, refactor, fix, etc.).
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

All development MUST align with the documents in the `requirements/` directory:

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
- **Clean Code:** Adhere to SOLID principles and DRY. Prefer composition over inheritance.
- **Safety:** Never commit secrets or connection strings; use `appsettings.json` with placeholders or environment variables.
