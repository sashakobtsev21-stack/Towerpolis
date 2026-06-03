# Towerpolis — MVP Core-Loop Feel Specification

*Owner: `game-designer`. Status: v1.1 — ratified by `game-director`; locked for Phase 2. R1, R2, OQ-01, OQ-02, OQ-11 resolved inline.*
*Consumers: `gameplay-programmer` (all systems), `physics-programmer` (fall + wobble), `vfx-artist` (juice), `audio-designer` (SFX), `game-qa-engineer` (fun-gate test plan).*
*Last updated: 2026-06-03.*

---

## 0. Governing Constraints (non-negotiable)

Before any number below: every grading and scoring computation must be implementable as pure C# arithmetic in `Towerpolis.Core` (no `UnityEngine` references, no `Random`, no float non-determinism from PhysX). All tunables are expressed as `float` constants in a `CoreConfig` ScriptableObject-backed data record so they can be adjusted in playtest without code changes. ADR-0002 is the law.

Block dimensions (fixed, from committed art): **2.0 m wide × 2.0 m deep × 1.5 m tall**, bottom-center pivot. "Width" in all threshold formulas = 2.0 m. The foundation block (`Base_Ground` / `Base_Ground_2`) is a fixed anchor; grading starts from the first stacked floor.

---

## 1. Crane and Swing

### 1.1 Swing model

The crane swings the pending block **sinusoidally** (cosine position, like a pendulum) left–right above the current tower top. This is not a linear ping-pong; the block accelerates through center and slows at the edges, creating natural anticipation windows.

```
x(t) = HalfArc * cos(2π * t / Period)
```

`x = 0` is directly above the tower center. Positive x = right. The full swing range is `2 * HalfArc` wide.

### 1.2 Starting parameters

| Parameter | Start value | Min | Max | Notes |
|---|---|---|---|---|
| `SwingHalfArc` | **1.4 m** | 0.8 m | 2.0 m | 0.7× block width. Wide enough to require timing skill; not so wide as to feel impossible. |
| `PeriodFloor1` | **2.8 s** | 2.0 s | 4.0 s | Slow enough for new players to read; Tower Bloxx original was ~2.5–3 s. |
| `PeriodMinClamp` | **2.0 s** | 1.6 s | 2.8 s | Floor on how fast the swing can ever get (never unfair). |
| `PeriodRampFactor` | **0.012 s/floor** | 0.006 | 0.020 | Period decreases by this much per floor above floor 1. |

**Period curve** (deterministic, computed in Core):

```csharp
float Period(int floor) =>
    Mathf.Max(PeriodMinClamp, PeriodFloor1 - PeriodRampFactor * (floor - 1));
```

At floor 1: 2.8 s. At floor 20: 2.8 − 0.012×19 = **2.57 s**. At floor 50: 2.8 − 0.012×49 ≈ **2.21 s**. At floor 100: clamped to **2.0 s**. The ramp is "minimal" as specified in GDD §3.1 — the period barely changes; the tension comes from the wobble, not speed. The clamp prevents the swing from ever becoming reflex-unfair.

Design note: the REAL rising difficulty is the wobble (§5), not the swing speed. The swing is a learnable rhythm; the wobble is the physical pressure.

### 1.3 Swing phase from RNG seed

Each run's crane sequence is seeded by `XorShiftRng` (ADR-0002). The per-run seed determines:
- Initial phase offset `φ₀ ∈ [0, 2π)` so the first block is not always center-ready.
- Per-floor phase-continuation (the swing is continuous across floors; only the block swapped out).

The seed for MVP is a hardcoded `uint64` per build (`SEED_MVP = 0xDEADBEEF_CAFEF00D`). Daily seed variant is post-MVP.

### 1.4 Drop-release on tap

