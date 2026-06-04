# Towerpolis — Getting better-looking assets (VFX, materials, models, skyboxes)

How to make the game stop looking like a prototype: drop in pro effects and art from outside.
Everything here plugs into the existing drop-in systems (no code needed from you).

> **Biggest win first (free):** the "kindergarten" look is mostly **flat lighting + the brown
> skybox**, not the flat colours. A *look-dev pass* — soft shadows, ambient occlusion, a gradient
> sky, bloom + colour grading — turns the same low-poly houses premium (think *Townscaper* /
> *Monument Valley*). I (Claude) can do that pass in code/URP settings — just ask. The downloads
> below are for going further.

---

## 1. Cool VFX (particles / effects)

### Where to download
**Unity Asset Store** (open in Unity: Window ▸ Asset Store, or assetstore.unity.com):
- **Unity Particle Pack** — FREE, by Unity. High-quality example effects (explosions, sparkles, magic).
- **Cartoon FX Remaster (Free)** by Jean Moreno — FREE sample of the hugely popular stylized FX set; great confetti/poofs/impacts. Paid full pack is cheap.
- **POLYGON Particle FX** (Synty) — matches the low-poly look (paid).
- Search **“confetti”**, **“stylized vfx”**, **“toon fx”**, **“impact”** — filter by Free + sort by rating.

**Other free sources:**
- **kenney.nl** — “Particle Pack” textures (CC0) if you want to build your own in Shuriken.
- **OpenGameArt.org** — CC0 particle sprites.

### How to plug it into the game (no code)
The game auto-loads effect prefabs by name:
1. Import the pack (double-click the `.unitypackage` or install from Asset Store).
2. Find the effect **prefab** you like in the pack (a GameObject with a `ParticleSystem`).
   Drag it into the scene, press Play to preview; tweak if you want.
3. Make it a one-shot: on the root ParticleSystem set **Looping = off**.
4. Drag that prefab into **`Assets/VFX/Resources/Vfx/`** and **rename it exactly**:
   - `confetti` → fires on a Perfect
   - `dust` → fires on every landed floor
5. Press Play. The code uses your prefab instead of the placeholder and cleans it up itself.

> Want more hooks (miss puff, topple debris, level-up burst)? Ask and I’ll add `miss`/`topple`
> prefab slots the same way.

---

## 2. Better-looking blocks (colour / texture / whole models)

Three levers, cheapest-impact first:

### A) Look-dev (FREE, biggest effect) — ask me to do it
URP post-processing + lighting: soft shadows, **ambient occlusion (SSAO)**, a **gradient skybox**,
**bloom**, **vignette**, **colour grading / tonemapping**, a warm key light + fill. No assets needed.
This is what makes flat low-poly read as “premium”. I can set it up in the project.

### B) PBR textures on the current houses (CC0 — free, no attribution)
Give the walls real brick/plaster/wood instead of flat colour:
- **ambientCG.com** — CC0 PBR materials (Bricks, Plaster, Concrete, WoodFloor, Roof…). Best source.
- **polyhaven.com/textures** — CC0 PBR, high quality.
- **sharetextures.com**, **cc0-textures**, **texturecan.com** — more CC0.

How to apply: import the texture set → on the block’s Material set **Base Map** (albedo),
**Normal Map**, and a **Mask/Roughness** map → tweak Smoothness. (Tell me which materials and I can
script the import/assignment, or set up a small material set.)

### C) Replace the house models with a pro kit (drop-in, like our FBX)
This is the fastest jump to “real game” art. The game loads block models from
**`Assets/Art/Resources/Blocks/`** by name — drop new FBX/GLB there using the SAME names
(`Floor_Standard`, `Floor_Balcony`, `Floor_Premium`, `Base_Ground`, …) and they replace the current ones.

Low-poly city kits:
- **Synty Studios — POLYGON City / POLYGON Town** (assetstore.unity.com, paid ~$15–40) — the
  industry-standard stylized look; coherent palette out of the box. (Our `ART_BIBLE.md` already
  targets Synty POLYGON.)
- **Kenney — City Kit / Buildings** (kenney.nl, **FREE CC0**) — clean, simple, great free option.
- **Quaternius — Ultimate Modular/City packs** (quaternius.com, **FREE CC0**).
- **KayKit / KayLousberg** (kaylousberg.itch.io, free/cheap) — charming stylized kits.

> Note: the current houses already auto-recolour by material name. A new kit will use its own
> materials/textures — if it imports white, tell me and I’ll wire a recolour/material pass for it
> (same trick as the current blocks).

### D) Skybox / background (free, big mood change)
- **polyhaven.com/hdris** — CC0 HDRIs (sunny sky, studio) → set as the skybox for nice lighting.
- **Asset Store**: search **“gradient skybox”** / **“stylized skybox”** (many free) — a soft
  gradient sky instantly kills the muddy brown.
- Or I can add a simple **gradient skybox** in code as part of the look-dev pass.

---

## 3. Licensing (don’t skip)
- **CC0** (ambientCG, Poly Haven, Kenney, Quaternius) — use freely, even commercially, no credit. Prefer these.
- **Unity Asset Store** — covered by the Asset Store EULA for use in your game; don’t redistribute the raw assets.
- **CC-BY** (some freesound/itch) — fine but you must **credit** the author (keep a CREDITS.txt).
- Avoid anything “editorial only”, “non-commercial”, or with unclear licrenses for a game you’ll publish.

## 4. Import tips (mobile)
- Textures: max size **1024** (or 512 for small props), **ASTC** compression for Android, sRGB on
  for albedo / off for normal & mask maps.
- Models: keep tris low; enable **Mesh Compression**; one shared atlas/material where possible
  (we’re under a **<200 draw-call** budget).
- VFX: prefer Shuriken (CPU) on mobile; cap particle counts; avoid heavy overdraw / huge soft particles.

---

### TL;DR
- **VFX:** grab *Cartoon FX Free* / *Unity Particle Pack*, drop a prefab named `confetti`/`dust` into
  `Assets/VFX/Resources/Vfx/`. Done.
- **Blocks looking childish:** first let me do the **free look-dev pass** (lighting + post + sky). Then,
  for more, drop a **Kenney/Synty** city kit into `Assets/Art/Resources/Blocks/` (same names) or add
  **ambientCG** PBR textures to the materials.
