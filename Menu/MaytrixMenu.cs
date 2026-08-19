using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace MaytrixMods
{
    /// <summary>
    /// Original world-space VR menu shell for Maytrix Mods.
    /// It intentionally contains only harmless UI/demo toggles.
    /// </summary>
    internal sealed class MaytrixMenu : IDisposable
    {
        private const float PanelWidth = 0.54f;
        private const float PanelHeight = 0.38f;
        private const float PanelDepth = 0.012f;
        private const int ItemsPerPage = 8;
        private const int MenuLayer = 5; // Unity's built-in UI layer.

        private static readonly Color Background = new(0.018f, 0.025f, 0.055f, 0.985f);
        private static readonly Color Surface = new(0.035f, 0.050f, 0.095f, 1f);
        private static readonly Color SurfaceRaised = new(0.055f, 0.072f, 0.125f, 1f);
        private static readonly Color Accent = new(0.18f, 0.72f, 1f, 1f);
        private static readonly Color AccentTwo = new(0.55f, 0.30f, 1f, 1f);
        private static readonly Color TextPrimary = new(0.95f, 0.98f, 1f, 1f);
        private static readonly Color TextMuted = new(0.55f, 0.64f, 0.76f, 1f);
        private static readonly Color Danger = new(1f, 0.28f, 0.36f, 1f);

        private readonly ManualLogSource _logger;
        private readonly Dictionary<string, List<MenuItem>> _categories = new();
        private readonly List<string> _categoryOrder = new();
        private readonly List<RenderedButton> _buttons = new();
        private readonly Dictionary<Color, Material> _materials = new();

        private GameObject? _root;
        private GameObject? _pointer;
        private LineRenderer? _pointerLine;
        private TextMesh? _tooltipText;
        private TextMesh? _pageText;

        private InputDevice _leftHand;
        private InputDevice _rightHand;
        private InputDevice _head;
        private string _activeCategory = "Home";
        private int _page;
        private bool _isOpen = true;
        private bool _lastMenuButton;
        private bool _lastTrigger;
        private bool _haptics = true;
        private bool _reducedMotion;
        private bool _highContrast;
        private bool _showTooltips = true;
        private float _nextDeviceRefresh;
        private RenderedButton? _hovered;

        public MaytrixMenu(ManualLogSource logger)
        {
            _logger = logger;
            CreateData();
            CreateRoot();
            RefreshDevices();
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

            bool menuButton = ReadButton(_leftHand, CommonUsages.primaryButton)
                              || ReadButton(_leftHand, CommonUsages.menuButton)
                              || Input.GetKeyDown(KeyCode.F6);

            if (menuButton && !_lastMenuButton)
                SetOpen(!_isOpen);

            _lastMenuButton = menuButton;

            if (!_isOpen)
                return;

            UpdatePose();
            UpdatePointer();
            AnimateHoveredButton();
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

        private void CreateData()
        {
            AddCategory("Home",
                new MenuItem("Quick Settings", "A clean starting area for your favorite Maytrix options."),
                new MenuItem("Performance HUD", "Reserved for a local performance display."),
                new MenuItem("Comfort Preset", "A placeholder preset for comfort-focused options."),
                new MenuItem("Reset Interface", "Restore the menu layout and visual settings.", MenuItemKind.Action, _ => ResetInterface()));

            AddCategory("Movement",
                new MenuItem("Smooth Turn Preview", "UI-only placeholder for a smooth-turn preference."),
                new MenuItem("Comfort Step", "UI-only placeholder for a comfort movement preference."),
                new MenuItem("Reduced Camera Motion", "Marks camera comfort as enabled in the menu."),
                new MenuItem("Left-Hand Layout", "Moves navigation concepts to a left-handed preset."),
                new MenuItem("Input Tester", "Reserved for displaying controller input states."));

            AddCategory("Visual",
                new MenuItem("High Contrast", "Boost menu contrast for easier reading.", MenuItemKind.Toggle, item =>
                {
                    _highContrast = item.Enabled;
                }),
                new MenuItem("Compact Labels", "A placeholder for shorter card labels."),
                new MenuItem("Performance HUD", "Reserved for FPS and frame-time information."),
                new MenuItem("Show Tooltips", "Display descriptions while pointing at menu cards.", MenuItemKind.Toggle,
                    item => _showTooltips = item.Enabled) { Enabled = true },
                new MenuItem("Soft Glow", "Enable the Maytrix cyan-violet accent glow.") { Enabled = true });

            AddCategory("Fun",
                new MenuItem("Party Accent", "Switch the menu to a brighter cosmetic accent."),
                new MenuItem("Button Pulse", "Gently animate the currently selected card."),
                new MenuItem("Random Theme", "Cycle to a random interface color preset.", MenuItemKind.Action, _ => RandomizeAccent()),
                new MenuItem("Minimal Mode", "Hide secondary interface text for a cleaner view."));

            AddCategory("Settings",
                new MenuItem("Haptics", "Vibrate the controller when a menu button is pressed.", MenuItemKind.Toggle, item => _haptics = item.Enabled) { Enabled = true },
                new MenuItem("Reduced Motion", "Disable hover movement in the interface.", MenuItemKind.Toggle, item => _reducedMotion = item.Enabled),
                new MenuItem("Menu Sounds", "Reserved for optional interface sounds."),
                new MenuItem("Remember Page", "Reserved for saving the last open category."),
                new MenuItem("Reset Interface", "Restore default visual and navigation settings.", MenuItemKind.Action, _ => ResetInterface()));

            AddCategory("Community",
                new MenuItem("Maytrix Mods Discord", "Open the configured Maytrix Mods Discord address.", MenuItemKind.Link, _ => OpenUrl(PluginInfo.DiscordUrl)),
                new MenuItem("GitHub Project", "Open the Maytrix Mods source repository.", MenuItemKind.Link, _ => OpenUrl(PluginInfo.RepositoryUrl)),
                new MenuItem("Credits", "Maytrix Mod Menu - original interface scaffold.", MenuItemKind.Action, _ => SetTooltip("Built for Maytrix Mods")),
                new MenuItem("Version", $"Current scaffold version: {PluginInfo.Version}", MenuItemKind.Action, _ => SetTooltip($"Maytrix {PluginInfo.Version}")));
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
            _pointerLine.startWidth = 0.003f;
            _pointerLine.endWidth = 0.001f;
            _pointerLine.sharedMaterial = CreateMaterial(Accent);
            _pointerLine.startColor = Accent;
            _pointerLine.endColor = new Color(Accent.r, Accent.g, Accent.b, 0.1f);
            _pointerLine.useWorldSpace = true;
        }

        private void BuildMenu()
        {
            if (_root == null)
                return;

            foreach (Transform child in _root.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.Destroy(child.gameObject);

            _buttons.Clear();

            CreateBlock(_root.transform, "Panel", Vector3.zero,
                new Vector3(PanelWidth, PanelHeight, PanelDepth), Background, false);

            CreateBlock(_root.transform, "Top Accent", new Vector3(0f, PanelHeight * 0.5f - 0.006f, 0.009f),
                new Vector3(PanelWidth - 0.018f, 0.006f, 0.006f), Accent, false);

            CreateText(_root.transform, "MAYTRIX", new Vector3(-0.245f, 0.158f, 0.010f),
                72, 0.0023f, TextAnchor.MiddleLeft, _highContrast ? Color.white : TextPrimary);
            CreateText(_root.transform, "MOD MENU", new Vector3(-0.245f, 0.129f, 0.010f),
                40, 0.0018f, TextAnchor.MiddleLeft, Accent);
            CreateText(_root.transform, $"v{PluginInfo.Version}", new Vector3(0.245f, 0.148f, 0.010f),
                34, 0.0016f, TextAnchor.MiddleRight, TextMuted);

            CreateBlock(_root.transform, "Sidebar", new Vector3(-0.207f, -0.015f, 0.008f),
                new Vector3(0.118f, 0.286f, 0.008f), Surface, false);

            for (int index = 0; index < _categoryOrder.Count; index++)
            {
                string category = _categoryOrder[index];
                float y = 0.095f - index * 0.042f;
                CreateButton(category, category, new Vector3(-0.207f, y, 0.015f),
                    new Vector3(0.103f, 0.034f, 0.012f), true, category == _activeCategory);
            }

            CreateText(_root.transform, _activeCategory.ToUpperInvariant(), new Vector3(-0.125f, 0.103f, 0.010f),
                48, 0.0019f, TextAnchor.MiddleLeft, TextPrimary);

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
                float x = -0.050f + column * 0.169f;
                float y = 0.061f - row * 0.057f;
                MenuItem item = items[itemIndex];

                CreateButton(item.Label, item, new Vector3(x, y, 0.015f),
                    new Vector3(0.156f, 0.047f, 0.012f), false, item.Enabled);
            }

            CreateButton("<", "PreviousPage", new Vector3(-0.105f, -0.154f, 0.015f),
                new Vector3(0.043f, 0.029f, 0.012f), false, false);
            CreateButton(">", "NextPage", new Vector3(0.202f, -0.154f, 0.015f),
                new Vector3(0.043f, 0.029f, 0.012f), false, false);

            _pageText = CreateText(_root.transform, $"{_page + 1} / {pageCount}", new Vector3(0.048f, -0.154f, 0.010f),
                34, 0.0016f, TextAnchor.MiddleCenter, TextMuted);

            string tooltip = _showTooltips ? "Point at a card to see details" : "Tooltips are off";
            _tooltipText = CreateText(_root.transform, tooltip, new Vector3(-0.245f, -0.181f, 0.010f),
                28, 0.00125f, TextAnchor.MiddleLeft, TextMuted);

            CreateText(_root.transform, "MAYTRIX MODS  /  COMMUNITY BUILD", new Vector3(0.245f, -0.181f, 0.010f),
                25, 0.00115f, TextAnchor.MiddleRight, AccentTwo);

            _root.SetActive(_isOpen);
        }

        private void CreateButton(
            string label,
            object payload,
            Vector3 localPosition,
            Vector3 size,
            bool isCategory,
            bool selected)
        {
            if (_root == null)
                return;

            Color color = selected
                ? Accent
                : _highContrast ? new Color(0.075f, 0.085f, 0.11f, 1f) : SurfaceRaised;
            GameObject buttonObject = CreateBlock(_root.transform, $"Button - {label}", localPosition, size, color, true);
            buttonObject.layer = MenuLayer;

            Color labelColor = selected ? new Color(0.01f, 0.03f, 0.06f, 1f) : TextPrimary;
            // Keep text outside the scaled cube hierarchy so its font size and
            // spacing are not distorted by the button's non-uniform scale.
            TextMesh labelText = CreateText(_root.transform, label,
                localPosition + new Vector3(0f, 0f, size.z * 0.65f), isCategory ? 31 : 29,
                isCategory ? 0.00145f : 0.00135f, TextAnchor.MiddleCenter, labelColor);

            _buttons.Add(new RenderedButton(buttonObject, buttonObject.GetComponent<BoxCollider>(), labelText,
                payload, isCategory, selected, size));
        }

        private GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 size,
            Color color,
            bool keepCollider)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localRotation = Quaternion.identity;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color);

            BoxCollider collider = block.GetComponent<BoxCollider>();
            if (!keepCollider && collider != null)
                UnityEngine.Object.Destroy(collider);

            return block;
        }

        private TextMesh CreateText(
            Transform parent,
            string value,
            Vector3 localPosition,
            int fontSize,
            float characterSize,
            TextAnchor anchor,
            Color color)
        {
            GameObject textObject = new($"Text - {value}");
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

            material = new Material(shader)
            {
                color = color
            };
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
                Vector3 targetPosition = leftPosition + leftRotation * new Vector3(0.09f, 0.10f, 0.08f);
                Quaternion targetRotation = Quaternion.LookRotation(camera.transform.position - targetPosition, Vector3.up);

                float smoothing = _reducedMotion ? 1f : 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
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

            if (!TryGetWorldPose(_rightHand, out Vector3 position, out Quaternion rotation))
            {
                _pointerLine.enabled = false;
                _hovered = null;
                return;
            }

            _pointerLine.enabled = true;
            Vector3 direction = rotation * Vector3.forward;
            Vector3 end = position + direction * 2f;
            _hovered = null;

            if (Physics.Raycast(position, direction, out RaycastHit hit, 2f, 1 << MenuLayer, QueryTriggerInteraction.Ignore))
            {
                RenderedButton? button = _buttons.FirstOrDefault(candidate => candidate.Collider == hit.collider);
                if (button != null)
                {
                    _hovered = button;
                    end = hit.point;
                    if (_showTooltips)
                        SetTooltip(GetDescription(button));
                }
            }

            _pointerLine.SetPosition(0, position);
            _pointerLine.SetPosition(1, end);

            bool trigger = ReadButton(_rightHand, CommonUsages.triggerButton);
            if (trigger && !_lastTrigger && _hovered != null)
                Press(_hovered);

            _lastTrigger = trigger;
        }

        private void AnimateHoveredButton()
        {
            foreach (RenderedButton button in _buttons)
            {
                float targetDepth = button == _hovered && !_reducedMotion ? 0.020f : 0.015f;
                Vector3 localPosition = button.Root.transform.localPosition;
                localPosition.z = Mathf.Lerp(localPosition.z, targetDepth, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
                button.Root.transform.localPosition = localPosition;

                Vector3 labelPosition = button.Label.transform.localPosition;
                labelPosition.z = localPosition.z + button.BaseSize.z * 0.65f;
                button.Label.transform.localPosition = labelPosition;
            }
        }

        private void Press(RenderedButton button)
        {
            PulseHaptics();

            if (button.Payload is string command)
            {
                if (button.IsCategory && _categories.ContainsKey(command))
                {
                    _activeCategory = command;
                    _page = 0;
                    Rebuild();
                    return;
                }

                if (command == "PreviousPage")
                {
                    _page = Math.Max(0, _page - 1);
                    Rebuild();
                    return;
                }

                if (command == "NextPage")
                {
                    int pageCount = Math.Max(1, Mathf.CeilToInt(_categories[_activeCategory].Count / (float)ItemsPerPage));
                    _page = Math.Min(pageCount - 1, _page + 1);
                    Rebuild();
                    return;
                }
            }

            if (button.Payload is MenuItem item)
            {
                item.Press();
                _logger.LogInfo($"Menu item pressed: {item.Label}");

                if (item.Kind == MenuItemKind.Toggle)
                    Rebuild();
                else
                    SetTooltip(item.Description);
            }
        }

        private string GetDescription(RenderedButton button)
        {
            return button.Payload switch
            {
                MenuItem item => item.Description,
                string category when button.IsCategory => $"Open the {category} category",
                "PreviousPage" => "Previous page",
                "NextPage" => "Next page",
                _ => "Maytrix Mod Menu"
            };
        }

        private void SetTooltip(string value)
        {
            if (_tooltipText != null)
                _tooltipText.text = value.Length > 58 ? value.Substring(0, 55) + "..." : value;
        }

        private void Rebuild()
        {
            BuildMenu();
        }

        private void ResetInterface()
        {
            _activeCategory = "Home";
            _page = 0;
            _highContrast = false;
            _reducedMotion = false;
            _haptics = true;
            _showTooltips = true;

            foreach (MenuItem item in _categories.Values.SelectMany(items => items))
                item.Enabled = item.Label is "Show Tooltips" or "Soft Glow" or "Haptics";

            Rebuild();
        }

        private void RandomizeAccent()
        {
            SetTooltip("Theme preview selected - connect this action to your theme system.");
        }

        private void OpenUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
                Application.OpenURL(url);
        }

        private void PulseHaptics()
        {
            if (_haptics && _rightHand.isValid)
                _rightHand.SendHapticImpulse(0u, 0.28f, 0.055f);
        }

        private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
        {
            return device.isValid && device.TryGetFeatureValue(usage, out bool pressed) && pressed;
        }

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
        }

        private sealed class RenderedButton
        {
            public RenderedButton(
                GameObject root,
                BoxCollider collider,
                TextMesh label,
                object payload,
                bool isCategory,
                bool selected,
                Vector3 baseSize)
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
