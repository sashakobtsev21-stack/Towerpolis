# Towerpolis — Phase 4 Progression Spec
# Upgrades · Cosmetics · Streaks · Missions · Achievements

*Status: RATIFIED — build against this.*
*Author: game-designer. Owned with: gameplay-programmer (Core impl), unity-engine-architect (SO schemas, save migration),
game-qa-engineer (gate criteria).*
*Parent docs: GDD.md §4.3/§4.8, pillars.md, meta-spec.md (Phase 3), ADR-0007.*

---

## 0. Scope and Constraints

Phase 4 delivers the full free-progression economy on top of the Phase 3 meta spine.

**In scope:**
- Currency wallet (coins already earnable; now add spend side + CoinWallet service in Core).
- Crane upgrades (2 tracks, Endless-only gameplay; 2 tracks cosmetic/everywhere).
- Block skins and crane skins (cosmetic, data-driven ScriptableObjects).
- Streak freeze and login-calendar track (extends existing `DailyStreak`).
- Weekly missions (rotating set; 8 base templates).
- Achievements (~10, permanent).
- Resident type design resolution (TODO-3 from meta-spec).
- SaveData schema v2 migration for all new state.

**Not in scope (later phases):**
- IAP, rewarded ads, premium gems purchase (Phase 7 — monetization OFF at launch).
- Online leaderboards, friend boards, cloud save (Phase 5).
- Share cards (Phase 5).
- Battle pass premium track (Phase 7).

### 0.1 Hard constraints (non-negotiable)

These override all creative preferences.

| Constraint | Ruling |
|---|---|
| **Daily-seed fairness** | NO upgrade may alter scoring, grading, perfect window, crane speed, crane arc, block width, or physics in Daily Seed mode. Any gameplay upgrade is silently suppressed during Daily Seed runs. |
| **Determinism** | All currency math, upgrade cost curves, mission progress evaluation, and achievement thresholds live in `Towerpolis.Core`. NUnit-tested. No engine dependency. |
| **One soft currency** | COINS only at launch. Gems already exist in the save (Phase 3 district rewards accumulate them); they remain earnable-only with zero spend targets in Phase 4. A gems spend layer is deferred to Phase 7 alongside IAP. Rationale: adding a second spendable currency before the economy is calibrated creates balancing debt with no player benefit yet. |
| **No dominant strategy** | Upgrades must not reduce the core skill challenge to zero. Magnet and slow-mo have per-run use caps; they improve *consistency* for casual players, not *ceiling* for experts. |
| **No pay-to-win** | All Phase 4 content is earnable with coins. No purchase required. |

---

## 1. Sources and Sinks — Economy Model

### 1.1 Coin earn sources (all values from CoreConfig; existing fields shown in bold)

| Source | Coins/event | Config field | Default | Range | Notes |
|---|---|---|---|---|---|
| Per floor placed (any grade) | 1 | **`CoinPerFloor`** | 1 | 1–3 | Existing |
| Per Perfect drop | +2 | **`CoinBonusPerfect`** | 2 | 1–5 | Existing |
| Daily Seed first run of day | 50 | **`DailySeedFirstWinCoins`** | 50 | 25–150 | Existing |
| Streak milestone day 3 | 75 | **`StreakMilestoneCoins[0]`** | 75 | — | Existing |
| Streak milestone day 7 | 200 | **`StreakMilestoneCoins[1]`** | 200 | — | Existing |
| Streak milestone day 14 | 400 | **`StreakMilestoneCoins[2]`** | 400 | — | Existing |
| Streak milestone day 30 | 1 000 | **`StreakMilestoneCoins[3]`** | 1 000 | — | Existing |
| District D1 complete | 200 | (DistrictInfo) | 200 | — | Existing |
| District D2 complete | 350 | (DistrictInfo) | 350 | — | Existing |
| District D3 complete | 500 | (DistrictInfo) | 500 | — | Existing |
| Login calendar day reward | 10–150 | `LoginCalendarCoins[]` | see §3.2 | ×0.5–×2 | NEW |
| Weekly mission complete | 50–200 | per `MissionDefinition.rewardCoins` | see §4.1 | — | NEW |
| Achievement unlock | 100–500 | per `AchievementDefinition.rewardCoins` | see §4.3 | — | NEW |

### 1.2 Coin sinks

| Sink | Cost | Config field | Notes |
|---|---|---|---|
| Crane upgrade: Magnet, level 1→2→3→4 | 80 / 200 / 450 / 900 | `MagnetUpgradeCosts[]` | see §2 |
| Crane upgrade: Slow-Mo, level 1→2→3→4 | 100 / 250 / 550 / 1 100 | `SlowMoUpgradeCosts[]` | see §2 |
| Block skin (common) | 150 | `SkinCostCommon` | 150 | cosmetic |
| Block skin (rare) | 400 | `SkinCostRare` | 400 | cosmetic |
| Crane skin (common) | 200 | `SkinCostCraneSkin` | 200 | cosmetic |
| Streak freeze (1 use) | 80 | `StreakFreezeCost` | 80 | consumable |

### 1.3 Balance check — coins per average run

Assumptions: median player, 25-floor run, 40% perfect rate (10 perfects), Endless mode.

```
Run base coins  = 25 × 1            = 25
Perfect bonus   = 10 × 2            = 20
Run total                           = 45 coins
Daily seed run  = 45 + 50 first-win = 95 coins (first run of day)
```

Sessions/day ~4. First is Daily (95 coins), remaining 3 Endless (~45 coins each).
**Typical daily earn: 95 + 3 × 45 = 230 coins/day.**

Login calendar adds 20–50 coins/day on average across the 30-day cycle.

**Total daily earn: ~250–280 coins/day.**

