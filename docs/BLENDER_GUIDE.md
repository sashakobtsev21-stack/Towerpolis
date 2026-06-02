# Towerpolis — Blender Guide (the droppable blocks) + BlenderMCP connector

You only need Blender for the **gameplay blocks** (the floors you stack). The city/background comes from Synty + Midjourney (see `ART_BIBLE.md`). Two ways to make the blocks: **(A)** model them by hand with the steps below, or **(B)** let Claude model them for you over the **BlenderMCP** connector (recommended — see the bottom).

---

## 1. What to model (5 small meshes)

All share the **same square footprint** so they stack cleanly and overhang-slicing works on the X/Z offset.

| Mesh | Purpose | Notes |
|---|---|---|
| `Floor_Standard` | the common floor | houses **2** residents · big friendly windows |
| `Floor_Balcony` | floor with a balcony | houses **3** · small balcony ledge + railing on the front |
| `Floor_Premium` | fancy floor | houses **4** · taller windows, a cornice/trim (gold material slot) |
| `Roof_Cap` | tops the finished tower | slightly tapered; optional water-tank/antenna |
| `Base_Ground` | the foundation the tower starts on | a slightly wider plinth |

**Specs (keep them uniform):**
- **Footprint:** 2 × 2 Unity units (= 2 m in Blender). **Height:** 1 unit per floor.
- **Origin/pivot:** **bottom-center** of each mesh (so gameplay places a block by putting its bottom at the tower's current top — clean, predictable stacking). *(This is the authoritative pivot rule; ignore any looser "top face" wording elsewhere.)*
- **Low-poly:** a floor should be a few hundred tris max. Cartoony, chunky, readable.
- **Material slots (so Unity recolors per district):** `Body`, `Windows` (emissive-ish), `Trim`. Flat saturated colors — recolor per district at runtime.

## 2. Hand-modeling steps (beginner-friendly)

1. **New file.** Delete the default cube (X → Delete) to start clean, or reuse it.
2. **Add the body.** `Shift+A → Mesh → Cube`. Open the N-panel (`N`) → **Item → Dimensions** → set **X 2, Y 2, Z 1**.
3. **Sit it on the floor.** Move it up so its bottom is at Z=0 (`G Z 0.5` if it's centered on origin). Then set the pivot: `Object → Set Origin → Origin to 3D Cursor` with the 3D cursor at the world origin (`Shift+C` resets the cursor to 0,0,0). Now the origin is at the bottom-center. ✅
4. **Windows.** Enter Edit Mode (`Tab`), select the 4 side faces, **Inset** (`I`, drag) to make window panels, optional shallow **Extrude inward** (`E`, then `-0.05`). Assign those faces the `Windows` material. (For the cartoony look you can keep it flat and let the texture/material do the work.)
5. **Balcony variant.** Duplicate the body (`Shift+D`), rename `Floor_Balcony`. On the front face, extrude a thin ledge out (`E Y 0.3`) and add a simple railing (a few thin cubes or an array). Keep it low-poly.
6. **Premium variant.** Duplicate, rename `Floor_Premium`. Taller window strips, add a top **cornice** (a slightly wider thin slab), give the trim the `Trim` material (gold).
7. **Roof_Cap.** A 2×2×~0.5 block, taper the top a touch (select top face, `S 0.8`), optional tiny antenna/water-tank.
8. **Base_Ground.** A 2.4×2.4×~0.6 plinth (a bit wider than the floors).
9. **Names matter** — name each object exactly as in the table (Unity uses these).

## 3. Export to Unity (FBX)

`File → Export → FBX (.fbx)` with:
- **Limit to: Selected Objects** (export one block, or all together).
- **Transform → Apply Scalings: `FBX All`**; **Forward `-Z Forward`**, **Up `Y Up`**; **Apply Unit ✔**.
- **Geometry → Apply Modifiers ✔**; Smoothing: `Face` or `Normals Only`.
- Save into `unity/Towerpolis/Assets/Art/Models/` (LFS-tracked via `.gitattributes`). One file `TowerBlocks.fbx` with all 5 named meshes is fine, or one file each.
- In Unity: import scale should read as 2×2×1 units. If it's 100× off, re-check "Apply Unit / FBX All".

## 4. Pivot recap (why bottom-center)
Gameplay drops a block; on land it freezes at `position.y = currentTowerTopY`; the next block's bottom-center sits exactly on the previous top. Overhang slicing compares the new block's X/Z vs the one below. Bottom-center origin makes all of that trivial. `gameplay-programmer` + `physics-programmer` rely on this.

---

## 5. BlenderMCP connector — let Claude model the blocks for you ⭐

**Answer to "can you make the connector yourself?":** the established **BlenderMCP** (`ahujasid/blender-mcp`) *is* exactly that connector and is mature — writing a custom one would be redundant and less robust, so we use it. With it connected, **Claude can create/modify the meshes in your Blender directly** (you just connect and confirm). It's already wired in [`.claude/mcp.json`](../.claude/mcp.json) as the `blender` server (`uvx blender-mcp`).

**One-time setup (Windows):**
1. **Install `uv`** (Python runner that provides `uvx`):
   `powershell -c "irm https://astral.sh/uv/install.ps1 | iex"`  (or `pip install uv`)
2. **Get the addon:** download `addon.py` from `https://github.com/ahujasid/blender-mcp`.
3. **Install it in Blender:** `Edit → Preferences → Add-ons → Install…` → pick `addon.py` → tick **"Interface: Blender MCP"**. (Blender **3.6+**.)
4. **Connect:** in the 3D viewport press `N` → **BlenderMCP** tab → **Connect to MCP server**.
5. **Use it:** open Claude Code **rooted in this repo** (so `.claude/mcp.json` loads) with Blender open + connected. Then ask the `3d-artist`/`technical-artist` flow to "model the 5 Towerpolis blocks per BLENDER_GUIDE §1" and Claude drives Blender.

**Caveats:** Blender must stay open and connected; the connector executes Blender Python, so only run trusted prompts; don't run `uvx blender-mcp` yourself in a terminal — the MCP client launches it.

**Source:** [github.com/ahujasid/blender-mcp](https://github.com/ahujasid/blender-mcp) · [README](https://github.com/ahujasid/blender-mcp/blob/main/README.md)
