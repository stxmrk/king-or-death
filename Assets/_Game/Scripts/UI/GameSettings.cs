using UnityEngine;

namespace KingOrDeath
{
    // Player options persisted in PlayerPrefs, applied before the first scene loads.
    public static class GameSettings
    {
        public static string Language;
        public static FullScreenMode WindowMode;
        public static int Width, Height; // 0 = keep the launch resolution
        public static int Quality;
        public static bool VSync;
        public static int FpsLimit; // -1 = unlimited
        public static float MasterVolume,
            MusicVolume,
            SfxVolume,
            VoiceVolume;
        public static bool EdgeScroll,
            DamageNumbers,
            AlwaysHealth;
        public static float CameraSpeed;
        public static bool Hints;
        public static float UiScale; // 1 = 1600 x 900 reference layout

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load()
        {
            Language = PlayerPrefs.GetString(
                "kod.lang",
                Application.systemLanguage == SystemLanguage.Russian ? "ru" : "en"
            );
            WindowMode = (FullScreenMode)PlayerPrefs.GetInt("kod.window", (int)FullScreenMode.Windowed);
            Width = PlayerPrefs.GetInt("kod.width", 0);
            Height = PlayerPrefs.GetInt("kod.height", 0);
            Quality = Mathf.Clamp(
                PlayerPrefs.GetInt("kod.quality", QualitySettings.names.Length - 1),
                0,
                QualitySettings.names.Length - 1
            );
            VSync = PlayerPrefs.GetInt("kod.vsync", 1) == 1;
            FpsLimit = PlayerPrefs.GetInt("kod.fps", -1);
            MasterVolume = PlayerPrefs.GetFloat("kod.volume", 1);
            MusicVolume = PlayerPrefs.GetFloat("kod.music", .7f);
            SfxVolume = PlayerPrefs.GetFloat("kod.sfx", .9f);
            VoiceVolume = PlayerPrefs.GetFloat("kod.voice", 1);
            EdgeScroll = PlayerPrefs.GetInt("kod.edge", 1) == 1;
            DamageNumbers = PlayerPrefs.GetInt("kod.dmg", 1) == 1;
            AlwaysHealth = PlayerPrefs.GetInt("kod.hpbars", 0) == 1;
            CameraSpeed = PlayerPrefs.GetFloat("kod.camspeed", 1);
            Hints = PlayerPrefs.GetInt("kod.hints", 1) == 1;
            UiScale = PlayerPrefs.GetFloat("kod.uiscale", 1);
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetString("kod.lang", Language);
            PlayerPrefs.SetInt("kod.window", (int)WindowMode);
            PlayerPrefs.SetInt("kod.width", Width);
            PlayerPrefs.SetInt("kod.height", Height);
            PlayerPrefs.SetInt("kod.quality", Quality);
            PlayerPrefs.SetInt("kod.vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt("kod.fps", FpsLimit);
            PlayerPrefs.SetFloat("kod.volume", MasterVolume);
            PlayerPrefs.SetFloat("kod.music", MusicVolume);
            PlayerPrefs.SetFloat("kod.sfx", SfxVolume);
            PlayerPrefs.SetFloat("kod.voice", VoiceVolume);
            PlayerPrefs.SetInt("kod.edge", EdgeScroll ? 1 : 0);
            PlayerPrefs.SetInt("kod.dmg", DamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt("kod.hpbars", AlwaysHealth ? 1 : 0);
            PlayerPrefs.SetFloat("kod.camspeed", CameraSpeed);
            PlayerPrefs.SetInt("kod.hints", Hints ? 1 : 0);
            PlayerPrefs.SetFloat("kod.uiscale", UiScale);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            foreach (var k in new[] { "lang", "window", "width", "height", "quality", "vsync", "fps", "volume", "music", "sfx", "voice", "edge", "dmg", "hpbars", "camspeed", "hints", "uiscale" })
                PlayerPrefs.DeleteKey("kod." + k);
            Load();
        }

        public static void Apply()
        {
            // shortcut: every quality level shares one URP asset, so presets differ little; add per-level URP assets when tuning graphics.
            QualitySettings.SetQualityLevel(Quality, true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FpsLimit;
            AudioListener.volume = MasterVolume;
            UiFx.Fit();
            if (Application.isEditor)
                return;
            if (Width > 0 && Height > 0)
                Screen.SetResolution(Width, Height, WindowMode);
            else
                Screen.fullScreenMode = WindowMode;
        }
    }
}
