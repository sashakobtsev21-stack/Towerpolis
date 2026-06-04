# 0007 — Meta: City persistence, daily seed, districts & local leaderboards (Core-modeled, GPGS-ready)

Status: accepted
Date: 2026-06-04

Context: Phase 3 adds the **"Build City" meta** on top of the proven core loop: a persistent 3D city
where each completed run deposits a tower, a **daily-seed** mode, **districts** (themed building +
resident + skybox + palette + music packs), and **SOLO / local leaderboards**. Online
(Google Play Games Services — GPGS sign-in, cloud save, friend/global boards) is **deferred to a
later phase** (GDD §4.4, §4.7), but the seams must be designed now so it plugs in without a rewrite.

This must fit the existing architecture: the deterministic, testable rules live in **`Towerpolis.Core`**
(Unity-free, `noEngineReferences:true`, `netstandard2.1`, dual-tested via `dotnet test` + Unity Test
Runner — [0002](0002-deterministic-core-no-physx-scoring.md), [0005](0005-core-dual-test-harness.md));
the Unity layer consumes Core and owns rendering, I/O and assets. Population/economy/seed math is
**never** derived from PhysX (cross-device fairness, anti-cheat). District art is delivered via
**Addressables** ([0004](0004-addressables-for-content.md)); content stays within the perf budget
([0003](0003-perf-budget.md): <200 draw calls, pooled, shared materials).

> The `game-designer`'s `docs/game/meta-spec.md` is being written in parallel and was **absent** when
> this ADR was authored. This ADR derives from **GDD §4.1–4.4, §4.7** and notes assumptions inline as
> **[A]**; the meta-spec refines *tunables/curves* (costs, fill goals, resident rules), not these seams.

## Decision

### 1. Core vs Unity split for the meta
Extend Core with a new **`Towerpolis.Core.Meta`** namespace (same assembly) holding the deterministic,
unit-testable meta models. The Unity layer owns everything engine-bound.

**In `Towerpolis.Core.Meta` (pure C#, NUnit-tested, no `UnityEngine`, reads no clock):**
- **`TowerRecord`** — the frozen, deterministic result of one completed run: floor count, per-`FloorType`
  counts, residents, `RunScore`, top grade/perfect-chain, `runSeed`, district id, UTC day stamp. Built
  from the existing `TowerRun`/`DropOutcome` at run-end. The unit deposited into the city.
- **`CityState`** — the aggregate save model: a list of placed `TowerRecord`s per district + plot index,
  wallet (coins/gems), unlocked districts, per-district population & fill progress.
- **`Population`** — sums residents across a district / the whole city (the meta score). The deposit math
  reuses the resident values already in `Scoring.BaseResidents` / `CoreConfig`; the meta never re-derives
  residents from the scene.
- **`Economy`** — currency math: run→coins payout, district-completion / streak rewards, unlock-cost
  affordability and spend. Pure functions over a wallet, golden-tested for no exploit (Phase-4 gate).
- **`DistrictProgression`** — given `CityState` + a static district table → which districts are unlocked,
  whether a district's **fill goal** is met, what the next unlock costs. The *rules*; the *content* (art,
  exact numbers) is Unity-side `DistrictDefinition` data passed in as plain values.
- **`DailyRun`** — derives the daily run inputs from a UTC date via the existing `DailySeed.ForDate` →
  `RunSeeds`. The date is an **input** (Core reads no clock).
- **`LocalLeaderboard`** — in-memory record-keeping over a list of `ScoreEntry` (insert, sort, top-N,
  per-board-id, daily/weekly window keys). Deterministic; no platform, no I/O.
- **`SaveData`** + **`SaveMigration`** — the versioned, serializable save shape and pure version→version
  upgrade functions (see §2). Plain DTOs so the I/O is engine-side but the *schema and migrations* are
  unit-tested.

