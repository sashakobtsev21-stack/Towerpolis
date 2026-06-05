using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Towerpolis.Core.Meta;
using Towerpolis.Game.Gameplay;
using Towerpolis.Game.Meta;

namespace Towerpolis.Game.UI
{
    /// <summary>
    /// MVP HUD built entirely in code (no manual Canvas wiring): score (top-centre), height (top-right),
    /// two strike pips (bottom-left), a "PERFECT!" world pop, a strike/miss vignette flash, and an
    /// end-of-run restart panel. Subscribes to <see cref="TowerGameController"/>'s events. UGUI + TMP.
    /// Add this component to any GameObject; it finds the game controller and builds itself.
    /// (If text doesn't appear: Window → TextMeshPro → Import TMP Essential Resources, once.)
    /// </summary>
    public sealed class HUDController : MonoBehaviour
    {
        static readonly Color Navy = new Color(0.12f, 0.23f, 0.37f);
        static readonly Color Coral = new Color(1.00f, 0.42f, 0.37f);
        static readonly Color Yellow = new Color(1.00f, 0.835f, 0.31f);
        static readonly Color OffWhite = new Color(0.97f, 0.97f, 0.95f);
        static readonly Color Mint = new Color(0.40f, 0.73f, 0.40f);
        static readonly Color PipEmpty = new Color(0.12f, 0.23f, 0.37f, 0.6f);

        // Combo meter (Phase B): three "ember" pips that light as the combo climbs (0..3).
        static readonly Color ComboGhost = new Color(0.18f, 0.28f, 0.42f, 0.30f); // unlit pip
        static readonly Color[] ComboLit =
        {
            new Color(0.18f, 0.28f, 0.42f, 0.30f), // 0 (no lit pip)
            new Color(1.00f, 0.60f, 0.18f, 1f),    // 1 — amber
            new Color(1.00f, 0.38f, 0.12f, 1f),    // 2 — orange-red
            new Color(1.00f, 0.84f, 0.18f, 1f),    // 3 — gold ("on fire")
        };

        TowerGameController _game;
        Camera _cam;

        RectTransform _safeRoot;
        TMP_Text _scoreLabel;
        TMP_Text _coinsLabel;   // live coin tally (top-right): wallet + this-run earned-so-far
        Image _coinIcon;        // gold coin disc next to the tally (generated sprite — no font glyph)
        int _shownCoins = -1;
        static Sprite _coinSprite;
        readonly Image[] _pips = new Image[2];
        Image _vignette;

        RectTransform _comboRoot;
        readonly Image[] _comboPips = new Image[3];
        TMP_Text _comboLabel;
        int _shownComboLevel = -1, _shownChain = -1;
        bool _runLive;
        Coroutine _comboPulseCo, _comboPunchCo;

        TMP_Text _perfectLabel;
        RectTransform _perfectRect;

        GameObject _restartPanel;
        TMP_Text _restartScore;
        TMP_Text _restartBest;
        TMP_Text _restartCoins;
        RunResult _toppledResult; // captured at topple (before any restart) for the coin breakdown

        TMP_Text _summitLabel;
        bool _summitShown;
        const int SummitHeight = 15; // TESTING: low so the beat is reachable (design = 200) — restore before launch

        int _highScore;

        void Start()
        {
            UiFont.EnsureCyrillic(); // render Cyrillic with the default TMP font
            Loc.Init();              // restore the saved/device language (ADR-0008)
            _game = GetComponent<TowerGameController>();
            if (_game == null) _game = FindFirstObjectByType<TowerGameController>();
            _cam = Camera.main;

            BuildUI();

            if (_game != null)
            {
                _game.ScoreChanged += OnScoreChanged;
                _game.FloorAdded += OnFloorAdded;
                _game.StrikeAdded += OnStrikeAdded;
                _game.PerfectHit += OnPerfect;
                _game.RunToppled += OnToppled;
                _game.RunStarted += OnRunStarted;
            }
            OnRunStarted();
        }

        void OnDestroy()
        {
            if (_game == null) return;
            _game.ScoreChanged -= OnScoreChanged;
            _game.FloorAdded -= OnFloorAdded;
            _game.StrikeAdded -= OnStrikeAdded;
            _game.PerfectHit -= OnPerfect;
            _game.RunToppled -= OnToppled;
            _game.RunStarted -= OnRunStarted;
        }

