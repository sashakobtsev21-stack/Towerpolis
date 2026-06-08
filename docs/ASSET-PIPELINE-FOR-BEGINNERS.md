# Towerpolis — Гайд по ассетам для чайников

*Основан на реальном коде проекта (`GameAudio.cs`, `BlockSpawner.cs`, `GameVfx.cs`, `ResidentFlyIn.cs`, `BackgroundLayer.cs`). Все пути проверены по фактической структуре папок и `Resources.Load`-вызовам.*

---

## Сводная таблица: что куда класть

| Тип ассета | Папка в проекте | Формат | Размер / спецификация |
|---|---|---|---|
| SFX (звуки) | `unity/Towerpolis/Assets/Audio/Resources/Sfx/` | `.wav` или `.ogg` | Моно, 44.1 кГц, см. длины ниже |
| Музыка | `unity/Towerpolis/Assets/Audio/Resources/Music/` | `.ogg` | Стерео, бесшовный луп, ~-14 LUFS |
| 3D-блоки (FBX) | `unity/Towerpolis/Assets/Art/Resources/Blocks/` | `.fbx` | Низкополи, pivot снизу, строго те имена |
| Жители (резиденты) | `unity/Towerpolis/Assets/Art/Resources/Residents/` | `.fbx` / `.prefab` | Низкополи, T-поза |
| Фон (слои неба) | `unity/Towerpolis/Assets/VFX/Resources/Background/` | `.png` (с прозрачностью) | 1024×256 / 512×512, POT |
| VFX-префабы | `unity/Towerpolis/Assets/VFX/Resources/Vfx/` | `.prefab` | Один-шот, Looping = off |
| Шрифт (TMP) | `unity/Towerpolis/Assets/TextMesh Pro/Resources/Fonts & Materials/` | `.asset` (Font Asset) | Статический SDF, диапазон 0020–007E + 0400–04FF |

> **Правило Unity:** всё, что грузится кодом через `Resources.Load`, ОБЯЗАНО лежать в папке `Resources` (или её подпапке) и **строчными именами** (в APK регистр важен). Пути в таблице уже правильные.

---

## 1. Звуки (SFX)

Короткие эффекты на события. `GameAudio.cs` грузит их по точным именам из `Assets/Audio/Resources/Sfx/`. Бросил файл → нажал Play → работает.

**Формат:** `.wav` (несжатый) или `.ogg` · 44100 Гц · 16 бит · **моно** · пики не выше -1 dBFS.

| Файл | Когда | Длина | Характер |
|---|---|---|---|
| `land.wav` | приземление блока (часто!) | 0.1–0.3 с | мягкий деревянный стук — не должен раздражать |
| `perfect.wav` | Perfect | 0.2–0.5 с | чистый колокольчик/чайм — код повышает тон в серии, нужен музыкальный тон |
| `miss.wav` | промах | 0.3–0.6 с | свист/whoosh |
| `topple.wav` | башня рухнула | 0.5–1.5 с | грохот/обрушение |
| `start.wav` | старт рана | 0.1–0.3 с | лёгкий pop (необязательный) |

**Где взять (CC0):** kenney.nl (Audio → Interface/Impact Sounds), freesound.org (фильтр License = CC0), pixabay.com/sound-effects, mixkit.co.

**Подготовка (Audacity):** Mix Stereo→Mono · Resample 44100 · обрезать тишину · Normalize -1 dB · Export WAV 16-bit.

**Импорт в Unity (Inspector):** Load Type `Decompress On Load`, Compression `PCM` (или Vorbis), Force To Mono если стерео.

---

## 2. Музыка

Зацикленная фоновая, с кроссфейдом при смене района. Районы: `downtown`, `neon`, `winter` + общая `theme` (запасная).

**Формат:** `.ogg` · 44100 Гц · **стерео** · ~-14 LUFS · 30–90 с · **бесшовный луп** (начало=конец по фазе).

| Файл | Район | Характер |
|---|---|---|
| `theme.ogg` | запасной | спокойный казуальный/lo-fi |
| `downtown.ogg` | Центр | бодрый джаз/акустик-поп |
| `neon.ogg` | Неон | синтвейв/lo-fi, ночь |
| `winter.ogg` | Зима | лёгкий оркестр/кельтика |

Можно начать только с `theme.ogg` — заработает; остальные добавляй позже.

**Бесшовный луп (Audacity):** найди точку замыкания кратно тактам → проверь стык с Loop Play (J) → Export OGG Quality 8.

