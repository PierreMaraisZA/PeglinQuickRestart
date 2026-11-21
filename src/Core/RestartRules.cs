using System;

namespace PeglinQuickRestart.Core
{
    /// <summary>
    /// Whether a restart asked for right now would go ahead, in the same order the restart itself checks: a restart
    /// already under way or a blocker stops it; then another mod's handler takes it; otherwise QuickRestart's own restart
    /// needs the run not to be won and its setup to be known. Pure C# (no Unity/BepInEx) so it can be unit tested.
    /// </summary>
    public static class RestartRules
    {
        /// <param name="setupKnown">Asked last, and only when QuickRestart would restart the run itself (it can read a file).</param>
        public static bool CanRestart(bool restartableScene, bool restarting, bool blocked, bool handlerTakesIt, bool runWon, Func<bool> setupKnown)
        {
            if (!restartableScene || restarting || blocked)
            {
                return false;
            }
            if (handlerTakesIt)
            {
                return true;
            }
            return !runWon && setupKnown();
        }
    }

    /// <summary>What asked for a restart, as told to restart listeners. New names may be added; ignore unknown ones.</summary>
    public static class RestartSources
    {
        /// <summary>The player held the restart key or controller button.</summary>
        public const string Key = "key";

        /// <summary>The player pressed the Restart button on the run-over screen.</summary>
        public const string Button = "button";

        /// <summary>Another mod called QuickRestartApi.RequestRestart.</summary>
        public const string Api = "api";
    }
}
