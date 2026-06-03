# 0005 — `Towerpolis.Core` dual-test harness (one source, `dotnet test` + Unity Test Runner)

Status: accepted
Date: 2026-06-03

Context: Per [0002](0002-deterministic-core-no-physx-scoring.md) the deterministic logic must be
testable, and the workflow rule is *test-first on Core, commit only when green*. We want a fast
standalone test loop (no editor launch) for CI and local TDD, **and** the same tests to run inside
the Unity Test Runner so engine-side contributors see them too — without duplicating test code.

Decision: **One copy of the source, two build systems pointing at it.**
- The Core runtime and its NUnit tests live in the Unity project under
  `unity/Towerpolis/Assets/_Core/Runtime/**` and `unity/Towerpolis/Assets/_Core/Tests/**`, with
  Unity `asmdef`s (`Towerpolis.Core` `noEngineReferences:true`; `Towerpolis.Core.Tests` editor-only,
  `nunit.framework.dll`).
- A standalone solution at `core/` (`Towerpolis.Core.slnx`) has two SDK-style projects whose
  `<Compile Include>` **globs those same `.cs` files**: `Towerpolis.Core` (→ `netstandard2.1`,
  warnings-as-errors) and `Towerpolis.Core.Tests` (→ `net10.0`, NUnit + NUnit3TestAdapter).
- Tests use **only `NUnit.Framework`** (no `UnityEngine`, no `[UnityTest]`), so the identical files
  compile and pass under both runners. `dotnet test` is the source of truth in CI; Unity Test Runner
  is the in-editor view.

Consequences:
- Sub-second `dotnet test` feedback; CI needs only the .NET SDK to verify Core (no Unity license).
- `netstandard2.1` on the standalone Core build acts as a guard rail: anything Unity's scripting
  runtime can't use won't compile.
- Discipline required: Core stays Unity-free and tests stay framework-pure, or the dual build breaks
  (which surfaces immediately).

Alternatives rejected:
- **Separate `/core` source copied/synced into Unity** — two copies drift.
- **Unity Test Runner only** — slow loop, needs an editor/license in CI, can't `dotnet test`.
- **A shared precompiled DLL imported into Unity** — loses in-editor source/debugging and couples
  build order.
