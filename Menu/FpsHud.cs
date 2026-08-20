using System;
using Maytrix.Menu.Services;
using UnityEngine;

namespace Maytrix.Menu.Menu
{
    internal sealed class FpsHud : IDisposable
    {
        private readonly MenuSettings _settings;
        private readonly LocalGraphicsController _graphics;
        private GameObject? _root;
        private TextMesh? _text;
        private float _nextRefreshAt;

        public FpsHud(MenuSettings settings, LocalGraphicsController graphics)
        {
            _settings = settings;
            _graphics = graphics;
        }

        public void Tick()
        {
            if (!_settings.ShowFps.Value)
            {
                if (_root != null)
                {
                    _root.SetActive(false);
                }

                return;
            }

            EnsureCreated();
            if (_root == null)
            {
                return;
            }

            var camera = Camera.main;
            var shouldShow = camera != null;
            _root.SetActive(shouldShow);
            if (!shouldShow || camera == null)
            {
                return;
            }

            var targetPosition = camera.transform.position + camera.transform.rotation * new Vector3(-0.19f, 0.13f, 0.55f);
            var targetRotation = camera.transform.rotation;
            _root.transform.SetPositionAndRotation(targetPosition, targetRotation);

            if (_text != null && Time.unscaledTime >= _nextRefreshAt)
            {
                var theme = MenuTheme.Resolve(_settings.ThemeIndex.Value, _settings.AccentIndex.Value);
                _text.text = $"FPS {_graphics.FpsLabel}  |  {_graphics.FrameTimeLabel}";
                _text.color = theme.Accent;
                _nextRefreshAt = Time.unscaledTime + 0.25f;
            }
        }

        public void Dispose()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
                _text = null;
            }
        }

        private void EnsureCreated()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("MaytrixFpsHud")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityEngine.Object.DontDestroyOnLoad(_root);

            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Background";
            backing.transform.SetParent(_root.transform, false);
            backing.transform.localScale = new Vector3(0.19f, 0.032f, 0.004f);
            var collider = backing.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = backing.GetComponent<Renderer>();
            if (renderer != null)
            {
                var colorBlock = new MaterialPropertyBlock();
                colorBlock.SetColor("_Color", new Color(0.01f, 0.015f, 0.025f, 1f));
                colorBlock.SetColor("_BaseColor", new Color(0.01f, 0.015f, 0.025f, 1f));
                renderer.SetPropertyBlock(colorBlock);
            }

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(_root.transform, false);
            textObject.transform.localPosition = new Vector3(-0.085f, 0f, -0.003f);
            _text = textObject.AddComponent<TextMesh>();
            _text.anchor = TextAnchor.MiddleLeft;
            _text.alignment = TextAlignment.Left;
            _text.fontSize = 48;
            _text.characterSize = 0.0065f;
            _text.text = "FPS Warming up";
            _root.SetActive(false);
        }
    }
}
