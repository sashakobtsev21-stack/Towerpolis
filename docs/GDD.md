# Towerpolis — Game Design Document

*Source of truth. Synthesized from a 6-track market/tech/monetization/art/naming/retention analysis (mid-2026). Keep this file updated as decisions change.*

---

## 1. Verdict & Positioning

Build it — but as a **hybrid-casual** game with a **city-building spine**, NOT a pure hyper-casual stacker. With **zero paid UA**, growth lives or dies on retention: a pure tap-stacker retains ~38–40% D1 but collapses to ~0% D30, which cannot compound organically. Wrapping the proven Tower Bloxx core in a persistent **"Build City"** meta pushes us toward the ~10% D30 zone where word-of-mouth and store featuring snowball.

**The one sharp positioning angle:**
> *"The daily-seed stacker where your towers build a living city."*

- **Daily seed** — every player worldwide gets the **same crane-sway pattern each day** (Wordle-for-stackers). Fair, shared, competitive, automated → near-zero ongoing content cost.
- **Build City** — every completed run physically deposits its tower into a persistent **3D metropolis** that grows in population. A city you own and show off.

No existing stacker (Stack, Tower Crash 3D, the dormant Tower Bloxx) offers this combination. That is the wedge.

---

## 2. Design Pillars

1. **Every tap must feel satisfying.** Juice (squash-stretch, wobble, confetti, score-pop, screen shake) is the "premium feel" — *not* polygon count.
2. **Fair, learnable difficulty.** Taller = heavier and tenser via a *tuned* wobble, never random/unfair. Random-feeling collapse = D1 churn.
3. **The city is the reason to return.** Completed towers populate a persistent 3D metropolis; perfect runs = more residents = higher population.
4. **A shared daily ritual.** One global daily seed everyone competes on; "I got rank 412 today" social hook.

---

## 3. Core Loop Spec (crane → drop → grade → wobble → bonus)

1. **Crane** swings the next block left↔right above the tower top. Oscillation period is driven by the **daily seed** (deterministic, identical for all players that day) and **shortens as the tower grows**.
2. **Drop** — player taps; the block detaches and falls as a **Rigidbody**. *This is the only moment real physics is active.*
3. **Grade** — on contact, **deterministic placement math** (in the Unity-free Core lib, NOT physics) computes overlap offset vs. the block below →
   - **Perfect** (offset ≤ ~5%) — no slice, full bonus, resident spawns, perfect-chain++.
   - **Good** (offset ≤ ~25%) — minor Stack-style overhang slice.
   - **Sloppy** (offset ≤ ~50%) — larger slice, adds lean.
   - **Miss** (no meaningful overlap) — block falls away; run ends (or costs a life/continue).
4. **Freeze & weld** — the landed block immediately freezes and welds into the single `Tower` object. Juice fires: squash-Y → DOTween elastic settle; dust ring at contact; camera `DOShakePosition` scaled by misalignment.
5. **Wobble** — the welded Tower runs **scripted sway** (fake spring). Amplitude grows and damping shrinks with height, so tall towers visibly lean and feel heavy. (Real rigidbody stacks explode past ~10–12 boxes — we **fake the wobble**, always.)
6. **Bonus** — a **Perfect** triggers confetti + a **parachuting resident** (Mecanim falling/landing clip) + "PERFECT!" punch-scale pop + rising chime. Perfect-chains escalate chime + resident count.
7. **End** → tower deposits into the **persistent city grid**; population +N (N scaled by perfect-ratio); daily-seed leaderboard updates; a share card auto-generates at the pride moment.

**Scoring rule:** score = f(height, perfect-ratio, perfect-chain bonus). **Never** derived from PhysX (non-deterministic across devices → cheating + breaks daily-seed fairness).

---

## 4. Systems

### 4.1 Build City (meta spine)
- Persistent 3D city grid. Each completed run deposits its tower onto a plot.
- Perfectly-aligned towers spawn more parachuting residents → higher **city population** (the meta score).
- Districts/themes unlock at population milestones (Downtown → Neon → Winter → …). Each is a recolor + prop swap on the base tower (seasonal art *system*, not bespoke).