### 1.4 "Runs to afford X" reference table

| Item | Cost | Runs (at 45 coins/run) | Days (at 230 coins/day) |
|---|---|---|---|
| Streak freeze | 80 | 2 | 0.3 |
| Block skin (common) | 150 | 4 | 0.7 |
| Magnet L1 | 80 | 2 | 0.3 |
| Magnet L2 | 200 | 5 | 0.9 |
| Crane skin | 200 | 5 | 0.9 |
| Block skin (rare) | 400 | 9 | 1.7 |
| Magnet L3 | 450 | 10 | 2.0 |
| Slow-Mo L3 | 550 | 13 | 2.4 |
| Magnet L4 | 900 | 20 | 3.9 |
| Slow-Mo L4 | 1 100 | 25 | 4.8 |
| All upgrades (max) | 3 530 | 79 | 15 |
| All skins (7 items at avg 250) | ~1 750 | 39 | 7.6 |

Early upgrades (L1/L2) are within 1 day. Max-everything takes roughly 3 weeks of daily play — a
sensible soft-launch target for the economy ceiling.

### 1.5 Economy health rules

1. The wallet can never go negative (spend is gated on affordability check in Core before mutation).
2. Sinks grow faster than sources at higher upgrade tiers (each upgrade tier costs ~2.2× the previous)
   to prevent "too much money, nothing to buy" past day 15.
3. No cosmetic is ever gated behind both time AND skill — any cosmetic is reachable within 2 weeks
   of casual play (~4 sessions/day).

---

## 2. Crane Upgrades

Four upgrade tracks in two categories. GDD §4.3 lists three candidate upgrades; this spec ratifies
two gameplay upgrades and two cosmetic/meta tracks, keeping daily fairness intact.

### 2.1 Upgrade categories

| Category | Affects Daily Seed? | Rationale |
|---|---|---|
| **Gameplay** (Magnet, Slow-Mo) | **NO — suppressed in Daily Seed** | These alter the drop accuracy distribution, which would corrupt the shared fair challenge. Unity side: check `RunContext.IsDaily` before applying; Core upgrade state is read but effect is skipped. |
| **Cosmetic / Meta** (Crane Rope Skin, City Bonus) | **YES — safe everywhere** | No effect on scoring, grading, or timing. |

### 2.2 Gameplay upgrade track A — Magnet (Auto-center)

**What it does:** at the moment of tap, the block's lateral offset is partially corrected toward
center. Does NOT guarantee a Perfect; it shifts the probability distribution toward the Perfect zone.
The player still has to time the tap — the magnet only reduces the *cost* of slight mistimes.

| Level | Effect (offset correction fraction) | Cost (coins) | Config field |
|---|---|---|---|
| 0 (none) | 0% — baseline | — | — |
| 1 | 15% correction | 80 | `MagnetUpgradeCosts[0]` |
| 2 | 28% correction | 200 | `MagnetUpgradeCosts[1]` |
| 3 | 38% correction | 450 | `MagnetUpgradeCosts[2]` |
| 4 (cap) | 45% correction | 900 | `MagnetUpgradeCosts[3]` |

**Correction formula (Core, Endless only):**
```
adjustedOffset = rawOffset × (1.0f - MagnetFraction[level])
```
The adjusted offset is passed to the existing grading logic (`PerfectThreshold`, `GoodThreshold`).
Raw offset and adjusted offset are both stored in `DropOutcome` so replays remain self-contained.

**Daily Seed:** `MagnetFraction[level]` is replaced with 0 when `RunContext.IsDaily == true`. No code
branch in the grading path — the caller passes the correct fraction.

**Cap reasoning:** 45% correction at max is insufficient to turn a sloppy mid-swing tap into a
Perfect (which requires offset < 15% of block width). It meaningfully helps sub-20% offsets become
Perfects, preserving the skill gap between expert and casual players.

**Tunable parameters (CoreConfig):**
```csharp
public float[] MagnetFractions = { 0f, 0.15f, 0.28f, 0.38f, 0.45f }; // index = level (0 = unupgraded)
public int[]   MagnetUpgradeCosts = { 80, 200, 450, 900 };
```

### 2.3 Gameplay upgrade track B — Slow-Mo Charge

**What it does:** the player can hold the tap for 0.4 s instead of releasing immediately. During the
hold, the crane's visible swing speed is multiplied by `SlowMoFactor[level]`. Releasing ends the slow-mo
and drops the block. One charge per run; refreshes on a Perfect drop (rewards skilled play with a
safety net, not a crutch).

| Level | Speed multiplier | Cost (coins) | Config field |
|---|---|---|---|
| 0 (none) | 1.0 (no effect) | — | — |
| 1 | 0.55 | 100 | `SlowMoUpgradeCosts[0]` |
| 2 | 0.45 | 250 | `SlowMoUpgradeCosts[1]` |
| 3 | 0.38 | 550 | `SlowMoUpgradeCosts[2]` |
| 4 (cap) | 0.32 | 1 100 | `SlowMoUpgradeCosts[3]` |

**Hold window:** `SlowMoDuration = 0.4f` seconds (tunable, range 0.2–0.6). The crane sway animation
is time-scaled by the multiplier on the Unity side; the deterministic drop position is sampled at
actual-hold-release time and passed to Core as a raw offset — same grading path as a normal tap.

**Daily Seed:** slow-mo does not apply (`SlowMoFactor` is treated as 1.0). The hold gesture is
consumed silently (no visual effect) so the UI does not need to hide the button; it just does nothing.

