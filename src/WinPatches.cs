using Battle;
using HarmonyLib;

namespace PeglinQuickRestart
{
    /// <summary>
    /// The game's own end of a won battle. After the map's final node (the Mines boss) or when the game shows the Core's
    /// win cutscene, the run is won, and the restart key stays off until a run is started or continued from the menu.
    /// The game skips its victory if the player died in the same moment, and so does this.
    /// </summary>
    [HarmonyPatch(typeof(BattleController), "CompleteVictory")]
    internal static class BattleWonPatch
    {
        private static readonly AccessTools.FieldRef<BattleController, PlayerHealthController> PlayerHealth =
            AccessTools.FieldRefAccess<BattleController, PlayerHealthController>("_playerHealthController");

        private static void Postfix(BattleController __instance)
        {
            PlayerHealthController health = PlayerHealth(__instance);
            RunRestarter.Wins.BattleWon(
                health == null || health.CurrentHealth > 0f,
                StaticGameData.currentNode != null && StaticGameData.currentNode.isFinalNode,
                __instance.coreWinTimeline != null && __instance.coreWinTimeline.activeSelf);
        }
    }

    /// <summary>Choosing to go into the Core after the Mines boss carries the run on.</summary>
    [HarmonyPatch(typeof(PostBattleController), nameof(PostBattleController.GoToCoreButtonPressed))]
    internal static class ContinuedToCorePatch
    {
        private static void Postfix()
        {
            RunRestarter.Wins.ContinuedToCore();
        }
    }
}
