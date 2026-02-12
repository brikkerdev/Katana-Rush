using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Runner.Audio
{
    public class SoundController : MonoBehaviour
    {
        public static SoundController Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private int maxPoolSize = 40;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;

        [Header("Player Sounds")]
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip doubleJumpSound;
        [SerializeField] private AudioClip landSound;
        [SerializeField] private AudioClip dashSound;
        [SerializeField] private AudioClip deflectSound;
        [SerializeField] private AudioClip deathSound;
        [SerializeField] private AudioClip reviveSound;

        [Header("Bullet Sounds")]
        [SerializeField] private AudioClip bulletFireSound;
        [SerializeField] private AudioClip bulletLethalFireSound;
        [SerializeField] private AudioClip bulletImpactSound;
        [SerializeField] private AudioClip bulletLethalImpactSound;
        [SerializeField] private AudioClip bulletWhizSound;
        [SerializeField] private AudioClip bulletExpireSound;

        [Header("Enemy Sounds")]
        [SerializeField] private AudioClip enemyHitSound;
        [SerializeField] private AudioClip enemyDeathSound;
        [SerializeField] private AudioClip enemySpawnSound;
        [SerializeField] private AudioClip enemyAlertSound;
        [SerializeField] private AudioClip enemyRagdollSound;
        [SerializeField] private AudioClip sniperChargeSound;
        [SerializeField] private AudioClip sniperFireSound;

        [Header("UI Sounds")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip buttonBackSound;
        [SerializeField] private AudioClip buttonDisabledSound;
        [SerializeField] private AudioClip tabSwitchSound;
        [SerializeField] private AudioClip panelOpenSound;
        [SerializeField] private AudioClip panelCloseSound;
        [SerializeField] private AudioClip popupShowSound;
        [SerializeField] private AudioClip popupHideSound;

        [Header("Collectible Sounds")]
        [SerializeField] private AudioClip coinCollectSound;
        [SerializeField] private AudioClip gemCollectSound;
        [SerializeField] private AudioClip powerupCollectSound;
        [SerializeField] private AudioClip healthCollectSound;

        [Header("Shop Sounds")]
        [SerializeField] private AudioClip purchaseSuccessSound;
        [SerializeField] private AudioClip purchaseFailSound;
        [SerializeField] private AudioClip equipSound;
        [SerializeField] private AudioClip unequipSound;
        [SerializeField] private AudioClip unlockSound;
        [SerializeField] private AudioClip upgradeSound;

        [Header("Roulette Sounds")]
        [SerializeField] private AudioClip rouletteTickSound;
        [SerializeField] private AudioClip rouletteSlowSound;
        [SerializeField] private AudioClip rouletteWinCommonSound;
        [SerializeField] private AudioClip rouletteWinRareSound;
        [SerializeField] private AudioClip rouletteWinEpicSound;
        [SerializeField] private AudioClip rouletteWinLegendarySound;
        [SerializeField] private AudioClip rouletteDuplicateSound;

        [Header("Game State Sounds")]
        [SerializeField] private AudioClip gameStartSound;
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private AudioClip countdownTickSound;
        [SerializeField] private AudioClip countdownGoSound;
        [SerializeField] private AudioClip pauseSound;
        [SerializeField] private AudioClip resumeSound;
        [SerializeField] private AudioClip newHighScoreSound;
        [SerializeField] private AudioClip milestoneSound;

        [Header("Score Sounds")]
        [SerializeField] private AudioClip scoreTickSound;
        [SerializeField] private AudioClip comboSound;
        [SerializeField] private AudioClip comboBreakSound;
        [SerializeField] private AudioClip distanceMilestoneSound;

        [Header("Environment Sounds")]
        [SerializeField] private AudioClip biomeTransitionSound;
        [SerializeField] private AudioClip obstacleDestroySound;

        [Header("Pitch Variation")]
        [SerializeField] private float defaultPitchVariation = 0.05f;

        private List<AudioSource> sourcePool;
        private List<AudioSource> activeSources;
        private Transform poolParent;

        private float lastScoreTickTime;
        private int consecutiveScoreTicks;

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set => sfxVolume = Mathf.Clamp01(value);
        }

        public float UiVolume
        {
            get => uiVolume;
            set => uiVolume = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePool();
        }

        private void InitializePool()
        {
            poolParent = new GameObject("SoundPool").transform;
            poolParent.SetParent(transform);

            sourcePool = new List<AudioSource>(initialPoolSize);
            activeSources = new List<AudioSource>(initialPoolSize);

            for (int i = 0; i < initialPoolSize; i++)
            {
                sourcePool.Add(CreateSource());
            }
        }

        private AudioSource CreateSource()
        {
            var go = new GameObject("SFX_Source");
            go.transform.SetParent(poolParent);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            go.SetActive(false);
            return source;
        }

        private AudioSource GetSource()
        {
            for (int i = 0; i < sourcePool.Count; i++)
            {
                if (!sourcePool[i].gameObject.activeSelf)
                {
                    return sourcePool[i];
                }
            }

            if (sourcePool.Count < maxPoolSize)
            {
                var newSource = CreateSource();
                sourcePool.Add(newSource);
                return newSource;
            }

            float oldest = float.MaxValue;
            AudioSource oldestSource = null;

            for (int i = 0; i < activeSources.Count; i++)
            {
                if (activeSources[i] != null && activeSources[i].time < oldest)
                {
                    oldest = activeSources[i].time;
                    oldestSource = activeSources[i];
                }
            }

            if (oldestSource != null)
            {
                oldestSource.Stop();
                return oldestSource;
            }

            return sourcePool[0];
        }

        private void ReturnSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.ignoreListenerPause = false;
            source.spatialBlend = 0f;
            source.gameObject.SetActive(false);
            activeSources.Remove(source);
        }

        public AudioSource Play(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
        {
            if (clip == null) return null;

            var source = GetSource();
            source.gameObject.SetActive(true);
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.pitch = pitch;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = false;
            source.Play();

            activeSources.Add(source);

            if (!loop)
            {
                StartCoroutine(ReturnAfterPlay(source, clip.length / Mathf.Abs(pitch)));
            }

            return source;
        }

        public AudioSource PlayWithVariation(AudioClip clip, float volume = 1f, float basePitch = 1f, float pitchVariation = -1f)
        {
            if (clip == null) return null;

            float variation = pitchVariation < 0f ? defaultPitchVariation : pitchVariation;
            float pitch = basePitch + Random.Range(-variation, variation);

            return Play(clip, volume, pitch);
        }

        public AudioSource PlayUI(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return null;

            var source = GetSource();
            source.gameObject.SetActive(true);
            source.clip = clip;
            source.volume = volume * uiVolume * masterVolume;
            source.pitch = pitch;
            source.loop = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.Play();

            activeSources.Add(source);

            StartCoroutine(ReturnAfterPlay(source, clip.length / Mathf.Abs(pitch)));

            return source;
        }

        public AudioSource PlayUIWithVariation(AudioClip clip, float volume = 1f, float basePitch = 1f, float pitchVariation = -1f)
        {
            if (clip == null) return null;

            float variation = pitchVariation < 0f ? defaultPitchVariation : pitchVariation;
            float pitch = basePitch + Random.Range(-variation, variation);

            return PlayUI(clip, volume, pitch);
        }

        public AudioSource Play3D(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float minDistance = 1f, float maxDistance = 50f)
        {
            if (clip == null) return null;

            var source = GetSource();
            source.gameObject.SetActive(true);
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.pitch = pitch;
            source.loop = false;
            source.spatialBlend = 1f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.ignoreListenerPause = false;
            source.Play();

            activeSources.Add(source);

            StartCoroutine(ReturnAfterPlay(source, clip.length / Mathf.Abs(pitch)));

            return source;
        }

        public AudioSource Play3DWithVariation(AudioClip clip, Vector3 position, float volume = 1f, float basePitch = 1f, float pitchVariation = -1f, float minDistance = 1f, float maxDistance = 50f)
        {
            if (clip == null) return null;

            float variation = pitchVariation < 0f ? defaultPitchVariation : pitchVariation;
            float pitch = basePitch + Random.Range(-variation, variation);

            return Play3D(clip, position, volume, pitch, minDistance, maxDistance);
        }

        public void StopSource(AudioSource source)
        {
            if (source == null) return;
            ReturnSource(source);
        }

        public void StopAll()
        {
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                if (activeSources[i] != null)
                {
                    ReturnSource(activeSources[i]);
                }
            }

            activeSources.Clear();
        }

        private IEnumerator ReturnAfterPlay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay + 0.05f);

            if (source != null && !source.isPlaying)
            {
                ReturnSource(source);
            }
        }

        public void PlayJump()
        {
            PlayWithVariation(jumpSound, 0.7f, 1f, 0.08f);
        }

        public void PlayDoubleJump()
        {
            PlayWithVariation(doubleJumpSound, 0.75f, 1.15f, 0.08f);
        }

        public void PlayLand()
        {
            PlayWithVariation(landSound, 0.5f, 1f, 0.06f);
        }

        public void PlayDash()
        {
            PlayWithVariation(dashSound, 0.8f, 1.1f, 0.1f);
        }

        public void PlayDeflect()
        {
            PlayWithVariation(deflectSound, 0.9f, 1.05f, 0.08f);
        }

        public void PlayDeath()
        {
            Play(deathSound, 1f, 1f);
        }

        public void PlayRevive()
        {
            Play(reviveSound, 0.9f, 1f);
        }

        public void PlayBulletFire(Vector3 position, bool lethal = false)
        {
            AudioClip clip = lethal ? bulletLethalFireSound : bulletFireSound;
            float volume = lethal ? 0.75f : 0.6f;
            Play3DWithVariation(clip, position, volume, 1f, 0.1f, 2f, 100f);
        }

        public void PlayBulletImpact(Vector3 position, bool lethal = false)
        {
            AudioClip clip = lethal ? bulletLethalImpactSound : bulletImpactSound;
            float volume = lethal ? 0.7f : 0.5f;
            Play3DWithVariation(clip, position, volume, 1f, 0.08f, 1f, 30f);
        }

        public void PlayBulletWhiz(Vector3 position)
        {
            Play3DWithVariation(bulletWhizSound, position, 0.4f, 1f, 0.15f, 1f, 20f);
        }

        public void PlayBulletExpire(Vector3 position)
        {
            Play3DWithVariation(bulletExpireSound, position, 0.25f, 1f, 0.1f, 1f, 15f);
        }

        public void PlayEnemyHit(Vector3 position)
        {
            Play3DWithVariation(enemyHitSound, position, 0.7f, 1f, 0.1f, 2f, 30f);
        }

        public void PlayEnemyDeath(Vector3 position)
        {
            Play3DWithVariation(enemyDeathSound, position, 0.8f, 1f, 0.08f, 3f, 35f);
        }

        public void PlayEnemySpawn(Vector3 position)
        {
            Play3DWithVariation(enemySpawnSound, position, 0.5f, 1f, 0.05f, 2f, 25f);
        }

        public void PlayEnemyAlert(Vector3 position)
        {
            Play3DWithVariation(enemyAlertSound, position, 0.6f, 1f, 0.05f, 3f, 30f);
        }

        public void PlayEnemyRagdoll(Vector3 position)
        {
            Play3DWithVariation(enemyRagdollSound, position, 0.5f, 0.9f, 0.1f, 2f, 20f);
        }

        public void PlaySniperCharge(Vector3 position)
        {
            Play3D(sniperChargeSound, position, 0.65f, 1f, 3f, 40f);
        }

        public void PlaySniperFire(Vector3 position)
        {
            Play3DWithVariation(sniperFireSound, position, 0.85f, 1f, 0.05f, 3f, 50f);
        }

        public void PlayButtonClick()
        {
            PlayUI(buttonClickSound, 0.6f, 1f);
        }

        public void PlayButtonHover()
        {
            PlayUI(buttonHoverSound, 0.3f, 1.1f);
        }

        public void PlayButtonBack()
        {
            PlayUI(buttonBackSound, 0.55f, 0.95f);
        }

        public void PlayButtonDisabled()
        {
            PlayUI(buttonDisabledSound, 0.4f, 0.85f);
        }

        public void PlayTabSwitch()
        {
            PlayUIWithVariation(tabSwitchSound, 0.5f, 1f, 0.05f);
        }

        public void PlayPanelOpen()
        {
            PlayUI(panelOpenSound, 0.6f, 1f);
        }

        public void PlayPanelClose()
        {
            PlayUI(panelCloseSound, 0.55f, 0.95f);
        }

        public void PlayPopupShow()
        {
            PlayUI(popupShowSound, 0.65f, 1.05f);
        }

        public void PlayPopupHide()
        {
            PlayUI(popupHideSound, 0.5f, 0.95f);
        }

        public void PlayCoinCollect()
        {
            PlayWithVariation(coinCollectSound, 0.6f, 1.2f, 0.15f);
        }

        public void PlayGemCollect()
        {
            PlayWithVariation(gemCollectSound, 0.7f, 1.1f, 0.1f);
        }

        public void PlayPowerupCollect()
        {
            Play(powerupCollectSound, 0.8f, 1f);
        }

        public void PlayHealthCollect()
        {
            Play(healthCollectSound, 0.7f, 1f);
        }

        public void PlayPurchaseSuccess()
        {
            PlayUI(purchaseSuccessSound, 0.8f, 1f);
        }

        public void PlayPurchaseFail()
        {
            PlayUI(purchaseFailSound, 0.6f, 0.9f);
        }

        public void PlayEquip()
        {
            PlayUI(equipSound, 0.7f, 1f);
        }

        public void PlayUnequip()
        {
            PlayUI(unequipSound, 0.5f, 0.95f);
        }

        public void PlayUnlock()
        {
            PlayUI(unlockSound, 0.85f, 1f);
        }

        public void PlayUpgrade()
        {
            PlayUI(upgradeSound, 0.8f, 1.05f);
        }

        public void PlayRouletteTick()
        {
            PlayUIWithVariation(rouletteTickSound, 0.45f, 1f, 0.1f);
        }

        public void PlayRouletteTickAtPitch(float pitch)
        {
            PlayUI(rouletteTickSound, 0.45f, pitch);
        }

        public void PlayRouletteSlow()
        {
            PlayUI(rouletteSlowSound, 0.55f, 1f);
        }

        public void PlayRouletteWinCommon()
        {
            PlayUI(rouletteWinCommonSound, 0.7f, 1f);
        }

        public void PlayRouletteWinRare()
        {
            PlayUI(rouletteWinRareSound, 0.8f, 1f);
        }

        public void PlayRouletteWinEpic()
        {
            PlayUI(rouletteWinEpicSound, 0.9f, 1f);
        }

        public void PlayRouletteWinLegendary()
        {
            PlayUI(rouletteWinLegendarySound, 1f, 1f);
        }

        public void PlayRouletteDuplicate()
        {
            PlayUI(rouletteDuplicateSound, 0.6f, 0.9f);
        }

        public void PlayRouletteWinByRarity(Inventory.KatanaRarity rarity)
        {
            switch (rarity)
            {
                case Inventory.KatanaRarity.Common:
                    PlayRouletteWinCommon();
                    break;
                case Inventory.KatanaRarity.Rare:
                    PlayRouletteWinRare();
                    break;
                case Inventory.KatanaRarity.Epic:
                    PlayRouletteWinEpic();
                    break;
                case Inventory.KatanaRarity.Legendary:
                case Inventory.KatanaRarity.Challenge:
                    PlayRouletteWinLegendary();
                    break;
                default:
                    PlayRouletteWinCommon();
                    break;
            }
        }

        public void PlayGameStart()
        {
            Play(gameStartSound, 0.8f, 1f);
        }

        public void PlayGameOver()
        {
            Play(gameOverSound, 1f, 1f);
        }

        public void PlayCountdownTick()
        {
            PlayUI(countdownTickSound, 0.7f, 1f);
        }

        public void PlayCountdownGo()
        {
            PlayUI(countdownGoSound, 0.85f, 1.1f);
        }

        public void PlayPause()
        {
            PlayUI(pauseSound, 0.6f, 1f);
        }

        public void PlayResume()
        {
            PlayUI(resumeSound, 0.6f, 1.05f);
        }

        public void PlayNewHighScore()
        {
            PlayUI(newHighScoreSound, 1f, 1f);
        }

        public void PlayMilestone()
        {
            Play(milestoneSound, 0.8f, 1f);
        }

        public void PlayScoreTick()
        {
            float now = Time.unscaledTime;

            if (now - lastScoreTickTime < 0.08f)
            {
                consecutiveScoreTicks++;
            }
            else
            {
                consecutiveScoreTicks = 0;
            }

            lastScoreTickTime = now;

            float pitch = Mathf.Min(1f + consecutiveScoreTicks * 0.05f, 2f);
            PlayUI(scoreTickSound, 0.35f, pitch);
        }

        public void PlayCombo(int comboCount)
        {
            float pitch = Mathf.Min(1f + comboCount * 0.08f, 2.5f);
            float volume = Mathf.Min(0.6f + comboCount * 0.03f, 1f);
            PlayWithVariation(comboSound, volume, pitch, 0.05f);
        }

        public void PlayComboBreak()
        {
            Play(comboBreakSound, 0.7f, 0.8f);
        }

        public void PlayDistanceMilestone()
        {
            PlayWithVariation(distanceMilestoneSound, 0.75f, 1f, 0.05f);
        }

        public void PlayBiomeTransition()
        {
            Play(biomeTransitionSound, 0.7f, 1f);
        }

        public void PlayObstacleDestroy(Vector3 position)
        {
            Play3DWithVariation(obstacleDestroySound, position, 0.6f, 1f, 0.1f, 2f, 25f);
        }

        private void Update()
        {
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                if (activeSources[i] == null)
                {
                    activeSources.RemoveAt(i);
                    continue;
                }

                if (!activeSources[i].isPlaying && !activeSources[i].loop)
                {
                    ReturnSource(activeSources[i]);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}