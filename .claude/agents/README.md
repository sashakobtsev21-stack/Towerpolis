# Towerpolis subagents

`game-dev/` holds the **15-agent Unity game-dev studio** copied from the `my_agents` framework
(`C:\Users\Oleksandr\Desktop\agents\ruflo\.claude\agents\game-dev`). They are first-class Claude Code
subagents — invocable as `subagent_type` (Agent tool) / `agentType` (Workflow) when a session is rooted
in this repo. See [`../../docs/BUILD_PLAN.md`](../../docs/BUILD_PLAN.md) §0 for the hierarchy and per-phase usage.

| Tier | Agent | Model | Owns |
|---|---|---|---|
| 0 | `game-director` | opus | vision, pillars, GDD, scope, cross-discipline coherence |
| 1 | `game-designer` | sonnet | core loop, mechanics, progression, economy, balance, levels |
| 1 | `unity-engine-architect` | opus | project structure, URP, asmdef, Addressables, perf budgets, ADRs |
| 2 | `gameplay-programmer` | sonnet | Unity C# mechanics, state, save/load, input |
| 2 | `physics-programmer` | sonnet | simulation, joints, colliders, the tower wobble, stability |
| 2 | `rendering-engineer` | sonnet | URP config, shaders, lighting, post, GPU perf |
| 2 | `mobile-performance-engineer` | sonnet | on-device profiling/optimization, frame/mem/thermal budget |
| 2 | `build-release-engineer` | sonnet | Unity build pipeline, AAB, signing, CI, store submission |
| 2 | `3d-artist` | sonnet | game-ready models, UVs, PBR, LODs within budget |
| 2 | `technical-artist` | sonnet | art↔engine bridge, standards, look-dev, asset optimization |
| 2 | `character-animator` | sonnet | rigging, Mecanim, blend trees, IK, procedural motion |
| 2 | `vfx-artist` | sonnet | particles / effect shaders / game-feel juice |
| 2 | `audio-designer` | sonnet | SFX, adaptive music, mix, middleware |
| 2 | `ui-ux-designer` | sonnet | touch controls, HUD, menus, responsive layout |
| 2 | `game-qa-engineer` | sonnet | playtesting, Unity Test Framework, device matrix, bug repro |

**Conventions the team relies on:** GDD at `docs/game/GDD.md`, pillars at `docs/game/pillars.md`,
architecture decisions as ADRs in `docs/adr/`, shared state in the `collaboration` memory namespace.
Updating an agent here does **not** change the source in `my_agents` (these are copies).
