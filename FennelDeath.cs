using System.Collections;
using UnityEngine;
using Logger = Modding.Logger;

namespace Fennel
{
    internal class FennelDeath : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Animator _anim;
        private Rigidbody2D _rb;
        private HeroController _target;
        private AudioSource _aud;
        public static bool isDying;

        private void Awake()
        {
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _anim = gameObject.GetComponent<Animator>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _aud = gameObject.GetComponent<AudioSource>();
            _target = HeroController.instance;
        }

        private void Start()
        {
            if (ArenaFinder.BossLevel == 0)
            {
                Fennel._settings.CompletionFennel.completedTier1 = true;
            }
            else if (ArenaFinder.BossLevel == 1)
            {
                 Fennel._settings.CompletionFennel.completedTier2 = true;
            }
            else
            {
                 Fennel._settings.CompletionFennel.completedTier3 = true;
            }
            isDying = true;
            _sr.material.SetFloat("_FlashAmount", 0f);
            _rb.velocity = new Vector2(0f, 0f);
            foreach (PolygonCollider2D i in gameObject.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                i.enabled = false;
            }
            StartCoroutine(DeathSentence());
        }

        IEnumerator DeathSentence()
        {
            PlayerData.instance.isInvincible = true;
            _rb.gravityScale = 1f;
            _anim.Play("hurt");
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() >= 6.75f);
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f, 0f);
            FaceHero();
            _anim.Play("death");
            _aud.clip = ArenaFinder.audioClips["sndDeath"];
            _aud.Play();
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            FaceHero();
            GameObject text = Instantiate(GameObject.Find("DialogueManager"));
            text.SetActive(true);
            yield return null;
            var txtfsm = text.LocateMyFSM("Box Open");
            txtfsm.SendEvent("BOX UP");
            GameManager.instance.playerData.SetBool("disablePause", true);
            _target.RelinquishControl();
            _target.StopAnimationControl();
            _target.gameObject.GetComponent<tk2dSpriteAnimator>().Play("Idle");
            GameObject sec = text.transform.Find("Text").gameObject;
            sec.GetComponent<DialogueBox>().StartConversation("FENNEL_END", "testudo");
            yield return new WaitWhile(() => sec.GetComponent<DialogueBox>().currentPage <= 1);
            txtfsm.SendEvent("BOX DOWN");
            text.SetActive(false);
            _target.RegainControl();
            GameManager.instance.playerData.SetBool("disablePause", false);
            _target.StartAnimationControl();
            yield return new WaitForSeconds(0.5f);
            //Do particle death here--------------------------------------------------------------------------------------------------------------------------------------------------------
            yield return new WaitForSeconds(1f);
            var endCtrl = GameObject.Find("Boss Scene Controller").LocateMyFSM("Dream Return");
            endCtrl.SendEvent("DREAM RETURN");
        }

        private float FaceHero()
        {
            var heroSignX = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
            var pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector2(Mathf.Abs(pScale.x) * heroSignX, pScale.y);
            if (heroSignX < 0) _target.FaceLeft();
            else _target.FaceRight();
            return heroSignX;
        }

        private static void Log(object obj)
        {
            Logger.Log("[Death] " + obj);
        }
    }
}

