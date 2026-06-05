# Tower Bloxx → Towerpolis: analysis & integration plan

_Research + plan, 2026-06-05. Sources at the bottom._

This document (1) analyses Towerpolis as it stands today, (2) recaps the Tower Bloxx
mechanics, (3) lists the gaps, and (4) proposes a phased, test-first plan to fold the
best Tower Bloxx ideas into our deterministic Core + Unity layers.

---

## 1. Our project today (analysis)

### 1.1 Core loop (deterministic — `Towerpolis.Core`)
- **Crane swings a whole block; tap drops it** (Tower-Bloxx pivot — blocks are never sliced).
  `TowerGameController.HandleSwingInput` → `DropBlock` → `FallingBlock` contact →
  `TowerRun.PlaceBlock(type, offsetX)`.
- **Grading** (`Grading.Evaluate`, `CoreConfig`): `Perfect` (≤15% of width off-centre → snaps
  to centre), `Good` (caught up to 80% offset), `Miss` (beyond → bounces off). `Sloppy` is
  collapsed into Good today.
- **Placement rule:** only `Perfect`/`Good` land; `Sloppy`/`Miss` tip off and **cost a strike**.
  **2 strikes → topple → run over** (`StrikeLimit = 2`).
- **Lean / sway:** a `Good` drop adds `offsetX × 0.15` to `LeanOffset`; a `Perfect` *reduces*
  lean by 25% (`PerfectLeanCorrectionFraction`). `TowerController` turns accumulated lean into a
  perpetual scripted sway (never decays by time — only clean play calms it).
- **Perfect chain** is tracked (`PerfectChain`, `MaxPerfectChain`).

### 1.2 Scoring, residents, coins (what we reward today)
- **Residents per floor by type:** Standard 2 / Balcony 3 / Premium 5; **Perfect adds** +1 / +2 / +3.
  (`CoreConfig.Residents*`, `PerfectBonus*`.)
- **Score:** floor score × grade multiplier (Perfect ×2) + **chain bonus** (50 / 150 / 350 / 600 at
  chains 1‑2 / 3‑5 / 6‑10 / 11+) + residents × 10. _Score is internal — not shown to the player._
- **Coins:** **+1 / floor, +2 / Perfect**, banked at run-end; now shown **live** on the HUD.
- **Population** = sum of residents deposited across the city grid — the headline meta-score.

> ⚠️ **Key observation:** our **perfect chain only boosts the hidden SCORE, not RESIDENTS.**
> In Tower Bloxx the combo's whole point is **more people**. That's the biggest missing piece.

### 1.3 Meta / "bonuses" today
- **Upgrades shop (БОНУСЫ):** Magnet (auto-centre nudge), City Bonus (× district-completion reward).
- **Cosmetics:** block & crane skins (buy/equip, live recolour).
- **Goals:** 3 weekly missions + 10 permanent achievements (pay coins once).
- **Login gift:** claim once per UTC day.
- **City:** 3 districts (Downtown / Neon / Winter), per-district grid + fill goal + one-time reward.
- **Removed by owner (2026-06-05):** Slow-Mo upgrade, Daily mode, streak freeze. Direction is
  **trim meta, lean on the core stacking + population loop** — which is exactly Tower Bloxx's ethos.

### 1.4 Architecture constraints (must respect)
- **Deterministic logic in `Towerpolis.Core`** (engine-free, NUnit-tested, `dotnet test` green: 184).
  Score/grade/residents **never** derive from PhysX.
- **Test-first on every Core change.** Files < 500 lines, typed APIs, no GC spikes, URP mobile budget.
- Residents already **fly in on umbrellas** (`ResidentFlyIn`) — same visual as Tower Bloxx.

---

## 2. Tower Bloxx recap (condensed)
- **Core:** crane swings a floor; drop it centred. Misalignment makes the tower sway; height speeds
  the swing; 3 lives.
- **Reward = PEOPLE, not a currency shop.** Precision **and speed** = more residents.
- **Combo meter:** a Perfect fills a meter; **while it ticks down every drop gives bonus residents**;
  consecutive perfects stack the bonus ("keep the sparkles going").
