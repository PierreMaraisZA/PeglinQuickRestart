using System;
using System.Collections.Generic;
using PeglinQuickRestart.Core;
using Xunit;

namespace PeglinQuickRestart.Tests
{
    public class RestartRegistryTests
    {
        private readonly List<string> _log = new List<string>();

        private RestartRegistry NewRegistry() => new RestartRegistry(_log.Add);

        // --- Defaults: nothing registered means the normal restart, exactly as before. ---

        [Fact]
        public void NothingRegistered_NotBlocked()
        {
            Assert.False(NewRegistry().IsBlocked());
        }

        [Fact]
        public void NothingRegistered_NoHandlerTakesTheRestart()
        {
            var registry = NewRegistry();

            Assert.False(registry.AnyCanHandle());
            Assert.False(registry.TryHandle());
            Assert.Empty(_log);
        }

        // --- Blockers ---

        [Fact]
        public void BlockerSayingFalseDoesNotBlock()
        {
            var registry = NewRegistry();
            registry.SetBlocker("a", () => false);

            Assert.False(registry.IsBlocked());
        }

        [Fact]
        public void AnyBlockerSayingTrueBlocks()
        {
            var registry = NewRegistry();
            registry.SetBlocker("a", () => false);
            registry.SetBlocker("b", () => true);
            registry.SetBlocker("c", () => false);

            Assert.True(registry.IsBlocked());
        }

        [Fact]
        public void BlockerIsAskedEveryTime()
        {
            var registry = NewRegistry();
            bool block = false;
            registry.SetBlocker("a", () => block);

            Assert.False(registry.IsBlocked());
            block = true;
            Assert.True(registry.IsBlocked());
        }

        [Fact]
        public void ThrowingBlockerDoesNotBlockAndIsLoggedOnce()
        {
            var registry = NewRegistry();
            registry.SetBlocker("bad", () => throw new Exception("boom"));

            Assert.False(registry.IsBlocked());
            Assert.False(registry.IsBlocked());
            Assert.Single(_log);
        }

        [Fact]
        public void ThrowingBlockerDoesNotHideAnotherBlocker()
        {
            var registry = NewRegistry();
            registry.SetBlocker("bad", () => throw new Exception());
            registry.SetBlocker("good", () => true);

            Assert.True(registry.IsBlocked());
        }

        [Fact]
        public void ReplacingABlockerReplacesItsAnswer()
        {
            var registry = NewRegistry();
            registry.SetBlocker("a", () => true);
            registry.SetBlocker("a", () => false);

            Assert.False(registry.IsBlocked());
        }

        [Fact]
        public void NullBlockerRemovesIt()
        {
            var registry = NewRegistry();
            registry.SetBlocker("a", () => true);
            registry.SetBlocker("a", null);

            Assert.False(registry.IsBlocked());
        }

        // --- Handlers ---

        [Fact]
        public void HandlerThatCanHandleGetsTheRestart()
        {
            var registry = NewRegistry();
            int restarts = 0;
            registry.RegisterHandler("a", () => true, () => restarts++);

            Assert.True(registry.AnyCanHandle());
            Assert.True(registry.TryHandle());
            Assert.Equal(1, restarts);
        }

        [Fact]
        public void HandlerThatCanNotHandleIsSkipped()
        {
            var registry = NewRegistry();
            int restarts = 0;
            registry.RegisterHandler("a", () => false, () => restarts++);

            Assert.False(registry.AnyCanHandle());
            Assert.False(registry.TryHandle());
            Assert.Equal(0, restarts);
        }

        [Fact]
        public void AnyCanHandleNeverRestarts()
        {
            var registry = NewRegistry();
            int restarts = 0;
            registry.RegisterHandler("a", () => true, () => restarts++);

            registry.AnyCanHandle();

            Assert.Equal(0, restarts);
        }

        [Fact]
        public void FirstMatchingHandlerWinsInRegistrationOrder()
        {
            var registry = NewRegistry();
            var calls = new List<string>();
            registry.RegisterHandler("a", () => false, () => calls.Add("a"));
            registry.RegisterHandler("b", () => true, () => calls.Add("b"));
            registry.RegisterHandler("c", () => true, () => calls.Add("c"));

            Assert.True(registry.TryHandle());
            Assert.Equal(new[] { "b" }, calls);
        }