### 4.2 Daily Seed (heartbeat)
- One global crane/sequence per day, generated deterministically by the Core lib (xorshift/seed-by-date).
- Its own midnight-reset leaderboard. Powers streaks, fair competition, shareability, daily opens.

### 4.3 Progression & Upgrades
- **Crane upgrades:** slower sway, wider drop tolerance, magnet/guide, slow-mo charge.
- **Cosmetics economy:** block/tower skins, city themes/districts, crane skins (soft + hard currency).
- **Streaks + daily login rewards:** escalating rewards, 7-day milestone (the 2.3× daily-engagement inflection), streak-freeze/catch-up to stop churn-after-a-miss.
- **Missions:** weekly ("build 5 towers >30 floors", "land 10 perfect drops"), achievements, prestige ("rebuild a megacity").
- **Free progression track** (battle-pass-shaped, free at launch; paid premium track later).

### 4.4 Leaderboards (design carefully)
- **(a) Friends board** — *primary* retention driver (Google Play Games / later Game Center).
- **(b) Weekly-reset global** — winnable window + pre-reset login spike.
- **(c) Per-district/theme boards** — newcomers aren't crushed by veterans.
- **No all-time global board as headline** — demotivates ~95% of players, hurts retention.

### 4.5 Sharing / Virality (the entire UA budget)
- Auto-generate a clean **vertical tower image / short collapse clip / "My City" panorama** with score + deep link, surfaced **at the peak-pride moment** (record run, district complete).
- One-tap **"Share" / "Challenge a friend on today's seed."** Reward sharing with cosmetic currency.
- Make the wobble/collapse genuinely juicy and funny so clips spread on their own.

### 4.6 Menus / Settings / Localization
- Main menu, settings (sound/music sliders, language, haptics, quality, reset/restore, privacy/consent).
- **RU + EN at launch** via Unity Localization string tables. No hardcoded user-facing strings. Store listings RU+EN too (localization is an explicit Apple featuring criterion).

---

## 5. Target Metrics (instrument from day one in GameAnalytics)

| Metric | Target (2026 realistic) |
|---|---|
| Retention D1 / D7 / D30 | **35% / 15% / 5%** |
| Store CVR | ≥ 25% |
| Rating | ≥ 4.0 (gates featuring) |
| Sessions/user/day | ~4+ (genre norm ~4.1) |
| Rewarded opt-in | ≥ 40% |
| Crash/ANR (Android Vitals) | low — weak vitals = auto-exclusion from Play promotion |

Treat **D1 < 30%** as "core not fun yet" — a design signal, not a content problem.

---

## 6. MVP Definition (strict vertical slice)

**The single question the MVP must answer: does tap-to-drop + grade + wobble feel satisfying within week one?** Nothing else ships until that is a "yes."

**In scope:**
1. One crane swinging one block left↔right (period tied to a hardcoded seed; speeds up with height).
2. Tap → drop → Rigidbody fall → contact.
3. **Deterministic placement grading** (Perfect/Good/Sloppy/Miss) + Stack-style overhang slicing — in the Core lib with NUnit tests.
4. Freeze + weld + **scripted wobble** (amplitude grows with height).
5. **Full juice loop:** squash-stretch, elastic settle, dust ring, misalignment-scaled camera shake, confetti + "PERFECT!" pop + chime on perfect.
6. One Blender block set (3–4 floor variants + cap) recolored to the locked palette; one Midjourney background/skybox.
7. Score + height counter; one local high score; instant restart ("one more try").
8. GameAnalytics D1 instrumentation hooked.

