using PeglinQuickRestart.Core;
using UnityEngine;

namespace PeglinQuickRestart
{
    /// <summary>Watches the restart key, starts the restart once it's held long enough, and shows the countdown.</summary>
    internal class RestartKeyWatcher : MonoBehaviour
    {
        private HoldTimer _hold;
        private RunRestarter _restarter;
        private GUIStyle _style;

        private void Awake()
        {
            _hold = new HoldTimer(Plugin.HoldSeconds.Value);
            Plugin.HoldSeconds.SettingChanged += (_, __) => _hold = new HoldTimer(Plugin.HoldSeconds.Value);
            _restarter = new RunRestarter(this);
        }

        private void Update()
        {
            // Unscaled time: the hold takes the same real time at any game speed, and works while paused.
            bool held = !_restarter.Busy && Plugin.RestartKey.Value.IsPressed() && _restarter.InRestartableScene();
            if (_hold.Update(held, Time.unscaledDeltaTime))
            {
                _restarter.TryRestart();
            }
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
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            }
            GUI.Box(new Rect(Screen.width / 2f - 260f, 20f, 520f, 44f), message, _style);
        }
    }
}