- **Specialty blocks:** ~4 perfects in a row unlock **balconies / trophy (bonus) roofs** → extra population.
- **Build City:** 5×5 grid, 4 colours with **adjacency rules** — 🔵 anywhere → 🔴 next to blue →
  🟢 next to blue+red → 🟡 next to blue+red+green → 🌈 Mega-Tower (next to all four, unlimited pop).
  Zones have **population goals**; you can demolish/replace after the build phase.
- **Power-ups (Tower Bloxx: New York):** Life (+1), Combo (multiplier), **Freeze (stop sway)**,
  **Happiness (+50)**. **Renovation:** swipe to straighten placed blocks.
- **Modes:** Story/Build City, Quick Play (tallest), Time Attack, Party/Co-op, Board Game (cards), Challenge.
- **Design lesson:** they *deepened the core* (3 weeks tuning drop/sway physics) instead of piling on features.

---

## 3. Gap analysis (TB has / we don't)
| Tower Bloxx mechanic | In Towerpolis? | Notes |
|---|---|---|
| Perfect → residents | ✅ flat per-type bonus | but **chain doesn't add residents** |
| **Combo meter → bonus residents while ticking** | ❌ | chain only feeds hidden score |
| Earned specialty blocks (balcony/trophy roof) | ⚠️ partial | Balcony/Premium exist but are **random**, not earned |
| Visible combo / "sparkles" feedback | ❌ | no combo UI |
| Build-City colour adjacency puzzle | ❌ | we have districts, no placement puzzle |
| Happiness stat | ❌ | — |
| Renovation (straighten) | ⚠️ | Magnet auto-corrects instead |
| Freeze sway | ❌ (was Slow-Mo, removed) | owner removed it |
| Time Attack / Quick mode | ⚠️ | only Endless (Daily removed) |

---

## 4. Integration plan (phased, prioritised)

