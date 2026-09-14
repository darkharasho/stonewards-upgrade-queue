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

        // Sonar ping on a new queued level-up: rings spread out from the counter and fade.
        private const int PingRings = 2;
        private const float PingRingDelay = 0.35f;
        private const float PingRingDuration = 1.1f;
        private const float PingSpread = 26f;
        private const float PingStartAlpha = 0.85f;
        private const float CornerRadius = 8f;
        private static readonly Color Gold = new Color(1f, 0.82f, 0.45f);

        private static Button _button;
        private static Label _label;
        private static readonly VisualElement[] _rings = new VisualElement[PingRings];
        private static float _pingStart = float.NegativeInfinity;
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

            UpdatePing();

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

            for (var i = 0; i < PingRings; i++)
            {
                var ring = new VisualElement { name = "UpgradeQueuePingRing", pickingMode = PickingMode.Ignore };
                var rs = ring.style;
                rs.position = Position.Absolute;
                rs.borderTopWidth = rs.borderBottomWidth = rs.borderLeftWidth = rs.borderRightWidth = 2f;
                rs.display = DisplayStyle.None;
                _button.Add(ring);
                _rings[i] = ring;
            }

            hud.hudDocument.rootVisualElement.Add(_button);
            _button.BringToFront();
            _attachedHud = hud;
        }

        /// <summary>Starts a ping. Uses unscaled time so it also plays while the game is paused.</summary>
        public static void Ping()
        {
            if (Plugin.PingOnLevelUp.Value)
                _pingStart = Time.unscaledTime;
        }

        private static void UpdatePing()
        {
            var elapsed = Time.unscaledTime - _pingStart;
            for (var i = 0; i < PingRings; i++)
            {
                var ring = _rings[i];
                var t = (elapsed - i * PingRingDelay) / PingRingDuration;
                if (t < 0f || t >= 1f)
                {
                    ring.style.display = DisplayStyle.None;
                    continue;
                }

                // Ease out: rings move fast at first and settle as they fade.
                var eased = 1f - (1f - t) * (1f - t) * (1f - t);
                var spread = PingSpread * eased;
                var color = new Color(Gold.r, Gold.g, Gold.b, PingStartAlpha * (1f - t) * (1f - t));

                var rs = ring.style;
                rs.display = DisplayStyle.Flex;
                // Offsets are from the button's padding box; start on its 2px border.
                rs.left = rs.right = rs.top = rs.bottom = -2f - spread;
                rs.borderTopLeftRadius = rs.borderTopRightRadius = rs.borderBottomLeftRadius = rs.borderBottomRightRadius = CornerRadius + spread;
                rs.borderTopColor = rs.borderBottomColor = rs.borderLeftColor = rs.borderRightColor = color;
            }
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
