# ADR 0002: Hybrid ranking as the baseline (not embeddings-only)

## Status

Accepted

## Context

The service must rank candidates for job roles at scale (~50K candidates, ~1.5K jobs, **Top 10** per job). Legacy keyword matching fails on semantic relevance and synonymy. A naive approach is to rank **only** by embedding cosine similarity between job and resume text.

That approach is simple to implement but **opaque** (hard to explain “why” a candidate ranked), **brittle** when embeddings are noisy or short text produces weak vectors, and **misaligned** with product needs for deterministic signals (skills, experience, controlled keyword overlap). Industry practice in search and recommendation commonly uses **two-stage** flows: **recall** (fast, broad) then **re-rank** (richer, explainable).

## Decision

We adopt **hybrid ranking** as the **baseline** product behavior:

1. **Semantic signal** — embeddings and similarity (e.g. cosine on normalized vectors) as one component.
2. **Deterministic signals** — skills overlap, experience fit, keyword/phrase overlap (and related structured features), normalized and validated where applicable.
3. **Configurable weights** — `RankingWeights` (or equivalent) so the blend is explicit and tunable.
4. **Explainability** — scores decomposed into named components (e.g. `ScoreBreakdown`) for assessors and debugging.
5. **Two-stage retrieval** — **Top-K** vector recall over the candidate corpus (pure .NET in-memory in v1), then **hybrid re-rank** to the final Top 10, avoiding full pairwise hybrid scoring of every candidate on every request as N grows.

LLM-based **structured extraction** for resumes and jobs is a **required** pipeline stage (with regex/heuristic fallback and validation), not optional; it feeds structured features for hybrid scoring and cold-start behavior.

## Consequences

**Positive**

- Better alignment with **explainable**, **auditable** ranking than embeddings-only.
- **Cold-start** jobs without historical data still benefit from **job text + structured extraction + hybrid** features.
- **Modular** evolution: swap embedding model, adjust weights, or add a stronger recall index later without rewriting the whole ranker.

**Negative**

- More implementation and test surface than a single cosine score.
- Weight tuning requires discipline (document defaults, avoid silent drift).

**Risks (managed, not ignored)**

- Bad or hallucinated LLM extractions — mitigated by **validation**, **fallback**, and **not** relying on LLM output alone for ranking.
- Embedding model changes — **version embedding model id in cache keys** and document in ADRs / DESIGN.md.

## Alternatives considered

- **Embeddings-only ranking** — rejected as primary approach; may remain a diagnostic or ablation baseline.
- **Full pairwise hybrid on every candidate for every job** — rejected at scale; two-stage recall + re-rank is the default pattern.
- **Learned end-to-end ranker (e.g. LambdaMART) with click data** — out of scope for cold-start thin-slice; **future ADR** if product has historical labels.

## References

- `docs/IMPLEMENTATION-PLAN.md` — Parts A.2, D.2, D.5
- `docs/Known-Architectural-Decisions.md` — vector search and two-stage pattern
- Industry pattern: **recall + re-rank** in search and recommendation systems
