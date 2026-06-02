# Towerpolis — Build Plan & Agent Orchestration

How we build this solo + Claude Code, using the **Ruflo / Claude Flow** agent framework at `C:\Users\Oleksandr\Desktop\agents\ruflo` (remote: `github.com/sashakobtsev21-stack/my_agents`).

---

## 0. How the agents actually run (read this first)

Ruflo ships **134 agent personas** (`.agents/skills/*/SKILL.md`) + ~33 plugins. **Important reality:** these personas are **TypeScript/web/backend-tuned — there are NO Unity / C# / 3D / game-design / game-art agents** in the framework. Two consequences:

1. **Claude Code writes the Unity C# directly.** The agents are used for *research, specification, architecture, test design, review, CI/CD, ASO, release* — the surrounding engineering — while Claude Code itself plays the "coder" role for Unity.
2. **`agent-app-store` is the Flow-Nexus internal marketplace, not real Google Play/App Store ASO**, and `agent-spec-mobile-react-native` is **React Native, not Unity**. For real ASO/store work we use **Claude Code + WebSearch** (as we did for the naming/market analysis).

### Two ways to drive the agents

| Mode | How | When |
|---|---|---|
| **A. Claude Code plays the roles** (recommended, immediate) | Claude's **Workflow** / **Agent** tool spawns sub-agents that play `researcher / planner / architect / coder / tester / reviewer` and return structured results. No setup. *(This is how the market analysis was produced.)* | Default for every phase. |
| **B. Ruflo CLI swarm** | `npx ruflo` (a.k.a. `npx claude-flow@v3alpha`) — `swarm init` (hierarchical, 6–8 agents, raft consensus) + Claude Code's Task tool executes. Needs Node 20+ and API keys. | When you want the Ruflo memory/hooks/cross-session learning layer, or to run a long background swarm. |

