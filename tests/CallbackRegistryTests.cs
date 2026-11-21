using System;
using System.Collections.Generic;
using System.Linq;
using PeglinQuickRestart.Core;
using Xunit;

namespace PeglinQuickRestart.Tests
{
    public class CallbackRegistryTests
    {
        private readonly List<string> _log = new List<string>();

        private CallbackRegistry<Func<int>> NewRegistry() => new CallbackRegistry<Func<int>>("provider", _log.Add);

        private static int[] Values(CallbackRegistry<Func<int>> registry) =>
            registry.Snapshot().Select(e => e.Callback()).ToArray();

        [Fact]
        public void StartsEmpty()
        {
            Assert.Equal(0, NewRegistry().Count);
            Assert.Empty(NewRegistry().Snapshot());
        }

        [Fact]
        public void KeepsRegistrationOrder()
        {
            var registry = NewRegistry();
            registry.Set("a", () => 1);
            registry.Set("b", () => 2);
            registry.Set("c", () => 3);

            Assert.Equal(new[] { 1, 2, 3 }, Values(registry));
            Assert.Equal(new[] { "a", "b", "c" }, registry.Snapshot().Select(e => e.Guid));
        }

        [Fact]
        public void RegisteringAgainReplacesInPlace()
        {
            var registry = NewRegistry();
            registry.Set("a", () => 1);
            registry.Set("b", () => 2);
            registry.Set("a", () => 10);

            Assert.Equal(2, registry.Count);
            Assert.Equal(new[] { 10, 2 }, Values(registry));
        }

        [Fact]
        public void NullCallbackRemovesThatGuidOnly()
        {
            var registry = NewRegistry();
            registry.Set("a", () => 1);
            registry.Set("b", () => 2);
            registry.Set("a", null);

            Assert.Equal(new[] { 2 }, Values(registry));
        }

        [Fact]
        public void NullCallbackForUnknownGuidDoesNothing()
        {
            var registry = NewRegistry();
            registry.Set("a", null);

            Assert.Equal(0, registry.Count);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void EmptyGuidIsIgnoredAndLogged(string guid)
        {
            var registry = NewRegistry();
            registry.Set(guid, () => 1);

            Assert.Equal(0, registry.Count);
            Assert.Single(_log);
        }

        [Fact]
        public void RemoveOnlyAffectsThatGuid()
        {
            var registry = NewRegistry();
            registry.Set("a", () => 1);
            registry.Set("b", () => 2);
            registry.Remove("a");
            registry.Remove("missing");

            Assert.Equal(new[] { 2 }, Values(registry));
        }

        [Fact]
        public void SnapshotIsUnaffectedByLaterChanges()
        {
            var registry = NewRegistry();
            registry.Set("a", () => 1);
            var snapshot = registry.Snapshot();
            registry.Remove("a");
            registry.Set("b", () => 2);

            Assert.Single(snapshot);
            Assert.Equal("a", snapshot[0].Guid);
        }

        [Fact]
        public void CallReturnsTheCallbacksResult()
        {
            var registry = NewRegistry();
            registry.Set("a", () => 7);

            Assert.Equal(7, registry.Call(registry.Snapshot()[0], f => f(), -1));
            Assert.Empty(_log);
        }

        [Fact]
        public void ThrowingCallbackGivesNoOpinionAndIsLoggedOnce()
        {
            var registry = NewRegistry();
            registry.Set("bad", () => throw new InvalidOperationException("boom"));
            var entry = registry.Snapshot()[0];

            Assert.Equal(-1, registry.Call(entry, f => f(), -1));
            Assert.Equal(-1, registry.Call(entry, f => f(), -1));
            Assert.Equal(-1, registry.Call(registry.Snapshot()[0], f => f(), -1));

            Assert.Single(_log);
            Assert.Contains("bad", _log[0]);
            Assert.Contains("boom", _log[0]);
        }

        [Fact]
        public void ReRegisteringAfterAThrowLogsTheNewCallbackAgain()
        {
            var registry = NewRegistry();
            registry.Set("bad", () => throw new InvalidOperationException("one"));
            registry.Call(registry.Snapshot()[0], f => f(), -1);
            registry.Set("bad", () => throw new InvalidOperationException("two"));
            registry.Call(registry.Snapshot()[0], f => f(), -1);

            Assert.Equal(2, _log.Count);
        }

        [Fact]
        public void NullLogDoesNotBreakAnything()
        {
            var registry = new CallbackRegistry<Func<int>>("provider", null);
            registry.Set(null, () => 1);
            registry.Set("bad", () => throw new Exception());

            Assert.Equal(-1, registry.Call(registry.Snapshot()[0], f => f(), -1));
        }
    }
}
