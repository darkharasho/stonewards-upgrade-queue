using UnityEngine;
using UnityEngine.UIElements;

namespace UpgradeQueue
{
    public enum CounterCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    /// <summary>
    /// Small "Upgrades: N" button added to the game's HUD document. Visible while upgrades are
    /// queued during gameplay or with the inventory open; clickable only where the game frees the
    /// cursor (the inventory).
    /// </summary>
    internal static class HudCounter
    {
        private const float Margin = 24f;
        // Kept tight to the screen edge so the counter clears the coin display under it.
        private const float EdgeMargin = 6f;

        private const float IconSize = 28f;
        private const string IconResource = "UpgradeQueue.counter-icon.png";

        private static Button _button;
        private static Label _label;
        private static Hud _attachedHud;
        private static Texture2D _icon;
        private static bool _iconLoadAttempted;

        public static void Update()
        {
            var hud = Hud.Instance;
            if (hud == null || hud.hudDocument == null)
                return;

            if (_button == null || _attachedHud != hud || _button.panel == null)
                Attach(hud);

            var state = InputManager.Instance != null ? InputManager.Instance.CurrentInputState : InputManager.InputState.None;
            var inGameplay = state == InputManager.InputState.Gameplay;
            var inInventory = state == InputManager.InputState.Inventory;
            var visible = Plugin.Enabled.Value && Plugin.ShowCounter.Value && QueueState.PendingCount > 0
                && !QueuedUpgradeSession.IsOpen && (inGameplay || inInventory);

            _button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                return;

            var text = inInventory
                ? $"Open upgrade ({QueueState.PendingCount})"
                : $"Upgrades: {QueueState.PendingCount}  [{Plugin.OpenQueueKey.Value}]";
            if (_label.text != text)
                _label.text = text;

            ApplyCorner(Plugin.CounterPosition.Value);
        }

        private static void Attach(Hud hud)
        {
            _button?.RemoveFromHierarchy();

            _button = new Button(() => QueuedUpgradeSession.RequestOpen()) { name = "UpgradeQueueCounter" };
            var s = _button.style;
            s.position = Position.Absolute;
            s.flexDirection = FlexDirection.Row;
            s.alignItems = Align.Center;
            s.paddingLeft = 8f;
            s.paddingRight = 12f;
            s.paddingTop = s.paddingBottom = 6f;
            s.marginLeft = s.marginRight = s.marginTop = s.marginBottom = 0f;
            s.fontSize = 18f;
            s.unityFontStyleAndWeight = FontStyle.Bold;
            s.color = new Color(1f, 0.87f, 0.55f);
            s.backgroundColor = new Color(0.08f, 0.06f, 0.05f, 0.75f);
            s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = 2f;
            s.borderTopColor = s.borderBottomColor = s.borderLeftColor = s.borderRightColor = new Color(0.78f, 0.58f, 0.24f);
            s.borderTopLeftRadius = s.borderTopRightRadius = s.borderBottomLeftRadius = s.borderBottomRightRadius = 8f;
            s.display = DisplayStyle.None;

            var icon = LoadIcon();
            if (icon != null)
            {
                var image = new VisualElement { name = "UpgradeQueueCounterIcon", pickingMode = PickingMode.Ignore };
                image.style.width = image.style.height = IconSize;
                image.style.marginRight = 8f;
                image.style.backgroundImage = icon;
                _button.Add(image);
            }

            _label = new Label { pickingMode = PickingMode.Ignore };
            _label.style.marginLeft = _label.style.marginRight = _label.style.marginTop = _label.style.marginBottom = 0f;
            _label.style.paddingLeft = _label.style.paddingRight = _label.style.paddingTop = _label.style.paddingBottom = 0f;
            _button.Add(_label);

            hud.hudDocument.rootVisualElement.Add(_button);
            _button.BringToFront();
            _attachedHud = hud;
        }

        // Loaded once and kept across scene loads; the counter falls back to text only if it fails.
        private static Texture2D LoadIcon()
        {
            if (_iconLoadAttempted)
                return _icon;
            _iconLoadAttempted = true;

            using (var stream = typeof(HudCounter).Assembly.GetManifestResourceStream(IconResource))
            {
                if (stream == null)
                {
                    Plugin.Log.LogWarning($"Missing embedded resource {IconResource}");
                    return null;
                }
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "UpgradeQueueCounterIcon",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                if (!texture.LoadImage(bytes))
                {
                    Plugin.Log.LogWarning("Could not decode the counter icon");
                    Object.Destroy(texture);
                    return null;
                }
                _icon = texture;
            }
            return _icon;
        }

        private static void ApplyCorner(CounterCorner corner)
        {
            var s = _button.style;
            var top = corner == CounterCorner.TopLeft || corner == CounterCorner.TopRight;
            var left = corner == CounterCorner.TopLeft || corner == CounterCorner.BottomLeft;
            // Positive offsets move the counter away from its corner.
            var x = Margin + Plugin.CounterOffsetX.Value;
            var y = EdgeMargin + Plugin.CounterOffsetY.Value;
            s.top = top ? y : StyleKeyword.Auto;
            s.bottom = top ? StyleKeyword.Auto : y;
            s.left = left ? x : StyleKeyword.Auto;
            s.right = left ? StyleKeyword.Auto : x;
        }
    }
}
