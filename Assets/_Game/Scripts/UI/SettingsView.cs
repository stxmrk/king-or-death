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
        static readonly float[] uiScales = { .85f, 1, 1.15f, 1.3f };

        readonly DropdownField window, resolution, quality, fps, language, uiScale;
        readonly Toggle vsync, hints, edge, damage, health;
        readonly Slider volume,
            musicVolume,
            sfxVolume,
            voiceVolume,
            cameraSpeed;
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
            uiScale = root.Q<DropdownField>("uiScaleField");
            vsync = root.Q<Toggle>("vsyncToggle");
            hints = root.Q<Toggle>("settingsHints");
            volume = root.Q<Slider>("volumeSlider");
            musicVolume = root.Q<Slider>("musicSlider");
            sfxVolume = root.Q<Slider>("sfxSlider");
            voiceVolume = root.Q<Slider>("voiceSlider");
            cameraSpeed = root.Q<Slider>("cameraSlider");
            edge = root.Q<Toggle>("edgeToggle");
            damage = root.Q<Toggle>("damageToggle");
            health = root.Q<Toggle>("healthToggle");
            // effects volume previews with a click so the level can be judged before Apply
            sfxVolume.RegisterValueChangedCallback(e => UiAudio.PlayAt("ui_click", e.newValue));
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
            uiScale.choices = uiScales.Select(v => Mathf.RoundToInt(v * 100) + "%").ToList();
            uiScale.index = Mathf.Max(0, Array.FindIndex(uiScales, v => Mathf.Approximately(v, GameSettings.UiScale)));
            vsync.value = GameSettings.VSync;
            hints.value = GameSettings.Hints;
            volume.value = GameSettings.MasterVolume;
            musicVolume.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfxVolume.SetValueWithoutNotify(GameSettings.SfxVolume);
            voiceVolume.SetValueWithoutNotify(GameSettings.VoiceVolume);
            cameraSpeed.SetValueWithoutNotify(GameSettings.CameraSpeed);
            edge.SetValueWithoutNotify(GameSettings.EdgeScroll);
            damage.SetValueWithoutNotify(GameSettings.DamageNumbers);
            health.SetValueWithoutNotify(GameSettings.AlwaysHealth);
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
            GameSettings.MusicVolume = musicVolume.value;
            GameSettings.SfxVolume = sfxVolume.value;
            GameSettings.VoiceVolume = voiceVolume.value;
            GameSettings.CameraSpeed = cameraSpeed.value;
            GameSettings.EdgeScroll = edge.value;
            GameSettings.DamageNumbers = damage.value;
            GameSettings.AlwaysHealth = health.value;
            GameSettings.Hints = hints.value;
            GameSettings.UiScale = uiScales[Mathf.Max(0, uiScale.index)];
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