**Tunable parameters (CoreConfig):**
```csharp
public float[] SlowMoFactors = { 1.0f, 0.55f, 0.45f, 0.38f, 0.32f }; // index = level
public int[]   SlowMoUpgradeCosts = { 100, 250, 550, 1100 };
public float   SlowMoDuration = 0.4f;    // seconds, range 0.2–0.6
```

**Note on "Extra Life" (GDD §4.3):** deferred. Adding a third strike in Endless would require a
Core StrikeLimit override path, which touches game-feel tuning that belongs in Phase 5 once soft-
launch data shows where players are dropping off. The two upgrades above cover the retention use case
(reduce frustration from near-misses) without altering the end condition.

### 2.4 Cosmetic upgrade track C — Crane Skin

**What it changes:** the visual asset (rope material, hook model, arm color) of the crane. No gameplay
effect. Safe in Daily Seed and Endless. Purchased once per skin, no levels.

Defined as a `CraneSkinDefinition` ScriptableObject (see §5.2).

| Skin | Cost | Notes |
|---|---|---|
| Default (wood/rope) | free | Always equipped |
| Steel Cable | 200 coins | Common |
| Gold Chain | 400 coins | Rare |
| Neon Flex | 400 coins | Rare (unlocks after Neon district) |

Unlock gate for Neon Flex: `CraneSkin.UnlockRequiresDistrictId = "neon"` — Core evaluates
affordability only if the district is filled (via `CityState.IsRewarded("neon")`). This is a
cosmetic lock, not a score-affecting gate; the lock is advisory in Core and enforced in UI.

### 2.5 Meta upgrade track D — City Bonus (passive)

**What it changes:** the coin multiplier applied to district-completion rewards only (does NOT affect
per-floor or per-perfect coins to avoid inflating the daily economy). This is a meta quality-of-life
upgrade for players who replay filled districts.

| Level | District completion coin multiplier | Cost (coins) | Config field |
|---|---|---|---|
| 0 (none) | 1.0× | — | — |
| 1 | 1.15× | 120 | `CityBonusUpgradeCosts[0]` |
| 2 | 1.30× | 320 | `CityBonusUpgradeCosts[1]` |
| 3 (cap) | 1.50× | 700 | `CityBonusUpgradeCosts[2]` |

**Formula:** `districtCoins = floor(baseRewardCoins × CityBonusMultiplier[level])`.
Applied in `CoinEarnCalculator` at run-end when `districtCompletedNow == true`.

**Daily Seed:** SAFE — only applies to the district-completion lump, which is awarded identically
regardless of mode. It does not touch per-floor or per-perfect coins, so no scoring distortion.

**Tunable parameters (CoreConfig):**
```csharp
public float[] CityBonusMultipliers = { 1.0f, 1.15f, 1.30f, 1.50f };
public int[]   CityBonusUpgradeCosts = { 120, 320, 700 };
```

### 2.6 Upgrade state in save

All upgrade levels are integers stored in `SaveData` (schema v2):

```csharp
public int MagnetLevel;    // 0–4
public int SlowMoLevel;    // 0–4
public int CityBonusLevel; // 0–3
public List<string> OwnedCraneSkins = new();   // stable skin ids
public string EquippedCraneSkinId = "default";
```

Core logic for purchasing: `UpgradeService.TryPurchase(upgradeId, level, wallet)` — pure function
returning success/fail with new wallet value; no mutation unless the caller applies the result.

### 2.7 Determinism / fairness ruling — upgrade summary

| Upgrade | Daily Seed | Reasoning |
|---|---|---|
| Magnet | SUPPRESSED (0% correction) | Changes drop accuracy distribution; violates daily fairness. |
| Slow-Mo | SUPPRESSED (1.0× speed) | Changes timing window; violates daily fairness. |
| Crane Skin | ALLOWED | Visual only; no stat effect. |
| City Bonus | ALLOWED | Affects only district-completion reward; no scoring/grading change. |

---

## 3. Streaks and Login Calendar

### 3.1 Streak freeze

Addresses meta-spec TODO-4. A streak freeze protects one missed day; it is consumed automatically
if the player did not complete a Daily Seed run and returns the next day.

**Rules (Core, extends `DailyStreak`):**
- Player holds 0–3 freeze charges at any time.
- Charges are purchased (80 coins each) or awarded by login calendar.
- On `DailyStreak.Record`: if `IsNextDay` is false (gap of exactly 1 day) AND `freezeCharges > 0`:
  decrement charges, increment streak as if consecutive, mark the freeze as consumed in the result.
- Gaps of 2+ days break the streak regardless of freeze count (one freeze per missed day).
- Freeze does not stack multiple days; if the player misses 2 consecutive days, both charges are not
  consumed — only 1 is used per missed day, but a 2-day gap cannot be bridged by 1 charge.

**New `DailyStreakState` fields:**
```csharp
public readonly int FreezeCharges;  // 0–3
```

**Updated `DailyStreak.Record` signature:**
```csharp
public static (DailyStreakState next, bool freezeConsumed) Record(
    in DailyStreakState prev, string todayKey, int freezeCharges);
```
The tuple is returned rather than mutating; the caller decides whether to apply it.

**CoreConfig:**
```csharp
public int StreakFreezeMaxCharges = 3;
public int StreakFreezeCost = 80;          // coins per charge
```

**SaveData v2:**
```csharp
public int StreakFreezeCharges;  // 0–StreakFreezeMaxCharges
```

### 3.2 Login calendar

A 30-day revolving calendar. Each UTC day the player opens the app, they claim one reward slot.
"Claim" = the player explicitly taps "collect" on the calendar screen; no auto-claim (prevents
exploit of device clock manipulation awarding all days in one session). The calendar resets after
day 30 (cycles infinitely). Missing a day skips that slot permanently for the current cycle; the
player continues from the next day's slot on the next login.

