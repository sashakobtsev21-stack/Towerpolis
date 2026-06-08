# Towerpolis — Project Status, History & Plan

*Living status doc — last updated 2026-06-08. Keep current alongside `README.md` (see CLAUDE.md workflow rule).*
*Source-of-truth design lives in [`game/GDD.md`](game/GDD.md), [`BUILD_PLAN.md`](BUILD_PLAN.md), [`adr/`](adr); this file tracks WHERE WE ARE.*

---

## 1. Snapshot

| | |
|---|---|
| **Current focus** | Phase 3 (Build City meta) closing + Phase-4 meta (prestige, progression) building ahead of full UI |
| **Engine** | Unity 6.3 LTS · URP mobile · Android-first |
| **Core** | `Towerpolis.Core` (Unity-free, deterministic) — **312 NUnit tests, green** (`dotnet test`) |
| **Build** | Game + Game.Tests compile against Unity DLLs; CI runs Core on every push |
| **Repo** | origin = github.com/sashakobtsev21-stack/Towerpolis · all work on `master` |
| **Monetization** | OFF (hooks only); flip after retention proven (D1≥30% / D7≥12% / rating≥4.0) |

**Verification debt:** a large body of Unity-layer code is built + compiles but is **not yet playtested on-device**. The owner verifies in Unity in batches. Anything below marked “(needs playtest)” compiles + is logic-tested but unconfirmed for feel.

---

## 2. Phase progress

- **0 — Concept & GDD** ✅  · **1 — Architecture / scaffold** ✅ (Unity project, Core + dual test harness, CI, ADRs)
- **2 — MVP core loop** ✅ (crane → tap-drop → deterministic grade → weld → scripted wobble + juice; FUN gate passed by owner)
- **3 — Build City meta** 🔨 *(feature-complete; on-device gate pending)* — districts, daily seed, local leaderboards, coins, streaks, district-complete screen, per-district music hooks, **best-N city grid** (no soft-lock).
- **4 — Progression / endless** 🔨 *(Core done + tested; UI partial)* — upgrades (Magnet, City Bonus), weekly missions, achievements, login calendar, streak-freeze, **prestige / endless loop**.
- **5 — UI/UX & Localization** 🔨 *(foundation done)* — custom RU/EN Loc service (ADR-0008), HUD + MetaHud localized, safe-area, settings. Full screen/polish pass pending.
- **6 — Art & audio production** ⏳ — **the big remaining content bucket** (models, textures, animations, SFX/music, backgrounds, VFX). Owner deferred to last.
- **7 — Monetization (dormant)** ⏳ · **8 — Polish/perf/QA** ⏳ · **9 — ASO/soft-launch** ⏳ · **10 — Live-ops** ⏳

---

## 3. What's DONE (systems)

**Deterministic Core (`Towerpolis.Core`, NUnit-tested):**
- Run grading/scoring/lean/strikes (`TowerRun`, `Grading`, `Scoring`), seeded crane swing + block sequence (`DailySeed`, `RunSeeds`, `BlockSequence`, `XorShiftRng`).
- Combo (СЕРИЯ): fills on Perfects, **any non-perfect resets**, fills to 5 → **+coins bonus** + reset; per-floor residents bonus by level.
- Meta: `CityGrid` (**best-N** — a full grid swaps its smallest tower for a bigger one), `CityState` (deposit → coins → daily/streak → fill-goal reward), `DistrictGoal`, `LocalLeaderboard`, `DailyStreak`, `CoinEarnCalculator`.
- Progression: `UpgradeState`/`UpgradeService` (Magnet, City Bonus), `MissionTracker`, `AchievementEvaluator`, `LoginCalendar`.
- **Prestige/endless:** `CityState.Prestige()/IsPrestigeReady()/PrestigeBonusMult`, `TowerRun(cfg, residentMult)`, Prestige Stars (permanent residents multiplier).
- Save: `SaveData` **schema v3** + forward-only `SaveMigration`.

**Unity layer (compiles; self-bootstraps, no scene wiring):**
- Gameplay: `TowerGameController`, `CraneController`, `TowerController` (scripted wobble), `BlockSpawner` (FBX + runtime recolor), `FallingBlock`, `SettleUpright`.
- HUD (`HUDController`): height + residents (left, icon-led), live coins (top-right), **vertical combo bar** (left, +N coins pop on fill), strike pips, PERFECT/SUMMIT/restart beats.
- Meta HUD (`MetaHud` — split into partial classes <500 lines): ☰ menu → City / Bonuses (upgrades+login) / Goals (missions+achievements) / Settings (RU·EN, sound, reset); **district-complete** celebration; **prestige screen** + city-view re-access button.
- `MetaService` (meta bridge: bank runs, persist, auto-advance district, prestige), `GameAudio` (SFX + per-district music crossfade — silent until tracks added), `GameVfx`, district themes/sky/atmosphere, custom Loc (`Loc`, `LocTables`, `LocalizedLabel`, dynamic Cyrillic font).
- CI (`.github/workflows/ci.yml`): Core tests always; Unity EditMode + Android AAB when `UNITY_LICENSE` secrets exist.

---

## 4. History (recent → older; full log in `git log`)

