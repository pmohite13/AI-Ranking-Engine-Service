# AI-native service architecture design
_Exported on 4/7/2026 at 11:26:31 GMT+5:30 from Cursor (3.0.12)_

---

**User**

I am designing an AI-native service named "AI-Ranking-Engine-Service" for a talent intelligence system that ranks candidates against job roles. The goal is to move beyond traditional keyword matching and build a scalable, high-quality matching engine. I had very detail level of discussion with ChatGPT. I have captured the entire discussion with ChatGPT in @prompts/conversation.txt document. I want you to very thoroughly read, study, and analyse each and every detail from minute to whole mentioned in @prompts/conversation.txt and get the context out of it. 

You will find in the @prompts/conversation.txt file with the suggestions and feedbacks. I just dont want to be a mediocre software professional who simply consume and integrate the technologies to build some system, I also want to be a real producer and problem solver type of techie. We want to create a better highly scalable, robust, standard, modular, secure and performant architecture and system.

It is highly possible that all the suggestions or feedback is not covering everything or missed some part, so feel free to include that as well.

We dont want naive solution. Follow best practices and standards of software development and AI software development.

We also want to keep provision for creating "Architecture Decision Records - ADR" so that we know why certain architectural decision is taken.

I have consolidated few inputs from the conversation.txt file below for your quick reference as they are very important to be included and you dont miss them.

See we dont want any workaround or patch-work. We need to have a standard, permanent, scalable, robust, modular approach. 

We want to have highest performance possible and lowest unnecessary usage of memory or bloating memory. We want to follow all best practices possible to keep our project highly performant, secure, light and robust. 

If needed then please also check what or how other people across the industry around the globe is doing in this type of scenario or tasks or project.

If needed please refer to and align with official documentation and latest best and standard practices.

Create unit tests and make sure everything is working properly.

If you want anytime to do research online, please feel free to do it.

Dont overcrowd any single file, use separate files if the count of lines of code is more than 500. This helps us to manage the code and give us clarity and stop creating mamoth lines of code in single file, bloating the file.

We dont need any naive solution. We dont want to duplicate any code, we want to leverage as much as we can the current code infrastructure. We want to make sure we always use highly scalable, robust, modular, performant, secure approach and solution.

So I want that you create single (not multiple) .md file format document and mention in it in-detail, step by step implementation plan.

My consolidated few inputs below:
###

1. We want to use .net
2. We want to use .net environment supportive  PDF and DOCX parsers
3. we want to use OpenAI and batch embedding.
4. Yes use In-memory FAISS with fallback to cosine similarity
5. Use Caching to cache hashed results for performance

We want to use relevant Design Pattern, SOLID Principles of Software Development Life Cycle. 
We want to use Fluent Validation 
We want to leverage DI, proper standard exception handling and retry mechanism
###

---

**Cursor**

Reading `conversation.txt` and scanning the project structure to produce a single detailed implementation-plan document.



Gathering industry practices for .NET embedding pipelines and ADR templates to strengthen the plan.



Creating a single comprehensive implementation plan that synthesizes the conversation, your consolidated requirements, industry practices, and ADR provisions.



Here’s what was delivered.

## Deliverable

A **single** markdown document:

**`e:\Repos\cursor.ai\public-projects\AI-Ranking-Engine-Service\IMPLEMENTATION-PLAN.md`**

It is a **step-by-step implementation plan** that:

1. **Extracts and organizes** everything from `prompts/conversation.txt`: thin-slice scope, hybrid ranking (semantic + skills + experience + keyword boost), cold start, producer–consumer concurrency, channel/backpressure, idempotency, DESIGN.md expectations (including **known failure modes** as production limitations), anti-patterns to avoid, LLM structured extraction with fallback/caching/batch ideas, testing focus, and suggested folder layout.

2. **Locks in your consolidated stack**: .NET, PdfPig/iText7 + Open XML for DOCX, OpenAI with explicit **two meanings of “batch”** (in-process synchronous batching vs **OpenAI Batch API** for bulk/offline), in-memory **FAISS** + **cosine fallback**, **hash-based caching**, FluentValidation, DI, exceptions, **Polly-style retries**.