**Important distinction from streak:** the login calendar advances on any app open, not just on
a completed Daily Seed run. It rewards *opening the app* as a softer daily engagement hook
alongside the harder streak hook.

**30-day reward table:**

| Day | Reward | Day | Reward | Day | Reward |
|---|---|---|---|---|---|
| 1 | 10 coins | 11 | 20 coins | 21 | 30 coins |
| 2 | 10 coins | 12 | 20 coins | 22 | 30 coins |
| 3 | 15 coins + 1 freeze charge | 13 | 25 coins | 23 | 35 coins |
| 4 | 15 coins | 14 | 25 coins | 24 | 35 coins |
| 5 | 15 coins | 15 | 25 coins | 25 | 40 coins |
| 6 | 20 coins | 16 | 30 coins | 26 | 40 coins |
| 7 | 50 coins (milestone) | 17 | 30 coins | 27 | 50 coins |
| 8 | 20 coins | 18 | 30 coins | 28 | 50 coins |
| 9 | 20 coins | 19 | 30 coins | 29 | 1 freeze charge |
| 10 | 20 coins | 20 | 30 coins | 30 | 100 coins (milestone) |

**Average per day: ~27 coins + ~0.07 freeze charges.**
**Total cycle value: ~815 coins + 2 freeze charges.**

**CoreConfig arrays:**
```csharp
public int[]    LoginCalendarCoins   = { 10, 10, 15, 15, 15, 20, 50, 20, 20, 20,
                                         20, 20, 25, 25, 25, 30, 30, 30, 30, 30,
                                         30, 30, 35, 35, 40, 40, 50, 50, 0,  100 };
public int[]    LoginCalendarFreezes = {  0,  0,  1,  0,  0,  0,  0,  0,  0,  0,
                                          0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
                                          0,  0,  0,  0,  0,  0,  0,  0,  1,  0 };
```
(Day 29 coins = 0; day 30 coins = 100. Arrays are 0-indexed; day N maps to index N-1.)

**Login calendar state in SaveData v2:**
```csharp
public int    LoginCalendarDay;        // 0 = never claimed; 1..30 = current position (1-indexed)
public string LoginCalendarLastClaim;  // UTC date "yyyy-MM-dd" of most recent claim
```

**Core service — `LoginCalendar` (new):**
```csharp
public static class LoginCalendar
{
    // Returns true if the player can claim today (not yet claimed today and day > 0 or first use).
    public static bool CanClaim(LoginCalendarState s, string todayKey);

    // Advance and return the reward. Does NOT mutate (returns new state + reward).
    public static (LoginCalendarState next, LoginCalendarReward reward) Claim(
        LoginCalendarState s, string todayKey, CoreConfig cfg);
}

public readonly struct LoginCalendarState
{
    public readonly int  Day;           // 1–30 (0 = not started)
    public readonly string LastClaim;   // "yyyy-MM-dd" or ""
}

public readonly struct LoginCalendarReward
{
    public readonly int Coins;
    public readonly int FreezeCharges;
    public readonly int DayNumber; // 1–30; for display
}
```

---

## 4. Weekly Missions and Achievements

### 4.1 Weekly missions

A set of 3 missions is offered each UTC week (Mon 00:00 → Sun 23:59 UTC). Each week draws 3 missions
from a pool using the date-derived seed (`DailySeed.ForDate` on the Monday of that week) so all
players worldwide see the same 3 missions. This means mission variety is shared and discussable,
reinforcing the daily-ritual pillar, while the fixed seed keeps the selection deterministic and
cheat-proof.

**Mission pool — 8 base templates:**

| ID | Description | Target | Reward (coins) |
|---|---|---|---|
| `m_floors_weekly` | Place N floors total across any runs this week | 200 | 100 |
| `m_perfects_weekly` | Land N Perfect drops total | 50 | 120 |
| `m_daily_runs` | Complete the Daily Seed run on N different days this week | 5 | 150 |
| `m_tall_tower` | Complete a single Endless run of N floors or more | 40 | 120 |
| `m_perfect_chain` | Achieve a perfect chain of N or more in a single run | 8 | 150 |
| `m_residents_weekly` | House N residents total across all runs | 400 | 100 |
| `m_district_runs` | Complete N runs in a specific district (district seeded by week) | 6 | 80 |
| `m_streak_days` | Maintain or reach a daily streak of N days by end of week | 5 | 200 |

The same template can appear at most once per week. The weekly draw picks 3 distinct templates
using the weekly seed.

**Parameters are fixed per template** (target and reward are part of the `MissionDefinition` SO
and are not randomized). Tunable in the SO without code changes.

**`MissionDefinition` ScriptableObject fields:**
```csharp
public string   missionId;          // stable, never renamed
public string   displayNameKey;     // loc key
public string   descriptionKey;     // loc key (uses {target} placeholder)
public int      target;             // quantity to complete the mission
public int      rewardCoins;        // coins on completion
public MissionMetric metric;        // enum: FloorsPlaced, PerfectDrops, DailyRunsCompleted,
                                    //        TowerHeight, PerfectChainLength, ResidentsHoused,
                                    //        DistrictRunsCompleted, StreakDays
public string   filterDistrictId;   // "" = any district; used by DistrictRunsCompleted
```

**Progress tracking in Core:**
Mission progress is a `Dictionary<string, int>` keyed by `missionId`, incremented by
`MissionTracker.Record(event)` at run-end. Progress is reset each week.

`MissionTracker` is a pure static class; the caller passes in the active mission set and current
progress map. It returns a new progress map. This is evaluated in `CityState.EndRun` after coins
are banked.

**Completion check:** `MissionTracker.IsComplete(missionId, progress, target)` — evaluated after
each run-end; if newly complete, the reward is banked to the wallet immediately and the completion
flag is set in `WeeklyMissionState`.

