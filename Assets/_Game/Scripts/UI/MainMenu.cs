using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : MonoBehaviour
    {
        public const string MenuScene = "MainMenu";
        public string gameScene = "Prototype_01";

        VisualElement root, main, scenario, settingsScreen, about, loading, current, loadingFill;
        Label loadingText;
        Toggle hints;
        SettingsView settings;

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
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-verify") >= 0)
            {
                SceneManager.LoadScene(gameScene);
                return;
            }
            Time.timeScale = 1;
            root = GetComponent<UIDocument>().rootVisualElement;
            main = root.Q("mainScreen");
            scenario = root.Q("scenarioScreen");
            settingsScreen = root.Q("settingsScreen");
            about = root.Q("aboutScreen");
            loading = root.Q("loadingScreen");
            loadingFill = root.Q("loadingFill");
            loadingText = root.Q<Label>("loadingText");
            hints = root.Q<Toggle>("hintsToggle");

            root.Q<Button>("newGameButton").clicked += () => Show(scenario);
            root.Q<Button>("continueButton").SetEnabled(false); // enabled once saves exist
            root.Q<Button>("loadButton").SetEnabled(false);
            root.Q<Button>("settingsButton").clicked += () => Show(settingsScreen);
            root.Q<Button>("aboutButton").clicked += () => Show(about);
            root.Q<Button>("quitButton").clicked += Quit;
            root.Q<Button>("scenarioBack").clicked += () => Show(main);
            root.Q<Button>("aboutBack").clicked += () => Show(main);
            root.Q<Button>("startButton").clicked += () => StartCoroutine(LoadGame());
            hints.RegisterValueChangedCallback(e =>
            {
                GameSettings.Hints = e.newValue;
                GameSettings.Save();
            });
            root.Q<Label>("versionLabel").text = "v" + Application.version + " · The First Frontier";
            settings = new SettingsView(settingsScreen, () => Show(main));
            Loc.Apply(root);
            Loc.Changed += Relocalize;
            Show(main);
        }

        void OnDestroy() => Loc.Changed -= Relocalize;

        void Relocalize()
        {
            Loc.Apply(root);
            settings.Refresh();
        }

        void Show(VisualElement screen)
        {
            foreach (var s in new[] { main, scenario, settingsScreen, about, loading })
                s.EnableInClassList("hidden", s != screen);
            current = screen;
            if (screen == settingsScreen)
                settings.Show();
            else if (screen == scenario)
            {
                hints.SetValueWithoutNotify(GameSettings.Hints);
                root.Q<Button>("startButton").Focus();
            }
            else
                screen.Q<Button>()?.Focus();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && current != null && current != main && current != loading)
                Show(main);
        }

        IEnumerator LoadGame()
        {
            Show(loading);
            root.Q<Label>("loadingTip").text = Loc.T("tip." + UnityEngine.Random.Range(1, 5));
            var op = SceneManager.LoadSceneAsync(gameScene);
            while (!op.isDone)
            {
                float p = Mathf.Clamp01(op.progress / .9f);
                loadingFill.style.width = Length.Percent(p * 100); // dynamic value, not a style token
                loadingText.text = Loc.T("load.building") + "  " + Mathf.RoundToInt(p * 100) + "%";
                yield return null;
            }
        }
    }
}
