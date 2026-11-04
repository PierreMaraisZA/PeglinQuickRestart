using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using PeglinQuickRestart.Core;
using UnityEngine;

namespace PeglinQuickRestart
{
    /// <summary>Watches the restart key and controller button, starts the restart once it's held long enough, and shows the countdown.</summary>
    internal class RestartKeyWatcher : MonoBehaviour
    {
        /// <summary>A long frame (scene load, focus regain) must not count as a whole hold's worth of time.</summary>
        private const float MaxHoldStep = 0.1f;

        private HoldTimer _hold;
        private RunRestarter _restarter;
        private GUIStyle _style;
        private EventHandler _holdSecondsChanged;

        private void Awake()
        {
            _hold = new HoldTimer(Plugin.HoldSeconds.Value);
            _holdSecondsChanged = (_, __) => _hold = new HoldTimer(Plugin.HoldSeconds.Value);
            Plugin.HoldSeconds.SettingChanged += _holdSecondsChanged;
            _restarter = new RunRestarter(this);
        }

        private void OnDestroy()
        {
            if (_holdSecondsChanged != null)
            {
                Plugin.HoldSeconds.SettingChanged -= _holdSecondsChanged;
            }
        }

        private void Update()
        {
            // Unscaled time: the hold takes the same real time at any game speed, and works while paused.
            bool held = !_restarter.Busy && (IsHeld(Plugin.RestartKey.Value) || IsHeld(Plugin.ControllerRestartButton.Value))
                && _restarter.InRestartableScene();
            if (_hold.Update(held, Mathf.Min(Time.unscaledDeltaTime, MaxHoldStep)))
            {
                _restarter.TryRestart();
            }
        }

        /// <summary>
        /// A key or button set to None never counts as held. Keyboard shortcuts use <see cref="KeyboardShortcut.IsPressed"/>.
        /// Shortcuts with a controller button in them can't: it fails while any other key is held, and Unity reports every
        /// pad button twice (JoystickButton8 and Joystick1Button8), so it never passes.
        /// </summary>
        private static bool IsHeld(KeyboardShortcut shortcut)
        {
            if (IsPadButton(shortcut.MainKey) || shortcut.Modifiers.Any(IsPadButton))
            {
                return ShortcutHold.IsHeld(shortcut.MainKey, shortcut.Modifiers, UnityInput.Current.GetKey);
            }
            return shortcut.MainKey != KeyCode.None && shortcut.IsPressed();
        }

        /// <summary>The JoystickButtonN and JoystickNButtonM key codes are one contiguous block; check both ends.</summary>
        private static bool IsPadButton(KeyCode key)
        {
            return key >= KeyCode.JoystickButton0 && key <= KeyCode.Joystick8Button19;
        }

        private void OnGUI()
        {
            string message = _restarter.Busy ? "Restarting run..." : _restarter.Notice;
            if (message == null && Plugin.ShowHoldProgress.Value && _hold.Charging)
            {
                message = $"Hold to restart run: {Mathf.Max(0f, _hold.HoldSeconds - _hold.Held):0.0}s";
            }
            if (message == null)
            {
                return;
            }
            // Rebuild if the skin or its font changed (PeglinFonts swaps the GUI font while the game runs).
            if (_style == null || _style.font != GUI.skin.box.font)
            {
                _style = new GUIStyle(GUI.skin.box) { fontSize = 30, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            }
            // Grow with the message: custom fonts are wider than the game's and would otherwise clip.
            Vector2 size = _style.CalcSize(new GUIContent(message));
            float width = Mathf.Max(520f, size.x + 80f);
            float height = Mathf.Max(52f, size.y + 20f);
            GUI.Box(new Rect((Screen.width - width) / 2f, 20f, width, height), message, _style);
        }
    }
}
