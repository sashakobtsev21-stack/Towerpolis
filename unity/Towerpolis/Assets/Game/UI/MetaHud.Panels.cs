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

            // Residents + height live on the gameplay HUD (HUDController, left column); the meta HUD only
            // owns the ☰ menu + its panels.
            MenuButton(safe); // ☰ top-right corner → opens the menu with every section
            BuildCityPanel(canvasGo.transform);
            BuildUpgradePanel(canvasGo.transform);
            BuildMissionPanel(canvasGo.transform);
            BuildSettingsPanel(canvasGo.transform);
            BuildMenuPanel(canvasGo.transform);
            BuildCompletePanel(canvasGo.transform);
            BuildPrestigePanel(canvasGo.transform);
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

        // ---------- prestige screen (endless-spec §2) ----------

        void BuildPrestigePanel(Transform parent)
        {
            _prestigePanel = new GameObject("PrestigePanel", typeof(RectTransform), typeof(Image));
            _prestigePanel.transform.SetParent(parent, false);
            var prt = (RectTransform)_prestigePanel.transform;
            Stretch(prt);
            _prestigePanel.GetComponent<Image>().color = Dim;
            PanelCard(prt);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(prt, false);
            _prestigeContent = (RectTransform)contentGo.transform;
            _prestigeContent.anchorMin = _prestigeContent.anchorMax = _prestigeContent.pivot = new Vector2(0.5f, 0.5f);
            _prestigeContent.anchoredPosition = Vector2.zero;
            _prestigeContent.sizeDelta = new Vector2(1000f, 1500f);

            var title = NewText("Title", _prestigeContent, 68, FontStyles.Bold, TextAlignmentOptions.Center);
            title.color = Gold;
            title.gameObject.AddComponent<LocalizedLabel>().Bind(title, LocKeys.PrestigeTitle);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 360f), new Vector2(960f, 110f));

            _prestigePop = NewText("Pop", _prestigeContent, 40, FontStyles.Normal, TextAlignmentOptions.Center);
            _prestigePop.color = OffWhite;
            Place(_prestigePop.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(960f, 56f));

            _prestigeStars = NewText("Stars", _prestigeContent, 48, FontStyles.Bold, TextAlignmentOptions.Center);
            _prestigeStars.color = Gold;
            Place(_prestigeStars.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(960f, 64f));

            _prestigeBonus = NewText("Bonus", _prestigeContent, 42, FontStyles.Bold, TextAlignmentOptions.Center);
            _prestigeBonus.color = Cyan;
            Place(_prestigeBonus.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(960f, 56f));

            _prestigeCoins = NewText("Kept", _prestigeContent, 34, FontStyles.Normal, TextAlignmentOptions.Center);
            _prestigeCoins.color = OffWhite;
            Place(_prestigeCoins.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(960f, 48f));

            Button go = MakeButton(prt, "PrestigeGo", new Vector2(0.5f, 0f), new Vector2(0f, 270f), new Vector2(460f, 110f), Gold, out _);
            go.onClick.AddListener(DoPrestigeTapped);
            var glbl = NewText("Label", go.transform, 42, FontStyles.Bold, TextAlignmentOptions.Center);
            glbl.color = Navy;
            glbl.gameObject.AddComponent<LocalizedLabel>().Bind(glbl, LocKeys.PrestigeButton);
            Stretch(glbl.rectTransform);

            Button close = MakeButton(prt, "PrestigeClose", new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(460f, 100f), Navy, out _);
            close.onClick.AddListener(() => HidePanel(_prestigePanel));
            var clbl = NewText("Label", close.transform, 38, FontStyles.Bold, TextAlignmentOptions.Center);
            clbl.color = OffWhite;
            clbl.gameObject.AddComponent<LocalizedLabel>().Bind(clbl, LocKeys.MetaClose);
            Stretch(clbl.rectTransform);

            _prestigePanel.SetActive(false);
        }

        void OnPrestigeReady() => ShowPrestige();

        // Populate from the live preview (population this cycle, stars to earn, new multiplier, coins kept) + show.
        void ShowPrestige()
        {
            if (_prestigePanel == null || _meta == null) return;
            if (_prestigePop != null) _prestigePop.text = Loc.T(LocKeys.PrestigePopLine, _meta.PrestigePopulation);
            if (_prestigeStars != null) _prestigeStars.text = Loc.T(LocKeys.PrestigeStarsLine, _meta.PrestigePreviewStars);
            if (_prestigeBonus != null)
                _prestigeBonus.text = Loc.T(LocKeys.PrestigeBonusLine,
                    _meta.PrestigePreviewMult.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            if (_prestigeCoins != null) _prestigeCoins.text = Loc.T(LocKeys.PrestigeKeptLine, _meta.PrestigeCoinsKept);
            ShowPanel(_prestigePanel);
        }

        void DoPrestigeTapped()
        {
            if (_meta != null) _meta.DoPrestige();
            HidePanel(_prestigePanel);
            if (_controller != null) _controller.NewRun(); // restart in the fresh (wiped) city
            if (_cityPanel != null && _cityPanel.activeSelf) PopulateCity();
        }
    }
}