**Weekly reset:** when the current UTC week key changes (Unity layer checks Monday boundary),
`WeeklyMissionState` is reset. The completed-but-not-collected state is flushed — any uncollected
rewards are forfeited to prevent hoarding. (Rewards are collected immediately on completion to avoid
this: the system marks complete AND pays out in the same `EndRun` call.)

**`WeeklyMissionState` — new SaveData v2 fields:**
```csharp
public string            ActiveWeekKey;    // "YYYY-Www" ISO week key
public List<string>      ActiveMissionIds; // 3 ids drawn by week seed
public List<IntEntry>    MissionProgress;  // missionId → current progress int
public List<string>      CompletedMissionIds;
```

### 4.2 Mission progress events

`MissionTracker.Record` consumes a `MissionEvent` value type built from `RunResult` and passed
contextual data:

```csharp
public readonly struct MissionEvent
{
    public readonly int  FloorsPlaced;
    public readonly int  PerfectDrops;
    public readonly int  ResidentsHoused;
    public readonly int  MaxPerfectChain;   // from RunResult (needs adding to RunResult — see §7)
    public readonly bool IsDailyRun;
    public readonly string DistrictId;
    public readonly int  NewStreakValue;    // after Record; 0 if not a daily run
}
```

This type is built in Unity at run-end and passed to Core's `MissionTracker`. It has no engine
dependency (plain struct with ints, bool, string).

### 4.3 Achievements

Achievements are permanent milestones — they trigger once, pay out coins, and are persisted in
the save as a set of completed achievement IDs. They do not rotate and are never reset.

**Achievement list (~10):**

| ID | Name (loc key) | Metric | Threshold | Reward |
|---|---|---|---|---|
| `ach_towers_5` | `ach.first_five` | TotalTowers | 5 | 100 coins |
| `ach_towers_50` | `ach.city_builder` | TotalTowers | 50 | 200 coins |
| `ach_towers_200` | `ach.megacity` | TotalTowers | 200 | 300 coins |
| `ach_residents_1k` | `ach.thousand_residents` | TotalResidents | 1 000 | 150 coins |
| `ach_residents_10k` | `ach.ten_thousand` | TotalResidents | 10 000 | 400 coins |
| `ach_perfects_100` | `ach.sharpshooter` | TotalPerfects | 100 | 200 coins |
| `ach_streak_7` | `ach.week_streak` | LongestStreak | 7 | 200 coins |
| `ach_streak_30` | `ach.month_streak` | LongestStreak | 30 | 500 coins |
| `ach_height_50` | `ach.skycraper` | BestFloorCount | 50 | 250 coins |
| `ach_d3_complete` | `ach.three_districts` | DistrictsCompleted | 3 | 300 coins |

Thresholds map directly onto values already tracked in `LocalLeaderboard` (stat keys) or
`DailyStreakState.Longest`. Achievement evaluation is a pure scan: given current stat values and
the set of already-completed achievement IDs, return the set of newly-triggered achievements.

**`AchievementDefinition` ScriptableObject fields:**
```csharp
public string   achievementId;      // stable key
public string   displayNameKey;
public string   descriptionKey;
public AchievementMetric metric;    // enum: TotalTowers, TotalResidents, TotalPerfects,
                                    //        LongestStreak, BestFloorCount, DistrictsCompleted
public int      threshold;
public int      rewardCoins;
```

**Core class — `AchievementEvaluator` (new):**
```csharp
public static class AchievementEvaluator
{
    // Returns newly triggered achievement ids (not previously in completedIds).
    public static IReadOnlyList<string> Evaluate(
        AchievementSnapshot stats,
        IEnumerable<AchievementDefinition> definitions,
        IReadOnlyCollection<string> completedIds);
}

public readonly struct AchievementSnapshot
{
    public readonly int TotalTowers;
    public readonly int TotalResidents;
    public readonly int TotalPerfects;
    public readonly int LongestStreak;
    public readonly int BestFloorCount;     // max across all districts
    public readonly int DistrictsCompleted; // count of rewarded districts
}
```

`AchievementDefinition` is passed from Unity as a plain values struct (engine SO fields flattened)
so the Core evaluator remains Unity-free. The Unity side passes a parallel `AchievementInfo` struct
matching the above SO fields.

**SaveData v2:**
```csharp
public List<string> CompletedAchievementIds = new();
```

---

## 5. Cosmetics

### 5.1 Block skins

A block skin replaces the visual of ALL floor types (standard, balcony, premium) in a run.
It has no effect on floor type distribution, resident counts, scoring, or grading.

The equipped skin is applied by the Unity renderer (via `MaterialPropertyBlock` recolor or a
block mesh swap where art supports it). Core tracks ownership + equipped ID; rendering is Unity-side.

**`BlockSkinDefinition` ScriptableObject:**
```csharp
public string   skinId;            // stable
public string   displayNameKey;
public SkinRarity rarity;          // Common | Rare
public int      costCoins;         // 150 (Common) or 400 (Rare)
public string   unlockRequiresDistrictId;  // "" = always available
// Unity-side asset refs:
public AssetReference floorStandardMesh;
public AssetReference floorBalconyMesh;
public AssetReference floorPremiumMesh;
public AssetReference capMesh;
public Color[]  palette;           // 4 colors: wall, trim, roof, accent
```

**Starter skin set (launch):**

| ID | Name | Rarity | Cost | District gate |
|---|---|---|---|---|
| `skin_default` | Classic Brick | free | — | always |
| `skin_pastel` | Pastel City | Common | 150 | always |
| `skin_metal` | Steel Frame | Common | 150 | always |
| `skin_neon_glow` | Neon Glow | Rare | 400 | neon district filled |
| `skin_snow` | Arctic White | Rare | 400 | winter district filled |

