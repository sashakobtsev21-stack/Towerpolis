using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Towerpolis.Core.Meta;
using Towerpolis.Game.Gameplay;
using Towerpolis.Game.Meta;

namespace Towerpolis.Game.UI
{
    /// <summary>
    /// The meta HUD, built in code (no scene wiring): a coins + city-population readout (top-left) and a
    /// CITY button that opens a city view — the active district's deposited towers as a growing skyline of
    /// bars (height ∝ floors), with population vs the fill goal. Reads <see cref="MetaService"/> and
    /// refreshes on its <see cref="MetaService.RunBanked"/> event. Self-bootstraps off the controller.
    /// This is the MVP visualisation; the 3D city view + per-district art come later.
    /// </summary>
    public sealed class MetaHud : MonoBehaviour
    {
        static readonly Color Navy = new Color(0.12f, 0.23f, 0.37f);
        static readonly Color OffWhite = new Color(0.97f, 0.97f, 0.95f);
        static readonly Color Gold = new Color(1.00f, 0.82f, 0.30f);
        static readonly Color Dim = new Color(0.06f, 0.10f, 0.16f, 0.92f);
        static readonly Color EmptyPlot = new Color(1f, 1f, 1f, 0.10f);

        MetaService _meta;
        TowerGameController _controller;

        TMP_Text _popLabel;

        static readonly string[] DistIds = { "downtown", "neon", "winter" };
        static readonly string[] DistNames = { LocKeys.DistDowntownShort, LocKeys.DistNeonShort, LocKeys.DistWinterShort };
        static readonly Color Locked = new Color(0.22f, 0.24f, 0.30f, 0.95f);
        Image[] _distImg;
        TMP_Text[] _distLbl;

        GameObject _cityPanel;
        TMP_Text _cityTitle;
        TMP_Text _cityPop;
        RectTransform _grid;
        GridLayoutGroup _gridLayout;

        // Upgrades panel
        static readonly UpgradeKind[] UpgKinds = { UpgradeKind.Magnet, UpgradeKind.CityBonus };
        static readonly string[] UpgNames = { LocKeys.UpgMagnetName, LocKeys.UpgCityBonusName };
        static readonly string[] UpgDescs = { LocKeys.UpgMagnetDesc, LocKeys.UpgCityBonusDesc };
        static readonly Color Disabled = new Color(0.60f, 0.62f, 0.67f);
        GameObject _upgPanel;
        TMP_Text _upgCoins;
        TMP_Text[] _upgInfo;
        TMP_Text[] _upgDesc;
        TMP_Text[] _upgBuyLbl;
        Image[] _upgBuyImg;
        TMP_Text _loginLbl;
        Image _loginImg;

        // Missions panel
        static readonly Color LockedText = new Color(0.62f, 0.64f, 0.68f);
        GameObject _missionPanel;
        TMP_Text[] _missionLines;
        TMP_Text[] _achLines;

        GameObject _menuPanel;
        GameObject _settingsPanel;
        Image _langRuImg, _langEnImg; // highlight the active language
        Image _soundImg; TMP_Text _soundLbl; bool _soundOn = true;
        Image _resetImg; TMP_Text _resetLbl; bool _resetArmed; // "Заново" needs a 2nd tap to confirm
        static readonly Color Danger = new Color(0.85f, 0.30f, 0.28f);

        // Run-end toasts (mission/achievement/district completions)
        static readonly Color Cyan = new Color(0.50f, 0.85f, 1f);
        Transform _toastRoot;
        readonly Queue<Toast> _toasts = new Queue<Toast>();
        bool _toasting;
        struct Toast { public string Text; public Color Color; }

        // District-complete celebration (meta-spec §2.4) — a distinct full-screen beat, not just a toast.
        GameObject _completePanel;
        RectTransform _completeContent;
        TMP_Text _completeName, _completeCoins, _completeGems, _completeNext;

        void Start()
        {
            UiFont.EnsureCyrillic(); // render Cyrillic with the default TMP font
            Loc.Init();              // restore the saved/device language (ADR-0008)
            _soundOn = PlayerPrefs.GetInt("towerpolis.sound", 1) == 1; // restore the sound setting
            AudioListener.volume = _soundOn ? 1f : 0f;
            _meta = MetaService.Instance != null ? MetaService.Instance : FindFirstObjectByType<MetaService>();
            _controller = FindFirstObjectByType<TowerGameController>();
            EnsureEventSystem(); // UGUI buttons need one to receive clicks
            BuildUI();
            if (_meta != null)
            {
                _meta.RunBanked += OnBanked;
                _meta.ProgressionChanged += OnProgressionChanged;
                _meta.SystemsResolved += OnSystemsResolved;
            }
            if (_controller != null)
            {
                _controller.FloorAdded += OnFloorLive;  // tick coins/population up as you build
                _controller.RunStarted += OnRunStartLive;
            }
            Loc.LanguageChanged += OnLanguageChanged;   // re-resolve dynamic labels on a language switch
            RefreshTopBar();
        }

        // Static captions re-resolve themselves via LocalizedLabel; repaint the dynamic labels + open panel.
        void OnLanguageChanged()
        {
            RefreshTopBar();
            if (_cityPanel != null && _cityPanel.activeSelf) PopulateCity();
            if (_upgPanel != null && _upgPanel.activeSelf) PopulateUpgrades();            if (_missionPanel != null && _missionPanel.activeSelf) PopulateMissions();
            if (_settingsPanel != null && _settingsPanel.activeSelf) RefreshSettings();
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>(); // project uses the Input System package
        }

