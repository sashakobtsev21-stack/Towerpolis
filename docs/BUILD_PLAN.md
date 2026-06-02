# Towerpolis — Build Plan & Agent Orchestration

How we build this solo + Claude Code, driving the **15-agent Unity game-dev studio** sourced from `my_agents` (`C:\Users\Oleksandr\Desktop\agents\ruflo\.claude\agents\game-dev`), **copied into this repo at [`.claude/agents/game-dev/`](../.claude/agents/game-dev)** so they're first-class subagents here.

---

## 0. The agent studio (read this first)

The game team is a **3-tier hierarchy** with clear ownership and model tiers:

```
                      game-director (opus)  ── owns vision, pillars, GDD, scope, coherence
                       /                 \
        game-designer (sonnet)      unity-engine-architect (opus)
        core loop / mechanics /     project structure / URP / asmdef /
        progression / economy /     Addressables / perf budgets / ADRs
        balance / levels
                       |                       |
   ┌───────────────────┴───────────┬───────────┴───────────────────────────┐
   ENGINEERING                     ART                                      QA
   gameplay-programmer (C#)        3d-artist (models/UVs/PBR)               game-qa-engineer
   physics-programmer (sim/wobble) technical-artist (art↔engine, look-dev)  (playtest, Unity Test
   rendering-engineer (URP/shade)  character-animator (rig/Mecanim/IK)       Framework, device matrix,
   mobile-performance-engineer     vfx-artist (particles/juice)             bug repro, go/no-go)
   build-release-engineer (AAB/CI) audio-designer (SFX/music/mix)
                                   ui-ux-designer (touch UI/HUD/menus)
```

**Coordination contract (baked into the personas):** the team reads `docs/game/GDD.md` + `docs/game/pillars.md`; the architect writes decisions as **ADRs in `docs/adr/`**; agents share state via the `collaboration` memory namespace. `game-director` sets the brief and judges against pillars; it doesn't micromanage. Defer *what/why* to director+designer, *how (engine)* to unity-engine-architect.

### How Claude drives them
- **Subagent type:** each `.claude/agents/game-dev/*.md` is a real Claude Code subagent. When a Claude Code session is **rooted in this Towerpolis repo**, they're invocable directly as `subagent_type` (Agent tool) / `agentType` (Workflow). That's the intended way to run them.
- **In-session orchestration:** Claude's **Workflow** tool fans these roles out deterministically (pipeline/parallel) and verifies (e.g. `game-qa-engineer` adversarially checks `gameplay-programmer`'s output). If a given `agentType` isn't resolvable in the current session, the persona text is inlined into the agent prompt — same behavior either way.
- **Ruflo CLI (optional, Mode B):** `npx ruflo swarm init` (hierarchical, 6–8 agents, raft) for the cross-session memory/hooks/learning layer.

### Honesty notes (so we don't drift)
- Claude Code still **writes the Unity C# itself** — the agents are the *roles/lenses* (designer specifies, gameplay-programmer implements, qa verifies). The hierarchy is how we keep a solo build disciplined and reviewed, not extra headcount.
- `ui-ux-designer` prefers **UI Toolkit** for new UI (UGUI where needed) — defer the UGUI-vs-UIToolkit call to that agent in Phase 5.
- `build-release-engineer` reuses the repo's generic CI/devops patterns (`devops/ci-cd/ops-cicd-github`) applied to Unity AAB builds.

### Standing engineering discipline (every phase)
- **Deterministic game logic → Unity-free `Towerpolis.Core`** assembly (`noEngineReferences: true`) with **NUnit** tests that run standalone via `dotnet test` AND in Unity Test Runner. Grading, daily seed, scoring, city population, economy math live here. **Score/grade NEVER from PhysX** (not cross-device deterministic → cheating + breaks daily-seed fairness; `physics-programmer` enforces this).
- Test-first on Core; commit only when green. Files < 500 lines; typed public APIs; validate at boundaries; no per-frame GC in hot paths.

---

## 1. Phase map (gate-driven) — with the agents that run each phase

> Each phase: **goal → lead + agents → deliverable → gate.** Research/spec for phase N+1 can overlap phase N.

