using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using Modding;
using On;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Logger = Modding.Logger;

namespace Fennel
{
    internal class FennelMoves : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private AudioSource _aud;
        private HealthManager _hm;
        private HeroController _target;
        private Animator _anim;
        public float distToGnd;
        private const float GROUND_Y = 6.75f;
        private FennelFight fight;
        private const float IDLE_TIME = 0.05f;
        private const float AATTACK_IDLE = 0.2f;

        private void Awake()
        {
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _anim = gameObject.GetComponent<Animator>();
            _target = HeroController.instance;
            _hm = gameObject.GetComponent<HealthManager>();
            fight = gameObject.GetComponent<FennelFight>();
            _aud = gameObject.GetComponent<AudioSource>();
            FaceHero(true);
        }

        public IEnumerator Dash()
        {
            fight.doNextAttack = false;
            float dir = FaceHero() * -1f;
            Animator orbAnim = SpawnOrb(gameObject.transform.position, 3f, fight.ORB_DASH_SIZE);
            yield return new WaitForSeconds(orbAnim.GetCurrentAnimatorStateInfo(0).length/3.3f);
            _anim.PlayAt("dash", 0);
            if (_hm.hp > FennelFight.HP_PHASE2) fight.afterImageStart = true;
            _rb.velocity = new Vector2(30f * dir, 0f);
            yield return new WaitForSeconds(0.25f);
            _rb.velocity = new Vector2(0f, 0f);
            if (_hm.hp > FennelFight.HP_PHASE2) fight.afterImageStart = false;
            _anim.Play("idle");
            fight.doNextAttack = true;
        }

