# Towerpolis 🏙️

> **Towerpolis** — *Stack the city to the sky.* / *Строй город до небес.*
> A premium-feel casual **3D physics tower-stacker** for **Android (iOS later)**, built solo + Claude Code, using the **Ruflo** agent framework (`../agents/ruflo`).

A modern take on the *Tower Bloxx* core loop: a crane swings a building block, you tap to drop it, it must land cleanly on the floor below. Perfect drops keep the tower straight and parachute new residents in; sloppy drops make the tower **lean and physically wobble** — the taller it gets, the heavier and tenser placement becomes. Completed towers populate a persistent **3D city** you grow and show off.

**The wedge (what no competitor does):** a **daily-seed** stacker (everyone worldwide gets the same crane pattern each day — Wordle-for-stackers) **+** a persistent, shareable **"Build City"** metropolis.

---

## Status

| | |
|---|---|
| **Current phase** | Phase 3 — **Build City** meta (feature-complete; on-device gate pending) |
| **Engine** | Unity 6.3 LTS (URP, mobile) |
| **Core** | `Towerpolis.Core` — Unity-free C#, **305 NUnit tests green** (`dotnet test`) |
| **Platforms** | Android first → iOS later |
| **Languages** | RU + EN from launch (custom localization service, ADR-0008) |
| **Monetization** | F2P, monetization-ready (ads + IAP), switched **on after** retention is proven |
| **Working name** | `Towerpolis` (store form: *Towerpolis: Sky City* / *Towerpolis: Город в облаках*) |

### Phase progress
- **0 — Concept & GDD** ✅ · **1 — Architecture/scaffold** ✅ (Unity project, Core lib + dual test harness, CI)
- **2 — MVP core loop** ✅ (crane → drop → deterministic grade → weld → scripted wobble + juice; FUN gate passed)
- **3 — Build City meta** 🔨 districts (Downtown→Neon→Winter), daily seed, local leaderboards, coins/streak, district-complete screen, per-district music hooks. Remaining: on-device gate (`docs/PHASE3_GATE_CHECKLIST.md`).
- **4 — Progression** — Core engine done (upgrades, missions, achievements, login, streak-freeze); UI partially wired.
- **5 — UI/UX & Localization** — custom RU/EN Loc service + HUD/MetaHud localized; full pass pending.
- **6+ — Art & audio production, monetization, polish, soft-launch** — see [`docs/BUILD_PLAN.md`](docs/BUILD_PLAN.md).

## Build & run

**Unity (game):** open `unity/Towerpolis/` with **Unity 6.3 LTS** (Android Build Support). The scene self-bootstraps — pressing **Play** brings up the full loop + meta HUD (no manual scene wiring). Drop your own audio in `Assets/Audio/Resources/` ([`docs/AUDIO_GUIDE.md`](docs/AUDIO_GUIDE.md)) and art in `Assets/Art/Resources/` ([`docs/ASSETS_GUIDE.md`](docs/ASSETS_GUIDE.md), [`docs/BLENDER_GUIDE.md`](docs/BLENDER_GUIDE.md)).

**Core tests (no Unity / no license needed):**
```bash
dotnet test core/Towerpolis.Core.Tests/Towerpolis.Core.Tests.csproj
```
The same Core sources also run inside the Unity Test Runner (ADR-0005). **CI** ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) runs the Core tests on every push/PR; the Unity EditMode + Android AAB job activates automatically once `UNITY_LICENSE` secrets are set.

> **Determinism rule:** score/grade/seed/economy live in `Towerpolis.Core` and are **never** derived from PhysX (not cross-device deterministic → would break daily-seed fairness). Physics is active only while a block falls; then freeze + weld + scripted wobble. See [`CLAUDE.md`](CLAUDE.md) and ADR-0002.

## Repo layout

```
Towerpolis/
├─ unity/Towerpolis/            # the Unity 6.3 project
│  └─ Assets/
│     ├─ _Core/ (Runtime + Tests)   # Towerpolis.Core — deterministic, Unity-free, NUnit
│     └─ Game/                       # MonoBehaviours, UGUI, gameplay, meta, audio, vfx (+ PlayTests)
├─ core/                        # standalone projects mirroring _Core for `dotnet test`
├─ docs/                        # design, specs, ADRs, guides (source of truth)
├─ .claude/agents/             # 109 connected subagents + studio-orchestrator
├─ .github/workflows/ci.yml    # GameCI
└─ CLAUDE.md                   # engineering conventions + how this project drives the agents
```

## Documents (source of truth)

- [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md) — **living status: what's done, what's left, history & plan** (start here) · [`docs/status.html`](docs/status.html) — the same as a standalone web page (overview + all commits)
- [`docs/game/GDD.md`](docs/game/GDD.md) — Game Design Document · [`docs/game/pillars.md`](docs/game/pillars.md) — the 3 design pillars
- [`docs/game/mvp-core-loop-spec.md`](docs/game/mvp-core-loop-spec.md) · [`docs/game/meta-spec.md`](docs/game/meta-spec.md) · [`docs/game/progression-spec.md`](docs/game/progression-spec.md) — phase specs
- [`docs/BUILD_PLAN.md`](docs/BUILD_PLAN.md) — phased build plan + the 15-agent studio per phase
- [`docs/MARKET_ANALYSIS.md`](docs/MARKET_ANALYSIS.md) — market, competitors, naming, sources
- [`docs/ART_BIBLE.md`](docs/ART_BIBLE.md) · [`docs/MIDJOURNEY_DIRECTIONS.md`](docs/MIDJOURNEY_DIRECTIONS.md) · [`docs/BLENDER_GUIDE.md`](docs/BLENDER_GUIDE.md) · [`docs/ASSETS_GUIDE.md`](docs/ASSETS_GUIDE.md) · [`docs/AUDIO_GUIDE.md`](docs/AUDIO_GUIDE.md) — art & audio pipeline
- [`docs/PHASE3_GATE_CHECKLIST.md`](docs/PHASE3_GATE_CHECKLIST.md) — on-device acceptance pass for Phase 3
- [`docs/AGENT_ORCHESTRATION.md`](docs/AGENT_ORCHESTRATION.md) — the full agent org, comms, MCP/plugins
- [`docs/QUESTIONS.md`](docs/QUESTIONS.md) — discovery Q&A (locked decisions) · [`docs/adr/`](docs/adr) — architecture decision records
- [`CLAUDE.md`](CLAUDE.md) — engineering conventions + how this project drives the agents

## Tech stack

Unity 6.3 LTS · URP mobile · **2-phase physics** (Rigidbody only while a block falls → freeze + weld → *scripted* wobble; ADR-0002) · deterministic **scoring in a Unity-free Core C# lib** (NUnit, dual harness ADR-0005) · **custom localization service** (RU+EN, ADR-0008) · **PrimeTween** (ADR-0006) · Addressables for content (ADR-0004).

*Planned (wired dormant / later phases):* Google Play Games Services (leaderboards/cloud save, ADR-0009) · Firebase Remote Config (daily seed / live-ops) · GameAnalytics · AdMob + IAP (off at launch behind a remote flag) · GameCI Android AAB.

## License

Proprietary — all rights reserved (placeholder; choose a license before any public release).
