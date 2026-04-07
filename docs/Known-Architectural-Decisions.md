# Known Architectural Decisions & Deferred Enhancements

This document records **intentional** architectural choices for the AI-Ranking-Engine-Service: what we chose, **why** (simplicity, time-box, thin-slice scope), and **what we are aware of** as stronger or more scalable alternatives for a future production system.

It is **not** a backlog of defects; it is a **reference** so future contributors do not mistake pragmatism for ignorance.

---

## How to use this document

- **Greenfield / scale-up**: Use the “Stronger alternatives” column to plan migrations.
- **ADRs**: When adopting an alternative, add or update an Architecture Decision Record in `docs/adr/` and link it here.
- **Interviews / reviews**: This table answers “what would you do at scale?” without rewriting the implementation plan.

---

## 1. Messaging, ingestion, and backpressure

| Current decision | Rationale | Stronger alternatives (aware of) | When to revisit |
|------------------|-----------|-------------------------------------|-----------------|
| **`System.Threading.Channels`** (in-process queues) for producer–consumer ingestion | Simple, no extra infrastructure, good for single-process demos and tests | **Azure Queue Storage**, **Azure Service Bus**, **AWS SQS**, **RabbitMQ**, **Kafka** for durable, multi-consumer, cross-region work | Multiple API instances must share one queue; need **durability** across restarts; strict **at-least-once** processing with external store |
| Bounded channels for backpressure only | Keeps memory predictable in one process | **Rate limiting** at the edge + queue depth alerts + **dead-letter** queues | Sustained overload or need for **replay** of failed jobs |

---

## 2. Vector search and semantic similarity

| Current decision | Rationale | Stronger alternatives (aware of) | When to revisit |
|------------------|-----------|-------------------------------------|-----------------|
| **Pure .NET in-memory** similarity (e.g., normalized embeddings + cosine / dot-product **Top-K** over the candidate set) | **No native dependencies** (FAISS), easy to build and run; sufficient for thin-slice and moderate N | **FAISS** (e.g., via FaissNet or native bindings), **HNSW** in other libs, **Pinecone**, **Weaviate**, **Qdrant**, **Azure AI Search** vector index, **pgvector** | **Latency** or **memory** at large N (e.g., 50–200K+ vectors), **multi-tenant** isolation, or **horizontal** scaling of search |
| Two-stage **recall** (Top-K) + **hybrid re-rank** in memory | Industry-recognized pattern; avoids scoring every candidate with full hybrid model on every request | Same pattern, but **recall** backed by an **ANN** or managed vector DB | Same as above; ANN becomes necessary when linear scan is too slow |

---

## 3. Caching

| Current decision | Rationale | Stronger alternatives (aware of) | When to revisit |
|------------------|-----------|-------------------------------------|-----------------|
| **`IMemoryCache`** (or equivalent) for **hash-keyed** embeddings, parses, LLM outputs | Zero extra services; fast for single instance | **Redis**, **SQL Server cache**, **NCache** for **shared** cache across nodes | **Horizontally scaled** APIs; **cache coherence** requirements; **survival** across deploys |
| No distributed invalidation | Simplicity | Topic-based **pub/sub** or versioned **cache keys** with model id | Model or embedding dimension changes must roll out **without** stale hits across instances |

---

## 4. Embeddings API usage

| Current decision | Rationale | Stronger alternatives (aware of) | When to revisit |
|------------------|-----------|-------------------------------------|-----------------|
| **Synchronous in-process batching** (batch multiple texts into efficient embedding calls per request limits) | Predictable, easy to debug, fits “rank now” flows | **OpenAI Batch API** (async JSONL jobs) for **bulk** re-embedding at lower cost | **Nightly** full- corpus re-embed; **cost** optimization over latency |
| Rely on **OpenAI** model selection / defaults as documented | Fewer moving parts; **MLOps** out of scope for v1 | **Explicit model pinning** in config; **version** in cache keys + telemetry | **Reproducibility** audits, **A/B** tests, or embedding **geometry** changes between model versions |

---

## 5. MLOps / AI operations

| Current decision | Rationale | Stronger alternatives (aware of) | When to revisit |
|------------------|-----------|-------------------------------------|-----------------|
| **No** formal embedding **drift** monitoring | Thin-slice; no offline golden set pipeline yet | **Monitoring** of query distribution, **embedding norm** stats, **slice** quality on held-out jobs | **Compliance** or product asks for **quality SLAs** |
| **No** dedicated **cost** dashboards (tokens, $/rank) beyond ad-hoc logging | Simplicity | **OpenAI usage** export, **FinOps** dashboards, **per-tenant** budgets | **Multi-tenant** billing or **cost** SLOs |
| **No** automated **evaluation** (NDCG, precision@k) | Optional / nice-to-have per implementation plan | **Offline** eval harness + **golden** sets; **shadow** ranking in production | **Regression** detection when changing weights or models |

---

## 6. Security, API hardening, and compliance

