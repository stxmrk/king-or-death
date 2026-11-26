using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // Mission briefing over the paused match: scenario art, the narrator reading brief.text, the text typed in step
    // with the voice. The not-yet-typed tail stays in the label as transparent rich text, so lines never re-wrap.
    public class BriefingView
    {
        const string Voice = "vo_briefing";
        const float VoiceDelay = .9f;
        readonly VisualElement root,
            art;
        readonly Label text;
        readonly Action begin;
        string full;
        float started,
            duration;

        public bool Active { get; private set; }

        public BriefingView(VisualElement root, Action begin)
        {
            this.root = root;
            this.begin = begin;
            art = root.Q(className: "briefing-art");
            text = root.Q<Label>("briefingText");
            text.enableRichText = true;
            root.Q<Button>("briefingStart").clicked += Finish;
            root.AddToClassList("hidden");
        }

        public void Show()
        {
            Active = true;
            full = Loc.T("brief.text");
            float voice = UiAudio.VoiceLength(Voice);
            duration = voice > 0 ? voice * .97f : full.Length * .045f;
            started = Time.unscaledTime + VoiceDelay;
            UiAudio.Voice(Voice, VoiceDelay);
            UiFx.Reveal(root);
            art.schedule.Execute(() => art.AddToClassList("art--drift")).StartingIn(100);
            Update();
            root.Q<Button>("briefingStart").Focus();
        }

        public void Update()
        {
            if (!Active)
                return;
            float t = Mathf.Clamp01((Time.unscaledTime - started) / duration);
            int n = Mathf.RoundToInt(full.Length * t);
            text.text = n >= full.Length ? full : full.Substring(0, n) + "<alpha=#00>" + full.Substring(n);
        }

        public void Finish()
        {
            if (!Active)
                return;
            Active = false;
            UiAudio.StopVoice();
            root.AddToClassList("hidden");
            begin();
        }
    }
}
