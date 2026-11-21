using System;
using System.Linq;
using Battle;
using HarmonyLib;
using Loading;
using PeglinQuickRestart.Core;
using PeglinUI.ControllerSupport;
using PeglinUI.UIUtils;
using Scenarios;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeglinQuickRestart
{
    /// <summary>
    /// Adds a Restart button next to End Run on the screen shown after dying (the scene object GameOverObj). The button
    /// is a copy of the game's own End Run button, so it looks, sounds and navigates the same. Pressing it (or holding
    /// the restart key on this screen) first does exactly what End Run does, so the run is recorded in the
    /// Encirclepedia and stats as usual, then skips the run summary and starts the same kind of run.
    /// </summary>
    internal sealed class RunOverScreen : MonoBehaviour
    {
        private const string ButtonName = "PeglinQuickRestart_RestartButton";

        /// <summary>The game's End Run button calls this method; it's how the button is found.</summary>
        private const string EndRunMethod = "FadeInAndLoadRunSummary";

        private static RunOverScreen _current;

        private Button _endRun;
        private RectTransform _endRunRect;
        private Vector2 _endRunHome;
        private Navigation _endRunNavigation;
        private Button _restart;
        private RectTransform _restartRect;
        private float _width;
        private bool _shown;
        private bool _leaving;
        private bool _restarting;
        private bool _endRunPressed;

        /// <summary>When another mod's handler took the restart (unscaled time), or -1.</summary>
        private float _handedOffAt = -1f;

        /// <summary>A handler that took the restart but left this screen up has declined it; Restart works again after this long.</summary>
        private const float HandOffGrace = 3f;

        /// <summary>End Run or Restart has been pressed (or another mod's handler is taking the restart), so this screen is on its way out.</summary>
        internal bool Leaving => _leaving;

        /// <summary>The run-over screen being shown, if any.</summary>
        internal static RunOverScreen Current => _current != null && _current.isActiveAndEnabled ? _current : null;

        /// <summary>Called when the game shows its run-over screen. Safe to call again; adds the button once.</summary>
        internal static void AttachTo(GameObject gameOver)
        {
            if (gameOver == null || !gameOver.activeInHierarchy || gameOver.GetComponent<RunOverScreen>() != null)
            {
                return;
            }
            try
            {
                // Only the visible button: the screen also holds hidden buttons with the same click target, and copying
                // one of those gives the wrong label and look.
                Button endRun = gameOver.GetComponentsInChildren<Button>().FirstOrDefault(IsEndRun);
                if (endRun == null)
                {
                    Plugin.Log.LogWarning("Run-over screen layout not recognised (no End Run button); the Restart button wasn't added.");
                    return;
                }
                gameOver.AddComponent<RunOverScreen>().Init(endRun);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Couldn't add the Restart button to the run-over screen: {e}");
            }
        }

        private static bool IsEndRun(Button button)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == EndRunMethod)
                {
                    return true;
                }
            }
            return false;
        }

        private void Init(Button endRun)
        {
            _endRun = endRun;
            _endRunRect = (RectTransform)endRun.transform;
            _endRunHome = _endRunRect.anchoredPosition;
            _endRunNavigation = endRun.navigation;
            // Measured once, before any hover scaling can change it.
            _width = _endRunRect.rect.width * _endRunRect.localScale.x;
            // Once End Run is pressed (by the player or by our restart), the Restart button is done.
            _endRun.onClick.AddListener(() => { _leaving = true; _endRunPressed = true; });

            _restart = CopyEndRun(endRun);
            _restartRect = (RectTransform)_restart.transform;
            _restart.gameObject.SetActive(false);
            _current = this;
            Refresh();
        }

        private Button CopyEndRun(Button endRun)
        {
            // Copy under an inactive parent so none of the copy's scripts wake up before it's been adjusted.
            var holder = new GameObject("PeglinQuickRestart_Holder");
            holder.SetActive(false);
            try
            {
                GameObject copy = Instantiate(endRun.gameObject, holder.transform, false);
                copy.name = ButtonName;
                // These would select the copy (or fight over the selection) when it appears; End Run stays the default.
                foreach (Component script in copy.GetComponentsInChildren<SetObjSelectedOnEnable>(true)
                    .Cast<Component>().Concat(copy.GetComponentsInChildren<EnsureButtonSelectable>(true)))
                {
                    DestroyImmediate(script);
                }
                // A controller shortcut copied from End Run would press Restart too whenever that shortcut is used; the
                // copy is reached by moving to it and pressing the select button instead.
                foreach (MapButtonToInput script in copy.GetComponentsInChildren<MapButtonToInput>(true))
                {
                    DestroyImmediate(script);
                }
                // The translation script would put "End Run" back.
                foreach (MonoBehaviour script in copy.GetComponentsInChildren<MonoBehaviour>(true).Where(s => s != null && s.GetType().Name == "Localize").ToList())
                {
                    DestroyImmediate(script);
                }
                foreach (TMP_Text text in copy.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.text = "Restart";
                }
                Button button = copy.GetComponent<Button>();
                // A new event drops End Run's own click target; the game's click sound is added back by the copy's
                // own sound script when it's enabled.
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => Press(RestartSources.Button));
                copy.transform.SetParent(endRun.transform.parent, false);
                copy.transform.SetSiblingIndex(endRun.transform.GetSiblingIndex() + 1);
                return button;
            }
            finally
            {
                Destroy(holder);
            }
        }

        private void Update()
        {
            Refresh();
        }

        /// <summary>Shows or hides the button (setting, other mods' blockers) and keeps it disabled once the screen is being left.</summary>
        private void Refresh()
        {
            if (_endRun == null || _restart == null)
            {
                return;
            }
            // Our restart stopped (failed) while this screen is still up: make both buttons usable again.
            if (_restarting && (RunRestarter.Instance == null || !RunRestarter.Instance.Busy))
            {
                _restarting = false;
                _leaving = false;
                _endRun.interactable = true;
            }
            // Another mod's handler took the restart but this screen is still up a while later: it didn't go ahead.
            // The wait only counts while nothing is loading, so a slow scene change never re-enables it.
            if (_handedOffAt >= 0f && PeglinSceneLoader.Instance != null && PeglinSceneLoader.Instance.isLoading)
            {
                _handedOffAt = Time.unscaledTime;
            }
            if (_handedOffAt >= 0f && !_endRunPressed && Time.unscaledTime - _handedOffAt > HandOffGrace)
            {
                _handedOffAt = -1f;
                _leaving = false;
            }
            bool show = Plugin.ShowRestartButton.Value && !QuickRestartApi.Registry.IsBlocked();
            if (show != _shown)
            {
                _shown = show;
                Layout(show);
            }
            _restart.interactable = !_leaving;
        }

        /// <summary>
        /// Shown: Restart sits to the left of End Run, which stays exactly where the game put it, and the two lead to
        /// each other in every direction.
        /// </summary>
        private void Layout(bool show)
        {
            if (!show && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _restart.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(_endRun.gameObject);
            }
            _restart.gameObject.SetActive(show);
            if (!show)
            {
                _endRun.navigation = _endRunNavigation;
                return;
            }
            // One button width plus a small gap.
            _restartRect.anchoredPosition = _endRunHome - new Vector2(_width * 1.15f, 0f);
            _endRun.navigation = PointingAt(_restart);
            _restart.navigation = PointingAt(_endRun);
        }

        private static Navigation PointingAt(Selectable other)
        {
            return new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = other,
                selectOnRight = other,
                selectOnUp = other,
                selectOnDown = other,
            };
        }

        /// <summary>The Restart button, the restart key held on this screen, or another mod's request. True if it went ahead.</summary>
        internal bool Press(string source)
        {
            if (_leaving || RunRestarter.Instance == null)
            {
                return false;
            }
            _leaving = true;
            _restart.interactable = false;
            switch (RunRestarter.Instance.Request(_endRun, source))
            {
                case RestartResult.Started:
                    // Our restart has pressed End Run itself; don't let it be pressed a second time.
                    _endRun.interactable = false;
                    _restarting = true;
                    return true;
                case RestartResult.Handled:
                    // Another mod took the restart. End Run stays usable, and Restart comes back if this screen stays up.
                    _handedOffAt = Time.unscaledTime;
                    return true;
                default:
                    _leaving = false;
                    _restart.interactable = true;
                    return false;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerHealthController), nameof(PlayerHealthController.CheckForDeathAndUpdateBar))]
    internal static class BattleDeathPatch
    {
        private static void Postfix(PlayerHealthController __instance)
        {
            RunOverScreen.AttachTo(__instance.GameOverObj);
        }
    }

    [HarmonyPatch(typeof(NonBattleDeathHandler), "CheckForDeath")]
    internal static class OtherDeathPatch
    {
        private static void Postfix(NonBattleDeathHandler __instance)
        {
            RunOverScreen.AttachTo(__instance.gameOverObject);
        }
    }
}
