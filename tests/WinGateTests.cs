using PeglinQuickRestart.Core;
using Xunit;

namespace PeglinQuickRestart.Tests
{
    public class WinGateTests
    {
        [Fact]
        public void Default_NotWon_NotBlocked()
        {
            var gate = new WinGate();
            Assert.False(gate.Won);
            Assert.False(gate.Blocks(false));
        }

        [Fact]
        public void OrdinaryBattle_NotWon()
        {
            var gate = new WinGate();
            gate.BattleWon(playerAlive: true, finalNode: false, coreWinShown: false);
            Assert.False(gate.Blocks(false));
        }

        [Fact]
        public void FinalNode_Won_Blocks()
        {
            var gate = new WinGate();
            gate.BattleWon(true, finalNode: true, coreWinShown: false);
            Assert.True(gate.Won);
            Assert.True(gate.Blocks(false));
        }

        [Fact]
        public void CoreWinCutscene_Won_Blocks()
        {
            var gate = new WinGate();
            gate.BattleWon(true, finalNode: false, coreWinShown: true);
            Assert.True(gate.Blocks(false));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void PlayerDiedInTheSameMoment_NotWon(bool finalNode, bool coreWinShown)
        {
            var gate = new WinGate();
            gate.BattleWon(playerAlive: false, finalNode, coreWinShown);
            Assert.False(gate.Won);
            Assert.False(gate.Blocks(false));
        }

        [Fact]
        public void LaterOrdinaryBattle_DoesNotClearTheWin()
        {
            var gate = new WinGate();
            gate.BattleWon(true, true, false);
            gate.BattleWon(true, false, false);
            Assert.True(gate.Blocks(false));
        }

        [Fact]
        public void LaterDeath_DoesNotClearTheWin()
        {
            var gate = new WinGate();
            gate.BattleWon(true, true, false);
            gate.BattleWon(false, false, false);
            Assert.True(gate.Blocks(false));
        }

        [Fact]
        public void GoingIntoTheCore_ClearsTheWin()
        {
            var gate = new WinGate();
            gate.BattleWon(true, true, false);
            gate.ContinuedToCore();
            Assert.False(gate.Blocks(false));
        }

        [Fact]
        public void CoreWinAfterGoingIntoTheCore_WonAgain()
        {
            var gate = new WinGate();
            gate.BattleWon(true, true, false);
            gate.ContinuedToCore();
            gate.BattleWon(true, false, true);
            Assert.True(gate.Blocks(false));
        }

        [Fact]
        public void NewOrContinuedRun_ClearsTheWin()
        {
            var gate = new WinGate();
            gate.BattleWon(true, true, false);
            gate.RunLoaded();
            Assert.False(gate.Blocks(false));
        }

        [Fact]
        public void AnotherModsHandler_IsNeverBlocked()
        {
            var gate = new WinGate();
            gate.BattleWon(true, true, false);
            Assert.False(gate.Blocks(true));
        }

        [Fact]
        public void ClearingTwice_IsHarmless()
        {
            var gate = new WinGate();
            gate.ContinuedToCore();
            gate.RunLoaded();
            gate.RunLoaded();
            Assert.False(gate.Blocks(false));
        }
    }
}
