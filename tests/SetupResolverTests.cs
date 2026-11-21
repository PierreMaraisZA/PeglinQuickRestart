using System;
using System.Collections.Generic;
using PeglinQuickRestart.Core;
using Xunit;

namespace PeglinQuickRestart.Tests
{
    public class SetupResolverTests
    {
        private static readonly Dictionary<string, int> Classes = new Dictionary<string, int>
        {
            ["Peglin"] = 0,
            ["Balladin"] = 1,
            ["Roundrel"] = 2,
            ["Spinventor"] = 3,
        };

        private sealed class Item
        {
            public Item(string name) => Name = name;

            public string Name { get; }
        }

        private static readonly Dictionary<string, Item> Known = new Dictionary<string, Item>
        {
            ["StoneOrb"] = new Item("StoneOrb"),
            ["Daggorb"] = new Item("Daggorb"),
        };

        private static Item Lookup(string name) => Known.TryGetValue(name, out Item item) ? item : null;

        // --- EnumValue ---

        [Theory]
        [InlineData("Peglin", 0)]
        [InlineData("Spinventor", 3)]
        public void EnumValue_KnownName(string saved, int expected)
        {
            Assert.Equal(expected, SetupResolver.EnumValue(saved, Classes));
        }

        [Theory]
        [InlineData("7", 7)]
        [InlineData("-3", -3)]
        [InlineData("2", 2)]
        public void EnumValue_NumberForAValueWithoutAName(string saved, int expected)
        {
            Assert.Equal(expected, SetupResolver.EnumValue(saved, Classes));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Necromancer")]
        [InlineData("peglin")]
        [InlineData("1.5")]
        [InlineData("99999999999")]
        public void EnumValue_UnknownIsNull(string saved)
        {
            Assert.Null(SetupResolver.EnumValue(saved, Classes));
        }

        // --- ResolveAll ---

        [Fact]
        public void ResolveAll_AllKnown()
        {
            var result = SetupResolver.ResolveAll(new[] { "StoneOrb", "Daggorb" }, Lookup);

            Assert.Equal(new[] { "StoneOrb", "Daggorb" }, result.Found.ConvertAll(i => i.Name));
            Assert.Empty(result.Skipped);
        }

        [Fact]
        public void ResolveAll_SkipsUnknownAndKeepsTheRestInOrder()
        {
            var result = SetupResolver.ResolveAll(new[] { "Daggorb", "ModdedOrb", "StoneOrb" }, Lookup);

            Assert.Equal(new[] { "Daggorb", "StoneOrb" }, result.Found.ConvertAll(i => i.Name));
            Assert.Equal(new[] { "ModdedOrb" }, result.Skipped);
        }

        [Fact]
        public void ResolveAll_KeepsDuplicates()
        {
            var result = SetupResolver.ResolveAll(new[] { "StoneOrb", "StoneOrb" }, Lookup);

            Assert.Equal(2, result.Found.Count);
        }

        [Fact]
        public void ResolveAll_ThrowingLookupSkipsThatNameOnly()
        {
            var result = SetupResolver.ResolveAll(new[] { "Broken", "StoneOrb" },
                name => name == "Broken" ? throw new InvalidOperationException() : Lookup(name));

            Assert.Single(result.Found);
            Assert.Equal(new[] { "Broken" }, result.Skipped);
        }

        [Fact]
        public void ResolveAll_EmptyAndNullNamesAreSkippedWithoutCallingTheLookup()
        {
            int calls = 0;
            var result = SetupResolver.ResolveAll(new[] { "", null }, name => { calls++; return Lookup(name); });

            Assert.Empty(result.Found);
            Assert.Equal(2, result.Skipped.Count);
            Assert.Equal(0, calls);
        }

        [Fact]
        public void ResolveAll_EmptyOrNullList()
        {
            Assert.Empty(SetupResolver.ResolveAll(new string[0], Lookup).Found);
            Assert.Empty(SetupResolver.ResolveAll<Item>(null, Lookup).Skipped);
        }

        // --- UseSavedOrbs ---

        [Fact]
        public void UseSavedOrbs_WhenAnyWasFound()
        {
            Assert.True(SetupResolver.UseSavedOrbs(SetupResolver.ResolveAll(new[] { "ModdedOrb", "StoneOrb" }, Lookup)));
        }

        [Fact]
        public void UseSavedOrbs_NotWhenNoneWasFound()
        {
            Assert.False(SetupResolver.UseSavedOrbs(SetupResolver.ResolveAll(new[] { "ModdedOrb" }, Lookup)));
            Assert.False(SetupResolver.UseSavedOrbs(SetupResolver.ResolveAll(new string[0], Lookup)));
        }
    }
}
