using BepInEx.Configuration;

namespace Maytrix.Menu.Menu
{
    internal sealed class MenuSettings
    {
        public MenuSettings(ConfigFile config)
        {
            ThemeIndex = BindRange(config, "Appearance", "Theme", 0, 0, 2, "0 Midnight, 1 Neon, 2 High Contrast.");
            AccentIndex = BindRange(config, "Appearance", "Accent", 0, 0, 3, "0 Cyan, 1 Purple, 2 Emerald, 3 Amber.");
            TextScaleStep = BindRange(config, "Appearance", "TextSize", 1, 0, 2, "0 Small, 1 Normal, 2 Large.");
            ScaleStep = BindRange(config, "Appearance", "MenuSize", 1, 0, 2, "0 Small, 1 Normal, 2 Large.");
            PointerWidthStep = BindRange(config, "Appearance", "PointerWidth", 1, 0, 2, "0 Thin, 1 Normal, 2 Bold.");

            MenuOnRight = config.Bind("Controls", "MenuOnRight", false, "Put the menu on the right hand and the pointer on the left.");
            Haptics = config.Bind("Controls", "Haptics", true, "Pulse the pointer controller after selection.");
            SmoothFollow = config.Bind("Controls", "SmoothFollow", true, "Smooth menu movement while it follows the controller.");
            DistanceStep = BindRange(config, "Controls", "MenuDistance", 1, 0, 2, "0 Close, 1 Normal, 2 Far.");
            PointerPitch = config.Bind(
                "Controls",
                "PointerPitch",
                0f,
                new ConfigDescription("Additional pointer pitch in degrees.", new AcceptableValueRange<float>(-30f, 30f)));

            ShowFps = config.Bind("Performance", "ShowFps", false, "Show the local FPS overlay.");
            FpsGoalStep = BindRange(config, "Performance", "FpsGoal", 0, 0, 3, "Optimizer goal: 0 72, 1 80, 2 90, 3 120.");
            AutoOptimize = config.Bind("Performance", "AutoOptimize", false, "Gradually lower local render scale after sustained low FPS.");
            QualityProfile = BindRange(config, "Performance", "QualityProfile", 0, 0, 2, "0 Original, 1 Balanced, 2 Performance.");
            LightingMode = BindRange(config, "Lighting", "Mode", 0, 0, 3, "0 Original, 1 Smooth, 2 Normal, 3 Rough.");
        }

        public ConfigEntry<int> ThemeIndex { get; }
        public ConfigEntry<int> AccentIndex { get; }
        public ConfigEntry<int> TextScaleStep { get; }
        public ConfigEntry<int> ScaleStep { get; }
        public ConfigEntry<int> PointerWidthStep { get; }
        public ConfigEntry<bool> MenuOnRight { get; }
        public ConfigEntry<bool> Haptics { get; }
        public ConfigEntry<bool> SmoothFollow { get; }
        public ConfigEntry<int> DistanceStep { get; }
        public ConfigEntry<float> PointerPitch { get; }
        public ConfigEntry<bool> ShowFps { get; }
        public ConfigEntry<int> FpsGoalStep { get; }
        public ConfigEntry<bool> AutoOptimize { get; }
        public ConfigEntry<int> QualityProfile { get; }
        public ConfigEntry<int> LightingMode { get; }

        public float MenuScale => ScaleStep.Value == 0 ? 0.86f : ScaleStep.Value == 2 ? 1.14f : 1f;
        public float TextScale => TextScaleStep.Value == 0 ? 0.88f : TextScaleStep.Value == 2 ? 1.12f : 1f;
        public float MenuDistance => DistanceStep.Value == 0 ? 0.12f : DistanceStep.Value == 2 ? 0.22f : 0.17f;
        public float PointerStartWidth => PointerWidthStep.Value == 0 ? 0.0035f : PointerWidthStep.Value == 2 ? 0.009f : 0.006f;
        public int FpsGoal => FpsGoalStep.Value == 1 ? 80 : FpsGoalStep.Value == 2 ? 90 : FpsGoalStep.Value == 3 ? 120 : 72;

        public string ThemeLabel => ThemeIndex.Value == 1 ? "Neon" : ThemeIndex.Value == 2 ? "High Contrast" : "Midnight";
        public string AccentLabel => AccentIndex.Value == 1 ? "Purple" : AccentIndex.Value == 2 ? "Emerald" : AccentIndex.Value == 3 ? "Amber" : "Cyan";
        public string TextSizeLabel => StepLabel(TextScaleStep.Value);
        public string MenuSizeLabel => StepLabel(ScaleStep.Value);
        public string PointerWidthLabel => PointerWidthStep.Value == 0 ? "Thin" : PointerWidthStep.Value == 2 ? "Bold" : "Normal";
        public string MenuHandLabel => MenuOnRight.Value ? "Right" : "Left";
        public string DistanceLabel => DistanceStep.Value == 0 ? "Close" : DistanceStep.Value == 2 ? "Far" : "Normal";
        public string ProfileLabel => QualityProfile.Value == 1 ? "Balanced" : QualityProfile.Value == 2 ? "Performance" : "Original";
        public string LightingLabel => LightingMode.Value == 1 ? "Smooth" : LightingMode.Value == 2 ? "Normal" : LightingMode.Value == 3 ? "Rough" : "Original";

        public void CycleTheme() => Cycle(ThemeIndex, 3);
        public void CycleAccent() => Cycle(AccentIndex, 4);
        public void CycleTextSize() => Cycle(TextScaleStep, 3);
        public void CycleMenuSize() => Cycle(ScaleStep, 3);
        public void CyclePointerWidth() => Cycle(PointerWidthStep, 3);
        public void CycleDistance() => Cycle(DistanceStep, 3);
        public void CycleFpsGoal() => Cycle(FpsGoalStep, 4);
        public void CycleProfile() => Cycle(QualityProfile, 3);

        public void CyclePointerPitch()
        {
            PointerPitch.Value = PointerPitch.Value >= 30f ? -30f : PointerPitch.Value + 15f;
        }

        public void ResetPerformance()
        {
            ShowFps.Value = false;
            FpsGoalStep.Value = 0;
            AutoOptimize.Value = false;
            QualityProfile.Value = 0;
        }

        public void ResetLighting()
        {
            LightingMode.Value = 0;
        }

        public void ResetAll()
        {
            ThemeIndex.Value = 0;
            AccentIndex.Value = 0;
            TextScaleStep.Value = 1;
            ScaleStep.Value = 1;
            PointerWidthStep.Value = 1;
            MenuOnRight.Value = false;
            Haptics.Value = true;
            SmoothFollow.Value = true;
            DistanceStep.Value = 1;
            PointerPitch.Value = 0f;
            ResetPerformance();
            ResetLighting();
        }

        private static ConfigEntry<int> BindRange(
            ConfigFile config,
            string section,
            string key,
            int defaultValue,
            int minimum,
            int maximum,
            string description)
        {
            return config.Bind(
                section,
                key,
                defaultValue,
                new ConfigDescription(description, new AcceptableValueRange<int>(minimum, maximum)));
        }

        private static void Cycle(ConfigEntry<int> entry, int count)
        {
            entry.Value = (entry.Value + 1) % count;
        }

        private static string StepLabel(int step)
        {
            return step == 0 ? "Small" : step == 2 ? "Large" : "Normal";
        }
    }
}
