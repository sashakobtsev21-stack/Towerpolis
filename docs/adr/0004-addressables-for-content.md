# 0004 — Addressables for content (districts), not Resources

Status: accepted
Date: 2026-06-03

Context: The meta is a growing set of **districts**, each a data + art pack (building set, resident
set, skybox, palette, music — see `DistrictDefinition`). The growth model ships 3 districts and
adds one per release/seasonal event, and critically wants new districts to be **server-gated via
Remote Config and released without an app update** (GDD §4.1). We also have a lean first-install
download target ([0003](0003-perf-budget.md)).

Decision: **Use Unity Addressables for district/content delivery.** The first (launch) districts
are bundled in the build; later districts are Addressable content groups that can be downloaded on
demand and gated by Remote Config. The deterministic rules stay in `Towerpolis.Core`; only *assets*
are Addressable. The `com.unity.addressables` package is added when this work starts (Phase 3) — it
is intentionally **not** in the Phase-1 manifest to keep the initial scaffold minimal and clean.

Consequences:
- Smaller first install; new districts shippable as content (no store re-review for the asset pack).
- Requires an Addressables build/release step in CI and content-catalog hosting (Phase 3 / 9).
- Some upfront complexity (groups, labels, remote catalog) vs. a flat build.

Alternatives rejected:
- **Resources/** — everything ships in the build (bloats download), no streaming, no remote gating;
  Unity itself discourages it for anything but tiny always-needed assets.
- **Raw AssetBundles** — Addressables is the supported higher-level layer over bundles; hand-rolling
  bundles is more error-prone for one developer.
