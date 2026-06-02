# Towerpolis — Design Pillars

*The three experiences the game must deliver. Owned by `game-director`. Every feature must serve a pillar; if it doesn't, cut it.*

## Pillar 1 — The satisfying tap
Every single drop must *feel* great: anticipation as the crane swings, a crisp tap, a juicy land (squash-stretch, dust, confetti, score-pop, a chime on Perfect). **Feel comes from juice, not polygon count.** If a drop isn't satisfying in isolation, nothing built on top of it matters. → owners: `game-designer` (feel spec), `gameplay-programmer`, `physics-programmer`, `vfx-artist`, `audio-designer`.

## Pillar 2 — Fair, rising tension
The challenge is skill, not luck. As the tower grows it gets **heavier and tenser** — the scripted wobble's amplitude rises and damping falls, so tall towers visibly lean and every drop matters more. The difficulty ramp must read as **learnable and fair**; a collapse the player feels they *caused* drives "one more try", a collapse that feels *random* drives churn. → owners: `game-designer` (curve), `physics-programmer` (the wobble), `game-qa-engineer` (fairness in playtest).

## Pillar 3 — A city you own and share
The reason to come back and the reason it spreads. Completed towers populate a **persistent 3D city** whose population grows with your perfect drops; a single **daily seed** (same crane pattern for everyone, that day) turns each session into a fair, shared competition. The proud moment — a record, a finished district, your skyline — auto-generates a clean shareable image/clip. **This is the entire zero-UA growth engine.** → owners: `game-director` (vision), `game-designer` (meta + daily), `gameplay-programmer`, `ui-ux-designer` (share), `3d-artist`/`technical-artist` (city look).

---

*Mobile reality constrains all three: thumb-reachable, instantly readable, short sessions, runs on mid-tier Android. Beauty that drops frames is a bug.*
