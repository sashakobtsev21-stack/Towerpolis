# Towerpolis — Art & Audio Bible

*Lock this NOW. The #1 solo-dev failure is "Frankenstein" art from mixing sources. Every asset — bought, modeled, or AI — gets a mandatory recolor/retone pass to this bible before it enters the project.*

---

## 1. Style Lock

**Stylized low-poly · vibrant saturated palette · chunky high-contrast silhouettes · flat / baked lighting.**

Why: cheapest 3D style, runs trivially on low-end Android, hides asset-mismatch sins, and matches the reference (bright cartoon city, fat blocks, parachuting residents). **"Premium feel" comes from juice, not polygon count** — budget time on animation/feel, not fidelity.

### Hero palette (pick 4–6, then freeze)
Draft direction (tune to taste): warm coral/red, sky cyan, sunny yellow, mint green, deep navy accent, off-white. Lock the hexes in this file once chosen and apply to all materials and UI.

### Rulebook
- One outline/lighting convention across everything (flat + soft GI / baked).
- Box-friendly, readable shapes; strong silhouette at thumbnail size.
- Recolor every imported Synty/AI/Blender asset to the hero palette.

---

## 2. Asset Pipeline (role separation resolves the 3D-vs-2D tension)

| Need | Tool | Notes |
|---|---|---|
| **App icon, store feature graphic (1024×500) + screenshots, full UI kit (buttons/panels/coin·star·gem icons), background cityscape + skybox plane, texture refs, all marketing** | **Midjourney v7** (`--style raw`) | 2D only. Always upscale → clean/cut in Photopea/Photoshop. Never ship raw output. *Verify MJ commercial terms on your paid plan.* |
| **3D city / building kit (meta-screen, proportion + palette anchor)** | **Synty POLYGON** | Buy ONE: **POLYGON MINI City** (272 mobile-optimized assets, 6 color variants) or POLYGON City Pack (331). Grab the **free POLYGON Starter Pack** first to confirm fit. **Strip to only needed prefabs** — full demo scenes tank low-end devices. *Confirm shipped-game license: perpetual vs. sub-active; buying the pack outright is the safer path.* |
| **The droppable gameplay "blocks"** | **Blender** (free) | Model these yourself: 5–8 modular floors (residential/office/shop) + roof caps + ground base. **Pivots at the TOP face** for stacking; collider-friendly proportions; recolor to palette. Gameplay-critical control you can't buy. |
| **One-off landmark / hero props only** | **AI-3D (scalpel, not main tool)** | **Rodin Gen-2** (cleanest topology, pay-per-download ~$0.50+) or **Tripo P1** (game-ready low-poly, mobile-tuned, ~$12/mo). Budget ≤$15/mo, expect retopo in Blender. **Do NOT source the kit from AI** (topology debt). Meshy 6 = best iteration but "frequently needs topology cleanup". Skip Luma Genie (NeRF capture, not generative). |

**Cost reality:** polished baseline ≈ **$30–60** (one Synty pack/sub ~$30 + Midjourney ~$10/mo + free Blender/DOTween/audio).

---

## 3. Juice Stack — where "premium" actually comes from

- **DOTween** (free): squash-stretch on land, elastic settle, score-pop punch-scale, camera `DOShakePosition`. *(Consider **PrimeTween** — zero-alloc — if profiling shows GC hitches on low-end.)*
- **Unity built-in Particle System:** confetti (perfect drop), dust ring (impact), sparks. Keep counts low for low-end Android.
- **Mecanim:** the parachuting resident — **one reusable rigged character + one parachute/fall/land clip.** Do NOT over-build this; it's the biggest art-labor trap. Expand only if retention data justifies it.

### Juice recipe for THIS game
- **On land:** squash block (scale Y↓ / X↑) → DOTween elastic ease back; spawn dust ring at contact; tiny camera `DOShakePosition` scaled by misalignment.
- **Perfect-align:** confetti burst + parachuting resident + "PERFECT!" score pop (DOScale punch + fade) + rising chime; chain escalates chime + resident count.
- **Tower height:** increase wobble amplitude / decrease damping with height → tall towers visibly lean and feel heavy/tense (the core game feel and main differentiator).

---

## 4. Audio (CC0-first to avoid attribution friction in a monetized app)

- **SFX:** Kenney.nl (CC0, no attribution), Pixabay Sounds (commercial-free), Freesound (**CC0 only**), Sonniss GDC bundles (game-licensed).
- **Music loop:** incompetech / Kevin MacLeod (CC-BY — **credit required**) or a cheap Epidemic Sound / Soundstripe sub for an exclusive signature theme.
- ⚠️ Avoid shipping CC-BY without crediting in-app — real store/legal risk. Prefer CC0 for the commercial release.

---

## 5. Ready-to-use Midjourney v7 prompts

**Style sheet:**
```
mobile game art, stylized low-poly cartoon city, chunky cute apartment buildings, bright saturated colors, soft global illumination, clean readable silhouettes, isometric, white background, game asset concept sheet --ar 1:1 --v 7 --style raw
```

**App icon:**
```
app icon, single cute pastel skyscraper tower with a tiny crane on top, rounded chunky shapes, vibrant gradient sky, centered, mobile game icon, high contrast, glossy --ar 1:1 --v 7
```

**UI kit:**
```
mobile game UI kit, casual city builder, candy/wooden rounded buttons, panels and frames, coin star and gem icons, vibrant, clean vector-like, game GUI sheet --ar 16:9 --v 7 --style raw
```

**Background cityscape / skybox plane (sits behind the 3D tower):**
```
stylized low-poly cartoon city skyline far background, soft pastel sky, fluffy clouds, sunny, depth haze, clean horizontal composition, game backdrop --ar 9:16 --v 7 --style raw
```

**Parachuting resident concept (reference for the Mecanim character):**
```
cute chunky low-poly cartoon character with a colorful round parachute, falling pose, bright saturated colors, clean silhouette, white background, game asset --ar 1:1 --v 7 --style raw
```

*Use `--sref` with a locked reference image to keep style consistent across generations. Budget Photoshop/Photopea cleanup for every output.*

---

## 6. Risks (art)

- **Frankenstein syndrome** → one palette + mandatory recolor pass; standardize on a single Synty-style family.
- **Over-investing in 3D fidelity** → players never notice poly count; weak juice makes the game feel dead. Feel/animation is the higher-ROI work.
- **AI-3D topology trap** → only for isolated props; prefer Tripo/Rodin; retopo in Blender.
- **License landmines** → verify MJ commercial terms; CC0 audio only (or credit CC-BY); confirm Synty shipped-game rights.
- **Imported-kit perf** → strip demo scenes, atlas textures, watch draw calls (<200, SRP Batcher on).
