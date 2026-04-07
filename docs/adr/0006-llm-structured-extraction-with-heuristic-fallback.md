# ADR 0006: LLM structured extraction with heuristic fallback and validation

## Status

Accepted

## Context

The product requires **structured features** (skills, experience, role cues) for **hybrid ranking** and **cold-start** behavior. Raw embeddings alone are insufficient for explainable deterministic signals. **OpenAI** chat/completions can return **JSON** structured payloads, but **network failures**, **invalid JSON**, **schema drift**, and **hallucinations** must not leave the pipeline without controlled behavior.

## Decision

1. Implement **`ILLMStructuredExtractor`** in **`OpenAiStructuredExtractor`**:
   - **Cache miss path:** compute **heuristic baseline** (`HeuristicStructuredFeatureExtractor`) **first** (always available).
   - Call **`IStructuredLlmClient`** for structured JSON payload.
   - **`MergeAndValidate`:** merge LLM fields with heuristics; **clamp** numeric ranges; **normalize** skill lists; **replace** empty or invalid LLM lists with heuristic values; **fix** invalid job min/max year ordering.
2. **Resume vs job** kinds differ in validation (e.g., resume does not carry min/max requirement years).
3. **Input length** truncation uses **`LlmExtractionOptions.MaxInputCharacters`** to bound tokens and latency.
4. **Caching** of merged **`StructuredFeatures`** uses **`LlmExtractionCacheKeyBuilder`** (see ADR 0005).
5. **Logging** at appropriate levels when LLM payload is null or fields are corrected—**no** silent success on obvious invalid state without fallback.

## Consequences

**Positive**

- Ranking **never** depends solely on LLM output; **deterministic** path always contributes.
- **Testability:** mock `IStructuredLlmClient` in unit tests for JSON success/failure paths.

**Negative**

- Two code paths (LLM + heuristic) must stay in sync when adding new fields.
- Heuristic quality caps **floor** quality when LLM fails.

## Alternatives considered

- **LLM-only extraction** — rejected; fails open/closed quality requirements.
- **Heuristics-only** — rejected; misses semantic nuance for skills/titles from unstructured text.
- **Fine-tuned models** — future ADR if cost/latency requires.

## References

- `docs/IMPLEMENTATION-PLAN.md` — Phase 5, Part A.3, D.2
- `docs/Known-Architectural-Decisions.md` — §8 LLM and parsing
- `src/AI.Ranking.Engine.Infrastructure/Extraction/OpenAiStructuredExtractor.cs`
- `src/AI.Ranking.Engine.Infrastructure/Extraction/HeuristicStructuredFeatureExtractor.cs`
