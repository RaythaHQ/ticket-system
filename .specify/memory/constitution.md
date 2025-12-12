<!--
Sync Impact Report
- Version change: N/A -> 1.0.0
- Modified principles: (initial creation)
- Added sections:
  - Core Principles
  - Architecture & Coding Standards (Summary)
  - Development Workflow & Quality Gates
  - Governance
- Removed sections: (none)
- Templates alignment:
  - ✅ .specify/templates/plan-template.md (generic, already compatible)
  - ✅ .specify/templates/spec-template.md (generic, already compatible)
  - ✅ .specify/templates/tasks-template.md (generic, already compatible)
- Follow-up TODOs: None
-->

# App Constitution

## Core Principles

### Clean Architecture & Dependency Rule

**Principle**: The codebase MUST follow a layered Clean Architecture where
dependencies flow strictly inwards.

**Rules**:
- The solution is organized into four primary layers: `App.Domain`,
  `App.Application`, `App.Infrastructure`, and `App.Web`.
- `App.Domain` contains ONLY business entities, value objects, domain events,
  and domain-specific exceptions. It MUST NOT depend on any other project.
- `App.Application` orchestrates use cases via CQRS (commands/queries), DTOs,
  validators, and interfaces. It may depend on `App.Domain` but MUST NOT
  depend on `App.Infrastructure` or `App.Web`.
- `App.Infrastructure` implements interfaces from `App.Application` for
  database access, file storage, email, background tasks, etc. It MUST NOT
  contain business rules.
- `App.Web` is a thin presentation layer (Razor Pages, controllers, middleware)
  that depends on `App.Application` and composes `App.Infrastructure` via DI.
- The dependency flow MUST always be:
  `Web → Application → Domain`, plus `Infrastructure → Application` (via
  interface implementations), never the reverse.

**Rationale**: Enforcing the dependency rule keeps business logic isolated from
infrastructure and UI concerns, making the system easier to test, evolve, and
reuse across future projects.

### CQRS & Mediator-Driven Use Cases

**Principle**: All application behavior MUST be modeled as explicit commands
and queries handled via the Mediator pipeline.

**Rules**:
- Each use case lives in its own file (e.g. `CreateUser.cs`, `GetUsers.cs`) and
  contains three nested types where applicable: `Command`/`Query`, `Validator`,
  and `Handler`.
- Commands MUST return `CommandResponseDto<T>`; queries MUST return
  `IQueryResponseDto<T>` / `QueryResponseDto<T>`.
- Validators use FluentValidation and MAY depend on `IAppDbContext` for
  read-only checks (e.g. uniqueness). They MUST NOT modify entities or call
  `SaveChanges`.
- Handlers inject only abstractions (e.g. `IAppDbContext`, `IEmailer`,
  `IFileStorageProvider`) and own the transaction boundary via
  `_db.SaveChangesAsync` at the end of the operation.
- All asynchronous work in handlers and supporting services MUST be `async`
  end-to-end and accept a `CancellationToken`.
- Business logic MUST live in handlers and, where appropriate, in domain types
  – NEVER directly in Razor PageModels, controllers, or views.

**Rationale**: CQRS plus Mediator gives a clear, testable unit of behavior per
use case, enforces separation of concerns, and keeps orchestration logic
consistent across the entire application.

### Razor Pages First, Minimal JavaScript

**Principle**: The default UI is server-rendered Razor Pages with minimal,
targeted JavaScript for progressive enhancement only.

**Rules**:
- Every screen is a Razor Page (`.cshtml` + `.cshtml.cs`) with a PageModel that
  inherits from the appropriate base (`BasePageModel` or `BaseAdminPageModel`).
- PageModels act as thin orchestrators: they set up breadcrumbs and view
  models, call `Mediator.Send` to execute commands/queries, and select the
  result view. They MUST NOT contain direct EF Core calls or complex business
  logic.
- JavaScript usage is limited to small, focused enhancements (e.g. file
  uploads, editors, dialogs). The application MUST NOT use heavy SPA
  frameworks (React, Vue, Angular, etc.).
- Frontend assets (CSS/JS) are organized under `wwwroot` by responsibility:
  core utilities, shared components, and page-specific modules.
- Tag Helpers and partials are preferred for cross-cutting UI concerns
  (breadcrumbs, alerts, reusable form fragments).

**Rationale**: A Razor Pages–first approach keeps the stack simple and
maintainable, reduces client-side complexity, and keeps the majority of logic
on the server where it is easier to secure and test.

### Explicit Data Access & Performance Discipline

**Principle**: All data access goes through well-defined interfaces with
explicit performance practices.

**Rules**:
- Application code interacts with persistence only via `IAppDbContext` from the
  Application layer; `AppDbContext` remains an Infrastructure detail.
- EF Core handlers MUST use async methods (`FirstOrDefaultAsync`,
  `ToListAsync`, `CountAsync`, etc.) and include the `CancellationToken`.
- Read-only queries SHOULD use `AsNoTracking()` and projection (`Select`) to
  DTOs to avoid unnecessary tracking and allocations, especially in list views.
- Handlers MUST avoid N+1 queries by using `Include` / `ThenInclude` or
  dedicated queries.
- Migrations are created and managed via EF Core; generated migration files
  are not edited except for explicit, reviewed custom SQL.
- Configuration for connection strings, file storage, and other infrastructure
  MUST come from configuration (appsettings, environment variables, secrets),
  never hard-coded.

**Rationale**: Treating data access as a first-class concern ensures the
boilerplate stays fast, resource-efficient, and predictable as projects grow.

### Security, Testing & Observability as First-Class Concerns

**Principle**: Security, reliability, and insight into behavior are
non‑negotiable and MUST be designed into every feature from the start.

