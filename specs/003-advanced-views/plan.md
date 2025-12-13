# Implementation Plan: Advanced Views

**Branch**: `003-advanced-views` | **Date**: 2025-12-13 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/003-advanced-views/spec.md`

## Summary

Enhance the ticket view system with advanced filtering (AND/OR condition builders with type-appropriate operators), multi-level sorting, and drag-drop column selection. This applies to both staff user-created views and admin system views.

**Technical Approach**: Extend the existing `TicketView` entity and `ViewFilterBuilder` service with new JSON structures for conditions, multi-level sorting, and column ordering. Build new Razor partial views for the filter builder, sort configurator, and column selector components. Use SortableJS (already available) for drag-drop interactions with minimal JavaScript enhancement.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0  
**Primary Dependencies**: ASP.NET Core Razor Pages, Entity Framework Core, MediatR, FluentValidation  
**Storage**: PostgreSQL via EF Core (existing `TicketView` entity with JSON columns)  
**Testing**: xUnit, FluentAssertions  
**Target Platform**: Linux server (Docker), Windows development  
**Project Type**: Web application (monolithic Clean Architecture)  
**Performance Goals**: Views with 20 conditions query 100k+ tickets in <2s  
**Constraints**: Minimal JavaScript (Razor Pages First principle), existing UI patterns  
**Scale/Scope**: Multi-tenant SaaS, typical 10k-100k tickets per organization

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance | Notes |
|-----------|------------|-------|
| **Clean Architecture** | ✅ PASS | Domain entities in `App.Domain`, commands/queries in `App.Application`, Razor Pages in `App.Web` |
| **CQRS & Mediator** | ✅ PASS | Extend existing `CreateTicketView`/`UpdateTicketView` commands; enhance `ViewFilterBuilder` service |
| **Razor Pages First** | ✅ PASS | Server-rendered filter builder with SortableJS for drag-drop only (already in codebase) |
| **Minimal JavaScript** | ✅ PASS | Use existing `SortableJS` library; minimal JS for dynamic form rows and drag interactions |
| **Explicit Data Access** | ✅ PASS | Filter execution via `ViewFilterBuilder.ApplyFilters()` using EF Core expressions |
| **Security & Validation** | ✅ PASS | FluentValidation for commands; permission checks for system views |
| **Route Constants** | ✅ PASS | Use existing `RouteNames` classes in Staff and Admin areas |
| **GUID vs ShortGuid** | ✅ PASS | Domain uses `Guid`, DTOs use `ShortGuid` per existing pattern |
| **Staff Area UI Pattern** | ✅ PASS | Use `.staff-card`, `.staff-table`, existing partials |
| **Admin Area Page Layout** | ✅ PASS | Use existing Admin page layout patterns |
| **BuiltIn Value Objects** | ✅ PASS | Use `TicketStatus`, `TicketPriority` value objects for filter options |

## Project Structure

### Documentation (this feature)

```text
specs/003-advanced-views/
├── plan.md              # This file
├── research.md          # Phase 0 output - resolved research
├── data-model.md        # Phase 1 output - enhanced data structures
├── quickstart.md        # Phase 1 output - developer guide
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── App.Domain/
│   └── Entities/
│       └── TicketView.cs                    # MODIFY: Add SortLevelsJson column
│
├── App.Application/
│   └── TicketViews/
│       ├── TicketViewDto.cs                 # MODIFY: Enhance with multi-level sort, extended conditions
│       ├── FilterAttributeDefinition.cs     # NEW: Define available filter attributes and operators
│       ├── Commands/
│       │   ├── CreateTicketView.cs          # MODIFY: Support new condition/sort structures
│       │   └── UpdateTicketView.cs          # MODIFY: Support new condition/sort structures
│       └── Services/
│           └── ViewFilterBuilder.cs         # MODIFY: Extend with new operators and AND/OR logic
│
├── App.Infrastructure/
│   └── Migrations/
│       └── YYYYMMDD_AddSortLevelsToTicketView.cs  # NEW: Add SortLevelsJson column
│
└── App.Web/
    └── Areas/
        ├── Staff/
        │   ├── Pages/
        │   │   └── Views/
        │   │       ├── Create.cshtml        # MODIFY: Use new filter/sort/column components
        │   │       ├── Create.cshtml.cs     # MODIFY: Handle new form structure
        │   │       ├── Edit.cshtml          # MODIFY: Use new filter/sort/column components
        │   │       └── Edit.cshtml.cs       # MODIFY: Handle new form structure
        │   └── Pages/
        │       └── Shared/
        │           └── _Partials/
        │               ├── _FilterBuilder.cshtml       # NEW: Advanced filter builder component
        │               ├── _SortConfigurator.cshtml    # NEW: Multi-level sort component
        │               └── _ColumnSelector.cshtml      # NEW: Drag-drop column selector
        │
        └── Admin/
            └── Pages/
                └── Tickets/
                    └── SystemViews/
                        ├── Create.cshtml    # MODIFY: Use shared filter/sort/column components
                        ├── Create.cshtml.cs # MODIFY: Handle new form structure
                        ├── Edit.cshtml      # MODIFY: Use shared filter/sort/column components
                        └── Edit.cshtml.cs   # MODIFY: Handle new form structure
        
        └── Shared/
            └── js/
                └── filter-builder.js        # NEW: Minimal JS for dynamic filter rows
                └── sort-configurator.js     # NEW: Minimal JS for sort level management

tests/
└── App.Application.UnitTests/
    └── TicketViews/
        └── ViewFilterBuilderTests.cs        # NEW: Test new operators and AND/OR logic
```

**Structure Decision**: Follows existing Clean Architecture with vertical slices. New UI components as Razor partials shared between Staff and Admin areas. Minimal new JavaScript following Razor Pages First principle.

## Complexity Tracking

No constitution violations requiring justification.
