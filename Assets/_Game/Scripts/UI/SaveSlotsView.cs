using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // Logic for SaveSlots.uxml, shared by the main menu (load) and the pause menu (save and load).
    public class SaveSlotsView
    {
        readonly VisualElement list,
            empty;
        readonly Label title;
        readonly Button newSave,
            back;
        readonly ConfirmDialog confirm;
        bool saving;
        readonly List<Texture2D> thumbnails = new List<Texture2D>();

        public Action<string> onSave; // slot id to write
        public Action<SaveData> onLoad;

        public SaveSlotsView(VisualElement root, Action back, ConfirmDialog confirm)
        {
            this.confirm = confirm;
            list = root.Q("slotList");
            empty = root.Q("slotsEmpty");
            title = root.Q<Label>("slotsTitle");
            newSave = root.Q<Button>("newSaveButton");
            this.back = root.Q<Button>("slotsBack");
            this.back.clicked += back;
            newSave.clicked += () => onSave?.Invoke(SaveSystem.NewId());
        }

        public void Show(bool save)
        {
            saving = save;
            title.text = Loc.T(save ? "save.title" : "load.title");
            newSave.EnableInClassList("hidden", !save);
            Rebuild();
            if (save)
                newSave.Focus();
            else
                (list.Q<Button>() ?? back).Focus();
        }

        public void Refresh() => Show(saving);

        public static CultureInfo Culture => CultureInfo.GetCultureInfo(GameSettings.Language == "ru" ? "ru-RU" : "en-GB");

        public static string Kind(string id) =>
            Loc.T(id == SaveSystem.Auto ? "save.kind.auto" : id == SaveSystem.Quick ? "save.kind.quick" : "save.kind.manual");

        void Rebuild()
        {
            list.Clear();
            foreach (var t in thumbnails)
                if (t)
                    UnityEngine.Object.Destroy(t);
            thumbnails.Clear();
            var culture = Culture;
            int shown = 0;
            foreach (var slot in SaveSystem.List())
            {
                // manual saves never overwrite the rotating auto/quick slots
                if (saving && (slot.id == SaveSystem.Auto || slot.id == SaveSystem.Quick))
                    continue;
                shown++;
                var row = new VisualElement();
                row.AddToClassList("slot-row");
                var thumb = new VisualElement();
                thumb.AddToClassList("slot-thumb");
                var picture = SaveSystem.LoadThumbnail(slot.id);
                if (picture)
                {
                    thumbnails.Add(picture);
                    thumb.style.backgroundImage = new StyleBackground(picture);
                }
                row.Add(thumb);
                var info = new VisualElement();
                info.AddToClassList("slot-info");
                var name = new Label(Loc.T("scn.frontier"));
                name.AddToClassList("item-title");
                var meta = new Label(
                    Kind(slot.id)
                        + "  ·  "
                        + slot.data.SavedAt.ToString("g", culture)
                        + "  ·  "
                        + string.Format(Loc.T("save.played"), Loc.Clock(slot.data.playTime))
                );
                meta.AddToClassList("item-meta");
                info.Add(name);
                info.Add(meta);
                row.Add(info);
                var act = new Button { text = Loc.T(saving ? "save.overwrite" : "load.button") };
                act.AddToClassList("ghost-button");
                var del = new Button { text = Loc.T("save.delete") };
                del.AddToClassList("link-button");
                row.Add(act);
                row.Add(del);
                var s = slot;
                act.clicked += () =>
                {
                    if (saving)
                        confirm.Ask("save.overwrite.title", "save.overwrite.text", "save.overwrite", () => onSave?.Invoke(s.id));
                    else
                        onLoad?.Invoke(s.data);
                };
                del.clicked += () =>
                    confirm.Ask(
                        "save.delete.title",
                        "save.delete.text",
                        "save.delete",
                        () =>
                        {
                            SaveSystem.Delete(s.id);
                            Rebuild();
                        }
                    );
                list.Add(row);
            }
            empty.EnableInClassList("hidden", shown > 0 || saving);
        }
    }
}
