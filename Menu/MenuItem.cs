using System;

namespace Maytrix.Menu.Menu
{
    internal enum MenuItemKind
    {
        Action,
        Navigation,
        External,
        Destructive,
        Info
    }

    internal sealed class MenuItem
    {
        public MenuItem(
            string label,
            string description,
            Action activate,
            Func<string>? valueText = null,
            MenuItemKind kind = MenuItemKind.Action,
            Func<bool>? isEnabled = null)
        {
            Label = label;
            Description = description;
            Activate = activate;
            ValueText = valueText;
            Kind = kind;
            IsEnabled = isEnabled;
        }

        public string Label { get; }
        public string Description { get; }
        public Action Activate { get; }
        public Func<string>? ValueText { get; }
        public MenuItemKind Kind { get; }
        public Func<bool>? IsEnabled { get; }
        public bool Enabled => IsEnabled == null || IsEnabled();

        public string DisplayLabel
        {
            get
            {
                var value = ValueText?.Invoke();
                return string.IsNullOrWhiteSpace(value) ? Label : $"{Label}: {value}";
            }
        }
    }
}