District-gated skins create desirability and tie cosmetics to progression without paywalling them.

### 5.2 Crane skins

Replaces the visual of the crane rope, hook, and arm. No gameplay or timing effect.

**`CraneSkinDefinition` ScriptableObject:**
```csharp
public string   skinId;
public string   displayNameKey;
public SkinRarity rarity;
public int      costCoins;
public string   unlockRequiresDistrictId;
// Unity-side asset refs:
public AssetReference armMesh;
public AssetReference ropeTexture;
public AssetReference hookMesh;
public Color    primaryColor;
public Color    accentColor;
```

**Starter crane skin set:**

| ID | Name | Rarity | Cost | District gate |
|---|---|---|---|---|
| `crane_default` | Wood Arm / Hemp Rope | free | — | always |
| `crane_steel` | Steel Cable | Common | 200 | always |
| `crane_gold` | Gold Chain | Rare | 400 | always |
| `crane_neon` | Neon Flex | Rare | 400 | neon district filled |

### 5.3 Ownership model (Core)

Both skin types share the same ownership contract in Core:

```csharp
public static class CosmeticInventory
{
    // Returns true if the player can purchase (has coins, not already owned, district gate met).
    public static bool CanPurchase(string skinId, int skinCost, string requiredDistrictId,
        int playerCoins, IReadOnlyCollection<string> ownedIds,
        IReadOnlyCollection<string> rewardedDistrictIds);

    // Pure: returns new coin total + new owned set. Caller applies or discards.
    public static (int newCoins, IEnumerable<string> newOwned) Purchase(
        string skinId, int skinCost, int playerCoins, IReadOnlyCollection<string> ownedIds);
}
```

**SaveData v2 additions:**
```csharp
public List<string> OwnedBlockSkins   = new() { "skin_default" };
public string       EquippedBlockSkin = "skin_default";
public List<string> OwnedCraneSkins   = new() { "crane_default" };
public string       EquippedCraneSkin = "crane_default";
```

---

## 6. Resident Type Design (TODO-3 resolution)

**Decision: resident types are VISUAL-ONLY variants in Phase 4.**

VIP/premium residents that grant extra population or a score multiplier are explicitly rejected for
Phase 4 on two grounds:
1. It would create a second population-quality axis that competes with the Perfect-drop system.
   The current model is clean: more floors → more residents; Perfect drops → bonus residents.
   A VIP resident that also multiplies score would add RNG to the deterministic Core, or would
   require scripted VIP spawn rules that complicate the content pipeline.
2. Daily-seed fairness: if VIP resident spawn is run-seeded, it could cause two players with
   identical block placement to score differently based on which floor type they dropped — the
   definition of unfair in a shared-seed competition.

**Phase 4 resident behaviour:** the existing `ResidentsStandard / Balcony / Premium` values and
`PerfectBonus*` fields fully describe resident counts. District-specific resident *appearance*
(businessfolk, neon-punks, etc.) is controlled by `ResidentVariantSet` ScriptableObject already
defined in `DistrictDefinition`. No code change required — this is art/animation only.

**Future (Phase 6+ only):** a "Prestige" resident type could be added as a collectible unlock with
cosmetic-only effect on the city panorama view (e.g., a visible celebrity character standing on
a tower roof). It would have no numerical population or score value and no daily-fairness issue.

---

## 7. SaveData Schema v2

Schema version increments from 1 to 2. A `SaveMigration.Upgrade` step from v1→v2 populates all
new fields with their defaults (existing players start with level 0 upgrades, no skins except
defaults, day 1 login calendar, empty mission/achievement state).

**Full diff of new fields in SaveData:**
```csharp
// Upgrades
public int MagnetLevel    = 0;    // 0–4
public int SlowMoLevel    = 0;    // 0–4
public int CityBonusLevel = 0;    // 0–3

// Cosmetics
public List<string> OwnedBlockSkins    = new() { "skin_default" };
public string       EquippedBlockSkin  = "skin_default";
public List<string> OwnedCraneSkins    = new() { "crane_default" };
public string       EquippedCraneSkin  = "crane_default";

// Streak freeze
public int StreakFreezeCharges = 0;

// Login calendar
public int    LoginCalendarDay       = 0;
public string LoginCalendarLastClaim = "";

// Weekly missions
public string         ActiveWeekKey      = "";
public List<string>   ActiveMissionIds   = new();
public List<IntEntry> MissionProgress    = new();
public List<string>   CompletedMissionIds = new();

// Achievements
public List<string> CompletedAchievementIds = new();
```

`SaveData.CurrentVersion` increments to 2. `SaveMigration` gains a v1→v2 step (pure, NUnit-tested
with a v1 JSON fixture and an expected v2 output fixture).

**RunResult gains one new field** to support the `PerfectChainLength` mission metric:
```csharp
public readonly int MaxPerfectChain; // longest perfect chain in this run
```
This is derivable from existing `TowerRun`/`DropOutcome` data — `TowerRun` already tracks the
current chain; it needs to track the peak. Add `TowerRun.MaxPerfectChain` (int, updated on each
Perfect) and snapshot it into `RunResult.From(run)`.

---

## 8. Determinism / Fairness Rulings (Authoritative Table)

This is the single ruling table for `unity-engine-architect` and `gameplay-programmer` to enforce.

