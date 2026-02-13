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

        public bool HasTargetedDeflect => cachedDeflect != null;
        public bool HasKillReward => cachedKillReward != null;
        public bool HasDoubleCoin => cachedDoubleCoin != null;
        public bool HasExtendedDash => cachedExtendedDash != null;
        public bool HasMagnetAura => cachedMagnetAura != null;

        public TargetedDeflectAbility TargetedDeflect => cachedDeflect;
        public EnemyKillRewardAbility KillReward => cachedKillReward;
        public DoubleCoinAbility DoubleCoin => cachedDoubleCoin;
        public ExtendedDashAbility ExtendedDash => cachedExtendedDash;
        public MagnetAuraAbility MagnetAura => cachedMagnetAura;

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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}