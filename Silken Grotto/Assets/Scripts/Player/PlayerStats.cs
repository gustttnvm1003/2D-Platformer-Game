using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Stores and computes live runtime player attributes from PlayerStatsConfig
    /// plus UpgradeManager tiers, with strict safety clamps.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private PlayerStatsConfig config;

        private int _maxHealth = 5;
        private int _currentHealth = 5;
        private float _maxSilk = 100f;
        private float _currentSilk = 50f;
        private int _attackDamage = 12;
        private float _walkSpeed = 6.2f;
        private float _runSpeed = 9.4f;
        private float _jumpForce = 15.2f;
        private float _airControlMultiplier = 0.85f;
        private float _damageReduction = 0f;
        private float _threadGuardTimer = 0f;

        public PlayerStatsConfig Config { get { return config; } }
        public int MaxHealth { get { return _maxHealth; } }
        public int CurrentHealth { get { return _currentHealth; } }
        public float MaxSilk { get { return _maxSilk; } }
        public float CurrentSilk { get { return _currentSilk; } }
        public int AttackDamage { get { return _attackDamage; } }
        public float WalkSpeed { get { return _walkSpeed; } }
        public float RunSpeed { get { return _runSpeed; } }
        public float JumpForce { get { return _jumpForce; } }
        public float AirControlMultiplier { get { return _airControlMultiplier; } }
        public float DamageReduction { get { return _damageReduction; } }
        public bool HasThreadGuardActive { get { return _threadGuardTimer > 0f; } }
        public float HurtInvulnerabilityDuration { get { return config != null ? config.hurtInvulnerabilityDuration : 1.05f; } }

        private void Awake()
        {
            ApplyUpgradeBonuses(0, 0, 0f, 0f, 0f, 0f, true);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            if (_threadGuardTimer > 0f)
            {
                _threadGuardTimer = Mathf.Max(0f, _threadGuardTimer - Time.deltaTime);
            }

            float regen = config != null ? config.passiveSilkRegenPerSec : 3.5f;
            if (_currentSilk < _maxSilk && _currentHealth > 0 && regen > 0f)
            {
                AddSilk(regen * Time.deltaTime);
            }
        }

        public void SetConfig(PlayerStatsConfig newConfig)
        {
            config = newConfig;
            ApplyUpgradeBonuses(0, 0, 0f, 0f, 0f, 0f, true);
        }

        public void ApplyUpgradeBonuses(
            int bonusMaxHealth,
            int bonusAttackDamage,
            float bonusSpeed,
            float bonusJumpControl,
            float bonusMaxSilk,
            float bonusDefense,
            bool refillVitals)
        {
            int baseHp = config != null ? config.baseMaxHealth : 5;
            int baseAtk = config != null ? config.baseAttackDamage : 12;
            float baseWalk = config != null ? config.baseWalkSpeed : 6.2f;
            float baseRun = config != null ? config.baseRunSpeed : 9.4f;
            float baseJump = config != null ? config.baseJumpForce : 15.2f;
            float baseAir = config != null ? config.baseAirControlMultiplier : 0.85f;
            float baseSilk = config != null ? config.baseMaxSilk : 100f;
            float baseDef = config != null ? config.baseDamageReduction : 0f;

            int hpCap = config != null ? config.maxHealthCap : 10;
            int atkCap = config != null ? config.maxAttackDamageCap : 48;
            float spdCap = config != null ? config.maxSpeedBonusCap : 4.0f;
            float jmpCap = config != null ? config.maxJumpBonusCap : 3.2f;
            float silkCap = config != null ? config.maxSilkCap : 220f;
            float defCap = config != null ? config.maxDamageReductionCap : 0.45f;

            int prevMaxHp = _maxHealth;
            _maxHealth = Mathf.Clamp(baseHp + Mathf.Max(0, bonusMaxHealth), 1, hpCap);
            _attackDamage = Mathf.Clamp(baseAtk + Mathf.Max(0, bonusAttackDamage), 1, atkCap);

            float clampedSpeed = Mathf.Clamp(bonusSpeed, 0f, spdCap);
            _walkSpeed = baseWalk + clampedSpeed * 0.7f;
            _runSpeed = baseRun + clampedSpeed;

            float clampedJump = Mathf.Clamp(bonusJumpControl, 0f, jmpCap);
            _jumpForce = baseJump + clampedJump * 0.65f;
            _airControlMultiplier = Mathf.Clamp(baseAir + clampedJump * 0.12f, 0.5f, 1.35f);

            _maxSilk = Mathf.Clamp(baseSilk + Mathf.Max(0f, bonusMaxSilk), 20f, silkCap);
            _damageReduction = Mathf.Clamp(baseDef + Mathf.Max(0f, bonusDefense), 0f, defCap);

            if (refillVitals)
            {
                _currentHealth = _maxHealth;
                _currentSilk = Mathf.Max(_currentSilk, _maxSilk * 0.6f);
            }
            else
            {
                if (_maxHealth > prevMaxHp)
                {
                    _currentHealth += (_maxHealth - prevMaxHp);
                }
                _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
                _currentSilk = Mathf.Clamp(_currentSilk, 0f, _maxSilk);
            }

            BroadcastVitals();
        }

        public void RestoreFullVitals()
        {
            _currentHealth = _maxHealth;
            _currentSilk = _maxSilk;
            _threadGuardTimer = 0f;
            BroadcastVitals();
        }

        public void SetVitalsFromSave(int savedHealth, float savedSilk)
        {
            _currentHealth = Mathf.Clamp(savedHealth <= 0 ? _maxHealth : savedHealth, 1, _maxHealth);
            _currentSilk = Mathf.Clamp(savedSilk, 0f, _maxSilk);
            BroadcastVitals();
        }

        public int ComputeIncomingDamage(int rawDamage)
        {
            if (rawDamage <= 0) return 0;
            if (_threadGuardTimer > 0f)
            {
                // Thread Guard absorbs the strike cleanly and shatters shield
                _threadGuardTimer = 0f;
                return 0;
            }
            float reduced = rawDamage * (1f - _damageReduction);
            return Mathf.Max(1, Mathf.RoundToInt(reduced));
        }

        public int ApplyHealthDamage(int rawDamage)
        {
            int finalDamage = ComputeIncomingDamage(rawDamage);
            if (finalDamage <= 0) return 0;
            _currentHealth = Mathf.Max(0, _currentHealth - finalDamage);
            GameEvents.RaisePlayerHealthChanged(_currentHealth, _maxHealth);
            return finalDamage;
        }

        public float BindHealSilkCost { get { return config != null ? config.bindHealSilkCost : 35f; } }
        public int BindHealMaskAmount { get { return config != null ? Mathf.Max(1, config.bindHealMaskAmount) : 2; } }

        public bool CanBindHeal()
        {
            return _currentHealth > 0 && _currentHealth < _maxHealth && _currentSilk >= BindHealSilkCost;
        }

        public bool CompleteBindHeal()
        {
            float cost = BindHealSilkCost;
            if (_currentHealth <= 0 || _currentHealth >= _maxHealth) return false;
            if (!TrySpendSilk(cost)) return false;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + BindHealMaskAmount);
            GameEvents.RaisePlayerHealthChanged(_currentHealth, _maxHealth);
            return true;
        }

        public bool HealHealth(int amount)
        {
            if (_currentHealth <= 0 || amount <= 0 || _currentHealth >= _maxHealth) return false;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            GameEvents.RaisePlayerHealthChanged(_currentHealth, _maxHealth);
            return true;
        }

        public bool TryBindHeal()
        {
            if (!CompleteBindHeal()) return false;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCheckpoint();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnSkillBurst(transform.position + Vector3.up, new Color(0.75f, 0.98f, 0.85f, 0.95f));
            return true;
        }

        public void AddSilk(float amount)
        {
            if (amount <= 0f) return;
            float prev = _currentSilk;
            _currentSilk = Mathf.Min(_maxSilk, _currentSilk + amount);
            if (!Mathf.Approximately(prev, _currentSilk))
            {
                GameEvents.RaisePlayerSilkChanged(_currentSilk, _maxSilk);
            }
        }

        public bool TrySpendSilk(float amount)
        {
            if (amount <= 0f) return true;
            if (_currentSilk < amount) return false;
            _currentSilk = Mathf.Max(0f, _currentSilk - amount);
            GameEvents.RaisePlayerSilkChanged(_currentSilk, _maxSilk);
            return true;
        }

        public void ActivateThreadGuard(float duration)
        {
            _threadGuardTimer = Mathf.Max(_threadGuardTimer, duration);
        }

        public void BroadcastVitals()
        {
            GameEvents.RaisePlayerHealthChanged(_currentHealth, _maxHealth);
            GameEvents.RaisePlayerSilkChanged(_currentSilk, _maxSilk);
        }
    }
}
