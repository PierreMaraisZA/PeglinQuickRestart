using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PeglinQuickRestart.Core
{
    /// <summary>
    /// What the player picked when starting a run: class, Cruciball level, and any custom seed or custom loadout.
    /// Orbs and relics are kept by name (orb prefab name, relic effect name) so the record can be saved to a file and
    /// still restart a run continued after the game was closed. Pure C# (no Unity/BepInEx) so it can be unit tested.
    /// </summary>
    public sealed class RunSetupRecord
    {
        /// <summary>The class, as the name of the game's Class enum value.</summary>
        public string Class = "";

        public int CruciballLevel;

        /// <summary>True when the player entered a seed. A restart then uses the same seed; otherwise a new random one.</summary>
        public bool CustomSeed;

        /// <summary>The seed the player entered. Empty unless CustomSeed.</summary>
        public string Seed = "";

        /// <summary>True when the player edited the starting orbs or relics.</summary>
        public bool CustomLoadout;

        public List<string> Orbs = new List<string>();

        public List<string> Relics = new List<string>();

        /// <summary>
        /// The seed the run actually used (the random one, for a run without a custom seed). Identifies which run this
        /// record belongs to, so a stale record is never applied to a different run.
        /// </summary>
        public string RunSeed = "";

        public bool IsForRun(string currentSeed)
        {
            return !string.IsNullOrEmpty(RunSeed) && RunSeed == currentSeed;
        }

        public string Serialize()
        {
            var text = new StringBuilder();
            text.Append("Class=").Append(Class).Append('\n');
            text.Append("CruciballLevel=").Append(CruciballLevel).Append('\n');
            text.Append("CustomSeed=").Append(CustomSeed).Append('\n');
            text.Append("Seed=").Append(Seed).Append('\n');
            text.Append("CustomLoadout=").Append(CustomLoadout).Append('\n');
            text.Append("Orbs=").Append(string.Join(",", Orbs)).Append('\n');
            text.Append("Relics=").Append(string.Join(",", Relics)).Append('\n');
            text.Append("RunSeed=").Append(RunSeed).Append('\n');
            return text.ToString();
        }

        /// <summary>Reads a record written by Serialize. Returns null if the text isn't one (missing or bad values).</summary>
        public static RunSetupRecord Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            var values = new Dictionary<string, string>();
            foreach (string line in text.Split('\n'))
            {
                int equals = line.IndexOf('=');
                if (equals > 0)
                {
                    values[line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim();
                }
            }
            if (!values.TryGetValue("Class", out string cls) || cls.Length == 0
                || !values.TryGetValue("CruciballLevel", out string level) || !int.TryParse(level, out int cruciballLevel)
                || !values.TryGetValue("CustomSeed", out string customSeed) || !bool.TryParse(customSeed, out bool hasCustomSeed)
                || !values.TryGetValue("CustomLoadout", out string customLoadout) || !bool.TryParse(customLoadout, out bool hasCustomLoadout)
                || !values.TryGetValue("RunSeed", out string runSeed) || runSeed.Length == 0)
            {
                return null;
            }
            values.TryGetValue("Seed", out string seed);
            if (hasCustomSeed && string.IsNullOrEmpty(seed))
            {
                return null;
            }
            values.TryGetValue("Orbs", out string orbs);
            values.TryGetValue("Relics", out string relics);
            return new RunSetupRecord
            {
                Class = cls,
                CruciballLevel = cruciballLevel,
                CustomSeed = hasCustomSeed,
                Seed = hasCustomSeed ? seed : "",
                CustomLoadout = hasCustomLoadout,
                Orbs = SplitList(orbs),
                Relics = SplitList(relics),
                RunSeed = runSeed,
            };
        }

        private static List<string> SplitList(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new List<string>();
            }
            return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToList();
        }
    }
}
