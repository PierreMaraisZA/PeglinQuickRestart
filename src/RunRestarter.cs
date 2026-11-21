using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cruciball;
using HarmonyLib;
using Loading;
using Peglin.ClassSystem;
using PeglinQuickRestart.Core;
using PeglinUI.LoadoutManager;
using PeglinUI.MainMenu;
using PeglinUI.MainMenu.Cruciball;
using PeglinUI.RunSummary;
using Relics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PeglinQuickRestart
{
    internal enum RestartResult
    {
        /// <summary>Nothing happened (blocked, already restarting, or the run's setup isn't known).</summary>
        NotStarted,

        /// <summary>Another mod's restart handler took it; QuickRestart did nothing else.</summary>
        Handled,

        /// <summary>QuickRestart's own restart is under way.</summary>
        Started,
    }

    /// <summary>
    /// Restarts the run the way a player would: leave the run (Abandon Run mid-run, or End Run on the run-over
    /// screen), go to the main menu, press Play, pick the same class, Cruciball level and loadout, and press Play
    /// again. Going through the menu runs the game's own cleanup and new-run setup, so the new run is the same as one
    /// started by hand.
    /// </summary>
    internal sealed class RunRestarter
    {
        private const string MainMenuScene = "MainMenu";
        private const float SceneLoadTimeout = 30f;
        private const float MenuTimeout = 10f;
        private const float NoticeSeconds = 4f;

        private static readonly AccessTools.FieldRef<CharacterSelectController, List<SelectableCharacter>> AvailableCharacters =
            AccessTools.FieldRefAccess<CharacterSelectController, List<SelectableCharacter>>("_availableCharacters");

        private static readonly AccessTools.FieldRef<CharacterSelectController, bool> ClassesLoaded =
            AccessTools.FieldRefAccess<CharacterSelectController, bool>("_classesLoaded");

        private static readonly AccessTools.FieldRef<LoadoutManager, DeckManager> LoadoutDeckManager =
            AccessTools.FieldRefAccess<LoadoutManager, DeckManager>("_deckManager");

        private static readonly AccessTools.FieldRef<LoadoutManager, RelicManager> LoadoutRelicManager =
            AccessTools.FieldRefAccess<LoadoutManager, RelicManager>("_relicManager");

        private static readonly MethodInfo ChangeClass =
            AccessTools.Method(typeof(CharacterSelectController), "ChangeClass", new[] { typeof(SelectableCharacter) });

        private static readonly IReadOnlyDictionary<string, int> ClassNames = EnumNames(typeof(Class));
        private static readonly IReadOnlyDictionary<string, int> RelicNames = EnumNames(typeof(RelicEffect));

        private readonly MonoBehaviour _host;
        private string _notice;
        private float _noticeUntil;

        internal RunRestarter(MonoBehaviour host)
        {
            _host = host;
            Instance = this;
        }

        internal static RunRestarter Instance { get; private set; }

        internal bool Busy { get; private set; }

        /// <summary>A short message to show on screen (why a restart didn't happen), or null.</summary>
        internal string Notice => Time.unscaledTime < _noticeUntil ? _notice : null;

        /// <summary>Set once the run is won, so the key can't abandon it before the game records the win.</summary>
        internal static readonly WinGate Wins = new WinGate();

        /// <summary>Anywhere but the main menu, and not while a scene is loading.</summary>
        internal bool InRestartableScene()
        {
            PeglinSceneLoader loader = UnityEngine.Object.FindObjectOfType<PeglinSceneLoader>();
            return loader != null && !loader.isLoading && SceneManager.GetActiveScene().name != MainMenuScene;
        }

        /// <summary>A restartable scene, and not after the run has been won (unless another mod's handler takes it).</summary>
        internal bool CanRestartHere()
        {
            return InRestartableScene() && !Wins.Blocks(QuickRestartApi.Registry.AnyCanHandle());
        }

        /// <summary>Whether a restart asked for now would go ahead (see <see cref="RestartRules.CanRestart"/>).</summary>
        internal bool CanRestartNow()
        {
            // On the run-over screen a restart is already on its way once End Run or Restart has been pressed.
            RunOverScreen screen = RunOverScreen.Current;
            bool restarting = Busy || _requesting || (screen != null && screen.Leaving);
            return RestartRules.CanRestart(InRestartableScene(), restarting, QuickRestartApi.Registry.IsBlocked(),
                QuickRestartApi.Registry.AnyCanHandle(), Wins.Won, () => RunSetup.ForCurrentRun() != null);
        }

        /// <summary>
        /// Restarts the current run. Another mod's restart handler gets it first. <paramref name="endRun"/> is the
        /// run-over screen's End Run button when restarting from there, or null mid-run. <paramref name="source"/> is
        /// what asked (<see cref="RestartSources"/>), passed on to restart listeners.
        /// </summary>
        internal RestartResult Request(Button endRun, string source)
        {
            // A handler or listener asking for another restart while this one is being decided gets nothing.
            if (_requesting)
            {
                return RestartResult.NotStarted;
            }
            _requesting = true;
            try
            {
                RestartResult result = Decide(endRun);
                if (result != RestartResult.NotStarted)
                {
                    QuickRestartApi.Registry.NotifyRestart(source);
                }
                return result;
            }
            finally
            {
                _requesting = false;
            }
        }

        private bool _requesting;

        private RestartResult Decide(Button endRun)
        {
            // Not while a scene is loading (e.g. a handler's own restart still on its way to the menu).
            if (Busy || !InRestartableScene() || QuickRestartApi.Registry.IsBlocked())
            {
                return RestartResult.NotStarted;
            }
            if (QuickRestartApi.Registry.TryHandle())
            {
                Plugin.Log.LogInfo("Restart handed to another mod's restart handler.");
                return RestartResult.Handled;
            }
            if (Wins.Blocks(false))
            {
                Plugin.Log.LogInfo("Restart ignored: the run has been won, so the game's own buttons finish it.");
                return RestartResult.NotStarted;
            }
            RunSetupRecord setup = RunSetup.ForCurrentRun();
            if (setup == null)
            {
                Show("Can't restart: this run's custom setup isn't known");
                Plugin.Log.LogWarning("Can't restart: no run in progress, or a custom run started before the mod was installed.");
                return RestartResult.NotStarted;
            }
            _host.StartCoroutine(Restart(setup, endRun));
            return RestartResult.Started;
        }

        private IEnumerator Restart(RunSetupRecord setup, Button endRun)
        {
            Busy = true;
            try
            {
                Plugin.Log.LogInfo($"Restarting run: {setup.Class}, Cruciball {setup.CruciballLevel}, " +
                    (setup.CustomSeed ? $"seed {setup.Seed}" : "random seed") + (setup.CustomLoadout ? ", custom loadout" : "") + ".");

                float waited = 0f;
                PeglinSceneLoader loader;
                if (endRun != null)
                {
                    // Exactly what End Run does: the game then records the finished run on the run summary screen.
                    endRun.onClick.Invoke();
                    RunSummary summary = null;
                    while (summary == null)
                    {
                        if ((waited += Time.unscaledDeltaTime) > SceneLoadTimeout)
                        {
                            Fail("the run summary didn't load");
                            yield break;
                        }
                        yield return null;
                        loader = UnityEngine.Object.FindObjectOfType<PeglinSceneLoader>();
                        summary = loader != null && !loader.isLoading ? UnityEngine.Object.FindObjectOfType<RunSummary>() : null;
                    }
                    // The run is recorded when the summary is enabled; give it a frame to finish starting, then leave.
                    yield return null;
                    summary.LoadMainMenu();
                    waited = 0f;
                }
                else
                {
                    AbandonRun();
                }

                loader = UnityEngine.Object.FindObjectOfType<PeglinSceneLoader>();
                while (loader == null || loader.isLoading || SceneManager.GetActiveScene().name != MainMenuScene)
                {
                    if ((waited += Time.unscaledDeltaTime) > SceneLoadTimeout)
                    {
                        Fail("the main menu didn't load");
                        yield break;
                    }
                    yield return null;
                    loader = UnityEngine.Object.FindObjectOfType<PeglinSceneLoader>();
                }

                PlayButton play = UnityEngine.Object.FindObjectOfType<PlayButton>();
                if (play == null)
                {
                    Fail("couldn't find the Play button");
                    yield break;
                }
                // Same as pressing Play on the title screen, which also saves the last run's class unlock progress.
                play.MovetoCharacterSelect();

                LoadoutManager loadout = null;
                CharacterSelectController characters = null;
                CruciballLevelSelector cruciballSelector = null;
                waited = 0f;
                while (true)
                {
                    loadout = UnityEngine.Object.FindObjectOfType<LoadoutManager>();
                    characters = loadout != null ? loadout.characterSelectController : null;
                    cruciballSelector = UnityEngine.Object.FindObjectOfType<CruciballLevelSelector>();
                    // The title screen's characters may already be loaded, so also wait for the switch to character select
                    // (it re-enables the loadout screen, which clears the seed; ours has to be set after that).
                    if (play._characterSelectGroup.gameObject.activeInHierarchy
                        && loadout != null && characters != null && ClassesLoaded(characters) && cruciballSelector != null)
                    {
                        break;
                    }
                    if ((waited += Time.unscaledDeltaTime) > MenuTimeout)
                    {
                        Fail("the character select screen didn't open");
                        yield break;
                    }
                    yield return null;
                }
                // Let the character select screen finish its own setup (Start, the first class change) before changing it.
                yield return null;

                string problem = ApplySetup(setup, loadout, characters);
                if (problem != null)
                {
                    Fail(problem);
                    yield break;
                }

                // Press Play. The prefix on ConfirmRunConfigAndStartGame records this run's setup like any other.
                if (cruciballSelector.playButton != null && cruciballSelector.playButton.onClick.GetPersistentEventCount() > 0)
                {
                    cruciballSelector.playButton.onClick.Invoke();
                }
                else
                {
                    loadout.SetupDataForNewGame();
                    play.ConfirmRunConfigAndStartGame();
                }
            }
            finally
            {
                // Also on an exception from the game, so a failed restart never leaves the key and buttons locked.
                Busy = false;
            }
        }

        /// <summary>The same as the pause menu's Abandon Run.</summary>
        private static void AbandonRun()
        {
            PauseMenu pauseMenu = UnityEngine.Object.FindObjectOfType<PauseMenu>();
            if (pauseMenu != null)
            {
                pauseMenu.AbandonRun();
                return;
            }
            // No pause menu in this scene (e.g. the run summary): do what AbandonRun does.
            if (!StaticGameData.hasReachedBoss)
            {
                PlayerPrefs.SetInt(GameInit.BOSS_STREAK_PREF_STRING, 0);
            }
            if (PauseMenu.Paused)
            {
                PauseMenu.Paused = false;
                PauseMenu.OnPauseToggled?.Invoke(false);
            }
            SaveManager.RequestDelete();
            PeglinSceneLoader.Instance.LoadScene(PeglinSceneLoader.Scene.MAIN_MENU);
        }

        /// <summary>
        /// Picks the class, Cruciball level, seed and loadout on the character select screen. Returns a problem, or
        /// null. A class, orb or relic the game doesn't know (removed, or from a mod that isn't loaded) is skipped
        /// with a log line rather than stopping the restart.
        /// </summary>
        private static string ApplySetup(RunSetupRecord setup, LoadoutManager loadout, CharacterSelectController characters)
        {
            int? classValue = SetupResolver.EnumValue(setup.Class, ClassNames);
            SelectableCharacter character = classValue == null ? null
                : AvailableCharacters(characters)?.FirstOrDefault(c => c != null && (int)c.characterClass == classValue.Value);
            if (character == null)
            {
                Plugin.Log.LogWarning($"Class {setup.Class} isn't known to the game; restarting as {characters.currentlySelectedClass} " +
                    "with that class's own Cruciball level.");
            }
            else
            {
                Class cls = character.characterClass;
                if (!character.IsUnlocked)
                {
                    return $"{setup.Class} isn't available";
                }
                if (characters.currentlySelectedClass != cls)
                {
                    // Like clicking the character: the loadout and Cruciball selector follow the class change.
                    ChangeClass.Invoke(characters, new object[] { character });
                }
                if (characters.currentlySelectedClass != cls)
                {
                    return $"couldn't select {setup.Class}";
                }

                // The Cruciball selector picks the level last chosen for the class, which is normally the one played.
                CruciballManager cruciball = RunSetup.LoadoutCruciballManager(loadout);
                if (cruciball != null && cruciball.currentCruciballLevel != setup.CruciballLevel)
                {
                    cruciball.SetRunCruciballLevelAndClass(setup.CruciballLevel, cls);
                }
            }

            if (setup.CustomLoadout)
            {
                DeckManager deck = LoadoutDeckManager(loadout);
                RelicManager relics = LoadoutRelicManager(loadout);
                Resolved<GameObject> orbs = SetupResolver.ResolveAll(setup.Orbs, name => deck != null ? deck.GetOrbPrefabFromName(name) : null);
                Resolved<Relic> startingRelics = SetupResolver.ResolveAll(setup.Relics, name => FindRelic(relics, name));
                foreach (string name in orbs.Skipped)
                {
                    Plugin.Log.LogWarning($"Starting orb {name} isn't known to the game; restarting without it.");
                }
                foreach (string name in startingRelics.Skipped)
                {
                    Plugin.Log.LogWarning($"Starting relic {name} isn't known to the game; restarting without it.");
                }
                loadout.hasCustomLoadout = true;
                if (SetupResolver.UseSavedOrbs(orbs))
                {
                    loadout.StartingOrbs.Clear();
                    loadout.StartingOrbs.AddRange(orbs.Found);
                }
                else if (setup.Orbs.Count > 0)
                {
                    Plugin.Log.LogWarning("None of the starting orbs could be found; using the class's own starting orbs.");
                }
                loadout.StartingRelics.Clear();
                loadout.StartingRelics.AddRange(startingRelics.Found);
            }

            // An empty seed makes the game roll a new random one when the run starts.
            loadout.seedInputPopup.hasCustomSeed = setup.CustomSeed;
            StaticGameData.currentSeed = setup.CustomSeed ? setup.Seed : string.Empty;
            return null;
        }

        /// <summary>The relic for a saved effect name, or null. NONE would give the game's consolation prize, so it's unknown too.</summary>
        private static Relic FindRelic(RelicManager relics, string name)
        {
            int? effect = SetupResolver.EnumValue(name, RelicNames);
            if (relics == null || effect == null || effect.Value == (int)RelicEffect.NONE)
            {
                return null;
            }
            return relics.GetRelicForEffectFromAllData((RelicEffect)effect.Value);
        }

        private static IReadOnlyDictionary<string, int> EnumNames(Type enumType)
        {
            return Enum.GetNames(enumType).ToDictionary(name => name, name => Convert.ToInt32(Enum.Parse(enumType, name)));
        }

        private void Fail(string reason)
        {
            Busy = false;
            Show("Restart stopped: " + reason);
            Plugin.Log.LogWarning($"Restart stopped: {reason}.");
        }

        private void Show(string message)
        {
            _notice = message;
            _noticeUntil = Time.unscaledTime + NoticeSeconds;
        }
    }
}
