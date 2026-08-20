using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Maytrix.Menu.Services;
using UnityEngine;
using UnityEngine.XR;

namespace Maytrix.Menu.Menu
{
    internal sealed class MenuController : IDisposable
    {
        private const string DiscordUrl = "https://discord.com/channels/1520945955375943822";
        private const string GitHubUrl = "https://github.com/koolaid-dotcom/Maytrix-Mods";
        private const float PointerLength = 4f;
        private const float ActionDebounceSeconds = 0.25f;

        private readonly MenuSettings _settings;
        private readonly LocalGraphicsController _graphics;
        private readonly ManualLogSource _logger;
        private readonly FpsHud _fpsHud;
        private readonly Dictionary<string, MenuPage> _pages = new Dictionary<string, MenuPage>(StringComparer.Ordinal);
        private readonly List<MenuButtonView> _buttons = new List<MenuButtonView>();

        private GameObject? _root;
        private GameObject? _content;
        private LineRenderer? _pointer;
        private Material? _pointerMaterial;
        private TextMesh? _status;
        private TextMesh? _footer;
        private string _pageId = "home";
        private string _hoverDescription = string.Empty;
        private string _toast = string.Empty;
        private string _defaultHelpText = "Hold left primary | Aim opposite hand | Trigger";
        private string _lastFooterSource = string.Empty;
        private string _lastFooterRendered = string.Empty;
        private string? _confirmKey;
        private float _toastUntil;
        private float _confirmUntil;
        private float _nextLabelRefresh;
        private float _lastActivationAt = -ActionDebounceSeconds;
        private bool _wasTriggerDown;
        private bool _triggerArmed;
        private bool _needsRebuild = true;
        private bool _justOpened;
        private XRNode _activeMenuNode = XRNode.LeftHand;
        private XRNode _activePointerNode = XRNode.RightHand;

        public MenuController(MenuSettings settings, LocalGraphicsController graphics, ManualLogSource logger)
        {
            _settings = settings;
            _graphics = graphics;
            _logger = logger;
            _fpsHud = new FpsHud(settings, graphics);
            CreatePages();
        }

        public void Tick()
        {
            _fpsHud.Tick();
            EnsureRoot();
            if (_root == null)
            {
                return;
            }

            var currentlyVisible = _root.activeSelf;
            var configuredMenuNode = _settings.MenuOnRight.Value ? XRNode.RightHand : XRNode.LeftHand;
            var configuredPointerNode = _settings.MenuOnRight.Value ? XRNode.LeftHand : XRNode.RightHand;
            var menuNode = currentlyVisible ? _activeMenuNode : configuredMenuNode;
            var pointerNode = currentlyVisible ? _activePointerNode : configuredPointerNode;
            var menuDevice = InputDevices.GetDeviceAtXRNode(menuNode);
            var wantsToShow = menuDevice.isValid && ReadButton(menuDevice, CommonUsages.primaryButton);
            var hasOrigin = XrPoseResolver.TryGetTrackingOrigin(out var originPosition, out var originRotation);
            var menuPose = default(XrWorldPose);
            var hasMenuPose = hasOrigin && XrPoseResolver.TryGetWorldPose(menuDevice, originPosition, originRotation, out menuPose);
            var shouldShow = wantsToShow && hasMenuPose;

            if (currentlyVisible != shouldShow)
            {
                _root.SetActive(shouldShow);
                _wasTriggerDown = false;
                _triggerArmed = false;
                _hoverDescription = string.Empty;
                _justOpened = shouldShow;

                if (shouldShow)
                {
                    _activeMenuNode = menuNode;
                    _activePointerNode = pointerNode;
                    _defaultHelpText = BuildHelpText(menuNode);
                }
                else
                {
                    CancelConfirmationAndPrompt();
                }
            }

            if (!shouldShow)
            {
                return;
            }

            UpdateMenuPose(menuPose);
            if (_needsRebuild)
            {
                RebuildPage();
            }

            RefreshVisibleText();
            UpdatePointer(originPosition, originRotation, _activePointerNode);
            UpdateFooter();
        }

