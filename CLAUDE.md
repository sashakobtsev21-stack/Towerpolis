# Towerpolis — Project Instructions for Claude Code

A premium-feel casual **3D physics tower-stacker** (Unity 6.3, Android-first → iOS later). Full design in [`docs/game/GDD.md`](docs/game/GDD.md); pillars in [`docs/game/pillars.md`](docs/game/pillars.md); build plan + agent orchestration in [`docs/BUILD_PLAN.md`](docs/BUILD_PLAN.md); art/audio in [`docs/ART_BIBLE.md`](docs/ART_BIBLE.md); decisions in [`docs/adr/`](docs/adr).

## Agent studio & orchestration
**109 agents** from `my_agents` are connected under [`.claude/agents/`](.claude/agents): the **15-agent Unity game-dev studio** (`game-dev/`) + the support specialists (research, security, payments, devops/release, docs, quality, prompts, telemetry, swarm coordinators). Full map: **[`docs/AGENT_ORCHESTRATION.md`](docs/AGENT_ORCHESTRATION.md)**.

- **Master coordinator = `studio-orchestrator`** (opus) — decomposes any goal, picks the phase roster, sequences handoffs, enforces binding decisions, synthesizes. It **defers creative/game calls to `game-director`** and routes cross-cutting work to support specialists.
- **3-tier game studio:** `game-director` → `game-designer` + `unity-engine-architect` → engineering/art/QA implementers.
- **Anti-drift (enforced):** hierarchical, **6–8 active agents per task**, specialized roles. The 109 are a *menu, not a standing team* — pull only the phase's roster (see `BUILD_PLAN.md` §1).
- **Comms:** named agents + `SendMessage`; namespaces `collaboration`/`coordination`/`tasks`/`security`; **never write secrets to a namespace.** Conflicts resolve: **ADR (`docs/adr/`) > GDD/pillars > game-director (creative) > orchestrator.**
- **Invoke** as `subagent_type` (Agent) / `agentType` (Workflow) when this repo is the session root, or via the `npx ruflo` swarm. **Claude Code still writes the Unity C# itself** — agents are the roles/lenses (designer specifies → gameplay-programmer implements → game-qa-engineer verifies).
- **MCP/plugins:** [`.claude/mcp.json`](.claude/mcp.json) wires claude-flow + ruv-swarm; run [`scripts/setup-agents.ps1`](scripts/setup-agents.ps1) once to install the game-relevant Ruflo plugins. Activation needs the session rooted here + Node 20+.

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
