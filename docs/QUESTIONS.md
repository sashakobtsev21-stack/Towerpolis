# Towerpolis — Discovery Questions (30)

*Answer at your pace — partial answers are fine, and every item has a sensible default in the GDD if you skip it. These shape design/economy/art/tech decisions before and during the build. Answer inline (e.g. "4. target height per district") or just give bullet points.*

## A. Vision & scope
1. **Core input** — closer to classic **Tower Bloxx** (time the swinging crane and drop) or to **"Stack"** (a block slides side-to-side, tap to lock)? Or a blend? *(decides the primary mechanic)*
2. **Tone** — cute/cartoony (bright, funny collapses), sleek/premium-minimal, or semi-realistic city? Pick the vibe.
3. **Run length** you imagine — ~30 sec, ~1–2 min, or "endless until you miss"?

## B. Gameplay & feel
4. **Win condition** per run — endless height until a miss, a target height per district/level, or both as separate modes?
5. **Miss handling** — one miss = game over (hardcore), 3 lives, or Stack-style "shave the overhang and keep going until the block is too small"?
6. **Main difficulty lever** — the crane swing speed, the tower wobble/balance, or both stacking together as you climb?
7. **Beginner assist** — show a "perfect zone" guide at first (removed by skill/upgrade), or pure skill from drop one?
8. **Camera** — locked vertical follow, or pull back to reveal the whole swaying tower as it grows tall?
9. **Haptics** — vibration on land / perfect on by default?

## C. Progression, economy & bonuses
10. **Crane upgrades** — rank what matters most: slower sway · wider tolerance · magnet/auto-align · slow-mo charge · extra life.
11. **Currencies** — two (coins earned + gems premium) OK, or one currency only at launch?
12. **Daily login + streaks** — in from the first public build, or added a bit later?
13. **Achievements/missions** — a light starter set at launch, or a bigger system from the start?

## D. City & districts (meta)
14. **Districts at launch** — 1, 2, or 3? And a rough target for how many over the first year.
15. **District unlock** — by population/score milestone, by spending coins, or by completing the previous district?
16. **Residents** — purely visual flavor, or do different resident types give gameplay/score effects (e.g. a VIP resident = bonus population)?

## E. Social, leaderboards & auth
17. **Friends competition** at launch (Google Play Games friends board) or solo/global leaderboards first?
18. **PvP later?** — async "build-off" vs a friend on the same daily seed, or leaderboards only, ever?
19. **Sign-in** — guest + optional one-tap Google sign-in is enough? Any reason you'd want email/account login early?

## F. Monetization
20. Confirm **ads OFF at launch → on after retention**, or should I also cost out a **"premium, no-ads, IAP-only"** alternative?
21. **IAP set** — remove-ads · cosmetics · gem packs · battle pass: anything you'd drop or add (e.g. one-time "full unlock")?

## G. Art & style
22. **Look reference** — do you have example images/games whose style you want to match? Send them (or I'll propose 2–3 Midjourney directions to pick from).
23. **Blender** — will you model the droppable blocks yourself, or should we lean on bought Synty + AI-3D so you never open Blender?
24. **App icon** concept lean — tower+crane, city skyline, a cute character, or want me to generate options?

## H. Audio
25. **Music vibe** — chill/lo-fi, upbeat/playful, or big/city? Is a unique signature track worth a small budget, or are free CC0 loops fine for v1?

## I. Tech, platform & devices
26. **Lowest device** we must run well on — do you have a cheap Android test phone? Is 30 fps acceptable on low-end, or must it be 60?
27. **Orientation** — portrait locked confirmed? Tablet support needed, or phones-only at launch?

## J. Process, business & success
28. **Time budget** — realistic hours/week you'll put in (sets the pace and how tightly we scope).
29. **First thing you want to see/feel fastest** — a playable wobble prototype, a pretty menu/visual, or the city? *(We still build core-first, but this tells me what to show you first.)*
30. **Success for v1** — "a polished game I'm proud of", a download number, "first revenue from ads", or "get store-featured"? *(This shapes every trade-off.)*

---
*Bonus context if handy: your Midjourney plan tier (affects upscales/consistency), whether you already have Unity 6.3 + a Google Play Console account, and any hard deadline (e.g. a seasonal launch you're aiming at).*

---

## ✅ Answers — locked 2026-06-03 (folded into the GDD)
- **A1 crane** time the swinging crane (Tower-Bloxx feel). **A2 tone** cartoony. **A3 run** endless until you fail; backdrop ascends through altitude tiers (~every 10 floors) → §4.9 Atmospheric Ascent.
- **B4** endless + per-district height goal. **B5** forgiving 2-strike: bad drop shaves the overhang (roof/balcony falls), 2nd miss = topple/end. **B6** both crane-swing + wobble; swing speed rises only minimally. **B7** no beginner assist. **B8** camera up; pulls back to show the whole tower on a miss. **B9** minimal haptics.
- **C10 upgrades** magnet/auto-center · slow-mo · extra life. **C11** two currencies (coins + gems). **C12/C13** dailies/streaks + achievements/missions explained in chat; folded into §4.3/§4.8.
- **D14** 3 districts at launch. **D15** unlock by population; residents per floor: standard 2 / balcony 3 / premium 4. **D16** residents parachute/umbrella in on every placed floor (types/effects = Phase-4 TODO).
- **E17** solo first. **E19** guest or Google; Google = cloud-saved, guest = one-time "progress not saved" warning.
- **F20/F21** no ads at launch; wire the dormant base, turn on later.
- **G22** style ref = the imagestowerbloxx folder; 4 Midjourney directions in `MIDJOURNEY_DIRECTIONS.md`. **G23** dev will open Blender (guide in `BLENDER_GUIDE.md`) — or Claude models via BlenderMCP. **G24** dev generates the icon (prompt provided).
- **H25** upbeat city music; mandatory SFX (floor land, resident chatter, fall/topple, perfect chime, menu/buttons) → ART_BIBLE §4.1.
- **I26** target normal phones @60fps (skip weak devices). **I27** portrait locked; tablet support yes.
- **J** success = a beautiful game with superb physics, gameplay, sound — visual + game-feel first → GDD §5.1.
- *Open:* time budget/week (J28) not yet given — pace TBD.