**Session 2026-06-06 → 08 (this push):**
- `28df4dc` Combo: any non-perfect resets the bar; fill bonus = **coins** (“+N монет” pop).
- `193cfd2` **Prestige/endless Core spine** + 10 tests; Winter grid 24→28; save v3.
- *(uncommitted at time of writing → in this push)* **Prestige SCREEN** wired (MetaService `PrestigeReady`/`DoPrestige`, MetaHud prestige panel + city-view re-access) → endless loop functional.
- `aaee6fa` Combo bar fill→bonus→reset loop; removed the confusing “×N” label.
- `dc2ec7e` HUD relayout (combo bar + height/residents left) + **dropped district switcher** (single active district, auto-advance) — from competitor analysis (researcher + game-designer agents).
- `f283ba2` Combo cap 3→5.
- `f17286c` README rewrite + CLAUDE.md “keep README current” rule.
- `26d4696` Remove dead code (post physics/skins cleanup).
- `5cd185e` Split `MetaHud` into partial classes (<500-line rule).
- `6b3a00a` **Remove cosmetic skins + Slow-Mo upgrade** (owner dropped both).
- `383e019` **Fix miss-block “snag on air”** — decouple the miss tumble from physics.
- `0f5d71e` Repo hygiene (.meta + Cyrillic font).
- `a4fd816` Phase-3 TODO-2 stub (`LastDailyAttemptDate`).
- `72e91d0` District-complete screen + per-district music hooks + **best-N grid (soft-lock fix)** + restore design fill goals.

**Earlier:** `dc56330` cut skins UI · `602d811` ☰ menu · `80c4e75` PlayMode scaffold · `7559d87`/`69c20c7` Core coverage · `e567ac4` cloud-save interface (ADR-0009) · `629a712` safe-area · Phase 0-2 foundation.

**Key decisions made:** Tower-Bloxx pivot (whole blocks, lean/sway); deterministic Core never from PhysX (ADR-0002); custom Loc service not Unity Localization (ADR-0008); districts = single active + linear gate + auto-advance (switcher returns with real art, Phase 6); end-game = **prestige/endless** (hybrid); combo = perfect-streak bar → coin bonus; skins + Slow-Mo cut. Competitor analysis → [`../` memory `reference-competitor-analysis-hud-meta`].

---

## 5. What's REMAINING (near-term, prioritized)

1. **On-device verification of everything built** (owner). Run `PHASE3_GATE_CHECKLIST.md` + sanity-check this session's changes (combo bar, prestige screen, HUD left column, miss tumble). **This gates building more on top.**
2. **Commit the auto-generated `.meta`** for the new prestige/combo files after first Unity open.
3. **Balance pass (Phase-4 gate):** combo coin bonus, prestige star/coin curves, district fill goals vs best-N reachability — tune the `CoreConfig` data after playtest; watch GameAnalytics signals (see endless-spec §7).
4. **Phase-4 UI polish:** prestige screen feel (VFX/star-rain), profile/trophy stat for `LifetimeBestPopulation`, prestige badge.
5. **Phase-5 pass:** main menu, end-screen/share card, full screen reachability, hardcoded-string re-audit, settings persistence on device.
6. **Open spec TODO-2** (Phase 5): daily-run void on app-quit (field `LastDailyAttemptDate` reserved).

---

## 6. Roadmap (Phase 6+ — see BUILD_PLAN.md)

- **6 — Art & audio (the deferred content bucket):** block/resident models, palettes, parachuting-resident rig/clip, VFX (confetti/dust/sparks), URP look-dev, **authored SFX + 3 district music beds** (drop into `Assets/Audio/Resources/Music`, see AUDIO_GUIDE), backgrounds/skyboxes. Re-introduce district switching once 2+ districts are visually distinct. Target: <200 draw calls, 60 fps mid-Android.
- **7 — Monetization (dormant):** rewarded + light interstitial + IAP behind a remote kill-switch, off at launch.
- **8 — Polish / perf / QA:** device matrix, GC hitches, Android Vitals clean, no P0/P1.
- **9 — ASO / soft-launch:** signed AAB, Play Console, RU+EN listings, 1–2 tier-2 markets; gate global on D1≥30% / D7≥12%.
- **10 — Live-ops:** daily seed (auto), weekly leaderboard reset + rotating missions, monthly themed district/skin.

---

## 7. How to work here (pointers)

- **Conventions + agent studio:** [`../CLAUDE.md`](../CLAUDE.md), [`AGENT_ORCHESTRATION.md`](AGENT_ORCHESTRATION.md).
- **Build & run + Core tests:** [`../README.md`](../README.md).
- **Specs:** [`game/meta-spec.md`](game/meta-spec.md), [`game/progression-spec.md`](game/progression-spec.md), [`game/mvp-core-loop-spec.md`](game/mvp-core-loop-spec.md).
- **Gate checklist:** [`PHASE3_GATE_CHECKLIST.md`](PHASE3_GATE_CHECKLIST.md). **Art/audio:** [`ART_BIBLE.md`](ART_BIBLE.md), [`ASSETS_GUIDE.md`](ASSETS_GUIDE.md), [`AUDIO_GUIDE.md`](AUDIO_GUIDE.md), [`BLENDER_GUIDE.md`](BLENDER_GUIDE.md).
- **Decisions:** [`adr/`](adr).
