Drop your Blender resident models here to replace the procedural placeholder figures.

The fly-in (ResidentFlyIn.cs) loads umbrella residents by name:
  Resident_Umbrella_1
  Resident_Umbrella_2
  Resident_Umbrella_3   (a random one is picked per resident)

Export them from scripts/blender_build_residents.py as FBX into THIS folder
(Assets/Art/Resources/Residents/), e.g. Resident_Umbrella_1.fbx. They load as
"Residents/Resident_Umbrella_1". If none are present, code-built placeholder
figures (capsule body + sphere head + a squashed-sphere umbrella) are used so the
effect works without art.

Model expectations:
  - Origin at the feet, facing -Z (toward the camera), roughly 1.5-2 units tall
    in Blender (the script auto-uses the model's authored scale).
  - Name the umbrella canopy object "Umbrella" if you want per-resident colour
    tinting; otherwise the model keeps its own materials.

When a floor lands, residents = the floor's tenant count drift in from the side
and slightly above and parachute INTO the new block. Tune the arc / count cap /
timing in ResidentFlyIn.cs.
