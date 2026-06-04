Drop a downloaded particle EFFECT PREFAB here to replace the built-in placeholder.

Exact names (a .prefab whose root has a ParticleSystem):
  dust       -> plays at each landed floor
  confetti   -> plays on a Perfect hit

Example: Assets/VFX/Resources/Vfx/confetti.prefab  ->  loaded as "Vfx/confetti".
When present, GameVfx instantiates it at the right spot, plays its particle
system(s), and cleans it up automatically. If absent, the code-built placeholder
is used. No wiring needed — press Play.

Tip: the prefab should be a ONE-SHOT burst (looping = off, Stop Action = Destroy
is fine too). Keep it mobile-light (a few hundred particles max).

See docs/ASSETS_GUIDE.md for where to download good effects and how to make a prefab.