| System / Upgrade | Lives in Core? | NUnit tested? | Affects Daily Seed scoring? | Ruling |
|---|---|---|---|---|
| `CoinEarnCalculator` (extended with CityBonus) | Yes | Yes | No | SAFE |
| Magnet correction fraction | Yes (config) | Yes (unit) | **YES if applied** | SUPPRESS in daily — pass fraction=0 |
| Slow-Mo speed factor | Unity (presentation) | Integration | **YES if applied** | SUPPRESS in daily — factor=1.0 |
| Block skin / crane skin | Unity (renderer) | No | No | SAFE everywhere |
| City Bonus multiplier | Yes (config) | Yes | No (district reward only) | SAFE everywhere |
| Streak freeze logic | Yes | Yes | No | SAFE |
| Login calendar reward | Yes | Yes | No | SAFE |
| Mission progress tracking | Yes | Yes | No | SAFE |
| Achievement evaluation | Yes | Yes | No | SAFE |
| `MissionEvent.NewStreakValue` | Core struct | Yes | No | SAFE |
| `RunResult.MaxPerfectChain` | Yes | Yes | No (stat only) | SAFE |
| Weekly mission draw (seeded by week) | Yes | Yes | No (separate seed) | SAFE |

**Enforcement mechanism:** `RunContext.IsDaily` (bool) is passed alongside `CoreConfig` into
all Endless-only upgrade queries. The config exposes `MagnetFractions[level]` but the caller
passes `float magnetFraction = runContext.IsDaily ? 0f : cfg.MagnetFractions[magnetLevel]`.
No branching inside the grading path. The daily run is truly identical to any unupgraded run.

---

## 9. Core Implementation Order and API Contracts

This is the sequence the gameplay-programmer implements, test-first. Each item is a standalone
Core change that can be committed green before the next begins.

### Step 1 — CoinWallet service and upgrade state (Foundation)

New Core types needed before any spend logic can be tested:

```csharp
namespace Towerpolis.Core.Meta
{
    public readonly struct UpgradeState
    {
        public readonly int MagnetLevel;    // 0–4
        public readonly int SlowMoLevel;    // 0–4
        public readonly int CityBonusLevel; // 0–3
        public static UpgradeState Default => new(0, 0, 0);
    }

    public static class UpgradeService
    {
        // Returns (success, newCoins, newLevel) — does NOT mutate CityState.
        public static (bool ok, int newCoins, int newLevel) TryPurchase(
            string upgradeId, int currentLevel, int[] costs, int maxLevel, int currentCoins);

        // Returns the effective magnet fraction (0f if isDaily regardless of level).
        public static float GetMagnetFraction(int level, CoreConfig cfg, bool isDaily);

        // Returns the effective slow-mo factor (1.0f if isDaily regardless of level).
        public static float GetSlowMoFactor(int level, CoreConfig cfg, bool isDaily);
    }
}
```

**Key NUnit tests for Step 1:**
- `UpgradeService_TryPurchase_SucceedsWhenAffordable`: level 0→1, sufficient coins, correct deduction.
- `UpgradeService_TryPurchase_FailsWhenInsufficient`: not enough coins → ok=false, coins unchanged.
- `UpgradeService_TryPurchase_FailsAtMaxLevel`: already at max → ok=false.
- `UpgradeService_MagnetFraction_IsZeroWhenDaily`: any level > 0 + isDaily=true → returns 0f.
- `UpgradeService_SlowMoFactor_IsOneWhenDaily`: any level > 0 + isDaily=true → returns 1.0f.

### Step 2 — CoinEarnCalculator extension (City Bonus)

Extend `CoinEarnCalculator.RunCoins` to accept `CityBonusLevel` and apply the multiplier only when
district-completion reward is non-zero:

```csharp
public static int RunCoins(in RunResult result, CoreConfig cfg,
    bool districtCompletedNow, int baseDistrictRewardCoins, int cityBonusLevel);
```

The existing overload without bonus params stays for backward compat and delegates to the extended
one with `districtCompletedNow=false`. This is a pure signature extension; no change to the existing
happy-path calculation.

**Key NUnit tests:**
- `CoinEarn_NoBonusLevel_MatchesExistingBehaviour`: existing Phase 3 test cases must still pass.
- `CoinEarn_CityBonusLevel3_AppliesMultiplierToDistrictReward`: 500 coins × 1.50 = 750.
- `CoinEarn_CityBonus_DoesNotAffectFloorOrPerfectCoins`: floor/perfect coins unchanged regardless of level.
- `CoinEarn_CityBonus_NotAppliedWhenDistrictNotCompleted`.

### Step 3 — Streak freeze (extend DailyStreak)

Add `FreezeCharges` to `DailyStreakState` and update `DailyStreak.Record`:

```csharp
// Updated Record (replaces existing — callers must be updated):
public static (DailyStreakState next, bool freezeConsumed) Record(
    in DailyStreakState prev, string todayKey, int freezeCharges = 0);
```

**Key NUnit tests:**
- `Streak_FreezeConsumedWhenOneDayGap`: prev.LastDate yesterday, today's date, charges=1 → streak maintained, freezeConsumed=true, FreezeCharges=0.
- `Streak_FreezeNotConsumedWhenConsecutive`: same-day or next-day → freezeConsumed=false.
- `Streak_FreezesDoNotBridgeTwoDayGap`: two-day gap, charges=2 → streak resets to 1, freezeConsumed=false (only one missed day can be bridged per gap, and a 2-day gap is > 1 missed day).
- `Streak_FreezesDoNotGoNegative`: charges=0, one-day gap → streak resets, freezeConsumed=false.

### Step 4 — LoginCalendar

Implement the new `LoginCalendar` static class as specified in §3.2.

