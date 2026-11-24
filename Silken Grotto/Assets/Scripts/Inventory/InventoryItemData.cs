using UnityEngine;

namespace SilkenGrotto
{
    public enum InventoryItemCategory
    {
        Consumable,
        EssenceRelic,
        KeyArtifact
    }

    /// <summary>
    /// ScriptableObject catalog definition for items stored in the player's Inventory Pouch.
    /// Supports restorative consumables, dissolvable Silk Essence shards/relics, and lore artifacts.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_New", menuName = "Silken Grotto/Inventory Item")]
    public class InventoryItemData : ScriptableObject
    {
        public string itemId = "item_new";
        public string displayName = "Cavern Relic";
        public InventoryItemCategory category = InventoryItemCategory.Consumable;
        [TextArea(2, 4)] public string description = "An object recovered within Silken Grotto.";
        public string effectSummary = "Inspect inside Inventory.";
        public Sprite icon;
        [Min(1)] public int maxStack = 99;

        [Header("Use / Channel Effects")]
        public bool usableFromInventory;
        [Min(0)] public int restoreHealthMasks;
        [Min(0f)] public float restoreSilkAmount;
        [Min(0)] public int essenceYield;
    }
}
