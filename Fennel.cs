using System;
using System.Diagnostics;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using ModCommon;
using MonoMod.RuntimeDetour;
using UnityEngine.SceneManagement;
using UnityEngine;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;

namespace Fennel
{
    [UsedImplicitly]
    public class Fennel : Mod, ITogglableMod
    {
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();

        public static Fennel Instance;

        public override string GetVersion()
        {
            return "1.0.0.0";
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Hornet_2","Boss Holder/Hornet Boss 2"),
                ("Ruins1_24_boss", "Mage Lord"),
                ("GG_Hollow_Knight", "Battle Scene/Focus Blasts/HK Prime Blast (4)/Blast")
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            preloadedGO.Add("hornet", preloadedObjects["GG_Hornet_2"]["Boss Holder/Hornet Boss 2"]);
            preloadedGO.Add("wave", preloadedObjects["Ruins1_24_boss"]["Mage Lord"]);
            preloadedGO.Add("orb", preloadedObjects["GG_Hollow_Knight"]["Battle Scene/Focus Blasts/HK Prime Blast (4)/Blast"]);
            preloadedGO.Add("fennel", null);

            Instance = this;
            Log("Initalizing.");

            Unload();
            ModHooks.Instance.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += LangGet;
        }

        private string LangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "FENNEL_NAME": return "Pardoner Fennel";
                case "FENNEL_DESC": return "Zealous follower of the Weeping God.";
                case "FENNEL_INTRO": return "Hmm...Pale One, why do you resist so? This kingdom's fate was sealed upon its inception, it sleeps now as a graveyard of false gods.<page>" +
                        "I know the tale of this land and the betrayal that gave it birth. The gods here forsook you, let Us take the last fragments of life lingering here so that the purpose of the God of gods may be fulfilled.<page>" +
                        "Be warned, denial will bring only pain.<page>";
                case "FENNEL_END": return "You...truly are a fool.<page>";
                default: return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private void AfterSaveGameLoad(SaveGameData data) => AddComponent();

        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<ArenaFinder>();
        }

        public void Unload()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            ModHooks.Instance.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= LangGet;

            var x = GameManager.instance?.gameObject.GetComponent<ArenaFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}