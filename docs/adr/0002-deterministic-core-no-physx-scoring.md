# 0002 — Deterministic game logic in a Unity-free `Towerpolis.Core`; never score from PhysX

Status: accepted
Date: 2026-06-03

Context: The game's headline differentiators are a **daily seed** (every player worldwide gets the
same crane-sway pattern that day) and **fair, shared competition / leaderboards**. Both require
that the same inputs produce the same outcome on every device. Unity's PhysX is *not*
deterministic across devices/CPUs, and float/physics drift would let two players diverge on the
"same" seed — breaking fairness and enabling score manipulation.

Decision:
- All **grading, scoring, the daily seed, city population and economy math** live in
  **`Towerpolis.Core`**, a Unity-free C# assembly (`asmdef` with `noEngineReferences: true`,
  `netstandard2.1` when built standalone). No `UnityEngine` references.
- **Score/grade are NEVER derived from PhysX.** Physics (a Rigidbody) is active *only* while a
  block is falling; on contact the block is frozen, welded into the single `Tower` object, and a
  **scripted (fake) wobble** plays. The grade comes from **deterministic placement math** (overlap
  offset vs. the block below), not from where PhysX settles it.
- Determinism primitives: `XorShiftRng` (xorshift64*, pure integer math → identical sequence
  everywhere) and `DailySeed` (date → stable 64-bit seed, SplitMix64). Core reads no clock — the
  date is always an input — so it stays pure and testable. Golden regression tests lock the exact
  sequences so an algorithm change can never silently shift historical daily seeds.

Consequences:
- Daily seed is identical across devices (Phase 3 gate) and scores are reproducible/anti-cheat.
- A hard architecture boundary: Unity (MonoBehaviours/UGUI/ScriptableObjects) *consumes* Core; Core
  never depends up. Enforced by the asmdef and by the standalone build target.
- Real stacked-rigidbody towers (which explode past ~10–12 floors) are avoided by design.

Alternatives rejected:
- **Score from physics settle position** — not cross-device deterministic; unfair + cheatable.
- **Deterministic physics engine** — heavy, still risky on mobile FPUs; unnecessary since gameplay
  only needs deterministic *grading*, not a deterministic *simulation*.