        void OnDestroy()
        {
            if (_meta != null)
            {
                _meta.RunBanked -= OnBanked;
                _meta.ProgressionChanged -= OnProgressionChanged;
                _meta.SystemsResolved -= OnSystemsResolved;
            }
            if (_controller != null)
            {
                _controller.FloorAdded -= OnFloorLive;
                _controller.RunStarted -= OnRunStartLive;
            }
            Loc.LanguageChanged -= OnLanguageChanged;
        }

        void OnBanked(RunEndOutcome outcome)
        {
            RefreshTopBar();
            if (_cityPanel != null && _cityPanel.activeSelf) PopulateCity();
            if (_upgPanel != null && _upgPanel.activeSelf) PopulateUpgrades();
            if (outcome.DistrictCompletedNow) ShowDistrictComplete(outcome); // a distinct celebration, not a toast
        }

        void OnProgressionChanged()
        {
            if (_upgPanel != null && _upgPanel.activeSelf) PopulateUpgrades();        }

        void OnSystemsResolved(RunSystemsOutcome sys)
        {
            if (_missionPanel != null && _missionPanel.activeSelf) PopulateMissions();

            if (sys.CompletedMissions != null)
                foreach (string id in sys.CompletedMissions)
                {
                    MissionDef m = MissionCatalog.Get(id);
                    EnqueueToast(Loc.T(LocKeys.ToastMission, Loc.T(m.Name), m.Info.RewardCoins), Gold);
                }
            if (sys.UnlockedAchievements != null)
                foreach (string id in sys.UnlockedAchievements)
                {
                    AchievementDef a = FindAchievement(id);
                    EnqueueToast(Loc.T(LocKeys.ToastAchievement, Loc.T(a.Name), a.Info.RewardCoins), Cyan);
                }
        }

        static AchievementDef FindAchievement(string id)
        {
            foreach (AchievementDef a in AchievementCatalog.All)
                if (a.Info.AchievementId == id) return a;
            return AchievementCatalog.All[0];
        }

        // ---------- toasts ----------

        void EnqueueToast(string text, Color color)
        {
            if (_toastRoot == null) return;
            _toasts.Enqueue(new Toast { Text = text, Color = color });
            if (!_toasting) StartCoroutine(RunToasts());
        }

        IEnumerator RunToasts()
        {
            _toasting = true;
            while (_toasts.Count > 0)
                yield return AnimateToast(_toasts.Dequeue());
            _toasting = false;
        }

        IEnumerator AnimateToast(Toast t)
        {
            TMP_Text lbl = NewText("Toast", _toastRoot, 44, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform rt = lbl.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(940f, 130f);
            lbl.text = t.Text;
            const float baseY = 380f;

            float e = 0f;
            while (e < 0.18f)
            {
                e += Time.deltaTime;
                float k = e / 0.18f;
                SetToast(lbl, t.Color, k, baseY);
                rt.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, k); // pop in
                yield return null;
            }
            rt.localScale = Vector3.one;
            e = 0f;
            while (e < 1.1f) { e += Time.deltaTime; SetToast(lbl, t.Color, 1f, baseY); yield return null; }
            e = 0f;
            while (e < 0.45f) { e += Time.deltaTime; float k = e / 0.45f; SetToast(lbl, t.Color, 1f - k, baseY + k * 70f); yield return null; }
            Destroy(lbl.gameObject);
        }

        static void SetToast(TMP_Text lbl, Color c, float alpha, float y)
        {
            c.a = Mathf.Clamp01(alpha);
            lbl.color = c;
            lbl.rectTransform.anchoredPosition = new Vector2(0f, y);
        }

        // ---------- panel show/hide animation ----------

        void ShowPanel(GameObject panel)
        {
            if (panel == null) return;
            panel.SetActive(true);
            InputGate.Suppress = true; // a modal is up → don't drop a block
            StartCoroutine(FadePanel(panel, 1f, 0.16f, false));
        }

        void HidePanel(GameObject panel)
        {
            InputGate.Suppress = false;
            if (panel == null) return;
            StartCoroutine(FadePanel(panel, 0f, 0.12f, true));
        }

        IEnumerator FadePanel(GameObject panel, float to, float dur, bool deactivateAtEnd)
        {
            if (panel == null) yield break;
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            float from = deactivateAtEnd ? cg.alpha : 0f;
            float e = 0f;
            cg.alpha = from;
            while (e < dur)
            {
                e += Time.deltaTime;
                if (panel == null || cg == null) yield break; // panel destroyed mid-fade → bail, don't throw
                cg.alpha = Mathf.Lerp(from, to, e / dur);
                yield return null;
            }
            if (cg != null) cg.alpha = to;
            if (deactivateAtEnd && panel != null) panel.SetActive(false);
        }

        void OnFloorLive(int floors) => RefreshTopBar();
        void OnRunStartLive() => RefreshTopBar();

        void RefreshTopBar()
        {
            if (_meta == null) return;

            // THIS building's residents (current run) — starts at 0 each run, grows as you stack. The
            // cumulative city population (the meta-score) is shown in the city view.
            int residents = _controller != null ? _controller.BuildRunResult().TotalResidents : 0;
            if (_popLabel != null) _popLabel.text = Loc.T(LocKeys.MetaResidents, residents);
        }

