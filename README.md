# Towerpolis 🏙️

> **Towerpolis** — *Stack the city to the sky.* / *Строй город до небес.*
> A premium-feel casual **3D physics tower-stacker** for **Android (iOS later)**, built solo + Claude Code, using the **Ruflo** agent framework (`../agents/ruflo`).

A modern take on the *Tower Bloxx* core loop: a crane swings a building block, you tap to drop it, it must land cleanly on the floor below. Perfect drops keep the tower straight and parachute new residents in; sloppy drops make the tower **lean and physically wobble** — the taller it gets, the heavier and tenser placement becomes. Completed towers populate a persistent **3D city** you grow and show off.

**The wedge (what no competitor does):** a **daily-seed** stacker (everyone worldwide gets the same crane pattern each day — Wordle-for-stackers) **+** a persistent, shareable **"Build City"** metropolis.

---

## Status

| | |
|---|---|
| **Phase** | 0 — Concept & GDD ✅ (this repo) → next: Phase 1 (Unity project + Core lib + CI) |
| **Engine** | Unity 6.3 LTS (URP, mobile) |
| **Platforms** | Android first → iOS later |
| **Languages** | RU + EN from launch |
| **Monetization** | F2P, monetization-ready (ads + IAP), switched **on after** retention is proven |
| **Working name** | `Towerpolis` (store form: *Towerpolis: Sky City* / *Towerpolis: Город в облаках*) — backups: `Cranetop`, `Highstack` |

## Documents (source of truth)

- [`docs/game/GDD.md`](docs/game/GDD.md) — Game Design Document (core loop, systems, MVP)
- [`docs/game/pillars.md`](docs/game/pillars.md) — the 3 design pillars
- [`docs/MARKET_ANALYSIS.md`](docs/MARKET_ANALYSIS.md) — market, competitors, naming, sources
- [`docs/BUILD_PLAN.md`](docs/BUILD_PLAN.md) — phased build plan + the 15-agent studio per phase
- [`docs/ART_BIBLE.md`](docs/ART_BIBLE.md) — art direction, palette, Midjourney prompts, 3D/audio pipeline
- [`docs/adr/`](docs/adr) — architecture decision records
- [`.claude/agents/game-dev/`](.claude/agents/game-dev) — the 15 Unity game-dev subagents (from `my_agents`)
- [`CLAUDE.md`](CLAUDE.md) — engineering conventions + how this project drives the agents

## Tech stack (decided)

Unity 6.3 LTS · URP mobile · **2-phase physics** (Rigidbody only while a block falls → freeze + weld → *scripted* wobble) · deterministic **scoring in a Unity-free Core C# lib** (NUnit, runs via `dotnet test`) · Google Play Games Services (leaderboards/saves) · Firebase Remote Config (daily seed / live-ops) · Unity Localization (RU+EN) · DOTween (+PrimeTween if needed) · GameAnalytics · AdMob (dormant at launch) · GameCI on GitHub Actions.

## License

Proprietary — all rights reserved (placeholder; choose a license before any public release).
