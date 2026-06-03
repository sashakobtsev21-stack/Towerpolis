# 0003 — Performance budget (mobile, non-negotiable)

Status: accepted
Date: 2026-06-03

Context: "Beauty that drops frames is a bug" (pillars). Success is defined as a beautiful game with
superb feel at a smooth frame rate, targeting **mid-tier phones at 60 fps, portrait, tablet-safe**
(GDD §5.1) — not chasing the weakest devices. Weak Android Vitals = auto-exclusion from Play
featuring, which is effectively our entire UA budget. The budget is set now so every discipline
designs against it from day one.

Decision — the budget every feature is held to:

| Axis | Target |
|---|---|
| Frame rate | **60 fps on a mid-tier 2021+ Android** (e.g. Snapdragon 7-series). ≤ ~14 ms CPU main-thread to leave thermal/headroom. Graceful **30 fps floor** on low tier via a quality tier. |
| Draw calls (SetPass) | **< 200**, SRP Batcher ON; shared materials; recolor via MaterialPropertyBlock/atlas, not new materials. |
| Triangles on screen | ≈ **≤ 150k**. (Gameplay blocks are 324–768 tris; a 40-floor tower ≈ 30k — backdrops/props get the rest.) |
| Textures | ASTC-compressed; atlas where possible; hero ≤ 1024, most ≤ 512. Backdrops are 2D parallax/skybox blends, not heavy 3D (GDD §4.9). |
| GC | **No per-frame allocations in hot paths** (drop loop, scripted wobble, camera follow). Pool blocks, residents, VFX, audio sources. No `Update()` LINQ/boxing/string concat. |
| Memory | Stay within a mid-phone budget; stream non-launch district content via Addressables ([0004](0004-addressables-for-content.md)). |
| Download | First-run install lean (target < ~150 MB); extra districts delivered as Addressable content. |

Owners: `unity-engine-architect` sets it; `mobile-performance-engineer` profiles real devices each
phase; `rendering-engineer`/`vfx-artist`/`technical-artist` design within it; `game-qa-engineer`
gates Vitals (crash/ANR) and frame time before any store submission (Phase 8 gate).

Consequences:
- Forces pooling, shared materials, the SRP Batcher, and a 2D-parallax backdrop strategy.
- A profiling pass is a standing per-phase task, not a one-off at the end.

Alternatives rejected:
- **"Optimize later"** — late perf work on mobile routinely means rewrites; the budget is cheaper
  enforced continuously.
- **Targeting the lowest-end devices** — explicitly out of scope (GDD §5.1); we pick beauty at
  60 fps on mid-tier over universal reach.
