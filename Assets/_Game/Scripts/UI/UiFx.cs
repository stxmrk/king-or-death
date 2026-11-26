using UnityEngine;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // Shared decoration for UI Toolkit screens: gilded corners on ".ornate" panels and a fade-up entrance.
    public static class UiFx
    {
        static readonly string[] corners = { "corner--tl", "corner--tr", "corner--bl", "corner--br" };

        // Adds the four corner ornaments (Shell.uss ".corner-ornament") to every ".ornate" element under root, once.
        public static void Decorate(VisualElement root)
        {
            root.Query(className: "ornate")
                .ForEach(panel =>
                {
                    if (panel.Q(className: "corner-ornament") != null)
                        return;
                    foreach (var c in corners)
                    {
                        var e = new VisualElement { pickingMode = PickingMode.Ignore };
                        e.AddToClassList("corner-ornament");
                        e.AddToClassList(c);
                        panel.Add(e);
                    }
                });
        }

        static PanelSettings scaled;

        // Every document uses one runtime copy of the panel settings, so the scale option never edits the asset.
        public static void UsePanel(UIDocument doc)
        {
            if (!scaled)
            {
                scaled = Object.Instantiate(doc.panelSettings);
                scaled.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            if (doc.panelSettings != scaled)
                doc.panelSettings = scaled;
            Fit();
        }

        // Scale with height on 16:9 and wider screens, with width on narrower ones, so 1600 x 900 logical always fits;
        // the interface scale option shrinks the reference resolution.
        public static void Fit()
        {
            if (!scaled)
                return;
            float match = (float)Screen.width / Mathf.Max(1, Screen.height) >= 16f / 9 - .01f ? 1 : 0;
            var reference = Vector2Int.RoundToInt(new Vector2(1600, 900) / Mathf.Max(.5f, GameSettings.UiScale));
            if (scaled.match != match)
                scaled.match = match;
            if (scaled.referenceResolution != reference)
                scaled.referenceResolution = reference;
        }

        // Hardware cursor from Resources/Cursors (imported as Cursor textures): default, interact, attack; null resets.
        public static void SetCursor(string name)
        {
            var tex = name == null ? null : Resources.Load<Texture2D>("Cursors/" + name);
            var hotspot = name == "attack" ? new Vector2(16, 16) : new Vector2(2, 2);
            UnityEngine.Cursor.SetCursor(tex, tex ? hotspot : Vector2.zero, CursorMode.Auto);
        }

        // Shows an element with the ".enter" start state, then drops it next frame so the USS transition plays.
        public static void Reveal(VisualElement e)
        {
            e.RemoveFromClassList("hidden");
            e.AddToClassList("enter");
            e.schedule.Execute(() => e.RemoveFromClassList("enter")).StartingIn(20);
        }

        // Same entrance one element after another (Shell.uss ".stagger-item" carries the slower transition).
        public static void Stagger(System.Collections.Generic.IEnumerable<VisualElement> items, long startMs = 60, long stepMs = 55)
        {
            long delay = startMs;
            foreach (var e in items)
            {
                e.AddToClassList("stagger-item");
                e.AddToClassList("enter");
                var item = e;
                item.schedule.Execute(() => item.RemoveFromClassList("enter")).StartingIn(delay);
                delay += stepMs;
            }
        }

        // Full-screen black ".fader": FadeIn clears it after the first frame, FadeOut covers the screen, then acts.
        public static void FadeIn(VisualElement fader)
        {
            fader.AddToClassList("fader--on");
            fader.schedule.Execute(() => fader.RemoveFromClassList("fader--on")).StartingIn(60);
        }

        public static void FadeOut(VisualElement fader, System.Action then)
        {
            fader.AddToClassList("fader--on");
            fader.schedule.Execute(then).StartingIn(520);
        }
    }

    // Glowing embers drifting up over the main menu art, drawn with Painter2D at ~30 fps.
    [UxmlElement]
    public partial class EmberField : VisualElement
    {
        struct Ember
        {
            public Vector2 p,
                v;
            public float age,
                life,
                size,
                phase;
        }

        const int Count = 64;
        readonly Ember[] embers = new Ember[Count];
        readonly System.Random rng = new System.Random(7);
        float last = -1;

        public EmberField()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            schedule.Execute(Tick).Every(33);
        }

        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        Ember Spawn(Rect r, bool anywhere) =>
            new Ember
            {
                // rise from the lower right, where the keep and the bridge are in menu_bg
                p = new Vector2(R(r.width * .42f, r.width * 1.02f), anywhere ? R(0, r.height) : r.height + R(4, 40)),
                v = new Vector2(R(-34, -8), R(-58, -22)),
                life = R(6, 13),
                age = anywhere ? R(0, 6) : 0,
                size = R(1.5f, 3.8f),
                phase = R(0, 6.28f),
            };

        void Tick()
        {
            var r = contentRect;
            if (r.width < 1 || panel == null)
                return;
            float now = Time.unscaledTime;
            float dt = last < 0 ? 0 : Mathf.Clamp(now - last, 0, .1f);
            for (int i = 0; i < Count; i++)
            {
                if (last < 0)
                {
                    embers[i] = Spawn(r, true);
                    continue;
                }
                var e = embers[i];
                e.age += dt;
                e.p += (e.v + new Vector2(Mathf.Sin(e.age * 1.7f + e.phase) * 9, 0)) * dt;
                embers[i] = e.age > e.life || e.p.y < -8 ? Spawn(r, false) : e;
            }
            last = now;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (last < 0)
                return;
            var p = ctx.painter2D;
            foreach (var e in embers)
            {
                float fade = Mathf.Sin(Mathf.PI * Mathf.Clamp01(e.age / e.life));
                float flicker = .6f + .4f * Mathf.Sin(e.age * 9 + e.phase);
                float a = fade * flicker;
                HudDraw.Disc(p, e.p, e.size * 3.6f, new Color(1, .5f, .18f, a * .16f));
                HudDraw.Disc(p, e.p, e.size, new Color(1, .84f, .52f, a));
            }
        }
    }
}