**Explicitly OUT of MVP (defer until core is proven fun):** city meta, parachuting residents, daily seed + leaderboards, streaks/missions, upgrades/shop, IAP/ads, RU localization, menu polish, settings, multiple districts, battle pass, share cards. *(Daily seed + city are the headline differentiators — but worthless if the 10-second core isn't fun, so they come in the very next milestone, not the MVP.)*

**MVP gate:** people *want* "one more try" and the wobble reads as tense-but-fair. Only then build the city + daily-seed layer; then soft-launch in 1–2 cheap tier-2 markets and validate **D1 ≥ 30% / D7 ≥ 12%** before any global push.

---

## 7. Monetization (free-to-play, monetization-ready, ON after retention is proven)

**Sequencing is critical:** launch **generous and ad-free** to chase a 4.0+ rating and word-of-mouth. Heavy ads/paywalls before that tank reviews and disqualify you from featuring (the main growth lever). Wire all hooks now; flip them on once **D1 ≥ 30% / D7 ≥ 12% / rating ≥ 4.0**.

**Ads (AdMob + mediation):**
- **Rewarded video (primary):** "continue the run" (one revive/attempt), "2× coins on this tower", "today's-seed retry", daily-bonus doubler. ~6–10 views/session, daily cap ~15–20. Tier-1 Android rewarded eCPM ~$16–45.
- **Interstitial (light):** between runs, frequency-capped (1 per 3–4 runs, ≥60–90 s apart). Tier-1 eCPM ~$10–14.
- **Banner:** optional, city/meta menu only. Lowest priority.

**IAP:** Remove Ads ($2.99–3.99) · cosmetics (block/tower/crane skins, city themes) · gem packs · battle pass (free track at launch, paid premium later). **Never pay-to-win.**

**Realistic zero-UA expectation (honest):** scale is the constraint, not eCPM. ~15K DAU × ~5 sessions × ~1.5 rewarded/session ≈ $3–9K/month from ads; hybrid IAP roughly doubles ARPDAU. **Reaching 15K DAU organically is the hard part** — expect months 1–3 in the hundreds-to-low-thousands DAU unless featured (a top slot can deliver ~+470% installs — effectively the whole UA budget). **Model $0–low-hundreds/month pre-featuring.**

**RU-specific note:** Google Play / App Store payouts and card payments for RF-based devs are constrained (sanctions/withdrawal friction). Decide the publishing entity / payout route early (foreign entity, alt stores like RuStore as a secondary channel, etc.).

---

## 8. Top Risks & Mitigations

1. **Zero-UA / organic-growth reality (existential).** → Retention + shareability + featuring *are* the UA strategy. Hit 35/15/5, engineer peak-emotion share cards, keep Vitals clean, submit Apple/Google featuring on schedule. Model $0–low-hundreds/month until featured.
2. **3D + premium + animation = solo scope sink.** → Buy one Synty kit (don't build a city kit), model only blocks in Blender, AI-3D only for one-off props, **invest saved time in DOTween juice.** Cap the parachuting-resident work at one reusable character + one clip.
3. **Physics-as-difficulty backfires / cheating.** → **Fake the wobble** (freeze + weld + scripted sway), score from deterministic Core-lib math, tune the difficulty ramp in soft launch so collapse feels skill-based.
4. **Hyper-casual / weak meta → ~0% D30.** → Ship the Build-City meta + daily-seed loop as a first-class milestone right after the core is proven fun; lead leaderboards with friends + weekly-reset + per-district.
5. **Performance / Android Vitals + Frankenstein art (silent killers).** → Enforce the perf budget (<200 draw calls, SRP Batcher, pooling, shared materials, strip Synty demo scenes) and the one-palette art bible + mandatory recolor pass.

*Secondary watch-items:* live-ops over-commitment → templatize, cap ~30–35 hrs/week; monetizing too early → stay ad-free until 4.0+; trademark → run USPTO/EUIPO/Rospatent clearance on "Towerpolis" pre-launch.

---

*See [`MARKET_ANALYSIS.md`](MARKET_ANALYSIS.md) for the numbers, [`BUILD_PLAN.md`](BUILD_PLAN.md) for the phased agent plan, [`ART_BIBLE.md`](ART_BIBLE.md) for art/audio.*
