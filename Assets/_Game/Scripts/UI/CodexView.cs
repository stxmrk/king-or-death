using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // Logic for the main menu codex: a list of units and mechanics with a portrait card. Texts are Loc keys
    // codex.<id>, codex.<id>.meta, codex.<id>.body; icons are Shell.uss "icon-*" classes.
    public class CodexView
    {
        static readonly (string id, string icon)[] units =
        {
            ("swordsman", "unit_swordsman"),
            ("worker", "unit_worker"),
            ("caravan", "unit_wagon"),
            ("wolf", "unit_wolf"),
            ("ashen", "unit_ashen"),
        };

        static readonly (string id, string icon)[] mechanics =
        {
            ("capture", "unit_store"),
            ("territory", "cmd_build"),
            ("logistics", "cmd_route"),
            ("remains", "cmd_bury"),
        };

        readonly ScrollView list;
        readonly VisualElement art;
        readonly Label title,
            meta,
            body;
        readonly List<(string id, Button item)> items = new List<(string, Button)>();
        string current = "swordsman",
            currentIcon = "unit_swordsman";

        public CodexView(VisualElement root, Action back)
        {
            list = root.Q<ScrollView>("codexList");
            art = root.Q("codexArt");
            title = root.Q<Label>("codexTitle");
            meta = root.Q<Label>("codexMeta");
            body = root.Q<Label>("codexBody");
            root.Q<Button>("codexBack").clicked += back;
        }

        public void Show()
        {
            list.Clear();
            items.Clear();
            Section("codex.units", units);
            Section("codex.mech", mechanics);
            Select(current, currentIcon);
            items.Find(i => i.id == current).item?.Focus();
        }

        void Section(string caption, (string id, string icon)[] entries)
        {
            var label = new Label(Loc.T(caption));
            label.AddToClassList("caption");
            label.AddToClassList("codex-caption");
            list.Add(label);
            foreach (var (id, icon) in entries)
            {
                var item = new Button(() => Select(id, icon));
                item.AddToClassList("codex-item");
                var i = new VisualElement { pickingMode = PickingMode.Ignore };
                i.AddToClassList("codex-item-icon");
                i.AddToClassList("icon-" + icon);
                item.Add(i);
                item.Add(new Label(Loc.T("codex." + id)) { pickingMode = PickingMode.Ignore });
                list.Add(item);
                items.Add((id, item));
            }
        }

        void Select(string id, string icon)
        {
            art.RemoveFromClassList("icon-" + currentIcon);
            current = id;
            currentIcon = icon;
            art.AddToClassList("icon-" + icon);
            title.text = Loc.T("codex." + id);
            meta.text = Loc.T("codex." + id + ".meta");
            body.text = Loc.T("codex." + id + ".body");
            foreach (var (i, item) in items)
                item.EnableInClassList("codex-item--active", i == id);
        }
    }
}
