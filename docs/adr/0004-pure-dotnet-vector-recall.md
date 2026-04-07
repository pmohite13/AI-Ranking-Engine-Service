# ADR 0004: Pure .NET in-memory vector recall (no FAISS in v1)

## Status

Accepted

## Context

The implementation plan requires **pure .NET in-memory** candidate vector storage and **Top-K** similarity search (cosine via L2-normalized embeddings and dot product) without native ANN libraries, to keep deployment simple and avoid FAISS-style binary dependencies.

Forces:

- **~50k** candidate scale in the product narrative; linear scan with sort-based Top-K is acceptable for moderate N on a single instance.
- **Consistency**: the same similarity definition used in **vector recall** must align with **hybrid re-ranking** (`HybridRankingMath` semantic input).
- **Thread safety** for concurrent ingestion and search in one process.

## Decision

1. Implement **`IVectorRecall`** as **`InMemoryVectorRecall`**: `ConcurrentDictionary<string, float[]>` of **L2-normalized** embeddings; **Upsert** replaces a candidate; **SearchTopKAsync** computes dot product against a normalized query and returns Top-K with deterministic ordering (**similarity descending**, **candidate id ascending** on ties).
2. Use **`EmbeddingVectorMath`** for dot product (scalar loop with optional **SIMD** via `System.Numerics.Vector` when hardware-accelerated) and L2 normalization.
3. **Do not** introduce **FAISS**, **FaissNet**, or an external vector DB in this version. If latency or memory at very large N demands sub-linear search, add a **new ADR** and swap the **`IVectorRecall`** implementation.

## Consequences

**Positive**

- No native interop or extra services; runs everywhere .NET runs.
- Clear contract for Phase 5 hybrid pipeline: recall → re-rank.

**Negative**

- **O(N)** scan per query; memory **O(N × dim)** for stored vectors.
- **Process restart** clears the index unless persistence is added later.

## Alternatives considered

- **FAISS / HNSW / managed ANN** — better asymptotic search at very large N; deferred due to ops and build complexity.
- **Min-heap Top-K only** — lower constant factor than full sort; deferred until profiling shows sort as a bottleneck; sort kept for deterministic tie-breaking simplicity.

## References

- `docs/IMPLEMENTATION-PLAN.md` — Phase 4, Part E.4
- `docs/Known-Architectural-Decisions.md` — §2 Vector search and semantic similarity