| Current decision | Rationale | Stronger alternatives (aware of) | When to revisit |
|------------------|-----------|-------------------------------------|-----------------|
| **Skip** rate limiting, API auth, RBAC for this project | Focus on core ranking logic; **internal** or demo use | **API keys**, **OAuth2/OIDC**, **mTLS**, **per-tenant** keys; **AspNetCore rate limiting** (fixed window, sliding, token bucket) | **Public** or **multi-tenant** exposure |
| **Skip** request-size limits beyond **configurable upload cap** (e.g., 200 KB default) at architectural doc level | App-level validation is the minimum; **edge** limits deferred | **Reverse proxy** limits (nginx, API Management, **Azure Front Door**) | **DoS** risk or **untrusted** clients |
| **Secrets** via **environment variables** and **`.env`** locally (gitignored) | No cloud vault dependency | **Azure Key Vault**, **AWS Secrets Manager**, **HashiCorp Vault** | **Enterprise** policy, **rotation**, **audit** |
| **PII**, **virus scanning**, **encryption at rest** not in scope | Assessment / thin-slice scope | **PII** classification, **DLP**, **ClamAV** / cloud malware scanning, **TDE** / customer-managed keys | **Production** HR data handling, **GDPR** / regional rules |

---

## 7. Data lifecycle and persistence

| Current decision | Rationale | Stronger alternatives (aware of) | When to revisit |
|------------------|-----------|-------------------------------------|-----------------|
| **In-memory** candidate/job store and vectors (process lifetime) | **No DB** to provision, deploy, or migrate | **PostgreSQL** + **pgvector**, **Cosmos DB**, **Blob** for originals + DB for metadata | **Durability**, **audit**, **replay**, **multi-region** |
| **No** retention / deletion / **GDPR** erase workflows | Out of scope | **Scheduled** deletion, **legal hold**, **right-to-erasure** APIs | **Regulatory** requirements |
| **Process restart** loses in-memory vectors | Acceptable for demo; **re-ingest** or reload path documented | **Snapshot** index to disk or **warm** from object store | **Zero** cold-start on deploy for large corpora |

---

## 8. LLM and parsing

| Current decision | Rationale | Stronger alternatives (aware of) | When to revisit |
|------------------|-----------|-------------------------------------|-----------------|
| **OpenAI** for **structured** resume/job extraction + **heuristic fallback** | Balance of quality and control | **Fine-tuned** or **smaller** open models for **cost**; **on-prem** for **air-gapped** | **Cost** at volume, **latency** SLOs, **data residency** |
| **PdfPig** + **Open XML** parsers only | **.NET-native**, license-friendly; no OCR | **OCR** (Azure Document Intelligence, **Tesseract**) for scanned PDFs | **Scanned** resumes dominate |

---

## 9. Observability and operations

| Current decision | Rationale | Stronger alternatives (aware of) | When to revisit |
|------------------|-----------|-------------------------------------|-----------------|
| **Structured logging** as baseline; **metrics/tracing** optional | Keep implementation light | **OpenTelemetry** → **Prometheus**, **Grafana**, **Azure Monitor**, **App Insights** | **SLOs**, **on-call**, **distributed** tracing across services |

---

## 10. Summary: intentional themes

| Theme | What we optimized for | What we trade away (knowingly) |
|--------|------------------------|--------------------------------|
| **Infrastructure** | Single deployable, minimal third-party **ops** | **Durability**, **horizontal** scale, **shared** queues and caches |
| **Vector search** | **Managed code**, no native ANN | **Sub-linear** search at very large N |
| **AI** | **OpenAI** APIs + **simple** batching | **Batch API** savings, **drift** observability, **pinned** model governance |
| **Security & compliance** | **Speed** of delivery for core logic | **Production** hardening, **identity**, **compliance** |
| **Data** | **In-memory** | **Durability**, **multi-instance** consistency |

---

## 11. Related documents

- `docs/DESIGN.md` — system design, data flow, scaling, **known failure modes**, ADR index.
- `docs/IMPLEMENTATION-PLAN.md` — step-by-step implementation aligned with these choices.
- `docs/adr/` — formal Architecture Decision Records (ADRs) when a decision is made to upgrade or replace an item above.
  - `docs/adr/0001-record-architecture-decisions.md` — ADR process and template.
  - `docs/adr/0002-hybrid-ranking-baseline.md` — hybrid (semantic + deterministic) ranking vs embeddings-only.
  - `docs/adr/0003-openai-embeddings-sync-in-process-batching.md` — synchronous in-process embedding batching (no OpenAI Batch API).
  - `docs/adr/0004-pure-dotnet-vector-recall.md` — in-memory managed Top-K recall vs native ANN (FAISS) later.
  - `docs/adr/0005-hash-keyed-memory-caching.md` — `IMemoryCache` via `ICacheService`, hash keys, ingestion dedupe.
  - `docs/adr/0006-llm-structured-extraction-with-heuristic-fallback.md` — LLM JSON + heuristic merge and validation.
  - `docs/adr/0007-fluentvalidation-for-api-commands.md` — FluentValidation for ingest/rank commands.
  - `docs/adr/0008-offline-ranking-evaluation-deferred.md` — offline NDCG-style metrics deferred until golden sets exist.

---

*Last updated: Phase 8 — DESIGN.md and ADRs 0005–0008; aligned with intentional simplifications in the implementation plan (thin-slice, workable, minimal third-party stack).*
