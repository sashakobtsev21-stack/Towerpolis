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

        // Top-bar buttons: a uniform evenly-spaced row (CITY · DAILY · UPGRADES · SKINS · GOALS).
        const float TopBarW = 168f;
        static float TopBarX(int i) => 24f + i * (TopBarW + 12f);

        MetaService _meta;
        TowerGameController _controller;

        TMP_Text _popLabel;
        Image _dailyButtonImg;
        TMP_Text _dailyLabel;
        TMP_Text _slowMoHint; // "HOLD TO SLOW" — shown only when a Slow-Mo charge is ready

        static readonly string[] DistIds = { "downtown", "neon", "winter" };
        static readonly string[] DistNames = { "ЦЕНТР", "НЕОН", "ЗИМА" };
        static readonly Color Locked = new Color(0.22f, 0.24f, 0.30f, 0.95f);
        Image[] _distImg;
        TMP_Text[] _distLbl;

        GameObject _cityPanel;
        TMP_Text _cityTitle;
        TMP_Text _cityPop;
        RectTransform _grid;
        GridLayoutGroup _gridLayout;

        // Upgrades panel
        static readonly UpgradeKind[] UpgKinds = { UpgradeKind.Magnet, UpgradeKind.SlowMo, UpgradeKind.CityBonus };
        static readonly string[] UpgNames = { "МАГНИТ", "ЗАМЕДЛЕНИЕ", "БОНУС ГОРОДА" };
        static readonly string[] UpgDescs =
        {
            "Подтягивает блок к центру (Endless)",
            "Зажми палец — кран замедляется (Endless)",
            "Больше монет за достройку района",
        };
        static readonly Color Disabled = new Color(0.60f, 0.62f, 0.67f);
        GameObject _upgPanel;
        TMP_Text _upgCoins;
        TMP_Text[] _upgInfo;
        TMP_Text[] _upgDesc;
        TMP_Text[] _upgBuyLbl;
        Image[] _upgBuyImg;
        TMP_Text _freezeInfo, _freezeBuyLbl, _loginLbl;
        Image _freezeBuyImg, _loginImg;

        // Skins panel
        static readonly Color Buyable = new Color(0.40f, 0.74f, 0.42f);
        GameObject _skinPanel;
        TMP_Text _skinCoins;
        Image[] _blockSkinImg, _craneSkinImg;
        TMP_Text[] _blockSkinLbl, _craneSkinLbl;

        // Missions panel
        static readonly Color LockedText = new Color(0.62f, 0.64f, 0.68f);
        GameObject _missionPanel;
        TMP_Text[] _missionLines;
        TMP_Text[] _achLines;

        // Run-end toasts (mission/achievement/district completions)
        static readonly Color Cyan = new Color(0.50f, 0.85f, 1f);
        Transform _toastRoot;
        readonly Queue<Toast> _toasts = new Queue<Toast>();
        bool _toasting;
        struct Toast { public string Text; public Color Color; }

        void Start()
        {
            UiFont.EnsureCyrillic(); // render Cyrillic with the default TMP font
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
            RefreshTopBar();
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
        }

        void OnBanked(RunEndOutcome outcome)
        {
            RefreshTopBar();
            if (_cityPanel != null && _cityPanel.activeSelf) PopulateCity();
            if (_upgPanel != null && _upgPanel.activeSelf) PopulateUpgrades();
            if (outcome.DistrictCompletedNow) EnqueueToast("РАЙОН ЗАВЕРШЁН!", Gold);
        }

        void OnProgressionChanged()
        {
            if (_upgPanel != null && _upgPanel.activeSelf) PopulateUpgrades();
            if (_skinPanel != null && _skinPanel.activeSelf) PopulateSkins();
        }

        void OnSystemsResolved(RunSystemsOutcome sys)
        {
            if (_missionPanel != null && _missionPanel.activeSelf) PopulateMissions();

            if (sys.CompletedMissions != null)
                foreach (string id in sys.CompletedMissions)
                {
                    MissionDef m = MissionCatalog.Get(id);
                    EnqueueToast("МИССИЯ ВЫПОЛНЕНА\n" + m.Name + "   +" + m.Info.RewardCoins, Gold);
                }
            if (sys.UnlockedAchievements != null)
                foreach (string id in sys.UnlockedAchievements)
                {
                    AchievementDef a = FindAchievement(id);
                    EnqueueToast("ДОСТИЖЕНИЕ\n" + a.Name + "   +" + a.Info.RewardCoins, Cyan);
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
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            float from = deactivateAtEnd ? cg.alpha : 0f;
            float e = 0f;
            cg.alpha = from;
            while (e < dur)
            {
                e += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, e / dur);
                yield return null;
            }
            cg.alpha = to;
            if (deactivateAtEnd) panel.SetActive(false);
        }

        void OnFloorLive(int floors) => RefreshTopBar();
        void OnRunStartLive() => RefreshTopBar();

        void RefreshTopBar()
        {
            if (_meta == null) return;

            // THIS building's residents (current run) — starts at 0 each run, grows as you stack. The
            // cumulative city population (the meta-score) is shown in the city view.
            int residents = _controller != null ? _controller.BuildRunResult().TotalResidents : 0;
            if (_popLabel != null) _popLabel.text = "ЖИЛЬЦЫ  " + residents;
            RefreshDaily();
        }

        void OnDailyTapped()
        {
            if (_meta == null) return;
            _meta.TryStartDaily(); // no-op if already played today
            RefreshDaily();
        }

        void RefreshDaily()
        {
            if (_meta == null) return;
            bool played = _meta.HasPlayedDailyToday();
            if (_dailyLabel != null)
            {
                _dailyLabel.text = played ? "ГОТОВО" : "ДЕНЬ";
                _dailyLabel.color = played ? OffWhite : Navy;
            }
            if (_dailyButtonImg != null) _dailyButtonImg.color = played ? Navy : Gold;
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
                    _distLbl[i].text = DistNames[i];
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

            if (_cityTitle != null) _cityTitle.text = view.DisplayName;
            if (_cityPop != null) _cityPop.text = "НАСЕЛЕНИЕ  " + population + "  /  " + info.FillGoal;
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

        void BuyFreeze()
        {
            bool ok = _meta != null && _meta.BuyFreezeCharge();
            PopulateUpgrades();
            if (ok && _freezeBuyImg != null) PopButton(_freezeBuyImg.transform);
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
            if (_upgCoins != null) _upgCoins.text = "МОНЕТЫ  " + coins;

            for (int i = 0; i < UpgKinds.Length; i++)
            {
                UpgradeKind k = UpgKinds[i];
                int lvl = _meta.UpgradeLevel(k), max = _meta.UpgradeMaxLevel(k);
                bool maxed = _meta.IsUpgradeMaxed(k);
                int cost = _meta.NextUpgradeCost(k);
                bool afford = coins >= cost;
                if (_upgInfo[i] != null) _upgInfo[i].text = UpgNames[i] + "   Ур " + lvl + " / " + max;
                SetBuy(_upgBuyImg[i], _upgBuyLbl[i], maxed ? "МАКС" : "КУПИТЬ " + cost, maxed, afford);
            }

            int charges = _meta.FreezeCharges, fmax = _meta.FreezeMax;
            bool freezeFull = charges >= fmax;
            if (_freezeInfo != null) _freezeInfo.text = "ЗАМОРОЗКА   x" + charges + " / " + fmax;
            SetBuy(_freezeBuyImg, _freezeBuyLbl, freezeFull ? "ПОЛНО" : "КУПИТЬ " + _meta.FreezeCost, freezeFull, coins >= _meta.FreezeCost);

            bool canClaim = _meta.CanClaimLoginToday();
            if (_loginLbl != null)
            {
                _loginLbl.text = canClaim ? "ЗАБРАТЬ ПОДАРОК" : "ПОДАРОК ВЗЯТ";
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

        // ---------- skins view ----------

        void OpenSkins()
        {
            if (_skinPanel == null) return;
            PopulateSkins();
            ShowPanel(_skinPanel);
        }

        void CloseSkins() => HidePanel(_skinPanel);

        void TapBlockSkin(string id)
        {
            bool ok = _meta != null && _meta.BuyOrEquipBlockSkin(id);
            PopulateSkins();
            int i = BlockSkinIndex(id);
            if (ok && i >= 0 && _blockSkinImg[i] != null) PopButton(_blockSkinImg[i].transform);
        }

        void TapCraneSkin(string id)
        {
            bool ok = _meta != null && _meta.BuyOrEquipCraneSkin(id);
            PopulateSkins();
            int i = CraneSkinIndex(id);
            if (ok && i >= 0 && _craneSkinImg[i] != null) PopButton(_craneSkinImg[i].transform);
        }

        static int BlockSkinIndex(string id)
        {
            BlockSkin[] a = CosmeticCatalog.BlockSkins;
            for (int i = 0; i < a.Length; i++) if (a[i].Id == id) return i;
            return -1;
        }

        static int CraneSkinIndex(string id)
        {
            CraneSkin[] a = CosmeticCatalog.CraneSkins;
            for (int i = 0; i < a.Length; i++) if (a[i].Id == id) return i;
            return -1;
        }

        void PopulateSkins()
        {
            if (_meta == null) return;
            int coins = _meta.Coins;
            if (_skinCoins != null) _skinCoins.text = "МОНЕТЫ  " + coins;

            BlockSkin[] blocks = CosmeticCatalog.BlockSkins;
            for (int i = 0; i < blocks.Length; i++)
                SetSkin(_blockSkinImg[i], _blockSkinLbl[i], blocks[i].DisplayName, blocks[i].Cost, blocks[i].RequiredDistrictId,
                    _meta.IsBlockSkinEquipped(blocks[i].Id), _meta.IsBlockSkinOwned(blocks[i].Id), coins);

            CraneSkin[] cranes = CosmeticCatalog.CraneSkins;
            for (int i = 0; i < cranes.Length; i++)
                SetSkin(_craneSkinImg[i], _craneSkinLbl[i], cranes[i].DisplayName, cranes[i].Cost, cranes[i].RequiredDistrictId,
                    _meta.IsCraneSkinEquipped(cranes[i].Id), _meta.IsCraneSkinOwned(cranes[i].Id), coins);
        }

        void SetSkin(Image img, TMP_Text lbl, string name, int cost, string gate, bool equipped, bool owned, int coins)
        {
            bool unlocked = _meta.IsDistrictRewarded(gate);
            string state;
            Color bg, fg;
            if (equipped) { state = "НАДЕТО"; bg = Gold; fg = Navy; }
            else if (owned) { state = "НАДЕТЬ"; bg = Navy; fg = OffWhite; }
            else if (!unlocked) { state = "ЗАКРЫТО"; bg = Locked; fg = Disabled; }
            else
            {
                bool afford = coins >= cost;
                state = "КУПИТЬ " + cost;
                bg = afford ? Buyable : Locked;
                fg = afford ? Navy : Disabled;
            }
            if (lbl != null) { lbl.text = name + "\n" + state; lbl.color = fg; }
            if (img != null) img.color = bg;
        }

        // Centre n equal cells of width w (gap g) and return the x of cell i.
        static float RowX(int n, int i, float w, float g)
        {
            float total = n * w + (n - 1) * g;
            return -total * 0.5f + w * 0.5f + i * (w + g);
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
                    _missionLines[i].text = def.Description + "    " + prog + "/" + def.Info.Target +
                                            "    +" + def.Info.RewardCoins + (done ? "    ГОТОВО" : "");
                    _missionLines[i].color = done ? Gold : OffWhite;
                }
                else _missionLines[i].text = "";
            }

            AchievementDef[] all = AchievementCatalog.All;
            for (int i = 0; i < _achLines.Length; i++)
            {
                if (_achLines[i] == null) continue;
                bool got = _meta.IsAchievementUnlocked(all[i].Info.AchievementId);
                _achLines[i].text = all[i].Name + " — " + all[i].Description +
                                    (got ? "    ГОТОВО" : "    +" + all[i].Info.RewardCoins);
                _achLines[i].color = got ? Gold : LockedText;
            }
        }

        // ---------- UI construction ----------

        void BuildUI()
        {
            var canvasGo = new GameObject("MetaHUD_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11; // above the gameplay HUD (10)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _toastRoot = canvasGo.transform;

            // Only POPULATION is shown in the top bar now (coins/streak are tracked in Core for later,
            // not displayed — owner: "нужно только население и этажей"; height is the HUD's big number).
            _popLabel = NewText("CityPop", canvasGo.transform, 40, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            _popLabel.color = Gold;
            Place(_popLabel.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -36f), new Vector2(420f, 56f));

            CityButton(canvasGo.transform);
            DailyButton(canvasGo.transform);
            UpgradesButton(canvasGo.transform);
            SkinsButton(canvasGo.transform);
            MissionsButton(canvasGo.transform);
            BuildCityPanel(canvasGo.transform);
            BuildUpgradePanel(canvasGo.transform);
            BuildSkinPanel(canvasGo.transform);
            BuildMissionPanel(canvasGo.transform);

            _slowMoHint = NewText("SlowMoHint", canvasGo.transform, 34, FontStyles.Bold, TextAlignmentOptions.Center);
            _slowMoHint.color = new Color(0.55f, 0.85f, 1f);
            _slowMoHint.text = "ЗАЖМИ, ЧТОБЫ ЗАМЕДЛИТЬ";
            Place(_slowMoHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(600f, 60f));
            _slowMoHint.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_slowMoHint == null || _controller == null) return;
            bool show = _controller.SlowMoHintActive;
            if (_slowMoHint.gameObject.activeSelf != show) _slowMoHint.gameObject.SetActive(show);
            if (show)
            {
                Color c = _slowMoHint.color;
                c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 3.2f)); // gentle pulse to catch the eye
                _slowMoHint.color = c;
            }
        }

        void CityButton(Transform parent)
        {
            var btnGo = new GameObject("CityButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(TopBarX(0), -104f);
            rt.sizeDelta = new Vector2(TopBarW, 72f);
            btnGo.GetComponent<Image>().color = Navy;
            btnGo.GetComponent<Button>().onClick.AddListener(OpenCity);

            var label = NewText("Label", rt, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            label.text = "ГОРОД";
            Stretch(label.rectTransform);
        }

        void DailyButton(Transform parent)
        {
            var btnGo = new GameObject("DailyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(TopBarX(1), -104f);
            rt.sizeDelta = new Vector2(TopBarW, 72f);
            _dailyButtonImg = btnGo.GetComponent<Image>();
            _dailyButtonImg.color = Gold;
            btnGo.GetComponent<Button>().onClick.AddListener(OnDailyTapped);

            _dailyLabel = NewText("Label", rt, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            _dailyLabel.color = Navy;
            _dailyLabel.text = "ДЕНЬ";
            Stretch(_dailyLabel.rectTransform);
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
            label.text = "ЗАКРЫТЬ";
            Stretch(label.rectTransform);
        }

        void UpgradesButton(Transform parent)
        {
            Button btn = MakeButton(parent, "UpgradesButton", new Vector2(0f, 1f), new Vector2(TopBarX(2), -104f), new Vector2(TopBarW, 72f), Navy, out _);
            btn.onClick.AddListener(OpenUpgrades);
            var label = NewText("Label", btn.transform, 25, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = OffWhite;
            label.text = "УЛУЧШЕНИЯ";
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
            title.text = "УЛУЧШЕНИЯ";
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 90f));

            _upgCoins = NewText("Coins", prt, 40, FontStyles.Bold, TextAlignmentOptions.Top);
            _upgCoins.color = Gold;
            Place(_upgCoins.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(900f, 60f));

            var hint = NewText("CoinHint", prt, 24, FontStyles.Italic, TextAlignmentOptions.Top);
            hint.color = Disabled;
            hint.text = "Монеты: +1 за этаж · +2 за идеальную постановку · награды за район/цели";
            Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -262f), new Vector2(980f, 36f));

            float[] ys = { 250f, 150f, 50f };
            for (int i = 0; i < UpgKinds.Length; i++) UpgradeRow(prt, i, ys[i]);

            _freezeInfo = NewText("FreezeInfo", prt, 34, FontStyles.Bold, TextAlignmentOptions.Left);
            _freezeInfo.color = OffWhite;
            Place(_freezeInfo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-150f, -70f), new Vector2(560f, 70f));
            Button freezeBtn = MakeButton(prt, "FreezeBuy", new Vector2(0.5f, 0.5f), new Vector2(330f, -70f), new Vector2(260f, 76f), Gold, out _freezeBuyImg);
            freezeBtn.onClick.AddListener(BuyFreeze);
            _freezeBuyLbl = NewText("Lbl", freezeBtn.transform, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(_freezeBuyLbl.rectTransform);

            Button loginBtn = MakeButton(prt, "LoginClaim", new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(520f, 84f), Gold, out _loginImg);
            loginBtn.onClick.AddListener(ClaimLoginGift);
            _loginLbl = NewText("Lbl", loginBtn.transform, 32, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(_loginLbl.rectTransform);

            Button close = MakeButton(prt, "UpgClose", new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(420f, 100f), Gold, out _);
            close.onClick.AddListener(CloseUpgrades);
            var clbl = NewText("Label", close.transform, 40, FontStyles.Bold, TextAlignmentOptions.Center);
            clbl.color = Navy;
            clbl.text = "ЗАКРЫТЬ";
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
            _upgDesc[i].text = UpgDescs[i];
            Place(_upgDesc[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-150f, y - 22f), new Vector2(620f, 34f));

            int idx = i; // capture for the listener
            Button btn = MakeButton(parent, "UpgBuy" + i, new Vector2(0.5f, 0.5f), new Vector2(330f, y), new Vector2(260f, 76f), Gold, out _upgBuyImg[i]);
            btn.onClick.AddListener(() => BuyUpgrade(UpgKinds[idx]));
            _upgBuyLbl[i] = NewText("Lbl", btn.transform, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(_upgBuyLbl[i].rectTransform);
        }

        void SkinsButton(Transform parent)
        {
            Button btn = MakeButton(parent, "SkinsButton", new Vector2(0f, 1f), new Vector2(TopBarX(3), -104f), new Vector2(TopBarW, 72f), Navy, out _);
            btn.onClick.AddListener(OpenSkins);
            var label = NewText("Label", btn.transform, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = OffWhite;
            label.text = "СКИНЫ";
            Stretch(label.rectTransform);
        }

        void BuildSkinPanel(Transform parent)
        {
            BlockSkin[] blocks = CosmeticCatalog.BlockSkins;
            CraneSkin[] cranes = CosmeticCatalog.CraneSkins;
            _blockSkinImg = new Image[blocks.Length];
            _blockSkinLbl = new TMP_Text[blocks.Length];
            _craneSkinImg = new Image[cranes.Length];
            _craneSkinLbl = new TMP_Text[cranes.Length];

            _skinPanel = new GameObject("SkinPanel", typeof(RectTransform), typeof(Image));
            _skinPanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_skinPanel.transform;
            Stretch(prt);
            _skinPanel.GetComponent<Image>().color = Dim;
            PanelCard(prt);

            var title = NewText("Title", prt, 64, FontStyles.Bold, TextAlignmentOptions.Top);
            title.color = OffWhite;
            title.text = "СКИНЫ";
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 90f));

            _skinCoins = NewText("Coins", prt, 40, FontStyles.Bold, TextAlignmentOptions.Top);
            _skinCoins.color = Gold;
            Place(_skinCoins.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(900f, 60f));

            SectionLabel(prt, "БЛОКИ", 260f);
            for (int i = 0; i < blocks.Length; i++)
            {
                string id = blocks[i].Id; // capture
                Button b = SkinButton(prt, "Bskin_" + id, RowX(blocks.Length, i, 196f, 12f), 170f, out _blockSkinImg[i], out _blockSkinLbl[i]);
                b.onClick.AddListener(() => TapBlockSkin(id));
            }

            SectionLabel(prt, "КРАН", -40f);
            for (int i = 0; i < cranes.Length; i++)
            {
                string id = cranes[i].Id; // capture
                Button b = SkinButton(prt, "Cskin_" + id, RowX(cranes.Length, i, 196f, 12f), -130f, out _craneSkinImg[i], out _craneSkinLbl[i]);
                b.onClick.AddListener(() => TapCraneSkin(id));
            }

            Button close = MakeButton(prt, "SkinClose", new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(420f, 100f), Gold, out _);
            close.onClick.AddListener(CloseSkins);
            var clbl = NewText("Label", close.transform, 40, FontStyles.Bold, TextAlignmentOptions.Center);
            clbl.color = Navy;
            clbl.text = "ЗАКРЫТЬ";
            Stretch(clbl.rectTransform);

            _skinPanel.SetActive(false);
        }

        void SectionLabel(Transform parent, string text, float y)
        {
            var t = NewText("Section_" + text, parent, 32, FontStyles.Bold, TextAlignmentOptions.Center);
            t.color = OffWhite;
            Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(900f, 50f));
        }

        Button SkinButton(Transform parent, string name, float x, float y, out Image img, out TMP_Text lbl)
        {
            Button btn = MakeButton(parent, name, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(196f, 150f), Navy, out img);
            lbl = NewText("Lbl", btn.transform, 26, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(lbl.rectTransform);
            return btn;
        }

        void MissionsButton(Transform parent)
        {
            Button btn = MakeButton(parent, "MissionsButton", new Vector2(0f, 1f), new Vector2(TopBarX(4), -104f), new Vector2(TopBarW, 72f), Navy, out _);
            btn.onClick.AddListener(OpenMissions);
            var label = NewText("Label", btn.transform, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = OffWhite;
            label.text = "ЦЕЛИ";
            Stretch(label.rectTransform);
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
            title.text = "ЦЕЛИ";
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 90f));

            SectionLabel(prt, "МИССИИ НЕДЕЛИ", 330f);
            float my = 270f;
            for (int i = 0; i < _missionLines.Length; i++)
            {
                _missionLines[i] = NewText("Mission" + i, prt, 30, FontStyles.Normal, TextAlignmentOptions.Center);
                _missionLines[i].color = OffWhite;
                Place(_missionLines[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, my), new Vector2(1000f, 48f));
                my -= 52f;
            }

            SectionLabel(prt, "ДОСТИЖЕНИЯ", 90f);
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
            clbl.text = "ЗАКРЫТЬ";
            Stretch(clbl.rectTransform);

            _missionPanel.SetActive(false);
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
