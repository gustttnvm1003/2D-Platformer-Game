using System;
using System.Collections.Generic;

namespace SilkenGrotto
{
    [Serializable]
    public class UpgradeSaveEntry
    {
        public string upgradeType;
        public int level;
    }

    [Serializable]
    public class InventorySaveEntry
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string lastCheckpointId = "start_glade";
        public float checkpointX;
        public float checkpointY;
        public float checkpointZ;

        public int currentHealth = 5;
        public float currentSilk = 60f;
        public int silkEssence = 0;
        public string equippedCrest = "Hunter";

        public List<string> unlockedSkills = new List<string>();
        public List<UpgradeSaveEntry> upgradeLevels = new List<UpgradeSaveEntry>();
        public List<InventorySaveEntry> inventoryItems = new List<InventorySaveEntry>();
        public List<string> brokenWalls = new List<string>();
        public List<string> defeatedElites = new List<string>();
        public List<string> discoveredAreas = new List<string>();
        public List<string> collectedPickups = new List<string>();
    }
}
