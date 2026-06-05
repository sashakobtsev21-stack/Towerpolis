# ADR-0009 — Cloud save behind an `ICloudSave` interface (local stub now, GPGS later)

**Status:** Accepted (2026-06-05) · **Phase:** 5 (scaffold) → wired in Phase 7 (Google Play Console)
**Relates to:** ADR-0007 (meta persistence; established the same "local-now / GPGS-later" seam for `ILeaderboardService`).

## Context
Phase 5 wants cloud save, but the real backend (Google Play Games Saved Games) needs a Google Play Console app + the GPGS Unity plugin imported — external setup that's Phase 7 homework and can't be done headless. We still want the seam in place now so the GPGS implementation drops in without touching the save flow.

## Decision
Introduce a thin **`ICloudSave`** abstraction in the Unity layer (`Towerpolis.Game.Meta`), with a no-op **`LocalCloudSave`** default and a settable **`CloudSave.Backend`** holder. `SaveManager.Save` pushes every local save to `CloudSave.Backend.Push(data)` (a no-op today). The local file at `persistentDataPath` stays the source of truth; the cloud is a sync target.

```
public interface ICloudSave
{
    bool IsAvailable { get; }              // a real backend is signed in & ready
    void Push(SaveData data);              // upload latest local save (fire-and-forget)
    void Pull(Action<SaveData> onResult);  // download cloud save (null if none/unavailable)
}
```

- `LocalCloudSave`: `IsAvailable = false`, `Push` no-op, `Pull → onResult(null)` → behaviour is exactly today's local-only save.
- `CloudSave.Backend` defaults to `LocalCloudSave`; at boot (Phase 7) set it to a `GpgsCloudSave` after sign-in.
- `SaveData` is unchanged (still in Core, string/engine-free). The interface uses it directly.

## Deferred to GPGS wiring (Phase 7), NOT built now
- **Sign-in flow** + `GpgsCloudSave : ICloudSave`.
- **Load reconciliation / conflict resolution.** `SaveManager.Load` stays local-first now. When GPGS lands, add a save **timestamp** to `SaveData` (schema v2→v3, with a `SaveMigration` step + Core tests) and reconcile cloud-vs-local by newest, with a conflict prompt if both advanced. Doing it now would be a speculative schema migration with no backend to test against.
- Throttling/queuing of `Push` (GPGS Saved Games has rate limits).

## Consequences
- New (Unity, Phase 5): `Assets/Game/Meta/CloudSave.cs` (interface + `LocalCloudSave` + `CloudSave` holder). `SaveManager.Save` gains one no-op call.
- Zero behaviour change today; zero Core/save-schema change.
- Phase 7: implement `GpgsCloudSave`, set `CloudSave.Backend`, add the save timestamp + reconciliation. The save call sites never change.