**Импорт в Unity:** Load Type `Streaming`, Compression `Vorbis`, Quality 70%. Loop игра ставит сама.

**Где взять:** pixabay.com/music (без атрибуции), mixkit.co, freemusicarchive.org (CC0). incompetech.com — CC-BY (нужно указать автора). AI: Suno.ai / Udio.com (проверь лицензию тарифа).

---

## 3. Изображения / текстуры (фоновые слои)

`BackgroundLayer.cs` грузит PNG по имени из `Assets/VFX/Resources/Background/`. Каждый слой параллакс-движется на своей высоте. Без файла — процедурный заменитель (кроме `city` и `balloon` — у них заменителя НЕТ, без файла не видны).

| Файл | Высота | Что |
|---|---|---|
| `city.png` | старт | тёмный силуэт линии города (нет заменителя!) |
| `cloud.png` | средне | облака |
| `balloon.png` | низ-средне | воздушный шар (нет заменителя!) |
| `plane.png` | высоко | самолёт/птица |
| `aurora.png` | очень высоко | полоса сияния |
| `star.png` | стратосфера | звезда |
| `moon.png` | космос | диск луны |

**Формат:** **PNG с альфа-каналом** · размер POT (степень двойки): широкие `city`/`cloud`/`aurora` → 1024×256 или 2048×512; объекты `balloon`/`plane`/`star`/`moon` → 256×256 или 512×512 · максимум 1024 по стороне · sRGB · прозрачный фон.

**Импорт в Unity:** Max Size 1024, Compression `ASTC 6x6`/Automatic, Alpha Source `Input Texture Alpha`, Alpha Is Transparency ✓.

**Как создать (Midjourney v7):**
```
stylized cartoon city skyline silhouette, dark blue-gray, horizontal, transparent background, wide panoramic, game backdrop layer --ar 4:1 --v 7 --style raw
```
Затем в **Photopea** (бесплатно): удалить фон → Export PNG (с прозрачностью) → Image Size до POT. Альтернативы: Adobe Firefly, Canva AI, freepik.com (Free + прозрачность).

**Текстуры блоков (необязательно):** блоки уже перекрашиваются палитрой в коде; если хочешь кирпич/штукатурку — tileable PNG 512²/1024², источники ambientcg.com / polyhaven.com (CC0).

---

## 4. 3D-модели (блоки) — УЖЕ ЕСТЬ в проекте ✅

Блоки-«этажи» уже лежат в `Assets/Art/Resources/Blocks/` (готовые FBX из Blender). Игра их использует. Делать с нуля НЕ нужно — раздел на случай замены/улучшения.

**Имена (строго):** `Floor_Standard.fbx`, `Floor_Balcony.fbx`, `Floor_Balcony_2.fbx`, `Floor_Premium.fbx`, `Base_Ground.fbx`, `Base_Ground_2.fbx`.

**Спецификации:** footprint 2×2 юнита, высота 1.5 · **pivot снизу по центру** (критично для стекинга) · ≤500–800 трисов/блок · материал-слоты по палитре кода (`TP_Green`/`TP_Yellow`/…/`TP_Glass`/`TP_Brick` — иначе слот не перекрасится; полный список в `BlockSpawner.cs`/`BlockModelPostprocessor.cs`).

**Экспорт из Blender:** FBX, Selected Objects, Apply Scalings `FBX All`, `-Z Forward`/`Y Up`, Apply Unit ✓, Apply Modifiers ✓, Smoothing `Normals Only`. Исходники → `art/blocks/fbx/`, копия → `Assets/Art/Resources/Blocks/`.

**Бесплатно (CC0):** kenney.nl (City Kit), quaternius.com (Modular Buildings), kay.lousberg.nl. Переименуй под нужные имена — код сделает остальное.

---

## 5. Жители (резиденты)

`ResidentFlyIn.cs` грузит из `Assets/Art/Resources/Residents/` по `Resident_Umbrella_1..3` и анимирует **полностью в коде** (полёт с зонтиком). Без файлов — процедурная фигурка из примитивов.

**Имена:** `Resident_Umbrella_1.fbx` (и `_2`, `_3` — необязательно; хватит одного).

**Спецификации:** ~0.4–0.5 юнита высотой (код масштабирует `×0.45`) · FBX/prefab · 100–300 трисов · **риг/клип не нужны** (анимация в коде) · дочерний меш зонтика назови `Umbrella` (с заглавной) → код его перекрасит для разнообразия.

