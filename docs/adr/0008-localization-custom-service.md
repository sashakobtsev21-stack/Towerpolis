# ADR-0008 — Localization: a lightweight custom `Loc` service (not the Unity Localization package)

**Status:** Accepted (2026-06-05) · **Deciders:** unity-engine-architect (ratified), studio-orchestrator
**Context phase:** Phase 5 (UI/UX + Localization, RU + EN)

## Context
All Towerpolis UI is built in **code** (`MetaHud`/`HUDController` construct every Canvas/Image/TMP_Text via `NewText`/`NewImage`/`Place`, self-bootstrapped — no prefabs/scenes with text). User-facing strings are hardcoded Russian literals, some composed at runtime with numbers. `Towerpolis.Core` is and must stay **string-free** (ADR-0002). The GDD requires RU + EN with **no hardcoded user-facing strings** and a language switch with no missing keys.

## Decision
Use a **lightweight custom localization service** (`Towerpolis.Game.UI.Loc`), **not** the Unity Localization package.

Rationale (tied to the all-code-UI reality):
- The package's value is its `LocalizedString`/`LocalizeStringEvent` components on prefab/scene text + the editor String Table workflow. We have **zero** prefab/scene text — nothing to attach drivers to. We'd pay the whole cost (Addressables-backed tables, settings asset, startup init, binary `.asset` tables that don't diff in PRs) to use ~5% of it, then write a code shim anyway.
- ~100–150 keys, solo dev, RU+EN: two reviewable C# dictionaries beat binary String Tables for authoring/diff/review.
- Runtime switch over **already-built code text** is the custom service's sweet spot: a `LocalizedLabel` helper re-resolves on a `Loc.LanguageChanged` event; dynamic labels re-paint via the existing `Refresh*`/`Populate*` paths.
- Mobile build size/startup: two static dictionaries vs Addressables locale loading. Respects ADR-0003 (perf budget). Strings stay **out of Addressables** (ADR-0004 reserves Addressables for district content).
- "No missing keys" is a trivial unit test on two dictionaries.

## Shape
- `Loc.T(key)` / `Loc.T(key, args...)` (the latter is `string.Format` — **never concatenate** localized fragments; word order differs).
- Keys are `const string` in `LocKeys`; RU/EN values in partial `LocTables` (`LocTables.Ru.cs` / `LocTables.En.cs`).
- Language persists in `PlayerPrefs` (`towerpolis.lang`), **not** in the deterministic save (Core/`SaveManager` stay locale-agnostic). First launch: `Russian` → RU, else EN (default-language *policy* is a game-director reach call; the mechanism is fixed here).
- Static captions use a self-registering `LocalizedLabel` (re-resolves on `LanguageChanged`); dynamic labels resolve via `Loc.T` in their existing repaint methods.
- Missing key → returns `"#key"` + `Debug.LogError` (dev) so a hole is loud, never blank/crash. A Unity EditMode test (`LocCompletenessTests`) asserts RU/EN key-set parity, no-empty-values, and `{n}` placeholder parity, plus catalog-id coverage.

## Consequences
- New: `Loc.cs`, `LocKeys.cs`, `LocTables.Ru.cs`, `LocTables.En.cs`, `LocalizedLabel.cs`, a Game-layer test asmdef + `LocCompletenessTests`.
- Catalog display fields (`MissionCatalog`/`AchievementCatalog` Name/Description, district/cosmetic names) become **keys** resolved at the display site; Core ids untouched.
- Revisit only if a third language with non-trivial plural/gender rules lands (then a CSV/XLIFF import or the package may pay off). RU plural agreement is avoided in copy for Phase 5 (game-designer copy review).
- Extends ADR-0004 by explicitly excluding strings from Addressables.