**In the Unity layer (`Towerpolis.Game`, MonoBehaviour/SO/asset-bound):**
- 3D **city rendering & plot layout**, camera, the deposit animation, the "My City" panorama.
- **Persistence I/O** — read/write/atomic-swap of the save file (Core defines the bytes, Unity moves them).
- **`DistrictDefinition`** ScriptableObjects (the district *content*, §3) and Addressables loading.
- **Scene flow** (gameplay ↔ city), `ILeaderboardService` platform implementations (§5), and turning the
  system clock into a UTC date for Core (§6).

Boundary stays one-directional: Unity → Core, enforced by the asmdef as today.

### 2. Save / persistence
- **Model in Core, I/O in Unity.** `SaveData` is a plain serializable DTO defined in `Towerpolis.Core.Meta`
  so its shape and migrations are deterministic and unit-tested; Unity performs the file read/write.
- **Format: JSON.** Human-readable for debugging/support, diff-able, trivially version-tolerant, and
  serializes from a pure DTO with no Unity types. Serializer is **System.Text.Json** in standalone Core
  tests and on device (Core stays Unity-free; we do **not** depend on `JsonUtility`). **[A]** binary/MessagePack
  is rejected for v1 — save size is tiny (tens of towers) and debuggability beats bytes here.
- **Location: `Application.persistentDataPath/save/city.json`** (Unity-side path; Core never sees it).
- **Versioning + migration:** `SaveData.SchemaVersion` (int, starts at 1). On load, Unity hands the raw
  JSON to Core, which deserializes, runs `SaveMigration.Upgrade(fromVersion → current)` as a chain of pure
  steps, and returns a current-version `CityState`. **Forward-only**, each step unit-tested with a golden
  fixture of the old shape → never silently lose a player's city.
- **Atomic write / corruption safety:** write to `city.json.tmp`, `flush`, then atomic rename over
  `city.json`; keep one rolling backup `city.bak`. On load, if the primary fails to parse, fall back to the
  backup; if both fail, start a fresh guest city and surface a non-destructive notice (never crash-loop).
- **Guest by default (GDD §4.7):** no login gate, ever. First launch creates a **local** guest `CityState`;
  the game is fully playable offline. Show the one-time "guest progress is on this device only" notice with a
  later "sign in to save" affordance. Cloud save is a **later** GPGS concern that syncs the *same* `SaveData`
  bytes — the format is designed cloud-ready now (versioned, self-contained, no device-local references).

### 3. `DistrictDefinition` ScriptableObject (Unity, data not code)
A district is **authored as data** so new districts are content drops, not releases (GDD §4.1,
[0004](0004-addressables-for-content.md)). Schema (Unity-side SO, Addressable):
- `districtId` (stable string key — the save & leaderboard join key; **never** renumber).
- `displayNameKey` (Localization key, not a literal — Phase-5 rule).
- **Building/palette set** (AssetReferences to the building/floor mesh + material variant set + color palette).
- **Skybox** + **music** (AssetReferences; per-district atmosphere, GDD §4.1/§4.9).
- **Resident set** (AssetReference to the parachuting-resident variant pack).
- `fillGoal` (population/grid target to complete the district).
- `unlockCost` (coins/gems) + `unlockRequiresDistrictId` (ordering).
- `leaderboardId` (the board this district submits to — local now, GPGS id later).
- `plotLayout` ref (grid dimensions / plot anchors for deposited towers).

Core's `DistrictProgression`/`Economy` receive these as **plain values** (id, fillGoal, cost), never the SO
itself — so the rules stay Unity-free and the numbers stay **server-gatable via Remote Config** later.

### 4. City scene / view architecture
- **Separate additive scene** `City` (loaded additively over a persistent bootstrap scene), distinct from the
  `Gameplay` scene. Keeps the two cameras/lighting/render setups clean and lets the city stream a district's
  Addressable content without dragging gameplay assets along. Scene flow is a small state machine in the
  bootstrap scene.
