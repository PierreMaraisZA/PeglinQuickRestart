using System.Collections.Generic;
using PeglinQuickRestart.Core;
using Xunit;

namespace PeglinQuickRestart.Tests
{
    public class RunSetupRecordTests
    {
        [Fact]
        public void Parse_ReadsBackACustomRun()
        {
            var record = new RunSetupRecord
            {
                Class = "Roundrel",
                CruciballLevel = 7,
                CustomSeed = true,
                Seed = "1234567",
                CustomLoadout = true,
                Orbs = new List<string> { "StoneOrb-Lvl1", "StoneOrb-Lvl1", "Daggorb-Lvl2" },
                Relics = new List<string> { "MATRYOSHKA_SHELL" },
                RunSeed = "1234567",
            };

            RunSetupRecord read = RunSetupRecord.Parse(record.Serialize());

            Assert.NotNull(read);
            Assert.Equal("Roundrel", read.Class);
            Assert.Equal(7, read.CruciballLevel);
            Assert.True(read.CustomSeed);
            Assert.Equal("1234567", read.Seed);
            Assert.True(read.CustomLoadout);
            Assert.Equal(record.Orbs, read.Orbs);
            Assert.Equal(record.Relics, read.Relics);
            Assert.Equal("1234567", read.RunSeed);
        }

        [Fact]
        public void Parse_ReadsBackAStandardRunWithEmptyLists()
        {
            var record = new RunSetupRecord { Class = "Peglin", RunSeed = "7654321" };

            RunSetupRecord read = RunSetupRecord.Parse(record.Serialize());

            Assert.NotNull(read);
            Assert.False(read.CustomSeed);
            Assert.Equal("", read.Seed);
            Assert.False(read.CustomLoadout);
            Assert.Empty(read.Orbs);
            Assert.Empty(read.Relics);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("garbage")]
        [InlineData("Class=Peglin\nCruciballLevel=x\nCustomSeed=False\nCustomLoadout=False\nRunSeed=1\n")]  // bad level
        [InlineData("Class=Peglin\nCruciballLevel=0\nCustomSeed=False\nCustomLoadout=False\nRunSeed=\n")]   // no run seed
        [InlineData("Class=Peglin\nCruciballLevel=0\nCustomSeed=True\nSeed=\nCustomLoadout=False\nRunSeed=1\n")] // custom seed missing
        public void Parse_RejectsTextThatIsntARecord(string text)
        {
            Assert.Null(RunSetupRecord.Parse(text));
        }

        [Fact]
        public void Parse_ToleratesWindowsLineEndings()
        {
            string text = new RunSetupRecord { Class = "Peglin", RunSeed = "1" }.Serialize().Replace("\n", "\r\n");
            Assert.NotNull(RunSetupRecord.Parse(text));
        }

        [Theory]
        [InlineData("1234567", true)]
        [InlineData("7654321", false)]
        [InlineData("", false)]
        public void IsForRun_MatchesOnlyTheSameSeed(string currentSeed, bool expected)
        {
            var record = new RunSetupRecord { RunSeed = "1234567" };
            Assert.Equal(expected, record.IsForRun(currentSeed));
        }

        [Fact]
        public void IsForRun_NeverMatchesARecordWithoutASeed()
        {
            Assert.False(new RunSetupRecord().IsForRun(""));
        }
    }
}