Guiding principle: **make residents/population the payoff for skill** (Tower Bloxx's heart), keep
Core deterministic + test-first, and avoid re-bloating the meta the owner just trimmed.

### ⭐ Phase A — Combo → residents (the heart of Tower Bloxx) — HIGH value, LOW risk
**Goal:** a live perfect-chain should pour **people** into the building, not just hidden score.

- **Core (`TowerRun.PlaceBlock`, `Scoring`, `CoreConfig`):**
  - Add a **combo resident bonus** applied to each placed floor, scaling by current `PerfectChain`
    tier — mirror the existing score-chain tiers, e.g.
    `ComboResidentBonus = {1‑2: +1, 3‑5: +2, 6‑10: +4, 11+: +6}` residents/floor.
  - Apply on **Perfect and Good** while the chain is alive (TB gives the bonus to *every* drop
    during the combo, not only perfects) — design choice to confirm with owner.
  - Surface it in `DropOutcome.ResidentsAdded` (already exists) + a new `DropOutcome.ComboTier`
    so the HUD can show the meter.
  - **Tests first:** chain builds bonus, Miss/Sloppy resets it, tiers boundary-correct, daily-safe
    (no RNG/clock). Keep `dotnet test` green.
- **Unity:** `ResidentFlyIn` already animates residents — just feed it the bigger number; add a
  "+N 🧍" popup near the placed floor (reuse the `PerfectHit`/`FloorPlacedAt` events).
- **Effort:** ~½ day. **Touches:** `TowerRun`, `Scoring`, `CoreConfig`, `RunResult`, `HUDController`.

### ⭐ Phase B — Visible combo meter + juice — HIGH value, LOW risk
**Goal:** make the combo legible and exciting ("keep the sparkles going").

- **Unity (`HUDController`):** a combo meter UI — a fill bar / "×N СЕРИЯ" near the HEIGHT number that
  fills on Perfect and **drains** while you're not perfect; colour ramps with tier; pulse + sparkle
  particles (reuse `GameVfx`). Drive it from `TowerGameController.PerfectChain` (already public) +
  the new `ComboTier`.
  - This is **purely presentation** — no Core change, no determinism risk. (We just removed the
    Slow-Mo gauge; this reuses that freed HUD space.)
- **Effort:** ~½ day. **Touches:** `HUDController`, `GameVfx`.

### ⭐ Phase C — Earned specialty blocks (balcony / trophy roof) — MED value, LOW risk
**Goal:** reward a streak with a **better block**, like TB's balconies/bonus roofs.

- **Core (`BlockSequence` / `TowerRun`):** when `PerfectChain` hits a threshold (e.g. every 4
  perfects), make the **next block a Balcony, then Premium** (more residents) — deterministic
  (driven by chain, not RNG). Optionally a run-end **"trophy roof"** = bonus residents for
  `MaxPerfectChain` milestones.
  - **Tests first:** threshold triggers the upgraded type deterministically; resets on a break.
- **Unity:** the Balcony block art already exists; just spawn the upgraded type + a sparkle.
- **Effort:** ~½–1 day. **Touches:** `BlockSequence`, `TowerRun`, `CoreConfig`, `BlockSpawner`.

### Phase D — Build-City colour adjacency puzzle — HIGH value, HIGH effort (decision needed)
**Goal:** turn the city grid into TB's placement puzzle (blue→red→green→yellow→mega).

- **Core (`CityState`, `CityGrid`, new `BuildingType`/adjacency rules):** each deposited tower has a
  **colour/type**; placement is gated by neighbours; Mega-Tower needs all four. Population scales by
  type. Districts become boards with goals.
- **Unity:** city view shows colour-coded plots, a placement step (pick the slot for your finished
  tower), neighbour highlights.
- **Effort:** several days; this **re-frames the meta**. Recommend AFTER A–C land and only if the
  owner wants the deeper city game (vs. the current "fill the district" model).

### Phase E — Optional flavour (decide per item)
- **Happiness stat** per building (affects population) — small Core add; adds a TB-flavour number.
- **Time Attack / Quick mode** — reuse the run loop with a timer / "tallest tower" goal.
- **Renovation (swipe-to-straighten)** — recovery mechanic; overlaps with Magnet, so probably skip.
- **Freeze** — owner already removed Slow-Mo; skip unless reconsidered.

---

## 5. Recommended sequencing
1. **Phase A** (combo → residents) — the single most Tower-Bloxx-faithful change; ~½ day, Core-tested.
2. **Phase B** (combo meter + juice) — makes A feel great; ~½ day, no Core risk.
3. **Phase C** (earned specialty blocks) — deepens the reward; ~½–1 day.
4. **Decide on Phase D** (Build-City puzzle) — big; only if we want the deeper city game.
5. Cherry-pick **Phase E** items.

A–C together (~1.5–2 days) deliver the core Tower Bloxx feel — **skillful stacking visibly fills your
building with people** — without re-bloating the meta. They sit cleanly on our existing
`TowerRun`/`ResidentFlyIn`/`HUDController` and stay deterministic + test-first.

## 6. Open questions for the owner
- **A:** combo resident bonus on *every* drop during a streak (TB-style), or only on Perfects?
- **A:** exact tier values (start from the score-chain tiers, then tune by feel)?
- **C:** every 4 perfects upgrade the next block, and/or a run-end trophy-roof population bonus?
- **D:** do we want the full colour-adjacency Build-City puzzle, or keep the current
  "fill the district to its goal" model?
- **E:** add a Happiness stat / a Time-Attack mode, or stay Endless-only for now?

---

### Sources
- Tower Bloxx Deluxe — review/walkthrough (JayIsGames): https://jayisgames.com/review/tower-bloxx-deluxe.php
- Tower Bloxx — review/walkthrough (JayIsGames): https://jayisgames.com/review/tower-bloxx.php
- Postmortem: Digital Chocolate's Tower Bloxx (Game Developer): https://www.gamedeveloper.com/design/postmortem-digital-chocolate-s-i-tower-bloxx-i-
- Tower Bloxx — Codex Gamicus: https://gamicus.fandom.com/wiki/Tower_Bloxx
- Tower Bloxx: New York — Achievement Guide (XboxAchievements): https://www.xboxachievements.com/game/tower-bloxx-new-york/guide/
- Tower Bloxx: New York — review (Windows Central): https://www.windowscentral.com/tower-bloxx-new-york-review