### Phase 0 — Concept, GDD, Pillars  ✅ DONE
- **Ran:** 6 research agents (market/monetization/tech/art/naming/retention) + producer synthesis → this repo.
- **Deliverable:** `docs/game/GDD.md`, `docs/game/pillars.md`, `MARKET_ANALYSIS.md`, `ART_BIBLE.md`, this plan. Name = **Towerpolis**.
- **Now owned by:** `game-director` (maintains GDD + pillars going forward).

### Phase 1 — Architecture & Project Scaffold
- **Lead:** `unity-engine-architect`. **With:** `gameplay-programmer` (Core scaffold), `build-release-engineer` (GameCI/AAB skeleton), `game-qa-engineer` (NUnit harness).
- **Goal:** Unity 6.3 URP project; **`Towerpolis.Core`** (Unity-free) + asmdef layout; ScriptableObject data pattern; Addressables decision; **perf budget written as ADR** (`docs/adr/`); Git/LFS; CI.
- **Gate:** opens in Unity clean; `dotnet test` runs Core; CI green skeleton; budget ADR published.

### Phase 2 — MVP Core Loop (vertical slice that proves "is it FUN?")
- **Lead:** `game-designer` (loop spec + tunables) → `gameplay-programmer` (C#) + `physics-programmer` (the wobble) → `vfx-artist`/`audio-designer` (juice) → `game-qa-engineer` (feel test).
- **Goal:** crane → tap → drop (Rigidbody) → **deterministic grade** (Core) → freeze+weld → **scripted wobble** → full juice. One Blender block set + one MJ background. Local high score + instant restart.
- **Gate (HARD):** `game-qa-engineer` + you confirm people want "one more try" and the wobble reads tense-but-fair. **Nothing else ships until yes.**

### Phase 3 — Meta: Build City + Daily Seed + Leaderboards
- **Lead:** `game-designer` + `unity-engine-architect` → `gameplay-programmer` (city grid, deposit, population) + `build-release-engineer`/backend (GPGS + Firebase Remote Config) → `3d-artist`/`technical-artist` (city tiles) → `game-qa-engineer`.
- **Goal:** persistent 3D city; daily seed (deterministic, server-driven); leaderboards (friends + weekly-reset + per-district) via Google Play Games Services.
- **Gate:** daily seed identical across two devices; leaderboard submit/read; city persists.

### Phase 4 — Progression, Upgrades, Streaks, Missions
- **Lead:** `game-designer` (economy model, sources/sinks, no dominant strategy) → `gameplay-programmer` → `game-qa-engineer` (balance playtest).
- **Goal:** crane upgrades, cosmetics economy, streaks + daily login + free progression track, weekly missions, achievements — all as ScriptableObject data (Core-tested currency math).
- **Gate:** economy balances in playtest; saves/loads; no currency exploit.

### Phase 5 — UI/UX, Menus, Settings, Localization (RU + EN)
- **Lead:** `ui-ux-designer` (UI Toolkit/UGUI call, thumb-zones, safe areas, responsive) → `gameplay-programmer` (state wiring) → `audio-designer` (UI SFX) → `game-qa-engineer` (touch ergonomics on device).
- **Goal:** main menu, settings, HUD, shop UI, end-screen + share card. Unity Localization tables RU+EN, zero hardcoded strings.
- **Gate:** RU/EN switch with no missing keys; all screens reachable; settings persist; safe areas correct on smallest+largest screens.

### Phase 6 — Art & Audio Production + Integration
- **Lead:** `technical-artist` (pipeline, import presets, budgets, look-dev, recolor discipline) → `3d-artist` (blocks/props) + `character-animator` (parachuting resident rig/clip) + `vfx-artist` (confetti/dust/sparks) + `rendering-engineer` (URP tiers, baked lighting, ASTC) + `audio-designer` (CC0-first SFX + music) → `mobile-performance-engineer` (watch draw calls/GC on import).
- **You:** generate MJ 2D (icon/UI/skybox/store), buy/strip one Synty kit, model blocks in Blender (per `ART_BIBLE.md`).
- **Gate:** one coherent palette (mandatory recolor pass); <200 draw calls; 60 fps on a mid Android.

### Phase 7 — Monetization (build dormant → turn on later)
- **Lead:** `game-designer` (reward placement, never pay-to-win) + `payments` persona (IAP/economy) → `gameplay-programmer` (AdMob + IAP SDK wiring, remote kill-switch) → `game-qa-engineer` (sandbox purchase/restore) → `security-auditor` (consent/ATT/GDPR/data-safety).
- **Goal:** rewarded + light interstitial + optional banner; IAP (remove-ads, cosmetics, gems, battle pass). **OFF at launch** behind a remote flag.
- **Gate:** sandbox rewarded serves; IAP purchase + restore works; default-off kill-switch verified.

### Phase 8 — Polish, Performance, QA, Security
- **Lead:** `mobile-performance-engineer` (profile real devices, find the real bound, kill GC hitches) + `game-qa-engineer` (device matrix, edge cases, go/no-go) + `security-auditor` (save/leaderboard/IAP) + `rendering-engineer` (GPU fixes).
- **Gate:** clean Android Vitals (crash/ANR low — else auto-excluded from Play featuring); no P0/P1; profiler within budget per device tier.

### Phase 9 — ASO, Store Setup, Soft-Launch, Featuring
- **Lead:** `build-release-engineer` (signed AAB, Play Console, staged rollout, fastlane) + `researcher` (live ASO/keyword/competitor pass) + Claude (RU+EN listing copy) + `game-director` (featuring-readiness verdict).
- **Goal:** Play + App Store listings (RU+EN), icon/feature-graphic/screenshots (MJ), soft-launch in 1–2 cheap tier-2 markets; **submit Apple featuring nomination (≥3 months ahead) + Google form + Indie Corner/Level Up.**
- **Gate:** soft-launch **D1 ≥ 30% / D7 ≥ 12%** before any global push.

### Phase 10 — Live-Ops (ongoing, solo-sustainable)
- **Lead:** `game-director` (calendar) → `gameplay-programmer` (seasonal art *system* + Remote Config) → `technical-artist` (recolor/prop swaps) → `game-qa-engineer`.
- **Cadence:** daily seed (automated) · weekly leaderboard reset + rotating missions · monthly themed district/skin. ≥2 content beats in first 90 days. ~30–35 hrs/week (60+ → burnout by ~month 9). Use Claude `/loop` + `schedule` for recurring tasks.

---

## 2. Repo shape (target)

```
Towerpolis/
├─ .claude/agents/game-dev/      # ✅ the 15 Unity agents (copied from my_agents)
├─ docs/
│  ├─ game/  GDD.md · pillars.md  # ✅ what the agents read
│  ├─ adr/                        # architecture decision records (unity-engine-architect)
│  ├─ MARKET_ANALYSIS.md · BUILD_PLAN.md · ART_BIBLE.md
├─ unity/Towerpolis/             # the Unity 6.3 project (generated on first open)
│  └─ Assets/
│     ├─ _Core/ (+ _Core.Tests)  # Towerpolis.Core — pure C#, NUnit
│     ├─ Game/ Data/ UI/ Art/ Audio/ Plugins/
├─ .github/workflows/ci.yml      # GameCI: build Android + run Core tests
└─ CLAUDE.md .gitignore .gitattributes README.md
```

**Remote git:** create a **new private repo** for Towerpolis (separate from `my_agents`); `git remote add origin …`. `my_agents` stays only as the agent/tooling source.

---

## 3. Your (human) homework, by phase

- **Now:** Unity Hub + **Unity 6.3 LTS** (Android Build Support; iOS later); confirm `.NET SDK` (`dotnet`); `git lfs install`.
- **Phase 2:** Android device + USB debugging for on-device feel tests.
- **Phase 6:** Midjourney sub; buy/sub one **Synty** pack (POLYGON MINI City) or grab the free Starter; install **Blender** (model the blocks).
- **Phase 7:** AdMob + Google Play Console; decide the **publishing entity / payout route** (RU payout constraints — resolve early; consider RuStore as a secondary channel).
- **Phase 9:** Apple Developer + App Store Connect (featuring nomination 3 months ahead); ASO assets.
