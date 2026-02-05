using UnityEngine;

namespace Runner.LevelGeneration
{
    public class SceneSetup : MonoBehaviour
    {
        [Header("Starting Biome")]
        [SerializeField] private BiomeData startingBiome;

        [Header("Scene Lighting")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;

        public BiomeData StartingBiome => startingBiome;
        public Light SunLight => sunLight;
        public Light MoonLight => moonLight;
    }
}