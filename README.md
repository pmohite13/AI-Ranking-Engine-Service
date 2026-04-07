# AI-Ranking-Engine-Service

AI-native candidate–job ranking service (thin-slice, production-minded): document ingestion → parsing → structured extraction → embeddings → vector recall → hybrid re-ranking. See `docs/IMPLEMENTATION-PLAN.md` and `docs/Known-Architectural-Decisions.md`.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (LTS)
- Optional: OpenAI API key (required once you wire embeddings and LLM stages; Phase 0 does not call external APIs)

## Build and test

From the repository root:

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
```

**Note:** `dotnet build` runs restore automatically when needed. Using `dotnet build --no-restore` only works after a successful restore has produced `obj/project.assets.json` for each project. If you see **NETSDK1004** (“Assets file … not found”), run `dotnet restore` first, or omit `--no-restore`.

## Run the API (Phase 0)

```bash
dotnet run --project src/AI.Ranking.Engine.Api -c Release
```

- Health check: `GET https://localhost:<port>/health` (see `launchSettings.json` for HTTP/HTTPS ports)
- Swagger UI (Development): `/swagger`

## Configuration and secrets

Secrets must **not** be committed.

1. Copy `.env.example` to `.env` in the repo root (`.env` is gitignored).
2. Set variables in `.env` for local tooling that reads it, **or** set the same names as **environment variables** or **user secrets** for the API process.

Planned variables (full list will grow with later phases):

| Variable | Purpose |
|----------|---------|
| `OPENAI_API_KEY` | OpenAI API key for embeddings and structured extraction (when implemented) |

For ASP.NET Core user secrets (development):

```bash
dotnet user-secrets init --project src/AI.Ranking.Engine.Api
dotnet user-secrets set "OpenAI:ApiKey" "<your-key>" --project src/AI.Ranking.Engine.Api
```

Align secret keys with `appsettings` / options binding as features land.

## Repository layout

| Path | Role |
|------|------|
| `src/AI.Ranking.Engine.Domain` | Domain model and pure logic |
| `src/AI.Ranking.Engine.Application` | Use cases, interfaces, validators |
| `src/AI.Ranking.Engine.Infrastructure` | OpenAI, parsers, cache, Polly, etc. |
| `src/AI.Ranking.Engine.Api` | Minimal API host |
| `tests/AI.Ranking.Engine.UnitTests` | Unit tests |
| `tests/AI.Ranking.Engine.IntegrationTests` | Integration tests |
| `docs/adr/` | Architecture Decision Records |

Shared MSBuild settings: `Directory.Build.props`. Editor/analyzer baseline: `.editorconfig`.

## Documentation

- `docs/IMPLEMENTATION-PLAN.md` — phased implementation plan
- `docs/Known-Architectural-Decisions.md` — intentional trade-offs for scale-up
- `docs/adr/` — ADRs (see `0001-record-architecture-decisions.md` for the process)

## License

Specify your license here when applicable.