**Rules**:
- All inputs at the boundary (commands/queries, HTTP endpoints) MUST be
  validated using FluentValidation. Complex validation belongs in validators,
  not handlers.
- Authorization is enforced via `[Authorize]` attributes and explicit policy
  checks in the Application layer where relevant.
- Sensitive information (passwords, tokens, secrets, PII) MUST NEVER be logged
  or exposed in error messages.
- Logging uses structured logs with context (e.g. user id, entity id) at
  appropriate levels (Information, Warning, Error) and is implemented via
  `ILogger`.
- Domain and Application logic SHOULD be covered by unit tests
  (especially value objects, validators, and critical commands), and important
  workflows SHOULD have integration tests where feasible.
- Commands that change state and are marked as loggable MUST participate in the
  audit behavior to produce an auditable trail of actions.

**Rationale**: This boilerplate is meant for production systems. Baking in
validation, security, testing, and logging from day one reduces incidents,
regressions, and debugging time.

## Architecture & Coding Standards (Summary)

This constitution sits above detailed coding standards and examples embedded in
this repository (e.g. in documentation files and the existing code patterns).
Wherever there is ambiguity, the rules below clarify expectations.

- **Separation of Concerns**: Domain types model business concepts; Application
  orchestrates use cases; Infrastructure integrates external systems; Web
  handles HTTP, authentication, and rendering.
- **Vertical Slices**: Organize features by vertical slices (e.g. `Users`,
  `Roles`, `EmailTemplates`) with subfolders for `Commands`, `Queries`,
  `EventHandlers`, and DTOs. Do NOT group by technical type alone.
- **Naming**: Namespaces follow the
  `App.{Layer}.{Feature}[.{Subfolder}]` pattern. Files and classes are named
  after their responsibility (e.g. `CreateUser`, `GetUsers`, `UserDto`).
- **Async Everywhere**: All I/O, including database and network calls, MUST be
  asynchronous. Avoid `.Result`, `.Wait()`, and blocking calls.
- **DTOs vs ViewModels**: DTOs from `App.Application` are immutable records for
  data transfer. Razor views use UI-specific view models mapped from DTOs in
  PageModels.
- **Configuration & Secrets**: All secrets live in environment-specific
  configuration (user secrets, env vars, secret stores). They MUST NOT be
  committed to source control.
- **Testing Priorities**: Value objects, validators, and critical business
  commands are Priority 1 for testing; query handlers, DTO mappings, and
  utilities are Priority 2; Razor Pages and full flows are nice-to-have but
  encouraged as the project matures.
- **Route Constants**: All Razor Page route references MUST use the centralized
  `RouteNames` class constants instead of hardcoded strings. Each area (Admin,
  Public, Staff) MUST have a `RouteNames.cs` file under
  `Areas/{Area}/Pages/Shared/` containing nested static classes that group route
  constants by feature. This eliminates magic strings, provides compile-time
  safety, and ensures route consistency. Hardcoded route strings in
  `RedirectToPage()`, `asp-page`, or similar attributes are prohibited except
  for error pages or special cases explicitly documented.

When in doubt, follow existing well-structured examples in the codebase using
these standards.

## Development Workflow & Quality Gates

This boilerplate is intended for feature‑oriented development driven by
SpecKit plans and specs.

- **Feature Documentation**: Each substantial feature SHOULD have a
  `plan.md`, `spec.md`, and `tasks.md` under `specs/[###-feature-name]/` driven
  by `/speckit.plan`, `/speckit.spec`, and `/speckit.tasks`.
- **User Stories First**: User journeys are captured as prioritized user
  stories that are independently testable. Implementation tasks are grouped by
  story so that each slice can be developed and verified in isolation.
- **MVP Mindset**: Start with the highest-priority user story (MVP), ship it,
  and then layer on additional stories incrementally.
- **Gates & Constitution Check**: Implementation plans MUST check proposed
  designs against this constitution (e.g. layer boundaries, CQRS usage,
  security and validation expectations). Any intentional deviations MUST be
  documented with justification.
- **Incremental Refactoring**: When touching existing code, bring it closer to
  these standards rather than attempting large rewrites. Prefer safe,
  incremental improvements.
- **Testing & Review**: Before merging, features SHOULD have tests as
  appropriate for their risk level and MUST be reviewed for compliance with
  this constitution.

## Governance

**Scope**: This constitution is the single source of truth for architecture,
layering, and non‑negotiable engineering standards in the App boilerplate.

**Amendments**:
- Any change to these principles or governance rules MUST be made via a pull
  request that clearly describes the motivation and impact.
- Changes that materially alter or add principles SHOULD bump the MINOR version
  (e.g. `1.0.0` → `1.1.0`). Backwards‑incompatible governance changes SHOULD
  bump the MAJOR version. Typos or purely editorial clarifications MAY bump the
  PATCH version.
- The `Version`, `Ratified`, and `Last Amended` fields at the bottom of this
  document MUST be kept in sync with the latest approved change.

**Compliance**:
- Reviewers are responsible for enforcing this constitution during code review
  and when approving SpecKit plans/specs.
- Where necessary, reviewers MAY require follow‑up tasks or refactors to bring
  code into alignment with these standards.
- Intentional, well‑justified exceptions are allowed but MUST be documented in
  the relevant plan/spec and, if systemic, considered for inclusion as future
  amendments here.

**Runtime Guidance**:
- Day‑to‑day implementation guidance lives in this document, in the
  repository's README, and in feature‑specific specs created via SpecKit.
- When conflicts arise between older docs and this constitution, this
  constitution prevails.

**Version**: 1.1.0 | **Ratified**: 2025-12-09 | **Last Amended**: 2025-12-12
