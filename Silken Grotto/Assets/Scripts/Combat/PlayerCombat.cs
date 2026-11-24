using System.Collections;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Handles melee needle attacks (3-hit ground combo, upward attack, downward air pogo attack,
    /// air slash), active-window Hitbox management, Enhanced Attack passive bonus,
    /// Silk Spear ranged special attack, and Thread Guard defensive skill.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private SkillManager skillManager;
        [SerializeField] private Hitbox meleeHitbox;
        [SerializeField] private HitStopController hitStopController;

        [Header("Attack Data (ScriptableObjects)")]
        [SerializeField] private AttackData[] groundComboAttacks;
        [SerializeField] private AttackData airForwardAttack;
        [SerializeField] private AttackData upAttack;
        [SerializeField] private AttackData downAirAttack;
        [SerializeField] private float comboResetWindow = 0.65f;

        [Header("Silk Spear Projectile")]
        [SerializeField] private Sprite silkSpearSprite;
        [SerializeField] private GameObject silkSpearPrefab;
        [SerializeField] private LayerMask enemyAndBreakableLayers;

        [Header("Charge Attack, Cross Stitch & Thread Storm Tuning")]
        [SerializeField] private float chargeReadyDuration = 0.44f;
        [SerializeField] private float crossStitchParryWindow = 0.58f;
        [SerializeField] private float threadStormRadius = 2.65f;
        [SerializeField] private float threadStormDuration = 0.46f;

        private bool _isAttacking;
        private bool _isAirAttacking;
        private int _comboIndex;
        private float _comboWindowTimer;
        private float _attackCooldownTimer;
        private Coroutine _attackCoroutine;
        private bool _isDead;
        private bool _isStaggered;
        private bool _isSitting;
        private bool _isExternalLocked;

        // Needle Charge Attack state (hold Attack >= 0.44s, release for 2-hit guard-breaking strike!)
        private bool _isChargeReady;
        private bool _wasChargingAuraVisible;
        private GameObject _chargeAuraObj;

        // Cross Stitch parry, Thread Storm, Sharpdart, Rune Rage, Pale Nails, Taunt, Harpoon, Silksoar & Needolin!
        private float _crossStitchTimer;
        private bool _isThreadStormActive;
        private bool _isSharpdartActive;
        private bool _isTaunting;
        private float _tauntElapsed;
        private float _tauntCooldownTimer;
        private Coroutine _tauntCoroutine;
        private bool _isHarpooning;
        private float _harpoonCooldownTimer;
        private LineRenderer _activeHarpoonCord;
        private bool _isSilksoaring;
        private float _silksoarCooldownTimer;
        private bool _hasUsedAirSilksoar;
        private bool _isPlayingNeedolin;
        private float _needolinElapsed;
        private float _needolinCooldownTimer;
        private Coroutine _needolinCoroutine;

        public bool IsAttacking { get { return _isAttacking; } }
        public bool IsAirAttacking { get { return _isAirAttacking; } }
        public bool IsChargingAttack { get { return _wasChargingAuraVisible || _isChargeReady; } }
        public bool IsChargeReady { get { return _isChargeReady; } }
        public bool IsCrossStitchParryWindow { get { return _crossStitchTimer > 0f; } }
        public bool IsThreadStormActive { get { return _isThreadStormActive; } }
        public bool IsSharpdartActive { get { return _isSharpdartActive; } }
        public bool IsTaunting { get { return _isTaunting; } }
        public bool IsHarpooning { get { return _isHarpooning; } }
        public bool IsSilksoaring { get { return _isSilksoaring; } }
        public bool IsPlayingNeedolin { get { return _isPlayingNeedolin; } }
        public int CurrentComboStep { get { return _comboIndex; } }

        public void SetExternalCombatLock(bool locked)
        {
            _isExternalLocked = locked;
            if (locked) CancelActiveAttack();
        }

        public event System.Action<int, bool> OnAttackStarted;

        private void Awake()
        {
            if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (skillManager == null) skillManager = GetComponent<SkillManager>();
            if (hitStopController == null) hitStopController = FindObjectOfType<HitStopController>();

            if (enemyAndBreakableLayers.value == 0)
            {
                enemyAndBreakableLayers = (1 << GameConstants.LayerEnemy)
                    | (1 << GameConstants.LayerBreakableWall)
                    | (1 << GameConstants.LayerDestructibleFoliage);
            }

            if (meleeHitbox != null)
            {
                // Include LayerHazard on melee hitbox so downward aerial pogo strikes bounce off thorn spikes
                meleeHitbox.SetTargetLayers(enemyAndBreakableLayers | (1 << GameConstants.LayerHazard));
                meleeHitbox.Deactivate();
            }

            _chargeAuraObj = ChoralCodeVFX.CreateNeedleChargeAura(transform);
        }

        public void ConfigureAttacks(
            AttackData[] combo,
            AttackData airFwd,
            AttackData upAtk,
            AttackData downAtk,
            Sprite spearSprite)
        {
            groundComboAttacks = combo;
            airForwardAttack = airFwd;
            upAttack = upAtk;
            downAirAttack = downAtk;
            silkSpearSprite = spearSprite;
        }

        public void SetDead(bool dead)
        {
            _isDead = dead;
            if (dead) CancelActiveAttack();
        }

        public void SetStaggered(bool staggered)
        {
            _isStaggered = staggered;
            if (staggered) CancelActiveAttack();
        }

        public void SetSitting(bool sitting)
        {
            _isSitting = sitting;
            if (sitting) CancelActiveAttack();
        }

        public void CancelActiveAttack()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }
            CancelTauntIfActive();
            CancelNeedolinIfActive();
            CleanupHarpoonCord();
            _isAttacking = false;
            _isAirAttacking = false;
            _isThreadStormActive = false;
            _isSharpdartActive = false;
            _isHarpooning = false;
            _isSilksoaring = false;
            ClearChargeAura();
            if (meleeHitbox != null) meleeHitbox.Deactivate();
            if (movement != null)
            {
                movement.SetExternalVelocityOverride(false, Vector2.zero);
                movement.SetCombatMovementLock(false);
                movement.SetAirSuspendLock(false);
            }
        }

        private void CleanupHarpoonCord()
        {
            if (_activeHarpoonCord != null)
            {
                Destroy(_activeHarpoonCord.gameObject);
                _activeHarpoonCord = null;
            }
        }

        private void CancelNeedolinIfActive()
        {
            if (_needolinCoroutine != null)
            {
                StopCoroutine(_needolinCoroutine);
                _needolinCoroutine = null;
            }
            if (_isPlayingNeedolin)
            {
                _isPlayingNeedolin = false;
                _needolinElapsed = 0f;
                if (movement != null) movement.SetCombatMovementLock(false);
            }
        }

        private void CancelTauntIfActive()
        {
            if (_tauntCoroutine != null)
            {
                StopCoroutine(_tauntCoroutine);
                _tauntCoroutine = null;
            }
            if (_isTaunting)
            {
                _isTaunting = false;
                _tauntElapsed = 0f;
                if (movement != null) movement.SetCombatMovementLock(false);
            }
        }

        private void ClearChargeAura()
        {
            _isChargeReady = false;
            _wasChargingAuraVisible = false;
            if (_chargeAuraObj != null) _chargeAuraObj.SetActive(false);
        }

        private void Update()
        {
            if (_isDead || _isStaggered || _isSitting || _isExternalLocked || inputHandler == null)
            {
                ClearChargeAura();
                return;
            }
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float dt = Time.deltaTime;
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= dt;
            if (_crossStitchTimer > 0f) _crossStitchTimer -= dt;
            if (_tauntCooldownTimer > 0f) _tauntCooldownTimer -= dt;
            if (_harpoonCooldownTimer > 0f) _harpoonCooldownTimer -= dt;
            if (_silksoarCooldownTimer > 0f) _silksoarCooldownTimer -= dt;
            if (_needolinCooldownTimer > 0f) _needolinCooldownTimer -= dt;
            if (movement != null && movement.IsGrounded) _hasUsedAirSilksoar = false;

            if (_isTaunting)
            {
                _tauntElapsed += dt;
                bool wantsCancel = _tauntElapsed >= 0.12f
                    && (inputHandler.HasBufferedJump || inputHandler.DashPressed || inputHandler.HasBufferedAttack
                        || inputHandler.SilkSpearPressed || inputHandler.PaleNailsPressed || inputHandler.ThreadGuardPressed
                        || inputHandler.HarpoonPressed || inputHandler.SilksoarPressed || inputHandler.NeedolinPressed
                        || Mathf.Abs(inputHandler.Horizontal) > 0.35f);
                if (wantsCancel || (movement != null && !movement.IsGrounded))
                {
                    CancelTauntIfActive();
                }
            }
            if (_isPlayingNeedolin)
            {
                _needolinElapsed += dt;
                bool wantsCancel = _needolinElapsed >= 0.12f
                    && (inputHandler.HasBufferedJump || inputHandler.DashPressed || inputHandler.HasBufferedAttack
                        || inputHandler.SilkSpearPressed || inputHandler.PaleNailsPressed || inputHandler.ThreadGuardPressed
                        || inputHandler.HarpoonPressed || inputHandler.SilksoarPressed || inputHandler.TauntPressed
                        || Mathf.Abs(inputHandler.Horizontal) > 0.25f);
                if (wantsCancel || (movement != null && !movement.IsGrounded))
                {
                    CancelNeedolinIfActive();
                }
            }
            if (_comboWindowTimer > 0f)
            {
                _comboWindowTimer -= dt;
                if (_comboWindowTimer <= 0f) _comboIndex = 0;
            }

            // 0a. Harpoon / Clawline (L-Ctrl / Mouse3, 00:06:08) -> Tethered Needle Zip-Pull & Enemy Pogo Vault!
            if (inputHandler.HarpoonPressed && !_isAttacking && !_isHarpooning && !_isSilksoaring && _harpoonCooldownTimer <= 0f
                && (movement == null || (!movement.IsMantling)))
            {
                CancelTauntIfActive();
                CancelNeedolinIfActive();
                if (movement != null && movement.IsDashing) movement.CancelDashForAttack();
                _attackCoroutine = StartCoroutine(ExecuteHarpoonRoutine());
                return;
            }

            // 0b. Silksoar (L-Alt / Y, 00:07:24) -> Explosive Vertical Silk Rocket Launch!
            bool canAirSoar = movement == null || movement.IsGrounded || !_hasUsedAirSilksoar;
            if (inputHandler.SilksoarPressed && !_isAttacking && !_isHarpooning && !_isSilksoaring && _silksoarCooldownTimer <= 0f
                && canAirSoar && (movement == null || (!movement.IsMantling && !movement.IsWallSliding)))
            {
                CancelTauntIfActive();
                CancelNeedolinIfActive();
                if (movement != null && movement.IsDashing) movement.CancelDashForAttack();
                _attackCoroutine = StartCoroutine(ExecuteSilksoarRoutine());
                return;
            }

            // 0c. Needolin (N, 00:06:29) -> Grounded Silk-Violin Resonance Melody (Dazes Enemies & Harmonizes Sanctuaries!)
            if (inputHandler.NeedolinPressed && !_isAttacking && !_isHarpooning && !_isSilksoaring && !_isPlayingNeedolin && _needolinCooldownTimer <= 0f
                && (movement == null || (movement.IsGrounded && !movement.IsDashing && !movement.IsWallSliding && !movement.IsMantling)))
            {
                CancelTauntIfActive();
                _needolinCoroutine = StartCoroutine(ExecuteNeedolinRoutine());
                return;
            }

            // 1. Cross Stitch Parry-Counter / Thread Guard (R / V)
            if (inputHandler.ThreadGuardPressed && !_isThreadStormActive && !_isSharpdartActive && !_isHarpooning && !_isSilksoaring)
            {
                CancelTauntIfActive();
                CancelNeedolinIfActive();
                TryCastThreadGuard();
            }

            // 2. Silk Skills (Q / F):
            //    - Up (W) + Q/F             -> Thread Storm (360-deg Silk Cyclone)
            //    - Down (S) + Q/F           -> Rune Rage (Delayed Multi-Rune AoE Detonation)
            //    - Shift + Q/F or Dash+Q/F  -> Sharpdart (i-Frame Piercing Silk Blitz + Delayed Slash)
            //    - Neutral Q/F              -> Silkspear (Linear Piercing Needle)
            if (inputHandler.SilkSpearPressed && !_isAttacking)
            {
                bool isDashingNow = movement != null && movement.IsDashing;
                if (!isDashingNow && inputHandler.Vertical > 0.35f)
                {
                    CancelTauntIfActive();
                    TryCastThreadStorm();
                    return;
                }
                if (!isDashingNow && inputHandler.Vertical < -0.35f)
                {
                    CancelTauntIfActive();
                    TryCastRuneRage();
                    return;
                }
                if (isDashingNow || inputHandler.SprintHeld)
                {
                    CancelTauntIfActive();
                    TryCastSharpdart();
                    return;
                }
                CancelTauntIfActive();
                TryCastSilkSpear();
                return;
            }

            // 2b. Pale Nails (C / Middle Mouse) -> Summons 3 Homing Pale Silk Claws!
            if (inputHandler.PaleNailsPressed && !_isAttacking && (movement == null || !movement.IsDashing))
            {
                CancelTauntIfActive();
                TryCastPaleNails();
                return;
            }

            // 2c. Taunt / Garama! (T / G) -> Challenge Flourish + Enemy Aggro Pull (+8 Silk if provoked!)
            if (inputHandler.TauntPressed && !_isAttacking && !_isTaunting && _tauntCooldownTimer <= 0f
                && (movement == null || (movement.IsGrounded && !movement.IsDashing && !movement.IsWallSliding && !movement.IsMantling)))
            {
                _tauntCoroutine = StartCoroutine(ExecuteTauntRoutine());
                return;
            }

            // 3. Needle Charge Attack tracking (Hold LMB/J/X >= 0.44s while moving/jumping, Release -> Heavy Guard-Breaking Strike!)
            UpdateNeedleChargeState();

            if (inputHandler.AttackReleasedThisFrame && _isChargeReady && !_isAttacking && (movement == null || !movement.IsDashing || movement.CanDashCancelToSprintAttack))
            {
                ClearChargeAura();
                inputHandler.ConsumeAttackBuffer();
                if (movement != null && movement.CanDashCancelToSprintAttack) movement.CancelDashForAttack();
                _attackCoroutine = StartCoroutine(ExecuteChargeAttackRoutine());
                return;
            }

            // 4. Melee Needle Attack (Sprint Lunge / Wall Slash / Combo / Up / Down-Pogo / Air)
            bool dashAllowsAttack = movement == null || !movement.IsDashing || movement.CanDashCancelToSprintAttack;
            if (inputHandler.HasBufferedAttack && !_isAttacking && _attackCooldownTimer <= 0f && dashAllowsAttack)
            {
                bool dashCancel = movement != null && movement.CanDashCancelToSprintAttack;
                if (dashCancel) movement.CancelDashForAttack();
                inputHandler.ConsumeAttackBuffer();
                StartSelectedMeleeAttack(dashCancel);
            }
        }

        private void UpdateNeedleChargeState()
        {
            if (inputHandler == null || _isAttacking)
            {
                if (_chargeAuraObj != null && _chargeAuraObj.activeSelf) _chargeAuraObj.SetActive(false);
                if (!inputHandler.AttackHeld)
                {
                    _isChargeReady = false;
                    _wasChargingAuraVisible = false;
                }
                return;
            }

            if (inputHandler.AttackHeld && inputHandler.AttackHoldDuration >= 0.18f)
            {
                float prog = Mathf.Clamp01((inputHandler.AttackHoldDuration - 0.18f) / Mathf.Max(0.05f, chargeReadyDuration - 0.18f));
                bool readyNow = inputHandler.AttackHoldDuration >= chargeReadyDuration;
                if (readyNow && !_isChargeReady)
                {
                    _isChargeReady = true;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.62f);
                    ChoralCodeVFX.SpawnTelegraphGlint(transform.position + Vector3.up * 0.98f, new Color(1.0f, 0.96f, 0.72f, 0.95f), 0.72f, 0.22f);
                }
                _wasChargingAuraVisible = true;
                if (_chargeAuraObj != null)
                {
                    _chargeAuraObj.SetActive(true);
                    float pulse = readyNow ? (1.02f + Mathf.Sin(Time.time * 24f) * 0.12f) : Mathf.Lerp(0.42f, 0.88f, prog);
                    _chargeAuraObj.transform.localScale = Vector3.one * pulse;
                    _chargeAuraObj.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * (readyNow ? 220f : 95f));
                }
            }
            else if (!inputHandler.AttackHeld)
            {
                if (!inputHandler.AttackReleasedThisFrame)
                {
                    ClearChargeAura();
                }
            }
        }

        private void StartSelectedMeleeAttack(bool fromDashCancel)
        {
            // A. Wall Attack (00:10:41): slash cleanly outward while clinging/sliding on a wall!
            if (movement != null && movement.IsWallSliding && movement.WallDirection != 0)
            {
                _attackCoroutine = StartCoroutine(ExecuteWallAttackRoutine(-movement.WallDirection));
                return;
            }

            bool grounded = movement == null || movement.IsGrounded;
            float verticalInput = inputHandler.Vertical;

            // B. Sprint Attack (00:09:32): forward lunging needle thrust when sprinting or dash-cancelling!
            bool isSprintingFast = grounded && movement != null
                && (fromDashCancel || (movement.IsRunning && Mathf.Abs(movement.HorizontalVelocity) >= 6.4f))
                && verticalInput <= 0.35f;
            if (isSprintingFast)
            {
                _attackCoroutine = StartCoroutine(ExecuteSprintAttackRoutine());
                return;
            }

            AttackData chosen = null;
            bool isAir = !grounded;
            int step = 1;

            if (verticalInput > 0.35f && upAttack != null)
            {
                chosen = upAttack;
                step = 4;
            }
            else if (!grounded && verticalInput < -0.35f && downAirAttack != null)
            {
                chosen = downAirAttack;
                step = 5;
            }
            else if (!grounded && airForwardAttack != null)
            {
                chosen = airForwardAttack;
                step = 1;
            }
            else if (groundComboAttacks != null && groundComboAttacks.Length > 0)
            {
                int idx = _comboIndex % groundComboAttacks.Length;
                chosen = groundComboAttacks[idx];
                step = idx + 1;
                _comboIndex = (idx + 1) % groundComboAttacks.Length;
                _comboWindowTimer = comboResetWindow;
            }

            _attackCoroutine = StartCoroutine(ExecuteAttackRoutine(chosen, step, isAir));
        }

        public int ComputeBaseMeleeDamage()
        {
            return ComputeMeleeDamage(null);
        }

        private IEnumerator ExecuteAttackRoutine(AttackData data, int comboStep, bool isAir)
        {
            _isAttacking = true;
            _isAirAttacking = isAir;

            CrestProfile crest = PlayerCrestSystem.ActiveProfile;
            float duration = (data != null ? data.animationDuration : 0.26f) * crest.DurationMultiplier;
            float activeStart = data != null ? data.hitboxActiveStart : 0.18f;
            float activeEnd = data != null ? data.hitboxActiveEnd : 0.62f;
            float cooldown = (data != null ? data.cooldown : 0.08f) * crest.CooldownMultiplier;
            Vector2 baseOffset = data != null ? data.hitboxOffset : new Vector2(1.25f, 1.05f);
            Vector2 baseSize = data != null ? data.hitboxSize : new Vector2(1.85f, 1.15f);
            bool isDirectionalVert = data != null && (data.direction == AttackDirection.Up || data.direction == AttackDirection.Down);
            Vector2 localOffset = isDirectionalVert
                ? baseOffset
                : new Vector2(baseOffset.x * crest.OffsetXMultiplier, baseOffset.y);
            Vector2 boxSize = new Vector2(baseSize.x * crest.HitboxWidthMultiplier, baseSize.y * crest.HitboxHeightMultiplier);
            float knockForce = (data != null ? data.knockbackForce : 6.5f) * crest.KnockbackMultiplier;
            float staggerDur = data != null ? data.staggerDuration : 0.20f;
            float hitStopDur = data != null ? data.hitStopDuration : 0.045f;
            float silkGain = data != null ? data.silkGainOnHit : 12f;
            bool lockMove = data != null && data.locksHorizontalMovement && !isAir;
            bool isDownPogo = data != null && data.direction == AttackDirection.Down;
            float slashAngle = data != null ? data.slashAngleDegrees : 0f;
            float slashScale = (data != null ? data.slashScale : 0.85f) * crest.SlashScaleMultiplier;
            Color slashTint = crest.SlashTint;

            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(ComputeMeleeDamage(data) * crest.DamageMultiplier));
            bool enhanced = skillManager != null && skillManager.IsUnlocked(SkillType.EnhancedAttack);
            if (enhanced)
            {
                slashScale *= 1.15f;
            }

            if (movement != null && lockMove)
            {
                movement.SetCombatMovementLock(true);
            }

            // Use dedicated Reaper Scythe (11) or Witch Whip (12) animation clips on forward melee attacks!
            int animStep = (!isDirectionalVert && crest.CustomMeleeComboStep != 0) ? crest.CustomMeleeComboStep : comboStep;
            if (OnAttackStarted != null) OnAttackStarted(animStep, isAir);

            if (AudioManager.Instance != null)
            {
                if (data != null && data.overrideSfx != null) AudioManager.Instance.PlayOneShot(data.overrideSfx, 0.9f);
                else AudioManager.Instance.PlayAttackSlash(crest.Type == CrestType.Reaper ? 3 : comboStep);
            }

            int facing = movement != null ? movement.FacingDirection : 1;
            Vector2 directedOffset = new Vector2(localOffset.x * facing, localOffset.y);
            Vector2 knockDir = new Vector2(facing, 0.25f);
            if (data != null && data.direction == AttackDirection.Up) knockDir = new Vector2(facing * 0.2f, 1f);
            else if (isDownPogo) knockDir = new Vector2(facing * 0.15f, -1f);

            float waitStart = duration * activeStart;
            float activeWindow = Mathf.Max(0.03f, duration * (activeEnd - activeStart));
            float waitEnd = Mathf.Max(0f, duration - waitStart - activeWindow);

            if (waitStart > 0f) yield return new WaitForSeconds(waitStart);

            // Spawn visual slash arc synchronized to active hitbox frames (plus signature Reaper/Witch crest arcs!)
            Vector3 slashPos = transform.position + new Vector3(directedOffset.x, directedOffset.y, 0f);
            if (crest.Type == CrestType.Reaper)
            {
                ChoralCodeVFX.SpawnReaperScytheArc(slashPos, facing, isDirectionalVert ? 0.90f : 1.05f);
            }
            else if (crest.Type == CrestType.Witch && !isDirectionalVert)
            {
                ChoralCodeVFX.SpawnWitchWhipArc(transform.position + new Vector3(facing * 0.45f, 1.02f, 0f), facing, 1.04f);
            }
            if (VFXManager.Instance != null)
            {
                float worldAngle = facing < 0 ? -slashAngle : slashAngle;
                VFXManager.Instance.SpawnSlashEffect(slashPos, facing, worldAngle, slashTint, slashScale);

                // Slashing mossy terrain kicks up fluttering hand-drawn leaf debris (strictly visual)
                bool groundedOnMoss = movement != null && movement.IsGrounded && !movement.IsStandingOnStone;
                if (groundedOnMoss && (data == null || data.direction != AttackDirection.Up))
                {
                    VFXManager.Instance.SpawnFoliageLeafBurst(
                        transform.position + new Vector3(facing * 1.15f, 0.14f, 0f),
                        11,
                        facing * 1.4f
                    );
                }
            }

            bool bouncedThisSwing = false;
            if (meleeHitbox != null)
            {
                DamageInfo dmgInfo = new DamageInfo(
                    finalDamage,
                    knockDir,
                    knockForce,
                    staggerDur,
                    gameObject,
                    transform.position,
                    isDownPogo
                );

                // Hitbox is on a child under root; if root itself flips, local X stays positive; if visualRoot flips, directedOffset applies facing
                Vector2 boxLocal = transform.localScale.x < 0f
                    ? new Vector2(Mathf.Abs(localOffset.x), localOffset.y)
                    : directedOffset;

                meleeHitbox.Activate(boxLocal, boxSize, dmgInfo, delegate(IDamageable target, Vector3 hitPoint)
                {
                    BreakableProp breakableProp = target as BreakableProp;
                    bool isProp = breakableProp != null;
                    if (stats != null && !isProp)
                    {
                        stats.AddSilk(silkGain);
                        if (PlayerCrestSystem.IsReaperHarvestActive)
                        {
                            stats.AddSilk(14f);
                            ChoralCodeVFX.SpawnReaperHarvestSpark(hitPoint, transform.position + Vector3.up * 0.95f);
                        }
                    }

                    EnemyController enemyTarget = target as EnemyController;
                    bool isEnemy = enemyTarget != null;
                    bool isArmoredOrMetallic = (target is HazardZone) || (isEnemy && enemyTarget.Stats != null && enemyTarget.Stats.isElite);

                    if (VFXManager.Instance != null && !isProp)
                    {
                        VFXManager.Instance.SpawnHitImpact(hitPoint, Color.white, isEnemy, !isEnemy && isArmoredOrMetallic);
                    }
                    if (AudioManager.Instance != null && !isProp)
                    {
                        if (isEnemy)
                        {
                            AudioManager.Instance.PlayEnemyHit();
                            if (isArmoredOrMetallic) AudioManager.Instance.PlayArmorClink(0.55f);
                        }
                        else if (isArmoredOrMetallic)
                        {
                            AudioManager.Instance.PlayArmorClink(0.82f);
                        }
                        else
                        {
                            AudioManager.Instance.PlayEnemyHit();
                        }
                    }

                    // Keep grass slicing silky-fluid with zero pause; keep hit-stop for combat & heavy structures
                    if (!isProp)
                    {
                        float clampedHitStop = Mathf.Clamp(hitStopDur, 0.032f, 0.050f);
                        if (hitStopController != null) hitStopController.TriggerHitStop(clampedHitStop);
                        GameEvents.RaiseCameraShake(enhanced ? 0.052f : 0.038f, 0.065f);
                    }

                    bool canPogo = !isProp || breakableProp.AllowPogoBounce;
                    if (isDownPogo && canPogo && !bouncedThisSwing && movement != null)
                    {
                        bouncedThisSwing = true;
                        movement.ApplyPogoBounce();
                    }
                });
            }

            yield return new WaitForSeconds(activeWindow);

            if (meleeHitbox != null) meleeHitbox.Deactivate();

            if (waitEnd > 0f) yield return new WaitForSeconds(waitEnd);

            if (movement != null) movement.SetCombatMovementLock(false);
            _isAttacking = false;
            _isAirAttacking = false;
            _attackCooldownTimer = cooldown;
            _attackCoroutine = null;
        }

        private int ComputeMeleeDamage(AttackData data)
        {
            int statAtk = stats != null ? stats.AttackDamage : 12;
            int attackBase = data != null ? data.baseDamage : 12;
            // Scale cleanly: player upgraded AttackDamage adds onto weapon attack delta
            int total = statAtk + Mathf.Max(0, attackBase - 12);

            if (skillManager != null && skillManager.IsUnlocked(SkillType.EnhancedAttack))
            {
                SkillData enh = skillManager.GetSkillData(SkillType.EnhancedAttack);
                float mult = enh != null && enh.powerValue > 1f ? enh.powerValue : 1.35f;
                total = Mathf.RoundToInt(total * mult);
            }
            return Mathf.Max(1, total);
        }

        private void TryCastSilkSpear()
        {
            if (skillManager == null || !skillManager.TryConsumeSkill(SkillType.SilkSpear, stats))
            {
                return;
            }

            SkillData skill = skillManager.GetSkillData(SkillType.SilkSpear);
            int facing = movement != null ? movement.FacingDirection : 1;
            int damage = Mathf.RoundToInt((stats != null ? stats.AttackDamage : 12) * (skill != null && skill.powerValue > 0.5f ? skill.powerValue : 1.75f));

            Vector3 spawnPos = transform.position + new Vector3(facing * 0.9f, 1.15f, 0f);
            GameObject projObj;
            if (silkSpearPrefab != null)
            {
                projObj = Instantiate(silkSpearPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                projObj = new GameObject("SilkSpearProjectile");
                projObj.transform.position = spawnPos;
                projObj.transform.localScale = Vector3.one * 0.72f;
                SpriteRenderer sr = projObj.AddComponent<SpriteRenderer>();
                sr.sprite = silkSpearSprite;
                sr.sortingLayerName = GameConstants.SortVFX;
                sr.sortingOrder = 12;
                BoxCollider2D col = projObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(2.2f, 0.45f);
                col.isTrigger = true;
                projObj.AddComponent<Rigidbody2D>();
                projObj.AddComponent<SilkSpearProjectile>();
            }

            SilkSpearProjectile proj = projObj.GetComponent<SilkSpearProjectile>();
            if (proj != null)
            {
                LayerMask wallMask = 1 << GameConstants.LayerGround;
                proj.Initialize(damage, 21f, facing, 1.3f, gameObject, enemyAndBreakableLayers, wallMask);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySilkSpear();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnSkillBurst(spawnPos, new Color(0.95f, 0.96f, 1f, 0.95f));
            GameEvents.RaiseCameraShake(0.16f, 0.09f);
        }

        private void TryCastThreadGuard()
        {
            if (skillManager == null || !skillManager.TryConsumeSkill(SkillType.ThreadGuard, stats))
            {
                return;
            }

            SkillData skill = skillManager.GetSkillData(SkillType.ThreadGuard);
            float dur = skill != null && skill.duration > 0.2f ? skill.duration : 2.6f;
            int facing = movement != null ? movement.FacingDirection : 1;

            if (stats != null) stats.ActivateThreadGuard(dur);
            _crossStitchTimer = crossStitchParryWindow;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayThreadGuard();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnAttachedThreadGuard(transform, dur);
            ChoralCodeVFX.SpawnCrossStitchStance(transform, facing, crossStitchParryWindow);
        }

        /// <summary>
        /// Triggered cleanly by PlayerController.TakeDamage when ThreadGuard / Cross Stitch absorbs an enemy strike!
        /// Inside the 0.58s Cross Stitch parry window: unleashes a 3-hit guard-breaking counter flurry with hitstop & +18 Silk!
        /// After the parry window (passive shield): unleashes a crisp 1-hit retaliatory silk slash!
        /// </summary>
        public bool TryTriggerCrossStitchCounter(DamageInfo incomingHit)
        {
            if (_isDead) return false;
            bool isPerfectParry = _crossStitchTimer > 0f;
            _crossStitchTimer = 0f;

            CancelActiveAttack();
            int counterDir = movement != null ? movement.FacingDirection : 1;
            if (incomingHit.Source != null)
            {
                float dx = incomingHit.Source.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.08f) counterDir = dx >= 0f ? 1 : -1;
            }

            _attackCoroutine = StartCoroutine(ExecuteCrossStitchCounterRoutine(counterDir, isPerfectParry));
            return true;
        }

        // =========================================================================
        // 1. SPRINT ATTACK (00:09:32 — Lunging High-Speed Needle Thrust!)
        // =========================================================================
        private IEnumerator ExecuteSprintAttackRoutine()
        {
            _isAttacking = true;
            _isAirAttacking = false;

            int facing = movement != null ? movement.FacingDirection : 1;
            int dmg = Mathf.RoundToInt(ComputeMeleeDamage(null) * 1.28f);

            if (movement != null) movement.ApplyForwardLunge(facing, 12.8f, 0.15f);
            if (OnAttackStarted != null) OnAttackStarted(3, false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAttackSlash(3);
            ChoralCodeVFX.SpawnChargeDust(transform.position, facing, false);

            yield return new WaitForSeconds(0.04f);

            Vector3 thrustOrigin = transform.position + new Vector3(facing * 0.45f, 1.02f, 0f);
            ChoralCodeVFX.SpawnThrustStreak(thrustOrigin, facing, 2.45f, 0.16f, Color.white, 0.14f);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSlashEffect(transform.position + new Vector3(facing * 1.55f, 1.02f, 0f), facing, 0f, Color.white, 1.05f);
            }

            TriggerCustomHitbox(
                new Vector2(1.65f, 1.02f),
                new Vector2(2.55f, 1.08f),
                dmg,
                new Vector2(facing, 0.22f),
                8.2f,
                0.24f,
                0.048f,
                15f,
                false);

            yield return new WaitForSeconds(0.14f);
            if (meleeHitbox != null) meleeHitbox.Deactivate();

            yield return new WaitForSeconds(0.07f);
            _isAttacking = false;
            _attackCooldownTimer = 0.08f;
            _attackCoroutine = null;
        }

        // =========================================================================
        // 2. WALL ATTACK (00:10:41 — Outward Crescent Slash While Wall-Sliding!)
        // =========================================================================
        private IEnumerator ExecuteWallAttackRoutine(int awayDir)
        {
            _isAttacking = true;
            _isAirAttacking = true;

            int dir = awayDir >= 0 ? 1 : -1;
            if (movement != null) movement.SetFacing(dir);
            int dmg = ComputeMeleeDamage(airForwardAttack);

            if (OnAttackStarted != null) OnAttackStarted(1, true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAttackSlash(1);

            yield return new WaitForSeconds(0.035f);

            Vector3 slashPos = transform.position + new Vector3(dir * 1.28f, 1.02f, 0f);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSlashEffect(slashPos, dir, 0f, Color.white, 0.94f);
            }
            ChoralCodeVFX.SpawnSlashArc(slashPos, dir, 1.15f, 0.85f, 118f, 0.14f, Color.white, 0.13f);

            TriggerCustomHitbox(
                new Vector2(1.28f, 1.02f),
                new Vector2(1.98f, 1.38f),
                dmg,
                new Vector2(dir, 0.25f),
                7.2f,
                0.20f,
                0.042f,
                12f,
                false);

            yield return new WaitForSeconds(0.11f);
            if (meleeHitbox != null) meleeHitbox.Deactivate();

            yield return new WaitForSeconds(0.06f);
            _isAttacking = false;
            _isAirAttacking = false;
            _attackCooldownTimer = 0.07f;
            _attackCoroutine = null;
        }

        // =========================================================================
        // 3. CHARGE ATTACK (00:08:52 — 2-Hit Heavy Guard-Breaking Double Arc!)
        // =========================================================================
        private IEnumerator ExecuteChargeAttackRoutine()
        {
            _isAttacking = true;
            bool isAir = movement != null && !movement.IsGrounded;
            _isAirAttacking = isAir;

            int facing = movement != null ? movement.FacingDirection : 1;
            int hitDmg = Mathf.RoundToInt(ComputeMeleeDamage(null) * 1.18f);

            if (movement != null)
            {
                movement.ApplyForwardLunge(facing, isAir ? 6.8f : 10.2f, 0.16f);
            }

            if (OnAttackStarted != null) OnAttackStarted(6, isAir);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAttackSlash(3);
                AudioManager.Instance.PlaySilkSpear();
            }
            ChoralCodeVFX.SpawnChargeDust(transform.position, facing, false);

            yield return new WaitForSeconds(0.045f);

            // Hit 1: Huge golden-white guard-breaking double-arc burst!
            Vector3 burstPos = transform.position + new Vector3(facing * 1.48f, 1.06f, 0f);
            ChoralCodeVFX.SpawnChargeSlashBurst(burstPos, facing);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSlashEffect(burstPos, facing, 10f * facing, new Color(1.0f, 0.96f, 0.78f, 1f), 1.32f);
            }
            GameEvents.RaiseCameraShake(0.068f, 0.095f);

            TriggerCustomHitbox(
                new Vector2(1.55f, 1.06f),
                new Vector2(2.75f, 1.55f),
                hitDmg,
                new Vector2(facing, 0.25f),
                7.6f,
                0.28f,
                0.052f,
                14f,
                true);

            yield return new WaitForSeconds(0.085f);
            if (meleeHitbox != null) meleeHitbox.Deactivate();

            // Hit 2: Immediate follow-through rending sweep (totals ~2.36x damage, breaks EnemyFrontBlockBox!)
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAttackSlash(2);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSlashEffect(burstPos + new Vector3(facing * 0.20f, 0.04f, 0f), facing, -12f * facing, Color.white, 1.38f);
            }

            TriggerCustomHitbox(
                new Vector2(1.68f, 1.08f),
                new Vector2(2.85f, 1.62f),
                hitDmg,
                new Vector2(facing, 0.32f),
                9.5f,
                0.32f,
                0.055f,
                14f,
                true);

            yield return new WaitForSeconds(0.095f);
            if (meleeHitbox != null) meleeHitbox.Deactivate();

            yield return new WaitForSeconds(0.08f);
            _isAttacking = false;
            _isAirAttacking = false;
            _attackCooldownTimer = 0.12f;
            _attackCoroutine = null;
        }

        // =========================================================================
        // 4. CROSS STITCH PARRY-COUNTER FLURRY (00:28:46)
        // =========================================================================
        private IEnumerator ExecuteCrossStitchCounterRoutine(int counterDir, bool isPerfectParry)
        {
            _isAttacking = true;
            _isAirAttacking = movement != null && !movement.IsGrounded;

            if (movement != null)
            {
                movement.SetFacing(counterDir);
                movement.SetAirSuspendLock(true);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayThreadGuard();
                AudioManager.Instance.PlayArmorClink(1.0f);
            }
            if (hitStopController != null) hitStopController.TriggerHitStop(isPerfectParry ? 0.070f : 0.045f);
            ChoralCodeVFX.SpawnCrossStitchCounterBurst(transform.position + new Vector3(counterDir * 0.85f, 1.02f, 0f), counterDir);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSkillBurst(transform.position + Vector3.up, new Color(0.82f, 0.99f, 1.0f, 1f));
            }
            GameEvents.RaiseCameraShake(isPerfectParry ? 0.14f : 0.08f, 0.10f);

            if (stats != null && isPerfectParry)
            {
                stats.AddSilk(18f);
            }

            yield return new WaitForSeconds(0.045f);

            if (movement != null)
            {
                movement.SetAirSuspendLock(false);
                movement.ApplyForwardLunge(counterDir, isPerfectParry ? 11.2f : 6.5f, isPerfectParry ? 0.18f : 0.10f);
            }

            int slashCount = isPerfectParry ? 3 : 1;
            int perHitDmg = Mathf.RoundToInt(ComputeMeleeDamage(null) * (isPerfectParry ? 0.80f : 1.20f));

            for (int i = 0; i < slashCount; i++)
            {
                if (OnAttackStarted != null) OnAttackStarted((i % 3) + 1, _isAirAttacking);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayAttackSlash((i % 3) + 1);

                float ang = (i == 0) ? 26f : (i == 1 ? -26f : 0f);
                Vector3 sPos = transform.position + new Vector3(counterDir * (1.25f + i * 0.18f), 1.04f, 0f);
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnSlashEffect(sPos, counterDir, ang * counterDir, new Color(0.88f, 0.99f, 1.0f, 1f), 1.18f);
                }
                ChoralCodeVFX.SpawnSlashArc(sPos, counterDir, 1.48f, 1.05f, 128f, 0.16f, Color.white, 0.12f);

                TriggerCustomHitbox(
                    new Vector2(1.35f + i * 0.15f, 1.04f),
                    new Vector2(2.65f, 1.65f),
                    perHitDmg,
                    new Vector2(counterDir, 0.25f),
                    i == slashCount - 1 ? 9.2f : 3.5f,
                    0.26f,
                    0.036f,
                    0f,
                    true);

                yield return new WaitForSeconds(0.068f);
                if (meleeHitbox != null) meleeHitbox.Deactivate();
                yield return new WaitForSeconds(0.022f);
            }

            _isAttacking = false;
            _isAirAttacking = false;
            _attackCooldownTimer = 0.06f;
            _attackCoroutine = null;
        }

        // =========================================================================
        // 5. THREAD STORM — 360-DEGREE MULTI-HIT SILK CYCLONE (Up + Q / F, 00:28:29)
        // =========================================================================
        private void TryCastThreadStorm()
        {
            if (_isThreadStormActive || skillManager == null || !skillManager.TryConsumeSkill(SkillType.SilkSpear, stats))
            {
                return;
            }
            _attackCoroutine = StartCoroutine(ExecuteThreadStormRoutine());
        }

        private IEnumerator ExecuteThreadStormRoutine()
        {
            _isAttacking = true;
            _isThreadStormActive = true;
            _isAirAttacking = movement != null && !movement.IsGrounded;

            if (movement != null)
            {
                movement.SetCombatMovementLock(true);
                if (!movement.IsGrounded) movement.SetAirSuspendLock(true);
            }

            if (OnAttackStarted != null) OnAttackStarted(7, _isAirAttacking);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySilkSpear();
                AudioManager.Instance.PlayThreadGuard();
            }

            yield return new WaitForSeconds(0.055f);

            ChoralCodeVFX.SpawnThreadStormVortex(transform, threadStormRadius, threadStormDuration);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSkillBurst(transform.position + Vector3.up * 0.98f, new Color(0.86f, 0.99f, 1.0f, 1f));
            }
            GameEvents.RaiseCameraShake(0.11f, threadStormDuration * 0.85f);

            int pulses = 5;
            float pulseInterval = threadStormDuration / pulses;
            int pulseDmg = Mathf.Max(4, Mathf.RoundToInt(ComputeMeleeDamage(null) * 0.50f));
            LayerMask stormMask = enemyAndBreakableLayers;

            for (int p = 0; p < pulses; p++)
            {
                if (AudioManager.Instance != null && (p % 2 == 1))
                {
                    AudioManager.Instance.PlayAttackSlash((p % 3) + 1);
                }

                Vector3 stormCenter = transform.position + Vector3.up * 0.98f;
                Collider2D[] cols = Physics2D.OverlapCircleAll(stormCenter, threadStormRadius, stormMask);
                System.Collections.Generic.HashSet<IDamageable> hitThisPulse = new System.Collections.Generic.HashSet<IDamageable>();

                for (int i = 0; i < cols.Length; i++)
                {
                    Collider2D col = cols[i];
                    if (col == null) continue;
                    Hurtbox hb = col.GetComponent<Hurtbox>();
                    IDamageable target = hb != null ? hb.Owner : col.GetComponentInParent<IDamageable>();
                    if (target == null || !target.IsAlive || hitThisPulse.Contains(target)) continue;

                    Vector3 hitPt = col.ClosestPoint(stormCenter);
                    Vector2 outDir = (Vector2)(hitPt - stormCenter);
                    if (outDir.sqrMagnitude < 0.01f) outDir = new Vector2(movement != null ? movement.FacingDirection : 1, 0.35f);
                    outDir.y = Mathf.Max(0.28f, outDir.y);

                    DamageInfo stormInfo = new DamageInfo(
                        pulseDmg,
                        outDir.normalized,
                        p == pulses - 1 ? 8.4f : 2.6f,
                        0.22f,
                        gameObject,
                        hitPt,
                        false,
                        true);

                    bool dealt = hb != null ? hb.ReceiveHit(stormInfo) : target.TakeDamage(stormInfo);
                    if (dealt)
                    {
                        hitThisPulse.Add(target);
                        if (VFXManager.Instance != null && !(target is BreakableProp))
                        {
                            VFXManager.Instance.SpawnHitImpact(hitPt, Color.white, target is EnemyController, false);
                        }
                    }
                }

                yield return new WaitForSeconds(pulseInterval);
            }

            if (movement != null)
            {
                movement.SetCombatMovementLock(false);
                movement.SetAirSuspendLock(false);
            }
            _isThreadStormActive = false;
            _isAttacking = false;
            _isAirAttacking = false;
            _attackCooldownTimer = 0.14f;
            _attackCoroutine = null;
        }

        // =========================================================================
        // 6. SHARPDART — I-FRAME PIERCING SILK BLITZ + DELAYED SLASH (Shift/Dash + Q/F, 00:29:03)
        // =========================================================================
        private void TryCastSharpdart()
        {
            if (_isSharpdartActive || skillManager == null || !skillManager.TryConsumeSkill(SkillType.SilkSpear, stats))
            {
                return;
            }
            if (movement != null && movement.IsDashing) movement.CancelDashForAttack();
            _attackCoroutine = StartCoroutine(ExecuteSharpdartRoutine());
        }

        private IEnumerator ExecuteSharpdartRoutine()
        {
            _isAttacking = true;
            _isSharpdartActive = true;
            _isAirAttacking = movement != null && !movement.IsGrounded;

            int facing = movement != null ? movement.FacingDirection : 1;
            if (inputHandler != null && Mathf.Abs(inputHandler.Horizontal) > 0.15f)
            {
                facing = inputHandler.Horizontal > 0f ? 1 : -1;
                if (movement != null) movement.SetFacing(facing);
            }

            if (movement != null)
            {
                movement.SetCombatMovementLock(true);
                movement.SetAirSuspendLock(true);
            }

            if (OnAttackStarted != null) OnAttackStarted(8, _isAirAttacking);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySilkSpear();
                AudioManager.Instance.PlayDash();
            }

            Vector3 startCenter = transform.position + new Vector3(0f, 0.96f, 0f);
            ChoralCodeVFX.SpawnTelegraphGlint(startCenter + new Vector3(facing * 0.65f, 0f, 0f), new Color(0.76f, 0.98f, 1.0f, 0.96f), 0.65f, 0.14f);
            yield return new WaitForSeconds(0.055f);

            if (movement != null)
            {
                movement.ApplyForwardLunge(facing, 27.5f, 0.165f);
            }

            int pierceDmg = Mathf.RoundToInt(ComputeMeleeDamage(null) * 1.15f);
            int afterSlashDmg = Mathf.RoundToInt(ComputeMeleeDamage(null) * 0.78f);
            System.Collections.Generic.HashSet<IDamageable> piercedTargets = new System.Collections.Generic.HashSet<IDamageable>();
            System.Collections.Generic.List<IDamageable> delayedTargets = new System.Collections.Generic.List<IDamageable>();
            System.Collections.Generic.List<Vector3> delayedPositions = new System.Collections.Generic.List<Vector3>();

            float blitzTime = 0.165f;
            float elapsed = 0f;
            while (elapsed < blitzTime)
            {
                elapsed += Time.fixedDeltaTime;
                Vector2 boxCenter = (Vector2)transform.position + new Vector2(facing * 0.45f, 0.96f);
                Collider2D[] cols = Physics2D.OverlapBoxAll(boxCenter, new Vector2(2.35f, 1.50f), 0f, enemyAndBreakableLayers);
                for (int i = 0; i < cols.Length; i++)
                {
                    Collider2D col = cols[i];
                    if (col == null) continue;
                    Hurtbox hb = col.GetComponent<Hurtbox>();
                    IDamageable target = hb != null ? hb.Owner : col.GetComponentInParent<IDamageable>();
                    if (target == null || !target.IsAlive || piercedTargets.Contains(target)) continue;

                    Vector3 hitPt = col.ClosestPoint(boxCenter);
                    DamageInfo blitzInfo = new DamageInfo(
                        pierceDmg,
                        new Vector2(facing, 0.18f).normalized,
                        4.2f,
                        0.26f,
                        gameObject,
                        hitPt,
                        false,
                        true);

                    bool dealt = hb != null ? hb.ReceiveHit(blitzInfo) : target.TakeDamage(blitzInfo);
                    if (dealt)
                    {
                        piercedTargets.Add(target);
                        if (!(target is BreakableProp))
                        {
                            delayedTargets.Add(target);
                            delayedPositions.Add(hitPt);
                            if (VFXManager.Instance != null) VFXManager.Instance.SpawnHitImpact(hitPt, Color.white, target is EnemyController, false);
                            if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyHit();
                        }
                    }
                }
                yield return new WaitForFixedUpdate();
            }

            Vector3 endCenter = transform.position + new Vector3(0f, 0.96f, 0f);
            ChoralCodeVFX.SpawnSharpdartTrail(startCenter, endCenter, facing);
            GameEvents.RaiseCameraShake(0.085f, 0.10f);

            // Restore movement & gravity cleanly (GameManager preserves layer pass-through globally)
            if (movement != null)
            {
                movement.SetAirSuspendLock(false);
                movement.SetCombatMovementLock(false);
            }

            // Fire delayed silk-slash detonation on every pierced target asynchronously so player recovers snappily!
            if (delayedTargets.Count > 0)
            {
                StartCoroutine(ExecuteSharpdartAfterSlashRoutine(delayedTargets, delayedPositions, afterSlashDmg, facing));
            }

            yield return new WaitForSeconds(0.06f);
            _isSharpdartActive = false;
            _isAttacking = false;
            _isAirAttacking = false;
            _attackCooldownTimer = 0.10f;
            _attackCoroutine = null;
        }

        private IEnumerator ExecuteSharpdartAfterSlashRoutine(
            System.Collections.Generic.List<IDamageable> targets,
            System.Collections.Generic.List<Vector3> fallbackPositions,
            int afterDmg,
            int facing)
        {
            yield return new WaitForSeconds(0.14f);
            bool anyBurst = false;
            for (int i = 0; i < targets.Count; i++)
            {
                IDamageable target = targets[i];
                if (target == null || !target.IsAlive) continue;
                MonoBehaviour mb = target as MonoBehaviour;
                Vector3 pos = mb != null ? (mb.transform.position + Vector3.up * 0.90f) : fallbackPositions[i];

                ChoralCodeVFX.SpawnSharpdartAfterSlash(pos);
                DamageInfo afterInfo = new DamageInfo(
                    afterDmg,
                    new Vector2(facing, 0.35f).normalized,
                    7.8f,
                    0.28f,
                    gameObject,
                    pos,
                    false,
                    true);
                target.TakeDamage(afterInfo);
                anyBurst = true;
            }
            if (anyBurst)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayAttackSlash(3);
                    AudioManager.Instance.PlayArmorClink(0.65f);
                }
                GameEvents.RaiseCameraShake(0.075f, 0.085f);
            }
        }

        // =========================================================================
        // 7. PALE NAILS — SUMMONS 3 HOMING PALE SILK CLAWS (C / MMB, 00:30:02)
        // =========================================================================
        private void TryCastPaleNails()
        {
            if (skillManager == null || !skillManager.TryConsumeSkill(SkillType.SilkSpear, stats))
            {
                return;
            }
            _attackCoroutine = StartCoroutine(ExecutePaleNailsRoutine());
        }

        private IEnumerator ExecutePaleNailsRoutine()
        {
            _isAttacking = true;
            _isAirAttacking = movement != null && !movement.IsGrounded;
            int facing = movement != null ? movement.FacingDirection : 1;

            if (OnAttackStarted != null) OnAttackStarted(9, _isAirAttacking);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayThreadGuard();
                AudioManager.Instance.PlaySilkSpear();
            }

            yield return new WaitForSeconds(0.055f);

            int nailDmg = Mathf.Max(6, Mathf.RoundToInt(ComputeMeleeDamage(null) * 0.78f));
            LayerMask wallMask = 1 << GameConstants.LayerGround;
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(-facing * 0.85f, 1.48f, 0f),
                new Vector3(-facing * 0.52f, 2.02f, 0f),
                new Vector3(-facing * 0.14f, 2.34f, 0f),
            };
            float[] delays = new float[] { 0.12f, 0.22f, 0.32f };

            for (int i = 0; i < offsets.Length; i++)
            {
                PaleNailProjectile.Spawn(
                    transform,
                    offsets[i],
                    facing,
                    nailDmg,
                    delays[i],
                    i,
                    enemyAndBreakableLayers,
                    wallMask);
            }

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSkillBurst(transform.position + new Vector3(-facing * 0.45f, 1.85f, 0f), new Color(0.84f, 0.98f, 1.0f, 1f));
            }
            GameEvents.RaiseCameraShake(0.045f, 0.065f);

            yield return new WaitForSeconds(0.14f);
            _isAttacking = false;
            _isAirAttacking = false;
            _attackCooldownTimer = 0.08f;
            _attackCoroutine = null;
        }

        // =========================================================================
        // 8. RUNE RAGE — DELAYED MULTI-RUNE AOE DETONATION (Down + Q/F, 00:29:28)
        // =========================================================================
        private void TryCastRuneRage()
        {
            if (skillManager == null || !skillManager.TryConsumeSkill(SkillType.SilkSpear, stats))
            {
                return;
            }
            _attackCoroutine = StartCoroutine(ExecuteRuneRageRoutine());
        }

        private IEnumerator ExecuteRuneRageRoutine()
        {
            _isAttacking = true;
            _isAirAttacking = movement != null && !movement.IsGrounded;
            int facing = movement != null ? movement.FacingDirection : 1;

            if (movement != null)
            {
                movement.SetCombatMovementLock(true);
                if (!movement.IsGrounded) movement.SetAirSuspendLock(true);
            }

            if (OnAttackStarted != null) OnAttackStarted(10, _isAirAttacking);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBindCharge();
                AudioManager.Instance.PlayThreadGuard();
            }

            yield return new WaitForSeconds(0.09f);

            // Gather up to 3 distinct alive enemies within 10.5u, then fill up to 4 rune seals!
            System.Collections.Generic.List<Vector3> sealPositions = new System.Collections.Generic.List<Vector3>();
            System.Collections.Generic.HashSet<EnemyController> chosenEnemies = new System.Collections.Generic.HashSet<EnemyController>();
            Collider2D[] nearCols = Physics2D.OverlapCircleAll(transform.position + Vector3.up * 0.95f, 10.5f, 1 << GameConstants.LayerEnemy);
            for (int i = 0; i < nearCols.Length && sealPositions.Count < 3; i++)
            {
                if (nearCols[i] == null) continue;
                EnemyController ec = nearCols[i].GetComponentInParent<EnemyController>();
                if (ec == null || !ec.IsAlive || chosenEnemies.Contains(ec)) continue;
                chosenEnemies.Add(ec);
                sealPositions.Add(ec.transform.position + Vector3.up * 0.85f);
            }

            Vector3[] fallbackOffsets = new Vector3[]
            {
                new Vector3(facing * 2.55f, 0.92f, 0f),
                new Vector3(-facing * 2.35f, 0.92f, 0f),
                new Vector3(facing * 4.65f, 1.15f, 0f),
                new Vector3(0f, 2.35f, 0f),
            };
            for (int f = 0; f < fallbackOffsets.Length && sealPositions.Count < 4; f++)
            {
                sealPositions.Add(transform.position + fallbackOffsets[f]);
            }

            float blastRadius = 1.85f;
            float telegraphDur = 0.34f;
            for (int i = 0; i < sealPositions.Count; i++)
            {
                ChoralCodeVFX.SpawnRuneRageSeal(sealPositions[i], blastRadius, telegraphDur + i * 0.075f);
            }

            int runeDmg = Mathf.RoundToInt(ComputeMeleeDamage(null) * 1.32f);
            StartCoroutine(ExecuteRuneDetonationsRoutine(sealPositions, blastRadius, telegraphDur, runeDmg));

            yield return new WaitForSeconds(0.18f);

            if (movement != null)
            {
                movement.SetCombatMovementLock(false);
                movement.SetAirSuspendLock(false);
            }
            _isAttacking = false;
            _isAirAttacking = false;
            _attackCooldownTimer = 0.14f;
            _attackCoroutine = null;
        }

        private IEnumerator ExecuteRuneDetonationsRoutine(
            System.Collections.Generic.List<Vector3> positions,
            float blastRadius,
            float initialDelay,
            int blastDamage)
        {
            yield return new WaitForSeconds(initialDelay);

            for (int r = 0; r < positions.Count; r++)
            {
                Vector3 center = positions[r];
                ChoralCodeVFX.SpawnRuneRageDetonation(center, blastRadius);
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnSkillBurst(center, new Color(1.0f, 0.94f, 0.72f, 1f));
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayBindBurst();
                    AudioManager.Instance.PlayAttackSlash(3);
                }
                GameEvents.RaiseCameraShake(0.095f, 0.095f);

                Collider2D[] hits = Physics2D.OverlapCircleAll(center, blastRadius, enemyAndBreakableLayers);
                System.Collections.Generic.HashSet<IDamageable> damagedThisBlast = new System.Collections.Generic.HashSet<IDamageable>();
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D col = hits[i];
                    if (col == null) continue;
                    Hurtbox hb = col.GetComponent<Hurtbox>();
                    IDamageable target = hb != null ? hb.Owner : col.GetComponentInParent<IDamageable>();
                    if (target == null || !target.IsAlive || damagedThisBlast.Contains(target)) continue;

                    Vector3 hitPt = col.ClosestPoint(center);
                    Vector2 outDir = (Vector2)(hitPt - center);
                    if (outDir.sqrMagnitude < 0.01f) outDir = new Vector2(0f, 1f);
                    outDir.y = Mathf.Max(0.45f, outDir.y);

                    DamageInfo blastInfo = new DamageInfo(
                        blastDamage,
                        outDir.normalized,
                        8.8f,
                        0.32f,
                        gameObject,
                        hitPt,
                        false,
                        true);

                    bool dealt = hb != null ? hb.ReceiveHit(blastInfo) : target.TakeDamage(blastInfo);
                    if (dealt)
                    {
                        damagedThisBlast.Add(target);
                        if (VFXManager.Instance != null && !(target is BreakableProp))
                        {
                            VFXManager.Instance.SpawnHitImpact(hitPt, new Color(1.0f, 0.95f, 0.75f, 1f), target is EnemyController, false);
                        }
                    }
                }

                yield return new WaitForSeconds(0.075f);
            }
        }

        // =========================================================================
        // 9. TAUNT / GARAMA! — SONIC CHALLENGE FLOURISH & AGGRO PULL (T / G, 00:07:46)
        // =========================================================================
        private IEnumerator ExecuteTauntRoutine()
        {
            _isTaunting = true;
            _tauntElapsed = 0f;
            _tauntCooldownTimer = 2.6f;

            int facing = movement != null ? movement.FacingDirection : 1;
            if (movement != null) movement.SetCombatMovementLock(true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayArmorClink(0.95f);
                AudioManager.Instance.PlayBindCharge();
            }

            Vector3 ringOrigin = transform.position + new Vector3(facing * 0.28f, 1.48f, 0f);
            ChoralCodeVFX.SpawnTauntVoiceRing(ringOrigin, facing);
            ChoralCodeVFX.SpawnTelegraphGlint(transform.position + new Vector3(facing * 0.72f, 1.88f, 0f), new Color(1.0f, 0.94f, 0.68f, 0.95f), 0.56f, 0.22f);
            GameEvents.RaiseCameraShake(0.032f, 0.065f);

            // Aggro / draw nearby enemies within 13.5u & reward +8 Silk if at least 1 enemy is provoked!
            int provokedCount = 0;
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position + Vector3.up * 0.95f, 13.5f, 1 << GameConstants.LayerEnemy);
            System.Collections.Generic.HashSet<EnemyController> seen = new System.Collections.Generic.HashSet<EnemyController>();
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null) continue;
                EnemyController ec = cols[i].GetComponentInParent<EnemyController>();
                if (ec == null || !ec.IsAlive || seen.Contains(ec)) continue;
                seen.Add(ec);
                ec.ReceiveTauntProvoke(transform.position, gameObject);
                provokedCount++;
            }

            if (provokedCount > 0 && stats != null)
            {
                stats.AddSilk(8f);
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnSkillBurst(transform.position + Vector3.up * 1.15f, new Color(1.0f, 0.95f, 0.72f, 1f));
                }
            }

            yield return new WaitForSeconds(0.40f);

            if (movement != null) movement.SetCombatMovementLock(false);
            _isTaunting = false;
            _tauntElapsed = 0f;
            _tauntCoroutine = null;
        }

        // =========================================================================
        // 10. HARPOON / CLAWLINE — TETHERED NEEDLE ZIP-PULL & VAULT (L-Ctrl / Mouse3, 00:06:08)
        // =========================================================================
        private IEnumerator ExecuteHarpoonRoutine()
        {
            _isAttacking = true;
            _isHarpooning = true;
            _harpoonCooldownTimer = 0.55f;

            int facing = movement != null ? movement.FacingDirection : 1;
            if (inputHandler != null && Mathf.Abs(inputHandler.Horizontal) > 0.15f)
            {
                facing = inputHandler.Horizontal > 0f ? 1 : -1;
                if (movement != null) movement.SetFacing(facing);
            }

            if (movement != null)
            {
                movement.SetCombatMovementLock(true);
                movement.SetAirSuspendLock(true);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySilkSpear();
                AudioManager.Instance.PlayAttackSlash(1);
            }

            Vector2 castOrigin = (Vector2)transform.position + new Vector2(facing * 0.45f, 0.98f);
            Vector2 castDir = new Vector2(facing, 0f);
            float maxRange = 9.5f;

            // Priority 1: Check for an enemy or breakable along the forgiving forward corridor!
            RaycastHit2D targetHit = Physics2D.CircleCast(castOrigin, 0.56f, castDir, maxRange, enemyAndBreakableLayers);
            IDamageable hookedDamageable = null;
            Hurtbox hookedHurtbox = null;
            Transform hookedTransform = null;
            bool hookedSolidWall = false;
            Vector3 anchorWorld = (Vector3)(castOrigin + castDir * 6.5f);

            if (targetHit.collider != null)
            {
                Hurtbox hb = targetHit.collider.GetComponent<Hurtbox>();
                IDamageable dmg = hb != null ? hb.Owner : targetHit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    hookedDamageable = dmg;
                    hookedHurtbox = hb;
                    hookedTransform = targetHit.collider.transform;
                    anchorWorld = new Vector3(targetHit.point.x, targetHit.point.y, 0f);
                }
            }

            // Priority 2: If no live enemy hooked, check for solid walls / cliffs / platforms to grapple-zip to!
            if (hookedDamageable == null)
            {
                int terrainMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerOneWayPlatform);
                RaycastHit2D wallHit = Physics2D.Raycast(castOrigin, castDir, maxRange, terrainMask);
                if (wallHit.collider != null && wallHit.distance >= 1.15f)
                {
                    hookedSolidWall = true;
                    anchorWorld = new Vector3(wallHit.point.x, wallHit.point.y, 0f);
                }
            }

            CleanupHarpoonCord();
            _activeHarpoonCord = ChoralCodeVFX.CreateLiveHarpoonCord(castOrigin);

            // Phase 1: Shoot the tethered silk needle rapidly outward (0.08s)
            float extendDur = 0.08f;
            float t = 0f;
            while (t < extendDur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / extendDur);
                Vector3 handPos = transform.position + new Vector3(facing * 0.48f, 0.98f, 0f);
                Vector3 liveAnchor = hookedTransform != null ? (hookedTransform.position + Vector3.up * 0.85f) : anchorWorld;
                if (_activeHarpoonCord != null)
                {
                    _activeHarpoonCord.SetPosition(0, handPos);
                    _activeHarpoonCord.SetPosition(1, Vector3.Lerp(handPos, liveAnchor, u));
                }
                yield return null;
            }

            if (hookedDamageable != null || hookedSolidWall)
            {
                Vector3 latchPos = hookedTransform != null ? (hookedTransform.position + Vector3.up * 0.85f) : anchorWorld;
                ChoralCodeVFX.SpawnTelegraphGlint(latchPos, new Color(0.78f, 0.98f, 1.0f, 0.98f), 0.58f, 0.16f);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayArmorClink(0.86f);
                    AudioManager.Instance.PlayDash();
                }

                // Phase 2: Zip-Pull player rapidly along the taut silk cord!
                float zipSpeed = 29.5f;
                float zipTimeout = 0.30f;
                float zipElapsed = 0f;
                while (zipElapsed < zipTimeout)
                {
                    zipElapsed += Time.fixedDeltaTime;
                    Vector3 curAnchor = hookedTransform != null ? (hookedTransform.position + Vector3.up * 0.85f) : anchorWorld;
                    Vector3 handPos = transform.position + new Vector3(facing * 0.42f, 0.98f, 0f);
                    if (_activeHarpoonCord != null)
                    {
                        _activeHarpoonCord.SetPosition(0, handPos);
                        _activeHarpoonCord.SetPosition(1, curAnchor);
                    }

                    Vector2 toTarget = (Vector2)(curAnchor - (transform.position + Vector3.up * 0.95f));
                    float dist = toTarget.magnitude;
                    float stopDist = hookedSolidWall ? 0.92f : 1.25f;
                    if (dist <= stopDist)
                    {
                        break;
                    }

                    if (movement != null)
                    {
                        movement.SetExternalVelocityOverride(true, toTarget.normalized * zipSpeed);
                    }
                    yield return new WaitForFixedUpdate();
                }

                CleanupHarpoonCord();
                if (movement != null)
                {
                    movement.SetExternalVelocityOverride(false, Vector2.zero);
                    movement.SetAirSuspendLock(false);
                    movement.SetCombatMovementLock(false);
                }

                // Phase 3: Arrival strike + acrobatic Pogo Vault (enemy) or Ledge/Wall Pop (wall)!
                if (hookedDamageable != null && hookedDamageable.IsAlive)
                {
                    Vector3 hitPos = hookedTransform != null ? (hookedTransform.position + Vector3.up * 0.85f) : anchorWorld;
                    int harpoonDmg = Mathf.Max(6, Mathf.RoundToInt(ComputeMeleeDamage(null) * 0.85f));
                    DamageInfo hInfo = new DamageInfo(
                        harpoonDmg,
                        new Vector2(facing, 0.30f).normalized,
                        5.2f,
                        0.22f,
                        gameObject,
                        hitPos,
                        false,
                        false);
                    bool dealt = hookedHurtbox != null ? hookedHurtbox.ReceiveHit(hInfo) : hookedDamageable.TakeDamage(hInfo);
                    if (dealt)
                    {
                        if (stats != null && !(hookedDamageable is BreakableProp)) stats.AddSilk(6f);
                        if (VFXManager.Instance != null) VFXManager.Instance.SpawnHitImpact(hitPos, Color.white, hookedDamageable is EnemyController, false);
                        ChoralCodeVFX.SpawnSlashArc(hitPos, facing, 1.05f, 0.78f, 110f, 0.14f, Color.white, 0.14f);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyHit();
                        if (hitStopController != null) hitStopController.TriggerHitStop(0.036f);
                        GameEvents.RaiseCameraShake(0.055f, 0.075f);
                        // Reset cooldown on enemy grapple-vault so skilled players can chain aerial Harpoons!
                        _harpoonCooldownTimer = 0.16f;
                    }
                    if (movement != null) movement.ApplyPogoBounce();
                }
                else
                {
                    if (VFXManager.Instance != null) VFXManager.Instance.SpawnHitImpact(anchorWorld, Color.white, false, true);
                    if (movement != null) movement.ApplyTraversalVault(7.6f, true);
                }
            }
            else
            {
                // Miss: crisp taut retract back into sleeve!
                float retractDur = 0.11f;
                float rt = 0f;
                while (rt < retractDur)
                {
                    rt += Time.deltaTime;
                    float u = 1f - Mathf.Clamp01(rt / retractDur);
                    Vector3 handPos = transform.position + new Vector3(facing * 0.48f, 0.98f, 0f);
                    if (_activeHarpoonCord != null)
                    {
                        _activeHarpoonCord.SetPosition(0, handPos);
                        _activeHarpoonCord.SetPosition(1, Vector3.Lerp(handPos, anchorWorld, u));
                    }
                    yield return null;
                }
                CleanupHarpoonCord();
                if (movement != null)
                {
                    movement.SetAirSuspendLock(false);
                    movement.SetCombatMovementLock(false);
                }
            }

            _isHarpooning = false;
            _isAttacking = false;
            _attackCooldownTimer = 0.06f;
            _attackCoroutine = null;
        }

        // =========================================================================
        // 11. SILKSOAR — EXPLOSIVE VERTICAL SILK ROCKET LAUNCH (L-Alt / Y, 00:07:24)
        // =========================================================================
        private IEnumerator ExecuteSilksoarRoutine()
        {
            _isAttacking = true;
            _isSilksoaring = true;
            _silksoarCooldownTimer = 1.35f;
            if (movement != null && !movement.IsGrounded) _hasUsedAirSilksoar = true;

            if (movement != null)
            {
                movement.SetCombatMovementLock(true);
                movement.SetAirSuspendLock(true);
            }

            // Phase 1: Coiled Silk Spiral Charge (0.16s)
            ChoralCodeVFX.SpawnSilksoarCharge(transform.position);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBindCharge();
            yield return new WaitForSeconds(0.16f);

            // Phase 2: Vertical Rocket Ascent (0.36s @ 25.5 u/s) with upward piercing hitbox!
            ChoralCodeVFX.SpawnSilksoarWake(transform, 0.38f);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnDoubleJumpRing(transform.position);
                VFXManager.Instance.SpawnSkillBurst(transform.position + Vector3.up * 0.65f, new Color(0.78f, 0.98f, 1.0f, 1f));
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySilkSpear();
                AudioManager.Instance.PlayDash();
            }
            GameEvents.RaiseCameraShake(0.085f, 0.11f);

            int soarDmg = Mathf.RoundToInt(ComputeMeleeDamage(null) * 1.10f);
            System.Collections.Generic.HashSet<IDamageable> hitOnAscent = new System.Collections.Generic.HashSet<IDamageable>();
            int solidCeilingMask = 1 << GameConstants.LayerGround;

            float launchDur = 0.36f;
            float elapsed = 0f;
            while (elapsed < launchDur)
            {
                elapsed += Time.fixedDeltaTime;

                // Stop cleanly if player reaches a solid ceiling overhead or explicitly cancels after 0.12s
                RaycastHit2D ceilHit = Physics2D.Raycast((Vector2)transform.position + new Vector2(0f, 1.85f), Vector2.up, 0.55f, solidCeilingMask);
                bool playerCancel = elapsed >= 0.12f && inputHandler != null && (inputHandler.DashPressed || inputHandler.HasBufferedJump);
                if (ceilHit.collider != null || playerCancel)
                {
                    if (ceilHit.collider != null)
                    {
                        if (VFXManager.Instance != null) VFXManager.Instance.SpawnHitImpact(ceilHit.point, Color.white, false, true);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayLand();
                    }
                    break;
                }

                float steerX = inputHandler != null ? inputHandler.Horizontal * 3.8f : 0f;
                if (movement != null)
                {
                    movement.SetExternalVelocityOverride(true, new Vector2(steerX, 25.5f));
                }

                // Pierce enemies & breakables directly overhead!
                Vector2 boxCenter = (Vector2)transform.position + new Vector2(0f, 1.45f);
                Collider2D[] cols = Physics2D.OverlapBoxAll(boxCenter, new Vector2(1.75f, 2.05f), 0f, enemyAndBreakableLayers);
                for (int i = 0; i < cols.Length; i++)
                {
                    Collider2D col = cols[i];
                    if (col == null) continue;
                    Hurtbox hb = col.GetComponent<Hurtbox>();
                    IDamageable target = hb != null ? hb.Owner : col.GetComponentInParent<IDamageable>();
                    if (target == null || !target.IsAlive || hitOnAscent.Contains(target)) continue;

                    Vector3 hitPt = col.ClosestPoint(boxCenter);
                    DamageInfo soarInfo = new DamageInfo(
                        soarDmg,
                        new Vector2(col.transform.position.x >= transform.position.x ? 0.35f : -0.35f, 0.94f).normalized,
                        8.2f,
                        0.28f,
                        gameObject,
                        hitPt,
                        false,
                        true);
                    bool dealt = hb != null ? hb.ReceiveHit(soarInfo) : target.TakeDamage(soarInfo);
                    if (dealt)
                    {
                        hitOnAscent.Add(target);
                        if (VFXManager.Instance != null && !(target is BreakableProp))
                        {
                            VFXManager.Instance.SpawnHitImpact(hitPt, Color.white, target is EnemyController, false);
                        }
                        if (AudioManager.Instance != null && !(target is BreakableProp)) AudioManager.Instance.PlayEnemyHit();
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            // Phase 3: Smooth buoyant apex release (refreshes Air Dash so player can chain Glide or Air Dash!)
            if (movement != null)
            {
                movement.SetAirSuspendLock(false);
                movement.SetCombatMovementLock(false);
                movement.ApplyTraversalVault(7.2f, true);
            }

            _isSilksoaring = false;
            _isAttacking = false;
            _attackCooldownTimer = 0.08f;
            _attackCoroutine = null;
        }

        // =========================================================================
        // 12. NEEDOLIN — SILK-VIOLIN RESONANCE MELODY (N, 00:06:29)
        // =========================================================================
        private IEnumerator ExecuteNeedolinRoutine()
        {
            _isPlayingNeedolin = true;
            _needolinElapsed = 0f;
            _needolinCooldownTimer = 0.75f;

            if (movement != null) movement.SetCombatMovementLock(true);

            int maxPulses = 7;
            int minPulses = 2;
            for (int p = 0; p < maxPulses; p++)
            {
                if (!_isPlayingNeedolin) yield break;

                Vector3 waveOrigin = transform.position + new Vector3(0f, 1.12f, 0f);
                ChoralCodeVFX.SpawnNeedolinWave(waveOrigin, p);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayNeedolinChord(p);

                // 1. Acoustic resonance: briefly entrances/dazes nearby enemies within 8.5u!
                Collider2D[] enemyCols = Physics2D.OverlapCircleAll(waveOrigin, 8.5f, 1 << GameConstants.LayerEnemy);
                System.Collections.Generic.HashSet<EnemyController> seen = new System.Collections.Generic.HashSet<EnemyController>();
                for (int i = 0; i < enemyCols.Length; i++)
                {
                    if (enemyCols[i] == null) continue;
                    EnemyController ec = enemyCols[i].GetComponentInParent<EnemyController>();
                    if (ec == null || !ec.IsAlive || seen.Contains(ec)) continue;
                    seen.Add(ec);
                    ec.ReceiveNeedolinResonance(0.75f);
                }

                // 2. Sanctuary harmony: playing near a Bench / Shrine or Upgrade Altar restores +4 Silk per chord!
                Collider2D[] interactCols = Physics2D.OverlapCircleAll(waveOrigin, 6.5f, 1 << GameConstants.LayerInteractable);
                bool nearSanctuary = false;
                for (int i = 0; i < interactCols.Length; i++)
                {
                    if (interactCols[i] == null) continue;
                    if (interactCols[i].GetComponentInParent<CheckpointShrine>() != null
                        || interactCols[i].GetComponentInParent<UpgradeAltar>() != null)
                    {
                        nearSanctuary = true;
                        break;
                    }
                }
                if (nearSanctuary && stats != null)
                {
                    stats.AddSilk(4f);
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.SpawnSkillBurst(waveOrigin, new Color(0.72f, 0.99f, 0.90f, 1f));
                    }
                }

                yield return new WaitForSeconds(0.42f);

                // After minimum 2-chord phrase, continue playing ONLY while the player holds Needolin!
                if (p + 1 >= minPulses && (inputHandler == null || !inputHandler.NeedolinHeld))
                {
                    break;
                }
            }

            if (movement != null) movement.SetCombatMovementLock(false);
            _isPlayingNeedolin = false;
            _needolinElapsed = 0f;
            _needolinCoroutine = null;
        }

        private void TriggerCustomHitbox(
            Vector2 localOffsetRight,
            Vector2 boxSize,
            int damage,
            Vector2 knockDir,
            float knockForce,
            float staggerDur,
            float hitStopDur,
            float silkGain,
            bool breaksGuard)
        {
            if (meleeHitbox == null) return;
            int facing = movement != null ? movement.FacingDirection : 1;
            Vector2 directedOffset = new Vector2(localOffsetRight.x * facing, localOffsetRight.y);
            Vector2 boxLocal = transform.localScale.x < 0f
                ? new Vector2(Mathf.Abs(localOffsetRight.x), localOffsetRight.y)
                : directedOffset;

            DamageInfo info = new DamageInfo(
                damage,
                knockDir,
                knockForce,
                staggerDur,
                gameObject,
                transform.position,
                false,
                breaksGuard);

            meleeHitbox.Activate(boxLocal, boxSize, info, delegate(IDamageable target, Vector3 hitPoint)
            {
                BreakableProp bp = target as BreakableProp;
                bool isProp = bp != null;
                if (stats != null && !isProp && silkGain > 0f)
                {
                    stats.AddSilk(silkGain);
                    if (PlayerCrestSystem.IsReaperHarvestActive)
                    {
                        stats.AddSilk(14f);
                        ChoralCodeVFX.SpawnReaperHarvestSpark(hitPoint, transform.position + Vector3.up * 0.95f);
                    }
                }
                EnemyController ec = target as EnemyController;
                bool isEnemy = ec != null;
                if (VFXManager.Instance != null && !isProp)
                {
                    VFXManager.Instance.SpawnHitImpact(hitPoint, Color.white, isEnemy, !isEnemy);
                }
                if (AudioManager.Instance != null && !isProp)
                {
                    AudioManager.Instance.PlayEnemyHit();
                    if (breaksGuard) AudioManager.Instance.PlayArmorClink(0.65f);
                }
                if (!isProp && hitStopDur > 0.01f && hitStopController != null)
                {
                    hitStopController.TriggerHitStop(Mathf.Clamp(hitStopDur, 0.030f, 0.060f));
                }
            });
        }
    }
}