        [Fact]
        public void ReRegisteringKeepsThePositionButUsesTheNewCallbacks()
        {
            var registry = NewRegistry();
            var calls = new List<string>();
            registry.RegisterHandler("a", () => true, () => calls.Add("a1"));
            registry.RegisterHandler("b", () => true, () => calls.Add("b"));
            registry.RegisterHandler("a", () => true, () => calls.Add("a2"));

            registry.TryHandle();

            Assert.Equal(new[] { "a2" }, calls);
        }

        [Fact]
        public void ThrowingCanHandleIsNoOpinionAndTheNextHandlerIsTried()
        {
            var registry = NewRegistry();
            var calls = new List<string>();
            registry.RegisterHandler("bad", () => throw new Exception("boom"), () => calls.Add("bad"));
            registry.RegisterHandler("good", () => true, () => calls.Add("good"));

            Assert.True(registry.TryHandle());
            Assert.Equal(new[] { "good" }, calls);
            Assert.Single(_log);
        }

        [Fact]
        public void ThrowingCanHandleAloneLeavesTheNormalRestart()
        {
            var registry = NewRegistry();
            registry.RegisterHandler("bad", () => throw new Exception(), () => { });

            Assert.False(registry.AnyCanHandle());
            Assert.False(registry.TryHandle());
        }

        [Fact]
        public void ThrowingRestartStillCountsAsHandledSoTheNormalRestartDoesNotRunOnTop()
        {
            var registry = NewRegistry();
            registry.RegisterHandler("bad", () => true, () => throw new Exception("half done"));

            Assert.True(registry.TryHandle());
            Assert.Single(_log);
            Assert.Contains("half done", _log[0]);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void HandlerWithAMissingCallbackIsNotRegistered(bool hasCanHandle, bool hasRestart)
        {
            var registry = NewRegistry();
            registry.RegisterHandler("a", hasCanHandle ? () => true : (Func<bool>)null, hasRestart ? () => { } : (Action)null);

            Assert.False(registry.TryHandle());
        }

        [Fact]
        public void HandlerWithAMissingCallbackRemovesTheOldOne()
        {
            var registry = NewRegistry();
            registry.RegisterHandler("a", () => true, () => { });
            registry.RegisterHandler("a", () => true, null);

            Assert.False(registry.AnyCanHandle());
        }

        [Fact]
        public void HandlerCanRemoveItselfWhileRestarting()
        {
            var registry = NewRegistry();
            int restarts = 0;
            registry.RegisterHandler("a", () => true, () => { restarts++; registry.RemoveAll("a"); });

            Assert.True(registry.TryHandle());
            Assert.False(registry.TryHandle());
            Assert.Equal(1, restarts);
        }

        [Fact]
        public void BlockerCanRemoveItselfWhileBeingAsked()
        {
            var registry = NewRegistry();
            registry.SetBlocker("a", () => { registry.RemoveAll("a"); return false; });
            registry.SetBlocker("b", () => true);

            Assert.True(registry.IsBlocked());
            registry.RemoveAll("b");
            Assert.False(registry.IsBlocked());
        }

        [Fact]
        public void HandlerRegisteredDuringCanHandleIsUsedNextTimeNotThisTime()
        {
            var registry = NewRegistry();
            var calls = new List<string>();
            registry.RegisterHandler("a", () => { registry.RegisterHandler("b", () => true, () => calls.Add("b")); return false; }, () => calls.Add("a"));

            Assert.False(registry.TryHandle());
            Assert.True(registry.TryHandle());
            Assert.Equal(new[] { "b" }, calls);
        }

        // --- RemoveAll ---

        [Fact]
        public void RemoveAllRemovesHandlerAndBlockerForThatGuidOnly()
        {
            var registry = NewRegistry();
            registry.RegisterHandler("a", () => true, () => { });
            registry.SetBlocker("a", () => true);
            registry.SetBlocker("b", () => false);

            registry.RemoveAll("a");

            Assert.False(registry.AnyCanHandle());
            Assert.False(registry.IsBlocked());
        }

        [Fact]
        public void RemoveAllLeavesOtherModsAlone()
        {
            var registry = NewRegistry();
            registry.RegisterHandler("a", () => true, () => { });
            registry.SetBlocker("b", () => true);

            registry.RemoveAll("c");
            registry.RemoveAll(null);

            Assert.True(registry.AnyCanHandle());
            Assert.True(registry.IsBlocked());
        }
    }
}
