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

## 3. Core Loop Spec (crane → drop → grade → settle → residents)

*Locked from the player's answers. Classic Tower-Bloxx timing feel (catch the swinging crane), cartoony tone, endless-until-you-fail with per-district height goals.*

1. **Crane** swings the next floor block left↔right above the tower top. Player **times the swing** — this is the core skill. Swing period **increases only slightly** with height (kept *minimal* per design); the real rising tension comes from the wobble. Deterministic per-run seed (a **daily-seed** variant is a later meta mode, §4.2).
2. **Drop** — player taps; the block detaches and falls as a **Rigidbody**. *Only moment real physics is active.*
3. **Grade** — on contact, **deterministic placement math** (Unity-free Core lib, NOT physics) computes overlap offset vs. the block below:
   - **Perfect** (tiny offset) — no slice; tower straightens a touch; **bonus residents** + perfect-chain++; "PERFECT!" pop + chime.
   - **Good / Sloppy** (partial overlap) — the **overhang is sliced off** (the **roof or balcony falls away**); adds lean. Counts as a **miss strike**.
   - **Miss** (no meaningful overlap) — block tumbles off; **miss strike**.
   - **2-strike rule (forgiving, per player):** the run does **not** end on the first bad drop — the overhang is shaved and you continue. On the **2nd miss the tower topples and the run ends** (a big, funny cartoony collapse). *(cumulative vs. consecutive = a `game-designer` tuning call.)*
