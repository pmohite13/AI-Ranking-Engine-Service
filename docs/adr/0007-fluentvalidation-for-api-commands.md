# ADR 0007: FluentValidation for ingest and rank commands

## Status

Accepted

## Context

API inputs must be validated **consistently** (entity id, upload constraints, Top-K / Top-N bounds) with **clear, testable rules**. The implementation plan specifies **FluentValidation** as a first-class concern (Part B.7, E.6).

Alternatives include **DataAnnotations** on DTOs only, or ad hoc checks in endpoints—both scale poorly for **composable** rules and **unit testing**.

## Decision

1. Define **application-layer** request records **`IngestRequest`** and **`RankRequest`** (or equivalent) validated by **`IngestRequestValidator`** and **`RankRequestValidator`** inheriting **`AbstractValidator<T>`**.
2. Register validators with **`AddValidatorsFromAssembly`** (FluentValidation dependency injection extensions) in **`ApplicationServiceCollectionExtensions`**.
3. **Minimal API** handlers resolve **`IValidator<T>`** and return **RFC 7807 Problem Details** **validation problems** (400) via `Results.ValidationProblem` when **`ValidateAsync`** fails.
4. **Additional** endpoint rules (e.g., multipart **file required**, extension mapping) **remain** in the endpoint when they depend on **ASP.NET Core** types (`IFormFile`), keeping domain validators **pure** and **host-agnostic**.

## Consequences

**Positive**

- Rules are **centralized**, **discoverable**, and **unit-testable** without HTTP.
- Aligns with [FluentValidation documentation](https://docs.fluentvalidation.net/) and community practice for ASP.NET Core.

**Negative**

- Two layers of validation (FluentValidation + endpoint checks) require discipline to avoid duplication; **document** which rules live where.

## Alternatives considered

- **DataAnnotations only** — rejected for richer rules and consistency with plan.
- **Manual validation in each endpoint** — rejected for duplication and testability.

## References

- [FluentValidation — ASP.NET Core](https://docs.fluentvalidation.net/en/latest/aspnet.html)
- `docs/IMPLEMENTATION-PLAN.md` — Part C.3 (FluentValidation vs DataAnnotations), E.6
- `src/AI.Ranking.Engine.Application/Validation/IngestRequestValidator.cs`
- `src/AI.Ranking.Engine.Application/Validation/RankRequestValidator.cs`