        // ---------- city view ----------

        void OpenCity()
        {
            if (_cityPanel == null) return;
            PopulateCity();
            ShowPanel(_cityPanel);
        }

        void CloseCity() => HidePanel(_cityPanel);

        void DistrictButtons(Transform parent)
        {
            _distImg = new Image[DistIds.Length];
            _distLbl = new TMP_Text[DistIds.Length];
            float[] xs = { -310f, 0f, 310f };
            for (int i = 0; i < DistIds.Length; i++)
            {
                int idx = i; // capture for the listener
                var btnGo = new GameObject("Dist_" + DistIds[i], typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(parent, false);
                var rt = (RectTransform)btnGo.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(xs[i], -300f);
                rt.sizeDelta = new Vector2(290f, 80f);
                _distImg[i] = btnGo.GetComponent<Image>();
                btnGo.GetComponent<Button>().onClick.AddListener(() => SwitchDistrict(DistIds[idx]));
                _distLbl[i] = NewText("L", rt, 28, FontStyles.Bold, TextAlignmentOptions.Center);
                Stretch(_distLbl[i].rectTransform);
            }
        }

        void RefreshDistrictButtons()
        {
            if (_meta == null || _distImg == null) return;
            string active = _meta.ActiveDistrictId;
            for (int i = 0; i < DistIds.Length; i++)
            {
                bool unlocked = _meta.IsDistrictUnlocked(DistIds[i]);
                bool isActive = DistIds[i] == active;
                if (_distImg[i] != null) _distImg[i].color = !unlocked ? Locked : isActive ? Gold : Navy;
                if (_distLbl[i] != null)
                {
                    _distLbl[i].text = Loc.T(DistNames[i]);
                    _distLbl[i].color = unlocked ? (isActive ? Navy : OffWhite) : new Color(0.55f, 0.57f, 0.62f);
                }
            }
        }

        void SwitchDistrict(string id)
        {
            if (_meta == null || !_meta.SetActiveDistrict(id)) return; // locked → ignored
            CloseCity();
            if (_controller != null) _controller.NewRun(); // restart in the new district → applies its look
        }

        void PopulateCity()
        {
            if (_meta == null) return;
            string id = _meta.ActiveDistrictId;
            DistrictView view = DistrictCatalog.GetView(id);
            DistrictInfo info = DistrictCatalog.Get(id);

            CityGrid grid = null;
            _meta.City?.Grids.TryGetValue(id, out grid);
            int population = grid != null ? grid.Population : 0;

            if (_cityTitle != null) _cityTitle.text = Loc.T(view.DisplayName);
            if (_cityPop != null) _cityPop.text = Loc.T(LocKeys.MetaPopulation, population, info.FillGoal);
            RefreshDistrictButtons();

            if (_gridLayout != null) _gridLayout.constraintCount = Mathf.Max(1, view.GridWidth);

            for (int i = _grid.childCount - 1; i >= 0; i--) Destroy(_grid.GetChild(i).gameObject);

            int capacity = info.GridCapacity;
            for (int i = 0; i < capacity; i++)
            {
                bool occupied = grid != null && i < grid.OccupiedCount && grid.Plots[i].Occupied;
                int floors = occupied ? grid.Plots[i].FloorCount : 0;
                AddTowerCell(occupied, floors, view.Accent);
            }
        }

        void AddTowerCell(bool occupied, int floors, Color accent)
        {
            var cell = new GameObject("Plot", typeof(RectTransform));
            cell.transform.SetParent(_grid, false);

            Image bar = NewImage("Bar", cell.transform, occupied ? accent : EmptyPlot);
            RectTransform brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0.22f, 0f);
            brt.anchorMax = new Vector2(0.78f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            float cellH = _gridLayout != null ? _gridLayout.cellSize.y : 150f;
            float h = occupied ? 10f + Mathf.Clamp01(floors / 25f) * (cellH - 14f) : 8f;
            brt.sizeDelta = new Vector2(0f, h);
            brt.anchoredPosition = Vector2.zero;
        }

        // ---------- upgrades view ----------

        void OpenUpgrades()
        {
            if (_upgPanel == null) return;
            PopulateUpgrades();
            ShowPanel(_upgPanel);
        }

        void CloseUpgrades() => HidePanel(_upgPanel);

        void BuyUpgrade(UpgradeKind kind)
        {
            bool ok = _meta != null && _meta.BuyUpgrade(kind); // ProgressionChanged → refresh
            PopulateUpgrades();
            int i = System.Array.IndexOf(UpgKinds, kind);
            if (ok && i >= 0 && _upgBuyImg[i] != null) PopButton(_upgBuyImg[i].transform);
        }

        void ClaimLoginGift()
        {
            bool ok = _meta != null && _meta.CanClaimLoginToday();
            if (_meta != null) _meta.ClaimLoginToday();
            PopulateUpgrades();
            if (ok && _loginImg != null) PopButton(_loginImg.transform);
        }

        void PopulateUpgrades()
        {
            if (_meta == null) return;
            int coins = _meta.Coins;
            if (_upgCoins != null) _upgCoins.text = Loc.T(LocKeys.MetaCoins, coins);

            for (int i = 0; i < UpgKinds.Length; i++)
            {
                UpgradeKind k = UpgKinds[i];
                int lvl = _meta.UpgradeLevel(k), max = _meta.UpgradeMaxLevel(k);
                bool maxed = _meta.IsUpgradeMaxed(k);
                int cost = _meta.NextUpgradeCost(k);
                bool afford = coins >= cost;
                if (_upgInfo[i] != null) _upgInfo[i].text = Loc.T(LocKeys.MetaUpgRow, Loc.T(UpgNames[i]), lvl, max);
                SetBuy(_upgBuyImg[i], _upgBuyLbl[i], maxed ? Loc.T(LocKeys.MetaMax) : Loc.T(LocKeys.MetaBuy, cost), maxed, afford);
            }

            bool canClaim = _meta.CanClaimLoginToday();
            if (_loginLbl != null)
            {
                _loginLbl.text = Loc.T(canClaim ? LocKeys.MetaLoginClaim : LocKeys.MetaLoginTaken);
                _loginLbl.color = canClaim ? Navy : OffWhite;
            }
            if (_loginImg != null) _loginImg.color = canClaim ? Gold : Navy;
        }

        // Colour a buy button by state: maxed/owned = navy, affordable = gold, too dear = greyed.
        static void SetBuy(Image img, TMP_Text label, string text, bool maxed, bool afford)
        {
            if (label != null)
            {
                label.text = text;
                label.color = maxed ? OffWhite : (afford ? Navy : Disabled);
            }
            if (img != null) img.color = maxed ? Navy : (afford ? Gold : Locked);
        }

        // ---------- goals view (weekly missions + achievements) ----------

        void OpenMissions()
        {
            if (_missionPanel == null) return;
            if (_meta != null) _meta.EnsureWeek(); // ensure this week's set is drawn
            PopulateMissions();
            ShowPanel(_missionPanel);
        }

        void CloseMissions() => HidePanel(_missionPanel);

        void PopulateMissions()
        {
            if (_meta == null) return;
            var active = _meta.ActiveMissionIds;
            for (int i = 0; i < _missionLines.Length; i++)
            {
                if (_missionLines[i] == null) continue;
                if (i < active.Count)
                {
                    MissionDef def = MissionCatalog.Get(active[i]);
                    int prog = Mathf.Min(_meta.MissionProgressFor(active[i]), def.Info.Target);
                    bool done = _meta.IsMissionComplete(active[i]);
                    _missionLines[i].text = Loc.T(LocKeys.MetaMissionLine, Loc.T(def.Description), prog, def.Info.Target, def.Info.RewardCoins)
                                            + (done ? "    " + Loc.T(LocKeys.MetaDone) : "");
                    _missionLines[i].color = done ? Gold : OffWhite;
                }
                else _missionLines[i].text = "";
            }

            AchievementDef[] all = AchievementCatalog.All;
            for (int i = 0; i < _achLines.Length; i++)
            {
                if (_achLines[i] == null) continue;
                bool got = _meta.IsAchievementUnlocked(all[i].Info.AchievementId);
                _achLines[i].text = Loc.T(LocKeys.MetaAchLine, Loc.T(all[i].Name), Loc.T(all[i].Description))
                                    + (got ? "    " + Loc.T(LocKeys.MetaDone) : "    +" + all[i].Info.RewardCoins);
                _achLines[i].color = got ? Gold : LockedText;
            }
        }

        // ---------- UI construction ----------

        void BuildUI()
        {
            var canvasGo = new GameObject("MetaHUD_Canvas");
            // Kept at scene root, NOT parented to this controller GameObject: TowerController lives on the
            // same GameObject and (a) rotates its transform for the tower sway and (b) ClearTower() destroys
            // ALL of its children every NewRun — which would wipe this HUD on a district switch / retry. A
            // ScreenSpaceOverlay canvas renders regardless of parent, so root is correct and safe.
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11; // above the gameplay HUD (10)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _toastRoot = canvasGo.transform;

            // Top-bar chrome lives under a safe-area root so notches / rounded corners never clip the buttons.
            // (Full-screen panels stay on the canvas — their content is centred, away from the edges.)
            var safeGo = new GameObject("SafeRoot", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            var safe = (RectTransform)safeGo.transform;
            Stretch(safe);
            safeGo.AddComponent<SafeAreaRoot>();

            // Only POPULATION is shown in the top bar now (coins/streak are tracked in Core for later,
            // not displayed — owner: "нужно только население и этажей"; height is the HUD's big number).
            _popLabel = NewText("CityPop", safe, 40, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            _popLabel.color = Gold;
            Place(_popLabel.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -36f), new Vector2(420f, 56f));

            MenuButton(safe); // ☰ top-right corner → opens the menu with every section
            BuildCityPanel(canvasGo.transform);
            BuildUpgradePanel(canvasGo.transform);
            BuildMissionPanel(canvasGo.transform);
            BuildSettingsPanel(canvasGo.transform);
            BuildMenuPanel(canvasGo.transform);
            BuildCompletePanel(canvasGo.transform);
        }

        // Hamburger (☰) in the top-right corner — the single entry point to every meta section.
        void MenuButton(Transform parent)
        {
            Button btn = MakeButton(parent, "MenuButton", new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(96f, 96f), Navy, out _);
            btn.onClick.AddListener(OpenMenu);
            var icon = new GameObject("Icon", typeof(RectTransform));
            icon.transform.SetParent(btn.transform, false);
            Stretch((RectTransform)icon.transform);
            MenuBar(icon.transform, 18f);
            MenuBar(icon.transform, 0f);
            MenuBar(icon.transform, -18f);
        }

        void MenuBar(Transform parent, float y)
        {
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(50f, 8f);
            var img = bar.GetComponent<Image>();
            img.color = OffWhite;
            img.raycastTarget = false; // taps fall through to the button
        }

        void BuildMenuPanel(Transform parent)
        {
            _menuPanel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
            _menuPanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_menuPanel.transform;
            Stretch(prt);
            _menuPanel.GetComponent<Image>().color = Dim;
            PanelCard(prt);

            var title = NewText("Title", prt, 64, FontStyles.Bold, TextAlignmentOptions.Top);
            title.color = OffWhite;
            title.gameObject.AddComponent<LocalizedLabel>().Bind(title, LocKeys.MetaMenu);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 90f));

            // One row per section; each closes the menu, then opens its panel.
            MenuItem(prt, LocKeys.MetaCity,      210f, OpenCity);
            MenuItem(prt, LocKeys.MetaBonuses,    70f, OpenUpgrades);
            MenuItem(prt, LocKeys.MetaGoals,     -70f, OpenMissions);
            MenuItem(prt, LocKeys.MetaSettings, -210f, OpenSettings);

            Button close = MakeButton(prt, "MenuClose", new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(420f, 100f), Gold, out _);
            close.onClick.AddListener(CloseMenu);
            var clbl = NewText("Label", close.transform, 40, FontStyles.Bold, TextAlignmentOptions.Center);
            clbl.color = Navy;
            clbl.gameObject.AddComponent<LocalizedLabel>().Bind(clbl, LocKeys.MetaClose);
            Stretch(clbl.rectTransform);

            _menuPanel.SetActive(false);
        }