| Parameter | Value | Notes |
|---|---|---|
| `InputLatencyBudget` | **≤ 50 ms** | Measured from touch-up event to Rigidbody detach. This is Pillar 1. Must be verified on device. |
| `ReleaseModel` | **Instant detach** | Block detaches on the exact frame the tap is registered. No animation before detach. Velocity at detach = 0 (the block was stationary in crane's local frame); gravity takes over immediately. |

The crane arm itself plays a short recoil animation (a spring-back visual only, not affecting block physics). This is handled in Unity layer, not Core.

### 1.5 Block-type sequence (deterministic, from seed) — game-director ruling OQ-02

The sequence of floor types is a **weighted random draw whose randomness source is fully derived from the run seed**, so the same seed produces the same blocks on every device, forever (the literal enabler of the Phase-3 daily seed and of cross-device anti-cheat).

**Two independent RNG streams** (game-director ruling OQ-11) — advancing the swing must never perturb the block sequence and vice-versa, so each gets its own `XorShiftRng` derived from the run seed via a **distinct SplitMix64 salt** (named constants in Core):

```csharp
const ulong SALT_BLOCK = 0xB10C5EEDB10C5EEDUL;  // block-type stream
const ulong SALT_SWING = 0x5717_6505_5717_6505UL; // swing-phase stream
var blockRng = new XorShiftRng(SeedMix(runSeed ^ SALT_BLOCK));
var swingRng = new XorShiftRng(SeedMix(runSeed ^ SALT_SWING)); // SeedMix = SplitMix64
```

**Gameplay types = three only** `{Standard, Balcony, Premium}` (floors 1..N). Floor 0 is always the fixed base anchor (not drawn, scored 0). `Base_Ground_2` and `Floor_Balcony_2` are **cosmetic variants, not gameplay types** — never weighted entries; if a Balcony is drawn, a cosmetic sub-roll picks `Balcony` vs `Balcony_2` mesh (same 150 score, same 3 residents).

**Weights (integer, total 100):**

| Type | Weight | ~Prob |
|---|---|---|
| `Standard` | **70** | 70% |
| `Balcony` | **22** | 22% |
| `Premium` | **8** | 8% |

Draw = `int r = blockRng.NextInt(0, 100)` → cumulative lookup (`<70 Standard`, `<92 Balcony`, else `Premium`). Two deterministic guards (both in Core, both seed-reproducible):

1. **Floors 1–3 are forced `Standard`** (override the draw) — new players learn on the simplest floor; a run never opens with a misread Premium.
2. **No more than 2 `Premium` in any rolling window of 5** — if a draw would violate the cap, advance `blockRng` and redraw (deterministic given the seed). Prevents a seed from coughing up a Premium cluster that warps shared-daily-seed fairness.

**Golden test (mandatory, ADR-0002):** lock the first ~50 entries of the sequence for `SEED_MVP = 0xDEADBEEFCAFEF00D` so an algorithm change can never silently shift a historical daily seed.

---

## 2. Drop and Contact Detection

### 2.1 Fall parameters

| Parameter | Value | Min | Max | Notes |
|---|---|---|---|---|
| `GravityScale` | **2.5** | 1.5 | 4.0 | Applied to the Rigidbody while falling. Gives a ~0.7–0.9 s fall from typical height. Snappier than real gravity; reads as "purposeful". |
| `TargetFallTime_10floors` | **~0.8 s** | — | — | At 10-floor height (~15 m above base), the block should hit in approximately 0.8 s at GravityScale 2.5. Verify in Unity layer. |

Fall time formula (for reference): `t = sqrt(2h / (g * GravityScale))` where `g = 9.81 m/s²`.

### 2.2 Contact detection (deterministic grading trigger)

"Contact" is defined as the frame the falling block's **bottom face Y position first reaches or passes** the top face Y of the current tower top block. This is a scripted check in the Unity layer (not a physics collision callback), but it fires the deterministic Core grade immediately.

The Unity layer provides to Core:
- `float droppedX` — the block's X world position at the contact frame, measured at **block bottom-center**.
- `float droppedZ` — block Z world position at contact frame.
- `float towerTopX`, `float towerTopZ` — the center of the current tower top block's top face.

Core computes:
```csharp
float offsetX = droppedX - towerTopX;
float offsetZ = droppedZ - towerTopZ;
float offset = Mathf.Sqrt(offsetX * offsetX + offsetZ * offsetZ);  // radial offset
```

**For MVP, the tower and crane are strictly aligned on the Z axis** (single left-right swing, no Z wobble). Therefore `offsetZ ≈ 0` and grading uses `|offsetX|` only. The radial formula is stubbed for later.

---

## 3. Grading Bands

**[R2 — game-director ratification]** Thresholds are fractions of **`currentTopWidth`** — the effective width of the surface being landed on (i.e. the top block of the tower) — **not** the constant 2.0 m. This is a Stack-style model: the falling block matches the current top width, the overlap is `currentTopWidth − |offsetX|`, and the surviving overlap becomes the next `currentTopWidth` (the tower narrows as you slice). Using a constant 2.0 m would let a "Good" be scored on a 0.4 m sliver where the block is visibly hanging in air — the cardinal Pillar-2 sin. `currentTopWidth` starts at `InitialBlockWidth = 2.0 m` (the base top) and shrinks with every Good/Sloppy slice.

```csharp
float ratio = Abs(offsetX) / currentTopWidth;   // honest at every width
```

| Grade | Condition (`r = |offsetX| / currentTopWidth`) | At full 2.0 m top | Meaning |
|---|---|---|---|
| **Perfect** | `r ≤ 0.10` | `|offsetX| ≤ 0.20 m` | Dead center. No slice; `currentTopWidth` unchanged. Tower straightens by a corrective nudge (§3.1). |
| **Good** | `0.10 < r ≤ 0.30` | `0.20 m < |offsetX| ≤ 0.60 m` | Acceptable overlap. Overhang sliced; top narrows. Small lean added. No strike. |
| **Sloppy** | `0.30 < r ≤ 0.50` | `0.60 m < |offsetX| ≤ 1.00 m` | Barely overlaps. Overhang sliced; top narrows. Larger lean added. Costs a strike (tunable, §4). |
| **Miss** | `r > 0.50` | `|offsetX| > 1.00 m` | Less than half overlaps. Block tumbles off; `currentTopWidth` unchanged. Costs a strike. |

**Design rationale:** the bands scale with the surface, so they stay "fair" at every width — a Perfect on a narrow tower demands proportionally finer precision, which reads as *earned difficulty*, not random failure (Pillar 2). At the full 2.0 m base, the Perfect window is ±20 cm — tight but achievable; Good is generous (to ±60 cm); Sloppy is the danger zone; Miss is no meaningful structural contact.

### 3.1 Perfect "nudge" (straighten)

On a Perfect, the deterministic Core reduces the accumulated lean offset by `PerfectLeanCorrectionFraction = 0.25` (i.e., 25% of the current lean is corrected). This rewards chains of Perfects with a visibly straightening tower, delivering the "tower recovers" feel without physics.

| Parameter | Value | Range |
|---|---|---|
| `PerfectLeanCorrectionFraction` | 0.25 | 0.10 – 0.40 |

### 3.2 Overhang slice rule (Good and Sloppy)

On Good or Sloppy, the non-overlapping portion of the dropped block is "sliced off" and falls away. The surviving overlap **becomes the next `currentTopWidth`** (the tower narrows). The falling block matches the current top width, so:

```csharp
float overlap = currentTopWidth - Abs(offsetX);              // metres of overlap
currentTopWidth = Max(overlap, MinBlockWidth);               // next surface width (Perfect/Miss leave it unchanged)
```

| Parameter | Value | Range |
|---|---|---|
| `MinBlockWidth` | **0.4 m** | 0.2 m – 0.8 m | The narrowest a block can ever be sliced to. Below this, the next drop is extremely punishing — set generously. |

The sliced "roof" fragment spawns as a visual-only kinematic object (no Rigidbody grading interaction) and falls/fades out. It is pooled.

**Lean accumulation:** each non-perfect drop adds lean to the tower. Lean is a single `float leanOffset` stored in Core state.

```csharp
// Good
leanOffset += offsetX * GoodLeanFactor;   // GoodLeanFactor = 0.15

// Sloppy
leanOffset += offsetX * SloppyLeanFactor; // SloppyLeanFactor = 0.35
```

| Parameter | Value | Range | Notes |
|---|---|---|---|
| `GoodLeanFactor` | 0.15 | 0.05 – 0.30 | Good drops barely tilt the tower. |
| `SloppyLeanFactor` | 0.35 | 0.15 – 0.60 | Sloppy drops noticeably shift the lean. |

Lean is consumed by the wobble system (§5) to bias the sway direction. It is also used to scale camera shake.

### 3.3 Miss (tumble)

On a Miss, the block is not welded. It tumbles off with a physics-driven fall (Rigidbody stays active, no freeze). A miss strike is registered in Core state. The tower is not modified. The effective width of the "top block" remains what it was before this drop.

---

## 4. The 2-Strike Rule

**Decision: CUMULATIVE misses, not consecutive.**

**Justification:** Consecutive-miss rules let a player survive indefinitely by alternating one good drop with one sloppy drop. That removes all genuine tension from Sloppy drops and undermines Pillar 2 ("fair, rising tension"). Cumulative misses mean every strike counts; there is no safe "release valve" between strikes. However, a Good drop does NOT clear strikes — only Perfects provide any relief (see Perfect lean correction above), but they do not forgive a strike. This is deliberately tense: once you've taken a strike, you are in danger permanently.

**The exact rule** (game-director ruling OQ-01: Sloppy costs a strike; exposed as one tunable, default `true`, the escape hatch if soft-launch shows median players eating Sloppy strikes — but it ships `true`):

```csharp
int missStrikes = 0;             // incremented by Sloppy (if enabled) and Miss
const int StrikeLimit = 2;       // run ends when missStrikes >= StrikeLimit
bool SloppyCostsStrike = true;   // [TUNE] OQ-01 — default true, do not soften pre-data

// On Sloppy grade:
if (SloppyCostsStrike) { missStrikes += 1; if (missStrikes >= StrikeLimit) TriggerTopple(); }

// On Miss grade:
missStrikes += 1;
if (missStrikes >= StrikeLimit) TriggerTopple();
```

This means:
- First bad drop (Sloppy or Miss): survive with a narrower/leaning tower.
- Second cumulative bad drop of any kind: the tower topples. Run ends.
- Good drops never add a strike. Perfects never clear a strike.

**Tower topple:** triggered by the second cumulative strike. The scripted wobble plays a collapse animation (amplitude ramps to extreme over `ToppleWobbleDuration = 0.8 s`), then all welded blocks are re-enabled as Rigidbodies simultaneously and explode outward in a cartoony spray. This is a visual-only event; the score is already locked in Core before it plays.

| Parameter | Value | Range | Notes |
|---|---|---|---|
| `StrikeLimit` | **2** | 1 – 3 | Hard 2-strike rule per GDD §3.4. |
| `ToppleWobbleDuration` | **0.8 s** | 0.4 – 1.2 s | Build-up before explosion. |

---

## 5. Scripted Wobble

The wobble is **entirely fake** — the `Tower` object rotates as a unit via scripted sway. No Rigidbodies are involved. This satisfies ADR-0002 and the "real stacks explode past ~10–12 floors" constraint.

### 5.1 Wobble model

The Tower object oscillates in rotation (tilt around the base center, X-axis for left-right, optionally Z for forward-back with the same formulas). At each frame:

```
wobbleAngle(t) = A(h, lean) * sin(2π * t / T(h)) * D(t_since_drop)
```

Where:
- `A(h, lean)` = amplitude (degrees), a function of floor height `h` and accumulated lean.
- `T(h)` = period of wobble oscillation, a function of height.
- `D(t_since_drop)` = damping envelope — the wobble rings out between drops but never fully stops (a residual idle sway remains for tense feel).

### 5.2 Amplitude formula

```csharp
float BaseAmplitude(int floorCount) =>
    Mathf.Clamp(
        WobbleAmpBase + WobbleAmpPerFloor * floorCount,
        WobbleAmpMin,
        WobbleAmpMax
    );

float WobbleAmplitude(int floorCount, float leanOffset) =>
    BaseAmplitude(floorCount) + WobbleLeanBias * Mathf.Abs(leanOffset);
```

| Parameter | Value | Min | Max | Notes |
|---|---|---|---|---|
| `WobbleAmpBase` | **0.5 deg** | 0.0 | 2.0 | Idle sway amplitude at floor 0. Even a fresh tower has a tiny life. |
| `WobbleAmpPerFloor` | **0.08 deg/floor** | 0.04 | 0.15 | Linear ramp. At floor 10: 0.5+0.8 = 1.3 deg. At floor 30: 0.5+2.4 = 2.9 deg. At floor 50: clamped. |
| `WobbleAmpMax` | **6.0 deg** | 3.0 | 10.0 | Reached around floor 69. Above this, amplitude is clamped — prevents visual chaos. |
| `WobbleAmpMin` | **0.5 deg** | 0.0 | 2.0 | Floor; the tower always sways a little. |
| `WobbleLeanBias` | **1.2 deg/m** | 0.5 | 2.5 | Extra tilt for each metre of accumulated lean. Makes sloppy play visibly dangerous. |

**Worked example:** at floor 30, leanOffset = 0.5 m (after one Sloppy drop): amplitude = 2.9 + 1.2×0.5 = **3.5 degrees**. Visibly leaning. At floor 50, leanOffset = 1.0 m (two Sloppy drops): 4.5 + 1.2×1.0 = 5.7 deg (clamped to 6.0). The tower is dramatically tilting.

**[R1 — game-director ratification]** The `WobbleAmpMax = 6.0°` cap and the OQ-05 "tense vs. dizzying" check are a **Pillar-2 *fairness* gate, not deferred comfort tuning**: a tower that reads as "dizzying" reads as "random" to the player's gut — the exact churn failure mode the pillar names. OQ-05 must pass before the MVP fun gate, and the wobble must oscillate smoothly (no jerky transitions).

### 5.3 Wobble period (frequency)

Taller towers sway more slowly, like a real skyscraper. This is pure feel — it makes height feel weighty.

```csharp
float WobblePeriod(int floorCount) =>
    Mathf.Clamp(
        WobblePeriodBase + WobblePeriodPerFloor * floorCount,
        WobblePeriodMin,
        WobblePeriodMax
    );
```

| Parameter | Value | Min | Max | Notes |
|---|---|---|---|---|
| `WobblePeriodBase` | **0.6 s** | 0.3 | 1.0 | Fast, snappy wobble on a short tower. |
| `WobblePeriodPerFloor` | **0.015 s/floor** | 0.008 | 0.025 | Gets slower with height. At floor 30: 0.6 + 0.45 = 1.05 s. At floor 60: 0.6 + 0.9 = 1.5 s. |
| `WobblePeriodMax` | **2.0 s** | 1.0 | 3.0 | A very tall tower sways lazily and massively — terrifying. |
| `WobblePeriodMin` | **0.5 s** | 0.3 | 0.7 | Never faster than this (too jittery). |

### 5.4 Damping envelope

On each new block contact (any grade), the wobble resets to full amplitude for a brief ring-out. Between drops, it damps toward the idle baseline, never fully zeroing (the residual sway is the tension).

```csharp
// Damping model (runs in Unity layer, purely visual):
// wobbleScale = 1.0 immediately after drop; decays toward IdleWobbleScale
// wobbleScale(t) = IdleWobbleScale + (1 - IdleWobbleScale) * exp(-DampingRate * t)
```

| Parameter | Value | Min | Max | Notes |
|---|---|---|---|---|
| `IdleWobbleScale` | **0.30** | 0.10 | 0.60 | 30% of full amplitude persists as idle sway. |
| `DampingRate` | **2.0 s⁻¹** | 0.8 | 4.0 | At floor 1–10, wobble halves in ~0.35 s. Gets tuned slower at height (see below). |

**Height-adjusted damping:** the damping rate decreases with height so tall towers ring out more slowly and feel heavier:

```csharp
float EffectiveDampingRate(int floorCount) =>
    Mathf.Max(DampingRateMin, DampingRate - DampingDecayPerFloor * floorCount);
```

| Parameter | Value | Min | Max |
|---|---|---|---|
| `DampingRateMin` | **0.5 s⁻¹** | 0.2 | 1.0 |
| `DampingDecayPerFloor` | **0.025 s⁻¹/floor** | 0.010 | 0.040 |

At floor 30: rate = 2.0 − 0.025×30 = **1.25 s⁻¹** (halves in 0.55 s). At floor 60: rate = 2.0 − 1.5 = **0.5 s⁻¹** (clamped; halves in 1.4 s). A 60-floor tower rings for nearly 3 full seconds after a drop.

### 5.5 Lean direction bias

The wobble's sine center (equilibrium position) is offset by `leanOffset / BlockWidth * MaxLeanBiasAngle` degrees in the drop direction. This means a leaning tower sways *around* its lean, not around vertical — the equilibrium has genuinely shifted. This makes the player feel they must "catch up" the lean by landing Perfects.

| Parameter | Value | Range |
|---|---|---|
| `MaxLeanBiasAngle` | **4.0 deg** | 1.0 – 8.0 |

---

## 6. Scoring Formula

Scoring is implemented entirely in `Towerpolis.Core`. No PhysX.

### 6.1 Per-floor score

```csharp
int FloorScore(FloorType type, Grade grade, int perfectChain) {
    int baseScore = FloorBaseScore[type];          // lookup table
    float gradeMultiplier = GradeMultiplier[grade]; // lookup table
    int chainBonus = PerfectChainBonus(perfectChain);
    return (int)(baseScore * gradeMultiplier) + chainBonus;
}
```

**Floor base scores (from block type):**

| Block type | `FloorBaseScore` |
|---|---|
| `Base_Ground` / `Base_Ground_2` | 0 (foundation, not scored) |
| `Floor_Standard` | **100** |
| `Floor_Balcony` / `Floor_Balcony_2` | **150** |
| `Floor_Premium` | **200** |

**Grade multipliers:**

| Grade | `GradeMultiplier` |
|---|---|
| Perfect | **2.0** |
| Good | **1.0** |
| Sloppy | **0.5** |
| Miss | **0.0** (floor not added; no score) |

### 6.2 Perfect-chain bonus

A perfect chain is the count of consecutive Perfect drops without interruption. Good or Sloppy drops reset the chain to 0. Miss also resets.

```csharp
int PerfectChainBonus(int chainLength) {
    if (chainLength <= 0) return 0;
    if (chainLength <= 2) return 50;
    if (chainLength <= 5) return 150;
    if (chainLength <= 10) return 350;
    return 600;   // chain > 10 — extraordinary
}
```

| Chain length | Bonus per floor |
|---|---|
| 1–2 | +50 |
| 3–5 | +150 |
| 6–10 | +350 |
| 11+ | +600 |

**Design note:** the chain bonus escalates sharply to reward sustained skill and make the decision to "push for Perfects" feel meaningful. A 10-floor Perfect chain on Premium floors yields (200×2 + 350)×10 = **7,500 points** from chain alone. A mixed Good run of 10 floors on Standard yields 100×1.0×10 = **1,000 points**. The delta is large enough to feel like mastery.

### 6.3 Total run score

```csharp
int RunScore = Sum_of_FloorScores + ResidentBonus;
```

**Resident bonus** (visual-only at MVP, placeholder value stored):

```csharp
int ResidentBonus = TotalResidents * ResidentScoreValue;
// ResidentScoreValue = 10 (per resident)
```

Residents per floor: Standard = 2, Balcony/Balcony_2 = 3, Premium = 4. A Perfect drop adds `PerfectResidentBonus = 1` extra resident to that floor. Total residents in a run = sum of all floor residents + perfect bonuses.

### 6.4 Worked example (10-floor run)

Assume: floors 1–10 are all `Floor_Standard`. First 5 are Perfect (chain 1→5), floors 6–8 are Good (chain resets to 0), floor 9 is Sloppy (gets strike 1), floor 10 is Perfect (chain restarts to 1).

| Floor | Grade | Base | Multiplier | Chain bonus | Floor score |
|---|---|---|---|---|---|
| 1 | Perfect | 100 | ×2.0 | +50 (chain 1) | 250 |
| 2 | Perfect | 100 | ×2.0 | +50 (chain 2) | 250 |
| 3 | Perfect | 100 | ×2.0 | +150 (chain 3) | 350 |
| 4 | Perfect | 100 | ×2.0 | +150 (chain 4) | 350 |
| 5 | Perfect | 100 | ×2.0 | +150 (chain 5) | 350 |
| 6 | Good | 100 | ×1.0 | 0 (chain reset) | 100 |
| 7 | Good | 100 | ×1.0 | 0 | 100 |
| 8 | Good | 100 | ×1.0 | 0 | 100 |
| 9 | Sloppy | 100 | ×0.5 | 0 | 50 |
| 10 | Perfect | 100 | ×2.0 | +50 (chain 1) | 250 |

Total floor scores: 2,150. Residents: 5 floors Perfect (2+1 bonus=3 each) + 3 Good (2 each) + 1 Sloppy (2) + 1 Perfect (3) = 15+6+2+3=26. Resident bonus: 26×10 = 260. **Total run score: 2,410.**

### 6.5 High score

`LocalHighScore` is a single `int` stored in `PlayerPrefs` (Unity layer). Computed and updated by Core after each run. No server. No leaderboard at MVP.

---

## 7. Juice Specification Per Event

All tweens use PrimeTween (ADR-0006, proposed). All particle effects are pooled. No per-frame GC.

### 7.1 Block land (any grade)

| Element | Spec |
|---|---|
| **Squash-stretch** | Tween block scale: Y→0.75, X/Z→1.15 over 0.06 s (ease out), then elastic-back to 1.0/1.0/1.0 over 0.25 s (elastic overshoot). |
| **Dust ring** | Spawn 1 pooled particle system at contact point. Duration 0.4 s. 12–16 particles. Radial spread, XZ plane. Color: off-white `#F7F7F2`. Keep below 50 particles/system. |
| **Camera shake** | `DOShakePosition` magnitude = `0.05 + 0.15 * (offsetX / BlockWidth)`, duration 0.18 s, vibrato 20. Scales with misalignment: perfect = 0.05 m shake, sloppy = 0.20 m shake. |
| **Haptic** | `Handheld.Vibrate()` equivalent: 1 short pulse (~20 ms), **every drop**. The minimal haptic tick specified in GDD §3.4. |
| **Audio** | "Thunk" SFX, pitch randomized ±5% each drop to prevent fatigue. |

### 7.2 Perfect drop

| Element | Spec |
|---|---|
| **"PERFECT!" text pop** | TextMeshPro label appears at block center, DOScale punch (0→1.3→1.0) over 0.3 s, then fade-out over 0.4 s. Color: sunshine yellow `#FFD54F`. |
| **Score pop** | Score counter DOPunchScale (1.0→1.25→1.0) over 0.2 s. |
| **Confetti burst** | 30–40 particles, random colors from hero palette, 1.0 s duration, burst from block top face. |
| **Chime** | Rising chime SFX. Pitch shifts up by `+semitone * (min(chain, 8))` so chain escalates. Chain 1 = base pitch. Chain 8 = 8 semitones up. Caps at chain 8 (+8 semitones) to avoid it becoming shrill. |
| **Lean correction visual** | If `leanOffset` was non-zero, play a subtle 0.15 s tower-sway-return (PrimeTween rotate toward center, ease OutSine). |
| **Resident arrival** | Spawn 1 resident (pooled prefab), parachute-float animation, lands on the new floor. Chatter SFX plays. |
| **Camera shake** | Same formula as §7.1 but MINIMUM shake (0.05 m) — the tap was perfect; don't punish it. |

### 7.3 Good drop

| Element | Spec |
|---|---|
| **Slice VFX** | Pooled particle system: 6–8 "chip" sprites fly off in the offset direction, tumble, fade. 0.6 s. |
| **Score pop** | Score counter DOPunchScale (1.0→1.1→1.0) over 0.15 s. Smaller than Perfect. |
| **Camera shake** | `0.05 + 0.15 * (offsetX / BlockWidth)` — small shake; the drop was okay. |
| **No "PERFECT!" label** | Silence = feedback that it was not perfect. |
| **Resident arrival** | 1 resident, same as Perfect but no escalating chime. Standard SFX. |
| **Audio** | Softer "clunk" SFX variant. |

### 7.4 Sloppy drop (strike 1)

| Element | Spec |
|---|---|
| **Strike indicator** | 1 of the 2 strike icons in the HUD fills/pulses red. DOPunchScale (1.0→1.4→1.0) over 0.3 s. |
| **Screen flash** | Single-frame semi-transparent red vignette overlay (alpha 0.25), fades over 0.4 s. |
| **Camera shake** | `0.15 + 0.15 * (offsetX / BlockWidth)` — noticeably stronger. The tower is in trouble. |
| **Slice VFX** | Same as Good but more particles (10–12 chips). |
| **Warning chime** | Low descending tone (instead of ascending). Communicates danger. |
| **Haptic** | Two short pulses (~20 ms each, 30 ms apart) — different texture from the normal land pulse. |
| **Audio** | "Crunch" SFX. |

### 7.5 Miss (block tumbles)

| Element | Spec |
|---|---|
| **No weld** | Block stays as active Rigidbody, bounces/tumbles off visually. |
| **Camera pull-back** | Camera zooms out over 0.6 s to frame the entire tower (even at 50+ floors). Ease: OutCubic. This is the GDD §3.4 "miss pull-back". |
| **Strike indicator** | If this is strike 1, fills 1 HUD icon (red pulse). If strike 2, fills second HUD icon — brief pause, then TriggerTopple(). |
| **Screen flash** | Red vignette alpha 0.40, fade over 0.5 s. |
| **Camera shake** | `0.30 m` magnitude, duration 0.25 s — largest shake in the game. |
| **Haptic** | Long pulse (~80 ms). |
| **Audio** | "Whoosh + miss" descending SFX. |

### 7.6 Topple (2nd cumulative strike)

| Element | Spec |
|---|---|
| **Pre-topple wobble** | Over `ToppleWobbleDuration = 0.8 s`: amplitude ramps from `WobbleAmplitude` to `3× WobbleAmplitude`, frequency speeds up ×2. This is the "oh no" beat. |
| **Collapse** | All welded blocks simultaneously re-enabled as Rigidbodies (Unity layer). Rigidbodies get a small randomized force so they fan out. Apply `ExplosionForce` at tower base center: `ForcePerBlock = 12 N`, radial pattern. |
| **Camera** | Wide pull-back continues (or if already pulled back, stays). `DOShakePosition` magnitude 0.50 m, duration 0.8 s. |
| **Particle** | Large dust cloud (pooled, 60–80 particles) at the tower base. |
| **Audio** | "Tower collapse" SFX (tumbling/crash), then silence for 0.5 s, then "run end" musical sting. |
| **Haptic** | Long rumble pattern: 100 ms on / 50 ms off / 100 ms on. |
| **Score lock** | Core locks the final score BEFORE the topple animation plays. The animation is purely cosmetic. |
| **Restart prompt** | Appears after collapse animation completes (~2.0 s total). "ONE MORE TRY" button. DOScale punch-in. |

### 7.7 Chain escalation visual

On consecutive Perfects, the chime pitch rises (§7.2). Additionally:
- Chain 3: confetti particle count increases to 50.
- Chain 5: a secondary "ring of light" VFX plays (single annular sprite, scale up + fade, 0.3 s). Color: warm glass `#FFF3C4`.
- Chain 10+: screen edge glow vignette (soft white, alpha 0.15) persists until chain breaks.

These are all pooled/faded, not permanent — no GC cost between events.

---

## 8. Camera Behavior

### 8.1 Follow-up (tower grows)

The camera tracks upward as the tower grows. Camera behavior is handled entirely in the Unity layer; Core has no camera awareness.

| Parameter | Value | Range |
|---|---|---|
| `CameraTargetOffsetY` | Tower top Y + **4.0 m** | 2.0 – 6.0 m | Camera looks at a point 4 m above the current tower top. |
| `CameraFollowSmoothTime` | **0.4 s** | 0.2 – 0.8 s | SmoothDamp lag. Camera lags slightly behind the tower rising — this feels natural, not jittery. |
| `CameraDistance` | **10.0 m** | 7.0 – 15.0 m | Camera orbits at this distance from the tower center axis. Slightly angled (25° from vertical) for a 3/4 view. |
| `CameraAngleX` | **25 deg** | 15 – 40 deg | Tilt below horizontal. Portrait view: slightly looking down at blocks for readability. |

As the tower grows above ~20 floors, `CameraDistance` gradually increases:
```
EffectiveCameraDistance = CameraDistance + CameraDistancePerFloor * max(0, floorCount - 20)
```

| Parameter | Value | Range |
|---|---|---|
| `CameraDistancePerFloor` | **0.08 m/floor** | 0.03 – 0.15 | At floor 50: 10 + 0.08×30 = 12.4 m. Keeps the full top visible. |
| `MaxCameraDistance` | **18.0 m** | 12 – 24 | |

### 8.2 Miss pull-back

On a Miss or Topple trigger: camera smoothly zooms out over 0.6 s to frame the **full tower height** (all welded blocks in view), then holds until the run ends or restart is tapped.

```
PullbackTargetY = TowerHeight / 2.0       // look at the tower midpoint
PullbackDistance = TowerHeight * 1.2 + 5  // generous framing
```

The pull-back is a PrimeTween tween on camera position + look target simultaneously.

### 8.3 Framing intent

At all times during play, the top 2–3 blocks of the tower plus the crane arm must be visible and unobstructed. The dangling block must be fully visible against the skybox. Portrait orientation, safe-area insets respected.

---

## 9. Residents (MVP Minimal Spec)

Per GDD §6 scope note: residents are a visual element of the core loop, NOT meta. Population tracking is stubbed in Core for later.

On every placed floor (Good, Sloppy, or Perfect — not Miss), spawn 1 resident prefab per the floor's resident count (Standard=2, Balcony=3, Premium=4, with 1 extra on Perfect). The resident prefab:
- Animates: parachute/umbrella open → float down → land → idle.
- Duration: ~2 s descent.
- No pathfinding, no AI, no state machine — one pooled prefab, one AnimationController with 3 clips (fall, land, idle).
- Chatter SFX plays on land.
- Residents remain visible on lower floors (idle). At MVP, they do not accumulate across runs; they are cleared on restart.
- Population value stored in Core for later meta (`int TotalResidents`), but not displayed in MVP HUD.

| Parameter | Value |
|---|---|
| `ResidentsStandard` | 2 |
| `ResidentsBalcony` | 3 (applies to `Floor_Balcony` and `Floor_Balcony_2`) |
| `ResidentsPremium` | 4 |
| `PerfectResidentBonus` | +1 (added to the floor's base count on a Perfect) |

---

## 10. Consolidated Tunables Table

Every value marked `[TUNE]` is a field in `CoreConfig` (or `JuiceConfig` for Unity-layer values) accessible without code recompilation.

| # | Parameter | Category | Start value | Min | Max | Controls |
|---|---|---|---|---|---|---|
| 1 | `SwingHalfArc` | Crane | 1.4 m | 0.8 | 2.0 | Swing width relative to block |
| 2 | `PeriodFloor1` | Crane | 2.8 s | 2.0 | 4.0 | Starting swing speed |
| 3 | `PeriodMinClamp` | Crane | 2.0 s | 1.6 | 2.8 | Fastest the swing ever gets |
| 4 | `PeriodRampFactor` | Crane | 0.012 s/floor | 0.006 | 0.020 | How much faster the crane gets per floor |
| 5 | `InputLatencyBudget` | Feel | 50 ms | — | — | Max tap-to-detach latency (QA gate) |
| 6 | `GravityScale` | Drop | 2.5 | 1.5 | 4.0 | How snappy the fall feels |
| 7 | `GradePerfectThreshold` | Grading | 0.10 × Width | 0.06 | 0.18 | Half-width for Perfect band |
| 8 | `GradeGoodThreshold` | Grading | 0.30 × Width | 0.20 | 0.45 | Half-width for Good band |
| 9 | `GradeSloppyThreshold` | Grading | 0.50 × Width | 0.40 | 0.65 | Half-width for Sloppy band |
| 10 | `MinBlockWidth` | Grading | 0.4 m | 0.2 | 0.8 | Narrowest surviving slice |
| 11 | `GoodLeanFactor` | Grading | 0.15 | 0.05 | 0.30 | Lean added per Good drop |
| 12 | `SloppyLeanFactor` | Grading | 0.35 | 0.15 | 0.60 | Lean added per Sloppy drop |
| 13 | `PerfectLeanCorrectionFraction` | Grading | 0.25 | 0.10 | 0.40 | Lean corrected by each Perfect |
| 14 | `StrikeLimit` | Rules | 2 | 1 | 3 | Strikes before topple |
| 15 | `ToppleWobbleDuration` | Rules | 0.8 s | 0.4 | 1.2 | Pre-collapse dramatic wobble |
| 16 | `WobbleAmpBase` | Wobble | 0.5 deg | 0.0 | 2.0 | Base idle sway (floor 0) |
| 17 | `WobbleAmpPerFloor` | Wobble | 0.08 deg/floor | 0.04 | 0.15 | Amplitude ramp rate |
| 18 | `WobbleAmpMax` | Wobble | 6.0 deg | 3.0 | 10.0 | Max wobble amplitude |
| 19 | `WobbleAmpMin` | Wobble | 0.5 deg | 0.0 | 2.0 | Min wobble amplitude (always sways) |
| 20 | `WobbleLeanBias` | Wobble | 1.2 deg/m | 0.5 | 2.5 | Extra tilt per metre of lean |
| 21 | `WobblePeriodBase` | Wobble | 0.6 s | 0.3 | 1.0 | Wobble period at floor 0 |
| 22 | `WobblePeriodPerFloor` | Wobble | 0.015 s/floor | 0.008 | 0.025 | Wobble slows with height |
| 23 | `WobblePeriodMax` | Wobble | 2.0 s | 1.0 | 3.0 | Max wobble period (tallest towers) |
| 24 | `WobblePeriodMin` | Wobble | 0.5 s | 0.3 | 0.7 | Min wobble period |
| 25 | `IdleWobbleScale` | Wobble | 0.30 | 0.10 | 0.60 | Residual wobble between drops |
| 26 | `DampingRate` | Wobble | 2.0 s⁻¹ | 0.8 | 4.0 | Wobble ring-out speed |
| 27 | `DampingRateMin` | Wobble | 0.5 s⁻¹ | 0.2 | 1.0 | Slowest possible ring-out |
| 28 | `DampingDecayPerFloor` | Wobble | 0.025 s⁻¹/floor | 0.010 | 0.040 | Damping slows as tower grows |
| 29 | `MaxLeanBiasAngle` | Wobble | 4.0 deg | 1.0 | 8.0 | Max equilibrium-shift from lean |
| 30 | `FloorBaseScore_Standard` | Score | 100 | 50 | 200 | Points for Standard floor |
| 31 | `FloorBaseScore_Balcony` | Score | 150 | 75 | 300 | Points for Balcony floor |
| 32 | `FloorBaseScore_Premium` | Score | 200 | 100 | 400 | Points for Premium floor |
| 33 | `GradeMultiplier_Perfect` | Score | 2.0 | 1.5 | 3.0 | Score multiplier for Perfect |
| 34 | `GradeMultiplier_Good` | Score | 1.0 | 0.75 | 1.5 | Score multiplier for Good |
| 35 | `GradeMultiplier_Sloppy` | Score | 0.5 | 0.25 | 1.0 | Score multiplier for Sloppy |
| 36 | `ChainBonus_1to2` | Score | 50 | 25 | 100 | Chain bonus, chain length 1–2 |
| 37 | `ChainBonus_3to5` | Score | 150 | 75 | 300 | Chain bonus, chain length 3–5 |
| 38 | `ChainBonus_6to10` | Score | 350 | 150 | 600 | Chain bonus, chain length 6–10 |
| 39 | `ChainBonus_11plus` | Score | 600 | 300 | 1000 | Chain bonus, chain 11+ |
| 40 | `ResidentScoreValue` | Score | 10 | 5 | 25 | Score per resident housed |
| 41 | `PerfectResidentBonus` | Score | 1 | 0 | 2 | Extra resident on Perfect drop |
| 42 | `CameraTargetOffsetY` | Camera | 4.0 m | 2.0 | 6.0 | Camera look-at height above tower top |
| 43 | `CameraFollowSmoothTime` | Camera | 0.4 s | 0.2 | 0.8 | Camera follow lag |
| 44 | `CameraDistance` | Camera | 10.0 m | 7.0 | 15.0 | Base camera orbit distance |
| 45 | `CameraDistancePerFloor` | Camera | 0.08 m/floor | 0.03 | 0.15 | Distance increase per floor above 20 |
| 46 | `MaxCameraDistance` | Camera | 18.0 m | 12.0 | 24.0 | Farthest camera ever gets |
| 47 | `CameraAngleX` | Camera | 25 deg | 15 | 40 | Downward tilt of camera |
| 48 | `LandSquashY` | Juice | 0.75 | 0.60 | 0.90 | Block Y scale at squash peak |
| 49 | `LandSquashXZ` | Juice | 1.15 | 1.05 | 1.30 | Block XZ scale at squash peak |
| 50 | `SquashDuration` | Juice | 0.06 s | 0.04 | 0.12 | Time to reach squash peak |
| 51 | `SettleDuration` | Juice | 0.25 s | 0.15 | 0.40 | Elastic settle-back duration |
| 52 | `ChimePitchStep` | Juice | 1 semitone | 0.5 | 2.0 | Pitch rise per chain step |
| 53 | `ChimePitchMaxChain` | Juice | 8 | 5 | 12 | Chain count where chime caps |

---

## 11. Open Questions and Risks

These require a decision before or during Phase 2 implementation. Items marked **[GAME-DIRECTOR]** need a creative sign-off; **[PLAYTEST]** can be resolved from data after the first internal build.

### 11.1 For game-director

**OQ-01: Sloppy vs Good — should Sloppy give a strike? → RESOLVED (game-director): YES.**
Sloppy costs a strike, exposed as tunable `SloppyCostsStrike` (default `true`, see §4). The telegraphed offset + red flash + warning chime make the strike feel *earned*, not random (Pillar 2). The lever order if soft-launch shows churn: first widen the Good band (OQ-04), only then flip `SloppyCostsStrike` to false. Ships `true`.

**OQ-02: Block type selection — random or patterned? → RESOLVED (game-director): weighted deterministic draw from seed.**
Fully specified in §1.5: weights Standard 70 / Balcony 22 / Premium 8 (gameplay types only; Base_Ground_2 & Balcony_2 are cosmetic sub-rolls), floors 1–3 forced Standard, max 2 Premium per rolling window of 5, fully seed-reproducible, golden-locked. Patterned was rejected (memorizable → dilutes "react to the crane" skill + looks mechanical).

**OQ-03: What does the HUD show?**
The spec names a "2 strike icons" indicator. Exact HUD layout (score counter position, height counter, strike icons) is owned by `ui-ux-designer`, but the presence/absence of each element is a game-designer call. Confirm: (a) running score, (b) current height in floors, (c) 2 strike icons, (d) no other persistent HUD elements at MVP. Residents count deferred.

### 11.2 Playtest-resolvable (tune in Phase 2)

**OQ-04: Perfect window too tight or too wide?**
The 10% half-width (~20 cm on a 2 m block) was chosen to be achievable but skill-gating. In playtest: if new players rarely land Perfects, widen to 0.15; if Perfects are trivial, tighten to 0.07. The `GradePerfectThreshold` is tunable without code changes.

**OQ-05: Wobble amplitude — does it read as "tense" or "dizzying"?**
The wobble curve reaches 6 degrees at ~floor 69. On a portrait mobile screen, 6 degrees of rotation on a tall tower is significant. May need to cap lower (4 deg) if players find it nauseating. Anti-nausea note: the wobble must oscillate smoothly — no jerky transitions. The `WobbleAmpMax` is tunable.

**OQ-06: Swing period ramp — is it perceptible at all?**
The current ramp is deliberately very slow (2.8→2.0 s over 100 floors). In playtest: if players don't feel a real challenge increase from the crane speed, we might nudge `PeriodRampFactor` up slightly. But the design intent is that tension comes from wobble, not swing speed, so this is intentionally subtle.

**OQ-07: Should the lean correction from Perfects be visible/communicated?**
Currently the lean correction plays a subtle tower sway-back animation. Should there be a text cue ("STEADY!") or is the visual enough? Test in playtest for player comprehension.

**OQ-08: Does the collapse feel "funny" (the design goal) or just punishing?**
The GDD calls for "a big, funny cartoony collapse." The spec has a pre-topple dramatic wobble + block explosion. The "funny" feel depends heavily on audio and timing. Audio-designer should get this spec and prototype the sound design before the MVP fun gate.

### 11.3 Architecture / implementation risks

**OQ-09: Contact detection frame accuracy.**
The "grading fires on the frame the block bottom reaches tower top Y" is a scripted check. If the block is fast (high GravityScale) and the frame rate drops, the block may pass through the tower top in one frame without triggering. This requires a swept/multi-step check, not a simple position compare. Flag for `gameplay-programmer` and `physics-programmer`.

**OQ-10: Lean accumulation and tower visual alignment.**
`leanOffset` in Core is a float used by the wobble system, but the visual "lean" of the tower (the actual rendered tilt) must be driven by this value. The wobble period and the lean bias (§5.5) must be synchronized. The tower's "resting angle" between drops = `leanOffset / BlockWidth * MaxLeanBiasAngle` degrees. This must not snap to the value instantly — it should be the wobble equilibrium. Implementation detail for `physics-programmer`.

**OQ-11: Block-type stream independence from swing stream. → RESOLVED (game-director): two salted streams.**
Two independent `XorShiftRng` instances, each from the run seed via a distinct SplitMix64 salt (`SALT_BLOCK`, `SALT_SWING` — named constants in Core). Fully specified in §1.5. Keeps both golden-testable in isolation.

---

## 12. Implementation Handoff Summary

**For `gameplay-programmer`:** implement all Core logic (grading bands §3, lean accumulation §3.2, 2-strike rule §4, scoring §6, lean correction §3.1) as NUnit-tested functions in `Towerpolis.Core`. The full `CoreConfig` struct or ScriptableObject must expose every parameter in the tunables table (§10). Write golden tests with the worked example from §6.4.

**For `physics-programmer`:** implement the scripted wobble (§5) as a MonoBehaviour on the Tower root object. The amplitude, period, and damping formulas are in §5.2–5.4. Lean bias equilibrium in §5.5. Contact detection swept check (OQ-09). The Rigidbody on the dropped block is active only during fall; freeze immediately on contact.

**For `vfx-artist`:** build pooled particle systems for dust ring, confetti, slice chips, ring-of-light, collapse cloud per §7. Keep particle counts within budget (< 80 particles/system). Pool all systems; no instantiate-on-drop.

**For `audio-designer`:** SFX targets per §7: land thunk (pitch-varied), perfect chime (semitone-stepped chain), crunch (sloppy), miss whoosh, collapse crash. Chatter SFX on resident land. Run-end sting. All SFX must respect the device mute/silent switch.

**For `ui-ux-designer`:** HUD per OQ-03: score counter (top center), height counter in floors (top right), 2 strike icons (bottom left). "PERFECT!" text pop (anchored to block world position, not screen). "ONE MORE TRY" button (post-collapse).

**For `game-qa-engineer`:** the fun-gate test plan derives directly from this spec. Key checks: (a) input latency ≤ 50 ms on a mid device (§1.4), (b) grading band boundaries are exact (within 1 mm tolerance in Unity world units), (c) two cumulative strikes topple every time, (d) score is deterministic (same position sequence → same score, cross-device), (e) wobble amplitude at floor 10/30/60 matches §5.2 worked values, (f) 60 fps on a Snapdragon 7-series equivalent device with the full juice stack active.

---

*End of MVP Core-Loop Feel Specification v1.0*
