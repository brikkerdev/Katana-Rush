using UnityEngine;

namespace Runner.LevelGeneration
{
    public class BiomeEnvironment : MonoBehaviour
    {
        private BiomeData biomeData;
        private float spawnZ;

        public BiomeData BiomeData => biomeData;
        public float SpawnZ => spawnZ;

        public void Setup(BiomeData biome, float z)
        {
            biomeData = biome;
            spawnZ = z;
        }
    }
}