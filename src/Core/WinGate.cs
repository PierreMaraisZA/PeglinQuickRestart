namespace PeglinQuickRestart.Core
{
    /// <summary>
    /// Remembers that the run has been won, so the restart key stays off on the win, Core choice and credits screens.
    /// A restart there would abandon the run before the game's run summary records the win. Pure C# (no Unity/BepInEx)
    /// so it can be unit tested.
    /// </summary>
    public sealed class WinGate
    {
        private bool _won;

        public bool Won => _won;

        /// <summary>
        /// The game finished a battle as a victory. The run is won after the map's final node (the Mines boss, which may
        /// lead to the Core choice) or when the game shows the Core's win cutscene. Nothing counts if the player died in
        /// the same moment: the game then shows the run-over screen instead.
        /// </summary>
        public void BattleWon(bool playerAlive, bool finalNode, bool coreWinShown)
        {
            if (playerAlive && (finalNode || coreWinShown))
            {
                _won = true;
            }
        }

        /// <summary>The player chose to go on into the Core after the Mines boss: the run is not over.</summary>
        public void ContinuedToCore()
        {
            _won = false;
        }

        /// <summary>A run was started or continued from the menu.</summary>
        public void RunLoaded()
        {
            _won = false;
        }

        /// <summary>
        /// Whether the restart is off. Another mod whose restart handler takes the current run (a different game mode)
        /// decides for itself, so a win never blocks it.
        /// </summary>
        public bool Blocks(bool anotherModHandles)
        {
            return _won && !anotherModHandles;
        }
    }
}
