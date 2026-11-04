using PeglinQuickRestart.Core;
using Xunit;

namespace PeglinQuickRestart.Tests
{
    public class HoldTimerTests
    {
        [Fact]
        public void Update_FiresOnceTheHoldTimeIsReached()
        {
            var hold = new HoldTimer(2.5f);
            Assert.False(hold.Update(true, 1f));
            Assert.False(hold.Update(true, 1f));
            Assert.True(hold.Update(true, 0.5f));
        }

        [Fact]
        public void Update_FiresOnlyOncePerHold()
        {
            var hold = new HoldTimer(1f);
            Assert.True(hold.Update(true, 1f));
            Assert.False(hold.Update(true, 5f));
            Assert.False(hold.Update(false, 0.1f));
            Assert.False(hold.Update(true, 0.5f));
            Assert.True(hold.Update(true, 0.5f));
        }

        [Fact]
        public void Update_LettingGoStartsTheHoldAgain()
        {
            var hold = new HoldTimer(2.5f);
            hold.Update(true, 2f);
            hold.Update(false, 0.1f);
            Assert.Equal(0f, hold.Held);
            Assert.False(hold.Update(true, 2f));
        }

        [Fact]
        public void Progress_GoesFromZeroToOne()
        {
            var hold = new HoldTimer(2f);
            Assert.Equal(0f, hold.Progress);
            Assert.False(hold.Charging);
            hold.Update(true, 1f);
            Assert.Equal(0.5f, hold.Progress);
            Assert.True(hold.Charging);
            hold.Update(true, 1f);
            Assert.Equal(1f, hold.Progress);
            Assert.False(hold.Charging);
        }

        [Fact]
        public void Update_ZeroHoldFiresOnTheFirstHeldFrame()
        {
            var hold = new HoldTimer(0f);
            Assert.Equal(0f, hold.Progress);
            Assert.True(hold.Update(true, 0f));
            Assert.False(hold.Update(true, 1f));
        }

        [Fact]
        public void Update_IgnoresNegativeTime()
        {
            var hold = new HoldTimer(1f);
            hold.Update(true, -5f);
            Assert.Equal(0f, hold.Held);
        }
    }
}
