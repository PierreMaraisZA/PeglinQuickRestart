using System;
using PeglinQuickRestart.Core;

namespace PeglinQuickRestart
{
    /// <summary>
    /// For other mods. Find this type by its simple name in this plugin's assembly, check <see cref="ApiVersion"/>, and
    /// register with your own plugin GUID. Registering again with the same GUID replaces your earlier registration.
    /// Every callback is guarded: one that throws is logged once and treated as having no opinion.
    /// </summary>
    public static class QuickRestartApi
    {
        internal static readonly RestartRegistry Registry = new RestartRegistry(message => Plugin.Log?.LogWarning(message));

        public static int ApiVersion => 1;

        /// <summary>
        /// Checked before the normal restart, by the hold key and by the Restart button on the run-over screen. The
        /// first handler whose <paramref name="canHandle"/> returns true gets <paramref name="restart"/> and does its own
        /// abandon and cleanup; QuickRestart then does nothing else. While <paramref name="canHandle"/> is true when the
        /// normal Play button is pressed, QuickRestart doesn't overwrite its own last-run record.
        /// </summary>
        public static void RegisterRestartHandler(string guid, Func<bool> canHandle, Action restart)
        {
            Registry.RegisterHandler(guid, canHandle, restart);
        }

        /// <summary>While <paramref name="block"/> returns true, the hold key does nothing and the Restart button is hidden.</summary>
        public static void SetRestartBlocker(string guid, Func<bool> block)
        {
            Registry.SetBlocker(guid, block);
        }

        /// <summary>
        /// Called when a restart is started (QuickRestart's own) or handed to a restart handler, with
        /// what asked for it: <c>"key"</c> (hold key or controller button), <c>"button"</c> (the run-over screen's
        /// Restart button) or <c>"api"</c> (<see cref="RequestRestart"/>). New names may be added; ignore unknown ones.
        /// Every listener is told; one that throws is logged once and skipped.
        /// </summary>
        public static void SetRestartListener(string guid, Action<string> listener)
        {
            Registry.SetListener(guid, listener);
        }

        /// <summary>
        /// True if <see cref="RequestRestart"/> (or the hold key) would restart right now: in a run, not loading, no
        /// restart under way, not blocked, and either a restart handler takes the run or QuickRestart can restart it
        /// itself (the run isn't won and its setup is known). Call from Unity's main thread.
        /// </summary>
        public static bool CanRestart => RunRestarter.Instance != null && RunRestarter.Instance.CanRestartNow();

        /// <summary>True while QuickRestart's own restart is going through the menus (a few seconds). Not set for a restart a handler took.</summary>
        public static bool IsRestarting => RunRestarter.Instance != null && RunRestarter.Instance.Busy;

        /// <summary>
        /// Restarts the run exactly as holding the restart key would, including handing it to a restart handler and,
        /// on the run-over screen, recording the run as End Run does. <paramref name="guid"/> is the caller's plugin
        /// GUID, for the log. Returns true if the restart went ahead (started or taken by a handler). Call from
        /// Unity's main thread; a call made while a restart is being decided does nothing.
        /// </summary>
        public static bool RequestRestart(string guid)
        {
            RunRestarter restarter = RunRestarter.Instance;
            if (restarter == null || !restarter.CanRestartHere())
            {
                return false;
            }
            Plugin.Log.LogInfo($"Restart requested by {(string.IsNullOrEmpty(guid) ? "another mod" : guid)}.");
            RunOverScreen screen = RunOverScreen.Current;
            return screen != null
                ? screen.Press(RestartSources.Api)
                : restarter.Request(null, RestartSources.Api) != RestartResult.NotStarted;
        }

        /// <summary>Removes everything registered with this GUID.</summary>
        public static void RemoveAll(string guid)
        {
            Registry.RemoveAll(guid);
        }
    }
}
