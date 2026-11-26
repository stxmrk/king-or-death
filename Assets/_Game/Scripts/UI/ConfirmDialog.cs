using System;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // Logic for Confirm.uxml: a modal yes/no question over the current screen.
    public class ConfirmDialog
    {
        readonly VisualElement root;
        readonly Label title,
            text;
        readonly Button yes,
            no;
        Action onYes;

        public bool IsOpen => !root.ClassListContains("hidden");

        public ConfirmDialog(VisualElement root)
        {
            this.root = root;
            title = root.Q<Label>("confirmTitle");
            text = root.Q<Label>("confirmText");
            yes = root.Q<Button>("confirmYes");
            no = root.Q<Button>("confirmNo");
            yes.clicked += () =>
            {
                Close();
                onYes?.Invoke();
            };
            no.clicked += Close;
            Close();
        }

        // All arguments are Loc keys.
        public void Ask(string titleKey, string textKey, string yesKey, Action action)
        {
            onYes = action;
            title.text = Loc.T(titleKey);
            text.text = Loc.T(textKey);
            yes.text = Loc.T(yesKey);
            UiFx.Reveal(root);
            no.Focus();
        }

        public void Close() => root.AddToClassList("hidden");
    }
}
