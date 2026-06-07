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
    public sealed partial class MetaHud : MonoBehaviour
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
    }
}
