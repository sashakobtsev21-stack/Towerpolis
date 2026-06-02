# Towerpolis — Midjourney Style Directions (pick one)

Reference: `C:\Users\Oleksandr\Desktop\imagestowerbloxx` (bright cartoon city, chunky window-blocks, residents parachuting in on colorful umbrellas, isometric, sunny blue sky). Tone locked = **cartoony**.

**How to use:** generate the *style sheet* prompt for each of the 4 directions below, compare, **pick ONE**, then lock consistency with `--sref <url-of-your-chosen-image>` on every later asset. All prompts are Midjourney **v7**. Always upscale → clean/cut in Photopea/Photoshop before use.

---

## Direction 1 — "Tower Bloxx revival" (closest to the reference) ⭐ my pick
Bright, glossy, isometric cartoon city; chunky apartment blocks with big friendly windows; sunny.
```
stylized cartoon city builder mobile game, chunky glossy apartment tower blocks with big bright windows and balconies, tiny characters parachuting in on colorful umbrellas, sunny blue sky, fluffy clouds, isometric, vibrant saturated colors, soft global illumination, clean readable silhouettes, playful, game asset concept sheet --ar 1:1 --v 7 --style raw
```

## Direction 2 — "Soft clay / claymation" (premium-casual, cozy)
Rounded matte clay shapes, tactile, pastel-pop; the "expensive casual" look (Gardenscapes-ish).
```
cute claymation style city builder, rounded matte clay apartment buildings, soft pastel-pop colors, tactile handmade feel, tiny clay characters with parachutes, soft studio lighting, shallow depth, isometric, mobile game art, cohesive style sheet --ar 1:1 --v 7 --style raw
```

## Direction 3 — "Vibrant low-poly" (clean, cheap to match in 3D)
Faceted low-poly, flat saturated colors; easiest to reproduce with Synty/Blender geometry.
```
vibrant low-poly cartoon city, faceted chunky buildings, flat saturated color blocks, minimal texture, clean geometric shapes, sunny sky, isometric, mobile game low poly art, asset concept sheet --ar 1:1 --v 7 --style raw
```

## Direction 4 — "Glossy toy / plastic" (candy, high-gloss premium)
Shiny toy-plastic buildings, candy colors, high gloss; punchy and premium (Toon Blast-ish).
```
glossy toy plastic city builder, shiny candy-colored apartment blocks, high gloss reflections, rounded chunky shapes, bright playful palette, tiny toy characters with parachutes, isometric, premium mobile game art, style sheet --ar 1:1 --v 7 --style raw
```

---

## App icon (you generate — here's the prompt)
Single hero tower + crane, readable at tiny size, vivid. Try a few seeds.
```
mobile game app icon, one cute chunky apartment tower with a tiny crane on top dropping a glowing block, a small parachuting character beside it, vibrant gradient sky, rounded bold shapes, high contrast, centered, glossy, no text --ar 1:1 --v 7
```
*Icon tips:* keep it **one clear focal object** (tower+crane), bold silhouette, no fine detail (it shrinks to ~48px). Generate 4–8, pick the most readable thumbnail. Make a **RU and EN-neutral** version (no words baked in).

## Background / atmosphere tiers (the §4.9 ascent — generate one per tier, as 9:16 backdrops)
Match your chosen direction's palette; these sit behind the 3D tower as parallax planes.
```
DIRECTION-STYLE cartoon backdrop, vertical, <TIER>, soft parallax depth, clean horizontal bands, mobile game background --ar 9:16 --v 7 --style raw
```
Swap `<TIER>` for: `city rooftops with trees and traffic` · `open blue sky with low clouds, kites and balloons` · `above the clouds, sea of clouds and sun glare, distant planes` · `upper atmosphere, thin air, faint aurora, earth curve beginning` · `edge of space, deep blue to black, first stars and satellites` · `outer space, stars, the curved earth below, the moon`.

## Resident reference (for the Mecanim character — `character-animator`)
```
cute chunky cartoon character with a colorful round parachute, falling/landing pose, bright saturated colors, clean silhouette, white background, mobile game character, T-pose reference and action pose --ar 1:1 --v 7 --style raw
```

---
*Consistency:* once you pick a direction, paste its best image URL as `--sref` on every subsequent prompt (icon, UI, backdrops, residents) so the whole game reads as one world. `prompt-engineer` agent can refine any of these further.
