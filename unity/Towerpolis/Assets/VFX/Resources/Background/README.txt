Drop your own animated-background art here to replace any layer.

Each layer loads a Texture2D by name (PNG with transparency). Put files here as:
  city      -> ground-level city skyline silhouette (wide, dark)   [EMPTY until you add it]
  cloud     -> drifting clouds (soft, fluffy)
  balloon   -> hot-air balloons / kites (small)                    [EMPTY until you add it]
  plane     -> planes / birds streaking across (small)
  aurora    -> aurora band (wide soft gradient, upper atmosphere)
  star      -> twinkling star / dot (small, bright)
  moon      -> moon / planet disc (space)

Example: Assets/VFX/Resources/Background/cloud.png  ->  loaded as "Background/cloud".

city + balloon are RESERVED slots: they have NO procedural placeholder, so they
stay empty until you drop a city.png / balloon.png here -- then they appear in
their proper place automatically. Every other name uses a procedural placeholder
until you replace it. No wiring -- just press Play.

Layers fade in/out over their own altitude band as you climb (atmospheric ascent,
GDD §4.9): city at street level -> clouds/balloons mid -> planes/aurora higher ->
stars + moon in space. Tints, sizes, drift and the fade bands are in
BackgroundLayer.cs (Defs) -- ask me to tweak any of them. Want more layers
(satellites, comets, Earth's curve)? Tell me the names and I'll add them.
