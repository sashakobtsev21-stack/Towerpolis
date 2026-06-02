# Architecture Decision Records (ADRs)

Owned primarily by `unity-engine-architect`. Each foundational engine/tech decision is recorded here so the 15 disciplines don't drift. One file per decision: `NNNN-short-title.md`.

**Suggested template:**
```
# NNNN — <decision title>
Status: proposed | accepted | superseded by NNNN
Date: YYYY-MM-DD
Context: <the forces / problem>
Decision: <what we chose>
Consequences: <trade-offs, what it enables/costs>
Alternatives rejected: <and why>
```

## Decisions already taken in Phase 0 (to be formalized in Phase 1)
- **Engine = Unity 6.3 LTS**, URP mobile (not Godot/HDRP).
- **Physics = 2-phase hybrid:** Rigidbody only while a block falls → freeze + weld into one `Tower` → **scripted (fake) wobble**. Never a real stacked-rigidbody tower (explodes past ~10–12).
- **Scoring/grading = deterministic math in Unity-free `Towerpolis.Core`** (NUnit). Never derived from PhysX (not cross-device deterministic).
- **Backend = Google Play Games Services** (Android v1) + Firebase Remote Config (daily seed / live-ops); Unity Cloud Save for iOS later; not PlayFab.
- **Localization = Unity Localization, RU + EN** from launch.
- **Monetization = AdMob + IAP, dormant at launch** behind a remote kill-switch.

## To decide in Phase 1 (open)
- Addressables vs Resources for content streaming & download size.
- UI Toolkit vs UGUI for the UI layer (defer to `ui-ux-designer`).
- DOTween vs PrimeTween for tweening (GC profile decides).
- Exact perf budget numbers (frame time, draw-call cap, tri/texture/memory) per device tier.