        void Update()
        {
            RefreshCoins();      // live tally — ticks up as floors land
            UpdateComboMeter();  // show/hide + repaint the combo embers
            // (device safe area is handled by the SafeAreaRoot component on _safeRoot)
        }

        // ---------- event handlers ----------

        void OnScoreChanged(int score)
        {
            // Score is no longer the headline — HEIGHT is (see OnFloorAdded). Still subscribed so the
            // leaderboard score keeps flowing through Core; only the on-screen number changed.
        }

        void OnFloorAdded(int floors)
        {
            // Headline = building HEIGHT (floors placed; the base floor is 0).
            if (_scoreLabel != null) _scoreLabel.text = floors.ToString();
            Punch(_scoreLabel != null ? _scoreLabel.rectTransform : null, 1.2f);
            RefreshCoins(); // each placed floor banks +1 coin (+2 on a Perfect) → show it immediately

            if (floors >= SummitHeight && !_summitShown)
            {
                _summitShown = true;
                StartCoroutine(SummitBeat());
            }
        }

        void OnStrikeAdded(int strikeNumber)
        {
            int idx = Mathf.Clamp(strikeNumber - 1, 0, _pips.Length - 1);
            if (_pips[idx] != null)
            {
                _pips[idx].color = Coral;
                Punch(_pips[idx].rectTransform, 1.4f);
            }
            FlashVignette(0.25f, 0.4f);
        }

        void OnPerfect(Vector3 worldPos)
        {
            if (_perfectLabel == null || _cam == null) return;
            StopCoroutine(nameof(PerfectPop));
            _pendingPerfectWorld = worldPos;
            StartCoroutine(nameof(PerfectPop));
        }

        void OnToppled()
        {
            FlashVignette(0.5f, 0.8f);
            for (int i = 0; i < _pips.Length; i++)
                if (_pips[i] != null) _pips[i].color = Coral;
            _runLive = false;
            StopComboAnim();
            _toppledResult = _game != null ? _game.BuildRunResult() : default; // capture now (before restart)
            StartCoroutine(ShowRestartAfter(1.2f));
        }

        void OnRunStarted()
        {
            if (_scoreLabel != null) _scoreLabel.text = "0";
            RefreshCoins();
            for (int i = 0; i < _pips.Length; i++)
                if (_pips[i] != null) _pips[i].color = PipEmpty;
            if (_restartPanel != null) _restartPanel.SetActive(false);
            _summitShown = false;
            if (_summitLabel != null) _summitLabel.gameObject.SetActive(false);
            _runLive = true;
            _shownComboLevel = -1; // force a repaint of the combo embers on the first drop
            _shownChain = -1;
            StopComboAnim();
        }

        // Live coin tally on the main HUD = banked wallet + this-run earned-so-far (1/floor + 2/perfect).
        // While a run is live it ticks up per placed floor; once the run is over we show the wallet alone
        // (which the run-end bank has just topped up), so the number never double-counts at the topple.
        void RefreshCoins()
        {
            if (_coinsLabel == null) return;
            MetaService meta = MetaService.Instance;
            if (meta == null) return;
            int preview = (_game != null && !_game.IsOver) ? meta.PreviewCoins(_game.BuildRunResult()) : 0;
            int total = meta.Coins + preview;
            if (total == _shownCoins) return; // only rebuild the text when it actually changes
            _shownCoins = total;
            _coinsLabel.text = total.ToString();
        }

