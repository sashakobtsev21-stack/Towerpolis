# 0006 — Tweening library = PrimeTween (proposed)

Status: proposed
Date: 2026-06-03

Context: The "satisfying tap" pillar is built from tween-driven juice — squash-stretch, elastic
settle, dust/score pops, `DOShake`-style camera shake (GDD §3). The GDD names DOTween throughout as
shorthand, but the perf budget ([0003](0003-perf-budget.md)) makes **"no per-frame GC in hot paths"
non-negotiable**, and the juice loop is the hottest path in the game.

Decision (proposed): **PrimeTween** as the tweening library.
- Zero-allocation by design (no GC during tweens) — aligns with the budget where the juice loop
  fires every drop.
- Free, single-package, simple API; no Pro license; good mobile track record.
- DOTween remains the fallback if a needed feature is missing; its GDD mentions are to be read as
  "tween-driven juice", not a binding library choice.

Status is **proposed**, to be confirmed by `gameplay-programmer` + `mobile-performance-engineer`
when the MVP juice is implemented (Phase 2), profiling the real GC/CPU cost of the settle+wobble+
shake stack on a mid device. Whichever wins, hot-path tweens must be pooled/zero-alloc.

Consequences:
- If confirmed: add `PrimeTween` in Phase 2; keep tween calls behind thin helpers so the library is
  swappable.
- If DOTween is chosen instead: budget for careful pooling to avoid its allocation patterns.

Alternatives rejected (for now):
- **DOTween (free)** — allocates per tween unless carefully pooled; easy to violate the GC budget.
- **Hand-rolled tweens** — reinventing easing/sequencing; only worth it for the few hottest cases.
- **Unity's built-in animation/Timeline** — too heavy/authoring-bound for code-driven micro-juice.
