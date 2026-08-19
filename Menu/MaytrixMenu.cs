using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace MaytrixMods
{
    /// <summary>
    /// Original controller-first world-space menu for Maytrix Mods.
    /// Every displayed option changes the local interface or opens an explicit link.
    /// </summary>
    internal sealed class MaytrixMenu : IDisposable
    {
        private const float PanelWidth = 0.57f;
        private const float PanelHeight = 0.43f;
        private const float PanelDepth = 0.012f;
        private const int ItemsPerPage = 8;
        private const int MenuLayer = 5;
        private const string PreferencePrefix = "Maytrix.Menu.";

        private static readonly Color TextPrimary = new Color(0.95f, 0.98f, 1f, 1f);
        private static readonly Color TextMuted = new Color(0.55f, 0.64f, 0.76f, 1f);

        private static readonly Palette[] Palettes =
        {
            new Palette("NEON", new Color(0.018f, 0.025f, 0.055f, 0.985f), new Color(0.035f, 0.050f, 0.095f, 1f), new Color(0.055f, 0.072f, 0.125f, 1f), new Color(0.18f, 0.72f, 1f, 1f), new Color(0.55f, 0.30f, 1f, 1f)),
            new Palette("AURORA", new Color(0.018f, 0.046f, 0.052f, 0.985f), new Color(0.030f, 0.082f, 0.083f, 1f), new Color(0.050f, 0.115f, 0.110f, 1f), new Color(0.18f, 1f, 0.73f, 1f), new Color(0.18f, 0.65f, 1f, 1f)),
            new Palette("SUNSET", new Color(0.055f, 0.022f, 0.052f, 0.985f), new Color(0.095f, 0.037f, 0.078f, 1f), new Color(0.135f, 0.055f, 0.100f, 1f), new Color(1f, 0.38f, 0.68f, 1f), new Color(1f, 0.66f, 0.24f, 1f)),
            new Palette("ICE", new Color(0.018f, 0.035f, 0.070f, 0.985f), new Color(0.035f, 0.065f, 0.120f, 1f), new Color(0.055f, 0.095f, 0.160f, 1f), new Color(0.40f, 0.90f, 1f, 1f), new Color(0.46f, 0.58f, 1f, 1f))
        };

        private static readonly float[] MenuScales = { 0.82f, 1f, 1.18f };
        private static readonly string[] MenuScaleNames = { "SMALL", "MEDIUM", "LARGE" };
        private static readonly float[] PointerLengths = { 1.25f, 2f, 3f };
        private static readonly string[] PointerLengthNames = { "SHORT", "NORMAL", "LONG" };
        private static readonly float[] FollowSpeeds = { 10f, 18f, 30f };
        private static readonly string[] FollowSpeedNames = { "SMOOTH", "BALANCED", "SNAPPY" };
        private static readonly string[] AnchorNames = { "WRIST", "PALM", "FLOATING" };

        private readonly ManualLogSource _logger;
        private readonly Dictionary<string, List<MenuItem>> _categories = new Dictionary<string, List<MenuItem>>();
        private readonly List<string> _categoryOrder = new List<string>();
        private readonly List<RenderedButton> _buttons = new List<RenderedButton>();
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();

        private GameObject? _root;
        private GameObject? _pointer;
        private LineRenderer? _pointerLine;
        private TextMesh? _tooltipText;
        private TextMesh? _performanceText;
        private TextMesh? _inputText;
        private InputDevice _leftHand;
        private InputDevice _rightHand;
        private InputDevice _head;
        private string _activeCategory = "Home";
        private int _page;
        private bool _isOpen;
        private bool _desktopOpen;
        private bool _lastTrigger;
        private bool _haptics = true;
        private bool _reducedMotion;
        private bool _highContrast;
        private bool _largeText;
        private bool _showTooltips = true;
        private bool _softGlow = true;
        private bool _buttonPulse = true;
        private bool _minimalMode;
        private bool _performanceHud;
        private bool _inputMonitor;
        private bool _leftHandPointer;
        private bool _rememberPage = true;
        private int _themeIndex;
        private int _menuScaleIndex = 1;
        private int _pointerLengthIndex = 1;
        private int _followSpeedIndex = 1;
        private int _anchorIndex;
        private float _nextDeviceRefresh;
        private float _nextStatusRefresh;
        private float _smoothedFps;
        private float _toastUntil;
        private RenderedButton? _hovered;

        public MaytrixMenu(ManualLogSource logger)
        {
            _logger = logger;
            CreateData();
            LoadPreferences();
            SyncToggleStates();
            CreateRoot();
            RefreshDevices();
            SetOpen(false);
        }

        public void Tick()
        {
            if (_root == null)
                return;

            if (Time.unscaledTime >= _nextDeviceRefresh)
            {
                RefreshDevices();
                _nextDeviceRefresh = Time.unscaledTime + 2f;
            }

            if (Input.GetKeyDown(KeyCode.F6))
                _desktopOpen = !_desktopOpen;

            bool controllerHeld = ReadButton(_leftHand, CommonUsages.primaryButton)
                                  || ReadButton(_leftHand, CommonUsages.menuButton);
            bool shouldBeOpen = controllerHeld || _desktopOpen;
            if (shouldBeOpen != _isOpen)
                SetOpen(shouldBeOpen);

            if (!_isOpen)
                return;

            UpdatePose();
            UpdatePointer();
            AnimateHoveredButton();
            UpdateLiveStatus();
        }

        public void Dispose()
        {
            if (_root != null)
                UnityEngine.Object.Destroy(_root);
            if (_pointer != null)
                UnityEngine.Object.Destroy(_pointer);

            foreach (Material material in _materials.Values)
            {
                if (material != null)
                    UnityEngine.Object.Destroy(material);
            }

            _buttons.Clear();
            _materials.Clear();
        }

        private Palette CurrentPalette => Palettes[Mathf.Clamp(_themeIndex, 0, Palettes.Length - 1)];

        private void CreateData()
        {
            MenuItem performanceHud = new MenuItem("Performance HUD", "Show a live local FPS and frame-time readout.", MenuItemKind.Toggle, item => { _performanceHud = item.Enabled; SavePreferences(); });
            MenuItem inputMonitor = new MenuItem("Input Monitor", "Show live left and right controller button states.", MenuItemKind.Toggle, item => { _inputMonitor = item.Enabled; SavePreferences(); });
            MenuItem highContrast = new MenuItem("High Contrast", "Use a black panel and brighter card separation.", MenuItemKind.Toggle, item => { _highContrast = item.Enabled; SavePreferences(); });
            MenuItem largeText = new MenuItem("Large Text", "Increase card text size for easier reading.", MenuItemKind.Toggle, item => { _largeText = item.Enabled; SavePreferences(); });
            MenuItem tooltips = new MenuItem("Tooltips", "Show a description when the pointer rests on a card.", MenuItemKind.Toggle, item => { _showTooltips = item.Enabled; SavePreferences(); });
            MenuItem softGlow = new MenuItem("Soft Glow", "Use a wider two-color pointer and accent rail.", MenuItemKind.Toggle, item => { _softGlow = item.Enabled; SavePreferences(); UpdatePointerStyle(); });
            MenuItem minimalMode = new MenuItem("Minimal Mode", "Hide secondary branding and status text.", MenuItemKind.Toggle, item => { _minimalMode = item.Enabled; SavePreferences(); });
            MenuItem haptics = new MenuItem("Haptics", "Pulse the pointing controller after a successful press.", MenuItemKind.Toggle, item => { _haptics = item.Enabled; SavePreferences(); });
            MenuItem reducedMotion = new MenuItem("Reduced Motion", "Disable hover movement and snap the panel into place.", MenuItemKind.Toggle, item => { _reducedMotion = item.Enabled; SavePreferences(); });
            MenuItem buttonPulse = new MenuItem("Button Pulse", "Gently raise the card under the pointer.", MenuItemKind.Toggle, item => { _buttonPulse = item.Enabled; SavePreferences(); });
            MenuItem leftPointer = new MenuItem("Left-Hand Pointer", "Move the selection beam from the right controller to the left.", MenuItemKind.Toggle, item => { _leftHandPointer = item.Enabled; _lastTrigger = false; SavePreferences(); });
            MenuItem rememberPage = new MenuItem("Remember Page", "Restore the last category when Maytrix loads again.", MenuItemKind.Toggle, item => { _rememberPage = item.Enabled; SavePreferences(); });
            MenuItem comfortPreset = new MenuItem("Comfort Preset", "Apply reduced motion, large text, high contrast, and a steady pointer.", MenuItemKind.Action, _ => ApplyComfortPreset());
            MenuItem resetAll = new MenuItem("Reset All", "Restore every Maytrix interface option to its default.", MenuItemKind.Action, _ => ResetInterface());

            AddCategory("Home",
                new MenuItem("Quick Settings", "Open the interface settings category.", MenuItemKind.Action, _ => GoToCategory("Interface")),
                performanceHud,
                inputMonitor,
                comfortPreset,
                resetAll);

            AddCategory("Interface",
                highContrast,
                largeText,
                tooltips,
                softGlow,
                minimalMode,
                rememberPage,
                new MenuItem("Theme", "Cycle between original Maytrix color palettes.", MenuItemKind.Action, _ => CycleTheme(), () => CurrentPalette.Name));

            AddCategory("Controls",
                haptics,
                leftPointer,
                buttonPulse,
                new MenuItem("Pointer Length", "Cycle the controller selection beam distance.", MenuItemKind.Action, _ => CyclePointerLength(), () => PointerLengthNames[_pointerLengthIndex]),
                new MenuItem("Menu Size", "Cycle the world-space panel size.", MenuItemKind.Action, _ => CycleMenuScale(), () => MenuScaleNames[_menuScaleIndex]),
                new MenuItem("Follow Speed", "Cycle how quickly the panel follows the controller.", MenuItemKind.Action, _ => CycleFollowSpeed(), () => FollowSpeedNames[_followSpeedIndex]),
                new MenuItem("Anchor", "Cycle the panel offset around the left controller.", MenuItemKind.Action, _ => CycleAnchor(), () => AnchorNames[_anchorIndex]));

            AddCategory("Access", reducedMotion, highContrast, largeText, haptics, comfortPreset, resetAll);

            AddCategory("Community",
                new MenuItem("Discord", "Open the configured Maytrix Mods Discord route.", MenuItemKind.Link, _ => OpenUrl(PluginInfo.DiscordUrl)),
                new MenuItem("GitHub", "Open the Maytrix Mods source and releases page.", MenuItemKind.Link, _ => OpenUrl(PluginInfo.RepositoryUrl)),
                new MenuItem("Controls Guide", "Hold the left primary/menu button, point, then press trigger.", MenuItemKind.Action),
                new MenuItem("Credits", "Original Maytrix interface by the Maytrix Mods project.", MenuItemKind.Action),
                new MenuItem("Version", $"Maytrix Mod Menu {PluginInfo.Version}.", MenuItemKind.Action, null, () => $"v{PluginInfo.Version}"));
        }

        private void AddCategory(string name, params MenuItem[] items)
        {
            _categoryOrder.Add(name);
            _categories[name] = items.ToList();
        }

        private void CreateRoot()
        {
            _root = new GameObject("Maytrix Mod Menu");
            BuildMenu();
            _pointer = new GameObject("Maytrix Pointer");
            _pointerLine = _pointer.AddComponent<LineRenderer>();
            _pointerLine.positionCount = 2;
            _pointerLine.useWorldSpace = true;
            UpdatePointerStyle();
        }

        private void BuildMenu()
        {
            if (_root == null)
                return;

            foreach (Transform child in _root.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.Destroy(child.gameObject);

            _buttons.Clear();
            _hovered = null;
            Palette palette = CurrentPalette;
            Color background = _highContrast ? Color.black : palette.Background;
            Color surface = _highContrast ? new Color(0.025f, 0.025f, 0.030f, 1f) : palette.Surface;

            CreateBlock(_root.transform, "Panel", Vector3.zero, new Vector3(PanelWidth, PanelHeight, PanelDepth), background, false);
            if (_softGlow)
            {
                CreateBlock(_root.transform, "Glow Rail", new Vector3(0f, PanelHeight * 0.5f - 0.0035f, 0.007f), new Vector3(PanelWidth - 0.008f, 0.010f, 0.004f), palette.AccentTwo, false);
            }
            CreateBlock(_root.transform, "Top Accent", new Vector3(0f, PanelHeight * 0.5f - 0.005f, 0.010f), new Vector3(PanelWidth - 0.018f, 0.005f, 0.006f), palette.Accent, false);

            CreateText(_root.transform, "MAYTRIX", new Vector3(-0.258f, 0.181f, 0.010f), 74, 0.00225f, TextAnchor.MiddleLeft, _highContrast ? Color.white : TextPrimary);
            if (!_minimalMode)
                CreateText(_root.transform, "MOD MENU", new Vector3(-0.258f, 0.151f, 0.010f), 40, 0.00175f, TextAnchor.MiddleLeft, palette.Accent);

            string performanceValue = _performanceHud ? "-- FPS  /  -- MS" : $"v{PluginInfo.Version}";
            _performanceText = CreateText(_root.transform, performanceValue, new Vector3(0.258f, 0.171f, 0.010f), 31, 0.00145f, TextAnchor.MiddleRight, _performanceHud ? palette.Accent : TextMuted);

            CreateBlock(_root.transform, "Sidebar", new Vector3(-0.218f, -0.010f, 0.008f), new Vector3(0.124f, 0.315f, 0.008f), surface, false);
            for (int index = 0; index < _categoryOrder.Count; index++)
            {
                string category = _categoryOrder[index];
                float y = 0.095f - index * 0.050f;
                CreateButton(category, category, new Vector3(-0.218f, y, 0.015f), new Vector3(0.108f, 0.039f, 0.012f), true, category == _activeCategory);
            }

            CreateText(_root.transform, _activeCategory.ToUpperInvariant(), new Vector3(-0.140f, 0.119f, 0.010f), 48, 0.00185f, TextAnchor.MiddleLeft, TextPrimary);
            List<MenuItem> items = _categories[_activeCategory];
            int pageCount = Math.Max(1, Mathf.CeilToInt(items.Count / (float)ItemsPerPage));
            _page = Mathf.Clamp(_page, 0, pageCount - 1);
            int start = _page * ItemsPerPage;

            for (int slot = 0; slot < ItemsPerPage; slot++)
            {
                int itemIndex = start + slot;
                if (itemIndex >= items.Count)
                    break;

                int column = slot % 2;
                int row = slot / 2;
                float x = -0.057f + column * 0.177f;
                float y = 0.069f - row * 0.059f;
                MenuItem item = items[itemIndex];
                CreateButton(item.DisplayLabel, item, new Vector3(x, y, 0.015f), new Vector3(0.163f, 0.049f, 0.012f), false, item.Kind == MenuItemKind.Toggle && item.Enabled);
            }

            if (pageCount > 1)
            {
                CreateButton("<", "PreviousPage", new Vector3(-0.112f, -0.166f, 0.015f), new Vector3(0.043f, 0.029f, 0.012f), false, false);
                CreateButton(">", "NextPage", new Vector3(0.211f, -0.166f, 0.015f), new Vector3(0.043f, 0.029f, 0.012f), false, false);
            }

            CreateText(_root.transform, $"{_page + 1} / {pageCount}", new Vector3(0.050f, -0.166f, 0.010f), 32, 0.00145f, TextAnchor.MiddleCenter, TextMuted);
            _tooltipText = CreateText(_root.transform, DefaultTooltip(), new Vector3(-0.258f, -0.204f, 0.010f), 25, 0.00115f, TextAnchor.MiddleLeft, TextMuted);
            string inputValue = _inputMonitor ? "L: - - -   R: - - -" : (_minimalMode ? string.Empty : "LOCAL UI  /  PRIVATE & MODDED PLAY");
            _inputText = CreateText(_root.transform, inputValue, new Vector3(0.258f, -0.204f, 0.010f), 23, 0.00105f, TextAnchor.MiddleRight, _inputMonitor ? palette.AccentTwo : palette.Accent);

            ApplyRootScale();
            _root.SetActive(_isOpen);
        }

        private void CreateButton(string label, object payload, Vector3 localPosition, Vector3 size, bool isCategory, bool selected)
        {
            if (_root == null)
                return;

            Palette palette = CurrentPalette;
            Color idle = _highContrast ? new Color(0.085f, 0.085f, 0.095f, 1f) : palette.SurfaceRaised;
            Color color = selected ? palette.Accent : idle;
            GameObject buttonObject = CreateBlock(_root.transform, $"Button - {label}", localPosition, size, color, true);
            buttonObject.layer = MenuLayer;

            Color labelColor = selected ? new Color(0.01f, 0.03f, 0.06f, 1f) : TextPrimary;
            int fontSize = isCategory ? (_largeText ? 34 : 31) : (_largeText ? 33 : 29);
            float characterSize = isCategory ? (_largeText ? 0.00155f : 0.00142f) : (_largeText ? 0.00148f : 0.00130f);
            TextMesh labelText = CreateText(_root.transform, label, localPosition + new Vector3(0f, 0f, size.z * 0.65f), fontSize, characterSize, TextAnchor.MiddleCenter, labelColor);
            BoxCollider? collider = buttonObject.GetComponent<BoxCollider>();
            if (collider != null)
                _buttons.Add(new RenderedButton(buttonObject, collider, labelText, payload, isCategory, selected, size));
        }

        private GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 size, Color color, bool keepCollider)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localRotation = Quaternion.identity;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color);
            BoxCollider? collider = block.GetComponent<BoxCollider>();
            if (!keepCollider && collider != null)
                UnityEngine.Object.Destroy(collider);
            return block;
        }

        private TextMesh CreateText(Transform parent, string value, Vector3 localPosition, int fontSize, float characterSize, TextAnchor anchor, Color color)
        {
            GameObject textObject = new GameObject($"Text - {value}");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = Quaternion.identity;
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = fontSize;
            text.characterSize = characterSize;
            text.anchor = anchor;
            text.alignment = anchor is TextAnchor.MiddleLeft or TextAnchor.UpperLeft or TextAnchor.LowerLeft
                ? TextAlignment.Left
                : anchor is TextAnchor.MiddleRight or TextAnchor.UpperRight or TextAnchor.LowerRight
                    ? TextAlignment.Right
                    : TextAlignment.Center;
            text.color = color;
            text.richText = false;
            return text;
        }

        private Material CreateMaterial(Color color)
        {
            if (_materials.TryGetValue(color, out Material material))
                return material;
            Shader? shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("Maytrix could not find a compatible Unity shader.");
            material = new Material(shader) { color = color };
            _materials[color] = material;
            return material;
        }

        private void RefreshDevices()
        {
            _leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            _rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            _head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        }

        private void UpdatePose()
        {
            if (_root == null)
                return;
            Camera? camera = Camera.main;
            if (TryGetWorldPose(_leftHand, out Vector3 leftPosition, out Quaternion leftRotation) && camera != null)
            {
                Vector3[] offsets = { new Vector3(0.09f, 0.10f, 0.08f), new Vector3(0.13f, 0.045f, 0.14f), new Vector3(0.02f, 0.18f, 0.22f) };
                Vector3 targetPosition = leftPosition + leftRotation * offsets[Mathf.Clamp(_anchorIndex, 0, offsets.Length - 1)];
                Quaternion targetRotation = Quaternion.LookRotation(camera.transform.position - targetPosition, Vector3.up);
                float followSpeed = FollowSpeeds[Mathf.Clamp(_followSpeedIndex, 0, FollowSpeeds.Length - 1)];
                float smoothing = _reducedMotion ? 1f : 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
                _root.transform.position = Vector3.Lerp(_root.transform.position, targetPosition, smoothing);
                _root.transform.rotation = Quaternion.Slerp(_root.transform.rotation, targetRotation, smoothing);
            }
            else if (camera != null)
            {
                _root.transform.position = camera.transform.position + camera.transform.forward * 0.65f;
                _root.transform.rotation = Quaternion.LookRotation(camera.transform.position - _root.transform.position, Vector3.up);
            }
        }

        private void UpdatePointer()
        {
            if (_pointerLine == null)
                return;
            InputDevice pointerDevice = _leftHandPointer ? _leftHand : _rightHand;
            float pointerLength = PointerLengths[Mathf.Clamp(_pointerLengthIndex, 0, PointerLengths.Length - 1)];
            if (!TryGetWorldPose(pointerDevice, out Vector3 position, out Quaternion rotation))
            {
                _pointerLine.enabled = false;
                _hovered = null;
                return;
            }

            _pointerLine.enabled = true;
            Vector3 direction = rotation * Vector3.forward;
            Vector3 end = position + direction * pointerLength;
            _hovered = null;
            if (Physics.Raycast(position, direction, out RaycastHit hit, pointerLength, 1 << MenuLayer, QueryTriggerInteraction.Ignore))
            {
                RenderedButton? button = _buttons.FirstOrDefault(candidate => candidate.Collider == hit.collider);
                if (button != null)
                {
                    _hovered = button;
                    end = hit.point;
                    if (_showTooltips && Time.unscaledTime >= _toastUntil)
                        SetTooltip(GetDescription(button));
                }
            }
            else if (Time.unscaledTime >= _toastUntil)
            {
                SetTooltip(DefaultTooltip());
            }

            _pointerLine.SetPosition(0, position);
            _pointerLine.SetPosition(1, end);
            bool trigger = ReadButton(pointerDevice, CommonUsages.triggerButton);
            if (trigger && !_lastTrigger && _hovered != null)
                Press(_hovered, pointerDevice);
            _lastTrigger = trigger;
        }

        private void AnimateHoveredButton()
        {
            foreach (RenderedButton button in _buttons)
            {
                bool animate = button == _hovered && _buttonPulse && !_reducedMotion;
                float targetDepth = animate ? 0.020f : 0.015f;
                Vector3 localPosition = button.Root.transform.localPosition;
                localPosition.z = Mathf.Lerp(localPosition.z, targetDepth, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
                button.Root.transform.localPosition = localPosition;
                Vector3 labelPosition = button.Label.transform.localPosition;
                labelPosition.z = localPosition.z + button.BaseSize.z * 0.65f;
                button.Label.transform.localPosition = labelPosition;
            }
        }

        private void UpdateLiveStatus()
        {
            float delta = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float currentFps = 1f / delta;
            _smoothedFps = _smoothedFps <= 0f ? currentFps : Mathf.Lerp(_smoothedFps, currentFps, 0.08f);
            if (Time.unscaledTime < _nextStatusRefresh)
                return;
            _nextStatusRefresh = Time.unscaledTime + 0.20f;

            if (_performanceText != null)
                _performanceText.text = _performanceHud ? $"{Mathf.RoundToInt(_smoothedFps)} FPS  /  {delta * 1000f:0.0} MS" : $"v{PluginInfo.Version}";
            if (_inputText != null)
                _inputText.text = _inputMonitor ? $"L:{InputMarks(_leftHand)}   R:{InputMarks(_rightHand)}" : (_minimalMode ? string.Empty : "LOCAL UI  /  PRIVATE & MODDED PLAY");
        }

        private static string InputMarks(InputDevice device)
        {
            string primary = ReadButton(device, CommonUsages.primaryButton) ? "P" : "-";
            string trigger = ReadButton(device, CommonUsages.triggerButton) ? "T" : "-";
            string grip = ReadButton(device, CommonUsages.gripButton) ? "G" : "-";
            return primary + trigger + grip;
        }

        private void Press(RenderedButton button, InputDevice pointerDevice)
        {
            PulseHaptics(pointerDevice);
            if (button.Payload is string command)
            {
                if (button.IsCategory && _categories.ContainsKey(command))
                {
                    _activeCategory = command;
                    _page = 0;
                    SavePreferences();
                    Rebuild();
                    ShowToast($"Opened {command}");
                    return;
                }
                if (command == "PreviousPage")
                {
                    _page = Math.Max(0, _page - 1);
                    SavePreferences();
                    Rebuild();
                    ShowToast("Previous page");
                    return;
                }
                if (command == "NextPage")
                {
                    int pageCount = Math.Max(1, Mathf.CeilToInt(_categories[_activeCategory].Count / (float)ItemsPerPage));
                    _page = Math.Min(pageCount - 1, _page + 1);
                    SavePreferences();
                    Rebuild();
                    ShowToast("Next page");
                    return;
                }
            }

            if (button.Payload is MenuItem item)
            {
                item.Press();
                _logger.LogInfo($"Menu item pressed: {item.Label}");
                if (item.Kind == MenuItemKind.Toggle)
                    Rebuild();
                string? value = item.ValueProvider?.Invoke();
                if (item.Kind == MenuItemKind.Toggle)
                    ShowToast($"{item.Label}: {(item.Enabled ? "ON" : "OFF")}");
                else if (!string.IsNullOrWhiteSpace(value))
                    ShowToast($"{item.Label}: {value}");
                else
                    ShowToast(item.Description);
            }
        }

        private string GetDescription(RenderedButton button)
        {
            if (button.Payload is MenuItem item)
            {
                string? value = item.ValueProvider?.Invoke();
                return string.IsNullOrWhiteSpace(value) ? item.Description : $"{item.Description} [{value}]";
            }
            if (button.Payload is string category && button.IsCategory)
                return $"Open the {category} category";
            string? command = button.Payload as string;
            return command == "PreviousPage" ? "Previous page" : command == "NextPage" ? "Next page" : "Maytrix Mod Menu";
        }

        private string DefaultTooltip()
        {
            if (!_showTooltips)
                return "TOOLTIPS OFF";
            return _leftHandPointer ? "POINT WITH LEFT  /  PRESS LEFT TRIGGER" : "POINT WITH RIGHT  /  PRESS RIGHT TRIGGER";
        }

        private void SetTooltip(string value)
        {
            if (_tooltipText != null)
                _tooltipText.text = value.Length > 68 ? value.Substring(0, 65) + "..." : value;
        }

        private void ShowToast(string value)
        {
            _toastUntil = Time.unscaledTime + 2.4f;
            SetTooltip(value);
        }

        private void Rebuild()
        {
            BuildMenu();
            UpdatePointerStyle();
        }

        private void GoToCategory(string category)
        {
            if (!_categories.ContainsKey(category))
                return;
            _activeCategory = category;
            _page = 0;
            SavePreferences();
            Rebuild();
        }

        private void ApplyComfortPreset()
        {
            _reducedMotion = true;
            _highContrast = true;
            _largeText = true;
            _softGlow = false;
            _buttonPulse = false;
            _followSpeedIndex = 2;
            SyncToggleStates();
            SavePreferences();
            Rebuild();
        }

        private void ResetInterface()
        {
            _activeCategory = "Home";
            _page = 0;
            _haptics = true;
            _reducedMotion = false;
            _highContrast = false;
            _largeText = false;
            _showTooltips = true;
            _softGlow = true;
            _buttonPulse = true;
            _minimalMode = false;
            _performanceHud = false;
            _inputMonitor = false;
            _leftHandPointer = false;
            _rememberPage = true;
            _themeIndex = 0;
            _menuScaleIndex = 1;
            _pointerLengthIndex = 1;
            _followSpeedIndex = 1;
            _anchorIndex = 0;
            SyncToggleStates();
            SavePreferences();
            Rebuild();
        }

        private void CycleTheme() { _themeIndex = (_themeIndex + 1) % Palettes.Length; SavePreferences(); Rebuild(); }
        private void CycleMenuScale() { _menuScaleIndex = (_menuScaleIndex + 1) % MenuScales.Length; SavePreferences(); Rebuild(); }
        private void CyclePointerLength() { _pointerLengthIndex = (_pointerLengthIndex + 1) % PointerLengths.Length; SavePreferences(); Rebuild(); }
        private void CycleFollowSpeed() { _followSpeedIndex = (_followSpeedIndex + 1) % FollowSpeeds.Length; SavePreferences(); Rebuild(); }
        private void CycleAnchor() { _anchorIndex = (_anchorIndex + 1) % AnchorNames.Length; SavePreferences(); Rebuild(); }

        private void OpenUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
                Application.OpenURL(url);
        }

        private void PulseHaptics(InputDevice device)
        {
            if (_haptics && device.isValid)
                device.SendHapticImpulse(0u, 0.28f, 0.055f);
        }

        private void UpdatePointerStyle()
        {
            if (_pointerLine == null)
                return;
            Palette palette = CurrentPalette;
            _pointerLine.sharedMaterial = CreateMaterial(palette.Accent);
            _pointerLine.startWidth = _softGlow ? 0.0045f : 0.0025f;
            _pointerLine.endWidth = _softGlow ? 0.0015f : 0.0008f;
            _pointerLine.startColor = palette.Accent;
            Color endColor = _softGlow ? palette.AccentTwo : palette.Accent;
            endColor.a = 0.12f;
            _pointerLine.endColor = endColor;
        }

        private void ApplyRootScale()
        {
            if (_root == null)
                return;
            float scale = MenuScales[Mathf.Clamp(_menuScaleIndex, 0, MenuScales.Length - 1)];
            _root.transform.localScale = Vector3.one * scale;
        }

        private void LoadPreferences()
        {
            _haptics = ReadPreference("Haptics", true);
            _reducedMotion = ReadPreference("ReducedMotion", false);
            _highContrast = ReadPreference("HighContrast", false);
            _largeText = ReadPreference("LargeText", false);
            _showTooltips = ReadPreference("Tooltips", true);
            _softGlow = ReadPreference("SoftGlow", true);
            _buttonPulse = ReadPreference("ButtonPulse", true);
            _minimalMode = ReadPreference("MinimalMode", false);
            _performanceHud = ReadPreference("PerformanceHud", false);
            _inputMonitor = ReadPreference("InputMonitor", false);
            _leftHandPointer = ReadPreference("LeftHandPointer", false);
            _rememberPage = ReadPreference("RememberPage", true);
            _themeIndex = Mathf.Clamp(PlayerPrefs.GetInt(PreferencePrefix + "Theme", 0), 0, Palettes.Length - 1);
            _menuScaleIndex = Mathf.Clamp(PlayerPrefs.GetInt(PreferencePrefix + "MenuScale", 1), 0, MenuScales.Length - 1);
            _pointerLengthIndex = Mathf.Clamp(PlayerPrefs.GetInt(PreferencePrefix + "PointerLength", 1), 0, PointerLengths.Length - 1);
            _followSpeedIndex = Mathf.Clamp(PlayerPrefs.GetInt(PreferencePrefix + "FollowSpeed", 1), 0, FollowSpeeds.Length - 1);
            _anchorIndex = Mathf.Clamp(PlayerPrefs.GetInt(PreferencePrefix + "Anchor", 0), 0, AnchorNames.Length - 1);
            if (_rememberPage)
            {
                string savedCategory = PlayerPrefs.GetString(PreferencePrefix + "Category", "Home");
                _activeCategory = _categories.ContainsKey(savedCategory) ? savedCategory : "Home";
                _page = Math.Max(0, PlayerPrefs.GetInt(PreferencePrefix + "Page", 0));
            }
        }

        private void SavePreferences()
        {
            WritePreference("Haptics", _haptics);
            WritePreference("ReducedMotion", _reducedMotion);
            WritePreference("HighContrast", _highContrast);
            WritePreference("LargeText", _largeText);
            WritePreference("Tooltips", _showTooltips);
            WritePreference("SoftGlow", _softGlow);
            WritePreference("ButtonPulse", _buttonPulse);
            WritePreference("MinimalMode", _minimalMode);
            WritePreference("PerformanceHud", _performanceHud);
            WritePreference("InputMonitor", _inputMonitor);
            WritePreference("LeftHandPointer", _leftHandPointer);
            WritePreference("RememberPage", _rememberPage);
            PlayerPrefs.SetInt(PreferencePrefix + "Theme", _themeIndex);
            PlayerPrefs.SetInt(PreferencePrefix + "MenuScale", _menuScaleIndex);
            PlayerPrefs.SetInt(PreferencePrefix + "PointerLength", _pointerLengthIndex);
            PlayerPrefs.SetInt(PreferencePrefix + "FollowSpeed", _followSpeedIndex);
            PlayerPrefs.SetInt(PreferencePrefix + "Anchor", _anchorIndex);
            if (_rememberPage)
            {
                PlayerPrefs.SetString(PreferencePrefix + "Category", _activeCategory);
                PlayerPrefs.SetInt(PreferencePrefix + "Page", _page);
            }
            else
            {
                PlayerPrefs.DeleteKey(PreferencePrefix + "Category");
                PlayerPrefs.DeleteKey(PreferencePrefix + "Page");
            }
            PlayerPrefs.Save();
        }

        private void SyncToggleStates()
        {
            SetToggleState("Haptics", _haptics);
            SetToggleState("Reduced Motion", _reducedMotion);
            SetToggleState("High Contrast", _highContrast);
            SetToggleState("Large Text", _largeText);
            SetToggleState("Tooltips", _showTooltips);
            SetToggleState("Soft Glow", _softGlow);
            SetToggleState("Button Pulse", _buttonPulse);
            SetToggleState("Minimal Mode", _minimalMode);
            SetToggleState("Performance HUD", _performanceHud);
            SetToggleState("Input Monitor", _inputMonitor);
            SetToggleState("Left-Hand Pointer", _leftHandPointer);
            SetToggleState("Remember Page", _rememberPage);
        }

        private void SetToggleState(string label, bool value)
        {
            foreach (MenuItem item in _categories.Values.SelectMany(items => items).Distinct())
            {
                if (item.Label == label)
                    item.Enabled = value;
            }
        }

        private static bool ReadPreference(string key, bool defaultValue) => PlayerPrefs.GetInt(PreferencePrefix + key, defaultValue ? 1 : 0) != 0;
        private static void WritePreference(string key, bool value) => PlayerPrefs.SetInt(PreferencePrefix + key, value ? 1 : 0);
        private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage) => device.isValid && device.TryGetFeatureValue(usage, out bool pressed) && pressed;

        private bool TryGetWorldPose(InputDevice device, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (!device.isValid)
                return false;
            bool hasPosition = device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPosition);
            bool hasRotation = device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRotation);
            if (!hasPosition || !hasRotation)
                return false;

            Camera? camera = Camera.main;
            bool hasHeadPosition = _head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition);
            bool hasHeadRotation = _head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation);
            if (camera != null && _head.isValid && hasHeadPosition && hasHeadRotation)
            {
                Quaternion trackingRotation = camera.transform.rotation * Quaternion.Inverse(headRotation);
                Vector3 trackingPosition = camera.transform.position - trackingRotation * headPosition;
                position = trackingPosition + trackingRotation * localPosition;
                rotation = trackingRotation * localRotation;
                return true;
            }

            Transform? trackingParent = camera != null ? camera.transform.parent : null;
            if (trackingParent != null)
            {
                position = trackingParent.TransformPoint(localPosition);
                rotation = trackingParent.rotation * localRotation;
            }
            else
            {
                position = localPosition;
                rotation = localRotation;
            }
            return true;
        }

        private void SetOpen(bool value)
        {
            _isOpen = value;
            if (_root != null)
                _root.SetActive(value);
            if (_pointer != null)
                _pointer.SetActive(value);
            if (!value)
            {
                _hovered = null;
                _lastTrigger = false;
            }
        }

        private sealed class Palette
        {
            public Palette(string name, Color background, Color surface, Color surfaceRaised, Color accent, Color accentTwo)
            {
                Name = name;
                Background = background;
                Surface = surface;
                SurfaceRaised = surfaceRaised;
                Accent = accent;
                AccentTwo = accentTwo;
            }
            public string Name { get; }
            public Color Background { get; }
            public Color Surface { get; }
            public Color SurfaceRaised { get; }
            public Color Accent { get; }
            public Color AccentTwo { get; }
        }

        private sealed class RenderedButton
        {
            public RenderedButton(GameObject root, BoxCollider collider, TextMesh label, object payload, bool isCategory, bool selected, Vector3 baseSize)
            {
                Root = root;
                Collider = collider;
                Label = label;
                Payload = payload;
                IsCategory = isCategory;
                Selected = selected;
                BaseSize = baseSize;
            }
            public GameObject Root { get; }
            public BoxCollider Collider { get; }
            public TextMesh Label { get; }
            public object Payload { get; }
            public bool IsCategory { get; }
            public bool Selected { get; }
            public Vector3 BaseSize { get; }
        }
    }
}
