using UnityEngine;
using System.Collections.Generic;
using Runner.Inventory.Abilities;

namespace Runner.Inventory
{
    public class AbilityManager : MonoBehaviour
    {
        public static AbilityManager Instance { get; private set; }

        private readonly List<KatanaAbility> activeAbilities = new List<KatanaAbility>();

        private TargetedDeflectAbility cachedDeflect;
        private EnemyKillRewardAbility cachedKillReward;
        private DoubleCoinAbility cachedDoubleCoin;
        private ExtendedDashAbility cachedExtendedDash;
        private MagnetAuraAbility cachedMagnetAura;
        private DashDamageBoostAbility cachedDashDamageBoost;
        private SlowFallAbility cachedSlowFall;
        private ScoreMultiplierAbility cachedScoreMultiplier;
        private DashChainAbility cachedDashChain;
        private ShieldAbility cachedShield;
        private SpeedSurgeAbility cachedSpeedSurge;
        private LaneSwitchDashAbility cachedLaneSwitchDash;
        private ExplosiveDashAbility cachedExplosiveDash;
        private CoinStreakAbility cachedCoinStreak;
        private GhostStepAbility cachedGhostStep;
        private DashResetOnKillAbility cachedDashResetOnKill;

        public bool HasTargetedDeflect => cachedDeflect != null;
        public bool HasKillReward => cachedKillReward != null;
        public bool HasDoubleCoin => cachedDoubleCoin != null;
        public bool HasExtendedDash => cachedExtendedDash != null;
        public bool HasMagnetAura => cachedMagnetAura != null;
        public bool HasDashDamageBoost => cachedDashDamageBoost != null;
        public bool HasSlowFall => cachedSlowFall != null;
        public bool HasScoreMultiplier => cachedScoreMultiplier != null;
        public bool HasDashChain => cachedDashChain != null;
        public bool HasShield => cachedShield != null;
        public bool HasSpeedSurge => cachedSpeedSurge != null;
        public bool HasLaneSwitchDash => cachedLaneSwitchDash != null;
        public bool HasExplosiveDash => cachedExplosiveDash != null;
        public bool HasCoinStreak => cachedCoinStreak != null;
        public bool HasGhostStep => cachedGhostStep != null;
        public bool HasDashResetOnKill => cachedDashResetOnKill != null;

        public TargetedDeflectAbility TargetedDeflect => cachedDeflect;
        public EnemyKillRewardAbility KillReward => cachedKillReward;
        public DoubleCoinAbility DoubleCoin => cachedDoubleCoin;
        public ExtendedDashAbility ExtendedDash => cachedExtendedDash;
        public MagnetAuraAbility MagnetAura => cachedMagnetAura;
        public DashDamageBoostAbility DashDamageBoost => cachedDashDamageBoost;
        public SlowFallAbility SlowFall => cachedSlowFall;
        public ScoreMultiplierAbility ScoreMultiplier => cachedScoreMultiplier;
        public DashChainAbility DashChain => cachedDashChain;
        public ShieldAbility Shield => cachedShield;
        public SpeedSurgeAbility SpeedSurge => cachedSpeedSurge;
        public LaneSwitchDashAbility LaneSwitchDash => cachedLaneSwitchDash;
        public ExplosiveDashAbility ExplosiveDash => cachedExplosiveDash;
        public CoinStreakAbility CoinStreak => cachedCoinStreak;
        public GhostStepAbility GhostStep => cachedGhostStep;
        public DashResetOnKillAbility DashResetOnKill => cachedDashResetOnKill;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterAbility(KatanaAbility ability)
        {
            if (ability == null) return;
            if (activeAbilities.Contains(ability)) return;

            activeAbilities.Add(ability);
            CacheAbility(ability);
        }

        public void UnregisterAbility(KatanaAbility ability)
        {
            if (ability == null) return;

            activeAbilities.Remove(ability);
            UncacheAbility(ability);
        }

        public void ClearAllAbilities()
        {
            activeAbilities.Clear();
            cachedDeflect = null;
            cachedKillReward = null;
            cachedDoubleCoin = null;
            cachedExtendedDash = null;
            cachedMagnetAura = null;
            cachedDashDamageBoost = null;
            cachedSlowFall = null;
            cachedScoreMultiplier = null;
            cachedDashChain = null;
            cachedShield = null;
            cachedSpeedSurge = null;
            cachedLaneSwitchDash = null;
            cachedExplosiveDash = null;
            cachedCoinStreak = null;
            cachedGhostStep = null;
            cachedDashResetOnKill = null;
        }

