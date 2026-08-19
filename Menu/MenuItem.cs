using System;

namespace MaytrixMods
{
    internal enum MenuItemKind
    {
        Toggle,
        Action,
        Link
    }

    internal sealed class MenuItem
    {
        public MenuItem(
            string label,
            string description,
            MenuItemKind kind = MenuItemKind.Toggle,
            Action<MenuItem>? onPressed = null)
        {
            Label = label;
            Description = description;
            Kind = kind;
            OnPressed = onPressed;
        }

        public string Label { get; }
        public string Description { get; }
        public MenuItemKind Kind { get; }
        public bool Enabled { get; set; }
        public Action<MenuItem>? OnPressed { get; }

        public void Press()
        {
            if (Kind == MenuItemKind.Toggle)
                Enabled = !Enabled;

            OnPressed?.Invoke(this);
        }
    }
}

