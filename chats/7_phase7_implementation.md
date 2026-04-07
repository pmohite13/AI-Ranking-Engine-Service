# Project implementation plan and phase 7 details
_Exported on 4/7/2026 at 13:41:32 GMT+5:30 from Cursor (3.0.12)_

---

**User**

I want you to please thoroughly read, study and analyse docs/IMPLEMENTATION-PLAN.md, docs/Known-Architectural-Decisions.md, docs/raw_requirement.txt and entire project so that you will get overall understanding about the project structure, requirement, dependencies and data/process flow.


As per "docs/IMPLEMENTATION-PLAN.md" document we have implemented seven phases so far "Phase 0 — Repository and governance (0.5–1 day)", "Phase 1 — Domain and application contracts (0.5–1 day)", "Phase 2 — Document parsing (1 day)", "Phase 3 — Embedding and caching (1–2 days)", "Phase 4 — Pure .NET vector recall (1–2 days)", "Phase 5 — LLM structured extraction + hybrid ranking (1–2 days)", "Phase 6 — API surface (0.5–1 day)" including all or some modules/features/tasks/goals implementation mentioned in those respective phase/s. And now we need to start with next phase "Phase 7 — Concurrency and ingestion (1 day)". Lets begin with all modules/features/tasks/goals "1.", "2.", "3." as they are all inter-related.


Read, study and analyse "docs/IMPLEMENTATION-PLAN.md" document to get better context.

See we dont want any workaround or patch-work. We need to have a standard, permanent, scalable, robust, modular approach. 

We want to have highest performance possible and lowest unnecessary usage of memory or bloating memory. We want to follow all best practices possible to keep our app highly performant, secure, light and robust. 

If needed then please also check what or how other people across the industry around the globe is doing in this type of scenario.

If needed check if it is aligned with official documentation and latest best and standard practices.

Create tests and make sure everything is working properly as per the updates. Dont run test, I will run manually later.

If you want anytime to do research online, please feel free to do it.

Dont overcrowd any single file, use separate files if the count of lines of code is more than 500. This helps us to manage the code and give us clarity and stop creating mamoth lines of code in single file, bloating the file.

Make sure the current functionality does not break. (MOST IMPORTANT)

We dont need any naive solution. Our project is very big and we dont want to duplicate any code, we want to leverage as much as we can the current code infrastructure. We want to make sure we always use highly scalable, robust, modular, performant, secure approach and solution.




---

**Cursor**

I’ll first read the implementation and architecture docs, then map the existing code paths for ingestion/concurrency so Phase 7 can be implemented cleanly on top of current modules without regressions. After that I’ll apply focused changes plus tests for the Phase 7 items 1–3.

I’ve finished reading the planning and architecture docs and will now inspect the full codebase to identify exactly what from Phase 7 is already present vs missing, then I’ll implement the remaining parts for items 1–3 with tests.

I’ve identified the concrete gap: ingestion is still synchronous inside the endpoint. Next I’m implementing a bounded channel queue + background workers + idempotency coordinator, while keeping the endpoint contract behavior stable so existing flows won’t break.

I’m now wiring the new ingestion pipeline into infrastructure with a bounded `Channel` queue, hosted workers, and idempotency store, then I’ll adapt the endpoint to use it and add tests.

---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\46.txt:7-41 

---

**Cursor**

Thanks for sharing the build output — I see the exact accessibility issue. I’ll fix that first, then run a full build pass and continue with the remaining Phase 7 test updates.

---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\48.txt:7-50 

---

**Cursor**


Resolving ambiguous `IngestionEntityKind` references in the endpoints file.




Adding a `using` alias for the API `IngestionEntityKind` so it doesn't conflict with the Application enum.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\49.txt:7-29 

---

**Cursor**


Fixing `ReadFileBytesAsync`: `IFormFile.OpenReadStream(long)` isn't available in this target framework — using `OpenReadStream()` and reading at most `maxUploadBytes`.




Replacing `OpenReadStream(maxUploadBytes)` (not available on this `IFormFile`) with `OpenReadStream()` and a bounded read loop.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\50.txt:7-26 

---

**Cursor**


**Build status:** The solution build completed successfully — all six projects (Domain, Application, Infrastructure, Api, UnitTests, IntegrationTests) compiled with **0 warnings and 0 errors**.

So the earlier fixes are in good shape: **`IFormFile.OpenReadStream()`** without the unsupported size overload, **bounded reads** up to `MaxUploadBytes`, and **unambiguous `IngestionEntityKind`** aliases in the ingest endpoint.

When you’re ready, run **`dotnet test`** on the solution locally to exercise unit and integration tests; I won’t run them here per your preference.

---

**User**

Are you 100% confident that this code will work properly. Please recheck and reevaluate everything to make sure everything is working smoothly and seamlessly and there are no dangling issues or new potential issues.

---

**Cursor**


Auditing the Phase 7 ingestion path end-to-end: pipeline, idempotency store, channel queue, background worker, DI lifetimes, and tests.




Checking `ICacheService` and the pipeline flow to fix idempotency memory and related issues.




Implementing fixes: we found real issues — idempotency store never removes completed entries (unbounded memory), sequential dedupe should use a bounded cache, and the background worker should log depth via `IIngestionQueue` instead of a fragile cast. Applying those changes.




Implementing fixes: completing the idempotency store (remove on complete), adding bounded dedupe via `ICacheService`, including the embedding model in the idempotency key, and cleaning up the background worker’s depth logging.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\51.txt:7-33 

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\52.txt:7-26 

---

**Cursor**


**Build status:** The solution build completed successfully — all projects compiled with **0 warnings and 0 errors**, including the Phase 7 unit test project after the `ConfigureAwait` fix in `IngestionIdempotencyStoreTests.cs`.

You can run `dotnet test` on the solution when you want to validate behavior end-to-end.

---

**User**

Okay so "Phase 7 — Concurrency and ingestion (1 day)" implementation is complete, correct?

---

**Cursor**


Here’s a precise answer against **Phase 7** in `docs/IMPLEMENTATION-PLAN.md`:

### Done (core Phase 7)

1. **Channel-based ingestion + hosted worker + bounded channel + depth logging**  
   - `ChannelIngestionQueue` (bounded `Channel`, `FullMode.Wait` for backpressure)  
   - `IngestionBackgroundWorker` (`BackgroundService`, configurable worker count)  
   - Periodic **Information** log of approximate queue depth  

2. **Idempotent ingestion by content hash (plus model)**  
   - In-flight **single-flight** via `IIngestionIdempotencyStore`  
   - **Sequential** dedupe via `ICacheService` / `IMemoryCache` with TTL (`Ingestion.IngestionDedupCacheHours`)  
   - API exposes `Deduplicated` on `IngestDocumentResponse`  

### Optional / not required for “done”

3. **“Load-test lightly (optional): verify backpressure does not OOM”**  
   - This is **explicitly optional** in the plan. There is **no** checked-in load test or script unless you added one separately; behavior is **by design** bounded (upload cap + bounded channel + wait-on-full), but **no** formal load/OOM verification unless you run it yourself.

So: **Yes — Phase 7 is implemented for the required items (1–2).** Item **3** is **optional** in the plan; treat it as **not done** unless you’ve run a load pass and documented results.

If you want Phase 7 to be “complete” including the optional slice, the next step is a short **k6 / NBomber / wrk** (or manual burst) run against `/api/v1/documents/ingest` with notes on queue depth and memory.

