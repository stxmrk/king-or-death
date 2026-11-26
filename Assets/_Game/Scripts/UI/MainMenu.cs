using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : MonoBehaviour
    {
        public const string MenuScene = "MainMenu",
            GameScene = "Prototype_01";
        public string gameScene = GameScene;

        VisualElement root, main, scenario, settingsScreen, loadScreen, codexScreen, about, loading, current, loadingFill, fader;
        VisualElement continueCard, continueThumb;
        Texture2D thumbnail;
        Label loadingText, continueMeta, continueCardMeta;
        Toggle hints;
        Button continueButton, loadButton;
        SettingsView settings;
        SaveSlotsView slots;
        CodexView codex;
        ConfirmDialog confirm;

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Start()
        {
            if (RuntimeVerification.Active)
            {
                SceneManager.LoadScene(gameScene);
                return;
            }
            Time.timeScale = 1;
            UiFx.UsePanel(GetComponent<UIDocument>());
            root = GetComponent<UIDocument>().rootVisualElement;
            main = root.Q("mainScreen");
            scenario = root.Q("scenarioScreen");
            settingsScreen = root.Q("settingsScreen");
            loadScreen = root.Q("loadScreen");
            codexScreen = root.Q("codexScreen");
            codex = new CodexView(codexScreen, () => Show(main));
            about = root.Q("aboutScreen");
            loading = root.Q("loadingScreen");
            loadingFill = root.Q("loadingFill");
            loadingText = root.Q<Label>("loadingText");
            hints = root.Q<Toggle>("hintsToggle");
            continueButton = root.Q<Button>("continueButton");
            loadButton = root.Q<Button>("loadButton");
            continueMeta = root.Q<Label>("continueMeta");
            continueCard = root.Q("continueCard");
            continueThumb = root.Q("continueThumb");
            continueCardMeta = root.Q<Label>("continueCardMeta");
            fader = root.Q("fader");
            confirm = new ConfirmDialog(root.Q("confirmScreen"));
            slots = new SaveSlotsView(loadScreen, () => Show(main), confirm)
            {
                onLoad = data => StartCoroutine(LoadGame(data)),
            };

            root.Q<Button>("newGameButton").clicked += () => Show(scenario);
            System.Action resume = () =>
            {
                var latest = SaveSystem.Latest();
                if (latest != null)
                    StartCoroutine(LoadGame(latest.data));
            };
            continueButton.clicked += resume;
            ((Button)continueCard).clicked += resume;
            loadButton.clicked += () => Show(loadScreen);
            root.Q<Button>("codexButton").clicked += () => Show(codexScreen);
            root.Q<Button>("settingsButton").clicked += () => Show(settingsScreen);
            root.Q<Button>("aboutButton").clicked += () => Show(about);
            root.Q<Button>("quitButton").clicked += Quit;
            root.Q<Button>("scenarioBack").clicked += () => Show(main);
            root.Q<Button>("aboutBack").clicked += () => Show(main);
            root.Q<Button>("startButton").clicked += () => StartCoroutine(LoadGame(null));
            hints.RegisterValueChangedCallback(e =>
            {
                GameSettings.Hints = e.newValue;
                GameSettings.Save();
            });
            root.Q<Label>("versionLabel").text = "v" + Application.version + " · " + Loc.T("scn.frontier");
            settings = new SettingsView(settingsScreen, () => Show(main));
            UiFx.Decorate(root);
            UiFx.SetCursor("default");
            UiFx.FadeIn(fader);
            UiAudio.Wire(root);
            UiAudio.Music("music_menu");
            UiAudio.Ambience(null);
            // slow drift across the key art; the USS transition is as long as the interval
            var art = root.Q(className: "art");
            art.schedule.Execute(() => art.ToggleInClassList("art--drift")).StartingIn(100).Every(40000);
            Loc.Apply(root);
            Loc.Changed += Relocalize;
            Show(main);
        }

        void OnDestroy()
        {
            Loc.Changed -= Relocalize;
            if (thumbnail)
                Destroy(thumbnail);
        }

        void Relocalize()
        {
            Loc.Apply(root);
            settings.Refresh();
            root.Q<Label>("versionLabel").text = "v" + Application.version + " · " + Loc.T("scn.frontier");
            if (current == loadScreen)
                slots.Refresh();
            if (current == codexScreen)
                codex.Show();
        }

        void Show(VisualElement screen)
        {
            foreach (var s in new[] { main, scenario, settingsScreen, loadScreen, codexScreen, about, loading })
                if (s != screen)
                    s.AddToClassList("hidden");
            UiFx.Reveal(screen);
            current = screen;
            if (screen == settingsScreen)
                settings.Show();
            else if (screen == loadScreen)
                slots.Show(false);
            else if (screen == codexScreen)
                codex.Show();
            else if (screen == scenario)
            {
                hints.SetValueWithoutNotify(GameSettings.Hints);
                root.Q<Button>("startButton").Focus();
            }
            else
            {
                if (screen == main)
                {
                    var latest = SaveSystem.Latest();
                    continueButton.SetEnabled(latest != null);
                    loadButton.SetEnabled(latest != null);
                    continueMeta.text = latest == null
                        ? ""
                        : Loc.Clock(latest.data.playTime) + "  ·  " + latest.data.SavedAt.ToString("g", SaveSlotsView.Culture);
                    continueCard.EnableInClassList("hidden", latest == null);
                    if (latest != null)
                    {
                        continueCardMeta.text = SaveSlotsView.Kind(latest.id) + "\n" + continueMeta.text;
                        if (thumbnail)
                            Destroy(thumbnail);
                        thumbnail = SaveSystem.LoadThumbnail(latest.id);
                        continueThumb.style.backgroundImage = thumbnail ? new StyleBackground(thumbnail) : StyleKeyword.Null;
                    }
                    var column = main.Q(className: "main-column");
                    var items = new List<VisualElement>();
                    foreach (var child in column.Children())
                        if (!child.ClassListContains("menu-list"))
                            items.Add(child);
                    items.AddRange(column.Q(className: "menu-list").Children());
                    UiFx.Stagger(items);
                }
                screen.Q<Button>(className: "menu-button")?.Focus();
            }
        }

        void Update()
        {
            UiFx.Fit();
            if (root != null)
                root.EnableInClassList("menu--short", root.layout.height < 800); // large UI scale leaves ~700 px
            if (!Input.GetKeyDown(KeyCode.Escape) || current == null)
                return;
            if (confirm.IsOpen)
                confirm.Close();
            else if (current != main && current != loading)
                Show(main);
        }

        IEnumerator LoadGame(SaveData save)
        {
            SaveSystem.Pending = save;
            Show(loading);
            root.Q<Label>("loadingTip").text = Loc.T("tip." + UnityEngine.Random.Range(1, 7));
            var op = SceneManager.LoadSceneAsync(gameScene);
            op.allowSceneActivation = false; // held at 90% until the screen has faded to black
            while (op.progress < .9f)
            {
                float p = Mathf.Clamp01(op.progress / .9f);
                loadingFill.style.width = Length.Percent(p * 100);
                loadingText.text = Loc.T(save != null ? "load.restoring" : "load.building") + "  " + Mathf.RoundToInt(p * 100) + "%";
                yield return null;
            }
            loadingFill.style.width = Length.Percent(100);
            loadingText.text = Loc.T(save != null ? "load.restoring" : "load.building") + "  100%";
            yield return new WaitForSecondsRealtime(.35f);
            fader.AddToClassList("fader--on");
            yield return new WaitForSecondsRealtime(.55f);
            op.allowSceneActivation = true;
            while (!op.isDone)
            {
                float p = Mathf.Clamp01(op.progress / .9f);
                loadingFill.style.width = Length.Percent(p * 100); // dynamic value, not a style token
                loadingText.text = Loc.T(save != null ? "load.restoring" : "load.building") + "  " + Mathf.RoundToInt(p * 100) + "%";
                yield return null;
            }
        }
    }
}