**Key NUnit tests:**
- `LoginCalendar_CanClaim_TrueOnFirstUse`: state with Day=0, any todayKey → CanClaim=true.
- `LoginCalendar_CanClaim_FalseIfAlreadyClaimedToday`: LastClaim==todayKey → CanClaim=false.
- `LoginCalendar_Claim_AdvancesDayAndReturnsCorrectReward`: day 7 → 50 coins, 0 freezes.
- `LoginCalendar_Claim_Day3_ReturnsFreeze`: day 3 → 15 coins, 1 freeze charge.
- `LoginCalendar_Claim_CyclesAfterDay30`: state at Day=30, claim → next state Day=1.
- `LoginCalendar_Claim_DoesNotClaimTwiceSameDay`: second call same todayKey → CanClaim=false, no reward.

### Step 5 — MissionTracker + weekly draw

```csharp
namespace Towerpolis.Core.Meta
{
    public static class MissionTracker
    {
        // Returns updated progress map (new Dictionary; does not mutate input).
        public static Dictionary<string, int> Record(
            MissionEvent evt,
            IEnumerable<MissionInfo> activeMissions,
            IReadOnlyDictionary<string, int> currentProgress);

        // True if progress[missionId] >= target.
        public static bool IsComplete(string missionId, IReadOnlyDictionary<string, int> progress, int target);

        // Draws 3 distinct mission ids from the pool using the week's seed.
        public static List<string> DrawWeeklyMissions(
            IEnumerable<string> allMissionIds, ulong weekSeed, int count = 3);
    }

    // Plain values version of MissionDefinition SO — passed from Unity, no engine dep.
    public readonly struct MissionInfo
    {
        public readonly string MissionId;
        public readonly MissionMetric Metric;
        public readonly int    Target;
        public readonly int    RewardCoins;
        public readonly string FilterDistrictId; // "" = any
    }

    public enum MissionMetric
    {
        FloorsPlaced, PerfectDrops, DailyRunsCompleted, TowerHeight,
        PerfectChainLength, ResidentsHoused, DistrictRunsCompleted, StreakDays
    }
}
```

**Key NUnit tests:**
- `MissionTracker_Record_IncrementsFloorsPlaced`: event with FloorsPlaced=25, active `m_floors_weekly` → progress["m_floors_weekly"] = 25.
- `MissionTracker_Record_IgnoresMetricNotInActiveSet`: no active perfect mission → no perfect key in result.
- `MissionTracker_IsComplete_TrueAtThreshold`: progress=200, target=200 → true.
- `MissionTracker_DrawWeeklyMissions_Returns3Distinct`: always 3, never duplicate.
- `MissionTracker_DrawWeeklyMissions_DeterministicForSameSeed`: same weekSeed → same list.
- `MissionTracker_DistrictFilter_OnlyCountsMatchingDistrict`: `DistrictRunsCompleted` with filterDistrictId="neon", event with DistrictId="downtown" → no increment.

### Step 6 — AchievementEvaluator

Implement as specified in §4.3. Pure function, no mutation.

**Key NUnit tests:**
- `Achievement_TriggerOnThreshold`: stats.TotalTowers=5, `ach_towers_5` threshold=5, not yet completed → returns ["ach_towers_5"].
- `Achievement_DoesNotRetrigger`: completedIds contains "ach_towers_5" → not returned again.
- `Achievement_DoesNotTriggerBelowThreshold`: TotalTowers=4, threshold=5 → empty list.
- `Achievement_MultipleTriggeredAtOnce`: TotalTowers=50 from 0, both `ach_towers_5` and `ach_towers_50` in result.

### Step 7 — SaveData v2 migration

Add a `v1→v2` migration step to `SaveMigration`:

```csharp
static SaveData MigrateV1ToV2(SaveData v1)
{
    // Copy all existing v1 fields unchanged.
    // Populate all v2-new fields with their spec-defined defaults (see §7).
    // Set SchemaVersion = 2.
}
```

**Key NUnit tests:**
- `SaveMigration_V1ToV2_PreservesAllV1Fields`: coins, gems, streak, districts, leaderboard all carry through.
- `SaveMigration_V1ToV2_NewFieldsAtDefaults`: MagnetLevel=0, OwnedBlockSkins=["skin_default"], etc.
- `SaveMigration_V1ToV2_VersionIs2`.
- Golden fixture: commit a `SaveDataV1.json` test asset; migration output must match `SaveDataV2_expected.json`.

---

## 10. Open Questions (Deferred)

**OQ-P4-01:** `RunResult.MaxPerfectChain` — requires `TowerRun` to track the running peak.
Confirm with `gameplay-programmer` whether `TowerRun.MaxPerfectChain` already exists or needs to be
added alongside the `PerfectChainLength` mission metric implementation.

**OQ-P4-02:** Weekly mission reset timing — forfeiture of uncollected rewards is the spec ruling.
If playtesting shows this frustrates players (e.g., player is at 95% progress on Sunday night),
revisit with a 24-hour grace window in Phase 5. Confirm with `game-director`.

**OQ-P4-03:** Slow-Mo hold gesture UX (hold vs. swipe vs. dedicated button) — handed to `ui-ux-designer`.
Core only cares about the sampled offset at release; the hold interaction is UI.

**OQ-P4-04:** Cosmetic bundle pricing — grouping block skin + crane skin as a bundle discount is a
Phase 7 concern (IAP), not a coin economy concern. Noted for the monetization spec author.

---

*All tunables are expressed as `CoreConfig` fields or ScriptableObject data — balance changes require
no code changes. Pillars served: Pillar 1 (upgrade tracks give a reason to perfect-drop); Pillar 2
(upgrades are Endless-only so daily difficulty remains fair and learnable); Pillar 3 (skins,
missions, and achievements give daily return reasons beyond population). Implementation entry point:
Step 1 of §9 (UpgradeService + UpgradeState) — run `dotnet test` green before proceeding to Step 2.*
