# Towerpolis — gameplay blocks (committed art)

The droppable tower floors. Previously these lived only in a live Blender session
(built from `scripts/blender_build_blocks.py`); they are now committed as real assets
so the art can't be lost with the session.

![block lineup](preview.png)

## Files
| File | What |
|---|---|
| `Towerpolis_Blocks.blend` | Master editable source — all 6 blocks + stage (lights/ground). Open this to edit. |
| `glb/<Name>.glb` | One self-contained GLB per block, **bottom-center pivot at world origin**, full PBR materials. |
| `fbx/<Name>.fbx` | Unity-native FBX per block — Y-up / −Z forward, unit-scaled, modifiers applied. Same pivot. |
| `glb/Towerpolis_Blocks_all.glb` | All 6 in one file, authored layout (showcase / quick import). |
| `preview.png` | Flat-colour Workbench reference render (above). |

## The 6 blocks
`Floor_Standard` (green) · `Floor_Balcony` (yellow) · `Floor_Balcony_2` (orange) ·
`Floor_Premium` (blue, full-width balcony) · `Base_Ground` (terracotta brick) ·
`Base_Ground_2` (sandstone). Footprint 2×2, ~324–768 tris each. Pivot = bottom-center
(per `docs/BLENDER_GUIDE.md` §1/§4 — clean stacking + overhang slicing).

## Regenerate
Headless, no GUI / no socket needed:
```powershell
& "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" `
  --background --python scripts/blender_export.py            # rebuild + export all formats
& "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe" `
  --background --python scripts/blender_preview_lineup.py    # refresh preview.png only
```
Edit the recipe in `scripts/blender_build_blocks.py`, then re-run the export.

## Into Unity (Phase 1+)
Copy the `fbx/` (or `glb/`) into `unity/Towerpolis/Assets/Art/Models/`. Import scale should
read 2×2×1 units; material slots recolor per district at runtime.