        private void CacheAbility(KatanaAbility ability)
        {
            if (ability is TargetedDeflectAbility deflect)
                cachedDeflect = deflect;
            else if (ability is EnemyKillRewardAbility killReward)
                cachedKillReward = killReward;
            else if (ability is DoubleCoinAbility doubleCoin)
                cachedDoubleCoin = doubleCoin;
            else if (ability is ExtendedDashAbility extendedDash)
                cachedExtendedDash = extendedDash;
            else if (ability is MagnetAuraAbility magnetAura)
                cachedMagnetAura = magnetAura;
            else if (ability is DashDamageBoostAbility dashDmg)
                cachedDashDamageBoost = dashDmg;
            else if (ability is SlowFallAbility slowFall)
                cachedSlowFall = slowFall;
            else if (ability is ScoreMultiplierAbility scoreMult)
                cachedScoreMultiplier = scoreMult;
            else if (ability is DashChainAbility dashChain)
                cachedDashChain = dashChain;
            else if (ability is ShieldAbility shield)
                cachedShield = shield;
            else if (ability is SpeedSurgeAbility speedSurge)
                cachedSpeedSurge = speedSurge;
            else if (ability is LaneSwitchDashAbility laneDash)
                cachedLaneSwitchDash = laneDash;
            else if (ability is ExplosiveDashAbility explosive)
                cachedExplosiveDash = explosive;
            else if (ability is CoinStreakAbility coinStreak)
                cachedCoinStreak = coinStreak;
            else if (ability is GhostStepAbility ghostStep)
                cachedGhostStep = ghostStep;
            else if (ability is DashResetOnKillAbility dashReset)
                cachedDashResetOnKill = dashReset;
        }

        private void UncacheAbility(KatanaAbility ability)
        {
            if (ability is TargetedDeflectAbility && cachedDeflect == ability)
                cachedDeflect = null;
            else if (ability is EnemyKillRewardAbility && cachedKillReward == ability)
                cachedKillReward = null;
            else if (ability is DoubleCoinAbility && cachedDoubleCoin == ability)
                cachedDoubleCoin = null;
            else if (ability is ExtendedDashAbility && cachedExtendedDash == ability)
                cachedExtendedDash = null;
            else if (ability is MagnetAuraAbility && cachedMagnetAura == ability)
                cachedMagnetAura = null;
            else if (ability is DashDamageBoostAbility && cachedDashDamageBoost == ability)
                cachedDashDamageBoost = null;
            else if (ability is SlowFallAbility && cachedSlowFall == ability)
                cachedSlowFall = null;
            else if (ability is ScoreMultiplierAbility && cachedScoreMultiplier == ability)
                cachedScoreMultiplier = null;
            else if (ability is DashChainAbility && cachedDashChain == ability)
                cachedDashChain = null;
            else if (ability is ShieldAbility && cachedShield == ability)
                cachedShield = null;
            else if (ability is SpeedSurgeAbility && cachedSpeedSurge == ability)
                cachedSpeedSurge = null;
            else if (ability is LaneSwitchDashAbility && cachedLaneSwitchDash == ability)
                cachedLaneSwitchDash = null;
            else if (ability is ExplosiveDashAbility && cachedExplosiveDash == ability)
                cachedExplosiveDash = null;
            else if (ability is CoinStreakAbility && cachedCoinStreak == ability)
                cachedCoinStreak = null;
            else if (ability is GhostStepAbility && cachedGhostStep == ability)
                cachedGhostStep = null;
            else if (ability is DashResetOnKillAbility && cachedDashResetOnKill == ability)
                cachedDashResetOnKill = null;
        }

        public bool HasAbility<T>() where T : KatanaAbility
        {
            for (int i = 0; i < activeAbilities.Count; i++)
            {
                if (activeAbilities[i] is T)
                    return true;
            }
            return false;
        }

        public T GetAbility<T>() where T : KatanaAbility
        {
            for (int i = 0; i < activeAbilities.Count; i++)
            {
                if (activeAbilities[i] is T typed)
                    return typed;
            }
            return null;
        }

        public int GetCoinMultiplier()
        {
            if (cachedDoubleCoin != null)
                return cachedDoubleCoin.CoinMultiplier;
            return 1;
        }

        public float GetMagnetRadius()
        {
            if (cachedMagnetAura != null)
                return cachedMagnetAura.MagnetRadius;
            return 0f;
        }

        public float GetDashDamageMultiplier()
        {
            if (cachedDashDamageBoost != null)
                return cachedDashDamageBoost.DamageMultiplier;
            return 1f;
        }

        public float GetScoreMultiplier()
        {
            if (cachedScoreMultiplier != null)
                return cachedScoreMultiplier.ScoreMultiplier;
            return 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}