        void MenuItem(Transform parent, string labelKey, float y, System.Action open)
        {
            Button b = MakeButton(parent, "Menu_" + labelKey, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(560f, 92f), Navy, out _);
            b.onClick.AddListener(() => { CloseMenu(); open(); });
            var lbl = NewText("Label", b.transform, 36, FontStyles.Bold, TextAlignmentOptions.Center);
            lbl.color = OffWhite;
            lbl.gameObject.AddComponent<LocalizedLabel>().Bind(lbl, labelKey);
            Stretch(lbl.rectTransform);
        }

        void OpenMenu() { if (_menuPanel != null) ShowPanel(_menuPanel); }
        void CloseMenu() => HidePanel(_menuPanel);

        // ---------- district-complete celebration (meta-spec §2.4) ----------

        void BuildCompletePanel(Transform parent)
        {
            _completePanel = new GameObject("CompletePanel", typeof(RectTransform), typeof(Image));
            _completePanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_completePanel.transform;
            Stretch(prt);
            _completePanel.GetComponent<Image>().color = Dim; // dim full-screen backdrop, blocks taps
            PanelCard(prt);

            // All the celebratory text lives under a centred content node so it can pop as one unit (the Dim
            // backdrop + card stay put). The Continue button stays on the panel so the pop never moves it.
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(prt, false);
            _completeContent = (RectTransform)contentGo.transform;
            _completeContent.anchorMin = _completeContent.anchorMax = _completeContent.pivot = new Vector2(0.5f, 0.5f);
            _completeContent.anchoredPosition = Vector2.zero;
            _completeContent.sizeDelta = new Vector2(1000f, 1500f);

            var title = NewText("Title", _completeContent, 72, FontStyles.Bold, TextAlignmentOptions.Center);
            title.color = Gold;
            title.gameObject.AddComponent<LocalizedLabel>().Bind(title, LocKeys.CompleteTitle);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 380f), new Vector2(960f, 120f));

