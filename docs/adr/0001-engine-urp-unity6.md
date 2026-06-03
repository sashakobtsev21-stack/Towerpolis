# 0001 — Engine = Unity 6.3 (6000.3) + URP mobile

Status: accepted
Date: 2026-06-03

Context: We need a cross-platform (Android-first → iOS) 3D engine for a premium-feel casual
physics stacker, built solo + Claude Code, that can hold 60 fps on mid Android with good-looking
lighting and a small download. Candidates: Unity (URP/HDRP), Godot 4.

Decision: **Unity 6.3 LTS (installed: 6000.3.16f1), Universal Render Pipeline (mobile preset).**
The project is scaffolded from Unity's `3d-cross-platform` (URP) template, so URP render pipeline
assets (`Mobile_RPAsset` / `Mobile_Renderer`, PC variants, global settings, default volume) are
already wired. Portrait-locked, tablet-safe (GDD §5.1).

Consequences:
- Mature mobile toolchain (AAB, GameCI, Addressables, Localization, Play/AdMob SDKs), SRP Batcher,
  ASTC, broad device QA — directly serves the perf budget ([0003](0003-perf-budget.md)).
- URP (not HDRP): HDRP is desktop/console-class and would blow the mobile budget. The HDRP
  template (`3d-high-end`) is explicitly rejected.
- Couples us to Unity's licensing/runtime; acceptable for a solo commercial mobile game.

Alternatives rejected:
- **Godot 4** — lighter and OSS, but weaker mobile monetization/Play-services ecosystem, smaller
  3D mobile track record, and no in-house agent tooling tuned for it.
- **HDRP** — wrong performance class for mobile.
