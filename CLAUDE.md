# Towerpolis — Project Instructions for Claude Code

A premium-feel casual **3D physics tower-stacker** (Unity 6.3, Android-first → iOS later). Full design in [`docs/GDD.md`](docs/GDD.md); build plan + agent orchestration in [`docs/BUILD_PLAN.md`](docs/BUILD_PLAN.md); art/audio in [`docs/ART_BIBLE.md`](docs/ART_BIBLE.md).

## Agent framework
The **Ruflo / Claude Flow** toolchain lives at `C:\Users\Oleksandr\Desktop\agents\ruflo` (remote `github.com/sashakobtsev21-stack/my_agents`). Use its personas (researcher / planner / system-architect / coder / tester / reviewer / cicd-engineer / performance-optimizer / payments / release-manager) via Claude's **Workflow/Agent** tools (Mode A) or the `npx ruflo` swarm (Mode B). **There are no Unity/game agents in Ruflo — Claude writes the Unity C# directly; agents do research/spec/architecture/tests/CI/ASO/review.** See `BUILD_PLAN.md` §0.

## Architecture rules
- **Deterministic game logic → `Towerpolis.Core`**, a Unity-free C# assembly (`noEngineReferences: true`) with **NUnit** tests that run via `dotnet test` standalone AND in Unity Test Runner. Grading, daily seed, scoring, city population, economy math live here.
- **Score/grade are NEVER derived from PhysX** (not cross-device deterministic → cheating + breaks daily-seed fairness). Physics is active only while a block is falling; then freeze + weld + **scripted wobble**.
- Unity layer (MonoBehaviours, UGUI, ScriptableObjects) consumes the Core lib. Keep files < 500 lines; typed public APIs; validate at boundaries.
- **No hardcoded user-facing strings** — Unity Localization (RU + EN) from Phase 5 on.

## Workflow discipline
- **Test-first on Core:** write/adjust NUnit tests with every Core change; run `dotnet test`; commit only when green.
- **Gate-driven phases** (see BUILD_PLAN). The Phase-2 MVP gate ("is the core FUN?") is HARD — do not build meta until it passes.
- **Perf budget (non-negotiable):** URP mobile, < 200 draw calls, SRP Batcher on, pool everything, shared materials, no GC spikes. Weak Android Vitals = auto-exclusion from Play featuring.
- **Monetization stays OFF at launch** — wire hooks, flip on only after D1 ≥ 30% / D7 ≥ 12% / rating ≥ 4.0.

## Repo / git
- This is a **standalone repo**, separate from `my_agents`. Create its own private remote before pushing.
- `git lfs install` before adding art/audio (LFS patterns in `.gitattributes`).
- Never commit secrets (`.env`, keystores, `google-services.json`). Branch off `main`; commit per green milestone.

## File organization
- `/docs` design & analysis · `/unity/Towerpolis` the Unity project · Core under `Assets/_Core` (+ `_Core.Tests`) · `/.github/workflows` CI.
- Don't dump working files in the repo root.
