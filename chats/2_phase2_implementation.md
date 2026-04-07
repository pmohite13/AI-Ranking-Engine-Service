# Project phase 2 implementation planning
_Exported on 4/7/2026 at 12:19:41 GMT+5:30 from Cursor (3.0.12)_

---

**User**

I want you to please thoroughly read, study and analyse docs/IMPLEMENTATION-PLAN.md, docs/Known-Architectural-Decisions.md, docs/raw_requirement.txt and entire project so that you will get overall understanding about the project structure, requirement, dependencies and data/process flow.


As per "docs/IMPLEMENTATION-PLAN.md" document we have implemented two phases so far "Phase 0 — Repository and governance (0.5–1 day)", "Phase 1 — Domain and application contracts (0.5–1 day)" including all or some modules/features/tasks/goals implementation mentioned in those respective phase/s. And now we need to start with next phase "Phase 2 — Document parsing (1 day)". Lets begin with all modules/features/tasks/goals "1.", "2.", "3.", "4.", "5." as they are all inter-related.


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


Implementing Phase 2 (Document parsing). Reading the documentation and exploring the project structure.




Implementing parsers, factory, text normalization, and tests. Checking Domain csproj and fetching a minimal PDF fixture.




Implementing Phase 2: adding domain text normalization, infrastructure parsers, factory, DI, and tests. Checking PdfPig package name and OpenXml text extraction patterns.




Implementing code: creating domain and infrastructure files and wiring DI.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\13.txt:7-50 

---

**Cursor**


Diagnosing the restore failure: checking the Infrastructure project file and aligning TargetFramework and PdfPig package versions.




Checking available UglyToad.PdfPig versions and verifying restore.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\14.txt:7-28 

---

**Cursor**


Checking NuGet for UglyToad.PdfPig versions and dependency compatibility.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\14.txt:7-15 

---

**Cursor**


Checking the Infrastructure project and switching to the `PdfPig` package (0.1.14 on NuGet) — `UglyToad.PdfPig` 0.1.9-alpha001-patch1 has missing satellite packages, causing NU1603.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\14.txt:7-49 

---

**Cursor**


Removing the duplicate `UglyToad.PdfPig` reference — `dotnet add PdfPig` added the new package but left the old one, causing NU1603.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\15.txt:7-12 

---

**Cursor**


Restore is succeeding now.

**What was wrong:** The project listed **both** `PdfPig` 0.1.14 and `UglyToad.PdfPig` 0.1.9-alpha001-patch1. The old alpha pulled in satellite packages (`UglyToad.PdfPig.Core`, etc.) that are not on NuGet at matching versions, which triggered **NU1603** (and with **TreatWarningsAsErrors**, those became hard errors).

**What fixed it:** Keeping only **`PdfPig` 0.1.14** (the maintained package on nuget.org) and removing **`UglyToad.PdfPig`**.

In code you still use the **`UglyToad.PdfPig`** API (e.g. `PdfDocument.Open`); the `PdfPig` package is the same library, just the correct package id for restore. No further action is required for restore unless you add conflicting references again.

---

**User**

Okay since now restoration got fixed and as we are already in implementation phase for "Phase 2 — Document parsing (1 day)" we can continue with it if not yet completely finished.

---

**Cursor**


Auditing the codebase for Phase 2 and completing any missing pieces.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\17.txt:7-18 

---

**Cursor**


Checking Phase 2 implementation status and completing any remaining work.




Implementing Phase 2: Infrastructure lacked source files. Adding parsers, bounded stream handling, factory, DI, tests, and wiring the API.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\18.txt:7-14 

---

**Cursor**


Verifying Phase 2 completion: checking API wiring and unit tests.




Solution build succeeded and the API registers infrastructure. Adding Phase 2 unit tests and the test project references we planned.




Fixing tests: replacing `TestContext.Current` (xUnit v3) with `CancellationToken.None`, and verifying `TextNormalizer` expectations.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\18.txt:7-55 

---

**Cursor**


Replacing `TestContext.Current.CancellationToken` — it's xUnit v3-only; the project uses xUnit 2.x. Using `CancellationToken.None` (or `default`) instead.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\19.txt:7-14 

---

**Cursor**


The unit test project builds cleanly now.

**Cause:** `TestContext.Current.CancellationToken` is from **xUnit v3**. This repo uses **xUnit 2.5.x**, which does not define `TestContext`, so the compiler failed.

**Change:** Use `CancellationToken.None` for those async parser calls (fine for these unit tests).

You can run `dotnet test` on the unit test project whenever you want to execute the Phase 2 tests.

---

**User**

So "Phase 2 — Document parsing (1 day)" implementation is complete, right?

---

**Cursor**


Yes. **Phase 2 — Document parsing** is implemented as described in `docs/IMPLEMENTATION-PLAN.md` for that phase.

| Plan item | Status |
|-----------|--------|
| **`IDocumentParser`** + **`PdfDocumentParser`**, **`DocxDocumentParser`**, plain-text parser ( **`PlainTextDocumentParser`** — same role as “PlainTextParser”) | Done (`Infrastructure/Parsing/`) |
| **Factory** by content type | **`IDocumentParserFactory`** + **`DocumentParserFactory`** |
| **Single shared normalizer** (Unicode, line endings, whitespace) | **`TextNormalizer`** in Domain |
| **Unit tests** + fixtures | **`tests/.../Phase2/`** + **`Fixtures/hello.pdf`**, **`hello.docx`** |
| **DI registration** | **`AddRankingEngineInfrastructure()`** in **`Program.cs`** |
| **Files split** (no giant single file) | Parsers / IO / DI in separate files |

**Note:** Choosing the NuGet package **`PdfPig`** (v0.1.14) instead of the broken **`UglyToad.PdfPig`** id matches the plan’s “PdfPig + Open XML” stack; the API is still **`UglyToad.PdfPig`** in code.

**Not part of Phase 2** (later phases): HTTP ingest that maps file extension → `DocumentContentType` and calls the factory (Phase 6 API). The parsing layer is ready for that.

**Optional:** A short **`docs/adr/0003-…md`** on PDF/DOCX libraries + NuGet package id is not required by the Phase 2 bullet list but fits your ADR process if you want it documented.