        public void Dispose()
        {
            _fpsHud.Dispose();

            if (_pointerMaterial != null)
            {
                UnityEngine.Object.Destroy(_pointerMaterial);
                _pointerMaterial = null;
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }

            _buttons.Clear();
        }

        private void CreatePages()
        {
            AddPage(new MenuPage(
                "home",
                "HOME",
                Navigation("DISCORD", "Community and support links.", "discord"),
                Navigation("PERFORMANCE", "FPS display and local graphics goals.", "performance"),
                Navigation("LIGHTING", "Reversible local lighting presets.", "lighting"),
                Navigation("APPEARANCE", "Menu themes, text, size, and pointer style.", "appearance"),
                Navigation("CONTROLS", "Handedness, placement, aim, and haptics.", "controls"),
                Navigation("HELP & ABOUT", "Controls, privacy, version, and reset.", "help")));

            AddPage(new MenuPage(
                "performance",
                "PERFORMANCE",
                new MenuItem(
                    "FPS Counter",
                    "Show a small local FPS and frame-time overlay.",
                    () => ChangeSetting(() => _settings.ShowFps.Value = !_settings.ShowFps.Value),
                    () => OnOff(_settings.ShowFps.Value)),
                new MenuItem(
                    "FPS Goal",
                    "Goal used by Auto Optimize; VR refresh rate is controlled by the headset runtime.",
                    () => ChangePerformance(_settings.CycleFpsGoal, restartAutomaticTimer: true),
                    () => _settings.FpsGoal.ToString()),
                new MenuItem(
                    "Auto Optimize",
                    "Gradually adjusts local render scale after sustained low FPS. No internet test is used.",
                    () => ChangePerformance(() => _settings.AutoOptimize.Value = !_settings.AutoOptimize.Value, restartAutomaticTimer: true),
                    () => _graphics.AutoStatusLabel),
                new MenuItem(
                    "Quality",
                    "Cycle Original, Balanced, and Performance render-scale profiles.",
                    () => ChangePerformance(_settings.CycleProfile, restartAutomaticTimer: true),
                    () => _graphics.EffectiveProfileLabel),
                new MenuItem(
                    "Free Memory",
                    "Manually release Unity assets that are no longer in use. This can briefly hitch VR.",
                    () => ConfirmAction("free-memory", RequestMemoryCleanup, "Press again to free unused memory"),
                    () => IsConfirming("free-memory") ? "CONFIRM" : _graphics.CleanupStatusLabel,
                    isEnabled: () => _graphics.CanRequestMemoryCleanup || IsConfirming("free-memory")),
                new MenuItem(
                    "Reset Performance",
                    "Press twice to restore the original render scale and performance settings.",
                    () => ConfirmAction("reset-performance", ResetPerformance, "Press again to reset performance"),
                    () => ConfirmValue("reset-performance"),
                    MenuItemKind.Destructive)));

            AddPage(new MenuPage(
                "lighting",
                "LIGHTING",
                LightingPreset("Smooth Lighting", "Higher-quality soft lighting. This may reduce FPS.", 1),
                LightingPreset("Normal Lighting", "Use a balanced local lighting preset.", 2),
                LightingPreset("Rough Lighting", "Use simpler hard lighting for a lower rendering cost.", 3),
                new MenuItem(
                    "Reset Lighting",
                    "Press twice to clear the preset and restore the captured game lighting.",
                    () => ConfirmAction("reset-lighting", ResetLighting, "Press again to reset lighting"),
                    () => ConfirmValue("reset-lighting"),
                    MenuItemKind.Destructive)));

            AddPage(new MenuPage(
                "appearance",
                "APPEARANCE",
                new MenuItem("Theme", "Cycle Midnight, Neon, and High Contrast.", () => ChangeVisual(_settings.CycleTheme), () => _settings.ThemeLabel),
                new MenuItem(
                    "Accent",
                    "Cycle Cyan, Purple, Emerald, and Amber accents.",
                    () => ChangeVisual(_settings.CycleAccent),
                    () => _settings.ThemeIndex.Value == 2 ? "Fixed" : _settings.AccentLabel,
                    isEnabled: () => _settings.ThemeIndex.Value != 2),
                new MenuItem("Text Size", "Cycle Small, Normal, and Large menu text.", () => ChangeVisual(_settings.CycleTextSize), () => _settings.TextSizeLabel),
                new MenuItem("Menu Size", "Cycle Small, Normal, and Large menu scale.", () => ChangeVisual(_settings.CycleMenuSize), () => _settings.MenuSizeLabel),
                new MenuItem("Pointer", "Cycle Thin, Normal, and Bold pointer widths.", () => ChangeSetting(_settings.CyclePointerWidth), () => _settings.PointerWidthLabel)));

            AddPage(new MenuPage(
                "controls",
                "CONTROLS",
                new MenuItem(
                    "Menu Hand",
                    "Switch the menu hand; the pointer uses the opposite hand. Applies after closing.",
                    ChangeMenuHand,
                    () => _settings.MenuHandLabel),
                new MenuItem("Haptics", "Pulse the pointer controller after a successful selection.", () => ChangeSetting(() => _settings.Haptics.Value = !_settings.Haptics.Value), () => OnOff(_settings.Haptics.Value)),
                new MenuItem("Placement", "Cycle Close, Normal, and Far menu placement.", () => ChangeSetting(_settings.CycleDistance), () => _settings.DistanceLabel),
                new MenuItem("Smooth Follow", "Smooth movement while the menu follows the controller.", () => ChangeSetting(() => _settings.SmoothFollow.Value = !_settings.SmoothFollow.Value), () => OnOff(_settings.SmoothFollow.Value)),
                new MenuItem("Pointer Angle", "Adjust controller pointer pitch from -30 to +30 degrees.", () => ChangeSetting(_settings.CyclePointerPitch), () => $"{_settings.PointerPitch.Value:+0;-0;0} deg")));

            AddPage(new MenuPage(
                "discord",
                "DISCORD",
                new MenuItem(
                    "Open Discord",
                    "Press twice to open the existing-members Discord page in the PC browser.",
                    () => ConfirmAction("open-discord", () => OpenAllowedUrl(DiscordUrl, "Discord browser request sent"), "Press again to open Discord"),
                    () => ConfirmValue("open-discord"),
                    MenuItemKind.External),
                new MenuItem("Access", "This link works for people who are already members.", () => ShowToast("Discord link: existing members"), () => "Members", MenuItemKind.Info),
                new MenuItem("Privacy", "Maytrix never reads Discord login details or tokens.", () => ShowToast("No Discord login or token access"), () => "No Login Read", MenuItemKind.Info),
                new MenuItem(
                    "Open GitHub",
                    "Press twice to open the Maytrix GitHub project in the PC browser.",
                    () => ConfirmAction("open-github", () => OpenAllowedUrl(GitHubUrl, "GitHub browser request sent"), "Press again to open GitHub"),
                    () => ConfirmValue("open-github"),
                    MenuItemKind.External)));

            AddPage(new MenuPage(
                "help",
                "HELP & ABOUT",
                new MenuItem("How to Use", "Hold the menu-hand primary button, aim with the other hand, and press trigger.", () => ShowToast("Hold primary | Aim opposite hand | Trigger"), kind: MenuItemKind.Info),
                new MenuItem("Version", "Current Maytrix Menu build.", () => ShowToast($"Maytrix Menu v{PluginInfo.Version}"), () => PluginInfo.Version, MenuItemKind.Info),
                new MenuItem("Privacy", "Local settings only. No telemetry or background web requests.", () => ShowToast("Local only | No telemetry"), () => "Local Only", MenuItemKind.Info),
                new MenuItem("Safety", "No cheats, player targeting, room disruption, or anti-cheat bypasses.", () => ShowToast("Safe local features only"), () => "Local UI", MenuItemKind.Info),
                new MenuItem(
                    "Reset Everything",
                    "Press twice to restore every Maytrix setting and graphics value.",
                    () => ConfirmAction("reset-all", ResetAll, "Press again to reset everything"),
                    () => ConfirmValue("reset-all"),
                    MenuItemKind.Destructive)));
        }

