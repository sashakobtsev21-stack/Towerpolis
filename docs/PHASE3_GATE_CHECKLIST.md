# Phase 3 Gate — On-Device Checklist (meta-spec §7)

The gate's **automated side is done**: the full Core NUnit suite is green
(`dotnet test core/Towerpolis.Core.Tests` → **286/286**). This doc is the **manual,
on-device pass** that the automated tests can't cover (real Android storage, real platform
math, the feel of the new screens). Tick each box on a physical device.

Which gate is auto vs manual:

| Gate | Automated (NUnit, green) | Manual (this doc) |
|---|---|---|
| 1 Daily-seed determinism | `DailySeedTests` (same seed, bit-identical RNG across "devices", golden lock) | same seed + same first swings on **two real devices** |
| 2 City persists | `SaveDataTests.RoundTrip_PreservesMetaState` | **force-quit + relaunch** on device (real file I/O) |
| 3 Leaderboard | `LocalLeaderboardTests`, `CityStateTests` | quick spot-check |
| 4 District unlock flow | `CityStateTests.DistrictComplete_FiresOnce_AndPaysRewardOnce` | the **flow + new DISTRICT-COMPLETE screen** |
| 5 Coin earn | `CoinEarnCalculatorTests` | coin delta matches a run |
| 6 No regression | `ScoringTests`, `TowerRunTests` | — (covered by CI) |

---

## Setup

1. Build to the device (Unity → Build & Run, or an AAB/APK to the phone).
2. **Fresh install** for a clean save (or clear app data) so populations start at 0.
3. Save file location on Android (default package `com.Towerpolis.Towerpolis`):
   ```
   /storage/emulated/0/Android/data/com.Towerpolis.Towerpolis/files/save/city.json
   ```
   Pull it any time with:
   ```
   adb shell run-as com.Towerpolis.Towerpolis cat files/save/city.json
   # or
   adb pull /sdcard/Android/data/com.Towerpolis.Towerpolis/files/save/city.json
   ```
   (Writes are atomic via a temp file + `File.Replace`, with a `city.json.bak` fallback.)

---

## Gate 1 — Daily Seed Determinism (needs 2 devices)

- [ ] Open **Daily** mode on device A and device B on the **same UTC day**.
- [ ] The crane swing pattern looks identical for the first ~20 drops (play both side by side).
- [ ] (Optional) Log `DailySeed.ForDateUtc(DateTime.UtcNow)` on both — same `ulong`.
- [ ] After playing Daily once, the **Daily button reads "DONE"** and a second attempt is refused.

> The math is already proven bit-identical in NUnit; this just confirms no platform float
> drift. The swing/sequence are integer/XorShift based, so divergence would be a real bug.

## Gate 2 — City Persists Across Sessions

- [ ] In **Downtown**, complete **3 runs** (let each tower topple) so 3 towers deposit.
- [ ] Note the city population (City view headline).
- [ ] **Force-quit** the app (swipe from recents / kill), then relaunch.
- [ ] City view shows the **3 towers** and the **same population**.
- [ ] (Optional) `city.json` contains the `downtown` district with 3 plots.

## Gate 3 — Leaderboard (spot-check)

- [ ] Beat your endless best → it updates. Do a worse run → it does **not** regress.
- [ ] A Daily run writes today's score and **increments the streak** by 1 (once per UTC day).

## Gate 4 — District Unlock Flow + DISTRICT-COMPLETE screen ⭐ (new this build)

The unlock gate is now **live** (linear: Downtown → Neon → Winter), and the goals are the
real design values (1200 / 1600 / 2200). Population uses **best-N**: once the 20 plots are
full, a new tower **replaces the smallest** if it has more residents, so the city always shows
your best buildings and the goal stays reachable by improving (no soft-lock). So out of the box:

- [ ] In the City view, **Neon and Winter show as locked** (greyed) until the prior district
      is completed. Tapping a locked district does nothing.
- [ ] After the grid is full, a **bigger** tower replaces the smallest bar (population ticks up);
      a **smaller** tower changes nothing.

To exercise the **completion flow + the new celebration screen** without grinding 1200
residents, temporarily lower one goal **for a test session only**:

> In `unity/Towerpolis/Assets/Game/Meta/DistrictCatalog.cs`, set Downtown's `fillGoal` to a
> small number (e.g. `20`), play to reach it, verify the items below, then **restore it to
> `1200`** (and re-test that the gate still reads locked from a fresh save).

- [ ] Reaching the goal triggers the **DISTRICT COMPLETE** full-screen beat (gold title,
      district name, reward coins + gems, "NEW DISTRICT UNLOCKED: …", Continue button).
- [ ] The screen has a spring **pop** and reads as a real milestone (not a toast).
- [ ] **Continue** closes it and the run can be restarted normally (no double-restart).
- [ ] The screen fires **exactly once** — replaying the completed district does **not** show it
      again and does **not** re-credit the reward (coins/gems unchanged on later runs).
- [ ] **Neon** is now selectable; switching to it restarts the run in Neon's look.
- [ ] On the **last** district (Winter), completing it shows **"YOUR CITY IS COMPLETE!"**
      instead of a next-district line.

## Gate 5 — Coin Earn Correctness

- [ ] Do a run; the coins credited = floors×1 + perfects×2 (+ first-daily 50 / milestones /
      district reward when applicable). Cross-check against `CoinEarnCalculator` values.

## Gate 6 — No Regression

- [ ] (CI) Phase-2 suite stays green — already covered by `dotnet test`.

---

## Also verify — this build's "доводка" extras

**Per-district music (only if you've added tracks):**
- [ ] Drop `Resources/Music/downtown|neon|winter` (or `theme`) — see `docs/AUDIO_GUIDE.md`.
- [ ] Switching district **crossfades** to the new bed (~0.9 s).
- [ ] An Endless **retry does not restart** the track.
- [ ] With **no** music files: the game is silent with no errors (expected until Phase 6).

**District-complete screen** (covered in Gate 4 above).

---

*When every box is ticked, Phase 3 is gate-complete. Remaining Phase-3 polish is content
(authored music tracks, DistrictDefinition SOs) handled in Phase 6.*
