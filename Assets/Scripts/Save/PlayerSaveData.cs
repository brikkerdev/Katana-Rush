using UnityEngine;
using System;
using System.Collections.Generic;
using Runner.Inventory;

namespace Runner.Save
{
    [Serializable]
    public class PlayerSaveData
    {
        public int coins;
        public int highScore;
        public float totalDistanceRun;
        public int totalEnemiesKilled;
        public int totalDashesUsed;
        public int totalGamesPlayed;
        public int totalCoinsCollected;
        public string equippedKatanaId;
        public List<string> ownedKatanaIds = new List<string>();
        public List<ChallengeProgressData> challengeProgress = new List<ChallengeProgressData>();
    }

    [Serializable]
    public class ChallengeProgressData
    {
        public string katanaId;
        public float currentValue;
    }

    public static class SaveManager
    {
        private const string SAVE_KEY = "PlayerSaveData";
        private static PlayerSaveData cachedData;
        private static bool isDirty;

        public static PlayerSaveData Data
        {
            get
            {
                if (cachedData == null)
                {
                    Load();
                }
                return cachedData;
            }
        }

        public static event Action OnDataChanged;

        public static void Load()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");

            if (string.IsNullOrEmpty(json))
            {
                cachedData = new PlayerSaveData();
                cachedData.coins = 0;
                cachedData.highScore = 0;
                cachedData.ownedKatanaIds = new List<string>();
                cachedData.challengeProgress = new List<ChallengeProgressData>();
            }
            else
            {
                try
                {
                    cachedData = JsonUtility.FromJson<PlayerSaveData>(json);

                    if (cachedData.ownedKatanaIds == null)
                        cachedData.ownedKatanaIds = new List<string>();

                    if (cachedData.challengeProgress == null)
                        cachedData.challengeProgress = new List<ChallengeProgressData>();
                }
                catch
                {
                    cachedData = new PlayerSaveData();
                }
            }
        }

        public static void Save()
        {
            if (cachedData == null) return;

            string json = JsonUtility.ToJson(cachedData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            isDirty = false;
        }

        public static void MarkDirty()
        {
            isDirty = true;
            OnDataChanged?.Invoke();
        }

        public static void SaveIfDirty()
        {
            if (isDirty)
            {
                Save();
            }
        }

        public static int GetCoins()
        {
            return Data.coins;
        }

        public static void SetCoins(int amount)
        {
            Data.coins = amount;
            MarkDirty();
            Save();
        }

        public static void AddCoins(int amount)
        {
            Data.coins += amount;
            Data.totalCoinsCollected += amount;
            MarkDirty();
            Save();
        }

        public static bool SpendCoins(int amount)
        {
            if (Data.coins < amount) return false;

            Data.coins -= amount;
            MarkDirty();
            Save();
            return true;
        }

        public static int GetHighScore()
        {
            return Data.highScore;
        }

        public static void SetHighScore(int score)
        {
            if (score > Data.highScore)
            {
                Data.highScore = score;
                MarkDirty();
                Save();
            }
        }

        public static void AddDistance(float distance)
        {
            Data.totalDistanceRun += distance;
            MarkDirty();
        }

        public static void AddEnemyKill()
        {
            Data.totalEnemiesKilled++;
            MarkDirty();
        }

        public static void AddDashUsed()
        {
            Data.totalDashesUsed++;
            MarkDirty();
        }

        public static void AddGamePlayed()
        {
            Data.totalGamesPlayed++;
            MarkDirty();
        }

        public static float GetChallengeValue(ChallengeType type)
        {
            switch (type)
            {
                case ChallengeType.TotalDistance:
                    return Data.totalDistanceRun;
                case ChallengeType.EnemiesKilled:
                    return Data.totalEnemiesKilled;
                case ChallengeType.DashesUsed:
                    return Data.totalDashesUsed;
                case ChallengeType.GamesPlayed:
                    return Data.totalGamesPlayed;
                case ChallengeType.CoinsCollected:
                    return Data.totalCoinsCollected;
                default:
                    return 0f;
            }
        }

        public static bool IsKatanaOwned(string katanaId)
        {
            return Data.ownedKatanaIds.Contains(katanaId);
        }

        public static void UnlockKatana(string katanaId)
        {
            if (!Data.ownedKatanaIds.Contains(katanaId))
            {
                Data.ownedKatanaIds.Add(katanaId);
                MarkDirty();
                Save();
            }
        }

        public static string GetEquippedKatanaId()
        {
            return Data.equippedKatanaId;
        }

        public static void SetEquippedKatana(string katanaId)
        {
            Data.equippedKatanaId = katanaId;
            MarkDirty();
            Save();
        }

        public static void ResetAllData()
        {
            cachedData = new PlayerSaveData();
            Save();
            OnDataChanged?.Invoke();
        }
    }
}