        private void AddPage(MenuPage page)
        {
            _pages.Add(page.Id, page);
        }

        private MenuItem Navigation(string label, string description, string pageId)
        {
            return new MenuItem(label, description, () => Navigate(pageId), kind: MenuItemKind.Navigation);
        }

        private MenuItem LightingPreset(string label, string description, int mode)
        {
            return new MenuItem(
                label,
                description,
                () => SetLighting(mode),
                () => _settings.LightingMode.Value == mode ? "Active" : string.Empty);
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("MaytrixMenu")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _root.SetActive(false);

            _pointer = _root.AddComponent<LineRenderer>();
            _pointer.positionCount = 2;
            _pointer.useWorldSpace = true;
            _pointer.numCapVertices = 3;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader != null)
            {
                _pointerMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _pointer.material = _pointerMaterial;
            }
        }

        private void RebuildPage()
        {
            if (_root == null)
            {
                return;
            }

            if (!_pages.TryGetValue(_pageId, out var page))
            {
                _pageId = "home";
                page = _pages[_pageId];
            }

            if (_content != null)
            {
                _content.SetActive(false);
                UnityEngine.Object.Destroy(_content);
            }

            _buttons.Clear();
            _content = new GameObject("Content");
            _content.transform.SetParent(_root.transform, false);
            _content.transform.localScale = Vector3.one * _settings.MenuScale;

            var theme = CurrentTheme;
            CreatePanel(theme);
            CreateHeader(page.Title, theme);
            _status = CreateText(string.Empty, new Vector3(0f, 0.155f, -0.012f), 34, 0.0053f, theme.Muted);

            var itemCount = Mathf.Min(page.Items.Count, 6);
            for (var index = 0; index < itemCount; index++)
            {
                CreateGridButton(page.Items[index], index, theme);
            }

            if (page.Items.Count > 6)
            {
                _logger.LogWarning($"Page {page.Id} has more than six items; extra items were not rendered.");
            }

            if (!string.Equals(page.Id, "home", StringComparison.Ordinal))
            {
                CreateHomeButton(theme);
            }

            _footer = CreateText(_defaultHelpText, new Vector3(0f, -0.238f, -0.012f), 28, 0.0048f, theme.Text);
            _lastFooterSource = string.Empty;
            _lastFooterRendered = string.Empty;
            _needsRebuild = false;
            _nextLabelRefresh = 0f;
            RefreshVisibleText(force: true);
        }

