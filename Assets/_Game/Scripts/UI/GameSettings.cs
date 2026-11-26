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
        public static float MasterVolume;
        public static bool Hints;

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
            Hints = PlayerPrefs.GetInt("kod.hints", 1) == 1;
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
            PlayerPrefs.SetInt("kod.hints", Hints ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            foreach (var k in new[] { "lang", "window", "width", "height", "quality", "vsync", "fps", "volume", "hints" })
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
            if (Application.isEditor)
                return;
            if (Width > 0 && Height > 0)
                Screen.SetResolution(Width, Height, WindowMode);
            else
                Screen.fullScreenMode = WindowMode;
        }
    }
}
