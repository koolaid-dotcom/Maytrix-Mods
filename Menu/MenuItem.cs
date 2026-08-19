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
            Action<MenuItem>? onPressed = null,
            Func<string>? valueProvider = null)
        {
            Label = label;
            Description = description;
            Kind = kind;
            OnPressed = onPressed;
            ValueProvider = valueProvider;
        }

        public string Label { get; }
        public string Description { get; }
        public MenuItemKind Kind { get; }
        public bool Enabled { get; set; }
        public Action<MenuItem>? OnPressed { get; }
        public Func<string>? ValueProvider { get; }

        public string DisplayLabel
        {
            get
            {
                string? value = ValueProvider?.Invoke();
                return string.IsNullOrWhiteSpace(value) ? Label : $"{Label}\n{value}";
            }
        }

        public void Press()
        {
            if (Kind == MenuItemKind.Toggle)
                Enabled = !Enabled;

            OnPressed?.Invoke(this);
        }
    }
}
