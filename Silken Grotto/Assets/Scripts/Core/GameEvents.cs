using System;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Central event bus decoupling gameplay systems (Player, Combat, Skills, Upgrades, Level)
    /// from presentation systems (UI, Audio, VFX, SaveSystem) without excessive Singletons.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<int, int> OnPlayerHealthChanged;
        public static event Action<float, float> OnPlayerSilkChanged;
        public static event Action<int> OnSilkEssenceChanged;
        public static event Action OnInventoryChanged;
        public static event Action<SkillData> OnSkillUnlocked;
        public static event Action<SkillType, float, float> OnSkillCooldownUpdated;
        public static event Action<UpgradeType, int> OnUpgradePurchased;
        public static event Action<string, Vector3> OnCheckpointActivated;
        public static event Action<string, string> OnAreaEntered;
        public static event Action<string, string> OnNotificationRequested;
        public static event Action<string> OnSecretDiscovered;
        public static event Action<string> OnEnemyDefeated;
        public static event Action OnPlayerDied;
        public static event Action<float, float> OnCameraShakeRequested;
        public static event Action<CrestType> OnCrestChanged;

        public static void RaiseGameStateChanged(GameState state)
        {
            if (OnGameStateChanged != null) OnGameStateChanged(state);
        }

        public static void RaisePlayerHealthChanged(int current, int max)
        {
            if (OnPlayerHealthChanged != null) OnPlayerHealthChanged(current, max);
        }

        public static void RaisePlayerSilkChanged(float current, float max)
        {
            if (OnPlayerSilkChanged != null) OnPlayerSilkChanged(current, max);
        }

        public static void RaiseSilkEssenceChanged(int amount)
        {
            if (OnSilkEssenceChanged != null) OnSilkEssenceChanged(amount);
        }

        public static void RaiseInventoryChanged()
        {
            if (OnInventoryChanged != null) OnInventoryChanged();
        }

        public static void RaiseSkillUnlocked(SkillData skill)
        {
            if (OnSkillUnlocked != null) OnSkillUnlocked(skill);
        }

        public static void RaiseSkillCooldownUpdated(SkillType type, float remaining, float total)
        {
            if (OnSkillCooldownUpdated != null) OnSkillCooldownUpdated(type, remaining, total);
        }

        public static void RaiseUpgradePurchased(UpgradeType type, int newLevel)
        {
            if (OnUpgradePurchased != null) OnUpgradePurchased(type, newLevel);
        }

        public static void RaiseCheckpointActivated(string checkpointId, Vector3 position)
        {
            if (OnCheckpointActivated != null) OnCheckpointActivated(checkpointId, position);
        }

        public static void RaiseAreaEntered(string areaName, string subtitle)
        {
            if (OnAreaEntered != null) OnAreaEntered(areaName, subtitle);
        }

        public static void RaiseNotification(string title, string subtitle)
        {
            if (OnNotificationRequested != null) OnNotificationRequested(title, subtitle);
        }

        public static void RaiseSecretDiscovered(string secretId)
        {
            if (OnSecretDiscovered != null) OnSecretDiscovered(secretId);
        }

        public static void RaiseEnemyDefeated(string enemyId)
        {
            if (OnEnemyDefeated != null) OnEnemyDefeated(enemyId);
        }

        public static void RaisePlayerDied()
        {
            if (OnPlayerDied != null) OnPlayerDied();
        }

        public static void RaiseCameraShake(float intensity, float duration)
        {
            if (OnCameraShakeRequested != null) OnCameraShakeRequested(intensity, duration);
        }

        public static void RaiseCrestChanged(CrestType crest)
        {
            if (OnCrestChanged != null) OnCrestChanged(crest);
        }

        public static void ClearAll()
        {
            OnGameStateChanged = null;
            OnPlayerHealthChanged = null;
            OnPlayerSilkChanged = null;
            OnSilkEssenceChanged = null;
            OnInventoryChanged = null;
            OnSkillUnlocked = null;
            OnSkillCooldownUpdated = null;
            OnUpgradePurchased = null;
            OnCheckpointActivated = null;
            OnAreaEntered = null;
            OnNotificationRequested = null;
            OnSecretDiscovered = null;
            OnEnemyDefeated = null;
            OnPlayerDied = null;
            OnCameraShakeRequested = null;
            OnCrestChanged = null;
        }
    }
}
