using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using Modding;
using On;
using System;
using System.Linq;
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
            _aud.PlayOneShot(ArenaFinder.audioClips["sndAttack"]);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
            pc1.enabled = false;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 15);
            pc2.enabled = true;
            _aud.PlayOneShot(ArenaFinder.audioClips["sndAttack2"]);
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
            yield return new WaitForSeconds(0.2f);
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
            _aud.PlayOneShot(ArenaFinder.audioClips["sndAttack3"]);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
            pc1.enabled = false;
            pc2.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
            pc2.enabled = false;
            pc3.enabled = true;
            StartCoroutine(SpawnLightning(new Vector2(gameObject.transform.GetPositionX(), 13f)));
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
            pc3.enabled = false;
            pc4.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
            pc4.enabled = false;
            pc5.enabled = true;
            if (_hm.hp <= FennelFight.HP_PHASE2)  StartCoroutine(SpawnLightning(new Vector2(gameObject.transform.GetPositionX(), 13f)));
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
            pc5.enabled = false;

            yield return new WaitWhile(() => _anim.IsPlaying() && !IsGrounded());
            StartCoroutine(SpawnLightning(new Vector2(gameObject.transform.GetPositionX(), 13f)));
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
            _aud.PlayOneShot(ArenaFinder.audioClips["sndBackflip"]);
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
            GameObject[] lightningsOrdered = new GameObject[17];
            GameObject[] lightningsOrdOff = new GameObject[17];
            int maxL = lightningsOrdered.Length;
            int halfL = maxL / 2;

            for (float x = xStart, i = 0; i < halfL; i++, x += distBtw)
            {
                int index = (int)i;
                lightningsOrdered[index] = Instantiate(Fennel.preloadedGO["lightning"]);
                lightningsOrdered[index].transform.SetPosition2D(x, 13f);
                lightningsOrdOff[index] = Instantiate(Fennel.preloadedGO["lightning"]);
                lightningsOrdOff[index].transform.SetPosition2D(x+distBtw/2f, 13f);
            }
            for (float x = xStart, i = halfL; i < maxL; i++, x -= distBtw)
            {
                int index = (int)i;
                lightningsOrdered[index] = Instantiate(Fennel.preloadedGO["lightning"]);
                lightningsOrdered[index].transform.SetPosition2D(x, 13f);
                lightningsOrdOff[index] = Instantiate(Fennel.preloadedGO["lightning"]);
                lightningsOrdOff[index].transform.SetPosition2D(x-distBtw/2f, 13f);
            }

            _anim.PlayAt("slam",0);
            SpawnOrb(gameObject.transform.position);
            yield return new WaitForSeconds(0.01f);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
            _aud.PlayOneShot(ArenaFinder.audioClips["sndThunder1"]);
            GameObject[] lightnings = lightningsOrdered.OrderBy(x => new System.Random().Next()).ToArray();
            foreach (GameObject parentL in lightnings)
            {
                GameObject light1 = parentL.transform.Find("light1").gameObject;
                light1.GetComponent<SpriteRenderer>().enabled = true;
                yield return new WaitForSeconds(0.003f);
            }
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
            foreach (GameObject parentL in lightnings)
            {
                GameObject light1 = parentL.transform.Find("light1").gameObject;
                light1.GetComponent<SpriteRenderer>().enabled = false;
                yield return new WaitForSeconds(0.003f);
            }
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 13);
            _aud.PlayOneShot(ArenaFinder.audioClips["sndThunder2"]);
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            foreach (GameObject parentL in lightnings)
            {
                GameObject light2 = parentL.transform.Find("light2").gameObject;
                light2.GetComponent<SpriteRenderer>().enabled = true;
                light2.GetComponent<BoxCollider2D>().enabled = true;
                StartCoroutine(SpawnRumbleParticle(new Vector2(parentL.transform.GetPositionX(), 4f)));
                yield return new WaitForSeconds(0.003f);
            }
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 15);
            foreach(GameObject parentL in lightnings)
            { 
                Destroy(parentL);
                yield return new WaitForSeconds(0.003f);
            }
            if (_hm.hp <= FennelFight.HP_PHASE2)
            {
                int rnd = UnityEngine.Random.Range(0, 2);
                if (rnd == 0) StartCoroutine(SpawnHorizontalLightning());
                else StartCoroutine(SlamGroundV2(lightningsOrdOff));
            }
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("idle");
            yield return new WaitForSeconds(IDLE_TIME);
            if (_hm.hp <= FennelFight.HP_PHASE2) yield return new WaitForSeconds(0.65f);
            fight.doNextAttack = true;
        }

        private IEnumerator SpawnHorizontalLightning()
        {
            GameObject[] lightOrd = new GameObject[2];
            int rnd = UnityEngine.Random.Range(0, 2);
            for (float y = 5.5f, i = 0; y < 10f; y += 3f, i++)
            {
                int index = (int) i;
                if (i == rnd) continue;
                lightOrd[index] = Instantiate(Fennel.preloadedGO["lightHoriz"]);
                lightOrd[index].transform.SetPosition2D(88f, y);
            }
            _aud.PlayOneShot(ArenaFinder.audioClips["sndThunder1"]);
            GameObject[] lightnings = lightOrd.OrderBy(x => new System.Random().Next()).ToArray();
            foreach (GameObject parentL in lightnings)
            {
                if (parentL == null) continue;
                foreach (SpriteRenderer childLSpr in parentL.GetComponentsInChildren<SpriteRenderer>(true).Where(x => x.name.Contains("light1")))
                {
                    childLSpr.enabled = true;
                }
                yield return new WaitForSeconds(0.003f);
            }
            yield return new WaitForSeconds(0.35f);
            foreach (GameObject parentL in lightnings)
            {
                if (parentL == null) continue;
                foreach (SpriteRenderer childLSpr in parentL.GetComponentsInChildren<SpriteRenderer>(true).Where(x => x.name.Contains("light1")))
                {
                    childLSpr.enabled = false;
                }
                yield return new WaitForSeconds(0.003f);
            }
            yield return new WaitForSeconds(0.1f);
            _aud.PlayOneShot(ArenaFinder.audioClips["sndThunder2"]);
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            foreach (GameObject parentL in lightnings)
            {
                if (parentL == null) continue;
                foreach (DamageHero childLDH in parentL.GetComponentsInChildren<DamageHero>(true))
                {
                    GameObject light2 = childLDH.gameObject;
                    light2.GetComponent<SpriteRenderer>().enabled = true;
                    light2.GetComponent<BoxCollider2D>().enabled = true;
                }
                yield return new WaitForSeconds(0.003f);
            }
            yield return new WaitForSeconds(0.1f);
            foreach (GameObject parentL in lightnings)
            {
                if (parentL == null) continue;
                Destroy(parentL);
                yield return new WaitForSeconds(0.003f);
            }
        }

        public IEnumerator Buff()
        {
            fight.doNextAttack = false;
            float dir = FaceHero();
            _anim.Play("buff");
            yield return new WaitForSeconds(0.01f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _aud.PlayOneShot(ArenaFinder.audioClips["sndBuff"]);
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


            _aud.PlayOneShot(ArenaFinder.audioClips["sndJump"]);

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
            impact.transform.localScale *= 1.2f;
            Animator impactAnim = impact.GetComponent<Animator>();
            impactAnim.speed = 0.8f;
            impact.transform.SetPosition2D(gameObject.transform.GetPositionX(), 17.4f);
            impactAnim.Play("impact");
            if (_hm.hp < FennelFight.HP_PHASE2) StartCoroutine(DoubleImpact());
            yield return new WaitWhile(() => impactAnim.GetCurrentFrame() <= 1);
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

            yield return new WaitWhile(() => impactAnim.GetCurrentFrame() < 7);
            impact.GetComponent<BoxCollider2D>().enabled = false;
            yield return new WaitWhile(() => impactAnim.IsPlaying());
            Destroy(impact);
            _anim.Play("idle");
            yield return new WaitForSeconds(IDLE_TIME);
            fight.doNextAttack = true;
        }

        //------------------------------------------------------------Utility------------------------------------------------------------

        private IEnumerator SlamGroundV2(GameObject[] lighOrdered) //Make second wave spawn in phase 2 that is in alternate pos
        {
            _aud.PlayOneShot(ArenaFinder.audioClips["sndThunder1"]);
            GameObject[] lightnings = lighOrdered.OrderBy(x => new System.Random().Next()).ToArray();
            foreach (GameObject parentL in lightnings)
            {
                GameObject light1 = parentL.transform.Find("light1").gameObject;
                light1.GetComponent<SpriteRenderer>().enabled = true;
                yield return new WaitForSeconds(0.003f);
            }
            yield return new WaitForSeconds(0.2f);
            foreach (GameObject parentL in lightnings)
            {
                GameObject light1 = parentL.transform.Find("light1").gameObject;
                light1.GetComponent<SpriteRenderer>().enabled = false;
                yield return new WaitForSeconds(0.003f);
            }
            yield return new WaitForSeconds(0.1f);
            _aud.PlayOneShot(ArenaFinder.audioClips["sndThunder2"]);
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            foreach (GameObject parentL in lightnings)
            {
                GameObject light2 = parentL.transform.Find("light2").gameObject;
                light2.GetComponent<SpriteRenderer>().enabled = true;
                light2.GetComponent<BoxCollider2D>().enabled = true;
                StartCoroutine(SpawnRumbleParticle(new Vector2(parentL.transform.GetPositionX(), 4f)));
                yield return new WaitForSeconds(0.003f);
            }
            yield return new WaitForSeconds(0.1f);
            foreach (GameObject parentL in lightnings)
            {
                Destroy(parentL);
                yield return new WaitForSeconds(0.003f);
            }
        }

        private IEnumerator SpawnLightning(Vector2 pos)
        {
            GameObject parentLight = Instantiate(Fennel.preloadedGO["lightning"]);
            parentLight.transform.SetPosition2D(pos.x, pos.y);

            _aud.PlayOneShot(ArenaFinder.audioClips["sndThunder1"]);
            GameObject preLight = parentLight.transform.Find("light1").gameObject;
            SpriteRenderer preLightSpr = preLight.GetComponent<SpriteRenderer>();
            preLightSpr.enabled = true;

            yield return new WaitForSeconds(0.25f);

            preLightSpr.enabled = false;
            _aud.PlayOneShot(ArenaFinder.audioClips["sndThunder2"]);
            GameObject light = parentLight.transform.Find("light2").gameObject;
            SpriteRenderer lightSpr = light.GetComponent<SpriteRenderer>();
            BoxCollider2D lightBC = light.GetComponent<BoxCollider2D>();
            StartCoroutine(SpawnRumbleParticle(new Vector2(pos.x, 4f)));
            lightSpr.enabled = true;
            lightBC.enabled = true;
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");

            yield return new WaitForSeconds(0.25f);

            Destroy(parentLight);
        }

        private IEnumerator DoubleImpact()
        {
            yield return new WaitForSeconds(0.5f);
            GameObject impact = Instantiate(Fennel.preloadedGO["impact"]);
            GameObject impact2 = Instantiate(Fennel.preloadedGO["impact"]);
            impact.transform.localScale *= 1.1f;
            impact2.transform.localScale *= 1.1f;
            Animator impactAnim = impact.GetComponent<Animator>();
            Animator impactAnim2 = impact.GetComponent<Animator>();
            impactAnim.speed = 0.8f;
            impact.transform.SetPosition2D(gameObject.transform.GetPositionX() + 2.5f, 16.4f);
            impact2.transform.SetPosition2D(gameObject.transform.GetPositionX() - 2.5f, 16.4f);
            impactAnim.Play("impact");
            impactAnim2.Play("impact");
            yield return new WaitWhile(() => impactAnim.GetCurrentFrame() <= 1);
            impact.GetComponent<BoxCollider2D>().enabled = true;
            impact2.GetComponent<BoxCollider2D>().enabled = true;
            yield return new WaitWhile(() => impactAnim.GetCurrentFrame() < 7);
            impact.GetComponent<BoxCollider2D>().enabled = false;
            impact2.GetComponent<BoxCollider2D>().enabled = false;
            yield return new WaitWhile(() => impactAnim.IsPlaying());
            Destroy(impact);
            Destroy(impact2);
        }

        private IEnumerator SpawnRumbleParticle(Vector2 pos)
        {
            GameObject go = Instantiate(Fennel.preloadedGO["realWave"]);
            go.GetComponent<PlayMakerFSM>().enabled = false;
            go.SetActive(true);
            go.transform.SetPosition2D(pos.x, pos.y);
            GameObject go2 = go.transform.Find("Burst Rocks Stomp").gameObject;
            Destroy(go.transform.Find("Roll Dust").gameObject);
            ParticleSystem ps1 = go2.GetComponent<ParticleSystem>();
            ps1.Play();
            yield return new WaitForSeconds(0.5f);
            ps1.Stop();
        }

        private Animator SpawnOrb(Vector2 pos, float speedScale = 1f, float sizeScale = 1f)
        {
            GameObject orb = Instantiate(Fennel.preloadedGO["orb"]);
            orb.GetComponent<Animator>().speed *= speedScale;
            orb.transform.SetPosition2D(pos);
            orb.transform.localScale *= sizeScale;
            orb.SetActive(true);
            return orb.GetComponent<Animator>();
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
            GameObject go = Instantiate(Fennel.preloadedGO["realWave"]);
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
