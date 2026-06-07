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
    public sealed partial class MetaHud
    {
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
