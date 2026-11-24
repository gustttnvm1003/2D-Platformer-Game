using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    [Serializable]
    public class InventoryItemStack
    {
        public string itemId;
        public int quantity;
    }

    /// <summary>
    /// Runtime player inventory storing collected items and bridging Silk Essence
    /// cleanly with UpgradeManager as the single source of truth.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [Header("Item Catalog (ScriptableObjects)")]
        [SerializeField] private List<InventoryItemData> itemCatalog = new List<InventoryItemData>();
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private UpgradeManager upgradeManager;

        private readonly Dictionary<string, InventoryItemData> _catalogMap = new Dictionary<string, InventoryItemData>();
        private readonly List<InventoryItemStack> _items = new List<InventoryItemStack>();

        public IList<InventoryItemData> Catalog { get { return itemCatalog; } }
        public IList<InventoryItemStack> Items { get { return _items; } }
        public int SilkEssence { get { return upgradeManager != null ? upgradeManager.SilkEssence : 0; } }

        private void Awake()
        {
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            if (upgradeManager == null) upgradeManager = GetComponent<UpgradeManager>();
            RebuildCatalogMap();
        }

        public void SetItemCatalog(List<InventoryItemData> catalog)
        {
            itemCatalog = catalog != null ? catalog : new List<InventoryItemData>();
            RebuildCatalogMap();
        }

        private void RebuildCatalogMap()
        {
            _catalogMap.Clear();
            for (int i = 0; i < itemCatalog.Count; i++)
            {
                InventoryItemData data = itemCatalog[i];
                if (data != null && !string.IsNullOrEmpty(data.itemId))
                {
                    _catalogMap[data.itemId] = data;
                }
            }
        }

        public InventoryItemData GetItemData(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (_catalogMap.Count == 0 && itemCatalog.Count > 0) RebuildCatalogMap();
            InventoryItemData data;
            return _catalogMap.TryGetValue(itemId, out data) ? data : null;
        }

        public int GetQuantity(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].itemId == itemId) return _items[i].quantity;
            }
            return 0;
        }

        public void ResetToDefaults()
        {
            _items.Clear();
            AddItemInternal("item_mossberry_dew", 1);
            AddItemInternal("item_essence_shard", 1);
            GameEvents.RaiseInventoryChanged();
        }

        public bool AddItem(string itemId, int amount = 1, bool showNotification = true)
        {
            int added = AddItemInternal(itemId, amount);
            if (added <= 0) return false;

            if (showNotification)
            {
                InventoryItemData data = GetItemData(itemId);
                string title = data != null ? data.displayName.ToUpper() : "ITEM OBTAINED";
                string sub = string.Format("Stored in Inventory [I] (+{0})", added);
                GameEvents.RaiseNotification(title, sub);
            }

            GameEvents.RaiseInventoryChanged();
            return true;
        }

        private int AddItemInternal(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return 0;
            InventoryItemData data = GetItemData(itemId);
            int maxStack = data != null ? Mathf.Max(1, data.maxStack) : 99;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].itemId == itemId)
                {
                    int space = Mathf.Max(0, maxStack - _items[i].quantity);
                    int gain = Mathf.Min(space, amount);
                    _items[i].quantity += gain;
                    return gain;
                }
            }

            int initial = Mathf.Min(maxStack, amount);
            _items.Add(new InventoryItemStack { itemId = itemId, quantity = initial });
            return initial;
        }

        public bool RemoveItem(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].itemId == itemId)
                {
                    if (_items[i].quantity < amount) return false;
                    _items[i].quantity -= amount;
                    if (_items[i].quantity <= 0)
                    {
                        _items.RemoveAt(i);
                    }
                    GameEvents.RaiseInventoryChanged();
                    return true;
                }
            }
            return false;
        }

        public bool CanUseItem(string itemId, out string reason)
        {
            reason = string.Empty;
            if (GetQuantity(itemId) <= 0)
            {
                reason = "None remaining in pouch.";
                return false;
            }

            InventoryItemData data = GetItemData(itemId);
            if (data == null || !data.usableFromInventory)
            {
                reason = "Passive relic — kept inside your traveler's pouch.";
                return false;
            }

            bool hasEffect = false;
            if (data.restoreHealthMasks > 0)
            {
                if (playerStats != null && playerStats.CurrentHealth < playerStats.MaxHealth) hasEffect = true;
                else if (data.restoreSilkAmount <= 0f && data.essenceYield <= 0)
                {
                    reason = "Masks are already full.";
                    return false;
                }
            }
            if (data.restoreSilkAmount > 0.01f)
            {
                if (playerStats != null && playerStats.CurrentSilk < playerStats.MaxSilk - 0.5f) hasEffect = true;
                else if (data.restoreHealthMasks <= 0 && data.essenceYield <= 0)
                {
                    reason = "Silk Spool is already full.";
                    return false;
                }
            }
            if (data.essenceYield > 0)
            {
                hasEffect = upgradeManager != null;
            }

            if (!hasEffect)
            {
                reason = "No effect available right now.";
                return false;
            }
            return true;
        }

        public bool TryUseItem(string itemId, out string feedback)
        {
            string denyReason;
            if (!CanUseItem(itemId, out denyReason))
            {
                feedback = denyReason;
                return false;
            }

            InventoryItemData data = GetItemData(itemId);
            List<string> results = new List<string>();

            if (data.restoreHealthMasks > 0 && playerStats != null && playerStats.HealHealth(data.restoreHealthMasks))
            {
                results.Add(string.Format("+{0} Mask", data.restoreHealthMasks));
            }
            if (data.restoreSilkAmount > 0.01f && playerStats != null)
            {
                float before = playerStats.CurrentSilk;
                playerStats.AddSilk(data.restoreSilkAmount);
                int gainedSilk = Mathf.RoundToInt(playerStats.CurrentSilk - before);
                if (gainedSilk > 0) results.Add(string.Format("+{0} Silk", gainedSilk));
            }
            if (data.essenceYield > 0 && upgradeManager != null)
            {
                upgradeManager.AddSilkEssence(data.essenceYield);
                results.Add(string.Format("+{0} Silk Essence", data.essenceYield));
            }

            RemoveItem(itemId, 1);

            if (AudioManager.Instance != null)
            {
                if (data.essenceYield > 0) AudioManager.Instance.PlayUpgrade();
                else AudioManager.Instance.PlayBindBurst();
            }
            if (VFXManager.Instance != null)
            {
                Vector3 pos = transform.position + Vector3.up * 0.95f;
                if (data.restoreHealthMasks > 0) VFXManager.Instance.SpawnBindHealBurst(pos);
                else VFXManager.Instance.SpawnSkillBurst(pos, new Color(0.68f, 0.98f, 0.84f, 0.92f));
            }

            feedback = results.Count > 0
                ? string.Format("Used {0}: {1}.", data.displayName, string.Join(", ", results))
                : string.Format("Used {0}.", data.displayName);
            return true;
        }

        public List<InventorySaveEntry> ExportSaveEntries()
        {
            List<InventorySaveEntry> list = new List<InventorySaveEntry>();
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null && !string.IsNullOrEmpty(_items[i].itemId) && _items[i].quantity > 0)
                {
                    list.Add(new InventorySaveEntry { itemId = _items[i].itemId, quantity = _items[i].quantity });
                }
            }
            return list;
        }

        public void ImportSaveEntries(List<InventorySaveEntry> entries)
        {
            _items.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    InventorySaveEntry entry = entries[i];
                    if (entry != null && !string.IsNullOrEmpty(entry.itemId) && entry.quantity > 0)
                    {
                        AddItemInternal(entry.itemId, entry.quantity);
                    }
                }
            }
            GameEvents.RaiseInventoryChanged();
        }
    }
}
