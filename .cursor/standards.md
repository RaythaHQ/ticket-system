# AI & Developer Guidance: Where to Find Standards

This file is intentionally **lightweight**. The real rules and examples now
live in two places:

- **Architecture & Governance (authoritative rules)**  
  `.specify/memory/constitution.md`  
  → The *App Constitution* defines layer boundaries, CQRS/Mediator usage,
  security/validation expectations, and governance/versioning rules.

- **Patterns & Examples (cookbook)**  
  `docs/architecture-standards.md`  
  → Curated examples for Razor PageModels, commands/queries/handlers,
  domain entities & value objects, EF Core usage, testing patterns, and
  JavaScript do/don'ts.

## How AI agents should use these

When generating or modifying code:

1. **Check the Constitution first** to ensure proposals respect:
   - Clean Architecture dependency rules
   - CQRS + Mediator patterns
   - Razor Pages as thin orchestrators
   - Security, validation, logging, and testing requirements

2. **Copy existing patterns** from `docs/architecture-standards.md` or from
   similar code in `src/`, rather than inventing new ones.

3. If a trade‑off appears to conflict with the Constitution, call it out to
   the user and propose an amendment or a clearly documented exception
   instead of silently diverging.

Humans should treat this file as a pointer only; the detailed documentation is
in the locations above.