        public IEnumerator Attack()
        {
            fight.doNextAttack = false;
            FaceHero();
            PolygonCollider2D pc1 = gameObject.transform.Find("attack1").GetComponent<PolygonCollider2D>();
            PolygonCollider2D pc2 = gameObject.transform.Find("attack2").GetComponent<PolygonCollider2D>();
            _anim.PlayAt("attack", 0);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
            pc1.enabled = true;
            _aud.clip = ArenaFinder.audioClips["sndAttack"];
            _aud.Play();
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
            pc1.enabled = false;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 15);
            pc2.enabled = true;
            _aud.clip = ArenaFinder.audioClips["sndAttack2"];
            _aud.Play();
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 19);
            pc2.enabled = false;
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("idle");
            yield return new WaitForSeconds(IDLE_TIME);
            fight.doNextAttack = true;
        }

        public IEnumerator AirAttack()
        {
            fight.doNextAttack = false;
            float dir = -1f * FaceHero();

            PolygonCollider2D pc1 = gameObject.transform.Find("attA1").GetComponent<PolygonCollider2D>();
            PolygonCollider2D pc2 = gameObject.transform.Find("attA2").GetComponent<PolygonCollider2D>();
            PolygonCollider2D pc3 = gameObject.transform.Find("attA3").GetComponent<PolygonCollider2D>();
            PolygonCollider2D pc4 = gameObject.transform.Find("attA4").GetComponent<PolygonCollider2D>();
            PolygonCollider2D pc5 = gameObject.transform.Find("attA5").GetComponent<PolygonCollider2D>();

            _anim.PlayAt("attackAir", 0);

            _rb.velocity = new Vector2(dir * 17.5f, 15.5f);
            _rb.gravityScale = 0.9f;

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
            pc1.enabled = true;
            _aud.clip = ArenaFinder.audioClips["sndAttack3"];
            _aud.Play();
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
            pc1.enabled = false;
            pc2.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
            pc2.enabled = false;
            pc3.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
            pc3.enabled = false;
            pc4.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
            pc4.enabled = false;
            pc5.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
            pc5.enabled = false;
            //yield return new WaitWhile(() => _anim.IsPlaying());

            yield return new WaitWhile(() => _anim.IsPlaying() && !IsGrounded());
            gameObject.transform.SetPosition2D(gameObject.transform.GetPositionX(), GROUND_Y);
            _rb.velocity = new Vector2(0f, 0f);
            _rb.gravityScale = 0f;
            _anim.Play("idle");
            yield return new WaitForSeconds(AATTACK_IDLE);
            fight.doNextAttack = true;
        }

        public IEnumerator BackFlip()
        {
            fight.doNextAttack = false;
            bool opposite = false;
            float dir = FaceHero(opposite);
            _anim.PlayAt("backflip",0);
            _aud.clip = ArenaFinder.audioClips["sndBackflip"];
            _aud.Play();
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
            _rb.velocity = new Vector2(15f * dir, 0f);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
            _rb.velocity = new Vector2(0f, 0f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("idle");
            yield return new WaitForSeconds(IDLE_TIME);
            fight.doNextAttack = true;
        }

        public IEnumerator SlamGround() //Make second wave spawn in phase 2 that is in alternate pos
        {
            fight.doNextAttack = false;
            float dir = FaceHero();

            float xStart = gameObject.transform.position.x;
            float distBtw = 3.8f;
            GameObject[] lightnings = new GameObject[17];
            int maxL = lightnings.Length;
            int halfL = maxL / 2;

            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                lightnings[index] = Instantiate(Fennel.preloadedGO["lightning"]);
                lightnings[index].transform.SetPosition2D(new Vector2(x, 13f));
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                lightnings[index] = Instantiate(Fennel.preloadedGO["lightning"]);
                lightnings[index].transform.SetPosition2D(new Vector2(x, 13f));
            }

            _anim.PlayAt("slam",0);
            SpawnOrb(gameObject.transform.position);
            yield return new WaitForSeconds(0.01f);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
            _aud.clip = ArenaFinder.audioClips["sndThunder1"];
            _aud.Play();
            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                GameObject light1 = lightnings[index].transform.Find("light1").gameObject;
                SpriteRenderer lightningAntic = light1.GetComponent<SpriteRenderer>();
                lightningAntic.enabled = true;
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                GameObject light1 = lightnings[index].transform.Find("light1").gameObject;
                SpriteRenderer lightningAntic = light1.GetComponent<SpriteRenderer>();
                lightningAntic.enabled = true;
            }

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                GameObject light1 = lightnings[index].transform.Find("light1").gameObject;
                SpriteRenderer lightningAntic = light1.GetComponent<SpriteRenderer>();
                lightningAntic.enabled = false;
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                GameObject light1 = lightnings[index].transform.Find("light1").gameObject;
                SpriteRenderer lightningAntic = light1.GetComponent<SpriteRenderer>();
                lightningAntic.enabled = false;
            }

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 13);
            _aud.clip = ArenaFinder.audioClips["sndThunder2"];
            _aud.Play();
            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                GameObject light2 = lightnings[index].transform.Find("light2").gameObject;
                SpriteRenderer lightning = light2.GetComponent<SpriteRenderer>();
                BoxCollider2D lightningBC = light2.GetComponent<BoxCollider2D>();
                lightning.enabled = true;
                lightningBC.enabled = true;
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                GameObject light2 = lightnings[index].transform.Find("light2").gameObject;
                SpriteRenderer lightning = light2.GetComponent<SpriteRenderer>();
                BoxCollider2D lightningBC = light2.GetComponent<BoxCollider2D>();
                lightning.enabled = true;
                lightningBC.enabled = true;
            }

            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake"); //SmallShake
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 17);
            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                Destroy(lightnings[index]);
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                Destroy(lightnings[index]);
            }
            if (_hm.hp <= FennelFight.HP_PHASE2) StartCoroutine(SlamGroundV2());
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("idle");
            yield return new WaitForSeconds(IDLE_TIME);
            fight.doNextAttack = true;
        }

        public IEnumerator SlamGroundV2() //Make second wave spawn in phase 2 that is in alternate pos
        {
            float xStart = gameObject.transform.position.x + 1.9f;
            float distBtw = 3.8f;
            GameObject[] lightnings = new GameObject[17];
            int maxL = lightnings.Length;
            int halfL = maxL / 2;

            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                lightnings[index] = Instantiate(Fennel.preloadedGO["lightning"]);
                lightnings[index].transform.SetPosition2D(new Vector2(x, 13f));
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                lightnings[index] = Instantiate(Fennel.preloadedGO["lightning"]);
                lightnings[index].transform.SetPosition2D(new Vector2(x, 13f));
            }

            _aud.clip = ArenaFinder.audioClips["sndThunder1"];
            _aud.Play();
            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                GameObject light1 = lightnings[index].transform.Find("light1").gameObject;
                SpriteRenderer lightningAntic = light1.GetComponent<SpriteRenderer>();
                lightningAntic.enabled = true;
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                GameObject light1 = lightnings[index].transform.Find("light1").gameObject;
                SpriteRenderer lightningAntic = light1.GetComponent<SpriteRenderer>();
                lightningAntic.enabled = true;
            }

            yield return new WaitForSeconds(0.3f);
            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                GameObject light1 = lightnings[index].transform.Find("light1").gameObject;
                SpriteRenderer lightningAntic = light1.GetComponent<SpriteRenderer>();
                lightningAntic.enabled = false;
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                GameObject light1 = lightnings[index].transform.Find("light1").gameObject;
                SpriteRenderer lightningAntic = light1.GetComponent<SpriteRenderer>();
                lightningAntic.enabled = false;
            }

            yield return new WaitForSeconds(0.3f);
            _aud.clip = ArenaFinder.audioClips["sndThunder2"];
            _aud.Play();
            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                GameObject light2 = lightnings[index].transform.Find("light2").gameObject;
                SpriteRenderer lightning = light2.GetComponent<SpriteRenderer>();
                BoxCollider2D lightningBC = light2.GetComponent<BoxCollider2D>();
                lightning.enabled = true;
                lightningBC.enabled = true;
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                GameObject light2 = lightnings[index].transform.Find("light2").gameObject;
                SpriteRenderer lightning = light2.GetComponent<SpriteRenderer>();
                BoxCollider2D lightningBC = light2.GetComponent<BoxCollider2D>();
                lightning.enabled = true;
                lightningBC.enabled = true;
            }

            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake"); //SmallShake
            yield return new WaitForSeconds(0.4f);
            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                Destroy(lightnings[index]);
            }
            for (float x = xStart - distBtw, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                Destroy(lightnings[index]);
            }
        }

        public IEnumerator Buff()
        {
            fight.doNextAttack = false;
            float dir = FaceHero();
            _anim.Play("buff");
            yield return new WaitForSeconds(0.01f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _aud.clip = ArenaFinder.audioClips["sndBuff"];
            _aud.Play();
            foreach (PolygonCollider2D i in gameObject.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                i.gameObject.GetComponent<DamageHero>().damageDealt = 2;
            }
            _anim.Play("idle");
            yield return new WaitForSeconds(IDLE_TIME);
            fight.doNextAttack = true;
        }

        public IEnumerator JumpDive() //Change wave color
        {
            fight.doNextAttack = false;
            float dir = FaceHero() * -1f;
            Vector2 vel = new Vector2(dir * 19f, 38f);


            _aud.clip = ArenaFinder.audioClips["sndJump"];
            _aud.Play();

            _anim.PlayAt("jump",0);
            _rb.velocity = vel;
            _rb.gravityScale = 1.5f;
            yield return new WaitForSeconds(0.4f);
            yield return new WaitWhile(() => !IsGrounded() && !IsPlayerWithinRange(1.5f));

            _rb.velocity = new Vector2(0f, 0f);
            _rb.gravityScale = 3f;
            yield return new WaitWhile(() => !IsGrounded());
            gameObject.transform.SetPosition2D(gameObject.transform.GetPositionX(), GROUND_Y);
            _rb.velocity = new Vector2(0f, 0f);
            _rb.gravityScale = 0f;
            _anim.PlayAt("plunge",0);
            GameObject impact = Instantiate(Fennel.preloadedGO["impact"]);
            Animator impactAnim = impact.GetComponent<Animator>();
            impact.transform.SetPosition2D(gameObject.transform.GetPositionX(), 10f);
            impactAnim.Play("impact");
            yield return new WaitWhile(() => impactAnim.GetCurrentFrame() < 1);
            impact.GetComponent<BoxCollider2D>().enabled = true;
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            if (dir < 0)
            {
                SpawnWave(false, 2f);
                SpawnWave(true, 1.5f);
            }
            else
            {
                SpawnWave(false, 1.5f);
                SpawnWave(true, 2f);
            }

            if (_hm.hp < FennelFight.HP_PHASE2) StartCoroutine(SpawnOrbStream());

            yield return new WaitWhile(() => impactAnim.GetCurrentFrame() < 7);
            impact.GetComponent<BoxCollider2D>().enabled = false;
            yield return new WaitWhile(() => impactAnim.IsPlaying());
            Destroy(impact);
            _anim.Play("idle");
            yield return new WaitForSeconds(IDLE_TIME);
            fight.doNextAttack = true;
        }

        //------------------------------------------------------------Utility------------------------------------------------------------

        private Animator SpawnOrb(Vector2 pos, float speedScale = 1f, float sizeScale = 1f)
        {
            GameObject orb = Instantiate(Fennel.preloadedGO["orb"]);
            orb.GetComponent<Animator>().speed *= speedScale;
            orb.transform.SetPosition2D(pos);
            orb.transform.localScale *= sizeScale;
            orb.SetActive(true);
            return orb.GetComponent<Animator>();
        }

        IEnumerator SpawnOrbStream()
        {
            for (int i = -2; i < 5; i++)
            {
                float randX = UnityEngine.Random.Range(90f + i * 5f, 90f + (i+1) * 5f);
                float randY = UnityEngine.Random.Range(6f + i * 3f, 6f + (i+1) * 3f);
                SpawnOrb(new Vector2(randX, randY), 1f, 0.8f);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private float FaceHero(bool opposite = false)
        {
            float heroSignX = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
            heroSignX = opposite ? -1f * heroSignX : heroSignX;
            Vector3 pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector2(Mathf.Abs(pScale.x) * heroSignX, pScale.y);
            return heroSignX;
        }

        private bool IsPlayerWithinRange(float range = 3.5f)
        {
            return FastApproximately(_target.transform.GetPositionX(), gameObject.transform.GetPositionX(), range);
        }

        public static bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }

        private void SpawnWave(bool faceRight, float size)
        {
            PlayMakerFSM lord = Fennel.preloadedGO["wave"].LocateMyFSM("Mage Lord");
            GameObject go = Instantiate(lord.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value);
            go.transform.localScale = new Vector2(size, 1f);
            PlayMakerFSM shock = go.LocateMyFSM("shockwave");
            shock.FsmVariables.FindFsmBool("Facing Right").Value = faceRight;
            shock.FsmVariables.FindFsmFloat("Speed").Value = 22f;
            go.SetActive(true);
            go.transform.SetPosition2D(gameObject.transform.position.x, 5.05f);
        }

        private bool IsGrounded()
        {
            return Physics2D.Raycast(transform.position, -Vector3.up, distToGnd + 0.1f);
        }

        private void Log(object o)
        {
            Logger.Log("[Fennel Moves] " + o);
        }
    }
}