4. **Settle & weld** — the landed block freezes and welds into the single `Tower` object. Juice: squash-Y → DOTween elastic settle; dust ring; **minimal** haptic tick; camera `DOShakePosition` scaled by misalignment. **Camera** tracks **up** as the tower grows; **on a miss the camera pulls back to reveal the whole swaying tower** (then the topple if it's the 2nd).
5. **Wobble** — the welded Tower runs **scripted sway** (fake spring); amplitude grows / damping shrinks with height → tall towers visibly lean and feel heavy/tense. (Real rigidbody stacks explode past ~10–12 — we **fake the wobble**, always.)
6. **Residents arrive on EVERY placed floor** — they **parachute / float down on umbrellas** onto the new floor (Tower-Bloxx signature), with **chatter SFX** on arrival. **Population per floor type:** standard floor = **2**, balcony floor = **3**, premium floor = **4**. A Perfect drop adds **bonus residents**. *(Resident behaviour/variety = a design TODO, §4.1.)*
7. **Backdrop ascends** — every ~10 floors the background steps up through the atmosphere (rooftops & trees → open sky → clouds → stratosphere → stars → space…). See §4.9.
8. **End** (2nd miss / topple, or reaching a district goal) → the completed tower **deposits into the persistent city grid**; population += sum of its residents; high score / leaderboard updates; a share card can auto-generate at the pride moment.

**Scoring rule:** score = f(height, perfect-ratio, perfect-chain, residents housed). **Never** derived from PhysX (non-deterministic across devices → cheating + breaks any shared-seed fairness).

---

## 4. Systems

### 4.1 Build City + Districts (meta spine)
- Persistent 3D city grid. Each completed run deposits its tower onto a plot; **city population** (the meta score) = the sum of residents housed across all towers.
- **Residents = population, per floor type** (from the core loop): standard floor = **2**, balcony floor = **3**, premium floor = **4**; a Perfect drop adds bonus residents. So *what* you build and *how cleanly* both feed the meta. Residents **parachute / float in on umbrellas onto every placed floor** with chatter SFX (the Tower-Bloxx signature). **Resident design TODO (`game-designer`):** do resident *types* differ only visually per district, or do special residents (VIP/premium) grant extra population/score? Decide in Phase 4.
- The city is split into **districts (районы)**. **Each district has its own identity on three axes:**
  1. **Architecture** — the building/floor *style you stack* changes per district (e.g. cozy brick low-rises → glass offices → neon high-rises → pagodas → gingerbread houses). Same gameplay block, different mesh/material set.
  2. **Residents** — the parachuting characters *look and animate differently* per district (business folk, neon punks, kimono characters, elves…). Distinct idle/land/celebrate clips.
  3. **Skybox + palette + music** — backdrop, hero colors, and music bed are themed per district.
- **Growth model (matches the release cadence):** ship with **3 districts**; **add a new city/district with each release / seasonal event** (Downtown → Neon → Winter → Sakura → Beach → Steampunk …). Each new district is a **data + art-pack drop on one shared system** (building variant set + resident variant set + skybox + palette + music) — *not* new code, so it's solo-sustainable. New districts can be **server-gated** (Remote Config) to release without an app update.
- **Per-district loop:** a district has a fill goal (populate its grid / reach a population) → completing it **unlocks the next district** and grants a reward; each district has its **own leaderboard** (see 4.4). This gives the daily-score core a long-horizon collection/progression spine.
- **Production note (`game-designer` + `technical-artist` own this):** define the district as a `DistrictDefinition` ScriptableObject (building set, resident set, skybox, palette, music, unlock cost, board id) so new districts are authored as data. See [`../ART_BIBLE.md`](../ART_BIBLE.md) for the seasonal art *system*.

### 4.2 Daily Seed (heartbeat)
- One global crane/sequence per day, generated deterministically by the Core lib (xorshift/seed-by-date).
- Its own midnight-reset leaderboard. Powers streaks, fair competition, shareability, daily opens.

### 4.3 Progression & Upgrades
- **Crane upgrades (chosen 3 to start):** **(1) Magnet / auto-center** — gently pulls the block toward alignment; **(2) Slow-mo charge** — briefly slows the swing on demand; **(3) Extra life** — survive one extra miss (raises the 2-strike limit). *(Others — slower sway, wider tolerance — kept as later additions.)*
- **Cosmetics economy:** block/tower skins, city themes/districts, crane skins (soft **coins** + premium **gems** — two currencies, see §4.8).
- **Streaks + daily login rewards:** escalating rewards, 7-day milestone (the 2.3× daily-engagement inflection), streak-freeze/catch-up to stop churn-after-a-miss.
- **Missions:** weekly ("build 5 towers >30 floors", "land 10 perfect drops"), achievements, prestige ("rebuild a megacity").
- **Free progression track** (battle-pass-shaped, free at launch; paid premium track later).

### 4.4 Leaderboards & Social — **solo first** (per player's choice)
- **v1 = solo:** personal high scores + per-district bests + achievements. **No social pressure at launch.**
- **Added later (when social goes in):** **(a)** friends board (Google Play Games / later Game Center) as the primary driver; **(b)** weekly-reset global (winnable window); **(c)** per-district boards so newcomers aren't crushed. **No all-time global board as headline** (demotivates ~95%).
- **PvP** (async "build-off" vs a friend on the same seed) is a *later* possibility, not v1.

### 4.5 Sharing / Virality (the entire UA budget)
- Auto-generate a clean **vertical tower image / short collapse clip / "My City" panorama** with score + deep link, surfaced **at the peak-pride moment** (record run, district complete).
- One-tap **"Share" / "Challenge a friend on today's seed."** Reward sharing with cosmetic currency.
- Make the wobble/collapse genuinely juicy and funny so clips spread on their own.

### 4.6 Menus / Settings / Localization
- Main menu, settings (sound/music sliders, language, haptics, quality, reset/restore, privacy/consent).
- **RU + EN at launch** via Unity Localization string tables. No hardcoded user-facing strings. Store listings RU+EN too (localization is an explicit Apple featuring criterion).

### 4.7 Authentication, Identity & Cloud Save
*Principle: frictionless first. Never gate play behind a login.*
- **Guest by default** — fully playable on first launch with a **local** profile; no sign-in required. **Show a one-time warning** that guest progress lives only on this device and is lost on uninstall/new device — with a "Sign in to save" prompt.
- **Android: Google Play Games Services (GPGS) sign-in** (optional, one tap) = **cloud-saved progress** + (later) leaderboards/achievements/friends. The frictionless way to not lose your city. (Game Center on iOS later.)
- **iOS (later): Apple Game Center** for the same, plus **Sign in with Apple** if a custom account is ever needed.
- **Cross-device / cross-platform sync (later):** a lightweight account layer (Firebase Auth or Unity Authentication) only if/when we need a single identity across Android↔iOS. Defer until iOS.
- **Privacy-light:** collect the minimum; GDPR/Play data-safety/ATT consent handled in settings; no PII beyond the platform identity. `security-auditor` reviews the auth/save path before each store submission.

### 4.8 Bonuses & Currency (earned now → purchasable later)
*Two currencies; everything launches **earnable**, purchasable layers come later (and never pay-to-win).*
- **Coins (soft, earned):** from every run (height + perfects), daily login, missions, district completion, watch-a-rewarded-ad doubler (later). Spent on crane upgrades + common cosmetics.
- **Gems (premium):** earned **sparingly** at launch (milestones, achievements, streak peaks); **purchasable later** via IAP. Spent on premium cosmetics, district skins, battle-pass premium, continues.
- **Bonuses you EARN (live at/after the core):** daily login ladder, streak milestones (7-day = the engagement inflection), weekly mission rewards, district-completion rewards, perfect-chain score bonuses, "first win of the day" bonus, achievement payouts.
- **Bonuses you BUY (later, post-retention):** gem packs, starter pack, cosmetic bundles, season/battle-pass premium track, "remove ads", convenience powerups (extra continue, crane-slow charge). All cosmetic/convenience.
- **Sequencing:** the earned economy is built in Phase 4 (data-driven, currency math in `Towerpolis.Core` with NUnit tests); the purchasable layer is wired dormant in Phase 7 and switched on only after retention gates pass.

### 4.9 Atmospheric Ascent (backdrop progression) — *signature visual*
*The player's idea, and a strong one: the higher you build, the higher into the sky you climb. Make it grander and "much wider" than a simple swap.*
- The backdrop **steps up through altitude tiers** as the tower grows (≈ every 10 floors, tunable):
  **Street level** (other rooftops, trees, traffic, birds) → **Open sky** (low clouds, kites, balloons) → **Cloud sea** (above the clouds, sun glare, planes) → **Upper atmosphere / stratosphere** (thin air, jet streams, aurora hint, curvature begins) → **Edge of space** (deep blue→black, first stars, satellites) → **Space** (stars, the Earth's curve below, moon, the odd UFO) → and beyond if they keep going.
- **Make it feel vast:** smooth **gradient/parallax transitions** between tiers (not hard cuts), a slowly shifting **sky gradient + sun/star position**, **parallax cloud/star layers**, ambient props per tier (balloons → planes → satellites → comets), and a subtle **color-grade + music-intensity shift** as you ascend. Crossing a tier is a small celebratory beat ("☁️ Above the clouds!").
- **Cheap & mobile-safe:** tiers are **2D parallax planes / skybox blends + a few pooled props**, not heavy 3D — owned by `rendering-engineer` + `vfx-artist` + `technical-artist` against the perf budget. Per-district palette recolors the same tier system (§4.1), so it composes with districts.
- This is **both** a difficulty-reward feedback (you *see* how high you got) and a share-worthy visual (a tower piercing into space). Reference: see [`../ART_BIBLE.md`](../ART_BIBLE.md).

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

### 5.1 Definition of success & platform targets (player-stated)
- **Primary success = a beautiful game with superb physics, gameplay, sound — visual + game-feel above all.** Quality-first; downloads/revenue are secondary outcomes that follow quality. Every scope trade-off favors *feel and polish* over feature count.
- **Devices:** target **normal/mid phones at 60 fps** — **not** chasing the weakest devices. **Orientation: portrait, locked.** **Tablet support: yes** (responsive layout / safe areas — `ui-ux-designer`).
- These set the perf budget posture for `unity-engine-architect` + `mobile-performance-engineer`: 60 fps on a mid device, portrait, tablet-safe, beauty that holds frame rate.

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

**Developer payout note (defer — resolve before Phase 7, not now):** the dev is a **Ukrainian citizen based in Georgia**, holding **RF self-employed (самозанятый) status + RF cards, plus Ukrainian and Georgian bank cards.** This is a *flexibility*, not a constraint — Google Play & Apple payouts work cleanly to a **Georgian** bank account / sole-proprietor (and many indies register a Georgian Individual Entrepreneur for a low, simple tax regime). Decision deferred: pick the **publishing entity + payout account** (Georgia is the natural fit) when monetization actually turns on. RuStore/alt-stores are an *optional extra* channel, not required. First priority per the dev: **ship the game — build, tune, and art it — money plumbing comes later.**

---

## 8. Top Risks & Mitigations

1. **Zero-UA / organic-growth reality (existential).** → Retention + shareability + featuring *are* the UA strategy. Hit 35/15/5, engineer peak-emotion share cards, keep Vitals clean, submit Apple/Google featuring on schedule. Model $0–low-hundreds/month until featured.
2. **3D + premium + animation = solo scope sink.** → Buy one Synty kit (don't build a city kit), model only blocks in Blender, AI-3D only for one-off props, **invest saved time in DOTween juice.** Cap the parachuting-resident work at one reusable character + one clip.
3. **Physics-as-difficulty backfires / cheating.** → **Fake the wobble** (freeze + weld + scripted sway), score from deterministic Core-lib math, tune the difficulty ramp in soft launch so collapse feels skill-based.
4. **Hyper-casual / weak meta → ~0% D30.** → Ship the Build-City meta + daily-seed loop as a first-class milestone right after the core is proven fun; lead leaderboards with friends + weekly-reset + per-district.
5. **Performance / Android Vitals + Frankenstein art (silent killers).** → Enforce the perf budget (<200 draw calls, SRP Batcher, pooling, shared materials, strip Synty demo scenes) and the one-palette art bible + mandatory recolor pass.

*Secondary watch-items:* live-ops over-commitment → templatize, cap ~30–35 hrs/week; monetizing too early → stay ad-free until 4.0+; trademark → run USPTO/EUIPO/Rospatent clearance on "Towerpolis" pre-launch.

---

*Design pillars: [`pillars.md`](pillars.md). See [`../MARKET_ANALYSIS.md`](../MARKET_ANALYSIS.md) for the numbers, [`../BUILD_PLAN.md`](../BUILD_PLAN.md) for the phased agent plan, [`../ART_BIBLE.md`](../ART_BIBLE.md) for art/audio, [`../adr/`](../adr) for architecture decisions.*
