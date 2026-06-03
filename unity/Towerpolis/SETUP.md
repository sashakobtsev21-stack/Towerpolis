# Towerpolis — Unity project setup

This folder **is** the Unity project (scaffolded from Unity's `3d-cross-platform` URP template for
**6000.3.16f1**, with `Towerpolis.Core` + `Towerpolis.Game` layered on). `Library/`, `Temp/`, and
the generated `*.csproj`/`*.sln` are git-ignored and regenerated on first open.

## First open
1. **Unity Hub → Add → Add project from disk →** select `unity/Towerpolis`.
2. Open it with **Unity 6000.3.16f1** (the exact version in `ProjectSettings/ProjectVersion.txt`).
   Hub will offer to install it if missing.
3. First import takes a few minutes (it pulls the packages in `Packages/manifest.json` and builds
   `Library/`). The URP render pipeline assets (`Assets/Settings/Mobile_RPAsset` etc.) are already
   assigned in **Project Settings → Graphics / Quality** — no manual wiring needed.
4. **Verify the scaffold:**
   - **Window → General → Test Runner → EditMode → Run All** → the `Towerpolis.Core.Tests`
     (XorShiftRng + DailySeed) pass in-editor, mirroring `dotnet test`.
   - **Project** window shows `Assets/_Core` (Runtime + Tests), `Assets/Game` (Data/DistrictDefinition),
     `Assets/Settings`, `Assets/Scenes/SampleScene`.

## Standalone Core tests (no editor needed)
```powershell
dotnet test core/Towerpolis.Core.Tests/Towerpolis.Core.Tests.csproj
```
Same source files as the in-editor tests (ADR [0005](../../docs/adr/0005-core-dual-test-harness.md)).
This is what CI runs on every push.

## Assembly layout
| Assembly | Location | Engine refs | Purpose |
|---|---|---|---|
| `Towerpolis.Core` | `Assets/_Core/Runtime` | **none** (`noEngineReferences`) | deterministic logic: daily seed, grading, scoring, population, economy |
| `Towerpolis.Core.Tests` | `Assets/_Core/Tests` | editor-only, NUnit | Core unit tests (also run via `dotnet test`) |
| `Towerpolis.Game` | `Assets/Game` | UnityEngine | MonoBehaviours, ScriptableObjects (e.g. `DistrictDefinition`); consumes Core |

Dependency direction is one-way: **Game → Core**. Core never references the engine.

## Pending (next phases — not done yet)
- **Android Build Support** module (install via Hub) before the first device build / GameCI.
- Import the gameplay block art from `art/blocks/fbx/` into `Assets/Art/Models/` (Phase 2, when the
  gameplay scene is built — see `docs/BLENDER_GUIDE.md` §3).
- Packages added later: `com.unity.addressables` (Phase 3, ADR 0004), `com.unity.localization`
  (Phase 5), AdMob/IAP (Phase 7).
- CI Unity job activates once `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` repo secrets exist.
