using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // Interface audio: one object that survives scene loads with an effects source, an ambience bed and two music
    // sources that crossfade. Clips are Resources/Audio/*.ogg made by tools/audio_gen.py; a missing clip is silent.
    public class UiAudio : MonoBehaviour
    {
        const float FadeSeconds = 2.5f;
        static UiAudio instance;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        AudioSource effects,
            ambience,
            voice;
        string queuedVoice;
        float queuedAt;
        readonly AudioSource[] music = new AudioSource[2];
        int active;
        float lastHover,
            ambienceTarget;
        VisualElement hovered;

        static UiAudio I
        {
            get
            {
                if (!instance)
                {
                    var go = new GameObject("UI audio");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<UiAudio>();
                }
                return instance;
            }
        }

        void Awake()
        {
            effects = Source(false);
            ambience = Source(true);
            voice = Source(false);
            music[0] = Source(true);
            music[1] = Source(true);
        }

        AudioSource Source(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0;
            s.ignoreListenerPause = true;
            s.volume = 0;
            return s;
        }

        static AudioClip Clip(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (!clips.TryGetValue(name, out var clip))
                clips[name] = clip = Resources.Load<AudioClip>("Audio/" + name);
            return clip;
        }

        public static void Play(string name, float volume = 1)
        {
            var clip = Clip(name);
            if (clip)
                I.effects.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }

        // Plays at an absolute effects level, ignoring the saved setting (settings preview).
        public static void PlayAt(string name, float level)
        {
            var clip = Clip(name);
            if (clip)
                I.effects.PlayOneShot(clip, level);
        }

        // Narrator / advisor line in the current language: Resources/Audio/<name>_<lang>.ogg. A newer line replaces a
        // queued one; delay lets a stinger land first. Music ducks while the voice speaks.
        public static void Voice(string name, float delay = 0)
        {
            var self = I;
            self.queuedVoice = name;
            self.queuedAt = Time.unscaledTime + delay;
        }

        public static void StopVoice()
        {
            var self = I;
            self.queuedVoice = null;
            self.voice.Stop();
        }

        // Seconds of a voice line in the current language, 0 when missing (typewriter pacing).
        public static float VoiceLength(string name)
        {
            var clip = Clip(name + "_" + GameSettings.Language);
            return clip ? clip.length : 0;
        }

        public static bool Speaking => instance && instance.voice.isPlaying;

        // Crossfades to a looping track; null fades the music out.
        public static void Music(string name)
        {
            var self = I;
            var clip = Clip(name);
            var current = self.music[self.active];
            if (clip && current.clip == clip && current.isPlaying)
                return;
            self.active = 1 - self.active;
            var next = self.music[self.active];
            next.clip = clip;
            next.volume = 0;
            if (clip)
                next.Play();
            else
                next.Stop();
        }

        public static void Ambience(string name)
        {
            var self = I;
            var clip = Clip(name);
            self.ambienceTarget = clip ? 1 : 0;
            if (clip && self.ambience.clip != clip)
            {
                self.ambience.clip = clip;
                self.ambience.volume = 0;
                self.ambience.Play();
            }
        }

        void Update()
        {
            float step = Time.unscaledDeltaTime / FadeSeconds;
            if (queuedVoice != null && Time.unscaledTime >= queuedAt)
            {
                var clip = Clip(queuedVoice + "_" + GameSettings.Language);
                queuedVoice = null;
                if (clip)
                {
                    voice.clip = clip;
                    voice.volume = GameSettings.VoiceVolume;
                    voice.Play();
                }
            }
            float duck = voice.isPlaying ? .4f : 1;
            for (int i = 0; i < 2; i++)
            {
                var s = music[i];
                float target = i == active && s.clip ? GameSettings.MusicVolume * .55f * duck : 0;
                s.volume = Mathf.MoveTowards(s.volume, target, step);
                if (i != active && s.isPlaying && s.volume <= 0)
                    s.Stop();
            }
            ambience.volume = Mathf.MoveTowards(ambience.volume, ambienceTarget * GameSettings.SfxVolume * .5f, step);
            if (ambienceTarget == 0 && ambience.isPlaying && ambience.volume <= 0)
                ambience.Stop();
        }

        static Button ButtonOf(object target)
        {
            for (var e = target as VisualElement; e != null; e = e.parent)
                if (e is Button b)
                    return b;
            return null;
        }

        // Hover and click sounds for every button under root, picked by the button's style class.
        public static void Wire(VisualElement root)
        {
            root.RegisterCallback<PointerEnterEvent>(
                e =>
                {
                    var b = ButtonOf(e.target);
                    var self = I;
                    if (b == null || !b.enabledInHierarchy || b == self.hovered || Time.unscaledTime - self.lastHover < .04f)
                        return;
                    self.hovered = b;
                    self.lastHover = Time.unscaledTime;
                    Play("ui_hover", .7f);
                },
                TrickleDown.TrickleDown
            );
            root.RegisterCallback<PointerLeaveEvent>(
                e =>
                {
                    if (ButtonOf(e.target) == I.hovered)
                        I.hovered = null;
                },
                TrickleDown.TrickleDown
            );
            root.RegisterCallback<ClickEvent>(
                e =>
                {
                    var b = ButtonOf(e.target);
                    if (b == null)
                        return;
                    Play(
                        b.ClassListContains("link-button") ? "ui_back"
                            : b.ClassListContains("tab") || b.ClassListContains("store-tab") || b.ClassListContains("speed-button") ? "ui_tab"
                            : b.ClassListContains("gold-button") ? "ui_confirm"
                            : "ui_click"
                    );
                },
                TrickleDown.TrickleDown
            );
        }
    }
}
