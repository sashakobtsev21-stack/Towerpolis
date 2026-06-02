# Towerpolis — Agent Orchestration

The complete agent system for this project: **who exists, who manages whom, how they talk, and how plugins/MCP wire in.** This is the source of truth for "how the agents work together." Phase-by-phase usage is in [`BUILD_PLAN.md`](BUILD_PLAN.md).

---

## 1. The org (3 layers)

```
                          ┌──────────────────────────────────────┐
   LAYER A — ORCHESTRATOR │  studio-orchestrator (opus)           │  master coordinator
                          │  ↳ project-coordinator (generic lead) │  routes everything, anti-drift,
                          └──────────────────┬───────────────────-┘  synthesizes results
                       ┌─────────────────────┴───────────────────────┐
        LAYER B — GAME STUDIO (15)                       LAYER C — SUPPORT SPECIALISTS (on-demand)
        game-director (opus) ─ vision/pillars/GDD        research/plan:  researcher, planner, goal-planner
          ├ game-designer ─ loop/economy/balance         spec/method:    specification, pseudocode, architecture,
          ├ unity-engine-architect (opus) ─ arch/ADR                     refinement, sparc-coordinator
          ├ gameplay-programmer ─ C#                      security:       security-auditor, security-manager
          ├ physics-programmer ─ the wobble              payments/IAP:   agentic-payments
          ├ rendering-engineer ─ URP/shaders             build/release:  devops-engineer, ci-cd, build-release-engineer*,
          ├ mobile-performance-engineer ─ profiling                      release-manager, pr-manager, repo-architect
          ├ build-release-engineer ─ AAB/CI/store        quality:        code-analyzer, analyze-code-quality,
          ├ 3d-artist ─ models                                           dependency-auditor, production-validator,
          ├ technical-artist ─ pipeline/look-dev                         tdd-london-swarm
          ├ character-animator ─ rig/Mecanim/IK          docs:           api-docs
          ├ vfx-artist ─ particles/juice                 prompts:        prompt-engineer (Midjourney/AI prompt refinement)
          ├ audio-designer ─ SFX/music/mix               telemetry:      observability-engineer, performance-monitor
          ├ ui-ux-designer ─ touch UI/HUD/menus          swarm helpers:  hierarchical-coordinator (anti-drift default),
          └ game-qa-engineer ─ playtest/tests/devices                    adaptive-coordinator, swarm-memory-manager
        (* build-release-engineer is the game-studio's release lead; the generic devops/ci-cd/release agents back it up.)
```

**109 agent files are connected** under [`.claude/agents/`](../.claude/agents) (the full useful roster from `my_agents`, minus the wrong-stack ones: flow-nexus/cloud, dual-mode/Codex, React-Native mobile). They are a **menu, not a standing team** — see anti-drift below.

## 2. Who manages whom
- **`studio-orchestrator`** (master) decomposes any project goal, picks the phase roster, sequences handoffs, enforces binding decisions, and synthesizes. It **defers creative/game calls to `game-director`** and routes cross-cutting work to the support specialists.
- **`game-director`** leads the 15-agent studio for everything *in* the game.
- **`unity-engine-architect`** owns engine architecture; its **ADRs in `docs/adr/` win** any tech dispute.
- Support specialists are pulled in **only when their phase/task needs them**, report back, and leave.

## 3. How they talk (comms protocol)
- **Named agents + `SendMessage`** — every spawned agent has a `name` and is told who to message next. Real-time coordination is messages, not polling.
- **Memory namespaces:** `collaboration` (shared context/decisions), `coordination` (swarm state), `tasks` (shared task list), `security` (audit findings). **Never write secrets to any namespace.**
- **Shared docs:** `docs/game/GDD.md` (≡ SPEC), `docs/game/pillars.md`, `docs/adr/*`. Agents read these first.

