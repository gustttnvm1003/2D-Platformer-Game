using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// JSON file Save & Load persistence stored in Application.persistentDataPath.
    /// Safely handles missing or corrupted save files with automatic backup recovery.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "silken_grotto_save.json";
        private const string BackupFileName = "silken_grotto_save.bak";

        public static bool ShouldLoadOnGameplayStart = false;

        public static string SaveFilePath
        {
            get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
        }

        private static string BackupFilePath
        {
            get { return Path.Combine(Application.persistentDataPath, BackupFileName); }
        }

        public static bool HasValidSaveFile()
        {
            SaveData probe;
            return TryReadSaveFile(SaveFilePath, out probe) || TryReadSaveFile(BackupFilePath, out probe);
        }

        public static void ClearSaveFile()
        {
            try
            {
                if (File.Exists(SaveFilePath)) File.Delete(SaveFilePath);
                if (File.Exists(BackupFilePath)) File.Delete(BackupFilePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SaveManager] Unable to delete save files: " + ex.Message);
            }
        }

        public bool SaveFromScene(PlayerController player)
        {
            if (player == null) return false;

            SaveData data = new SaveData();
            data.version = 1;
            data.lastCheckpointId = player.LastCheckpointId;
            data.checkpointX = player.LastCheckpointPosition.x;
            data.checkpointY = player.LastCheckpointPosition.y;
            data.checkpointZ = player.LastCheckpointPosition.z;

            if (player.Stats != null)
            {
                data.currentHealth = player.Stats.CurrentHealth;
                data.currentSilk = player.Stats.CurrentSilk;
            }
            data.equippedCrest = PlayerCrestSystem.ActiveCrest.ToString();
            if (player.Upgrades != null)
            {
                data.silkEssence = player.Upgrades.SilkEssence;
                data.upgradeLevels = player.Upgrades.ExportUpgradeLevels();
            }
            if (player.Skills != null)
            {
                data.unlockedSkills = player.Skills.ExportUnlockedSkillIds();
            }
            if (player.Inventory != null)
            {
                data.inventoryItems = player.Inventory.ExportSaveEntries();
            }

            data.brokenWalls = new List<string>(player.BrokenWallIds);
            data.defeatedElites = new List<string>(player.DefeatedEnemyIds);
            data.discoveredAreas = new List<string>(player.DiscoveredAreas);
            data.collectedPickups = new List<string>(player.CollectedPickupIds);

            return WriteSaveAtomically(data);
        }

        public bool LoadAndApplyToScene(PlayerController player)
        {
            if (player == null) return false;

            SaveData data;
            if (!TryReadSaveFile(SaveFilePath, out data))
            {
                if (!TryReadSaveFile(BackupFilePath, out data))
                {
                    Debug.LogWarning("[SaveManager] No valid save found; initializing fresh run.");
                    player.InitializeFreshRun();
                    return false;
                }
            }

            if (player.Skills != null)
            {
                player.Skills.ImportUnlockedSkillIds(data.unlockedSkills);
            }
            if (player.Upgrades != null)
            {
                player.Upgrades.ImportUpgradeLevels(data.upgradeLevels);
                player.Upgrades.SetSilkEssence(data.silkEssence);
            }
            if (player.Inventory != null)
            {
                player.Inventory.ImportSaveEntries(data.inventoryItems);
            }
            if (player.Stats != null)
            {
                player.Stats.SetVitalsFromSave(data.currentHealth, data.currentSilk);
            }
            PlayerCrestSystem.ImportFromSave(data.equippedCrest);

            Vector3 cpPos = new Vector3(data.checkpointX, data.checkpointY, data.checkpointZ);
            player.ApplyWorldFlagsFromSave(
                data.brokenWalls,
                data.defeatedElites,
                data.discoveredAreas,
                data.collectedPickups,
                data.lastCheckpointId,
                cpPos
            );

            ApplyWorldPersistenceToSceneObjects(data);
            return true;
        }

        private void ApplyWorldPersistenceToSceneObjects(SaveData data)
        {
            if (data == null) return;

            if (data.brokenWalls != null && data.brokenWalls.Count > 0)
            {
                HashSet<string> brokenSet = new HashSet<string>(data.brokenWalls);
                BreakableWall[] walls = FindObjectsOfType<BreakableWall>();
                for (int i = 0; i < walls.Length; i++)
                {
                    if (brokenSet.Contains(walls[i].WallId))
                    {
                        walls[i].ForceSetBrokenFromSave();
                    }
                }
            }

            if (data.defeatedElites != null && data.defeatedElites.Count > 0)
            {
                HashSet<string> defeatedSet = new HashSet<string>(data.defeatedElites);
                EnemyController[] enemies = FindObjectsOfType<EnemyController>();
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (defeatedSet.Contains(enemies[i].EnemyId))
                    {
                        enemies[i].ForceSetDefeatedFromSave();
                    }
                }
            }

            if (data.collectedPickups != null && data.collectedPickups.Count > 0)
            {
                HashSet<string> pickupSet = new HashSet<string>(data.collectedPickups);
                InventoryPickup[] pickups = FindObjectsOfType<InventoryPickup>();
                for (int i = 0; i < pickups.Length; i++)
                {
                    if (pickupSet.Contains(pickups[i].PickupId))
                    {
                        pickups[i].ForceSetCollectedFromSave();
                    }
                }
            }

            AbilityPedestal[] pedestals = FindObjectsOfType<AbilityPedestal>();
            for (int i = 0; i < pedestals.Length; i++)
            {
                pedestals[i].RefreshClaimedState();
            }
        }

        private static bool WriteSaveAtomically(SaveData data)
        {
            try
            {
                string dir = Path.GetDirectoryName(SaveFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonUtility.ToJson(data, true);
                if (File.Exists(SaveFilePath))
                {
                    File.Copy(SaveFilePath, BackupFilePath, true);
                }
                File.WriteAllText(SaveFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[SaveManager] Failed to save game: " + ex.Message);
                return false;
            }
        }

        private static bool TryReadSaveFile(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return false;

                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                if (parsed == null || parsed.version <= 0) return false;
                data = parsed;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SaveManager] Corrupted or unreadable save at " + path + ": " + ex.Message);
                return false;
            }
        }
    }
}
