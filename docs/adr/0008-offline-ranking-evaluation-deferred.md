# ADR 0008: Offline ranking evaluation metrics (NDCG, precision@k) — deferred

## Status

Accepted

## Context

The implementation plan lists **optional** offline evaluation (e.g., **NDCG**, **precision@k**) as a **nice-to-have** when **golden datasets** exist (Part H, Phase 8 item 3). The product **does not** currently require a **regression harness** for rank quality as a **gate** for the thin-slice deliverable.

Forces:

- Golden sets require **labeling investment** and **maintenance** when models or weights change.
- Premature metrics infrastructure can **distract** from core pipeline correctness and architecture.

## Decision

1. **Defer** building a **dedicated offline evaluation** project (datasets, metrics, baselines) until **product** or **research** commits to **labeled** job–candidate relevance data.
2. **Unit tests** remain the **primary** quality gate for **deterministic** ranking math, vector ordering, **validation**, and **fallback** behavior.
3. **When** introducing offline evaluation:
   - Add a **new ADR** or **supersede** this one with **dataset format**, **metric choices**, and **CI integration**.
   - Prefer **version-controlled** small fixtures over **live** OpenAI calls in CI.
4. **Shadow** ranking or **A/B** experiments in production are **out of scope** for this repository version.

## Consequences

**Positive**

- Scope stays focused on **working** pipeline and **documented** failure modes.
- Avoids **false confidence** from metrics without representative labels.

**Negative**

- **Regression** in rank quality from model or weight changes may be caught **later** than with automated offline tests.

## Alternatives considered

- **Implement NDCG harness now with synthetic data** — deferred; low signal without real labels.
- **Integration tests against live OpenAI** as quality gate — rejected for **flakiness** and **cost**; use **mocks** or **record/replay** if needed later.

## References

- `docs/IMPLEMENTATION-PLAN.md` — Part H.3, Phase 8 item 3, Part N (evaluation optional)
- `docs/DESIGN.md` — §7 Known failure modes
- Industry practice: **offline eval** standard for large search teams — **adopt when labels exist**
