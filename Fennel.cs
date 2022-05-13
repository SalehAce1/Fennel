using System;
using System.Diagnostics;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using UnityEngine;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;

namespace Fennel
{
    [UsedImplicitly]
    public class Fennel : Mod, ITogglableMod,IGlobalSettings<GlobalModSettings>
    {
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();
        public static Dictionary<string, AssetBundle> assetbundles = new Dictionary<string, AssetBundle>();
        public static readonly List<Sprite> Sprites = new List<Sprite>();
        public static Fennel Instance;
       public static GlobalModSettings _settings = new GlobalModSettings();
        
        public override string GetVersion()
        {
            return "1.0.0.0";
        }
        public void OnLoadGlobal(GlobalModSettings s) => _settings = s;
        public GlobalModSettings OnSaveGlobal() => _settings;
        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Hornet_2","Boss Holder/Hornet Boss 2"),
                ("Ruins1_24_boss", "Mage Lord"),
                ("GG_Hollow_Knight", "Battle Scene/Focus Blasts/HK Prime Blast (4)/Blast"),
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
            On.HeroController.Start += AddCP;
            ModHooks.LanguageGetHook += LangGet;
            ModHooks.SetPlayerVariableHook += SetVariableHook;
            ModHooks.GetPlayerVariableHook += GetVariableHook;
            
            string path = "";
            string path2 = "";
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    path = "fennelWin";
                    path2 = "outlineWin";
                    break;
                case OperatingSystemFamily.Linux:
                    path = "fennelLin";
                    path2 = "outlineLin";
                    break;
                case OperatingSystemFamily.MacOSX:
                    path = "fennelMC";
                    path2 = "outlineMC";
                    break;
                default:
                    Log("ERROR UNSUPPORTED SYSTEM: " + SystemInfo.operatingSystemFamily);
                    return;
            }
            
            Assembly asm = Assembly.GetExecutingAssembly();
            int ind = 0;
            foreach (string res in asm.GetManifestResourceNames())
            {
                using (Stream s = asm.GetManifestResourceStream(res))
                {
                    if (s == null) continue;
                    byte[] buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Dispose();
                    if (res.EndsWith(".png"))
                    {
                        // Create texture from bytes
                        var tex = new Texture2D(1, 1);
                        tex.LoadImage(buffer, true);
                        // Create sprite from texture
                        Sprites.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
                        Log("Created sprite from embedded image: " + res + " at ind " + ++ind);
                    }
                    else
                    {
                        string bundleName = Path.GetExtension(res).Substring(1);
                        if (bundleName != path && bundleName != path2) continue;
                        assetbundles[bundleName] = AssetBundle.LoadFromMemory(buffer); 
                        
                    }
                }
            }
        }
        private void AddCP(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);
            if (GameManager.instance.gameObject.GetComponent<ArenaFinder>() == null)
            {
                AddComponent();
            }
        }
        private string LangGet(string key, string sheettitle,string orig)
        {
            switch (key)
            {
                case "FENNEL_NAME": return "Pardoner Fennel";
                case "FENNEL_DESC": return "Zealous follower of the Weeping God.";
                case "FENNEL_INTRO":
                    return "Hmm...Pale One, why do you resist so? This kingdom's fate was sealed upon its inception, it sleeps now as a graveyard of false gods.<page>" +
       "I know the tale of this land and the betrayal that gave it birth. The gods here forsook you, let Us take the last fragments of life lingering here so that the purpose of the God of gods may be fulfilled.<page>" +
       "Be warned, denial will bring only pain.<page>";
                case "FENNEL_END": return "You...truly are a fool.<page>";
                default: return orig;
            }
        }
        
        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "statueStateFennel")
                _settings.CompletionFennel = (BossStatue.Completion)obj;
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            if (key == "statueStateFennel")
                return _settings.CompletionFennel;
            return orig;
        }


        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<ArenaFinder>();
        }

        public void Unload()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            ModHooks.LanguageGetHook -= LangGet;
            On.HeroController.Start -= AddCP;
            ModHooks.SetPlayerVariableHook -= SetVariableHook;
            ModHooks.GetPlayerVariableHook -= GetVariableHook;
            
            var x = GameManager.instance?.gameObject.GetComponent<ArenaFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}