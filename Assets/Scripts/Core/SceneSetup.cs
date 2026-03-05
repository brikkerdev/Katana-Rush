using System.Collections.Generic;
using UnityEngine;

namespace Runner.LevelGeneration
{
    public class SceneSetup : MonoBehaviour
    {
        [Header("Starting Biome")]
        [SerializeField] private BiomeData startingBiome;
        [SerializeField] private List<BiomeData> availableBiomes;

        [Header("Scene Lighting")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;

        public BiomeData StartingBiome => startingBiome;
        public List<BiomeData> AvailableBiomes => availableBiomes;
        public Light SunLight => sunLight;
        public Light MoonLight => moonLight;
    }
}