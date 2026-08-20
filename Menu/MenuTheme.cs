using UnityEngine;

namespace Maytrix.Menu.Menu
{
    internal readonly struct MenuTheme
    {
        private MenuTheme(Color panel, Color button, Color navigation, Color info, Color hover, Color accent, Color danger, Color text, Color muted)
        {
            Panel = panel;
            Button = button;
            Navigation = navigation;
            Info = info;
            Hover = hover;
            Accent = accent;
            Danger = danger;
            Text = text;
            Muted = muted;
        }

        public Color Panel { get; }
        public Color Button { get; }
        public Color Navigation { get; }
        public Color Info { get; }
        public Color Hover { get; }
        public Color Accent { get; }
        public Color Danger { get; }
        public Color Text { get; }
        public Color Muted { get; }

        public static MenuTheme Resolve(int themeIndex, int accentIndex)
        {
            var accent = accentIndex == 1
                ? new Color(0.67f, 0.35f, 1f, 1f)
                : accentIndex == 2
                    ? new Color(0.12f, 0.90f, 0.56f, 1f)
                    : accentIndex == 3
                        ? new Color(1f, 0.64f, 0.12f, 1f)
                        : new Color(0.10f, 0.85f, 1f, 1f);

            if (themeIndex == 2)
            {
                accent = new Color(1f, 0.85f, 0.05f, 1f);
                return new MenuTheme(
                    new Color(0.01f, 0.01f, 0.01f, 1f),
                    new Color(0.13f, 0.13f, 0.13f, 1f),
                    new Color(0.20f, 0.20f, 0.20f, 1f),
                    new Color(0.08f, 0.08f, 0.08f, 1f),
                    accent,
                    accent,
                    new Color(0.85f, 0.18f, 0.15f, 1f),
                    Color.white,
                    new Color(0.70f, 0.70f, 0.70f, 1f));
            }

            if (themeIndex == 1)
            {
                return new MenuTheme(
                    new Color(0.025f, 0.055f, 0.12f, 1f),
                    new Color(0.06f, 0.12f, 0.22f, 1f),
                    new Color(0.07f, 0.20f, 0.30f, 1f),
                    new Color(0.04f, 0.09f, 0.16f, 1f),
                    accent,
                    accent,
                    new Color(0.90f, 0.20f, 0.28f, 1f),
                    Color.white,
                    new Color(0.62f, 0.74f, 0.82f, 1f));
            }

            return new MenuTheme(
                new Color(0.025f, 0.030f, 0.055f, 1f),
                new Color(0.075f, 0.085f, 0.13f, 1f),
                new Color(0.09f, 0.12f, 0.18f, 1f),
                new Color(0.045f, 0.052f, 0.08f, 1f),
                accent,
                accent,
                new Color(0.82f, 0.16f, 0.24f, 1f),
                new Color(0.96f, 0.97f, 1f, 1f),
                new Color(0.60f, 0.64f, 0.74f, 1f));
        }
    }
}
