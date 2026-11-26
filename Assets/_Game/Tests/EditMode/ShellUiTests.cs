using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace KingOrDeath.Tests
{
    // Menus load from the real UXML: every key is translated in both languages and every element the controllers query exists.
    public class ShellUiTests
    {
        static readonly Regex untranslatedKey = new Regex(@"^[a-z]+(\.[a-z0-9]+)+$");
        string language;

        [SetUp]
        public void SetUp() => language = GameSettings.Language;

        [TearDown]
        public void TearDown() => GameSettings.Language = language;

        static VisualElement Load(string uxml) =>
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Game/UI/" + uxml).CloneTree();

        [TestCase("MainMenu.uxml", "newGameButton", "continueButton", "loadButton", "settingsButton", "aboutButton", "quitButton", "scenarioBack", "aboutBack", "startButton", "hintsToggle", "versionLabel", "loadingFill", "loadingText", "loadingTip", "loadScreen", "confirmScreen", "continueMeta", "continueCard", "continueThumb", "continueCardMeta", "fader", "codexButton", "codexScreen", "codexList", "codexArt", "codexTitle", "codexMeta", "codexBody", "codexBack")]
        [TestCase("PauseMenu.uxml", "pauseRoot", "pausePanel", "pauseSubtitle", "resumeButton", "saveButton", "pauseLoadButton", "pauseSettingsButton", "restartButton", "mainMenuButton", "pauseQuitButton", "resultsScreen", "resultsTitle", "resultsSubtitle", "resultsStats", "resultsContinue", "resultsLoad", "resultsRestart", "resultsMenu", "resultsArt", "slotsScreen", "confirmScreen", "fader", "briefing", "briefingText", "briefingStart")]
        [TestCase("SaveSlots.uxml", "slotsTitle", "slotsBack", "newSaveButton", "slotList", "slotsEmpty")]
        [TestCase("Confirm.uxml", "confirmTitle", "confirmText", "confirmYes", "confirmNo")]
        [TestCase("Hud.uxml", "hudRoot", "worldLayer", "minimap", "storeTabs", "woodValue", "stoneValue", "foodValue", "armyValue", "workerValue", "clockLabel", "pauseButton", "speed1", "speed2", "speed3", "objectivesButton", "helpButton", "menuButton", "eventLog", "helpPanel", "objectivesPanel", "objectiveCount", "objectiveFill", "objectiveList", "packLabel", "scentFill", "scentMark", "scentLabel", "hostLabel", "hostDetail", "placementBanner", "placementText", "pausedBanner", "tooltip", "tooltipTitle", "tooltipKey", "tooltipCost", "tooltipText", "singlePanel", "portrait", "selectionTitle", "selectionMeta", "healthBar", "healthFill", "healthText", "selectionState", "selectionDetail", "storeStock", "storeWood", "storeStone", "storeFood", "multiPanel", "multiTitle", "unitGrid", "idlePanel", "cardTitle", "commandCard", "caravanState", "caravanRoute", "caravanControls", "cargoFill", "routePineButton", "routeEastButton", "routeToggleButton", "cargoButton", "reverseButton", "selectCaravanButton", "dragRect", "announce", "announceIcon", "announceTitle", "announceText", "hoverTip", "hoverTitle", "hoverMeta", "hoverBar", "hoverFill", "nextObjective", "nextHint", "uiTip", "uiTipText", "quickBar", "quickIdle", "quickIdleCount", "quickArmy", "quickArmyCount", "quickCaravan", "groupBar")]
        public void UxmlHasEveryNamedElement(string uxml, params string[] names)
        {
            var root = Load(uxml);
            foreach (var n in names)
                Assert.IsNotNull(root.Q(n), uxml + " lacks " + n);
        }

        [TestCase("MainMenu.uxml")]
        [TestCase("PauseMenu.uxml")]
        public void SettingsViewBinds(string uxml)
        {
            var root = Load(uxml);
            Assert.IsNotNull(root.Q("settingsScreen").Q<DropdownField>("languageField"));
            Assert.DoesNotThrow(() => new SettingsView(root.Q("settingsScreen"), () => { }));
        }

        [TestCase("en")]
        [TestCase("ru")]
        public void CodexEntriesAreTranslated(string lang)
        {
            GameSettings.Language = lang;
            var root = Load("MainMenu.uxml");
            Loc.Apply(root);
            new CodexView(root.Q("codexScreen"), () => { }).Show();
            root.Q("codexScreen").Query<TextElement>().ForEach(te => Assert.IsFalse(untranslatedKey.IsMatch(te.text ?? ""), "untranslated " + te.text));
        }

        [TestCase("Hud.uxml")]
        [TestCase("MainMenu.uxml")]
        public void EveryTooltipIsTranslated(string uxml)
        {
            int count = 0;
            Load(uxml).Query<VisualElement>().ForEach(e =>
            {
                if (string.IsNullOrEmpty(e.tooltip))
                    return;
                count++;
                Assert.IsTrue(Loc.Has(e.tooltip), uxml + " tooltip " + e.tooltip);
            });
            if (uxml == "Hud.uxml")
                Assert.Greater(count, 15);
        }

        [Test]
        public void EveryAudioClipExists()
        {
            foreach (var n in new[] { "ui_hover", "ui_click", "ui_tab", "ui_back", "ui_confirm", "ui_error", "ui_open", "ui_alert", "sting_raid", "sting_wolves", "sting_capture", "sting_lost", "sting_delivery", "sting_wagon", "sting_build", "sting_victory", "sting_defeat", "amb_frontier", "music_menu", "music_match" })
                Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.AudioClip>("Audio/" + n), n);
            // every voice line in both languages (UiAudio.Voice appends the language)
            foreach (var line in new[] { "briefing", "raid", "wolves", "captured", "lost", "delivery", "wagon", "attack", "victory", "defeat" })
                foreach (var lang in new[] { "en", "ru" })
                    Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.AudioClip>("Audio/vo_" + line + "_" + lang), line + " " + lang);
        }

        [Test]
        public void EveryWorkerAndCaravanStateIsAKey()
        {
            foreach (var k in new[] { "ws.Idle", "ws.ToResource", "ws.Bury", "ws.Burn", "ws.Hide", "ws.Carry", "ws.NoStore", "ws.Delivering", "ws.Gathering", "ws.Exhausted", "ts.NoRoute", "ts.ToSource", "ts.Stopped", "ts.Suspended", "ts.Blocked", "ts.Delivering", "ts.Returning", "ts.Waiting" })
                Assert.IsTrue(Loc.Has(k), k);
            foreach (var o in GameSession.Objectives)
                Assert.IsTrue(Loc.Has("obj." + o + ".hint"), o);
        }

        [TestCase("MainMenu.uxml")]
        [TestCase("PauseMenu.uxml")]
        [TestCase("Hud.uxml")]
        [TestCase("SaveSlots.uxml")]
        [TestCase("Confirm.uxml")]
        public void EveryKeyTranslatesAndSwitchesLanguage(string uxml)
        {
            var root = Load(uxml);
            GameSettings.Language = "en";
            Loc.Apply(root);
            root.Query<TextElement>().ForEach(te => Assert.IsFalse(untranslatedKey.IsMatch(te.text ?? ""), "untranslated " + te.text));
            var keyed = root.Query<TextElement>().Where(te => te.userData is string).First();
            string english = keyed.text;
            GameSettings.Language = "ru";
            Loc.Apply(root);
            root.Query<TextElement>().ForEach(te => Assert.IsFalse(untranslatedKey.IsMatch(te.text ?? ""), "untranslated " + te.text));
            Assert.AreNotEqual(english, keyed.text);
        }
    }
}
