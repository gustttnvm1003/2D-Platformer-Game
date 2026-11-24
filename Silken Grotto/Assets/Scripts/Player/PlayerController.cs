using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    public interface IInteractable
    {
        string GetInteractPrompt();
        void Interact(PlayerController player);
    }

    /// <summary>
    /// Main player facade coordinating Stats, Movement, Combat, Skills, Upgrades,
    /// Damage reception, Checkpoint Respawn, and World Interactions.
    /// </summary>
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Core Components")]
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerAnimatorController animController;
        [SerializeField] private SkillManager skillManager;
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private Hurtbox hurtbox;

        [Header("Interaction")]
        [SerializeField] private float interactRadius = 1.9f;
        [SerializeField] private LayerMask interactableLayers;

        private bool _isAlive = true;
        private float _staggerTimer;
        private bool _isSitting;
        private bool _isApproachingBench;
        private bool _isBindingHeal;
        private bool _isWitchBinding;
        private CheckpointShrine _activeBench;
        private Coroutine _benchRoutine;
        private Coroutine _bindHealCoroutine;
        private Vector3 _checkpointPosition;
        private string _lastCheckpointId = "start_glade";
        private readonly HashSet<string> _brokenWallIds = new HashSet<string>();
        private readonly HashSet<string> _defeatedEnemyIds = new HashSet<string>();
        private readonly HashSet<string> _discoveredAreas = new HashSet<string>();
        private readonly HashSet<string> _collectedPickupIds = new HashSet<string>();

        public PlayerStats Stats { get { return stats; } }
        public PlayerMovement Movement { get { return movement; } }
        public PlayerCombat Combat { get { return combat; } }
        public SkillManager Skills { get { return skillManager; } }
        public UpgradeManager Upgrades { get { return upgradeManager; } }
        public InventoryManager Inventory { get { return inventoryManager; } }

        public bool IsAlive { get { return _isAlive; } }
        public bool IsStaggered { get { return _staggerTimer > 0f; } }
        public bool IsSitting { get { return _isSitting; } }
        public bool IsBindingHeal { get { return _isBindingHeal; } }
        public bool IsWitchBinding { get { return _isWitchBinding; } }
        public bool IsInvulnerable
        {
            get
            {
                bool dashIFrames = movement != null && movement.IsDashing;
                bool sharpdartIFrames = combat != null && (combat.IsSharpdartActive || combat.IsHarpooning);
                bool hurtIFrames = hurtbox != null && hurtbox.IsInvulnerable;
                return dashIFrames || sharpdartIFrames || hurtIFrames || _isSitting || _isApproachingBench;
            }
        }

        public Vector3 LastCheckpointPosition { get { return _checkpointPosition; } }
        public string LastCheckpointId { get { return _lastCheckpointId; } }
        public ICollection<string> BrokenWallIds { get { return _brokenWallIds; } }
        public ICollection<string> DefeatedEnemyIds { get { return _defeatedEnemyIds; } }
        public ICollection<string> DiscoveredAreas { get { return _discoveredAreas; } }
        public ICollection<string> CollectedPickupIds { get { return _collectedPickupIds; } }

        private void Awake()
        {
            if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (animController == null) animController = GetComponent<PlayerAnimatorController>();
            if (skillManager == null) skillManager = GetComponent<SkillManager>();
            if (upgradeManager == null) upgradeManager = GetComponent<UpgradeManager>();
            if (inventoryManager == null) inventoryManager = GetComponent<InventoryManager>();
            if (hurtbox == null) hurtbox = GetComponentInChildren<Hurtbox>();

            if (interactableLayers.value == 0)
            {
                interactableLayers = 1 << GameConstants.LayerInteractable;
            }

            _checkpointPosition = transform.position;
        }

        public void InitializeFreshRun()
        {
            StopBenchRoutine();
            _isAlive = true;
            _staggerTimer = 0f;
            _isSitting = false;
            _isApproachingBench = false;
            _checkpointPosition = transform.position;
            _lastCheckpointId = "start_glade";
            _brokenWallIds.Clear();
            _defeatedEnemyIds.Clear();
            _discoveredAreas.Clear();
            _collectedPickupIds.Clear();

            if (skillManager != null) skillManager.ResetToDefaults();
            if (upgradeManager != null) upgradeManager.ResetUpgrades();
            if (inventoryManager != null) inventoryManager.ResetToDefaults();
            PlayerCrestSystem.ResetToDefault();
            if (stats != null) stats.RestoreFullVitals();
            if (hurtbox != null) hurtbox.ClearInvulnerability();
            if (movement != null) movement.ResetMovementState(_checkpointPosition);
            if (StageProgressionManager.Instance != null) StageProgressionManager.Instance.SyncStageFromPosition(_checkpointPosition);
            if (combat != null)
            {
                combat.SetDead(false);
                combat.SetStaggered(false);
                combat.SetSitting(false);
            }
        }

        private void Update()
        {
            if (!_isAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_staggerTimer > 0f)
            {
                _staggerTimer -= Time.deltaTime;
                if (_staggerTimer <= 0f && combat != null)
                {
                    combat.SetStaggered(false);
                }
            }

            if (_isSitting && !_isApproachingBench && inputHandler != null)
            {
                bool wantsStand = inputHandler.InteractPressed
                    || inputHandler.HasBufferedJump
                    || inputHandler.DashPressed
                    || Mathf.Abs(inputHandler.Horizontal) > 0.25f;
                if (wantsStand)
                {
                    bool instantJumpOff = inputHandler.HasBufferedJump;
                    if (instantJumpOff) inputHandler.ConsumeJumpBuffer();
                    bool clearMoveAfter = inputHandler.InteractPressed || inputHandler.DashPressed;
                    StandUpFromBench();
                    if (instantJumpOff)
                    {
                        if (movement != null) movement.PerformRestJumpOff();
                        if (animController != null) animController.TriggerRestJumpOff();
                    }
                    if (clearMoveAfter) inputHandler.ClearFrameInputs();
                }
                return;
            }

            if (_isApproachingBench) return;

            if (inputHandler != null)
            {
                if (inputHandler.CycleCrestPressed && !_isBindingHeal && !IsStaggered)
                {
                    CrestType switched = PlayerCrestSystem.CycleNextCrest();
                    CrestProfile prof = PlayerCrestSystem.GetProfile(switched);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUpgrade();
                    if (VFXManager.Instance != null) VFXManager.Instance.SpawnSkillBurst(transform.position + Vector3.up * 1.05f, prof.AccentColor);
                }

                if (inputHandler.HealPressed)
                {
                    TryStartBindHeal();
                }

                if (inputHandler.InteractPressed && !_isBindingHeal)
                {
                    TryInteractNearest();
                }
            }
        }

        public void TryStartBindHeal()
        {
            if (_isBindingHeal || !_isAlive || IsStaggered || _isSitting || _isApproachingBench) return;
            if (movement == null || movement.IsDashing || movement.IsMantling || movement.IsWallSliding) return;
            if (combat != null && combat.IsAttacking) return;
            if (stats == null) return;

            // Witch Crest (00:19:25): Root Leech offensive lifesteal Bind can be triggered anytime Silk >= BindCost!
            if (PlayerCrestSystem.ActiveCrest == CrestType.Witch)
            {
                if (stats.CurrentSilk < stats.BindHealSilkCost) return;
                _bindHealCoroutine = StartCoroutine(WitchRootBindRoutine());
                return;
            }

            if (!stats.CanBindHeal()) return;

            // Hunter & Reaper Crests: Ground or Mid-Air Suspended Bind Heal (+ Reaper's Harvest buff!)
            _bindHealCoroutine = StartCoroutine(BindHealRoutine());
        }

        public void CancelBindHeal()
        {
            if (_bindHealCoroutine != null)
            {
                StopCoroutine(_bindHealCoroutine);
                _bindHealCoroutine = null;
            }
            if (_isBindingHeal)
            {
                _isBindingHeal = false;
                _isWitchBinding = false;
                if (movement != null) movement.SetBindHealLock(false);
                if (combat != null) combat.SetExternalCombatLock(false);
            }
        }

        private IEnumerator BindHealRoutine()
        {
            _isBindingHeal = true;
            _isWitchBinding = false;
            if (movement != null) movement.SetBindHealLock(true);
            if (combat != null) combat.SetExternalCombatLock(true);

            // Frames 0..1: Heal Wind-Up & Channeling Hold (0.52s) with swirling silk ribbons + harmonic hum
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBindCharge();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnBindChannelRibbons(transform, 0.52f);

            yield return new WaitForSeconds(0.52f);

            // Frame 2: Heal Burst Climax (white silhouette flash, starburst, mask restore & heal chime)
            if (_isAlive && stats != null && stats.CompleteBindHeal())
            {
                if (hurtbox != null) hurtbox.TriggerWhiteSilhouetteFlash(0.14f);
                if (VFXManager.Instance != null) VFXManager.Instance.SpawnBindHealBurst(transform.position + Vector3.up * 1.02f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayBindBurst();
                GameEvents.RaiseCameraShake(0.055f, 0.095f);

                // Reaper Crest (00:10:58): Activate Reaper's Harvest (+14 bonus Silk per melee strike for 10s!)
                if (PlayerCrestSystem.ActiveCrest == CrestType.Reaper)
                {
                    PlayerCrestSystem.ActivateReaperHarvest(10.0f);
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.SpawnSkillBurst(transform.position + Vector3.up * 1.05f, new Color(1.0f, 0.84f, 0.42f, 1f));
                    }
                    ChoralCodeVFX.SpawnReaperScytheArc(transform.position + Vector3.up * 1.05f, movement != null ? movement.FacingDirection : 1, 0.90f);
                }
            }

            // Frame 3: Exhale Recovery settle back into neutral idle
            yield return new WaitForSeconds(0.32f);

            _isBindingHeal = false;
            _isWitchBinding = false;
            if (movement != null) movement.SetBindHealLock(false);
            if (combat != null) combat.SetExternalCombatLock(false);
            _bindHealCoroutine = null;
        }

        private IEnumerator WitchRootBindRoutine()
        {
            _isBindingHeal = true;
            _isWitchBinding = true;
            if (movement != null) movement.SetBindHealLock(true);
            if (combat != null) combat.SetExternalCombatLock(true);

            // Snappy Root Leech snare wind-up (0.36s)
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBindCharge();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnBindChannelRibbons(transform, 0.36f);

            yield return new WaitForSeconds(0.36f);

            if (_isAlive && stats != null && stats.TrySpendSilk(stats.BindHealSilkCost))
            {
                float leechRadius = 4.2f;
                int pulseDmg = Mathf.Max(8, Mathf.RoundToInt((combat != null ? combat.ComputeBaseMeleeDamage() : 12) * 0.95f));
                int masksStolen = 0;
                int maxMasksToSteal = 3;
                LayerMask targetMask = (1 << GameConstants.LayerEnemy) | (1 << GameConstants.LayerBreakableWall) | (1 << GameConstants.LayerDestructibleFoliage);

                for (int p = 0; p < 3; p++)
                {
                    Vector3 center = transform.position + Vector3.up * 0.98f;
                    ChoralCodeVFX.SpawnWitchRootLeechBurst(center, leechRadius);
                    if (AudioManager.Instance != null)
                    {
                        if (p == 0) AudioManager.Instance.PlayBindBurst();
                        AudioManager.Instance.PlayAttackSlash((p % 3) + 1);
                    }
                    GameEvents.RaiseCameraShake(0.085f, 0.08f);

                    Collider2D[] cols = Physics2D.OverlapCircleAll(center, leechRadius, targetMask);
                    HashSet<IDamageable> hitThisPulse = new HashSet<IDamageable>();
                    bool hitAnyEnemy = false;

                    for (int i = 0; i < cols.Length; i++)
                    {
                        Collider2D col = cols[i];
                        if (col == null) continue;
                        Hurtbox hb = col.GetComponent<Hurtbox>();
                        IDamageable target = hb != null ? hb.Owner : col.GetComponentInParent<IDamageable>();
                        if (target == null || !target.IsAlive || hitThisPulse.Contains(target)) continue;

                        Vector3 hitPt = col.ClosestPoint(center);
                        Vector2 outDir = (Vector2)(hitPt - center);
                        if (outDir.sqrMagnitude < 0.01f) outDir = Vector2.up;
                        outDir.y = Mathf.Max(0.30f, outDir.y);

                        DamageInfo rootInfo = new DamageInfo(
                            pulseDmg,
                            outDir.normalized,
                            p == 2 ? 7.8f : 2.8f,
                            0.26f,
                            gameObject,
                            hitPt,
                            false,
                            true);

                        bool dealt = hb != null ? hb.ReceiveHit(rootInfo) : target.TakeDamage(rootInfo);
                        if (dealt)
                        {
                            hitThisPulse.Add(target);
                            if (target is EnemyController)
                            {
                                hitAnyEnemy = true;
                                if (VFXManager.Instance != null)
                                {
                                    VFXManager.Instance.SpawnHitImpact(hitPt, new Color(0.52f, 0.98f, 0.72f, 1f), true, false);
                                }
                            }
                        }
                    }

                    // Lifesteal: stealing life energy restores +1 Mask per enemy-hitting pulse (up to +3 Masks!)
                    if (hitAnyEnemy && masksStolen < maxMasksToSteal)
                    {
                        masksStolen++;
                        if (stats.HealHealth(1))
                        {
                            if (hurtbox != null) hurtbox.TriggerWhiteSilhouetteFlash(0.10f);
                            if (VFXManager.Instance != null) VFXManager.Instance.SpawnBindHealBurst(center);
                        }
                    }

                    yield return new WaitForSeconds(0.11f);
                }
            }

            yield return new WaitForSeconds(0.16f);

            _isBindingHeal = false;
            _isWitchBinding = false;
            if (movement != null) movement.SetBindHealLock(false);
            if (combat != null) combat.SetExternalCombatLock(false);
            _bindHealCoroutine = null;
        }

        public void BeginBenchRest(CheckpointShrine bench)
        {
            if (bench == null || !_isAlive) return;
            StopBenchRoutine();
            _activeBench = bench;
            _benchRoutine = StartCoroutine(ApproachAndSitRoutine(bench));
        }

        public void StandUpFromBench()
        {
            StopBenchRoutine();
            bool wasActive = _isSitting || _isApproachingBench;
            _isSitting = false;
            _isApproachingBench = false;

            if (_activeBench != null)
            {
                _activeBench.NotifyPlayerStoodUp();
                _activeBench = null;
            }
            if (movement != null)
            {
                movement.SetRestingOnBench(false, transform.position, movement.FacingDirection);
            }
            if (combat != null)
            {
                combat.SetSitting(false);
            }
            if (wasActive && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayFootstep();
            }
        }

        private void StopBenchRoutine()
        {
            if (_benchRoutine != null)
            {
                StopCoroutine(_benchRoutine);
                _benchRoutine = null;
            }
        }

        private IEnumerator ApproachAndSitRoutine(CheckpointShrine bench)
        {
            _isApproachingBench = true;
            if (combat != null) combat.SetSitting(true);

            Vector3 seatPos = bench.GetSeatWorldPosition();
            Vector3 startPos = transform.position;
            float dx = seatPos.x - startPos.x;

            if (movement != null)
            {
                if (Mathf.Abs(dx) > 0.08f)
                {
                    movement.SetFacing(dx >= 0f ? 1 : -1);
                }
                movement.SetBenchApproach(Mathf.Abs(dx) > 0.08f ? 4.8f : 0.01f);
            }

            float walkDuration = Mathf.Clamp(Mathf.Abs(dx) / 5.2f, 0.06f, 0.28f);
            float elapsed = 0f;
            while (elapsed < walkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / walkDuration);
                float nextX = Mathf.Lerp(startPos.x, seatPos.x, t);
                transform.position = new Vector3(nextX, seatPos.y, startPos.z);
                yield return null;
            }

            _isApproachingBench = false;
            _isSitting = true;
            if (movement != null)
            {
                movement.SetRestingOnBench(true, seatPos, bench.BenchFacingDirection);
            }
            if (animController != null)
            {
                animController.TriggerSitDown();
            }
            _benchRoutine = null;
        }

        private void TryInteractNearest()
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position + Vector3.up * 0.9f, interactRadius, interactableLayers);
            IInteractable best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < cols.Length; i++)
            {
                IInteractable candidate = cols[i].GetComponentInParent<IInteractable>();
                if (candidate == null) continue;
                float d = (cols[i].transform.position - transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = candidate;
                }
            }

            if (best != null)
            {
                best.Interact(this);
            }
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (!_isAlive || IsInvulnerable) return false;
            CancelBindHeal();

            bool hadThreadGuard = stats != null && stats.HasThreadGuardActive;
            int dealt = stats != null ? stats.ApplyHealthDamage(info.Amount) : Mathf.Max(1, info.Amount);

            if (dealt <= 0 && hadThreadGuard)
            {
                // Cross Stitch Parry-Counter / Thread Guard absorbed the hit cleanly!
                if (hurtbox != null) hurtbox.SetInvulnerable(0.65f);
                if (combat != null && combat.TryTriggerCrossStitchCounter(info))
                {
                    return true;
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayThreadGuard();
                if (VFXManager.Instance != null) VFXManager.Instance.SpawnSkillBurst(transform.position + Vector3.up, new Color(0.75f, 0.98f, 0.95f, 1f));
                GameEvents.RaiseCameraShake(0.18f, 0.10f);
                return true;
            }

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnHitImpact(transform.position + Vector3.up * 1.0f, new Color(0.96f, 0.25f, 0.34f, 1f));
            }
            GameEvents.RaiseCameraShake(0.32f, 0.16f);

            if (stats != null && stats.CurrentHealth <= 0)
            {
                HandleDeath();
                return true;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerHit();
            _staggerTimer = Mathf.Max(0.18f, info.StaggerDuration);
            if (combat != null) combat.SetStaggered(true);
            if (movement != null) movement.ApplyKnockback(info.KnockbackDirection, Mathf.Max(5.5f, info.KnockbackForce), 0.22f);
            if (hurtbox != null) hurtbox.SetInvulnerable(stats != null ? stats.HurtInvulnerabilityDuration : 1.05f);
            return true;
        }

        public void TakeHazardDamage(int damage, Vector3 hazardPos)
        {
            if (!_isAlive || IsInvulnerable) return;
            Vector2 knockDir = (transform.position - hazardPos).normalized;
            knockDir.y = 0.7f;
            bool took = TakeDamage(new DamageInfo(damage, knockDir, 7.5f, 0.25f, null, transform.position));
            if (took && _isAlive && movement != null)
            {
                // Reposition safely to last solid mossy ground ledge after spike contact
                transform.position = movement.LastSafeGroundPosition + Vector3.up * 0.25f;
            }
        }

        private void HandleDeath()
        {
            CancelBindHeal();
            StopBenchRoutine();
            _isSitting = false;
            _isApproachingBench = false;
            if (_activeBench != null)
            {
                _activeBench.NotifyPlayerStoodUp();
                _activeBench = null;
            }
            _isAlive = false;
            _staggerTimer = 0f;
            if (combat != null)
            {
                combat.SetSitting(false);
                combat.SetDead(true);
            }
            if (movement != null) movement.SetDead(true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerDeath();
            GameEvents.RaisePlayerDied();
        }

        public void SetCheckpoint(string checkpointId, Vector3 worldPos)
        {
            _lastCheckpointId = string.IsNullOrEmpty(checkpointId) ? "checkpoint" : checkpointId;
            _checkpointPosition = worldPos;
            if (stats != null) stats.RestoreFullVitals();
            GameEvents.RaiseCheckpointActivated(_lastCheckpointId, _checkpointPosition);
        }

        public void RespawnAtLastCheckpoint()
        {
            CancelBindHeal();
            StopBenchRoutine();
            _isSitting = false;
            _isApproachingBench = false;
            if (_activeBench != null)
            {
                _activeBench.NotifyPlayerStoodUp();
                _activeBench = null;
            }
            _isAlive = true;
            _staggerTimer = 0f;
            if (stats != null) stats.RestoreFullVitals();
            if (hurtbox != null) hurtbox.ClearInvulnerability();
            if (combat != null)
            {
                combat.SetDead(false);
                combat.SetStaggered(false);
                combat.SetSitting(false);
            }
            if (movement != null)
            {
                movement.ResetMovementState(_checkpointPosition);
            }
            else
            {
                transform.position = _checkpointPosition;
            }
            if (StageProgressionManager.Instance != null)
            {
                StageProgressionManager.Instance.SyncStageFromPosition(_checkpointPosition);
            }
        }

        public void RecordBrokenWall(string wallId)
        {
            if (!string.IsNullOrEmpty(wallId)) _brokenWallIds.Add(wallId);
        }

        public void RecordDefeatedEnemy(string enemyId)
        {
            if (!string.IsNullOrEmpty(enemyId)) _defeatedEnemyIds.Add(enemyId);
        }

        public void RecordDiscoveredArea(string areaId)
        {
            if (!string.IsNullOrEmpty(areaId)) _discoveredAreas.Add(areaId);
        }

        public void RecordCollectedPickup(string pickupId)
        {
            if (!string.IsNullOrEmpty(pickupId)) _collectedPickupIds.Add(pickupId);
        }

        public void ApplyWorldFlagsFromSave(List<string> walls, List<string> enemies, List<string> areas, List<string> pickups, string checkpointId, Vector3 checkpointPos)
        {
            _brokenWallIds.Clear();
            if (walls != null)
            {
                for (int i = 0; i < walls.Count; i++) _brokenWallIds.Add(walls[i]);
            }

            _defeatedEnemyIds.Clear();
            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++) _defeatedEnemyIds.Add(enemies[i]);
            }

            _discoveredAreas.Clear();
            if (areas != null)
            {
                for (int i = 0; i < areas.Count; i++) _discoveredAreas.Add(areas[i]);
            }

            _collectedPickupIds.Clear();
            if (pickups != null)
            {
                for (int i = 0; i < pickups.Count; i++) _collectedPickupIds.Add(pickups[i]);
            }

            _lastCheckpointId = string.IsNullOrEmpty(checkpointId) ? "start_glade" : checkpointId;
            _checkpointPosition = checkpointPos;
            _isAlive = true;
            if (movement != null) movement.ResetMovementState(_checkpointPosition);
            if (StageProgressionManager.Instance != null) StageProgressionManager.Instance.SyncStageFromPosition(_checkpointPosition);
            if (combat != null)
            {
                combat.SetDead(false);
                combat.SetStaggered(false);
            }
        }
    }
}
