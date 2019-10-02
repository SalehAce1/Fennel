using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using Modding;
using On;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Logger = Modding.Logger;

namespace Fennel
{
    internal class FennelFight : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private DamageHero _dmg;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
        private Recoil _rc;
        private HeroController _target;
        private HealthManager _hm;
        private AudioSource _aud;
        private GameObject musicControl;
        private Animator _anim;
        private Text title;
        private GameObject canvas;
        private FennelMoves moves;
        private bool soundOverride;
        private bool fennelDamage;
        private bool flashing;
        private bool introTextDone;
        private bool heroDmg;
        private bool buffed;
        private float afterimageTimer = 0f;
        private int afterimageIndex = 0;
        private float fadeCopyTime = 8f;
        private bool shouldBackflip;
        private GameObject[] afterimages;
        private Dictionary<IEnumerator, int> movesCount;
        private const float GROUND_Y = 6.75f;
        private const float RIGHT_X = 117f;
        private const float LEFT_X = 88f;
        private const float TOO_FAR_X = 5f;
        private const int MAX_REPEAT = 1;
        public const int HP_MAX = 600;
        public const int HP_PHASE2 = 400;
        public bool afterImageStart;

        private void Awake()
        {
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _dmg = gameObject.AddComponent<DamageHero>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _rc = gameObject.AddComponent<Recoil>();
            _hm = gameObject.GetComponent<HealthManager>();
            _aud = gameObject.AddComponent<AudioSource>();
            _anim = gameObject.GetComponent<Animator>();
            _target = HeroController.instance;
            movesCount = new Dictionary<IEnumerator, int>();
        }

        private IEnumerator Start()
        {
            FennelDeath.isDying = false;
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            _hm.OnDeath += _hm_OnDeath;
            SetSoundSettings(_aud);
            gameObject.layer = 11;
            gameObject.transform.SetPosition3D(HeroController.instance.transform.position.x + 10f, GROUND_Y, 0f);
            _hm.hp = HP_MAX;
            _bc.isTrigger = true;
            _rc.SetRecoilSpeed(15f);
            _rb.gravityScale = 0f;

            movesCount.Add(moves.Dash(), 0);
            movesCount.Add(moves.SlamGround(), 0);
            movesCount.Add(moves.JumpDive(), 0);
            movesCount.Add(moves.BackFlip(), 0);
            movesCount.Add(moves.Attack(), 0);
            movesCount.Add(moves.AirAttack(), 0);

            //More of Katie's image code
            afterimages = new GameObject[10];
            for (int i = 0; i < afterimages.Length; i++)
            {
                afterimages[i] = new GameObject("afterimage");
                afterimages[i].AddComponent<SpriteRenderer>();
                afterimages[i].GetComponent<SpriteRenderer>().material = _sr.material;
                afterimages[i].AddComponent<AfterimageFader>();
            }

            yield return new WaitForSeconds(0.1f);
            moves = gameObject.GetComponent<FennelMoves>();
            moves.distToGnd = _bc.bounds.extents.y;
            _anim.Play("intro1");
            yield return new WaitWhile(() => _anim.IsPlaying());
            StartCoroutine(IntroText());
            StartCoroutine(IntroAnim());
        }

