# ADR 0005: Hash-keyed in-memory caching (`IMemoryCache`)

## Status

Accepted

## Context

The implementation plan requires **caching** keyed by **cryptographic hashes** of canonical inputs (normalized text, model identifiers) to reduce **OpenAI** cost and **latency** for repeated documents and identical extraction inputs.

Forces:

- **Single-instance** deployment first; **no Redis** or SQL cache in v1.
- **Stampede** risk on cold cache: mitigate with **idempotent ingestion** and consistent key formulas.
- **Model or dimension changes** must not silently reuse stale vectors or extracts.

## Decision

1. Expose caching via **`ICacheService`** implemented by **`MemoryCacheService`**, wrapping **`IMemoryCache`** from `Microsoft.Extensions.Caching.Memory`.
2. **Embedding cache keys** include **model id**, **dimensions**, and **hash of canonical text** (`EmbeddingCacheKeyBuilder`).
3. **LLM structured extraction cache keys** include **model id**, **document kind** (resume vs job), and **hash of input text** (`LlmExtractionCacheKeyBuilder`), with configurable **absolute expiration** per `LlmExtractionOptions`.
4. **Ingestion deduplication** uses a prefix key (`ingest:dedupe:v1:`) plus **idempotency key** derived from entity metadata and **SHA-256** of file bytes, storing **`IngestionProcessResult`** to short-circuit duplicate uploads.
5. **Distributed invalidation** is **out of scope**; multi-node deployments must treat cache as **best-effort per instance** until a future ADR adds **Redis** or similar.

## Consequences

**Positive**

- Simple operations profile; no extra infrastructure.
- Keys are **stable** and **testable** (unit tests on key builders).

**Negative**

- **Process-wide** memory growth for hot corpora; tune expiration and cache size options at host level.
- **No** cross-replica consistency; identical requests on different nodes may each pay OpenAI until warm.

## Alternatives considered

- **No caching** — unacceptable cost/latency for duplicate paths.
- **Redis** — better for scale-out; deferred until horizontal scaling is required.

## References

- `docs/IMPLEMENTATION-PLAN.md` — Part B.5, E.5, Phase 3
- `docs/Known-Architectural-Decisions.md` — §3 Caching
- `src/AI.Ranking.Engine.Infrastructure/Caching/MemoryCacheService.cs`
- `src/AI.Ranking.Engine.Infrastructure/Embeddings/EmbeddingCacheKeyBuilder.cs`
- `src/AI.Ranking.Engine.Infrastructure/Extraction/LlmExtractionCacheKeyBuilder.cs`