- **Deposited towers are cheap proxies, not replayed runs.** Each `TowerRecord` renders as a **pooled,
  low-poly tower proxy** built from its floor counts — **GPU-instanced** per shared material, recolored via
  `MaterialPropertyBlock` (no new materials), honoring [0003](0003-perf-budget.md). Off-screen plots cull;
  distant districts use an even cheaper LOD/impostor. No physics, no per-floor GameObjects for placed towers.
- **View reads Core, never writes gameplay:** the city view is a **pure projection of `CityState`** — it asks
  Core for population, unlocked districts and the tower list and renders them; it never recomputes score.

### 5. Leaderboard abstraction (local now, GPGS later)
Define **`ILeaderboardService`** in the Unity layer (it touches platform SDKs, so it is **not** in Core):
```
SubmitScore(boardId, score) · GetTopEntries(boardId, window) · GetPlayerEntry(boardId)
```
- **`LocalLeaderboardService`** (now): backed by Core's `LocalLeaderboard` record-keeping + the save file.
  Satisfies the Phase-3 gate (solo per-district + daily/weekly-window boards) fully offline.
- **`GpgsLeaderboardService`** (later): same interface over Google Play Games Services; iOS Game Center later.
- All meta code depends on the **interface**, selected at composition root. Swapping in GPGS touches one
  binding, not the meta. `boardId` strings on `DistrictDefinition`/`DailyRun` map 1:1 to GPGS board ids later.

### 6. Daily-seed wiring
- Unity reads **`DateTime.UtcNow`** (UTC, so the "day" is global — GDD §4.2) and passes Y/M/D to Core.
- Core `DailySeed.ForDate(y,m,d)` → the stable 64-bit day seed → `RunSeeds.BlockRng/SwingRng` (existing,
  salted per stream). Identical date → identical seed → identical run on every device (ADR-0002, Phase-3 gate).
- The **daily run is one reproducible run per UTC day**: its `TowerRecord` carries the day stamp; re-entering
  the same day reuses the same seed; the daily board keys on the UTC day. A future Remote Config "seed
  override" can substitute the seed without touching the Core derivation path.

## Consequences
- Phase-3 gate is met **offline**: daily seed is cross-device identical (Core determinism), local
  leaderboards read/write, and the city persists across sessions with corruption-safe atomic saves.
- Online is a **drop-in**: cloud save reuses the versioned `SaveData`; boards swap behind
  `ILeaderboardService`; board/district ids are already stable join keys. No meta rewrite when GPGS lands.
- Population/economy/progression/seed logic is **unit-tested in `dotnet test`** with no editor or device —
  exploits and migration regressions are caught pre-commit (Phase-4 economy gate, §1).
- City rendering is constrained to instanced low-poly proxies up front, protecting the draw-call budget as
  the city grows — a deliberate limit on per-tower visual fidelity in the panorama.
- Costs: a save-migration discipline (every schema change needs a tested upgrade step + old-shape fixture),
  and a second render path (proxy towers) to build and budget.

## Alternatives rejected
- **Model the city in MonoBehaviours / `JsonUtility`** — pulls deterministic meta logic into Unity, breaks the
  Core/Unity boundary and the standalone `dotnet test` loop, and couples the save schema to engine types
  (blocks cloud reuse). Rejected per 0002/0005.
- **`PlayerPrefs` for the save** — not atomic, size/robustness-limited, not a portable document for future
  cloud sync. Fine only for tiny settings, not the city.
- **Build GPGS/online now** — out of scope this phase (GDD §4.4 solo-first); adds a login/online dependency to
  a loop that must stay frictionless and offline-playable. We build the *seams*, not the integration.
- **Replay each deposited run live in the city** — far over the draw-call/CPU budget; the city only needs a
  *projection* of each `TowerRecord`, not its simulation.
- **Binary/opaque save format** — saves CPU/bytes we don't need at this scale while costing debuggability,
  support, and version tolerance. Revisit only if save size becomes a real constraint.
