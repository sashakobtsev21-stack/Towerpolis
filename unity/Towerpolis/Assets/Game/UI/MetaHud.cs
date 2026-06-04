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

        TMP_Text _coinsLabel;
        TMP_Text _popLabel;
        TMP_Text _streakLabel;
        Image _dailyButtonImg;
        TMP_Text _dailyLabel;

        GameObject _cityPanel;
        TMP_Text _cityTitle;
        TMP_Text _cityPop;
        RectTransform _grid;
        GridLayoutGroup _gridLayout;

        void Start()
        {
            _meta = MetaService.Instance != null ? MetaService.Instance : FindFirstObjectByType<MetaService>();
            EnsureEventSystem(); // UGUI buttons need one to receive clicks
            BuildUI();
            if (_meta != null) _meta.RunBanked += OnBanked;
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
            if (_meta != null) _meta.RunBanked -= OnBanked;
        }

        void OnBanked(RunEndOutcome outcome)
        {
            RefreshTopBar();
            if (_cityPanel != null && _cityPanel.activeSelf) PopulateCity();
        }

        void RefreshTopBar()
        {
            if (_meta == null) return;
            if (_coinsLabel != null) _coinsLabel.text = "COINS  " + _meta.Coins;
            if (_popLabel != null) _popLabel.text = "CITY  " + _meta.TotalPopulation;
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
                _dailyLabel.text = played ? "DONE" : "DAILY";
                _dailyLabel.color = played ? OffWhite : Navy;
            }
            if (_dailyButtonImg != null) _dailyButtonImg.color = played ? Navy : Gold;
            if (_streakLabel != null) _streakLabel.text = "STREAK  " + _meta.StreakCurrent;
        }

        // ---------- city view ----------

        void OpenCity()
        {
            if (_cityPanel == null) return;
            PopulateCity();
            _cityPanel.SetActive(true);
            InputGate.Suppress = true; // don't drop a block while the city view is up
        }

        void CloseCity()
        {
            if (_cityPanel != null) _cityPanel.SetActive(false);
            InputGate.Suppress = false;
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
            if (_cityPop != null) _cityPop.text = "POPULATION  " + population + "  /  " + info.FillGoal;

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

            _coinsLabel = NewText("Coins", canvasGo.transform, 40, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            _coinsLabel.color = Gold;
            Place(_coinsLabel.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -36f), new Vector2(420f, 56f));

            _popLabel = NewText("CityPop", canvasGo.transform, 34, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(_popLabel.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -92f), new Vector2(420f, 50f));

            _streakLabel = NewText("Streak", canvasGo.transform, 30, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(_streakLabel.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -140f), new Vector2(420f, 46f));

            CityButton(canvasGo.transform);
            DailyButton(canvasGo.transform);
            BuildCityPanel(canvasGo.transform);
        }

        void CityButton(Transform parent)
        {
            var btnGo = new GameObject("CityButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(28f, -200f);
            rt.sizeDelta = new Vector2(180f, 72f);
            btnGo.GetComponent<Image>().color = Navy;
            btnGo.GetComponent<Button>().onClick.AddListener(OpenCity);

            var label = NewText("Label", rt, 32, FontStyles.Bold, TextAlignmentOptions.Center);
            label.text = "CITY";
            Stretch(label.rectTransform);
        }

        void DailyButton(Transform parent)
        {
            var btnGo = new GameObject("DailyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(224f, -200f);
            rt.sizeDelta = new Vector2(180f, 72f);
            _dailyButtonImg = btnGo.GetComponent<Image>();
            _dailyButtonImg.color = Gold;
            btnGo.GetComponent<Button>().onClick.AddListener(OnDailyTapped);

            _dailyLabel = NewText("Label", rt, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            _dailyLabel.color = Navy;
            _dailyLabel.text = "DAILY";
            Stretch(_dailyLabel.rectTransform);
        }

        void BuildCityPanel(Transform parent)
        {
            _cityPanel = new GameObject("CityPanel", typeof(RectTransform), typeof(Image));
            _cityPanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_cityPanel.transform;
            Stretch(prt);
            _cityPanel.GetComponent<Image>().color = Dim; // dim full-screen backdrop, blocks taps

            _cityTitle = NewText("Title", prt, 64, FontStyles.Bold, TextAlignmentOptions.Top);
            _cityTitle.color = OffWhite;
            Place(_cityTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 90f));

            _cityPop = NewText("Pop", prt, 40, FontStyles.Normal, TextAlignmentOptions.Top);
            _cityPop.color = Gold;
            Place(_cityPop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(900f, 60f));

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
            label.text = "CLOSE";
            Stretch(label.rectTransform);
        }

        // ---------- helpers ----------

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
