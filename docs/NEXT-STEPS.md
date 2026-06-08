# Towerpolis — что делать дальше

> Единый список следующих действий по приоритету. Детали — в связанных доках.
> Игра **уже запускается и играбельна** (процедурные плейсхолдеры), Core покрыт **321 тестом**.
> Чекбоксы отмечай по мере выполнения.

Связанные документы:
- 📦 **Ассеты (звук/картинки/3D/шрифт):** [`ASSET-PIPELINE-FOR-BEGINNERS.md`](ASSET-PIPELINE-FOR-BEGINNERS.md) — что/формат/размер/папка.
- 🔧 **Полный аудит-чеклист (P1/P2/P3):** [`PROJECT_STATUS.md`](PROJECT_STATUS.md) §5b.
- ✅ **On-device приёмка:** [`PHASE3_GATE_CHECKLIST.md`](PHASE3_GATE_CHECKLIST.md).
- 🗺️ **Роадмап фаз 6+:** [`BUILD_PLAN.md`](BUILD_PLAN.md).

---

## ⭐ Сейчас — быстрее всего меняет ощущение (контент, ~1.5 ч)

Самое заметное. Полные шаги — в `ASSET-PIPELINE-FOR-BEGINNERS.md` → «Быстрый старт».

- [ ] **`land.wav` + `perfect.wav`** → `Assets/Audio/Resources/Sfx/` (kenney.nl, 15 мин) — без них игра «мёртвая».
- [ ] **`topple.wav`, `miss.wav`, `start.wav`** → `Sfx/` (freesound.org, CC0).
- [ ] **`theme.ogg`** (фоновая музыка) → `Assets/Audio/Resources/Music/` (pixabay.com/music).
- [ ] **`city.png`** (силуэт города) → `Assets/VFX/Resources/Background/` — без файла слой пустой.
- [ ] **`balloon.png`** → `Background/` — тоже без процедурного заменителя.
- [ ] **Кириллический шрифт** (TMP Font Asset, диапазон `0020-007E,0400-04FF`) — убирает предупреждение в логе + чётче текст.

---

## ⚙️ Код в Unity (пишешь/проверяешь в Editor; рецепты в PROJECT_STATUS §5b)

- [ ] **`SummitHeight` 15 → 200** (`Game/UI/HUDController.cs:70`) — это тестовое значение, вернуть перед launch.
- [ ] **Перф:** пул резидентов в `ResidentFlyIn` (сейчас `Instantiate`+`new Material()` на каждого), кэш `HUDController.BuildRunResult()` (зовётся каждый кадр).
- [ ] **Прокинуть `CoreConfig` → `GameTuning`** — сейчас `BuildCoreConfig()` отдаёт дефолты; без этого нельзя тюнить баланс без перекомпиляции (блокирует пункт ниже).
- [ ] Полировка: разбить `HUDController` (677 строк) на partial; магические константы → `GameTuning`; `BackgroundLayer.Update` — dirty-check на `material.color`.

---

## 🎮 Баланс — после playtest на реальном Android (твоя калибровка «фила»)

- [ ] **Пороги Grading:** Sloppy-зона схлопнута (`Good==Sloppy==0.80`), Perfect-окно (0.15) узко на мобиле. Попробовать Good 0.55 / Sloppy 0.80 / Perfect 0.20–0.25 → проиграть 10 сессий на телефоне.
- [ ] **Prestige-экономика:** сейчас вайп апгрейдов + ½ монет за +25% жителей — наказывает. Сохранять косметику/скины; coin-retain растёт с числом prestige. Сделать табличную модель.
- [ ] Мелкие награды: комбо-бонус (20), день-7 streak (200), login день-1 (10) — поднять; gem'ам дать ранний сток.
- [ ] Weekly-mission `m_streak_days` может быть недостижимой — сделать относительной или исключать.

---

## 📱 Релиз (фазы 6 → 9)

- [ ] **On-device gate:** прогнать `PHASE3_GATE_CHECKLIST.md` на Android — **это разблокирует всё дальше**.
- [ ] **Phase 6 — арт+аудио:** довести контент (см. asset-гайд) до «премиум»-уровня.
- [ ] **Daily-seed virality:** показать «вчерашний счёт» + share-card вместе с daily (а не в Phase 5).
- [ ] **Phase 7 — монетизация** (rewarded + IAP за kill-switch, off на старте).
- [ ] **Phase 8 — QA:** device-matrix, 60fps mid-Android, <200 draw calls, Android Vitals чистые.
- [ ] **Phase 9 — soft-launch:** подписанный AAB, Play Console, RU+EN листинги, 1–2 tier-2 рынка. Gate global на D1≥30% / D7≥12%.
- [ ] CI Unity-сборка активируется, когда добавишь секреты `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` в репозиторий (тогда заработает джоб `unity-android` в `ci.yml`).

---

## 🤝 Где может помочь ассистент
- Скачал CC0-пак ассетов → напишу скрипт, который разложит файлы по правильным папкам с нужными именами.
- Unity код-фиксы патчами (ты компилируешь+проверяешь в Editor, потом коммитим).
- Настройка CI Unity-сборки / конфиг AAB / чек-лист Play Console.
- Любая Core-логика/тесты (это проверяется `dotnet test` без Unity).

---
*Обновлено в ходе аудита проекта. Core: 321 NUnit-тест зелёный (`dotnet test core/Towerpolis.Core.Tests`).*
