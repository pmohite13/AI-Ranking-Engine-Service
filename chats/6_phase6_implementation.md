# Project implementation plan and best practices
_Exported on 4/7/2026 at 13:24:03 GMT+5:30 from Cursor (3.0.12)_

---

**User**

I want you to please thoroughly read, study and analyse docs/IMPLEMENTATION-PLAN.md, docs/Known-Architectural-Decisions.md, docs/raw_requirement.txt and entire project so that you will get overall understanding about the project structure, requirement, dependencies and data/process flow.


As per "docs/IMPLEMENTATION-PLAN.md" document we have implemented six phases so far "Phase 0 — Repository and governance (0.5–1 day)", "Phase 1 — Domain and application contracts (0.5–1 day)", "Phase 2 — Document parsing (1 day)", "Phase 3 — Embedding and caching (1–2 days)", "Phase 4 — Pure .NET vector recall (1–2 days)", "Phase 5 — LLM structured extraction + hybrid ranking (1–2 days)" including all or some modules/features/tasks/goals implementation mentioned in those respective phase/s. And now we need to start with next phase "Phase 6 — API surface (0.5–1 day)". Lets begin with all modules/features/tasks/goals "1.", "2.", "3." as they are all inter-related.


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

Understood — I’ll first study `docs/IMPLEMENTATION-PLAN.md`, `docs/Known-Architectural-Decisions.md`, and `docs/raw_requirement.txt`, then map the existing Phase 0–5 implementation in code so I can implement Phase 6 (`1`, `2`, `3`) in a robust, production-grade way without breaking current behavior. I’ll start by gathering the docs and project structure now.

I’ve finished reading the core docs and constraints; next I’m mapping the actual implementation status of Phases 0–5 in the codebase and locating exactly what’s missing for Phase 6 API surface before I start editing.

I confirmed Phase 6 is only partially present (`/health` exists), so I’m now tracing existing contracts/services to wire proper ingest and rank endpoints without duplicating logic.

I’ve completed the architecture pass and identified the missing pieces for Phase 6: robust ingest/rank endpoints, centralized Problem Details error mapping, and an in-memory profile/job catalog to connect existing Phase 2–5 modules. I’m now implementing these in modular files and adding integration tests for the new API surface.

I’ve finished the core code changes and added Phase 6 integration tests with deterministic test doubles. Next I’m doing a compile/lint pass to catch any wiring issues and then I’ll patch any remaining errors.

---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\42.txt:7-68 

---

**Cursor**


Checking the `ScoreBreakdown` type definition and fixing the API endpoint mapping.




Updating the response mapping to use `ScoreBreakdown`'s actual property names (`SemanticRaw`, `WeightedSemantic`, etc.).



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\43.txt:9-35 

---

**Cursor**

I see it — that build error is from a missing namespace import in the new integration test factory. I’ll add the correct ASP.NET Core hosting using and keep the rest unchanged.

Fixed. `IWebHostBuilder` lives in `Microsoft.AspNetCore.Hosting`, and that `using` was missing in:

- `tests/AI.Ranking.Engine.IntegrationTests/Phase6/Phase6ApiEndpointsTests.cs`

Please rerun your build command:

- `dotnet build "e:\Repos\cursor.ai\public-projects\AI-Ranking-Engine-Service\AI.Ranking.Engine.sln" -c Release --no-restore`

If another compiler error appears, share the next terminal slice and I’ll patch it immediately.

---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\44.txt:7-26 

---

**Cursor**

Great — build is now clean across the full solution.

From your terminal output:

- `Build succeeded`
- `0 Warning(s)`
- `0 Error(s)`

Phase 6 implementation changes compile successfully with Release settings.

Next logical step (as you requested, you can run manually):
- `dotnet test "e:\Repos\cursor.ai\public-projects\AI-Ranking-Engine-Service\AI.Ranking.Engine.sln" -c Release --no-build`

If you want, I can now do a focused pass to harden Phase 6 further (request/response contracts, endpoint docs examples, and additional edge-case integration tests) before Phase 7.

---

**User**

The name of the files like "Phase6Contract.cs", "Phase6Endpoints.cs", "Phase6ApiEndpointsTests.cs" is not proper. We want business intentional names for these files because Phase 6 and all is the core to implementation logic and plan which does not make sense for using as actual file names. 

---

**Cursor**


Renaming Phase6-prefixed files to business-oriented names and updating references.




Renaming files and types to business-oriented names: `DocumentIngestionAndRankingContracts`, `DocumentIngestionAndRankingEndpoints`, and matching integration tests.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\44.txt:7-38 

---

**Cursor**


Your solution build completed successfully: **Release configuration, 0 warnings, 0 errors**, including `AI.Ranking.Engine.Api` and `AI.Ranking.Engine.IntegrationTests`.

The earlier renames are in place:

| Old | New |
|-----|-----|
| `Phase6Contracts.cs` | `DocumentIngestionAndRankingContracts.cs` |
| `Phase6Endpoints.cs` | `DocumentIngestionAndRankingEndpoints.cs` (`MapDocumentIngestionAndRankingEndpoints`) |
| `Phase6/Phase6ApiEndpointsTests.cs` | `DocumentIngestionAndRanking/DocumentIngestionAndRankingEndpointsTests.cs` |

`Program.cs` calls `MapDocumentIngestionAndRankingEndpoints()`. You can run tests locally when ready with `dotnet test` on the solution.

---

**User**

Okay so "Phase 6 — API surface (0.5–1 day)" implementation is complete, correct?

---

**Cursor**


Yes. For **Phase 6 — API surface** in `docs/IMPLEMENTATION-PLAN.md`, what the plan calls for is in place:

| Plan item | Status |
|-----------|--------|
| **1.** Minimal API: `POST /api/v1/documents/ingest`, `POST /api/v1/jobs/{id}/rank`, `GET /health` | Implemented (`DocumentIngestionAndRankingEndpoints.cs`, `Program.cs`). |
| **2.** Problem Details–style errors; configurable upload cap; PDF/DOCX | `AddProblemDetails` + `UseExceptionHandler` + `GlobalExceptionHandler`; ingest validated with `IngestRequest` / `IngestRequestValidator` and `Ingest:MaxUploadBytes` (default 200 KB in options); PDF/DOCX enforced by filename + `DocumentContentType`. |
| **3.** Swagger/OpenAPI | `AddSwaggerGen` / `UseSwagger` + `UseSwaggerUI` in Development (auth/rate limiting remain out of scope per the plan). |

Supporting pieces: integration tests under `DocumentIngestionAndRanking/`, and the solution builds cleanly (as in your last successful build).

**Caveat (honest):** “Configurable upload size” is enforced in **application validation** (declared length + bounded reads in parsers). If you later need the **host** to reject oversized multipart bodies before your handler runs, you’d add Kestrel/form limits in config—that’s an extra hardening step, not required to mark the plan’s Phase 6 slice as done for this repo.

