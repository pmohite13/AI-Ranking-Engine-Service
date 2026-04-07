# ADR 0001: Record architecture decisions

## Status

Accepted

## Context

The AI-Ranking-Engine-Service is a multi-module .NET solution with long-lived design choices (ranking strategy, vector recall, caching, OpenAI usage). Without a written record, decisions are rediscovered in code review or lost when people rotate. We need a lightweight, version-controlled way to capture **why** we chose an approach, **what** we trade off, and **when** to revisit.

## Decision

We adopt **Architecture Decision Records (ADRs)** as a standard artifact:

- **Location:** `docs/adr/`
- **Naming:** `NNNN-short-title.md` — sequential number, kebab-case title (e.g. `0002-hybrid-ranking-baseline.md`).
- **One ADR per decision** (or one ADR that explicitly supersedes a previous one).

Each ADR should be self-contained and include:

1. **Title** — short, descriptive
2. **Status** — Proposed | Accepted | Superseded (with pointer to replacement if superseded)
3. **Context** — problem, constraints, forces
4. **Decision** — what we will do
5. **Consequences** — positive and negative outcomes
6. **Alternatives considered** — briefly, with why not chosen
7. **References** — official docs, internal spikes, benchmarks

The **meta-ADR** (this file) establishes that ADRs exist and how to write them. New major choices (hybrid vs embedding-only, pure .NET recall vs FAISS, etc.) get their own numbered ADR.

## Consequences

**Positive**

- Onboarding and reviews can cite a single document instead of archaeology in chat logs.
- Superseded decisions remain traceable when we change direction.
- Aligns with common engineering practice for long-lived systems.

**Negative**

- Small maintenance cost: ADRs should be updated when the decision is reversed or materially changed.
- Risk of stale ADRs if the team forgets to update status — mitigated by linking ADRs from `docs/Known-Architectural-Decisions.md` and reviewing major PRs.

## Alternatives considered

- **Wiki-only decisions** — easier to drift from code; not versioned with the repo by default.
- **Comments in code only** — poor for cross-cutting decisions and high-level trade-offs.
- **Single DESIGN.md only** — good for overview; ADRs complement it with decision-by-decision history.

## References

- [Documenting architecture decisions — Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- `docs/IMPLEMENTATION-PLAN.md` — Part C (ADR provision and process)