## 4. Anti-drift rules (the framework's own guidance — enforced)
- **Hierarchical topology, 6–8 active agents per task, specialized non-overlapping roles.** Smaller team = less drift. Do **not** activate all 109 at once.
- One owner per concern; conflicts resolve by: **ADR > GDD/pillars > game-director (creative) > orchestrator**.
- Test-first on `Towerpolis.Core`; commit only on green; respect BUILD_PLAN gates (esp. the Phase-2 "is it fun?" hard gate).

## 5. How Claude actually drives this
- **Default (Mode A):** Claude's **Workflow** tool fans the phase roster out deterministically (pipeline/parallel) and verifies adversarially (e.g. `game-qa-engineer` checks `gameplay-programmer`). When the session is rooted in this repo, agents resolve by `agentType`/`subagent_type` name; otherwise their persona text is inlined — same result.
- **Mode B (Ruflo CLI swarm):** `npx ruflo swarm init --topology hierarchical --maxAgents 8 --strategy specialized` + Claude's Task tool executes. Use for the cross-session memory/hooks/learning layer.
- **Session root matters:** to invoke these as named subagents, open Claude Code in `C:\Users\Oleksandr\Desktop\Towerpolis`.

## 6. MCP & plugins (orchestration backbone)
**MCP** ([`.claude/mcp.json`](../.claude/mcp.json)) wires the **claude-flow** server (provides `mcp__claude-flow__*` — swarm init, memory, task orchestration, the tools the coordinator agents reference) + **ruv-swarm** (extra topologies). Activate by opening Claude Code in this repo; verify with `npx claude-flow@v3alpha doctor`. flow-nexus (cloud) is optional.

**Plugins** — install the game-relevant Ruflo plugins once (see [`../scripts/setup-agents.ps1`](../scripts/setup-agents.ps1)):

| Plugin | Why for Towerpolis |
|---|---|
| `ruflo-core`, `ruflo-swarm` | core MCP tools + agent-team/swarm coordination, Monitor, worktrees — **the orchestration backbone** |
| `ruflo-sparc` | SPARC method (spec → pseudocode → architecture → refinement) for the Core lib |
| `ruflo-rag-memory`, `ruflo-agentdb` | persistent **semantic memory across sessions** (long project — agents remember decisions) |
| `ruflo-adr` | ADR lifecycle (matches `unity-engine-architect` writing `docs/adr/`) |
| `ruflo-testgen` | test-gap detection / coverage for `Towerpolis.Core` |
| `ruflo-docs` | doc generation + drift detection |
| `ruflo-observability`, `ruflo-cost-tracker` | logging/tracing + **token-cost tracking per agent** (manage the AI budget) |
| `ruflo-security-audit`, `ruflo-aidefence` | security review + PII/injection defense (auth/IAP) |
| `ruflo-browser` | Playwright — ASO/store research, competitor scraping, later UI smoke tests |
| `ruflo-loop-workers`, `ruflo-autopilot`, `ruflo-goals` | `/loop` automation + long-horizon planning for **live-ops cadence** |
| `ruflo-jujutsu`, `ruflo-ddd`, `ruflo-workflows` | git workflows · domain-driven scaffolding for Core · visual workflow automation |

*Not installed (off-domain):* neural-trader, market-data, iot-cognitum, ruvllm, federation, knowledge-graph, migrations, graph-intelligence — irrelevant to a casual game.

## 7. Activation checklist (one-time, human)
1. Open Claude Code rooted in `C:\Users\Oleksandr\Desktop\Towerpolis` (so the agents + `.claude/mcp.json` load).
2. Ensure **Node 20+**; run `npx claude-flow@v3alpha doctor` (set `ANTHROPIC_API_KEY` if asked).
3. Run [`scripts/setup-agents.ps1`](../scripts/setup-agents.ps1) to install the plugins above + `git lfs install`.
4. Confirm `studio-orchestrator`, `game-director`, etc. appear as subagents.
5. From then on: ask for a phase, the orchestrator runs the roster.