        private void CreatePanel(MenuTheme theme)
        {
            CreateColoredBlock("Panel", Vector3.zero, new Vector3(0.56f, 0.56f, 0.015f), theme.Panel, disableCollider: true);
            CreateColoredBlock("AccentBar", new Vector3(0f, 0.267f, -0.010f), new Vector3(0.54f, 0.008f, 0.018f), theme.Accent, disableCollider: true);
        }

        private void CreateHeader(string title, MenuTheme theme)
        {
            CreateText("MM  MAYTRIX", new Vector3(0f, 0.228f, -0.012f), 64, 0.0068f, theme.Accent);
            CreateText(title, new Vector3(0f, 0.190f, -0.012f), 43, 0.0058f, theme.Text);
        }

        private void CreateGridButton(MenuItem item, int index, MenuTheme theme)
        {
            var column = index % 2;
            var row = index / 2;
            var x = column == 0 ? -0.122f : 0.122f;
            var y = 0.090f - row * 0.075f;
            CreateButton(item, new Vector3(x, y, -0.018f), new Vector3(0.225f, 0.060f, 0.018f), theme);
        }

        private void CreateHomeButton(MenuTheme theme)
        {
            var home = new MenuItem("HOME", "Return to the six main categories.", () => Navigate("home"), kind: MenuItemKind.Navigation);
            CreateButton(home, new Vector3(0f, -0.155f, -0.018f), new Vector3(0.47f, 0.050f, 0.018f), theme);
        }

