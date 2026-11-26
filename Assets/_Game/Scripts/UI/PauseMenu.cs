using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // In-match Esc menu, save/load screens and the end-of-match results. While open, the simulation is frozen and
    // game input is ignored. F5 quicksaves, F9 loads the quicksave.
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu I { get; private set; }
        public static bool IsOpen { get; private set; }

        VisualElement root, overlay, panel, settingsScreen, slotsScreen, results, resultsStats, resultsArt, fader;
        Label subtitle, resultsTitle, resultsSubtitle;
        Button saveButton, resultsContinue;
        SettingsView settings;
        SaveSlotsView slots;
        ConfirmDialog confirm;
        BriefingView briefing;
        int briefingEndFrame = -1;
        BuildingPlacement placement;
        float resumeScale = 1;
        bool resultsDismissed;

        void Awake() => I = this;

        void Start()
        {
            UiFx.UsePanel(GetComponent<UIDocument>());
            root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore; // full-screen container must not swallow world clicks
            overlay = root.Q("pauseRoot");
            panel = root.Q("pausePanel");
            settingsScreen = root.Q("settingsScreen");
            slotsScreen = root.Q("slotsScreen");
            results = root.Q("resultsScreen");
            resultsStats = root.Q("resultsStats");
            resultsArt = root.Q("resultsArt");
            fader = root.Q("fader");
            subtitle = root.Q<Label>("pauseSubtitle");
            resultsTitle = root.Q<Label>("resultsTitle");
            resultsSubtitle = root.Q<Label>("resultsSubtitle");
            saveButton = root.Q<Button>("saveButton");
            resultsContinue = root.Q<Button>("resultsContinue");
            confirm = new ConfirmDialog(root.Q("confirmScreen"));
            settings = new SettingsView(settingsScreen, ShowHome);
            slots = new SaveSlotsView(slotsScreen, ShowHome, confirm)
            {
                onSave = id =>
                {
                    if (GameSession.I.TrySave(id, SaveSlotsView.Kind(id)))
                        ShowHome();
                },
                onLoad = data => confirm.Ask("confirm.load.title", "confirm.lose", "menu.load", () => LoadMatch(data)),
            };

            root.Q<Button>("resumeButton").clicked += Close;
            saveButton.clicked += () => ShowSlots(true);
            root.Q<Button>("pauseLoadButton").clicked += () => ShowSlots(false);
            root.Q<Button>("pauseSettingsButton").clicked += () =>
            {
                Only(settingsScreen);
                settings.Show();
            };
            root.Q<Button>("restartButton").clicked += () =>
                confirm.Ask("confirm.restart.title", "confirm.lose", "pause.restart", Restart);
            root.Q<Button>("mainMenuButton").clicked += () =>
                confirm.Ask("confirm.menu.title", "confirm.lose", "pause.mainmenu", ToMainMenu);
            root.Q<Button>("pauseQuitButton").clicked += () =>
                confirm.Ask("confirm.quit.title", "confirm.lose", "pause.quit", () => Leave(MainMenu.Quit));
            resultsContinue.clicked += () =>
            {
                resultsDismissed = true;
                Close();
            };
            root.Q<Button>("resultsLoad").clicked += () => ShowSlots(false);
            root.Q<Button>("resultsRestart").clicked += Restart;
            root.Q<Button>("resultsMenu").clicked += ToMainMenu;
            placement = FindAnyObjectByType<BuildingPlacement>();
            UiFx.Decorate(root);
            UiAudio.Wire(root);
            UiFx.FadeIn(fader);
            Loc.Apply(root);
            Loc.Changed += Relocalize;
            GameSession.Ended += ShowResults;
            overlay.AddToClassList("hidden");
            briefing = new BriefingView(root.Q("briefing"), EndBriefing);
            if (!RuntimeVerification.Active && GameSession.I.FreshStart)
                StartBriefing();
        }

        // The match waits, paused and with the HUD hidden, until the player takes command.
        void StartBriefing()
        {
            IsOpen = true;
            resumeScale = 1;
            Time.timeScale = 0;
            briefing.Show();
        }

        // Released a frame later, so the key that closed the briefing does not also reach the game (Space = pause).
        void EndBriefing() => briefingEndFrame = Time.frameCount;

        void OnDestroy()
        {
            IsOpen = false;
            Loc.Changed -= Relocalize;
            GameSession.Ended -= ShowResults;
        }

        void Relocalize()
        {
            Loc.Apply(root);
            settings.Refresh();
            if (!slotsScreen.ClassListContains("hidden"))
                slots.Refresh();
            if (!results.ClassListContains("hidden"))
                FillResults(GameSession.I.outcome);
        }

        static bool Shown(VisualElement e) => !e.ClassListContains("hidden");

        bool InResults => GameSession.I && GameSession.I.outcome != Outcome.Playing && !resultsDismissed;

        void Update()
        {
            if (briefingEndFrame >= 0 && Time.frameCount > briefingEndFrame)
            {
                briefingEndFrame = -1;
                IsOpen = false;
                Time.timeScale = resumeScale;
                return;
            }
            if (briefing.Active)
            {
                briefing.Update();
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
                {
                    UiAudio.Play("ui_confirm");
                    briefing.Finish();
                }
                return;
            }
            if (!IsOpen && !placement.active)
            {
                if (Input.GetKeyDown(KeyCode.F5))
                    GameSession.I.TrySave(SaveSystem.Quick, SaveSlotsView.Kind(SaveSystem.Quick));
                if (Input.GetKeyDown(KeyCode.F9))
                {
                    var quick = SaveSystem.List().Find(s => s.id == SaveSystem.Quick);
                    if (quick != null)
                        LoadMatch(quick.data);
                    else
                        GameSession.I.Notify(Loc.T("hud.n.noquick"));
                }
            }
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;
            if (!IsOpen)
            {
                if (placement && placement.active)
                    placement.Cancel();
                else
                    Open();
            }
            else if (confirm.IsOpen)
            {
                UiAudio.Play("ui_back");
                confirm.Close();
            }
            else if (Shown(settingsScreen) || Shown(slotsScreen))
            {
                UiAudio.Play("ui_back");
                ShowHome();
            }
            else if (!InResults)
            {
                UiAudio.Play("ui_back");
                Close();
            }
        }

        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;
            UiAudio.Play("ui_open");
            resumeScale = Time.timeScale;
            Time.timeScale = 0;
            subtitle.text = Loc.T("scn.frontier") + " · " + Loc.Clock(GameSession.I.playTime);
            overlay.RemoveFromClassList("hidden");
            ShowHome();
        }

        public void Close()
        {
            IsOpen = false;
            Time.timeScale = resumeScale;
            overlay.AddToClassList("hidden");
            confirm.Close();
        }

        void Only(VisualElement screen)
        {
            foreach (var s in new[] { panel, settingsScreen, slotsScreen, results })
                if (s != screen)
                    s.AddToClassList("hidden");
            if (screen.ClassListContains("hidden"))
                UiFx.Reveal(screen);
        }

        // The pause panel, or the results while a finished match has not been dismissed.
        void ShowHome()
        {
            if (InResults)
            {
                Only(results);
                (Shown(resultsContinue) ? resultsContinue : root.Q<Button>("resultsMenu")).Focus();
                return;
            }
            Only(panel);
            saveButton.SetEnabled(GameSession.I.outcome != Outcome.Defeat);
            root.Q<Button>("resumeButton").Focus();
        }

        void ShowSlots(bool save)
        {
            Only(slotsScreen);
            slots.Show(save);
        }

        void ShowResults(Outcome outcome)
        {
            resultsDismissed = false;
            IsOpen = true;
            resumeScale = GameSession.I.speed;
            overlay.RemoveFromClassList("hidden");
            FillResults(outcome);
            UiAudio.Music(null);
            UiAudio.Play(outcome == Outcome.Victory ? "sting_victory" : "sting_defeat");
            UiAudio.Voice(outcome == Outcome.Victory ? "vo_victory" : "vo_defeat", 2.5f);
            ShowHome();
        }

        void FillResults(Outcome outcome)
        {
            var g = GameSession.I;
            bool won = outcome == Outcome.Victory;
            resultsTitle.text = Loc.T(won ? "results.victory" : "results.defeat");
            resultsTitle.EnableInClassList("results-title--defeat", !won);
            resultsArt.EnableInClassList("results-art--defeat", !won);
            resultsSubtitle.text = Loc.T(won ? "results.victory.text" : "results.defeat.text");
            resultsContinue.EnableInClassList("hidden", !won);
            resultsStats.Clear();
            var s = g.stats;
            Stat("results.time", Loc.Clock(g.playTime));
            Stat("results.objectives", g.milestones.Count + " / " + GameSession.Objectives.Length);
            Stat("results.slain", s.enemiesSlain.ToString());
            Stat("results.lost", s.unitsLost.ToString());
            Stat("results.recruited", s.recruited.ToString());
            Stat("results.harvested", s.harvested.ToString());
            Stat("results.delivered", s.delivered.ToString());
            Stat("results.built", s.built.ToString());
            Stat("results.cleared", s.remainsCleared.ToString());
        }

        void Stat(string key, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("results-row");
            var name = new Label(Loc.T(key));
            name.AddToClassList("results-name");
            var v = new Label(value);
            v.AddToClassList("results-value");
            row.Add(name);
            row.Add(v);
            resultsStats.Add(row);
        }

        // Covers the screen in black, then leaves; the menu stays open meanwhile so no game input slips through.
        void Leave(System.Action go)
        {
            IsOpen = true;
            UiFx.FadeOut(
                fader,
                () =>
                {
                    IsOpen = false;
                    go();
                }
            );
        }

        void Restart() => Leave(() => GameSession.I.Restart());

        void ToMainMenu() =>
            Leave(() =>
            {
                Time.timeScale = 1;
                SceneManager.LoadScene(MainMenu.MenuScene);
            });

        void LoadMatch(SaveData data) => Leave(() => SaveSystem.Load(data));
    }
}
