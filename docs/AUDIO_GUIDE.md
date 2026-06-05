# Towerpolis — Adding your own sounds & music

The game plays **your** audio files. There's no wiring: name the files correctly,
drop them in the right folder, press **Play**. Missing files are simply silent, so
you can add sounds one at a time.

## 1. The 6 files you need

| Name (lowercase) | When it plays | Vibe | Length |
|---|---|---|---|
| `land`    | every floor that lands (Good **and** Perfect) | soft wooden *tap/clack*, not harsh — you hear it a lot | 0.1–0.3 s |
| `perfect` | a Perfect hit | bright **tonal** *chime / bell / coin / ding* (it pitches **up** with combos, so pick a clean musical tone) | 0.2–0.5 s |
| `miss`    | a missed block | *whoosh / swoosh* or a soft “nope” blip | 0.3–0.6 s |
| `topple`  | the run ends (tower collapses) | *crash / rubble / thud* | 0.5–1.5 s |
| `start`   | a new run begins (optional) | light *pop / ui-start* | 0.1–0.3 s |
| `theme`   | background music (loops) — the **fallback** used by any district without its own track | calm, casual, cheerful / lo-fi — nothing tiring | 30–90 s, **seamless loop** |

### Music can be per-district (optional)

Each district can have its own looping track. The game **crossfades** to it when you switch
district (in the City view). Drop these in `Music/` and they're picked up automatically:

| Name | District | Vibe (design brief) |
|---|---|---|
| `downtown` | Downtown / «Центр»            | upbeat jazzy / acoustic, friendly |
| `neon`     | Neon Quarter / «Неоновый квартал» | synthwave / lo-fi, pulsing |
| `winter`   | Winter Heights / «Зимние высоты»  | gentle orchestral / celtic, cosy |

Any district **without** its own file just plays `theme`. If you only ship `theme`, the music
keeps playing seamlessly when you switch districts (no restart). An Endless **retry never
restarts** the track — it only changes when the district changes.

## 2. Where to put them

```
unity/Towerpolis/Assets/Audio/Resources/
    Sfx/
        land.wav
        perfect.wav
        miss.wav
        topple.wav
        start.wav
    Music/
        theme.ogg          # fallback, used by any district with no track of its own
        downtown.ogg       # optional per-district beds (crossfade on district switch)
        neon.ogg
        winter.ogg
```

- Extension can be **.wav** or **.ogg** (both import fine). The name (without extension)
  must match exactly, **lowercase** — `land`, `perfect`, `miss`, `topple`, `start`, `theme`
  (and optionally `downtown`, `neon`, `winter` for per-district music).
- Drop the file into the folder, switch to Unity (it imports automatically), press Play.

## 3. Where to get free sounds (CC0 / royalty-free)

Best for a casual game — all free, no attribution headaches:

- **kenney.nl/assets?q=audio** — CC0 game-audio packs (UI, impacts, “Casino/Interface” SFX). Great `land`/`perfect`/`start` here.
- **freesound.org** — huge library; **set the license filter to “Creative Commons 0”**. Good for `miss`/`topple`/whooshes.
- **pixabay.com/sound-effects** and **pixabay.com/music** — royalty-free SFX + music, no attribution.
- **mixkit.co/free-sound-effects** and **/free-stock-music** — free, casual-friendly.

For the **music**, search “lofi loop”, “casual game loop”, “puzzle background”, “cozy” — and
check it loops cleanly (or trim it to a bar so the end meets the start).

## 4. Format / import tips (optional — defaults already work)

- **Mono** for SFX (they're 2D feedback), stereo is fine for music.
- Select a clip in Unity → Inspector to tweak the importer:
  - SFX: *Load Type* = **Decompress On Load**, *Compression* = PCM or Vorbis.
  - Music: *Load Type* = **Streaming**, *Compression* = **Vorbis**, quality ~70%.
- Keep SFX peaks from clipping; the game already mixes at sensible volumes.

## 5. Tuning volume / behaviour (optional)

By default the audio component is added automatically at runtime. To get live sliders,
add the **GameAudio** component to the GameObject (the one with `TowerGameController`):

- **Master Volume**, **Music Volume**, **Play Music**
- **Perfect Climbs** — Perfect pitches up with the combo (turn off for a constant pitch)
- **Land Pitch Jitter** — tiny random pitch on `land` so repeats don't feel robotic
- **Music Crossfade** — seconds to fade between district beds on a switch (default 0.9 s)
- You can also drag clips straight into the **SFX / per-district Music** slots instead of
  using Resources (`downtownMusic` / `neonMusic` / `winterMusic`, plus the fallback theme).

## 6. How it's hooked up (for reference)

`Assets/Game/Audio/GameAudio.cs` subscribes to the controller's gameplay events
(`FloorAdded`, `PerfectHit`, `StrikeAdded`, `RunToppled`, `RunStarted`) and plays the
matching clip through a small AudioSource voice-pool. On `RunStarted` it also picks the
active district's music bed and **crossfades** to it (two looping AudioSources) when the
district changed. This is the temporary MVP setup; the audio-designer agent replaces it
with authored SFX + middleware (FMOD/Wwise) later.
