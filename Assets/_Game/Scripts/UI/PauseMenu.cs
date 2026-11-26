using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // In-match Esc menu. While open, the simulation is frozen and game input is ignored.
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu I { get; private set; }
        public static bool IsOpen { get; private set; }

        VisualElement root, overlay, panel, settingsScreen;
        Label subtitle;
        SettingsView settings;
        BuildingPlacement placement;
        float resumeScale = 1;

        void Awake() => I = this;

        void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            overlay = root.Q("pauseRoot");
            panel = root.Q("pausePanel");
            settingsScreen = root.Q("settingsScreen");
            subtitle = root.Q<Label>("pauseSubtitle");
            root.Q<Button>("resumeButton").clicked += Close;
            root.Q<Button>("saveButton").SetEnabled(false); // enabled once saves exist
            root.Q<Button>("pauseSettingsButton").clicked += () =>
            {
                panel.AddToClassList("hidden");
                settingsScreen.RemoveFromClassList("hidden");
                settings.Show();
            };
            root.Q<Button>("restartButton").clicked += () =>
            {
                IsOpen = false;
                GameSession.I.Restart();
            };
            root.Q<Button>("mainMenuButton").clicked += () =>
            {
                IsOpen = false;
                Time.timeScale = 1;
                SceneManager.LoadScene(MainMenu.MenuScene);
            };
            root.Q<Button>("pauseQuitButton").clicked += MainMenu.Quit;
            settings = new SettingsView(settingsScreen, ShowPanel);
            placement = FindAnyObjectByType<BuildingPlacement>();
            Loc.Apply(root);
            Loc.Changed += Relocalize;
            overlay.AddToClassList("hidden");
        }

        void OnDestroy()
        {
            IsOpen = false;
            Loc.Changed -= Relocalize;
        }

        void Relocalize()
        {
            Loc.Apply(root);
            settings.Refresh();
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;
            if (!IsOpen)
            {
                if (placement && placement.active)
                    placement.Cancel();
                else
                    Open();
            }
            else if (!settingsScreen.ClassListContains("hidden"))
                ShowPanel();
            else
                Close();
        }

        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;
            resumeScale = Time.timeScale;
            Time.timeScale = 0;
            int t = Mathf.FloorToInt(Time.timeSinceLevelLoad);
            subtitle.text = $"The First Frontier · {t / 60:00}:{t % 60:00}";
            overlay.RemoveFromClassList("hidden");
            ShowPanel();
        }

        public void Close()
        {
            IsOpen = false;
            Time.timeScale = resumeScale;
            overlay.AddToClassList("hidden");
        }

        void ShowPanel()
        {
            settingsScreen.AddToClassList("hidden");
            panel.RemoveFromClassList("hidden");
            root.Q<Button>("resumeButton").Focus();
        }
    }
}
