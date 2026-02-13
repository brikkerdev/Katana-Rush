using UnityEngine;

namespace Runner.Inventory
{
    public abstract class KatanaAbility : ScriptableObject
    {
        [Header("Info")]
        [SerializeField] private string abilityId;

        public string AbilityId => abilityId;

        public abstract void Activate();
        public abstract void Deactivate();

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(abilityId))
            {
                abilityId = System.Guid.NewGuid().ToString();
            }
        }
    }
}