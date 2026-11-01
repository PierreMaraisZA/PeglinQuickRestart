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
using Relics;
using Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeglinQuickRestart
{
    /// <summary>
    /// Restarts the run the way a player would: abandon it (like Abandon Run in the pause menu), go to the main menu,
    /// press Play, pick the same class, Cruciball level and loadout, and press Play again. Going through the menu runs
    /// the game's own cleanup and new-run setup, so the new run is the same as one started by hand.
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

        private readonly MonoBehaviour _host;
        private string _notice;
        private float _noticeUntil;

        internal RunRestarter(MonoBehaviour host)
        {
            _host = host;
        }

        internal bool Busy { get; private set; }

        /// <summary>A short message to show on screen (why a restart didn't happen), or null.</summary>
        internal string Notice => Time.unscaledTime < _noticeUntil ? _notice : null;

        /// <summary>Anywhere but the main menu, and not while a scene is loading.</summary>
        internal bool InRestartableScene()
        {
            PeglinSceneLoader loader = UnityEngine.Object.FindObjectOfType<PeglinSceneLoader>();
            return loader != null && !loader.isLoading && SceneManager.GetActiveScene().name != MainMenuScene;
        }

        internal void TryRestart()
        {
            RunSetupRecord setup = RunSetup.ForCurrentRun();
            if (setup == null)
            {
                Show("Can't restart: this run's custom setup isn't known");
                Plugin.Log.LogWarning("Can't restart: no run in progress, or a custom run started before the mod was installed.");
                return;
            }
            if (!Enum.IsDefined(typeof(Class), setup.Class))
            {
                Show("Can't restart: unknown class " + setup.Class);
                return;
            }
            _host.StartCoroutine(Restart(setup));
        }

        private IEnumerator Restart(RunSetupRecord setup)
        {
            Busy = true;
            Plugin.Log.LogInfo($"Restarting run: {setup.Class}, Cruciball {setup.CruciballLevel}, " +
                (setup.CustomSeed ? $"seed {setup.Seed}" : "random seed") + (setup.CustomLoadout ? ", custom loadout" : "") + ".");

            AbandonRun();

            float waited = 0f;
            PeglinSceneLoader loader = UnityEngine.Object.FindObjectOfType<PeglinSceneLoader>();
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
            Busy = false;
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

        /// <summary>Picks the class, Cruciball level, seed and loadout on the character select screen. Returns a problem, or null.</summary>
        private static string ApplySetup(RunSetupRecord setup, LoadoutManager loadout, CharacterSelectController characters)
        {
            var cls = (Class)Enum.Parse(typeof(Class), setup.Class);
            SelectableCharacter character = AvailableCharacters(characters).FirstOrDefault(c => c.characterClass == cls);
            if (character == null || !character.IsUnlocked)
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

            if (setup.CustomLoadout)
            {
                DeckManager deck = LoadoutDeckManager(loadout);
                RelicManager relics = LoadoutRelicManager(loadout);
                var orbs = new List<GameObject>();
                foreach (string name in setup.Orbs)
                {
                    GameObject orb = deck != null ? deck.GetOrbPrefabFromName(name) : null;
                    if (orb == null)
                    {
                        return $"couldn't find starting orb {name}";
                    }
                    orbs.Add(orb);
                }
                var startingRelics = new List<Relic>();
                foreach (string name in setup.Relics)
                {
                    Relic relic = relics != null && Enum.IsDefined(typeof(RelicEffect), name)
                        ? relics.GetRelicForEffectFromAllData((RelicEffect)Enum.Parse(typeof(RelicEffect), name))
                        : null;
                    if (relic == null)
                    {
                        return $"couldn't find starting relic {name}";
                    }
                    startingRelics.Add(relic);
                }
                loadout.hasCustomLoadout = true;
                loadout.StartingOrbs.Clear();
                loadout.StartingOrbs.AddRange(orbs);
                loadout.StartingRelics.Clear();
                loadout.StartingRelics.AddRange(startingRelics);
            }

            // An empty seed makes the game roll a new random one when the run starts.
            loadout.seedInputPopup.hasCustomSeed = setup.CustomSeed;
            StaticGameData.currentSeed = setup.CustomSeed ? setup.Seed : string.Empty;
            return null;
        }

        private void Fail(string reason)
        {
            Busy = false;
            Show("Restart stopped: " + reason);
            Plugin.Log.LogWarning($"Restart stopped at the main menu: {reason}.");
        }

        private void Show(string message)
        {
            _notice = message;
            _noticeUntil = Time.unscaledTime + NoticeSeconds;
        }
    }
}
