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

        private static Button _button;
        private static Hud _attachedHud;

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
            if (_button.text != text)
                _button.text = text;

            ApplyCorner(Plugin.CounterPosition.Value);
        }

        private static void Attach(Hud hud)
        {
            _button?.RemoveFromHierarchy();

            _button = new Button(() => QueuedUpgradeSession.RequestOpen()) { name = "UpgradeQueueCounter" };
            var s = _button.style;
            s.position = Position.Absolute;
            s.paddingLeft = s.paddingRight = 12f;
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

            hud.hudDocument.rootVisualElement.Add(_button);
            _button.BringToFront();
            _attachedHud = hud;
        }

        private static void ApplyCorner(CounterCorner corner)
        {
            var s = _button.style;
            var top = corner == CounterCorner.TopLeft || corner == CounterCorner.TopRight;
            var left = corner == CounterCorner.TopLeft || corner == CounterCorner.BottomLeft;
            s.top = top ? Margin : StyleKeyword.Auto;
            s.bottom = top ? StyleKeyword.Auto : Margin;
            s.left = left ? Margin : StyleKeyword.Auto;
            s.right = left ? StyleKeyword.Auto : Margin;
        }
    }
}
