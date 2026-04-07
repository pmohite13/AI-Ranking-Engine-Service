# AI-Ranking-Engine-Service — Detailed Step-by-Step Implementation Plan

This document consolidates the full analysis of `prompts/conversation.txt`, your explicit engineering constraints, industry-aligned practices for AI-native talent matching, and a phased roadmap suitable for a **thin-slice but production-minded** service (standalone API replacing or augmenting a legacy matcher).

---

## Part A — Context Extracted from the Conversation

### A.1 Business and assessment context

- **Problem**: Legacy keyword matching yields poor candidate–role matches at scale.
- **Scale**: ~50,000 active candidates and ~1,500 live job openings; target output is **Top 10 candidates per job role**.
- **Deliverable type**: A **working thin-slice** focused on **core logic** — not a full recruitment platform. One **AI-native ranking service** that: autonomously parses disparate documents (PDF, DOCX, etc.), runs a **document ingestion → parsing → scoring → ranked output** pipeline, supports **high-concurrency ingestion** without data loss, and documents **cold-start** behavior.
- **Artifacts expected in assessments**: runnable code, **DESIGN.md** (system design, data flow, scaling, **known failure modes**), AI interaction log. This plan complements DESIGN.md; **known failure modes** means **honest production limitations and degradation paths**, not an admission of unfixed bugs.

### A.2 Architectural themes the conversation emphasizes

| Theme | Intent |
|--------|--------|
| **Hybrid ranking** | Do not stop at “embedding + cosine”. Combine **semantic similarity** with **deterministic** signals (skills, experience, controlled keyword overlap). |
| **Determinism + explainability** | Scoring should be **reproducible** and **explainable** (weights, components, optional score breakdown). |
| **Cold start** | New jobs have no history; rely on **embeddings + rule/ontology-based features + structured job/resume understanding**, not on learned click data. |
| **Concurrency** | **Producer–consumer**, **channels**, **batching**, **backpressure**, **retries**, **idempotency**. |
| **Modularity** | Replaceable parsers, embedding providers, vector index, rankers — interfaces over implementations. |
| **Observability & testability** | Logs, metrics, trace hooks; unit tests for parsing, ranking edge cases, and validation. |
| **Pitfalls to avoid** | Embeddings-only, no concurrency story, no cold-start narrative, opaque scores, monolithic files, ignoring failure modes. |

### A.3 Suggested technology mapping from the conversation (with project decisions applied)

