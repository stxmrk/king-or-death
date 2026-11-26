using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // Logic for Settings.uxml; shared by the main menu and the pause menu.
    public class SettingsView
    {
        static readonly FullScreenMode[] modes =
        {
            FullScreenMode.Windowed,
            FullScreenMode.FullScreenWindow,
            FullScreenMode.ExclusiveFullScreen,
        };
        static readonly string[] modeKeys = { "win.windowed", "win.borderless", "win.fullscreen" };
        static readonly int[] fpsValues = { 30, 60, 144, -1 };
        static readonly string[] languages = { "en", "ru" };

        readonly DropdownField window, resolution, quality, fps, language;
        readonly Toggle vsync, hints;
        readonly Slider volume;
        readonly List<Vector2Int> sizes;
        readonly Button[] tabs;
        readonly VisualElement[] pages;

        public SettingsView(VisualElement root, Action back)
        {
            window = root.Q<DropdownField>("windowField");
            resolution = root.Q<DropdownField>("resolutionField");
            quality = root.Q<DropdownField>("qualityField");
            fps = root.Q<DropdownField>("fpsField");
            language = root.Q<DropdownField>("languageField");
            vsync = root.Q<Toggle>("vsyncToggle");
            hints = root.Q<Toggle>("settingsHints");
            volume = root.Q<Slider>("volumeSlider");
            sizes = Screen
                .resolutions.Select(r => new Vector2Int(r.width, r.height))
                .Append(new Vector2Int(Screen.width, Screen.height))
                .Distinct()
                .OrderBy(s => s.x * 10000 + s.y)
                .ToList();
            tabs = new[] { "tabVideo", "tabAudio", "tabControls", "tabGame" }.Select(n => root.Q<Button>(n)).ToArray();
            pages = new[] { "pageVideo", "pageAudio", "pageControls", "pageGame" }.Select(n => root.Q(n)).ToArray();
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].clicked += () => ShowTab(index);
            }
            root.Q<Button>("settingsBack").clicked += back;
            root.Q<Button>("applyButton").clicked += Apply;
            root.Q<Button>("resetButton").clicked += () =>
            {
                string before = GameSettings.Language;
                GameSettings.Reset();
                Changed(before);
            };
            ShowTab(0);
            Refresh();
        }

        void ShowTab(int index)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].EnableInClassList("tab--active", i == index);
                pages[i].EnableInClassList("hidden", i != index);
            }
        }

        public void Show()
        {
            Refresh();
            tabs[0].Focus();
        }

        // Fills every control from GameSettings in the current language.
        public void Refresh()
        {
            window.choices = modeKeys.Select(k => Loc.T(k)).ToList();
            window.index = Mathf.Max(0, Array.IndexOf(modes, GameSettings.WindowMode));
            resolution.choices = sizes.Select(s => s.x + " × " + s.y).ToList();
            var current = GameSettings.Width > 0
                ? new Vector2Int(GameSettings.Width, GameSettings.Height)
                : new Vector2Int(Screen.width, Screen.height);
            resolution.index = Mathf.Max(0, sizes.IndexOf(current));
            quality.choices = QualitySettings.names.Select(n => Loc.T("q." + n, n)).ToList();
            quality.index = GameSettings.Quality;
            fps.choices = fpsValues.Select(v => v < 0 ? Loc.T("fps.none") : v.ToString()).ToList();
            fps.index = Mathf.Max(0, Array.IndexOf(fpsValues, GameSettings.FpsLimit));
            language.choices = new List<string> { "English", "Русский" };
            language.index = Array.IndexOf(languages, GameSettings.Language);
            vsync.value = GameSettings.VSync;
            hints.value = GameSettings.Hints;
            volume.value = GameSettings.MasterVolume;
        }

        void Apply()
        {
            string before = GameSettings.Language;
            GameSettings.WindowMode = modes[window.index];
            GameSettings.Width = sizes[resolution.index].x;
            GameSettings.Height = sizes[resolution.index].y;
            GameSettings.Quality = quality.index;
            GameSettings.VSync = vsync.value;
            GameSettings.FpsLimit = fpsValues[fps.index];
            GameSettings.MasterVolume = volume.value;
            GameSettings.Hints = hints.value;
            GameSettings.Language = languages[Mathf.Max(0, language.index)];
            GameSettings.Save();
            GameSettings.Apply();
            Changed(before);
        }

        void Changed(string languageBefore)
        {
            string now = GameSettings.Language;
            GameSettings.Language = languageBefore;
            Loc.SetLanguage(now); // raises Loc.Changed, whose listeners re-translate and call Refresh
            Refresh();
        }
    }
}