3. **Adds what the chat often skipped**: security/PII, API limits, two-stage **recall + re-rank** at scale, evaluation/ops notes, multi-instance cache caveats, and pointers to **official docs** (Microsoft Learn, OpenAI embeddings/batch, FluentValidation, Polly, RFC 7807 Problem Details).

4. **Defines ADR practice**: `docs/adr/`, meta-ADR, naming, and a table of **early ADR topics**.

5. **Phases work** from Phase 0 (repo + ADRs) through Phase 8 (hardening + DESIGN.md), with a **Definition of Done** and **~500 lines per file** called out.

6. **Testing**: unit tests for parsers, ranking, cosine/FAISS parity, validators, cache behavior; optional integration with mocked OpenAI.

If you want the next concrete step in the repo, we can scaffold the .NET solution and `docs/adr/` to match this plan.

---

**User**

As per @docs/IMPLEMENTATION-PLAN.md document, below are some of the consideration. Please go through them and update the @docs/IMPLEMENTATION-PLAN.md document accordingly. 

1. For "Parsing: PDF via PdfPig or iText7; DOCX via Open XML SDK (conversation: “OpenXML”)." - Lets go with PdfPig as it is completely free for both personal and commercial projects without the restrictive "copyleft" requirements of the AGPL.
2. For "LLM layer (optional “advanced edge”): Structured JSON extraction from resume/job text; never the sole source of truth — pair with regex/heuristic fallback and validation." - We don't want to keep this optional, we want to leverage this.
3. For "A.4 Gaps the conversation does not fully spell out (included here on purpose)" 

	a. Security: secrets management, least-privilege API keys, PII handling for resumes, secure file upload limits, virus scanning policy (org-specific). - we want to keep things as miminal as possible for this project - We can use .env files for secrets, upload limits of 200KB (configurable), rest we can skip like authentication/authorization, RBAC, PII, etc for this project.
	b. API hardening: rate limiting, request size limits, authentication/authorization for production boundaries. - we can skip this for now for this project.
	c. Data lifecycle: retention, deletion, encryption at rest (if persistence is added later). - we can skip this for now for this project.
	d. MLOps / AI ops: model/version pinning for embeddings, monitoring embedding drift, cost controls. - we can skip this for now for this project as we will rely on OpenAI Vector Embedding API.
	e. Evaluation: offline metrics (NDCG, precision@k) if you later add golden sets — not mandatory for thin-slice but worth ADR if introduced. - Evaluation is good have, but keep it optional.
	f. Two-stage retrieval (industry common): fast candidate recall (vectors/FAISS) + re-rank (hybrid features) at larger scale than brute-force full pairwise comparison. - yes that make sense

4. For "Part F — Security, Performance, and Memory Discipline" 
	a. Store API keys in User Secrets (dev), Azure Key Vault / environment variables (prod) — never in source control. - use environment variables
	b. File upload: max size, allowed extensions, stream processing where possible to avoid loading huge files entirely into memory unless bounded. - 200KB (configurable), extensions - PDF, DOCX (extensible for new formats), stream processing - yes.
	c. For "Streaming and spans where safe; avoid LINQ in hot loops for huge candidate sets." - Yes but dont overkill and dont overdo. Only use wherever make sense not for sake of it.
	d. For "Object pooling for large buffers only if profiling shows need (avoid premature optimization — ADR if added)." - yes dont overkill or overdo.
	
5. For "E.3 OpenAI embeddings and “batch”" we will go with "Synchronous batching (in-process)" since we want to keep things simple and workable.

6. For "E.4 FAISS on .NET" - Eventhough FaissNet is good choice considering scale and performance, we will go with "Pure .NET (Fallback Vector Search)" as primary approach as it is simple and achievable without introducing complexity of FAISS native dependencies.
	

---

**Cursor**


Reading `docs/IMPLEMENTATION-PLAN.md` and updating it to reflect your decisions.




Updating the implementation plan throughout: applying edits in logical sections.




Here is what was updated in `docs/IMPLEMENTATION-PLAN.md`:

### Summary of edits

1. **PdfPig** — Locked as the PDF library, with a short rationale (commercial-friendly, no AGPL-style copyleft). **iText7** is no longer presented as an equal choice.