        // A small filled gold disc for the coin icon — generated once so it needs no font glyph or art asset.
        static Sprite CoinSprite()
        {
            if (_coinSprite != null) return _coinSprite;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            float r = s * 0.5f - 1f, c = s * 0.5f - 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - c, dy = y - c;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy)); // solid inside, ~1px soft edge
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            _coinSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _coinSprite;
        }

        // ---------- combo meter (Phase B) ----------

        // Three "ember" pips in the cleared bottom-centre band + a chain badge below them.
        void BuildComboMeter(Transform parent)
        {
            var go = new GameObject("ComboMeter", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _comboRoot = (RectTransform)go.transform;
            _comboRoot.anchorMin = _comboRoot.anchorMax = _comboRoot.pivot = new Vector2(0.5f, 0f);
            _comboRoot.anchoredPosition = new Vector2(0f, 220f);
            _comboRoot.sizeDelta = new Vector2(360f, 96f);

            float[] xs = { -100f, 0f, 100f };
            for (int i = 0; i < 3; i++)
            {
                _comboPips[i] = NewImage("ComboPip" + i, _comboRoot, ComboGhost);
                _comboPips[i].sprite = CoinSprite(); // reuse the generated disc
                _comboPips[i].raycastTarget = false;
                Place(_comboPips[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(xs[i], 12f), new Vector2(72f, 72f));
            }

            _comboLabel = NewText("ComboLabel", _comboRoot, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            _comboLabel.color = ComboLit[3];
            Place(_comboLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(320f, 40f));

            _comboRoot.gameObject.SetActive(false);
        }

        // Show only during a live run with no modal open; repaint the embers when the level/chain changes.
        void UpdateComboMeter()
        {
            if (_comboRoot == null || _game == null) return;
            bool show = _runLive && !_game.IsOver && !InputGate.Suppress;
            if (_comboRoot.gameObject.activeSelf != show) _comboRoot.gameObject.SetActive(show);
            if (!show) return;

            int level = _game.ComboLevel; // 0..3
            int chain = _game.PerfectChain;
            if (level == _shownComboLevel && chain == _shownChain) return;
            int prev = _shownComboLevel;
            _shownComboLevel = level;
            _shownChain = chain;

            int lit = Mathf.Clamp(level, 1, 3);
            for (int i = 0; i < 3; i++)
                if (_comboPips[i] != null) _comboPips[i].color = i < level ? ComboLit[lit] : ComboGhost;

            if (_comboLabel != null)
            {
                bool showLabel = level > 0;
                if (_comboLabel.gameObject.activeSelf != showLabel) _comboLabel.gameObject.SetActive(showLabel);
                if (showLabel)
                {
                    _comboLabel.text = Loc.T(level >= 3 ? LocKeys.HudStreak : LocKeys.HudChain, chain);
                    _comboLabel.color = ComboLit[lit];
                }
            }

            if (level == prev) return; // only the chain count changed → no scale animation

            if (level >= 3)
            {
                if (_comboPulseCo == null) _comboPulseCo = StartCoroutine(ComboPulseCo()); // breathe owns the scale at L3
            }
            else
            {
                if (_comboPulseCo != null) { StopCoroutine(_comboPulseCo); _comboPulseCo = null; } // leaving L3
                if (prev < 0) { if (_comboRoot != null) _comboRoot.localScale = Vector3.one; } // first paint, no punch
                else if (level > prev) StartComboPunch(1.22f); // tier up → pop
                else if (level > 0) StartComboPunch(0.90f);    // decay one tier → small dip
                else StartComboPunch(0.80f);                   // break → crush
            }
        }

        // Stop both combo tweens and reset scale — used on run start / topple so nothing leaks into the next run.
        void StopComboAnim()
        {
            if (_comboPulseCo != null) { StopCoroutine(_comboPulseCo); _comboPulseCo = null; }
            if (_comboPunchCo != null) { StopCoroutine(_comboPunchCo); _comboPunchCo = null; }
            if (_comboRoot != null) _comboRoot.localScale = Vector3.one;
        }

        void StartComboPunch(float peak)
        {
            if (_comboPunchCo != null) StopCoroutine(_comboPunchCo);
            _comboPunchCo = StartCoroutine(ComboPunchCo(peak));
        }

        IEnumerator ComboPunchCo(float peak)
        {
            RectTransform rt = _comboRoot;
            float t = 0f, dur = 0.2f;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float s = 1f + (peak - 1f) * Mathf.Sin(t / dur * Mathf.PI);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
            _comboPunchCo = null;
        }

        IEnumerator ComboPulseCo() // gentle breathe while "on fire" (level 3)
        {
            RectTransform rt = _comboRoot;
            while (rt != null)
            {
                float s = 1.08f + 0.06f * Mathf.Sin(Time.time * (2f * Mathf.PI / 1.1f));
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        // ---------- animations ----------

        void Punch(RectTransform rt, float peak)
        {
            if (rt == null) return;
            StopCoroutine(nameof(PunchCo));
            _punchTarget = rt;
            _punchPeak = peak;
            StartCoroutine(nameof(PunchCo));
        }

        RectTransform _punchTarget;
        float _punchPeak;

        IEnumerator PunchCo()
        {
            RectTransform rt = _punchTarget;
            float t = 0f, dur = 0.2f;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float p = t / dur;
                float s = 1f + (_punchPeak - 1f) * Mathf.Sin(p * Mathf.PI); // up then back to 1
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        Vector3 _pendingPerfectWorld;

        IEnumerator PerfectPop()
        {
            _perfectLabel.gameObject.SetActive(true);
            float t = 0f, dur = 0.7f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                Vector3 sp = _cam.WorldToScreenPoint(_pendingPerfectWorld);
                _perfectRect.position = sp;
                float scale = p < 0.4f ? Mathf.Lerp(0f, 1.3f, p / 0.4f) : Mathf.Lerp(1.3f, 1.0f, (p - 0.4f) / 0.6f);
                _perfectRect.localScale = new Vector3(scale, scale, 1f);
                float a = p < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.5f) / 0.5f);
                _perfectLabel.color = new Color(Yellow.r, Yellow.g, Yellow.b, a);
                yield return null;
            }
            _perfectLabel.gameObject.SetActive(false);
        }

        IEnumerator SummitBeat()
        {
            if (_summitLabel == null) yield break;
            _summitLabel.text = Loc.T(LocKeys.HudSummit, SummitHeight);
            _summitLabel.gameObject.SetActive(true);
            FlashVignette(0.35f, 1.0f, Yellow); // gold flash so it can't be missed
            RectTransform rt = _summitLabel.rectTransform;
            float t = 0f, dur = 2.2f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                float scale = p < 0.18f ? Mathf.Lerp(0.4f, 1.2f, p / 0.18f)
                            : p < 0.32f ? Mathf.Lerp(1.2f, 1f, (p - 0.18f) / 0.14f)
                            : 1f;
                rt.localScale = new Vector3(scale, scale, 1f);
                float a = p < 0.72f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.72f) / 0.28f);
                _summitLabel.color = new Color(Yellow.r, Yellow.g, Yellow.b, a);
                yield return null;
            }
            _summitLabel.gameObject.SetActive(false);
        }

        void FlashVignette(float peakAlpha, float fade) => FlashVignette(peakAlpha, fade, new Color(1f, 0f, 0f));

        void FlashVignette(float peakAlpha, float fade, Color color)
        {
            if (_vignette == null) return;
            StopCoroutine(nameof(VignetteCo));
            _vigPeak = peakAlpha;
            _vigFade = fade;
            _vigColor = color;
            StartCoroutine(nameof(VignetteCo));
        }

        float _vigPeak, _vigFade;
        Color _vigColor = new Color(1f, 0f, 0f);

        IEnumerator VignetteCo()
        {
            _vignette.gameObject.SetActive(true);
            float t = 0f;
            while (t < _vigFade)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(_vigPeak, 0f, t / _vigFade);
                _vignette.color = new Color(_vigColor.r, _vigColor.g, _vigColor.b, a);
                yield return null;
            }
            _vignette.gameObject.SetActive(false);
        }

        IEnumerator ShowRestartAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            int finalHeight = _game != null ? _game.Floors : 0; // headline result = building height
            bool newBest = finalHeight > _highScore;
            if (newBest) _highScore = finalHeight;

            if (_restartScore != null) _restartScore.text = finalHeight.ToString();
            if (_restartBest != null)
            {
                _restartBest.text = newBest ? Loc.T(LocKeys.HudRecord) : Loc.T(LocKeys.HudBest, _highScore);
                _restartBest.color = newBest ? Mint : Navy;
            }
            // Coins banked this run + the running total — so the reward is visible (coins are otherwise
            // only shown inside the shop panels).
            if (_restartCoins != null)
            {
                MetaService meta = MetaService.Instance;
                if (meta != null)
                {
                    int earned = meta.PreviewCoins(_toppledResult); // floors×1 + perfects×2
                    string trophy = _toppledResult.TrophyRoofResidents > 0
                        ? Loc.T(LocKeys.HudTrophyLine, _toppledResult.TrophyRoofResidents)
                        : "";
                    _restartCoins.text = Loc.T(LocKeys.HudRunCoins, earned, _toppledResult.FloorCount,
                                               _toppledResult.PerfectDrops, meta.Coins) + trophy;
                }
                else _restartCoins.text = "";
            }
            if (_restartPanel != null) _restartPanel.SetActive(true);
        }

        // ---------- UI construction ----------

        void BuildUI()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            // Kept at scene root, NOT parented to this controller GameObject: TowerController lives on the
            // same GameObject and ClearTower() destroys ALL of its children every NewRun (and rotates it for
            // the sway) — which would wipe this HUD on a district switch / retry. A ScreenSpaceOverlay canvas
            // renders regardless of parent, so root is correct and safe.
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _vignette = NewImage("Vignette", canvasGo.transform, new Color(1f, 0f, 0f, 0f));
            Stretch(_vignette.rectTransform);
            _vignette.raycastTarget = false;
            _vignette.gameObject.SetActive(false);

            var safeGo = new GameObject("SafeRoot", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safeRoot = (RectTransform)safeGo.transform;
            Stretch(_safeRoot);
            safeGo.AddComponent<SafeAreaRoot>(); // insets to the device safe area each frame

            _scoreLabel = NewText("Score", _safeRoot, 72, FontStyles.Bold, TextAlignmentOptions.Top);
            Place(_scoreLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(700f, 100f));

            _coinIcon = NewImage("CoinIcon", _safeRoot, Yellow); // gold disc (generated sprite, font-independent)
            _coinIcon.sprite = CoinSprite();
            _coinIcon.raycastTarget = false;
            Place(_coinIcon.rectTransform, new Vector2(1f, 1f), new Vector2(-130f, -40f), new Vector2(46f, 46f)); // left of the ☰ menu button

            _coinsLabel = NewText("Coins", _safeRoot, 48, FontStyles.Bold, TextAlignmentOptions.TopRight);
            _coinsLabel.color = Yellow; // gold — the live coin tally
            Place(_coinsLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-188f, -36f), new Vector2(320f, 80f)); // shifted left for the ☰ menu button

            for (int i = 0; i < 2; i++)
            {
                _pips[i] = NewImage("Pip" + i, _safeRoot, PipEmpty);
                Place(_pips[i].rectTransform, new Vector2(0f, 0f), new Vector2(34f + i * 64f, 56f), new Vector2(52f, 52f));
            }

            BuildComboMeter(_safeRoot);

            _perfectLabel = NewText("Perfect", canvasGo.transform, 80, FontStyles.Bold, TextAlignmentOptions.Center);
            _perfectLabel.color = Yellow;
            _perfectLabel.gameObject.AddComponent<LocalizedLabel>().Bind(_perfectLabel, LocKeys.HudPerfect);
            _perfectRect = _perfectLabel.rectTransform;
            _perfectRect.sizeDelta = new Vector2(600f, 120f);
            _perfectLabel.gameObject.SetActive(false);

            _summitLabel = NewText("Summit", canvasGo.transform, 96, FontStyles.Bold, TextAlignmentOptions.Center);
            _summitLabel.color = Yellow;
            var srt = _summitLabel.rectTransform;
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0f, 180f);
            srt.sizeDelta = new Vector2(980f, 260f);
            _summitLabel.gameObject.SetActive(false);

            BuildRestartPanel(canvasGo.transform);
        }

        void BuildRestartPanel(Transform parent)
        {
            _restartPanel = new GameObject("RestartPanel", typeof(RectTransform));
            _restartPanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_restartPanel.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, 360f);
            prt.sizeDelta = new Vector2(700f, 520f);

            _restartScore = NewText("FinalScore", prt, 110, FontStyles.Bold, TextAlignmentOptions.Center);
            _restartScore.color = OffWhite;
            Place(_restartScore.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(700f, 160f));

            _restartBest = NewText("Best", prt, 40, FontStyles.Normal, TextAlignmentOptions.Center);
            Place(_restartBest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(700f, 60f));

            _restartCoins = NewText("Coins", prt, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            _restartCoins.color = Yellow;
            Place(_restartCoins.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Vector2(760f, 110f));

            var btnGo = new GameObject("RetryButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(prt, false);
            var brt = (RectTransform)btnGo.transform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 20f);
            brt.sizeDelta = new Vector2(480f, 110f);
            btnGo.GetComponent<Image>().color = Coral;
            btnGo.GetComponent<Button>().onClick.AddListener(OnRetry);

            var label = NewText("RetryLabel", brt, 42, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = OffWhite;
            label.gameObject.AddComponent<LocalizedLabel>().Bind(label, LocKeys.HudRetry);
            Stretch(label.rectTransform);

            _restartPanel.SetActive(false);
        }

        void OnRetry()
        {
            if (_restartPanel != null) _restartPanel.SetActive(false);
            if (_game != null) _game.NewRun();
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