**Где взять:** kenney.nl / quaternius.com (CC0). Или Midjourney-силуэт → Tripo AI / Rodin Gen-2 → ретопология в Blender.

---

## 6. Анимация

Для текущей версии **отдельные клипы НЕ нужны** — все движения (полёт жителя, сжатие блока) сделаны кодом (DOTween/Coroutine). Никакого Mecanim/`.anim`.

Если позже добавишь анимированную модель: экспортируй FBX с Bake Animation → в Unity на FBB вкладка Animation задай Range+имя клипа → создай Animator Controller → повесь `Animator` на prefab. Это фаза B.

---

## 7. Шрифт (кириллица)

Сейчас RU-текст рендерится через динамический fallback `LiberationSans SDF` — работает, но качество ниже и в логе предупреждения. `UiFont.cs` — заглушка под будущий статический шрифт.

**Как сделать правильно (15 мин):**
1. Скачай бесплатный TTF с кириллицей: **Roboto** (google.com/fonts), **Inter** (rsms.me/inter) или **Onest** (onest.me) — Regular + Bold.
2. Положи `.ttf` в `Assets/TextMesh Pro/Fonts/`.
3. `Window → TextMeshPro → Font Asset Creator`: Source = твой ttf · Atlas Resolution `2048×2048` · Character Set `Custom Range` · диапазон `0020-007E,0400-04FF` (латиница + кириллица) · Render Mode `SDF32` → **Generate** → **Save** в `Assets/TextMesh Pro/Resources/Fonts & Materials/` как `MyFont SDF.asset`.
4. `TMP Settings.asset` → Default Font Asset = твой `MyFont SDF`.
5. (Опц.) в `LiberationSans SDF` → Fallback Font Assets замени fallback на свой.

---

## Полная карта папок

```
unity/Towerpolis/Assets/
├── Audio/Resources/
│   ├── Sfx/{land,perfect,miss,topple,start}.wav     ← land+perfect ОБЯЗАТЕЛЬНЫ
│   └── Music/{theme,downtown,neon,winter}.ogg        ← theme ОБЯЗАТЕЛЕН
├── Art/Resources/
│   ├── Blocks/{Floor_*,Base_*}.fbx                   ← УЖЕ ЕСТЬ ✅
│   └── Residents/Resident_Umbrella_{1,2,3}.fbx       ← код рисует сам, можно улучшить
├── VFX/Resources/
│   ├── Background/{city,cloud,balloon,plane,aurora,star,moon}.png  ← city+balloon без заменителя
│   └── Vfx/{dust,confetti}.prefab                    ← есть заменитель
└── TextMesh Pro/Resources/Fonts & Materials/
    ├── LiberationSans SDF.asset                      ← УЖЕ ЕСТЬ
    └── MyCyrillicFont SDF.asset                      ← сделай сам (раздел 7)
```

---

## ⭐ Быстрый старт: минимум, чтобы игра звучала и выглядела прилично

По порядку — каждый шаг заметно улучшает:

1. **`land.wav` + `perfect.wav`** (15 мин) — самые частые звуки, без них игра «мёртвая». kenney.nl → Interface/Casual Audio. → `Sfx/`.
2. **`topple.wav`** (5 мин) — конец рана. freesound.org «rubble collapse» CC0. → `Sfx/`.
3. **`theme.ogg`** (20 мин) — фоновая музыка. pixabay.com/music «casual lo-fi loop». → `Music/`.
4. **`city.png`** (20 мин) — силуэт города (без файла слой пустой). Midjourney → Photopea → PNG 1024×256. → `Background/`.
5. **`balloon.png`** (10 мин) — без файла не виден. Midjourney → 512×512. → `Background/`.
6. **`miss.wav` + `start.wav`** (10 мин) — добивает звуковой набор.
7. **Кириллический шрифт** (15 мин) — раздел 7. Убирает предупреждения + чётче текст.
8. **`downtown/neon/winter.ogg`** (1 ч) — музыка по районам (кроссфейд уже в коде).

### Что уже есть vs что добавить
| Что | Статус |
|---|---|
| 3D-блоки | **УЖЕ ЕСТЬ** (`Art/Resources/Blocks/`) |
| Небо (skybox), VFX dust/confetti, жители | **РАБОТАЮТ** процедурно (код) |
| SFX, музыка | **ДОБАВИТЬ** — папки ждут файлы |
| Фон `city`/`balloon` | **ДОБАВИТЬ** — нет процедурных заменителей |
| Кириллический шрифт | работает через fallback, улучшение опционально |