2. **LLM structured extraction** — Treated as a **required** pipeline stage everywhere (Parts A.3, B, D.2, D.5, Phase 5, interfaces `ILLMStructuredExtractor`, tests, failure modes). Still paired with **regex/heuristic fallback** and validation.

3. **Section A.4** — Replaced the old “gaps” list with a **minimal scope table**: `.env` + env vars, **200 KB** configurable uploads, **PDF/DOCX** + streaming, explicit **out-of-scope** items (auth, RBAC, PII program, rate limits, data lifecycle, MLOps). **Evaluation** (NDCG, etc.) marked **optional** with ADR if added. **Two-stage retrieval** (Top-K + hybrid re-rank) kept **in scope**.

4. **Part B** — Renumbered items: **sync in-process embedding batching only**, **pure .NET vector recall** as primary (FAISS only as a possible future ADR), **LLM stage required**.

5. **Part E** — **E.2** PdfPig-only; **E.3** only synchronous in-process batching (OpenAI **Batch API** explicitly out of scope unless a future ADR); **E.4** retitled to **pure managed vector search** as primary, FAISS as optional later.

6. **Part F** — Secrets via **environment variables** + **`.env`** for local (gitignored); **200 KB** default, **PDF/DOCX**, streaming; performance text tuned to **pragmatic** use of spans/LINQ (no overkill); object pooling only when profiling justifies it.

7. **Phases / DoD / Next actions** — Reordered to include **LLM extraction** early; **Phase 4** = pure .NET recall (no FAISS); **Phase 8** ADRs and optional evaluation; **Definition of Done** and **Part O** aligned with the above.

8. **Part L** — Removed FAISS restart wording; added **in-memory store empty on restart** and **LLM JSON failure** failure modes.

If you want the same content mirrored at the repo root (you previously had `IMPLEMENTATION-PLAN.md` there), say so and we can add a short pointer file or copy.

---

**User**

We made lots of decisions which are intentional in nature just to keep things simple and we are aware of better, scalable approaches like for example  
1. To use in-memory Channels, then distributed alternatives like Azure Queue storage, AWS SQS, etc. 
2. We are intentionally using in-memory processing for semantic similarity instead of using available 3rd party vector DB like Pinecone or Faiss, etc., because we want to keep things simple and achievable for this task.
3. Currently we want to keep things workable and simple, so wherever possible we want to use in-memory processing, instead of relying on third party techstack.
4. API hardening: rate limiting, request size limits, authentication/authorization for production boundaries. - we can skip this for now for this project.
5. MLOps / AI ops: model/version pinning for embeddings, monitoring embedding drift, cost controls. - we can skip this for now for this project as we will rely on OpenAI API auto model suggestion.
etc. etc...

So Can you please list down all the known architectural decisions we took and aware of and log it in @docs/Known-Architectural-Gaps.md document for future references?

---

**Cursor**


Writing a structured document that records intentional simplifications and the stronger alternatives we're deferring.




Created **`docs/Known-Architectural-Gaps.md`** as a single reference that:

1. **States purpose** — Intentional trade-offs vs real defects; how to use it with ADRs and scale-up planning.

2. **Uses tables** for: **messaging/queues** (Channels vs Azure Queue, SQS, Service Bus, etc.), **vector search** (pure .NET vs FAISS, Pinecone, pgvector, managed search), **caching** (in-memory vs Redis), **embeddings** (sync batching vs OpenAI Batch API, model pinning), **MLOps** (drift, cost, offline eval), **security/API** (no rate limit/auth for now vs production patterns), **secrets** (env/.env vs Key Vault), **data lifecycle** (in-memory vs DB, persistence), **LLM/parsing** (OpenAI + PdfPig vs OCR/fine-tuned), **observability** (logging vs OpenTelemetry stack).

3. **Adds a short “summary themes” section** so the overall intent is visible at a glance.

4. **Links** to `IMPLEMENTATION-PLAN.md` and `docs/adr/`.

If you want, we can add a one-line pointer in `IMPLEMENTATION-PLAN.md` under Part A or Part C pointing to this file (I did not edit the plan unless you ask).