        public void NextAttack()
        {
            IEnumerator curr = moves.Dash();
            if (_hm.hp > HP_PHASE2)
            {
                if (DistXToPlayer() > TOO_FAR_X)  //If player is far
                {
                    int rnd = UnityEngine.Random.Range(0, 4);
                    switch (rnd)
                    {
                        case 0:
                            curr = moves.Dash();
                            shouldBackflip = false;
                            break;
                        case 1:
                            curr = moves.SlamGround();
                            break;
                        case 2:
                            curr = moves.JumpDive();
                            break;
                        case 3:
                            curr = moves.AirAttack();
                            break;
                    }
                }
                else //If player is close
                {
                    float heroSignX = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX()) * -1f;
                    if (shouldBackflip && heroSignX < 0 && gameObject.transform.GetPositionX() < 115f ||
                        heroSignX > 0 && gameObject.transform.GetPositionX() > 90f && movesCount[moves.BackFlip()] < MAX_REPEAT)
                    {
                        shouldBackflip = false;
                        curr = moves.BackFlip();
                    }
                    else if (heroSignX < 0 && gameObject.transform.GetPositionX() > 115f ||
                        heroSignX > 0 && gameObject.transform.GetPositionX() < 90f && movesCount[moves.JumpDive()] < MAX_REPEAT)
                    {
                        curr = moves.JumpDive();
                    }
                    else if (heroSignX < 0 && gameObject.transform.GetPositionX() < 115f ||
                        heroSignX > 0 && gameObject.transform.GetPositionX() > 90f && movesCount[moves.AirAttack()] < MAX_REPEAT)
                    {
                        curr = moves.AirAttack();
                    }
                    else
                    {
                        curr = moves.ComboAttack();
                    }
                }
                StartCoroutine(curr);
                return;
            }
            if (!buffed)
            {
                buffed = true;
                afterImageStart = true;
                StartCoroutine(moves.Buff());
                return;
            }
            //Phase 2 attacks
            if (_hm.hp <= HP_PHASE2)
            {
                if (DistXToPlayer() > TOO_FAR_X)  //If player is far
                {
                    int rnd = UnityEngine.Random.Range(0, 4);

                    switch (rnd)
                    {
                        case 0:
                            curr = moves.Dash();
                            shouldBackflip = false;
                            break;
                        case 1:
                            curr = moves.SlamGround();
                            break;
                        case 2:
                            curr = moves.JumpDive();
                            break;
                        case 3:
                            curr = moves.AirAttack();
                            break;
                    }
                }
                else //If player is close
                {
                    float heroSignX = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX()) * -1f;
                    if (shouldBackflip && heroSignX < 0 && gameObject.transform.GetPositionX() < 115f ||
                        heroSignX > 0 && gameObject.transform.GetPositionX() > 90f )
                    {
                        shouldBackflip = false;
                        curr = moves.BackFlip();
                    }
                    else if (heroSignX < 0 && gameObject.transform.GetPositionX() > 115f ||
                        heroSignX > 0 && gameObject.transform.GetPositionX() < 90f)
                    {
                        curr = moves.JumpDive();
                    }
                    else if (heroSignX < 0 && gameObject.transform.GetPositionX() < 115f ||
                        heroSignX > 0 && gameObject.transform.GetPositionX() > 90f)
                    {
                        curr = moves.AirAttack();
                    }
                    else
                    {
                        curr = moves.ComboAttack();
                    }
                }
                UpdateMovesCount(curr);
                StartCoroutine(curr);
                return;
            }
        }

        IEnumerator IntroText()
        {
            yield return new WaitForSeconds(1f);
            if (!ArenaFinder.foundBoss)
            {
                GameObject text = Instantiate(GameObject.Find("DialogueManager"));
                var txtfsm = text.LocateMyFSM("Box Open");
                txtfsm.SendEvent("BOX UP");
                yield return null;
                GameManager.instance.playerData.disablePause = true;
                _target.RelinquishControl();
                _target.StopAnimationControl();
                _target.gameObject.GetComponent<tk2dSpriteAnimator>().Play("Idle");
                _target.transform.localScale = new Vector2(-1f * Mathf.Abs(_target.transform.localScale.x), _target.transform.localScale.y);
                GameObject sec = text.transform.Find("Text").gameObject;
                sec.GetComponent<DialogueBox>().StartConversation("FENNEL_INTRO", "testudo");
                yield return new WaitWhile(() => sec.GetComponent<DialogueBox>().currentPage <= 1);
                _anim.Play("intro2");
                _aud.clip = ArenaFinder.audioClips["sndIntroHead"];
                _aud.Play();
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return new WaitWhile(() => sec.GetComponent<DialogueBox>().currentPage <= 3);
                txtfsm.SendEvent("BOX DOWN");
                text.SetActive(false);
                _target.RegainControl();
                GameManager.instance.playerData.disablePause = false;
                _target.StartAnimationControl();
                //ArenaFinder.foundBoss = true;
            }
            introTextDone = true;
        }

        IEnumerator IntroAnim()
        {
            yield return new WaitWhile(() => !introTextDone);
            StartCoroutine(EndingTextFade());
            _anim.Play("intro3");
            _aud.clip = ArenaFinder.audioClips["sndIntroAttack"];
            _aud.Play();
            StartCoroutine(MusicControl());
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("intro4");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            NextAttack();
        }

        private void Update()
        {
            Vector2 posH = _target.transform.position;
            Vector2 posP = gameObject.transform.position;

            if (posP.x <= LEFT_X && _rb.velocity.x < 0) _rb.velocity = new Vector2(0f, _rb.velocity.y);
            else if (posP.x >= RIGHT_X && _rb.velocity.x > 0) _rb.velocity = new Vector2(0f, _rb.velocity.y);

            //Taken from Katie's fader code
            if (!afterImageStart) return;
            afterimageTimer += Time.deltaTime;
            if (afterimageTimer > fadeCopyTime / 60)
            {
                afterimageTimer -= fadeCopyTime / 60;
                afterimages[afterimageIndex].GetComponent<SpriteRenderer>().sprite = _sr.sprite;
                afterimages[afterimageIndex].transform.position = gameObject.transform.position;
                afterimages[afterimageIndex].transform.localScale = gameObject.transform.localScale;
                afterimages[afterimageIndex].GetComponent<AfterimageFader>().BeginFade();
                afterimageIndex++;
                afterimageIndex = afterimageIndex % afterimages.Length;
            }
        }

        //------------------------------------------------------------Stuff------------------------------------------------------------
        private void _hm_OnDeath()
        {
            gameObject.AddComponent<FennelDeath>();
            _aud.Stop();
            musicControl.GetComponent<AudioSource>().Stop();
            Destroy(gameObject.GetComponent<BoxCollider2D>());
            StopAllCoroutines();
            Destroy(gameObject.GetComponent<FennelFight>());
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("fennel"))
            {
                fennelDamage = true;
                shouldBackflip = true;
                if (!flashing)
                {
                    flashing = true;
                    StartCoroutine(FlashWhite());
                }
            }
            orig(self, hitInstance);
        }

        IEnumerator MusicControl()
        {
            yield return null;
            musicControl = new GameObject("pardonerDance");
            musicControl.transform.SetPosition2D(HeroController.instance.transform.position);
            musicControl.SetActive(true);
            AudioSource comp = musicControl.AddComponent<AudioSource>();
            SetSoundSettings(comp);
            comp.loop = true;
            comp.clip = ArenaFinder.audioClips["pardonerDance"];
            comp.Play();
            StartCoroutine(MusPauseHand(comp));
            StartCoroutine(MusicVol(comp));
        }

        public static void SetSoundSettings(AudioSource aud)
        {
            aud.enabled = true;
            aud.volume = 1f;
            aud.bypassEffects = true;
            aud.bypassReverbZones = true;
            aud.bypassListenerEffects = true;
        }

        IEnumerator MusicVol(AudioSource one)
        {
            while (true)
            {
                if (!soundOverride) one.volume = GameManager.instance.gameSettings.musicVolume / 10f;
                yield return new WaitForEndOfFrame();
            }
        }

        IEnumerator MusPauseHand(AudioSource one)
        {
            while (true)
            {
                if (heroDmg && !GameManager.instance.isPaused)
                {
                    soundOverride = true;
                    heroDmg = false;
                    for (float i = 1f; i > 0.2f; i -= 0.1f)
                    {
                        one.volume = i;
                        yield return new WaitForEndOfFrame();
                    }
                    yield return new WaitForSeconds(1.5f);
                    for (float i = 0.2f; i <= 1f; i += 0.1f)
                    {
                        one.volume = i;
                        yield return new WaitForEndOfFrame();
                    }
                    soundOverride = false;
                    yield return new WaitForSeconds(0.5f);
                }
                if (GameManager.instance.isPaused)
                {
                    soundOverride = true;
                    one.volume = 0.2f;
                    yield return new WaitWhile(() => GameManager.instance.isPaused);
                    one.volume = 1f;
                    soundOverride = false;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        IEnumerator FlashWhite()
        {
            _sr.material.SetFloat("_FlashAmount", 1f);
            yield return null;
            for (float i = 1f; i >= 0f; i -= 0.05f)
            {
                _sr.material.SetFloat("_FlashAmount", i);
                yield return null;
            }
            yield return null;
            flashing = false;
        }

        IEnumerator EndingTextFade()
        {
            CanvasUtil.CreateFonts();
            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920f, 1080f));//1536f, 864f));
            title = CanvasUtil.CreateTextPanel(canvas, "Pardoner Fennel by Bombservice", 40, TextAnchor.MiddleCenter, new CanvasUtil.RectData(new Vector2(1000, 1500), new Vector2(0f, 65f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f)), false).GetComponent<Text>();
            title.color = new Color(1f, 1f, 1f, 0f);
            title.font = CanvasUtil.TrajanBold;//CanvasUtil.GetFont("Perpetua");
            for (float i = 0f; i <= 1f; i += 0.05f)
            {
                title.color = new Color(1f, 1f, 1f, i);
                yield return new WaitForSeconds(0.01f);
            }
            yield return new WaitForSeconds(1.7f);
            for (float i = 1; i >= 0f; i -= 0.05f)
            {
                title.color = new Color(1f, 1f, 1f, i);
                yield return new WaitForSeconds(0.01f);
            }
            title.text = "";
        }

        private float DistXToPlayer()
        {
            return Mathf.Abs(_target.transform.GetPositionX() - gameObject.transform.GetPositionX());
        }

        private void UpdateMovesCount(IEnumerator move)
        {
            foreach (IEnumerator i in movesCount.Keys)
            {
                if (move == i)
                {
                    movesCount[i]++;
                    continue;
                }
                movesCount[i] = 0;
            }
        }

        private void Log(object o)
        {
            Logger.Log("[Fennel] " + o);
        }
    }
}