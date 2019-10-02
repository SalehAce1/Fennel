using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Linq;
using Logger = Modding.Logger;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Fennel
{
    internal class ArenaFinder : MonoBehaviour
    {
        public static Dictionary<string, AudioClip> audioClips;
        public static Dictionary<string, Material> materials;
        public static Dictionary<string, RuntimeAnimatorController> animators;
        private GameObject fennel;
        public static Shader flashShader;
        public static bool foundBoss;

        private void Start()
        {
            audioClips = new Dictionary<string, AudioClip>();
            materials = new Dictionary<string, Material>();
            animators = new Dictionary<string, RuntimeAnimatorController>();

            string path = "";
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    path = "fennelWin";
                    break;
                case OperatingSystemFamily.Linux:
                    path = "fennelLin";
                    break;
                case OperatingSystemFamily.MacOSX:
                    path = "fennelMC";
                    break;
                default:
                    Log("ERROR UNSUPPORTED SYSTEM: " + SystemInfo.operatingSystemFamily);
                    return;
            }
            USceneManager.activeSceneChanged += SceneChanged;
            AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, path));
            UObject[] assets = ab.LoadAllAssets();
            Fennel.preloadedGO["fennel"] = ab.LoadAsset<GameObject>("fennel");
            Fennel.preloadedGO["lightning"] = ab.LoadAsset<GameObject>("lightning");
            Fennel.preloadedGO["impact"] = ab.LoadAsset<GameObject>("impact");
            animators["fennel"] = ab.LoadAsset<RuntimeAnimatorController>("fennel");
            flashShader = ab.LoadAsset<Shader>("Diffuse Flash");
            foreach (AudioClip a in ab.LoadAllAssets<AudioClip>())
            {
                audioClips[a.name] = a;
            }
            materials["flash"] = ab.LoadAsset<Material>("Material");

        }
        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg0.name == "GG_Mighty_Zote" && arg1.name == "GG_Workshop")
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                Destroy(fennel.GetComponent<FennelFight>());
                Destroy(fennel);
                PlayerData.instance.isInvincible = false;
            }

            if (arg1.name == "GG_Workshop") SetStatue();

            if (arg1.name != "GG_Mighty_Zote") return;
            if (arg0.name != "GG_Workshop") return;
            StartCoroutine(AddComponent());
        }

        private void SetStatue()
        {

            //Used 56's pale prince code here
            GameObject statue = Instantiate(GameObject.Find("GG_Statue_ElderHu"));
            statue.transform.SetPosition2D(25.4f, statue.transform.GetPositionY());//6.5f); //248
            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Mighty_Zote";
            var bs = statue.GetComponent<BossStatue>();
            bs.bossScene = scene;
            bs.statueStatePD = "FennelArena";

            var gg = new BossStatue.Completion
            {
                completedTier1 = true,
                seenTier3Unlock = true,
                completedTier2 = true,
                completedTier3 = true,
                isUnlocked = true,
                hasBeenSeen = true,
                usingAltVersion = false,
            };
            bs.StatueState = gg;
            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "FENNEL_NAME";
            details.descriptionKey = details.descriptionSheet = "FENNEL_DESC";
            bs.bossDetails = details;
            foreach (var i in bs.statueDisplay.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.sprite = new Sprite();
            }

        }

        private IEnumerator AddComponent()
        {
            yield return null;
            Destroy(GameObject.Find("Battle Control"));
            Destroy(GameObject.Find("Zote Boss"));

            fennel = Instantiate(Fennel.preloadedGO["fennel"]);
            fennel.SetActive(true);
            var _hm = fennel.AddComponent<HealthManager>();
            HealthManager hornHP = Fennel.preloadedGO["hornet"].GetComponent<HealthManager>();

            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(_hm, fi.GetValue(hornHP));
            }

            foreach (SpriteRenderer i in Fennel.preloadedGO["lightning"].GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
            }
            GameObject lightning = Fennel.preloadedGO["lightning"].transform.Find("light2").gameObject;
            lightning.AddComponent<DamageHero>().damageDealt = 2;
            lightning.gameObject.layer = 17;

            foreach (PolygonCollider2D i in fennel.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                i.isTrigger = true;
                i.gameObject.AddComponent<DamageHero>();
                i.gameObject.layer = 17;
            }

            Fennel.preloadedGO["impact"].AddComponent<DamageHero>().damageDealt = 2;
            Fennel.preloadedGO["impact"].layer = 17;
            Fennel.preloadedGO["impact"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));

            var _sr = fennel.GetComponent<SpriteRenderer>();
            _sr.material = materials["flash"];
            fennel.AddComponent<FennelFight>();
            fennel.AddComponent<FennelMoves>();
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
        }
        public static void Log(object o)
        {
            Logger.Log("[Lost Arena] " + o);
        }
    }
}