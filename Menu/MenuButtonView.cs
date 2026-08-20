using UnityEngine;

namespace Maytrix.Menu.Menu
{
    internal sealed class MenuButtonView : MonoBehaviour
    {
        private Renderer? _renderer;
        private MaterialPropertyBlock? _colorBlock;
        private TextMesh? _label;
        private MenuItem? _item;
        private Color _normal;
        private Color _hover;
        private Color _disabled;
        private Color _normalText;
        private Color _disabledText;
        private bool _hovered;

        public string Description => _item?.Description ?? string.Empty;

        public void Initialize(MenuItem item, MenuTheme theme, float textScale)
        {
            _item = item;
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }

            _renderer = GetComponent<Renderer>();
            _colorBlock = new MaterialPropertyBlock();
            _normal = item.Kind == MenuItemKind.Navigation
                ? theme.Navigation
                : item.Kind == MenuItemKind.Destructive
                    ? theme.Danger
                    : item.Kind == MenuItemKind.Info
                        ? theme.Info
                        : theme.Button;
            _hover = theme.Hover;
            _disabled = theme.Info;
            _normalText = theme.Text;
            _disabledText = theme.Muted;

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.56f);
            textObject.transform.localRotation = Quaternion.identity;
            textObject.transform.localScale = new Vector3(
                SafeInverse(transform.localScale.x),
                SafeInverse(transform.localScale.y),
                SafeInverse(transform.localScale.z));

            _label = textObject.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 48;
            _label.characterSize = 0.0095f * textScale;
            _label.color = item.Enabled ? _normalText : _disabledText;
            Refresh();
        }

        public void Refresh()
        {
            if (_item == null)
            {
                return;
            }

            if (_label != null)
            {
                _label.text = Clip(_item.DisplayLabel, 27);
                _label.color = !_item.Enabled ? _disabledText : _hovered ? Color.black : _normalText;
            }

            SetColor(!_item.Enabled ? _disabled : _hovered ? _hover : _normal);
        }

        public void SetHovered(bool hovered)
        {
            if (_item == null)
            {
                return;
            }

            _hovered = hovered;
            SetColor(hovered && _item.Enabled ? _hover : _item.Enabled ? _normal : _disabled);
            if (_label != null)
            {
                _label.color = !_item.Enabled ? _disabledText : hovered ? Color.black : _normalText;
            }
        }

        public bool Activate()
        {
            if (_item?.Enabled == true)
            {
                _item.Activate();
                return true;
            }

            return false;
        }

        public bool HitTest(Ray ray, float maxDistance, out float distance, out Vector3 hitPoint)
        {
            var worldToLocal = transform.worldToLocalMatrix;
            var origin = worldToLocal.MultiplyPoint3x4(ray.origin);
            var direction = worldToLocal.MultiplyVector(ray.direction);
            var minimum = 0f;
            var maximum = maxDistance;

            if (!ClipAxis(origin.x, direction.x, ref minimum, ref maximum) ||
                !ClipAxis(origin.y, direction.y, ref minimum, ref maximum) ||
                !ClipAxis(origin.z, direction.z, ref minimum, ref maximum) ||
                maximum < 0f)
            {
                distance = 0f;
                hitPoint = default;
                return false;
            }

            distance = minimum >= 0f ? minimum : maximum;
            if (distance < 0f || distance > maxDistance)
            {
                hitPoint = default;
                return false;
            }

            hitPoint = ray.GetPoint(distance);
            return true;
        }

        private void SetColor(Color color)
        {
            if (_renderer == null || _colorBlock == null)
            {
                return;
            }

            _colorBlock.Clear();
            _colorBlock.SetColor("_Color", color);
            _colorBlock.SetColor("_BaseColor", color);
            _renderer.SetPropertyBlock(_colorBlock);
        }

        private static float SafeInverse(float value)
        {
            return Mathf.Abs(value) < 0.0001f ? 1f : 1f / value;
        }

        private static bool ClipAxis(float origin, float direction, ref float minimum, ref float maximum)
        {
            if (Mathf.Abs(direction) < 0.000001f)
            {
                return origin >= -0.5f && origin <= 0.5f;
            }

            var first = (-0.5f - origin) / direction;
            var second = (0.5f - origin) / direction;
            if (first > second)
            {
                var swap = first;
                first = second;
                second = swap;
            }

            minimum = Mathf.Max(minimum, first);
            maximum = Mathf.Min(maximum, second);
            return minimum <= maximum;
        }

        private static string Clip(string value, int maximumLength)
        {
            return value.Length <= maximumLength ? value : value.Substring(0, maximumLength - 3) + "...";
        }
    }
}
