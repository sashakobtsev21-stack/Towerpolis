# Towerpolis — Phase 3 Meta Design Spec
# Build City · Daily Seed · Local Leaderboards · Currency Earn Hook

*Status: RATIFIED — build against this.*
*Author: game-designer. Owned with: unity-engine-architect (Core split), gameplay-programmer (C# impl), game-qa-engineer (gate criteria).*
*Parent docs: GDD.md, pillars.md, BUILD_PLAN.md §Phase 3.*

---

## 0. Scope of Phase 3

Phase 3 delivers the "reason to return" (Pillar 3) in its minimum shippable form:

- A persistent city that grows run-by-run (visual proof the player is building something).
- 3 starter districts with distinct identities, each unlockable via a fill goal.
- Daily Seed mode: one shared global run per day with its own local board.
- Solo/local leaderboards: endless high score, per-district best, daily-seed board, streak counter.
- Coin earn sources (values only; spend/shop is Phase 4).

What Phase 3 does NOT include: online leaderboards, friends boards, Google Play Games Services integration, cloud save, upgrades, missions, cosmetics shop, rewarded ads, share cards, UI polish pass (all Phase 4/5+).

---

## 1. Build City

### 1.1 What a "plot" is

A plot is one reserved cell in the district grid. It is the slot a completed tower deposits into. A plot is either:

| State      | Definition |
|------------|------------|
| `Empty`    | No tower yet. Shown as a flat foundation tile. |
| `Occupied` | Contains one deposited tower from a completed run. |

A plot stores: the floor count of the deposited tower, the resident count deposited, a timestamp, and the run's grade summary (used for display only, not re-scored).

### 1.2 City grid layout

Each district has its own rectangular grid of plots. Grid dimensions are per-district data (see §2). The grid is mobile-cheap: rendered as a flat overhead map (orthographic camera, no high-poly city mesh required). Individual tower previews can be shown as simplified LOD silhouettes.

Layout rules:
- Plots fill left-to-right, back-to-front (new towers appear at the front of the grid first, giving the best visual density).
- The grid is fixed size per district. Once all plots are occupied the district is considered "full" even if the fill-goal population was reached earlier.
- A player can replay a district (pile more towers in) for score purposes even after the fill goal is reached, but the population cap for that district is the sum of all plot residents up to the grid limit.

### 1.3 Run-deposit flow

When a run ends (second miss / topple, or the player manually ends a run), the following sequence executes:

1. Core computes `RunResult` (final `FloorCount`, `TotalResidents`, `Score`, grade distribution).
2. If the current district has empty plots: `CityGrid.Deposit(districtId, runResult)` → assigns the result to the next empty plot; increments `CityPopulation` by `runResult.TotalResidents`.
3. Unity side shows the deposit animation (tower silhouette flies into the city map; population counter increments with a pop). This is presentation only — the data is already committed.
4. Check `DistrictFillGoal`: if `CityPopulation >= fillGoal`, trigger district-complete flow (§2.4).
5. Update local leaderboards (§4).
6. Coin earn computed and awarded (§5).

A run that ends with zero floors placed (immediate miss on floor 1) deposits nothing and awards no coins.

### 1.4 City population (meta-score)

`CityPopulation` = sum of `TotalResidents` across all deposited towers in a district.

This is the primary meta-score. It is displayed on the city overview screen as the headline number ("Population: 2,847").

The global city population (across all districts) is also tracked and shown on the main menu as the player's "total city population" — the one number that summarises their entire progress.

### 1.5 Deterministic boundary

The following are in `Towerpolis.Core` (Unity-free, NUnit-tested):

- `CityGrid` state: plot allocation, deposit logic, `CityPopulation` aggregation.
- `DistrictFillGoal` evaluation.
- Coin earn calculation per run.
- `DailySeed` generation, daily streak state, daily-run validity window.

The following are Unity-side (presentation only):

- Deposit animation, population counter tween.
- City overview camera, plot LOD rendering.
- District-unlock cutscene / reward screen.

---

## 2. Districts

Three starter districts. Each is a `DistrictDefinition` ScriptableObject. New districts are data + art-pack drops with no code changes.

### 2.1 District data table

| Field                  | Type       | Notes |
|------------------------|------------|-------|
| `id`                   | string     | Stable, never renamed (used as save key). |
| `displayName`          | loc-key    | Localised via Unity Localization. |
| `gridWidth`            | int        | Columns in the plot grid. |
| `gridDepth`            | int        | Rows in the plot grid. |
| `fillGoalPopulation`   | int        | Residents required to complete the district. |
| `unlockRequirement`    | UnlockRule | See §2.3. |
| `rewardCoins`          | int        | Coins awarded on fill-goal completion. |
| `rewardGems`           | int        | Gems awarded on fill-goal completion (0 for D1, 1 for D2, 2 for D3). |
| `buildingPaletteKey`   | string     | References a `BlockPalette` SO (recolor of the shared block set). |
| `residentVariantKey`   | string     | References a `ResidentVariantSet` SO. |
| `skyboxKey`            | string     | References the district skybox/gradient asset. |
| `musicTrackKey`        | string     | References the district music bed. |
| `atmospherePalette`    | Color[]    | Overrides the altitude-tier sky gradient colors (6 tiers × 2 colors). |
| `boardId`              | string     | Local leaderboard key for per-district best score. |

### 2.2 The three starter districts

#### District 1 — Downtown ("Центр")

| Property | Value |
|---|---|
| `id` | `"downtown"` |
| Architecture identity | Warm red-brick mid-rises. Corner windows, stone cornices, street-level awnings. Cozy, human-scale city centre feel. |
| Block palette | Terracotta walls (#C47A51), cream trim (#F2E2C4), dark slate roof (#3D3D4A), brass/gold accents. |
| Residents look | Office workers in suits and coats, businesswomen with briefcases, delivery couriers. Warm skin tones, earth-tone clothing. |
| Skybox + palette | Blue-sky daytime, warm golden hour as tower rises. Sun visible in upper tiers. Pigeons in lower atmosphere tiers. |
| Music | Upbeat jazzy/acoustic city track. Moderate tempo, friendly energy. |
| Grid | 5 × 4 = 20 plots |
| Fill goal | 1 200 residents |
| Unlock requirement | Available from the start (no requirement). |
| Reward | 200 coins, 0 gems |
| Atmosphere palette override | Ground-level: warm amber fog. Upper: clear blue sky. Space tier: deep navy + gold stars. |

#### District 2 — Neon Quarter ("Неоновый квартал")

| Property | Value |
|---|---|
| `id` | `"neon"` |
| Architecture identity | Sleek glass-and-steel towers. Neon signage strips, reflective panels, cantilevered overhangs. High-tech, urban night energy. |
| Block palette | Midnight blue-grey walls (#1E2A3A), teal neon trim (#00E5CC), deep purple glass (#3A1A5C), hot-pink highlights (#FF3A7A). |
| Residents look | Tech workers with hoodies and backpacks, neon-punks with coloured hair, street-food vendors in aprons. Cool/jewel-toned clothing. |
| Skybox + palette | Perpetual evening/night sky. Low clouds lit from below by city glow. Stars visible from mid-height onward. Moon in upper tiers. |
| Music | Synthwave/lo-fi electronic track. Higher energy than Downtown, pulsing beat. |
| Grid | 5 × 4 = 20 plots |
| Fill goal | 1 600 residents |
| Unlock requirement | Downtown fill goal completed (population ≥ 1 200). |
| Reward | 350 coins, 1 gem |
| Atmosphere palette override | Ground: deep teal with orange horizon glow. Mid: purple-indigo night sky. Space: black with bright neon stars. |

**Art note (Phase 3):** Neon Quarter reuses the same block meshes as Downtown with a different `BlockPalette`. Neon signage/detail is handled by the emissive channel of the shared material — no new meshes required. Full mesh variant (dedicated glass/panel geometry) is a Phase 6 art-pack item.

#### District 3 — Winter Heights ("Зимние высоты")

| Property | Value |
|---|---|
| `id` | `"winter"` |
| Architecture identity | Snow-capped alpine towers. Steeply pitched snow roofs, timber-frame balconies, frosted glass, ice-crystal ornaments. Fairy-tale cosy-cold feel. |
| Block palette | Snow-white walls (#EEF4F8), icy blue trim (#8EC8E8), deep pine-green accents (#2A4A30), warm amber window glow (#FFD580). |
| Residents look | Bundled-up skiers, children in puffer jackets, reindeer-hat wearers, hot-drink vendors with scarves. Soft pastel + earth clothing. |
| Skybox + palette | Overcast pale blue winter sky. Snow flurries (particle system) in lower tiers. Clear cold starfield from upper atmosphere upward. Aurora hint in stratosphere tier. |
| Music | Gentle orchestral/celtic winter track. Slower tempo, wonder-ful feel. |
| Grid | 6 × 4 = 24 plots |
| Fill goal | 2 200 residents |
| Unlock requirement | Neon Quarter fill goal completed (population ≥ 1 600). |
| Reward | 500 coins, 2 gems |
| Atmosphere palette override | Ground: grey-white overcast. Mid: pale ice-blue. Stratosphere: aurora green + violet wash. Space: deep indigo + silver stars. |

**Art note (Phase 3):** Snow detail on roofs is a roof-cap mesh swap (one alternate roof variant) + a white emissive rim on floor tops. Snow particle is a lightweight looping particle system active only in this district's atmosphere tier 0. No new block geometry beyond the alternate roof cap.

### 2.3 Unlock rules

| Rule type | Meaning |
|---|---|
| `AlwaysUnlocked` | Available from first session (Downtown). |
| `DistrictComplete(id)` | Unlocked when the named district reaches its fill goal. |

Districts are unlocked linearly: Downtown → Neon → Winter. This is intentional — each district is a goal horizon that teaches the loop before the next variation is introduced.

TODO: decide whether a locked district is visible in the UI as a teased silhouette (recommended, creates anticipation) or hidden entirely. Recommendation: show it as a greyed-out locked card with "Unlock by filling Downtown" hint.

### 2.4 District-complete flow

Trigger: `CityGrid.CityPopulation >= district.fillGoalPopulation`.

Sequence:
1. Mid-run: complete the current drop; run ends normally.
2. End screen shows "DISTRICT COMPLETE" beat (distinct from normal run-end).
3. Reward screen: coins + gems awarded; display the district grid as fully populated.
4. Next district card revealed/unlocked.
5. Player returns to city overview; the filled district is visually marked as complete (e.g., gold border on the grid).

The player does not lose their filled district progress. They may continue placing towers in that district for score farming after completion (excess towers are accepted until the grid is physically full).

---

## 3. Daily Seed Mode

### 3.1 Seed generation

Uses existing `DailySeed.ForDate(year, month, day)` → `ulong` seed. Called with the device's UTC date. The seed is the same for every player on the same calendar day UTC.

Seed feeds `RunSeeds` to produce `SwingRng` (crane swing pattern) and `BlockRng` (block type sequence) for that day's run.

No server dependency is required for seed generation — it is purely deterministic math. The server is used only later (Phase 5+) to compare scores across players.

### 3.2 Mode rules

| Rule | Value |
|---|---|
| Attempts per day | 1 official attempt per player per day. |
| Retry | Not permitted for the official daily attempt. (A "practice run on today's seed" mode can be added later as a rewarded feature — not in Phase 3.) |
| Resets | Midnight UTC. |
| District context | Daily Seed runs in the context of the player's currently active district (uses that district's block/resident set). The seed only controls the swing pattern and block sequence — it does not override district identity. |
| Deposits into city | Yes. A daily seed run deposits its tower into the active district on completion, same as an endless run. |
| Can be ended early | No. A daily seed run must play to completion (second miss or topple). The player cannot quit and re-submit. If the player exits the app during a daily seed run, the run is voided and the daily attempt is consumed. |

TODO: define exact "void" behaviour on app quit during daily run — does it count as a failed run (0-floor deposit) or is the attempt simply lost? Recommendation: count it as a failed run (0 floors, 0 residents) to prevent quit-and-retry exploit while not punishing phone calls.

### 3.3 "First win of the day" bonus

A "win" in daily seed context = any completed daily run (even a 1-floor collapse counts as "completed" since the player attempted it). Awarded once per UTC day.

| Bonus | Value |
|---|---|
| Coin bonus | 50 coins (tunable: `DailySeedFirstWinCoins`, default 50, range 25–150) |
| Streak increment | +1 to daily streak counter if played within the UTC day. |

The phrase "first win" is a retention hook — even a poor run earns the bonus to reward daily engagement over daily perfection.

### 3.4 Streak rules

| Rule | Detail |
|---|---|
| Streak increments | Once per UTC day, on any completed daily seed run. |
| Streak breaks | If the player does not complete a daily seed run on any given UTC day, the streak resets to 0 the following day. |
| Streak display | Shown on the main menu HUD and the daily seed entry screen. |
| Streak milestones | Milestone rewards at 3, 7, 14, 30 days (coin values: 75, 200, 400, 1 000). Milestone rewards are one-time per milestone level; re-earning after a break re-triggers them. |
| Streak freeze | NOT in Phase 3. Planned for Phase 4 alongside the upgrade/mission system. |

Streak is stored as: `currentStreak` (int), `lastStreakDate` (UTC date string `"YYYY-MM-DD"`), `longestStreak` (int). These live in the player save file.

### 3.5 How daily seed differs from endless mode

| Axis | Endless | Daily Seed |
|---|---|---|
| Seed | New random seed each run (from `RunSeeds` with a random run seed). | Fixed global seed for the UTC day. |
| Attempts | Unlimited. | 1 per day. |
| Retry | Instant restart at will. | Not permitted. |
| Leaderboard | Per-district personal best. | Daily board (local in Phase 3; stub for online later). |
| Stake | Low — practice and score-chasing. | High — one shot, makes the tap matter more. |
| Streak | Does not contribute. | Increments streak. |

---

## 4. Leaderboards (Solo / Local)

Phase 3 ships local leaderboards only. The data schema is designed so that online leaderboards (GPGS / Firebase) can be layered on top without restructuring saves.

### 4.1 Records kept

| Record | Key | Type | Notes |
|---|---|---|---|
| Endless high score (all-time) | `endless_best_score` | int | Personal best `RunScore` across all endless runs, all districts. |
| Per-district endless best | `district_best_{id}` | int | Best `RunScore` for a given district, endless mode. One entry per district. |
| Per-district best floor count | `district_best_floors_{id}` | int | Tallest tower ever built in a district (endless). |
| Daily seed best score (all-time) | `daily_best_score` | int | Best ever `RunScore` on any daily seed run. |
| Daily seed today's score | `daily_today_{YYYY-MM-DD}` | int | Score from today's daily run (0 if not played). Keyed by date. |
| Daily streak (current) | `streak_current` | int | |
| Daily streak (longest ever) | `streak_longest` | int | |
| Total towers built | `stat_towers_built` | int | Lifetime run count. |
| Total residents housed | `stat_total_residents` | int | Lifetime sum across all runs. |
| Total perfect drops | `stat_total_perfects` | int | Lifetime perfect-drop count. |

All records stored locally in `PlayerPrefs` (or a JSON save file — architecture call for `unity-engine-architect`). Keys are stable strings so online sync can be added by posting the same values to a backend.

### 4.2 Player-visible boards

The leaderboard screen (Phase 5 for full UI polish; Phase 3 ships a minimal end-screen summary) shows:

**Personal Bests panel:**
- All-time endless best score.
- Best score per district (up to 3 rows in Phase 3).
- Tallest tower per district.

**Daily Seed panel:**
- Today's score (or "Not played yet" if untried).
- All-time daily best.
- Current streak + longest streak.
- Rank stub: "You are player #??? today" — the field is present in the data model but shows a placeholder in Phase 3 (online rank requires the server, Phase 5+).

**Stats panel:**
- Total towers, total residents, total perfects. These are engagement metrics and sharing hooks, not competitive boards.

### 4.3 Score submission logic

Submission is local write only in Phase 3. The flow is:
1. Run ends → Core produces `RunResult`.
2. `LeaderboardService.Submit(runResult, mode, districtId, date)` checks each record and updates if the new value is a personal best.
3. No network call. No async.

The `LeaderboardService` interface is designed so that Phase 5+ can add a parallel `OnlineLeaderboardService` that posts the same payload to GPGS/Firebase without changing the local path.

---

## 5. Currency Earn Hook (Phase 3 scope only)

Currency math lives in `Towerpolis.Core`. The spend side (shop, upgrades) is Phase 4.

### 5.1 Earn sources and values

All values are tunable via `CoreConfig` ScriptableObject fields. Default values and tuning ranges shown.

| Source | Coins earned | Tunable field | Default | Range |
|---|---|---|---|---|
| Per floor placed (any grade) | 1 coin/floor | `CoinPerFloor` | 1 | 1–3 |
| Per Perfect drop | +2 coins bonus | `CoinBonusPerfect` | 2 | 1–5 |
| Per resident deposited | 0 (floors drive the economy, not residents) | — | — | — |
| Daily seed first-run bonus | 50 coins | `DailySeedFirstWinCoins` | 50 | 25–150 |
| Streak milestone rewards | 75 / 200 / 400 / 1 000 | `StreakMilestoneCoins[4]` | see above | ×0.5–×2 |
| District fill-goal completion | Per district data | `DistrictDefinition.rewardCoins` | D1:200, D2:350, D3:500 | — |

**Design rationale for residents not earning coins directly:** residents are the meta-score (city population), keeping their meaning pure. If residents also earned coins, Perfect drops would double-dip (more residents + more coins), making the economy too perfects-focused and reducing the value of volume play. Floors drive coin income; perfects drive population quality.

### 5.2 Coin earn per run (example)

A 20-floor run with 8 perfects:
- 20 floors × 1 = 20 coins base.
- 8 perfects × 2 = 16 bonus coins.
- Total: 36 coins.

A 40-floor run with 15 perfects + district completion:
- 40 + 30 = 70 coins + district reward.

These numbers are intentionally modest because the spending economy (Phase 4) sets the prices. The earn rate should be tuned once upgrade costs are defined.

### 5.3 Gem earn (Phase 3)

Gems are awarded only at district completion (0, 1, 2 for D1/D2/D3 respectively). No other gem earn source in Phase 3. Gems have no spend target in Phase 3 — they accumulate for Phase 4.

### 5.4 Deterministic requirement

Coin calculation is deterministic and Unity-free:
- `CoinEarnCalculator.Calculate(RunResult result, CoreConfig config)` → `CoinReward` struct.
- NUnit-tested: given a specific RunResult, always produces the same coin total.

---

## 6. Deterministic Core vs Presentation Split

This table is the authoritative split for `unity-engine-architect` to enforce via assembly boundaries.

| System | Lives in | NUnit tested | Notes |
|---|---|---|---|
| Daily seed generation | `Towerpolis.Core` | Yes | `DailySeed.ForDate(y,m,d)` |
| Daily streak state (current, longest, lastDate) | `Towerpolis.Core` | Yes | Pure state machine, no Unity time |
| Run result calculation (score, residents, grade) | `Towerpolis.Core` | Yes | Already done in Phase 2 |
| Coin earn calculation | `Towerpolis.Core` | Yes | `CoinEarnCalculator` |
| District fill-goal evaluation | `Towerpolis.Core` | Yes | `DistrictGoalEvaluator` |
| City grid plot allocation, deposit, population sum | `Towerpolis.Core` | Yes | `CityGrid` |
| Leaderboard record update logic (is-new-best) | `Towerpolis.Core` | Yes | `LeaderboardService` (logic only, no I/O) |
| Streak milestone trigger | `Towerpolis.Core` | Yes | Pure: given streak int, return milestone or null |
| Serialisation / save-file I/O | Unity (`SaveManager`) | No (integration) | Reads/writes Core state structs; no game logic here |
| City overview rendering, plot LOD, deposit animation | Unity | No | Presentation only |
| District-unlock screen, reward display | Unity | No | Presentation only |
| Daily seed run enforcement (one-per-day UI gate) | Unity (+ Core date check) | Core date check yes | Core says "has run today"; Unity gates the button |
| Leaderboard screen layout, rank stub display | Unity | No | Presentation only |
| Atmospheric ascent visuals | Unity | No | Tier index driven by `FloorCount` (Core value) |

Rule: if a system's output could affect score fairness, cross-device consistency, or can be exploited if client-side, it goes in Core.

---

## 7. Phase-3 Gate (Acceptance Criteria)

The Phase-3 milestone is complete when ALL of the following pass `game-qa-engineer` sign-off:

### Gate 1 — Daily Seed Determinism
- Run `DailySeed.ForDate(2026, 6, 4)` on two separate Android devices (or two Unity Editor instances with no shared state). Confirm both return the identical `ulong` seed.
- Play the daily seed run on both instances. Confirm the crane swing pattern (first 20 swings) is identical.
- NUnit test: `DailySeedTest.SameSeedSameDay` passes in CI.

### Gate 2 — City Persists Across Sessions
- Complete 3 runs in Downtown. Force-quit the app. Relaunch. Confirm the city grid shows the 3 deposited towers and the population counter matches the sum of their residents.
- NUnit test: `CityGridTest.DepositPersistsAfterSaveLoad` passes (using a mock save adapter).

### Gate 3 — Leaderboard Submit and Read
- Complete a run. Verify the `endless_best_score` record updates if the run's `RunScore` is a new personal best.
- Complete a run with a lower score. Verify the record does not regress.
- Complete a daily seed run. Verify `daily_today_{date}` is written and `streak_current` increments.
- All via `LeaderboardServiceTests` NUnit suite.

### Gate 4 — District Unlock Flow
- Fill Downtown to its goal (1 200 residents). Confirm district-complete screen fires exactly once.
- Confirm Neon Quarter becomes selectable.
- Confirm Downtown reward (200 coins) is credited exactly once, not on every run after.

### Gate 5 — Coin Earn Correctness
- NUnit: `CoinEarnCalculatorTests` cover: zero-floor run, all-perfect run, no-perfect run, run + daily bonus, streak milestone triggers at 3/7/14/30.
- Manual: complete a run; verify the UI coin delta matches the `CoinEarnCalculator` output for that run's `RunResult`.

### Gate 6 — No Score Regression from Phase 2
- The Phase-2 NUnit suite (`TowerRunTests`, `GradeTests`, `ScoringTests`) remains 100% green after all Phase-3 Core additions.

---

## 8. Open Design Questions (TODOs)

These are deferred decisions. The spec above is buildable without resolving them; they affect only future phases or cosmetic detail.

**TODO-1 (Phase 3, minor):** Locked district visibility — greyed silhouette card (recommended) vs. fully hidden. Does not block implementation; UI implementation can default to hidden and we upgrade to silhouette in Phase 5 polish.

**TODO-2 (Phase 3, minor):** App-quit during daily seed run — count as failed (0-floor deposit, streak not broken, attempt consumed) or void the attempt entirely. Recommendation: failed run. Needs sign-off from game-director before implementation.

**TODO-3 (Phase 4):** Do resident types differ only visually per district, or do VIP/premium residents grant extra population/score multiplier? Deferred to Phase 4 progression design. Phase 3 residents are visual-only variants.

**TODO-4 (Phase 4):** Streak freeze mechanic. Not in Phase 3 scope. Placeholder field `streakFreezeCount` can be stubbed in save schema now.

**TODO-5 (Phase 5+):** Online leaderboard rank. The `daily_rank_stub` field is present in the data model as an int (default -1 = unknown). Phase 5 populates it via GPGS/Firebase. Phase 3 UI shows "---" in the rank slot.

**TODO-6 (Phase 5):** Share card generation (tower-deposits-into-city moment, district complete). Design intent: auto-generate on district complete and on new personal best. Implementation handed to `ui-ux-designer` in Phase 5.

**TODO-7 (Live-ops):** District 4 identity (Sakura / spring theme) and District 5+ cadence. Not in Phase 3 scope. `DistrictDefinition` data schema supports them without code changes.

---

## 9. Handoff Notes by Role

**gameplay-programmer:** implement `CityGrid`, `LeaderboardService`, `CoinEarnCalculator`, `DistrictGoalEvaluator`, `DailyStreakStateMachine` in `Towerpolis.Core`. Wire Unity `SaveManager` to serialize/deserialize Core state. Wire deposit animation trigger from run-end event. Gate the daily seed run button via Core's `HasPlayedDailyToday(date)` check.

**unity-engine-architect:** enforce the Core/Unity split in the table in §6. Define the `DistrictDefinition` ScriptableObject schema from the field table in §2.1. Decide save format (PlayerPrefs vs JSON file vs SQLite) — document as an ADR before implementation starts.

**3d-artist / technical-artist:** Phase 3 needs only palette recolors of the existing block set for Neon and Winter districts. Winter additionally needs one alternate roof-cap mesh variant. Full mesh variant packs are Phase 6. Neon emissive trim is a material channel change, not a new mesh.

**game-qa-engineer:** use §7 as the acceptance checklist. All NUnit tests must pass in CI before the Phase-3 gate is called.

**audio-designer:** three distinct music beds needed for Phase 3 (Downtown jazz, Neon synthwave, Winter orchestral). CC0-first sourcing. Tracks loop cleanly. Crossfade on district switch.

---

*This spec is < 500 lines. All tunables are expressed as `CoreConfig` or `DistrictDefinition` SO fields so balance changes require no code. Pillars served: Pillar 3 (city you own) and Pillar 4 (daily ritual). Pillar 1 and 2 are unchanged from Phase 2.*
