using System;
using System.IO;
using System.Linq;
using BepInEx;
using Cruciball;
using HarmonyLib;
using PeglinQuickRestart.Core;
using PeglinUI.LoadoutManager;
using PeglinUI.MainMenu;
using UnityEngine;

namespace PeglinQuickRestart
{
    /// <summary>
    /// Remembers how the current run was set up, so a restart can start the same kind of run. Recorded when the player
    /// presses Play, and saved to a file so a run continued after closing the game can be restarted too.
    /// </summary>
    internal static class RunSetup
    {
        private static readonly string SavePath = Path.Combine(Paths.ConfigPath, "PeglinQuickRestart.lastrun.txt");

        internal static readonly AccessTools.FieldRef<LoadoutManager, CruciballManager> LoadoutCruciballManager =
            AccessTools.FieldRefAccess<LoadoutManager, CruciballManager>("_cruciballManager");

        /// <summary>Recorded on Play, waiting for the run to start so its seed is known.</summary>
        internal static RunSetupRecord Pending;

        /// <summary>The run being played, if it was started (or its record loaded) this session.</summary>
        private static RunSetupRecord _current;

        internal static void RecordPlay(LoadoutManager loadout)
        {
            CruciballManager cruciball = LoadoutCruciballManager(loadout);
            var record = new RunSetupRecord
            {
                Class = StaticGameData.chosenClass.ToString(),
                CruciballLevel = cruciball != null ? cruciball.currentCruciballLevel : 0,
                CustomSeed = loadout.seedInputPopup.hasCustomSeed,
                Seed = loadout.seedInputPopup.hasCustomSeed ? StaticGameData.currentSeed : "",
                CustomLoadout = loadout.hasCustomLoadout,
            };
            if (record.CustomLoadout)
            {
                record.Orbs = loadout.StartingOrbs.Select(orb => orb.name).ToList();
                record.Relics = loadout.StartingRelics.Select(relic => relic.effect.ToString()).ToList();
            }
            Pending = record;
        }

        /// <summary>Called when a new run has started: its seed is now set, so the record is complete.</summary>
        internal static void RunStarted()
        {
            if (Pending == null)
            {
                return;
            }
            Pending.RunSeed = StaticGameData.currentSeed;
            _current = Pending;
            Pending = null;
            try
            {
                File.WriteAllText(SavePath, _current.Serialize());
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Couldn't save the run setup to {SavePath}: {e.Message}");
            }
        }

        /// <summary>
        /// How the run being played was set up, or null if that isn't known (a custom run started before the mod was
        /// installed). A standard run can always be restarted: it only needs the class and Cruciball level.
        /// </summary>
        internal static RunSetupRecord ForCurrentRun()
        {
            string seed = StaticGameData.currentSeed;
            if (string.IsNullOrEmpty(seed))
            {
                return null;
            }
            if (_current != null && _current.IsForRun(seed))
            {
                return _current;
            }
            RunSetupRecord saved = Load();
            if (saved != null && saved.IsForRun(seed))
            {
                _current = saved;
                return saved;
            }
            if (StaticGameData.CurrentRunStats != null && !StaticGameData.CurrentRunStats.isCustomRun)
            {
                CruciballManager cruciball = Resources.FindObjectsOfTypeAll<CruciballManager>().FirstOrDefault();
                return new RunSetupRecord
                {
                    Class = StaticGameData.chosenClass.ToString(),
                    CruciballLevel = cruciball != null ? cruciball.currentCruciballLevel : 0,
                    RunSeed = seed,
                };
            }
            return null;
        }

        private static RunSetupRecord Load()
        {
            try
            {
                return File.Exists(SavePath) ? RunSetupRecord.Parse(File.ReadAllText(SavePath)) : null;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Couldn't read the run setup from {SavePath}: {e.Message}");
                return null;
            }
        }
    }

    /// <summary>The game's Play button (also what a restart presses): record the setup before the run starts.</summary>
    [HarmonyPatch(typeof(PlayButton), nameof(PlayButton.ConfirmRunConfigAndStartGame))]
    internal static class RecordSetupOnPlayPatch
    {
        private static void Prefix()
        {
            LoadoutManager loadout = UnityEngine.Object.FindObjectOfType<LoadoutManager>();
            if (loadout == null)
            {
                Plugin.Log.LogWarning("Couldn't find the loadout screen when the run started, so this run can't be restarted with the same setup.");
                RunSetup.Pending = null;
                return;
            }
            RunSetup.RecordPlay(loadout);
        }
    }

    /// <summary>GameInit.Start picks the random seed for a new run; record it once it has.</summary>
    [HarmonyPatch(typeof(GameInit), "Start")]
    internal static class RecordSeedOnRunStartPatch
    {
        private static readonly AccessTools.FieldRef<GameInit, LoadMapData> LoadData =
            AccessTools.FieldRefAccess<GameInit, LoadMapData>("LoadData");

        private static void Postfix(GameInit __instance)
        {
            LoadMapData data = LoadData(__instance);
            if (data != null && data.NewGame)
            {
                RunSetup.RunStarted();
            }
        }
    }
}