- **Runtime**: .NET (Minimal API or equivalent thin host).
- **Parsing**: **PDF** via **[PdfPig](https://github.com/UglyToad/PdfPig)** — **fully free** for personal and commercial use without **AGPL-style copyleft** obligations that apply to some alternatives. **DOCX** via **DocumentFormat.OpenXml** (Open XML SDK).
- **Embeddings**: OpenAI **Embeddings API** with **synchronous in-process batching** (group inputs into efficient API calls; see Part E.3).
- **Vector search (primary)**: **Pure .NET, in-memory** approximate recall (e.g., normalized vectors + cosine / dot-product **Top-K** over the candidate corpus) — **no native FAISS dependency** in this project phase (keeps deployment simple; see Part E.4).
- **LLM layer (required)**: **Structured JSON extraction** from resume and job text via OpenAI (or configured chat/completions API) — **always** combined with **regex/heuristic fallback** and **validation** so extraction is never the only signal and failures degrade safely.

### A.4 Out-of-scope vs in-scope for *this* project (minimal by design)

The conversation listed production gaps; **this repository intentionally stays minimal**:

| Area | This project |
|------|----------------|
| **Secrets** | **`.env` files** (local/dev) and **environment variables** at runtime — no Key Vault requirement here. |
| **Uploads** | **Max size 200 KB** ( **configurable** ); allowed types **PDF, DOCX** with a design that **allows new formats** later via parsers + factory. **Stream** processing where practical so files are not held entirely in memory without bound. |
| **Security extras** | **Out of scope**: authentication/authorization, RBAC, formal PII programs, virus scanning — document as future work if productized. |
| **API hardening** | **Out of scope for now**: rate limiting, prod-style auth boundaries — can be ADR’d later. |
| **Data lifecycle** | **Out of scope for now**: retention policies, encryption at rest, deletion workflows — add when persistence/production DB is introduced. |
| **MLOps / AI ops** | **Out of scope for now**: drift monitoring, formal cost dashboards — rely on **OpenAI Embeddings API** with sensible client-side batching and caching. |
| **Evaluation** | **Nice-to-have / optional**: offline metrics (e.g., NDCG, precision@k) if golden sets appear — **ADR if introduced**. |
| **Two-stage retrieval** | **In scope**: **fast in-memory vector recall (Top-K)** + **hybrid re-ranking** (semantic + structured features) — avoids naive full pairwise scoring of every candidate on every request as N grows. |

---

## Part B — Your Locked Engineering Constraints (Consolidated)

1. **.NET** as the primary stack.
2. **PDF** with **PdfPig**; **DOCX** with **Open XML SDK** — **.NET-native**, no FAISS-style native deps for vector search in v1.
3. **OpenAI** **Embeddings API** with **synchronous in-process batching** only (simple, workable path; no OpenAI Batch API jobs required for this project).
4. **Pure .NET in-memory vector search** as the **primary** recall mechanism (normalized embeddings + **Top-K** by cosine similarity / dot product in managed code). Optional future **ADR** if **FAISS** (e.g., FaissNet) is introduced for scale.
5. **Caching** keyed by **cryptographic hashes** of canonical inputs (document bytes or normalized text + model id) for **parsed outputs**, **embeddings**, and **LLM structured extracts** — to reduce cost and latency.
6. **LLM structured extraction** is a **required** pipeline stage (with fallback + validation), not an optional add-on.
7. **SOLID**, **appropriate design patterns**, **FluentValidation**, **DI**, **structured exception handling**, **retry** (e.g., Polly) — as **first-class** concerns, not afterthoughts.
8. **Architecture Decision Records (ADRs)** as a **standard** artifact for every major choice.
9. **No single source file** should become a “god file”; **target &lt; ~500 lines per file** (split by responsibility when approaching that limit).
10. **Unit tests** (and selective integration tests) to keep the pipeline honest as it evolves.

---

## Part C — Architecture Decision Records (ADR) — Provision and Process

### C.1 Location and naming

- Create `docs/adr/` (or `Architecture/adr/`) with:
  - `0001-record-architecture-decisions.md` — **meta-ADR** explaining why ADRs exist and the template.
  - `NNNN-short-title.md` — one file per decision (e.g., `0002-use-hybrid-ranking.md`).

### C.2 Suggested ADR template (embedded in repo)

Each ADR should contain: **Title**, **Status** (Proposed / Accepted / Superseded), **Context**, **Decision**, **Consequences** (positive/negative), **Alternatives considered**, **References** (official docs, internal spikes).

### C.3 Candidates for early ADRs

| ADR topic | Why |
|-----------|-----|
| Hybrid vs embedding-only ranking | Core product quality and explainability. |
| Pure .NET vector recall vs native index (e.g., FAISS) later | Simplicity now vs scale/latency trade-off when N is very large. |
| Synchronous in-process embedding batching (chosen) | Documents alignment with “simple and workable” — no Batch API jobs. |
| Caching layer (memory vs distributed) | Multi-instance consistency only if you scale out later. |
| Secrets via `.env` / environment variables | Minimal local and deployment story without cloud vaults. |
| FluentValidation vs DataAnnotations | Consistency and complex rules. |
| LLM extraction + heuristic fallback | Required stage; failure modes and test strategy. |

---

## Part D — Recommended Solution Architecture (Logical View)

### D.1 Layering (Clean/Hexagonal-friendly)

Keep **domain** free of OpenAI/HTTP/SDK types:

- **Domain**: entities (`CandidateProfile`, `JobProfile`, `ParsedDocument`, `RankingResult`, `ScoreBreakdown`), value objects, domain exceptions, **pure** scoring math where possible.
- **Application**: use cases (`IngestDocument`, `EmbedBatch`, `RankTopCandidates`), orchestration, **FluentValidation** validators for commands/DTOs, interfaces (`IDocumentParser`, `IEmbeddingClient`, `IVectorIndex` / `IVectorRecall`, `ILLMStructuredExtractor`, `IRankingStrategy`, `ICacheService`).
- **Infrastructure**: OpenAI client wrappers (embeddings + LLM for structured extraction), **pure .NET** in-memory vector index / Top-K search, PDF (PdfPig) / DOCX parsers, Polly policies, cache implementations (`IMemoryCache` + optional Redis later), telemetry.
- **Presentation**: Minimal API endpoints, request/response DTOs, middleware (exception handling, correlation IDs).

This supports **SOLID**: single responsibility per component, **open/closed** via strategies, **Liskov**-safe swappable index implementations, **interface segregation** for small client contracts, **dependency inversion** toward abstractions.

### D.2 End-to-end data flow (pipeline)

1. **Ingest**: accept file bytes or text + metadata (candidate/job id, content type); enforce **configurable max size (default 200 KB)** and **PDF/DOCX** (extensible to more types later).
2. **Parse**: format-specific parser → **normalized plain text** + optional structure.
3. **Enrich (required)**: LLM **structured extraction** (skills, years, role, etc.) → **merge** with **regex/heuristic fallback** on parse/validation failure; validate ranges (e.g., years 0–60); normalize skills (synonym map — ADR if non-trivial).
4. **Embed**: **synchronous in-process batches** → **float[]** vectors; **cache** by hash(model + normalized text).
5. **Index / recall**: maintain candidate vectors in a **pure .NET in-memory** structure; for a job query vector, **retrieve Top-K** (e.g., 50–200) by cosine similarity, then **hybrid re-rank** — **two-stage** pattern without native ANN libraries in v1.
6. **Rank**: combine **semantic** (cosine / similarity from embeddings) + **skill overlap** + **experience fit** + **keyword/phrase** signals; apply **configurable weights** (`RankingWeights`).
7. **Respond**: Top 10 with **explainable breakdown**.

### D.3 Cold start (explicit product behavior)

- **No historical matches required**: job embedding + required skills + min experience come from **current job text** and structured extraction.
- **Quality levers**: clear skill list extraction, seniority normalization, synonym handling (“C#” vs “C Sharp”), and **hybrid** scoring so a single bad embedding does not dominate.

### D.4 High-concurrency ingestion

- **Producer–consumer** with `System.Threading.Channels.Channel<T>` (bounded capacity for **backpressure**).
- **Worker pool** (`Parallel.ForEachAsync` or hosted `BackgroundService` consumers) with **max parallelism** tied to CPU and external API limits.
- **Idempotency**: ingestion keyed by **content hash** + entity id; duplicate uploads **dedupe** or **no-op**.
- **Retries**: transient failures (HTTP 429/5xx) via **Polly** (retry + jitter + circuit breaker for dependency protection).

### D.5 Design patterns (use deliberately)

| Pattern | Where |
|---------|--------|
| **Strategy** | `IRankingStrategy`, pluggable `ISkillNormalizer`. |
| **Chain of responsibility** | Enrichment pipeline (parse → normalize → **LLM extract** → validate → merge fallback). |
| **Decorator** | Cache decorator around `IEmbeddingClient` / `ILLMExtractor`. |
| **Factory** | Parser factory by MIME/extension. |
| **Template method** | Shared embedding batching skeleton with provider-specific fill-ins. |
| **Outbox / queue** (future) | If moving from in-memory to message bus — ADR first. |

---

## Part E — Technology Choices Aligned with Official / Common Practice

### E.1 .NET

- **.NET 8+** LTS for long-term support, minimal API, `Microsoft.Extensions.*` hosting.
- **Official docs**: [ASP.NET Core fundamentals](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/), [Dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection), [HttpClient best practices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests) (`IHttpClientFactory`).

### E.2 PDF / DOCX

- **PDF**: **PdfPig** — permissive license, **free for commercial use**, and **no AGPL-style copyleft** that would force derivative works to adopt the same license (a concern cited for some alternatives). Fits a **simple, standard** dependency story.
- **DOCX**: **DocumentFormat.OpenXml** — Microsoft-supported Open XML SDK.

### E.3 OpenAI embeddings and “batch”

**Decision for this project**: use **only synchronous in-process batching** — group multiple texts into **one or few** embedding API calls per [OpenAI Embeddings API](https://platform.openai.com/docs/guides/embeddings) limits (tokens per request, batch size). This keeps the pipeline **simple, predictable, and easy to run** without asynchronous **Batch API** jobs or JSONL file round-trips.

- **Out of scope (unless a future ADR reverses)**: OpenAI **Batch API** (24h-style async jobs) for embeddings — reserve for a possible **bulk re-embed** product phase, not required here.

Use **`openai-dotnet`** (or equivalent) with **`IHttpClientFactory`** + Polly.

### E.4 Vector search on .NET (primary: pure managed)

**Decision for this project**: **pure .NET in-memory vector recall** as the **primary** approach — e.g., store **L2-normalized** embedding vectors and compute **Top-K** candidates by **cosine similarity** (dot product on normalized vectors) or an equivalent **O(N)** scan with **SIMD-friendly** inner loops **only where profiling shows benefit**. This avoids **native FAISS** binaries, platform interop, and extra deployment complexity.

- **Why not FAISS now**: FaissNet / native FAISS can win at very large N, but add **operational and build complexity**; the plan prioritizes **achievable, maintainable** code first.
- **Future option**: introduce **FAISS** (or an external vector DB) behind `IVectorRecall` via **ADR** if latency or memory at 50K+ vectors requires it.
- **Consistency**: whether you use a simple list, an array of structs, or later an optimized index, **the same similarity definition** used in **hybrid re-ranking** should match the **recall** stage (test with fixed vectors).

### E.5 Caching

- **Keyed by SHA-256** (or SHA-512) of **canonical bytes/text** + **model name/version**.
- Prefer **`IMemoryCache`** for single instance; document **cache stampede** mitigation (single-flight / `GetOrCreateAsync`).
- For multi-node later: **Redis** + ADR.

### E.6 Validation — FluentValidation

- Validate **commands** and **API models** in the application layer; keep rules **testable** and **composable**.
- Official: [FluentValidation docs](https://docs.fluentvalidation.net/).

### E.7 Resilience — Polly

- Retries with **exponential backoff + jitter** on 429/5xx; **circuit breaker** around OpenAI; **timeout** per attempt.
- Official: [Polly](https://www.pollydocs.org/) and Microsoft resilience extensions where applicable.

---

## Part F — Security, Performance, and Memory Discipline

### F.1 Secrets and uploads (minimal scope)

- **Secrets**: load from **environment variables**; for local development, support **`.env`** (e.g., `dotnet-dotenv` or template copied to `.env` — **gitignore** it) so keys are **not** committed. No Key Vault requirement for this project.
- **File upload**: **default max size 200 KB** (**configurable**); allowed extensions **PDF** and **DOCX**, with parser registration designed so **new formats** can be added without rewriting the pipeline.
- **Processing**: prefer **streaming** reads so the service does not unnecessarily hold entire files in memory when a bounded stream suffices.
- **Explicitly out of scope** for this repo: formal **PII** governance, auth/RBAC, virus scanning — call these out in DESIGN.md as **future** if the product hardens.

### F.2 Performance (pragmatic)

- Use **`Span<T>` / `Memory<T>`**, **avoid LINQ in tight hot paths**, or **micro-optimizations** only **where they clearly help** (large N, profiling). Do **not** optimize for its own sake.
- **Parallelism**: cap `MaxDegreeOfParallelism` to protect CPU and API quotas.

### F.3 Memory

- **Chunking** long documents before embedding if over token limits; release raw buffers after parsing when possible.
- **In-memory embedding store**: footprint ≈ `num_vectors × dim × sizeof(float)` — acceptable for the thin-slice; **FAISS / quantization** only if an ADR adds them for scale.
- **Object pooling**: only if profiling shows need — **avoid premature optimization**; ADR if added.

---

## Part G — Exception Handling Model

Define a **small hierarchy**:

- **DomainException** — invalid business state (e.g., empty corpus).
- **ValidationException** — FluentValidation aggregate (map to **400**).
- **ExternalServiceException** — OpenAI, wrapped inner exception (**502/503** with safe message).

Use a **global exception handler** middleware mapping exceptions to **Problem Details** ([RFC 7807](https://www.rfc-editor.org/rfc/rfc7807)) for consistent API errors.

---

## Part H — Testing Strategy (Unit + Focused Integration)

### H.1 Unit tests

- **Parsers**: sample PDF/DOCX fixtures (small files in `tests/Fixtures`); assert non-empty text, no throw on well-formed docs.
- **LLM extraction**: mock `ILLMStructuredExtractor` — valid JSON → domain model; invalid / empty → **fallback** path; boundary validation (e.g., absurd years).
- **Normalization / skill matching**: synonym map, case insensitivity, “Java” vs “JavaScript” edge cases.
- **Ranking**: synthetic profiles — higher skill overlap ⇒ higher score; experience gap reduces score; weights sum behavior; deterministic ordering tie-break (e.g., by id).
- **Vector recall**: fixed vectors → **deterministic Top-K** ordering; hybrid re-rank uses **consistent** similarity with recall stage.
- **Validators**: FluentValidation rules (missing job id, invalid topK, upload over **200 KB** when configured, wrong extension, etc.).
- **Cache**: hash collision resistance not required; **same input ⇒ cache hit** mocked.

### H.2 Integration tests (optional but valuable)

- **Testcontainers** or **WireMock** for OpenAI (record/replay) — keep CI deterministic and **keyless**.

### H.3 Quality gates

- `dotnet test` in CI; coverage targets negotiated (e.g., **80%+** on domain + application).

---

## Part I — Observability

- **Structured logging** (`ILogger`) with **correlation id** per request.
- **Metrics** (future): embedding latency, queue depth, cache hit rate, OpenAI token usage.
- **Tracing**: OpenTelemetry hooks for HTTP and outbound calls — ADR when enabling exporters.

---

## Part J — Step-by-Step Implementation Plan (Phased)

### Phase 0 — Repository and governance (0.5–1 day)

1. Initialize **.NET solution**: `src/` for projects, `tests/`, `docs/adr/`.
2. Add **EditorConfig**, **nullable reference types**, **TreatWarningsAsErrors** (optional, team choice).
3. Add **README** with how to run, **`.env` / environment variables** for API keys (no secrets in git).
4. Write **ADR 0001** (meta) and **ADR 0002** (hybrid ranking baseline).

### Phase 1 — Domain and application contracts (0.5–1 day)

1. Create **Domain** project: entities, value objects, `RankingWeights`, `ScoreBreakdown`, domain errors.
2. Create **Application** project: interfaces for parsers, embeddings, index, ranking, cache; **CQRS-lite** commands if desired (optional).
3. Add **FluentValidation** validators for public API contracts (`IngestRequest`, `RankRequest`).
4. Unit tests for **pure** ranking and validation.

### Phase 2 — Document parsing (1 day)

1. Implement **`IDocumentParser`** with **`PdfDocumentParser`**, **`DocxDocumentParser`**, **`PlainTextParser`** (for tests).
2. **Factory** to select parser by content type/extension.
3. Normalize text: unicode, line endings, repeated whitespace — **single shared normalizer** (DRY).
4. Unit tests with fixtures; **500-line rule**: split parsers into separate files if needed.

### Phase 3 — Embedding and caching (1–2 days)

1. Implement **`OpenAIEmbeddingClient`** using official SDK + **`IHttpClientFactory`**.
2. Implement **synchronous in-process batching** only (chunk inputs to respect token limits); **ADR** documenting **no OpenAI Batch API** in v1.
3. Implement **`CachingEmbeddingClient`** decorator: key = `Hash(canonicalText + model + dimensions)`.
4. Polly policies: retry, breaker, timeout.
5. Unit tests with mocked HTTP; integration optional.

### Phase 4 — Pure .NET vector recall (1–2 days)

1. Implement **`IVectorRecall`** (or `IVectorIndex`): **add/update** candidate vectors, **search Top-K** by cosine similarity in **managed code** (normalized vectors recommended).
2. Unit tests: deterministic ordering, K boundary cases, consistency with hybrid score inputs.
3. Optional **ADR**: when to revisit **FAISS** / native ANN if N or latency demands it.

### Phase 5 — LLM structured extraction + hybrid ranking (1–2 days)

1. Implement **`ILLMStructuredExtractor`** (OpenAI chat/completions with **JSON schema** or strict JSON prompt) for **resume and job** text; **merge** with **regex/heuristic fallback**; **FluentValidation**-style checks on extracted numbers/lists.
2. Implement **`HybridRankingService`** using **configurable weights** from `IOptions<RankingWeights>`.
3. Workflow: **vector Top-K recall** → **hybrid re-rank** → **Top 10** (two-stage retrieval).

### Phase 6 — API surface (0.5–1 day)

1. Minimal API: e.g., `POST /api/v1/documents/ingest`, `POST /api/v1/jobs/{id}/rank`, `GET /health`.
2. **Problem Details** middleware; enforce **configurable upload size** (default **200 KB**); **PDF/DOCX** content types.
3. Swagger/OpenAPI for assessors. (**Auth/rate limiting** — out of scope for this project unless requirements change.)

### Phase 7 — Concurrency and ingestion (1 day)

1. **Channel**-based ingestion queue; hosted background worker; **bounded channel** + metrics log for depth.
2. Demonstrate **idempotent** ingestion by content hash.
3. Load-test lightly (optional): verify backpressure does not OOM.

### Phase 8 — Hardening and documentation (0.5–1 day)

1. Fill **DESIGN.md**: diagrams, data flow, scaling, **known failure modes** (parsing failures, ambiguous skills, API rate limits, in-memory index empty after restart, stale cache, LLM JSON parse failures → fallback).
2. Add remaining ADRs (OpenAI sync batching, pure .NET recall, caching, LLM extraction, validation).
3. Optional **evaluation** spike (offline metrics) — **only if time/value**; **ADR if added**.
4. Final **`dotnet test`** sweep; fix flakiness; document CI command.

---

## Part K — Suggested Repository Layout (illustrative)

```text
/docs/adr/
/src/
  AI.Ranking.Engine.Domain/
  AI.Ranking.Engine.Application/
  AI.Ranking.Engine.Infrastructure/
  AI.Ranking.Engine.Api/
/tests/
  AI.Ranking.Engine.UnitTests/
  AI.Ranking.Engine.IntegrationTests/   (optional)
```

Split any type cluster approaching **500 lines** into additional partial files or sibling types (e.g., `RankingService.Score.cs`).

---

## Part L — “Known Failure Modes” (for DESIGN.md alignment)

Document explicitly:

- Malformed or scanned PDFs (OCR not in scope unless ADR adds it).
- Skill ambiguity and taxonomy limits.
- Embedding model updates changing geometry — **version embeddings** in cache keys.
- OpenAI **rate limits** and **cost spikes** — circuit breaker and queue backlog.
- **Single-instance cache** inconsistency across scaled replicas — only relevant if you scale out; otherwise note **in-memory** limits.
- **Process restart**: **pure in-memory** vector store is **empty** until re-ingestion or reload — document rebuild strategy for a real deployment (persistence = future ADR).
- **LLM extraction** returns invalid JSON or hallucinated fields — **fallback** path and validation limits damage.

---

## Part M — Industry alignment (brief)

- **Hybrid retrieval + re-ranking** is standard in search and recommendation (first stage recall, second stage precision).
- **Two-tower** models (separate job/candidate encoders) appear at large scale — optional future ADR; **shared embedding model** is acceptable for thin-slice.
- **Responsible AI**: bias awareness in automated hiring — disclose limitations; human-in-the-loop for decisions — policy, not only code.

---

## Part N — Definition of Done (for this service increment)

- All **must-have** flows implemented: parse → **LLM structured extract** (+ fallback) → embed (**synchronous** in-process batches) → **pure .NET Top-K recall** → hybrid **Top 10**.
- **PdfPig** + **Open XML** parsers; **200 KB** default upload limit **configurable**.
- **Caching** by hash reduces repeat work for embeddings and LLM outputs where applicable.
- **FluentValidation**, **DI**, **Polly retries**, **structured errors** in place.
- **ADRs** started for major decisions (including pure .NET recall, no Batch API, secrets via env).
- **Unit tests** green; no file unnecessarily exceeds **~500 lines** without split.
- **DESIGN.md** and this plan kept consistent with actual behavior.
- **Evaluation** metrics (NDCG, etc.): **optional** — not a gate for this increment.

---

## Part O — Next Actions (execution order)

1. Scaffold solution and ADR folder.
2. Implement domain + hybrid ranking **pure logic** + tests (fast feedback).
3. Add parsers (**PdfPig**, **Open XML**) + normalization + tests.
4. Add **LLM structured extraction** + heuristic fallback + validation + tests.
5. Add OpenAI embedding client (**sync in-process batching** only) + Polly + cache decorator + tests.
6. Add **pure .NET** in-memory **Top-K** recall + hybrid re-rank + tests.
7. Wire Minimal API + ingestion channel + end-to-end manual run.
8. Write DESIGN.md and finalize ADRs; export AI interaction log for submission if required.

---

*This plan is a living companion to implementation: update ADRs and this document when scope or constraints change.*