        private void CreateButton(MenuItem item, Vector3 position, Vector3 scale, MenuTheme theme)
        {
            if (_content == null)
            {
                return;
            }

            var button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = $"Button_{item.Label}";
            button.transform.SetParent(_content.transform, false);
            button.transform.localPosition = position;
            button.transform.localScale = scale;

            var view = button.AddComponent<MenuButtonView>();
            view.Initialize(item, theme, _settings.TextScale);
            _buttons.Add(view);
        }

        private GameObject CreateColoredBlock(string name, Vector3 position, Vector3 scale, Color color, bool disableCollider)
        {
            if (_content == null)
            {
                throw new InvalidOperationException("Menu content has not been created.");
            }

            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(_content.transform, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;

            var collider = block.GetComponent<Collider>();
            if (disableCollider && collider != null)
            {
                collider.enabled = false;
            }

            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                var colorBlock = new MaterialPropertyBlock();
                colorBlock.SetColor("_Color", color);
                colorBlock.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(colorBlock);
            }

            return block;
        }

        private TextMesh CreateText(string value, Vector3 position, int fontSize, float characterSize, Color color)
        {
            if (_content == null)
            {
                throw new InvalidOperationException("Menu content has not been created.");
            }

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(_content.transform, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = Quaternion.identity;

            var text = textObject.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = fontSize;
            text.characterSize = characterSize * _settings.TextScale;
            text.color = color;
            return text;
        }

        private void UpdateMenuPose(XrWorldPose menuPose)
        {
            if (_root == null)
            {
                return;
            }

            var sideOffset = _activeMenuNode == XRNode.LeftHand ? 0.10f : -0.10f;
            var targetPosition = menuPose.Position + menuPose.Rotation * new Vector3(sideOffset, 0.08f, _settings.MenuDistance);
            var targetRotation = _root.transform.rotation;
            var camera = Camera.main;
            if (camera != null)
            {
                var awayFromCamera = targetPosition - camera.transform.position;
                if (awayFromCamera.sqrMagnitude > 0.0001f)
                {
                    targetRotation = Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
                }
            }

            if (_justOpened || !_settings.SmoothFollow.Value)
            {
                _root.transform.SetPositionAndRotation(targetPosition, targetRotation);
                _justOpened = false;
                return;
            }

            var blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f);
            _root.transform.position = Vector3.Lerp(_root.transform.position, targetPosition, blend);
            _root.transform.rotation = Quaternion.Slerp(_root.transform.rotation, targetRotation, blend);
        }

        private void UpdatePointer(Vector3 originPosition, Quaternion originRotation, XRNode pointerNode)
        {
            if (_pointer == null)
            {
                return;
            }

            var pointerDevice = InputDevices.GetDeviceAtXRNode(pointerNode);
            if (!pointerDevice.isValid || !XrPoseResolver.TryGetWorldPose(pointerDevice, originPosition, originRotation, out var pointerPose))
            {
                _pointer.enabled = false;
                _wasTriggerDown = false;
                _triggerArmed = false;
                _hoverDescription = string.Empty;
                return;
            }

            _pointer.enabled = true;
            _pointer.startWidth = _settings.PointerStartWidth;
            _pointer.endWidth = _settings.PointerStartWidth * 0.35f;
            var direction = pointerPose.Rotation * Quaternion.Euler(_settings.PointerPitch.Value, 0f, 0f) * Vector3.forward;
            var hovered = FindNearestMenuButton(new Ray(pointerPose.Position, direction), out var hitPoint);
            var end = hovered == null ? pointerPose.Position + direction * PointerLength : hitPoint;
            _pointer.SetPosition(0, pointerPose.Position);
            _pointer.SetPosition(1, end);

            var theme = CurrentTheme;
            _pointer.startColor = theme.Accent;
            _pointer.endColor = theme.Accent;

            foreach (var button in _buttons)
            {
                button.SetHovered(button == hovered);
            }

            _hoverDescription = hovered?.Description ?? string.Empty;
            var triggerDown = ReadTrigger(pointerDevice);
            if (!_triggerArmed)
            {
                _triggerArmed = !triggerDown;
                _wasTriggerDown = triggerDown;
                return;
            }

            if (hovered != null && triggerDown && !_wasTriggerDown && Time.unscaledTime - _lastActivationAt >= ActionDebounceSeconds)
            {
                _lastActivationAt = Time.unscaledTime;
                var confirmationBefore = _confirmKey;
                try
                {
                    var activated = hovered.Activate();
                    if (activated)
                    {
                        if (confirmationBefore != null && string.Equals(_confirmKey, confirmationBefore, StringComparison.Ordinal))
                        {
                            CancelConfirmation();
                        }

                        Pulse(pointerDevice);
                        RefreshVisibleText(force: true);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError($"Menu action failed: {exception}");
                    ShowToast("That action could not be completed");
                }

                _triggerArmed = false;
            }

            _wasTriggerDown = triggerDown;
        }

        private MenuButtonView? FindNearestMenuButton(Ray ray, out Vector3 hitPoint)
        {
            hitPoint = ray.origin + ray.direction * PointerLength;
            MenuButtonView? nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var button in _buttons)
            {
                if (button.HitTest(ray, PointerLength, out var distance, out var point) && distance < nearestDistance)
                {
                    nearest = button;
                    nearestDistance = distance;
                    hitPoint = point;
                }
            }

            return nearest;
        }