**Per-task routing (Ruflo's own anti-drift table):** Feature → `coordinator, architect, coder, tester, reviewer` · Bugfix → `coordinator, researcher, coder, tester` · Refactor → `architect, coder, reviewer` · Performance → `perf-engineer, coder` · Security → `security-architect, auditor`. Keep swarms at **6–8 agents, hierarchical, specialized roles, frequent checkpoints, test-first.**

### Standing engineering discipline (applies every phase)
- **Test-first on the Core lib.** The deterministic game logic (grading, seed, scoring, city population) lives in a **Unity-free C# `Towerpolis.Core` assembly** with NUnit tests that run standalone via `dotnet test` *and* in Unity Test Runner. Commit only when green. (Proven pattern from the earlier AlchemyDeckbuilder project.)
- **Keep files < 500 lines, typed public APIs, validate at boundaries.**
- **Commit per green milestone.** Branch off `main`; never commit secrets.

---

## 1. Phase map (gate-driven)

> Each phase lists: **goal → Ruflo agents (role) → deliverable → gate**. Phases are sequential but research/spec for phase N+1 can overlap phase N.

### Phase 0 — Concept & GDD  ✅ DONE
- **Agents:** `researcher` ×6 (market, monetization, tech, art, naming, retention) → `planner`/producer synthesis.
- **Deliverable:** this repo — `GDD.md`, `MARKET_ANALYSIS.md`, `ART_BIBLE.md`, `BUILD_PLAN.md`, name = **Towerpolis**.
- **Gate:** ✅ direction locked.

### Phase 1 — Architecture & Project Scaffold
- **Goal:** Unity 6.3 project + `Towerpolis.Core` (Unity-free) + asmdefs + Git/LFS + GameCI + folder structure.
- **Agents:** `system-architect` (assembly/dependency design), `repo-architect` (layout), `cicd-engineer` (`agent-ops-cicd-github` → GameCI workflow), `coder` (Claude, scaffold), `tester` (NUnit harness).
- **Deliverable:** compiles clean in Unity; `dotnet test` runs the Core suite; CI green skeleton.
- **Gate:** project opens in Unity, Console clean, sample Core test passes.

### Phase 2 — MVP Core Loop (the vertical slice that proves "is it FUN?")
- **Goal:** crane → tap → drop (Rigidbody) → **deterministic grade** → freeze+weld → **scripted wobble** → full juice. One Blender block set + one MJ background. Local high score + instant restart.
- **Agents:** `coder` (Claude — Unity gameplay), `tester` / `tdd-london-swarm` (Core grading + seed tests), `production-validator` (does it run on device?), `reviewer`.
- **Deliverable:** playable slice on an Android device.
- **Gate (HARD):** internal feel-test = people want "one more try"; wobble reads tense-but-fair. **Do not proceed until yes.**

### Phase 3 — Meta: Build City + Daily Seed + Leaderboards
- **Goal:** persistent 3D city grid (towers deposit, population grows, residents parachute), daily seed (deterministic, server-driven), leaderboards (friends + weekly-reset + per-district) via Google Play Games Services.
- **Agents:** `architect`, `coder` (Claude), `backend-dev` (GPGS + Firebase Remote Config wiring), `tester`, `reviewer`.
- **Gate:** daily seed identical across two devices; leaderboard submits/reads; city persists across sessions.

### Phase 4 — Progression, Upgrades, Streaks, Missions
- **Goal:** crane upgrades, cosmetics economy (currencies), streaks + daily login + free progression track, weekly missions, achievements.
- **Agents:** `coder` (Claude), `architect` (economy/data model via ScriptableObjects), `tester`, `reviewer`.
- **Gate:** economy balances in playtest; progression saves/loads; no exploit in currency math (Core-tested).

### Phase 5 — UI/UX, Menus, Settings, Localization (RU + EN)
- **Goal:** main menu, settings (sound/music/lang/haptics/quality/privacy), HUD, shop UI, end-screen + share card. Unity Localization string tables RU+EN, zero hardcoded strings.
- **Agents:** `coder` (Claude — UGUI), `reviewer` (UX pass), `researcher` (localization QA for RU strings).
- **Gate:** full RU/EN switch with no missing keys; all screens reachable; settings persist.

### Phase 6 — Art & Audio Integration
- **Goal:** wire final assets per `ART_BIBLE.md` — Synty kit (stripped), Blender blocks (recolored), Midjourney 2D (icon/UI/skybox/store), DOTween juice polish, audio (CC0-first).
- **Agents:** Claude wires assets + animation; `performance-optimizer` watches draw calls/GC during import. **You** generate MJ art + source/buy Synty + model blocks.
- **Gate:** one coherent palette (art-bible recolor pass enforced); <200 draw calls; 60 fps on a mid Android.

### Phase 7 — Monetization (build dormant → turn on later)
- **Goal:** AdMob (rewarded + light interstitial + optional banner) with reward callbacks, IAP catalog (remove-ads, cosmetics, gems, battle pass), consent/ATT/GDPR. **Switched OFF at launch.**
- **Agents:** `agent-payments` / `agent-agentic-payments` (IAP/economy design + integration plan), `coder` (Claude — SDK wiring), `security-manager` (consent/data-safety), `tester`.
- **Gate:** rewarded test ad serves in sandbox; IAP sandbox purchase + restore works; everything behind a remote kill-switch (default off).

### Phase 8 — Polish, Performance, QA, Security
- **Goal:** hit perf budget, fix Android Vitals (crash/ANR), edge-case QA, security pass on save/leaderboard/IAP.
- **Agents:** `performance-optimizer` + `performance-benchmarker`, `tester` / `production-validator`, `security-audit` / `security-manager`, `code-review-swarm`.
- **Gate:** clean Vitals; no P0/P1 bugs; profiler within budget; security findings closed.

### Phase 9 — ASO, Store Setup, Soft-Launch, Featuring
- **Goal:** Play Console + App Store listings (RU+EN), icon/feature-graphic/screenshots from MJ, store CVR optimization, soft-launch in 1–2 cheap tier-2 markets, **submit Apple featuring nomination (3 months out) + Google featuring form + Indie Corner/Level Up.**
- **Agents:** `researcher` (live ASO keyword/competitor pass), `release-manager` + `cicd-engineer` (signed AAB pipeline, fastlane), Claude (listing copy RU+EN).
- **Gate:** soft-launch **D1 ≥ 30% / D7 ≥ 12%** before any global push.

### Phase 10 — Live-Ops (ongoing, solo-sustainable)
- **Goal:** templatized cadence — **daily** seed (automated), **weekly** leaderboard reset + rotating missions, **monthly** themed district/skin (recolor + prop swap). Server-driven via Remote Config; ≥2 content beats in first 90 days.
- **Agents:** `coder` (Claude) for the seasonal art *system* + config; `researcher` for retention-data response.
- **Cadence:** ~30–35 hrs/week (60+ → burnout by ~month 9). Use Claude's `/loop` + `schedule` for recurring tasks.

---

## 2. Concrete kickoff for Phase 1 (commands & shape)

```
Towerpolis/                      # this repo
├─ docs/                         # ✅ GDD, MARKET_ANALYSIS, BUILD_PLAN, ART_BIBLE
├─ unity/Towerpolis/             # the Unity 6.3 project (generated on first open)
│  └─ Assets/
│     ├─ _Core/                  # Towerpolis.Core (asmdef, noEngineReferences) — pure C#
│     ├─ _Core.Tests/            # NUnit EditMode tests (asmdef)
│     ├─ Game/                   # MonoBehaviours: Crane, Block, Tower, Camera, Juice
│     ├─ Data/                   # ScriptableObjects (BlockDef, CraneUpgradeDef, DistrictDef)
│     ├─ UI/                     # UGUI + Localization tables
│     ├─ Art/ Audio/             # LFS-tracked assets
│     └─ Plugins/                # DOTween, GPGS, AdMob, etc.
├─ .github/workflows/ci.yml      # GameCI: build Android + run Core tests
└─ CLAUDE.md .gitignore .gitattributes README.md
```

**Phase-1 agent run (Mode A example):** spawn `system-architect` → defines the Core ↔ Data ↔ Game ↔ UI assembly graph + the grading/seed interfaces; `repo-architect` → confirms layout; `cicd-engineer` → writes `ci.yml`; Claude scaffolds the Core lib + a first NUnit test for the placement-grading function; `tester` reviews coverage. Then: `git lfs install`, open in Unity, install DOTween + Input System, run Test Runner.

**Remote git:** create a **new private repo** for Towerpolis (separate from `my_agents`) and `git remote add origin …`; keep `my_agents` only as the agent toolchain source.

---

## 3. Your (human) homework, by phase

- **Now:** install/confirm Unity Hub + **Unity 6.3 LTS** (Android Build Support + later iOS); confirm .NET SDK (`dotnet`) for standalone Core tests; `git lfs install`.
- **Phase 2:** Android device + USB debugging for on-device feel-tests.
- **Phase 6:** Midjourney subscription; buy/sub one **Synty** pack (POLYGON MINI City) or grab the free Starter pack; install Blender (model the blocks).
- **Phase 7:** AdMob + Google Play Console account; decide the **publishing entity / payout route** (RU payout constraints — resolve early).
- **Phase 9:** Apple Developer + App Store Connect (for the featuring nomination 3 months ahead); ASO assets.
