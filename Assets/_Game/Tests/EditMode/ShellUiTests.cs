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

        [TestCase("MainMenu.uxml", "newGameButton", "continueButton", "loadButton", "settingsButton", "aboutButton", "quitButton", "scenarioBack", "aboutBack", "startButton", "hintsToggle", "versionLabel", "loadingFill", "loadingText", "loadingTip")]
        [TestCase("PauseMenu.uxml", "pauseRoot", "pausePanel", "pauseSubtitle", "resumeButton", "saveButton", "pauseSettingsButton", "restartButton", "mainMenuButton", "pauseQuitButton")]
        public void UxmlHasEveryNamedElementAndSettings(string uxml, params string[] names)
        {
            var root = Load(uxml);
            foreach (var n in names)
                Assert.IsNotNull(root.Q(n), uxml + " lacks " + n);
            Assert.IsNotNull(root.Q("settingsScreen").Q<DropdownField>("languageField"));
            Assert.DoesNotThrow(() => new SettingsView(root.Q("settingsScreen"), () => { }));
        }

        [TestCase("MainMenu.uxml")]
        [TestCase("PauseMenu.uxml")]
        public void EveryKeyTranslatesAndSwitchesLanguage(string uxml)
        {
            var root = Load(uxml);
            GameSettings.Language = "en";
            Loc.Apply(root);
            root.Query<TextElement>().ForEach(te => Assert.IsFalse(untranslatedKey.IsMatch(te.text ?? ""), "untranslated " + te.text));
            string englishTitle = root.Q<Button>().text;
            GameSettings.Language = "ru";
            Loc.Apply(root);
            root.Query<TextElement>().ForEach(te => Assert.IsFalse(untranslatedKey.IsMatch(te.text ?? ""), "untranslated " + te.text));
            Assert.AreNotEqual(englishTitle, root.Q<Button>().text);
        }
    }
}
