# Project implementation plan and best practices
_Exported on 4/7/2026 at 11:48:58 GMT+5:30 from Cursor (3.0.12)_

---

**User**

I want you to please thoroughly read, study and analyse docs/IMPLEMENTATION-PLAN.md, docs/Known-Architectural-Decisions.md, docs/raw_requirement.txt so that you will get overall understanding about the project structure, requirement, dependencies and data/process flow.

Okay as mentioned in "docs/IMPLEMENTATION-PLAN.md" document lets start implementation for "Phase 0 — Repository and governance (0.5–1 day)". Lets begin with modules/features/tasks/steps/goals/phases "1.", "2.", "3.", "4.", and all their respective sub-tasks as they are inter-related. 

Read, study and analyse "docs/IMPLEMENTATION-PLAN.md" document to get better context.

See we dont want any workaround or patch-work. We need to have a standard, permanent, scalable, robust, modular approach. 

We want to have highest performance possible and lowest unnecessary usage of memory or bloating memory. We want to follow all best practices possible to keep our app highly performant, secure, light and robust. 

If needed then please also check what or how other people across the industry around the globe is doing in this type of scenario.

If needed check if it is aligned with official documentation and latest best and standard practices.

Create tests and make sure everything is working properly as per the updates. Dont run test, I will run manually later.

If you want anytime to do research online, please feel free to do it.

Dont overcrowd any single file, use separate files if the count of lines of code is more than 500. This helps us to manage the code and give us clarity and stop creating mamoth lines of code in single file, bloating the file.

We dont need any naive solution. Our project is big and we dont want to duplicate any code, we want to leverage as much as we can the current code infrastructure. We want to make sure we always use highly scalable, robust, modular, performant, secure approach and solution.

---

**Cursor**


Reading the referenced docs and exploring the repository to implement Phase 0 (tasks 1–4).




Checking the repo for existing solution files and `docs/adr`:




Scaffolding the .NET 8 solution, governance files, ADRs, README, and smoke tests. No existing code — creating the full Phase 0 structure.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\2.txt:7-8 

---

**Cursor**


Confirming .NET SDK 8.0.405. Checking the repository and completing any remaining Phase 0 work.




Implementing Phase 0: scaffolding the .NET 8 solution (repo still lacks code). Creating the solution structure, governance files, ADRs, and a smoke test.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\3.txt:7-26 

---

**Cursor**


Wire up the solution, add project references, governance files, ADRs, README, and tests. Checking the repository state.




Adding projects to the solution, wiring references, trimming the API to a health endpoint, and completing Phase 0 artifacts (ADRs, README, tests).



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\4.txt:7-18 

---

**Cursor**


Completing Phase 0: adding project references, cleaning the API entry point, ADRs, README, `.env.example`, and tests.




Wiring project references, slimming duplicate `csproj` settings (using `Directory.Build.props`), replacing template code with a health endpoint and Phase 0 tests, and adding ADRs plus README.



---

**User**

@c:\Users\Pranay Mohite\.cursor\projects\e-Repos-cursor-ai-public-projects-AI-Ranking-Engine-Service\terminals\5.txt:7-115 

---

**Cursor**


First build failed because `--no-restore` ran before any `project.assets.json` existed; restore then build succeeded. Documenting the correct workflow and checking for missing Phase 0 artifacts (README, ADRs, `.env.example`).




Adding README (including restore/build order), ADRs, and `.env.example` to complete Phase 0 documentation.




Here is what that terminal output means and what was added.

### Why the first build failed
`dotnet build ... --no-restore` runs **without** restoring packages. On a fresh clone (or before any restore), there is no `obj/project.assets.json`, so you get **NETSDK1004**. That is expected.

**Fix:** run `dotnet restore` first, or use plain `dotnet build` (restore runs automatically when needed). After your `dotnet restore`, the **Release** build completed successfully.

### What I added so this is documented
- **`README.md`** — prerequisites, **`dotnet restore` → `dotnet build` → `dotnet test`**, explicit note that **`--no-restore` requires a prior restore**, run instructions, secrets (`.env` / env vars / user secrets), repo layout.
- **`.env.example`** — template for `OPENAI_API_KEY` (for later phases).
- **`docs/adr/0001-record-architecture-decisions.md`** — meta-ADR (process and template).
- **`docs/adr/0002-hybrid-ranking-baseline.md`** — hybrid ranking baseline (per Phase 0).
- **`docs/Known-Architectural-Decisions.md`** — links to those two ADRs under “Related documents”.

Your successful Release build confirms the solution graph is wired; you can run tests locally when ready with `dotnet test -c Release`.