        private void RefreshVisibleText(bool force = false)
        {
            if (!force && Time.unscaledTime < _nextLabelRefresh)
            {
                return;
            }

            if (_confirmKey != null && Time.unscaledTime > _confirmUntil)
            {
                CancelConfirmation();
            }

            foreach (var button in _buttons)
            {
                button.Refresh();
            }

            if (_status != null)
            {
                _status.text = Clip(StatusText, 62);
            }

            _nextLabelRefresh = Time.unscaledTime + 0.25f;
        }

        private void UpdateFooter()
        {
            if (_footer == null)
            {
                return;
            }

            var message = Time.unscaledTime < _toastUntil
                ? _toast
                : string.IsNullOrWhiteSpace(_hoverDescription)
                    ? _defaultHelpText
                    : _hoverDescription;
            if (string.Equals(message, _lastFooterSource, StringComparison.Ordinal))
            {
                return;
            }

            _lastFooterSource = message;
            _lastFooterRendered = WrapFooter(message);
            _footer.text = _lastFooterRendered;
        }

        private string StatusText => _pageId == "home" || _pageId == "performance"
            ? $"{_graphics.FpsLabel} FPS | GOAL {_graphics.EffectiveGoalLabel} | AUTO {_graphics.AutoStatusLabel}"
            : _pageId == "lighting"
                ? $"LIGHTING {_settings.LightingLabel} | RENDER {_graphics.RenderScaleLabel}"
                : $"LOCAL ONLY | v{PluginInfo.Version} | NO TELEMETRY";

        private MenuTheme CurrentTheme => MenuTheme.Resolve(_settings.ThemeIndex.Value, _settings.AccentIndex.Value);

        private void Navigate(string pageId)
        {
            if (!_pages.ContainsKey(pageId))
            {
                return;
            }

            _pageId = pageId;
            _needsRebuild = true;
            CancelConfirmationAndPrompt();
        }

        private void ChangeSetting(Action change)
        {
            change();
            ShowToast("Saved");
        }

        private void ChangeVisual(Action change)
        {
            change();
            _needsRebuild = true;
            ShowToast("Appearance saved");
        }

        private void ChangePerformance(Action change, bool restartAutomaticTimer)
        {
            change();
            _graphics.ApplyPerformanceSettings(restartAutomaticTimer);
            ShowToast("Performance setting saved");
        }

        private void ChangeMenuHand()
        {
            _settings.MenuOnRight.Value = !_settings.MenuOnRight.Value;
            ShowToast("Hand change applies after closing");
        }

