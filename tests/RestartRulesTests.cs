using System;
using System.Collections.Generic;
using PeglinQuickRestart.Core;
using Xunit;

namespace PeglinQuickRestart.Tests
{
    public class RestartRulesTests
    {
        // scene, restarting, blocked, handler, won, setupKnown
        [Theory]
        [InlineData(true, false, false, false, false, true, true)]   // ordinary mid-run restart
        [InlineData(false, false, false, false, false, true, false)] // main menu or loading
        [InlineData(false, false, false, true, false, true, false)]  // handler can't override the scene check
        [InlineData(true, true, false, false, false, true, false)]   // already restarting
        [InlineData(true, true, false, true, false, true, false)]    // already restarting, even with a handler
        [InlineData(true, false, true, false, false, true, false)]   // blocked
        [InlineData(true, false, true, true, false, true, false)]    // blocked beats a handler
        [InlineData(true, false, false, true, true, false, true)]    // handler takes a won run with no known setup
        [InlineData(true, false, false, false, true, true, false)]   // won run, no handler
        [InlineData(true, false, false, false, false, false, false)] // setup unknown, no handler
        public void CanRestart(bool scene, bool restarting, bool blocked, bool handler, bool won, bool setup, bool expected)
        {
            Assert.Equal(expected, RestartRules.CanRestart(scene, restarting, blocked, handler, won, () => setup));
        }

        [Fact]
        public void SetupIsOnlyLookedUp_WhenQuickRestartWouldDoItItself()
        {
            int lookups = 0;
            Func<bool> setup = () => { lookups++; return true; };
            RestartRules.CanRestart(true, false, false, true, false, setup);
            RestartRules.CanRestart(true, false, true, false, false, setup);
            RestartRules.CanRestart(true, false, false, false, true, setup);
            Assert.Equal(0, lookups);
            Assert.True(RestartRules.CanRestart(true, false, false, false, false, setup));
            Assert.Equal(1, lookups);
        }
    }

    public class RestartListenerTests
    {
        private readonly List<string> _log = new List<string>();

        private RestartRegistry NewRegistry() => new RestartRegistry(_log.Add);

        [Fact]
        public void NoListeners_NotifyIsHarmless()
        {
            NewRegistry().NotifyRestart("key");
            Assert.Empty(_log);
        }

        [Fact]
        public void EveryListener_IsToldTheSource_InRegistrationOrder()
        {
            var registry = NewRegistry();
            var heard = new List<string>();
            registry.SetListener("a", s => heard.Add("a:" + s));
            registry.SetListener("b", s => heard.Add("b:" + s));
            registry.NotifyRestart("button");
            Assert.Equal(new[] { "a:button", "b:button" }, heard);
        }

        [Fact]
        public void SameGuid_ReplacesTheListener()
        {
            var registry = NewRegistry();
            var heard = new List<string>();
            registry.SetListener("a", s => heard.Add("old"));
            registry.SetListener("a", s => heard.Add("new"));
            registry.NotifyRestart("key");
            Assert.Equal(new[] { "new" }, heard);
        }

        [Fact]
        public void NullListener_Removes()
        {
            var registry = NewRegistry();
            var heard = new List<string>();
            registry.SetListener("a", s => heard.Add(s));
            registry.SetListener("a", null);
            registry.NotifyRestart("key");
            Assert.Empty(heard);
        }

        [Fact]
        public void ThrowingListener_IsLoggedOnce_AndOthersStillHear()
        {
            var registry = NewRegistry();
            var heard = new List<string>();
            registry.SetListener("bad", s => throw new InvalidOperationException("boom"));
            registry.SetListener("good", s => heard.Add(s));
            registry.NotifyRestart("key");
            registry.NotifyRestart("api");
            Assert.Equal(new[] { "key", "api" }, heard);
            Assert.Single(_log);
            Assert.Contains("bad", _log[0]);
        }

        [Fact]
        public void RemoveAll_RemovesTheListenerToo()
        {
            var registry = NewRegistry();
            var heard = new List<string>();
            registry.SetListener("a", s => heard.Add(s));
            registry.RemoveAll("a");
            registry.NotifyRestart("key");
            Assert.Empty(heard);
        }

        [Fact]
        public void ListenerThatRegistersDuringNotify_DoesNotBreakTheWalk()
        {
            var registry = NewRegistry();
            var heard = new List<string>();
            registry.SetListener("a", s => { heard.Add("a"); registry.SetListener("c", x => heard.Add("c")); });
            registry.SetListener("b", s => heard.Add("b"));
            registry.NotifyRestart("key");
            Assert.Equal(new[] { "a", "b" }, heard);
            Assert.Empty(_log);
        }

        [Fact]
        public void BlockerAskingIfBlocked_GetsNoOpinion_InsteadOfLooping()
        {
            var registry = NewRegistry();
            bool inner = true;
            registry.SetBlocker("a", () => { inner = registry.IsBlocked(); return true; });
            Assert.True(registry.IsBlocked());
            Assert.False(inner);
        }

        [Fact]
        public void HandlerAskingAboutHandlers_GetsNoOpinion_InsteadOfLooping()
        {
            var registry = NewRegistry();
            bool inner = true;
            registry.RegisterHandler("a", () => { inner = registry.AnyCanHandle() || registry.IsBlocked(); return true; }, () => { });
            Assert.True(registry.AnyCanHandle());
            Assert.False(inner);
        }

        [Fact]
        public void BlockerAskingAboutHandlers_GetsNoOpinion()
        {
            var registry = NewRegistry();
            registry.RegisterHandler("h", () => true, () => { });
            bool inner = true;
            registry.SetBlocker("b", () => { inner = registry.AnyCanHandle(); return false; });
            Assert.False(registry.IsBlocked());
            Assert.False(inner);
        }

        [Fact]
        public void AfterARecursiveQuestion_TheGuardIsReleased()
        {
            var registry = NewRegistry();
            registry.SetBlocker("a", () => registry.IsBlocked());
            registry.IsBlocked();
            registry.SetBlocker("a", () => true);
            Assert.True(registry.IsBlocked());
        }

        [Fact]
        public void HandlerRestartMayAskQuestions()
        {
            var registry = NewRegistry();
            bool? seen = null;
            registry.RegisterHandler("a", () => true, () => seen = registry.AnyCanHandle());
            Assert.True(registry.TryHandle());
            Assert.True(seen);
        }

        [Fact]
        public void EmptyGuid_IsIgnored()
        {
            var registry = NewRegistry();
            var heard = new List<string>();
            registry.SetListener("", s => heard.Add(s));
            registry.NotifyRestart("key");
            Assert.Empty(heard);
            Assert.Single(_log);
        }
    }
}