            _completeName = NewText("Name", _completeContent, 48, FontStyles.Bold, TextAlignmentOptions.Center);
            _completeName.color = OffWhite;
            Place(_completeName.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(960f, 70f));

            _completeCoins = NewText("Coins", _completeContent, 46, FontStyles.Bold, TextAlignmentOptions.Center);
            _completeCoins.color = Gold;
            Place(_completeCoins.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(960f, 60f));

            _completeGems = NewText("Gems", _completeContent, 42, FontStyles.Bold, TextAlignmentOptions.Center);
            _completeGems.color = Cyan;
            Place(_completeGems.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(960f, 56f));

            _completeNext = NewText("Next", _completeContent, 40, FontStyles.Normal, TextAlignmentOptions.Center);
            _completeNext.color = OffWhite;
            Place(_completeNext.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(980f, 130f));

            Button cont = MakeButton(prt, "CompleteContinue", new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(460f, 110f), Gold, out _);
            cont.onClick.AddListener(CloseComplete);
            var clbl = NewText("Label", cont.transform, 42, FontStyles.Bold, TextAlignmentOptions.Center);
            clbl.color = Navy;
            clbl.gameObject.AddComponent<LocalizedLabel>().Bind(clbl, LocKeys.CompleteContinue);
            Stretch(clbl.rectTransform);