        private void SetLighting(int mode)
        {
            _settings.LightingMode.Value = mode;
            _graphics.ApplyLightingSettings();
            ShowToast($"Lighting: {_settings.LightingLabel}");
        }

        private void RequestMemoryCleanup()
        {
            _graphics.RequestMemoryCleanup(out var message);
            ShowToast(message, 3f);
        }

        private void ResetPerformance()
        {
            _settings.ResetPerformance();
            _graphics.ApplyPerformanceSettings(restartAutomaticTimer: true);
            ShowToast("Performance restored");
        }

        private void ResetLighting()
        {
            _settings.ResetLighting();
            _graphics.ApplyLightingSettings();
            ShowToast("Lighting restored");
        }

        private void ResetAll()
        {
            _settings.ResetAll();
            _graphics.ApplyAllSettings(restartAutomaticTimer: true);
            _pageId = "home";
            _needsRebuild = true;
            ShowToast("All Maytrix settings restored");
        }

        private void OpenAllowedUrl(string url, string successMessage)
        {
            var allowed = string.Equals(url, DiscordUrl, StringComparison.Ordinal) || string.Equals(url, GitHubUrl, StringComparison.Ordinal);
            if (!allowed || !url.StartsWith("https://", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("External URL is not allowlisted.");
            }

            Application.OpenURL(url);
            ShowToast(successMessage);
        }

        private void ConfirmAction(string key, Action confirmedAction, string prompt)
        {
            if (string.Equals(_confirmKey, key, StringComparison.Ordinal) && Time.unscaledTime <= _confirmUntil)
            {
                _confirmKey = null;
                _confirmUntil = 0f;
                confirmedAction();
                return;
            }

            _confirmKey = key;
            _confirmUntil = Time.unscaledTime + 5f;
            ShowToast(prompt, 5f);
        }

        private string ConfirmValue(string key)
        {
            return IsConfirming(key) ? "CONFIRM" : string.Empty;
        }

        private bool IsConfirming(string key)
        {
            return string.Equals(_confirmKey, key, StringComparison.Ordinal) && Time.unscaledTime <= _confirmUntil;
        }

        private void CancelConfirmation()
        {
            _confirmKey = null;
            _confirmUntil = 0f;
        }

        private void CancelConfirmationAndPrompt()
        {
            if (_confirmKey != null)
            {
                _toastUntil = 0f;
            }

            CancelConfirmation();
        }

        private void ShowToast(string message, float seconds = 1.8f)
        {
            _toast = message;
            _toastUntil = Time.unscaledTime + seconds;
        }

        private void Pulse(InputDevice device)
        {
            if (!_settings.Haptics.Value)
            {
                return;
            }

            if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0u, 0.25f, 0.04f);
            }
        }

        private bool ReadTrigger(InputDevice device)
        {
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out var pressed))
            {
                return pressed;
            }

            if (!device.TryGetFeatureValue(CommonUsages.trigger, out var amount))
            {
                return false;
            }

            return _wasTriggerDown ? amount > 0.55f : amount > 0.75f;
        }

        private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
        {
            return device.TryGetFeatureValue(usage, out var value) && value;
        }

        private static string OnOff(bool value)
        {
            return value ? "On" : "Off";
        }

        private static string BuildHelpText(XRNode menuNode)
        {
            var hand = menuNode == XRNode.RightHand ? "right" : "left";
            return $"Hold {hand} primary | Aim opposite hand | Trigger";
        }

        private static string Clip(string value, int maximumLength)
        {
            return value.Length <= maximumLength ? value : value.Substring(0, maximumLength - 3) + "...";
        }

        private static string WrapFooter(string value)
        {
            const int lineLength = 48;
            var clipped = Clip(value, lineLength * 2 - 1);
            if (clipped.Length <= lineLength)
            {
                return clipped;
            }

            var split = clipped.LastIndexOf(' ', lineLength);
            if (split < lineLength / 2)
            {
                split = lineLength;
            }

            return clipped.Substring(0, split).TrimEnd() + "\n" + clipped.Substring(split).TrimStart();
        }
    }
}
