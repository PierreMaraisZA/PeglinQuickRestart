using System;
using System.Collections.Generic;
using System.Globalization;

namespace PeglinQuickRestart.Core
{
    /// <summary>
    /// Turns the names saved in a run setup (class, relic effects, orb names) back into game objects, skipping what
    /// the game no longer knows (a removed or modded item) instead of giving up on the whole restart.
    /// Pure C# (no Unity/BepInEx) so it can be unit tested.
    /// </summary>
    public static class SetupResolver
    {
        /// <summary>
        /// The enum value a saved name stands for: a known name exactly, or a plain number (how an enum value that
        /// isn't named, such as a class or relic added by another mod, is written). Null if it's neither.
        /// </summary>
        public static int? EnumValue(string saved, IReadOnlyDictionary<string, int> namedValues)
        {
            if (string.IsNullOrEmpty(saved))
            {
                return null;
            }
            if (namedValues.TryGetValue(saved, out int value))
            {
                return value;
            }
            if (int.TryParse(saved, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number))
            {
                return number;
            }
            return null;
        }

        /// <summary>
        /// Looks up each name in order. Duplicates are kept (a loadout can hold two of the same orb). A name the lookup
        /// returns null for, or throws on, goes in <see cref="Resolved{T}.Skipped"/>.
        /// </summary>
        public static Resolved<T> ResolveAll<T>(IEnumerable<string> names, Func<string, T> lookup) where T : class
        {
            var result = new Resolved<T>();
            if (names == null)
            {
                return result;
            }
            foreach (string name in names)
            {
                T found;
                try
                {
                    found = string.IsNullOrEmpty(name) ? null : lookup(name);
                }
                catch (Exception)
                {
                    found = null;
                }
                if (found != null)
                {
                    result.Found.Add(found);
                }
                else
                {
                    result.Skipped.Add(name ?? "");
                }
            }
            return result;
        }

        /// <summary>
        /// Whether to replace the class's starting orbs with the saved ones. Not when none of them could be found:
        /// a run can't start without orbs, so the class's own starting orbs are used instead.
        /// </summary>
        public static bool UseSavedOrbs<T>(Resolved<T> orbs) where T : class => orbs.Found.Count > 0;
    }

    public sealed class Resolved<T> where T : class
    {
        public List<T> Found { get; } = new List<T>();

        public List<string> Skipped { get; } = new List<string>();
    }
}
