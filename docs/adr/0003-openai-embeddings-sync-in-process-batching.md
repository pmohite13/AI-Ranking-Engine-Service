# ADR 0003: OpenAI embeddings — synchronous in-process batching (no Batch API)

## Status

Accepted

## Context

The implementation plan requires **OpenAI Embeddings API** usage with **synchronous in-process batching** only: group multiple texts into efficient requests within the same process, without OpenAI’s asynchronous **Batch API** (24h-style JSONL jobs).

Forces:

- Predictable latency for interactive ranking flows.
- Simple operations (no job polling, no separate batch artifact lifecycle).
- Cost and rate-limit efficiency via multi-input embedding calls per HTTP request.

## Decision

1. Use the official **OpenAI .NET SDK** (`OpenAI` package) **`EmbeddingClient.GenerateEmbeddingsAsync`** with inputs batched by:
   - configurable **max inputs per request**;
   - configurable **estimated token budget per request** (heuristic chars/token ratio);
   - **per-input token ceiling** — inputs over the limit fail fast (callers should chunk long documents upstream).
2. **Do not** use the OpenAI **Batch API** for embeddings in this service version. Bulk re-embed or offline cost optimization via Batch API is a **future ADR** if product requirements change.
3. Outbound HTTP uses **`IHttpClientFactory`** with a named client and **Polly** policies (retry with backoff + jitter, circuit breaker, client timeout) aligned with `docs/IMPLEMENTATION-PLAN.md` Part E.3 / E.7.

## Consequences

**Positive**

- Straightforward debugging and tracing; no async job state machine.
- Fits “rank now” APIs and single-instance or few-instance deployments.

**Negative**

- Very large offline re-embed jobs are not optimized for minimum $/token via Batch API.
- In-process batching still respects API token limits; extremely large corpora need chunking or a dedicated bulk pipeline later.

## Alternatives considered

- **OpenAI Batch API** — lower cost for bulk, higher latency and operational complexity; deferred.
- **Per-text HTTP calls only** — simpler but worse throughput and cost; rejected.

## References

- [OpenAI Embeddings guide](https://platform.openai.com/docs/guides/embeddings)
- `docs/IMPLEMENTATION-PLAN.md` — Phase 3, Part E.3
- `docs/Known-Architectural-Decisions.md` — §4 Embeddings API usage