            _completePanel.SetActive(false);
        }

        // The completed district is the active one at run-end; show its reward + the next district unlocked.
        void ShowDistrictComplete(RunEndOutcome outcome)
        {
            if (_completePanel == null || _meta == null) return;
            string id = _meta.ActiveDistrictId;

            if (_completeName != null) _completeName.text = Loc.T(DistrictCatalog.GetView(id).DisplayName);
            if (_completeCoins != null) _completeCoins.text = Loc.T(LocKeys.CompleteCoins, outcome.DistrictRewardCoins);

            bool hasGems = outcome.GemsEarned > 0;
            if (_completeGems != null)
            {
                _completeGems.gameObject.SetActive(hasGems);
                if (hasGems) _completeGems.text = Loc.T(LocKeys.CompleteGems, outcome.GemsEarned);
            }

            string nextId = DistrictCatalog.NextId(id);
            if (_completeNext != null)
                _completeNext.text = string.IsNullOrEmpty(nextId)
                    ? Loc.T(LocKeys.CompleteNextNone)
                    : Loc.T(LocKeys.CompleteNext, Loc.T(DistrictCatalog.GetView(nextId).DisplayName));

            ShowPanel(_completePanel);
            StartCoroutine(CelebratePop());
        }

        void CloseComplete() => HidePanel(_completePanel);

        // A spring pop on the celebration content (overshoot → settle) so the beat reads as a milestone.
        IEnumerator CelebratePop()
        {
            if (_completeContent == null) yield break;
            float e = 0f;
            const float dur = 0.42f;
            while (e < dur)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(e / dur);
                float ease = 1f - (1f - k) * (1f - k);        // ease-out toward 1
                float overshoot = Mathf.Sin(k * Mathf.PI) * 0.08f; // brief bulge past 1, back to 0
                float s = Mathf.Lerp(0.7f, 1f, ease) + overshoot;
                if (_completeContent == null) yield break;
                _completeContent.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (_completeContent != null) _completeContent.localScale = Vector3.one;
        }

        void BuildCityPanel(Transform parent)
        {
            _cityPanel = new GameObject("CityPanel", typeof(RectTransform), typeof(Image));
            _cityPanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_cityPanel.transform;
            Stretch(prt);
            _cityPanel.GetComponent<Image>().color = Dim; // dim full-screen backdrop, blocks taps
            PanelCard(prt);

            _cityTitle = NewText("Title", prt, 64, FontStyles.Bold, TextAlignmentOptions.Top);
            _cityTitle.color = OffWhite;
            Place(_cityTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 90f));

            _cityPop = NewText("Pop", prt, 40, FontStyles.Normal, TextAlignmentOptions.Top);
            _cityPop.color = Gold;
            Place(_cityPop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(900f, 60f));

            DistrictButtons(prt);

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(prt, false);
            _grid = (RectTransform)gridGo.transform;
            _grid.anchorMin = new Vector2(0.5f, 0.5f);
            _grid.anchorMax = new Vector2(0.5f, 0.5f);
            _grid.pivot = new Vector2(0.5f, 0.5f);
            _grid.anchoredPosition = new Vector2(0f, 40f);
            _grid.sizeDelta = new Vector2(960f, 760f);
            _gridLayout = gridGo.AddComponent<GridLayoutGroup>();
            _gridLayout.cellSize = new Vector2(150f, 150f);
            _gridLayout.spacing = new Vector2(14f, 14f);
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = 5;
            _gridLayout.childAlignment = TextAnchor.MiddleCenter;

            CloseButton(prt);
            _cityPanel.SetActive(false);
        }

        void CloseButton(Transform parent)
        {
            var btnGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 140f);
            rt.sizeDelta = new Vector2(420f, 100f);
            btnGo.GetComponent<Image>().color = Gold;
            btnGo.GetComponent<Button>().onClick.AddListener(CloseCity);

            var label = NewText("Label", rt, 40, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = Navy;
            label.gameObject.AddComponent<LocalizedLabel>().Bind(label, LocKeys.MetaClose);
            Stretch(label.rectTransform);
        }

        void BuildUpgradePanel(Transform parent)
        {
            _upgInfo = new TMP_Text[UpgKinds.Length];
            _upgDesc = new TMP_Text[UpgKinds.Length];
            _upgBuyLbl = new TMP_Text[UpgKinds.Length];
            _upgBuyImg = new Image[UpgKinds.Length];

            _upgPanel = new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image));
            _upgPanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_upgPanel.transform;
            Stretch(prt);
            _upgPanel.GetComponent<Image>().color = Dim;
            PanelCard(prt);

            var title = NewText("Title", prt, 64, FontStyles.Bold, TextAlignmentOptions.Top);
            title.color = OffWhite;
            title.gameObject.AddComponent<LocalizedLabel>().Bind(title, LocKeys.MetaUpgradesTitle);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 90f));

            _upgCoins = NewText("Coins", prt, 40, FontStyles.Bold, TextAlignmentOptions.Top);
            _upgCoins.color = Gold;
            Place(_upgCoins.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(900f, 60f));

            var hint = NewText("CoinHint", prt, 24, FontStyles.Italic, TextAlignmentOptions.Top);
            hint.color = Disabled;
            hint.gameObject.AddComponent<LocalizedLabel>().Bind(hint, LocKeys.MetaUpgHint);
            Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -262f), new Vector2(980f, 36f));

            float[] ys = { 250f, 150f };
            for (int i = 0; i < UpgKinds.Length; i++) UpgradeRow(prt, i, ys[i]);

            Button loginBtn = MakeButton(prt, "LoginClaim", new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(520f, 84f), Gold, out _loginImg);
            loginBtn.onClick.AddListener(ClaimLoginGift);
            _loginLbl = NewText("Lbl", loginBtn.transform, 32, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(_loginLbl.rectTransform);

            Button close = MakeButton(prt, "UpgClose", new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(420f, 100f), Gold, out _);
            close.onClick.AddListener(CloseUpgrades);
            var clbl = NewText("Label", close.transform, 40, FontStyles.Bold, TextAlignmentOptions.Center);
            clbl.color = Navy;
            clbl.gameObject.AddComponent<LocalizedLabel>().Bind(clbl, LocKeys.MetaClose);
            Stretch(clbl.rectTransform);

            _upgPanel.SetActive(false);
        }

        void UpgradeRow(Transform parent, int i, float y)
        {
            _upgInfo[i] = NewText("UpgInfo" + i, parent, 34, FontStyles.Bold, TextAlignmentOptions.Left);
            _upgInfo[i].color = OffWhite;
            Place(_upgInfo[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-150f, y + 14f), new Vector2(560f, 44f));

            _upgDesc[i] = NewText("UpgDesc" + i, parent, 22, FontStyles.Italic, TextAlignmentOptions.Left);
            _upgDesc[i].color = Disabled;
            _upgDesc[i].text = Loc.T(UpgDescs[i]);
            Place(_upgDesc[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-150f, y - 22f), new Vector2(620f, 34f));

            int idx = i; // capture for the listener
            Button btn = MakeButton(parent, "UpgBuy" + i, new Vector2(0.5f, 0.5f), new Vector2(330f, y), new Vector2(260f, 76f), Gold, out _upgBuyImg[i]);
            btn.onClick.AddListener(() => BuyUpgrade(UpgKinds[idx]));
            _upgBuyLbl[i] = NewText("Lbl", btn.transform, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(_upgBuyLbl[i].rectTransform);
        }

        void SectionLabel(Transform parent, string key, float y)
        {
            var t = NewText("Section_" + key, parent, 32, FontStyles.Bold, TextAlignmentOptions.Center);
            t.color = OffWhite;
            t.gameObject.AddComponent<LocalizedLabel>().Bind(t, key); // also fixes a latent bug: text was never set
            Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(900f, 50f));
        }

        void BuildMissionPanel(Transform parent)
        {
            _missionLines = new TMP_Text[3];
            _achLines = new TMP_Text[AchievementCatalog.All.Length];

            _missionPanel = new GameObject("MissionPanel", typeof(RectTransform), typeof(Image));
            _missionPanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_missionPanel.transform;
            Stretch(prt);
            _missionPanel.GetComponent<Image>().color = Dim;
            PanelCard(prt);

            var title = NewText("Title", prt, 64, FontStyles.Bold, TextAlignmentOptions.Top);
            title.color = OffWhite;
            title.gameObject.AddComponent<LocalizedLabel>().Bind(title, LocKeys.MetaGoals);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 90f));

            SectionLabel(prt, LocKeys.MetaSectionWeekly, 330f);
            float my = 270f;
            for (int i = 0; i < _missionLines.Length; i++)
            {
                _missionLines[i] = NewText("Mission" + i, prt, 30, FontStyles.Normal, TextAlignmentOptions.Center);
                _missionLines[i].color = OffWhite;
                Place(_missionLines[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, my), new Vector2(1000f, 48f));
                my -= 52f;
            }

            SectionLabel(prt, LocKeys.MetaSectionAchieve, 90f);
            float ay = 30f;
            for (int i = 0; i < _achLines.Length; i++)
            {
                _achLines[i] = NewText("Ach" + i, prt, 24, FontStyles.Normal, TextAlignmentOptions.Center);
                _achLines[i].color = LockedText;
                Place(_achLines[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, ay), new Vector2(1000f, 38f));
                ay -= 40f;
            }

            Button close = MakeButton(prt, "MissionClose", new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(420f, 100f), Gold, out _);
            close.onClick.AddListener(CloseMissions);
            var clbl = NewText("Label", close.transform, 40, FontStyles.Bold, TextAlignmentOptions.Center);
            clbl.color = Navy;
            clbl.gameObject.AddComponent<LocalizedLabel>().Bind(clbl, LocKeys.MetaClose);
            Stretch(clbl.rectTransform);

            _missionPanel.SetActive(false);
        }

        // ---------- settings (language) ----------

        void BuildSettingsPanel(Transform parent)
        {
            _settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            _settingsPanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_settingsPanel.transform;
            Stretch(prt);
            _settingsPanel.GetComponent<Image>().color = Dim;
            PanelCard(prt);

            var title = NewText("Title", prt, 64, FontStyles.Bold, TextAlignmentOptions.Top);
            title.color = OffWhite;
            title.gameObject.AddComponent<LocalizedLabel>().Bind(title, LocKeys.MetaSettings);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 90f));

            SectionLabel(prt, LocKeys.MetaLanguage, 250f);

            // Language names are endonyms — each shown in its own script, intentionally NOT localized.
            Button ru = MakeButton(prt, "LangRu", new Vector2(0.5f, 0.5f), new Vector2(-170f, 170f), new Vector2(300f, 96f), Navy, out _langRuImg);
            ru.onClick.AddListener(() => SetLanguage(SystemLanguage.Russian));
            var rul = NewText("Label", ru.transform, 34, FontStyles.Bold, TextAlignmentOptions.Center);
            rul.text = "Русский";
            Stretch(rul.rectTransform);

            Button en = MakeButton(prt, "LangEn", new Vector2(0.5f, 0.5f), new Vector2(170f, 170f), new Vector2(300f, 96f), Navy, out _langEnImg);
            en.onClick.AddListener(() => SetLanguage(SystemLanguage.English));
            var enl = NewText("Label", en.transform, 34, FontStyles.Bold, TextAlignmentOptions.Center);
            enl.text = "English";
            Stretch(enl.rectTransform);

            // Sound on/off (global mute via AudioListener.volume; label set in RefreshSettings).
            Button sound = MakeButton(prt, "SoundToggle", new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(460f, 90f), Navy, out _soundImg);
            sound.onClick.AddListener(ToggleSound);
            _soundLbl = NewText("Label", sound.transform, 34, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(_soundLbl.rectTransform);

            // "Заново" — wipes all progress; needs a confirming 2nd tap.
            Button reset = MakeButton(prt, "ResetProgress", new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(460f, 90f), Danger, out _resetImg);
            reset.onClick.AddListener(OnResetTapped);
            _resetLbl = NewText("Label", reset.transform, 34, FontStyles.Bold, TextAlignmentOptions.Center);
            _resetLbl.color = OffWhite;
            Stretch(_resetLbl.rectTransform);

            Button close = MakeButton(prt, "SettingsClose", new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(420f, 100f), Gold, out _);
            close.onClick.AddListener(CloseSettings);
            var clbl = NewText("Label", close.transform, 40, FontStyles.Bold, TextAlignmentOptions.Center);
            clbl.color = Navy;
            clbl.gameObject.AddComponent<LocalizedLabel>().Bind(clbl, LocKeys.MetaClose);
            Stretch(clbl.rectTransform);

            _settingsPanel.SetActive(false);
        }

        void OpenSettings()
        {
            if (_settingsPanel == null) return;
            _resetArmed = false; // always open in the safe state
            RefreshSettings();
            ShowPanel(_settingsPanel);
        }

        void CloseSettings() { _resetArmed = false; HidePanel(_settingsPanel); }

        void SetLanguage(SystemLanguage lang)
        {
            Loc.SetLanguage(lang); // fires LanguageChanged → LocalizedLabel + OnLanguageChanged re-resolve everything
            RefreshSettings();
        }

        void ToggleSound()
        {
            _soundOn = !_soundOn;
            AudioListener.volume = _soundOn ? 1f : 0f;
            PlayerPrefs.SetInt("towerpolis.sound", _soundOn ? 1 : 0);
            PlayerPrefs.Save();
            RefreshSettings();
        }

        // First tap arms ("ТОЧНО?"), second tap wipes progress and restarts.
        void OnResetTapped()
        {
            if (!_resetArmed) { _resetArmed = true; RefreshSettings(); return; }
            _resetArmed = false;
            if (_meta != null) _meta.ResetProgress();
            if (_controller != null) _controller.NewRun();
            RefreshTopBar();
            CloseSettings();
        }

        // Highlight the active language button + refresh the sound/reset button labels.
        void RefreshSettings()
        {
            bool ru = Loc.Language == SystemLanguage.Russian;
            if (_langRuImg != null) _langRuImg.color = ru ? Gold : Navy;
            if (_langEnImg != null) _langEnImg.color = ru ? Navy : Gold;
            if (_soundLbl != null) _soundLbl.text = Loc.T(_soundOn ? LocKeys.MetaSoundOn : LocKeys.MetaSoundOff);
            if (_soundLbl != null) _soundLbl.color = _soundOn ? Navy : OffWhite;
            if (_soundImg != null) _soundImg.color = _soundOn ? Gold : Navy;
            if (_resetLbl != null) _resetLbl.text = Loc.T(_resetArmed ? LocKeys.MetaResetConfirm : LocKeys.MetaReset);
            if (_resetImg != null) _resetImg.color = _resetArmed ? Danger : Navy;
        }

        // ---------- helpers ----------

        static Button MakeButton(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color bg, out Image img)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            img = go.GetComponent<Image>();
            img.color = bg;
            return go.GetComponent<Button>();
        }

        // A centred dialog surface behind a panel's content (so it reads as a card, not text on dim).
        static void PanelCard(Transform panel)
        {
            var go = new GameObject("Card", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1000f, 1720f);
            go.GetComponent<Image>().color = new Color(0.09f, 0.14f, 0.22f, 0.98f);
        }

        // A quick 1 → 1.18 → 1 scale bounce on a button (purchase/equip feedback).
        void PopButton(Transform t)
        {
            if (t != null) StartCoroutine(Pop(t));
        }

        static IEnumerator Pop(Transform t)
        {
            float e = 0f;
            const float dur = 0.20f;
            while (e < dur)
            {
                e += Time.deltaTime;
                if (t == null) yield break;
                float s = 1f + 0.18f * Mathf.Sin(Mathf.Clamp01(e / dur) * Mathf.PI);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one;
        }

        static TMP_Text NewText(string name, Transform parent, float size, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = OffWhite;
            t.raycastTarget = false;
            return t;
        }

        static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }
    }
}
