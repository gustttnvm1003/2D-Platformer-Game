using UnityEngine;

namespace SilkenGrotto
{
    public enum MountFayArchetype
    {
        ServitorBoran,
        Mnemonord,
        Mnemonid,
        Driftlin
    }

    /// <summary>
    /// Refactored 60 FPS Finite State Machine AI for Mount Fay (Batch 1) preserving all existing sprite assets:
    /// 1. Servitor Boran (Tracking Turret — 2-Damage Laser):
    ///    LanternScan (soft gradient lantern cone tracking player) -> LaserCharge (0.55s narrowing locked aim line + tremor)
    ///    -> LaserFire (0.35s bright straight 2-damage beam) -> BurstPause (0.60s, repeats 2-3 shots while player in range).
    /// 2. Driftlin (Stationary Aerial Pogo Pad):
    ///    StationaryFloat (calm anchor hover, passive) <-> PogoBounce (elastic 0.20s downward recoilY on downward pogo).
    ///    High HP + 3s auto-respawn + configurable 0/1 contact damage so parkour pogo flow never breaks.
    /// 3. Mnemonid (Small) & Mnemonord (Large) (Pogo Platform + Retaliation Spikes):
    ///    DormantFloat (safe to pogo on first hit) -> SpikeTriggered (0.12s grace delay so player bounces away)
    ///    -> SpikeActive (1.20s small / 1.50s large: 360-deg code-drawn spike ring, all-side metallic clang + 1 touch damage)
    ///    -> DormantFloat.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class MountFayEnemyAI : MonoBehaviour
    {
        [Header("Archetype")]
        [SerializeField] private MountFayArchetype archetype = MountFayArchetype.ServitorBoran;

        [Header("Legacy Sprite Hooks (Kept For Serialized Compatibility)")]
        [SerializeField] private Sprite lightWaveSprite;
        [SerializeField] private Sprite iceCrystalSprite;

        [Header("Procedural Motion Juice")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float hoverAmplitude = 0.24f;
        [SerializeField] private float hoverFrequency = 2.2f;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private EnemyStateMachine _fsm;
        private EnemyContactHazard _contactHazard;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private ENEMY_COMBAT_CONFIG.MountFayProfile _profile;

        private LayerMask _solidMask;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private Vector3 _anchorPosition;

        private float _animPhase;
        private float _cooldownTimer;
        private float _pogoRecoilTimer;
        private float _pogoRecoilDuration = 0.20f;
        private float _pogoRecoilOffsetY;

        // Servitor Boran lantern/laser state & pure code LineRenderer rig
        private float _lanternAngleRad;
        private Vector2 _lockedLaserDir = Vector2.right;
        private int _burstShotsFired;
        private float _laserChargeProgress;
        private bool _isLaserCharging;
        private bool _isLaserFiring;
        private bool _laserHitPlayerThisShot;
        private float _nextWallSparkTime;
        private GameObject _lanternRigRoot;
        private LineRenderer _lanternConeLine;
        private LineRenderer _laserCoreLine;
        private LineRenderer _lanternHaloLine;
        private SpriteSheetVfxPlayer _laserFlareVfx;
        private SpriteSheetVfxPlayer _laserBeamVfx;
        private float _lastLaserLength;

        // Mnemonid & Mnemonord retaliation spike ring state
        private GameObject _spikeRingObj;
        private SpriteSheetVfxPlayer _spikeSpriteVfx;
        private float _spikeBaseScale = 1.0f;
        private bool _isSpikeTelegraph;
        private bool _isSpikeActive;
        private float _spikeAnimProgress;
        private float _nextSpikeTouchDamageTime;

        // States
        private LanternScanState _lanternScanState;
        private LaserChargeState _laserChargeState;
        private LaserFireState _laserFireState;
        private BurstPauseState _burstPauseState;

        private StationaryFloatState _stationaryFloatState;
        private PogoBounceState _pogoBounceState;

        private DormantFloatState _dormantFloatState;
        private SpikeTriggeredState _spikeTriggeredState;
        private SpikeActiveState _spikeActiveState;

        private DeathState _deathState;

        public MountFayArchetype Archetype { get { return archetype; } }

        public void ConfigureArchetype(MountFayArchetype kind, Sprite waveSprite, Sprite crystalSprite)
        {
            archetype = kind;
            if (waveSprite != null) lightWaveSprite = waveSprite;
            if (crystalSprite != null) iceCrystalSprite = crystalSprite;
            ENEMY_COMBAT_CONFIG.MountFayProfile p = ENEMY_COMBAT_CONFIG.GetMountFay(kind);
            hoverAmplitude = p.HoverAmplitude;
            hoverFrequency = p.HoverFrequency;
        }

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _contactHazard = GetComponentInChildren<EnemyContactHazard>();
            _fsm = new EnemyStateMachine();
            _profile = ENEMY_COMBAT_CONFIG.GetMountFay(archetype);

            _solidMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerBreakableWall);
            _animPhase = (Mathf.Abs(transform.position.x) * 0.68f) % 6.28f;
            _anchorPosition = transform.position;

            if (visualTransform == null)
            {
                Transform v = transform.Find("Visual");
                visualTransform = v != null ? v : transform;
            }
            _baseVisualScale = visualTransform.localScale;
            _baseVisualScale.x = Mathf.Abs(_baseVisualScale.x);
            _baseVisualScale.y = Mathf.Abs(_baseVisualScale.y);
            _baseVisualLocalPos = visualTransform.localPosition;

            _lanternScanState = new LanternScanState(this);
            _laserChargeState = new LaserChargeState(this);
            _laserFireState = new LaserFireState(this);
            _burstPauseState = new BurstPauseState(this);

            _stationaryFloatState = new StationaryFloatState(this);
            _pogoBounceState = new PogoBounceState(this);

            _dormantFloatState = new DormantFloatState(this);
            _spikeTriggeredState = new SpikeTriggeredState(this);
            _spikeActiveState = new SpikeActiveState(this);

            _deathState = new DeathState(this);
        }

        private void OnEnable()
        {
            if (_enemy != null)
            {
                _enemy.OnDamaged += HandleDamaged;
                _enemy.OnDied += HandleDied;
                _enemy.OnRespawned += HandleRespawned;
            }
        }

        private void OnDisable()
        {
            if (_enemy != null)
            {
                _enemy.OnDamaged -= HandleDamaged;
                _enemy.OnDied -= HandleDied;
                _enemy.OnRespawned -= HandleRespawned;
            }
        }

        private void Start()
        {
            _profile = ENEMY_COMBAT_CONFIG.GetMountFay(archetype);
            _anchorPosition = transform.position;
            hoverAmplitude = _profile.HoverAmplitude;
            hoverFrequency = _profile.HoverFrequency;
            ResolvePlayer();

            if (_enemy != null && _profile.RespawnAfterSeconds > 0f)
            {
                _enemy.SetRespawnDelay(_profile.RespawnAfterSeconds);
            }
            if (_contactHazard != null)
            {
                _contactHazard.SetContactDamage(_profile.ContactDamage);
            }

            if (_profile.IsFlying && _rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.drag = 0f;
            }

            int initialDir = (Mathf.Abs(transform.position.x) % 2f) < 1f ? 1 : -1;
            _enemy.SetFacing(initialDir);
            _lanternAngleRad = initialDir >= 0 ? 0f : Mathf.PI;

            if (archetype == MountFayArchetype.ServitorBoran)
            {
                BuildLanternAndLaserRig();
                TransitionTo(_lanternScanState);
            }
            else if (archetype == MountFayArchetype.Driftlin)
            {
                TransitionTo(_stationaryFloatState);
            }
            else
            {
                BuildSpikeRingVisual();
                TransitionTo(_dormantFloatState);
            }
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_playerTransform == null) ResolvePlayer();
            float dt = Time.deltaTime;
            if (_cooldownTimer > 0f) _cooldownTimer -= dt;
            if (_pogoRecoilTimer > 0f) _pogoRecoilTimer -= dt;
            _animPhase += dt;

            _fsm.Tick(dt);
            ApplyProceduralVisualJuice();
        }

        private void FixedUpdate()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            _fsm.FixedTick(Time.fixedDeltaTime);
        }

        private void BuildLanternAndLaserRig()
        {
            if (_lanternRigRoot != null) return;
            _lanternRigRoot = new GameObject("ServitorBoran_LanternLaserRig");
            _lanternRigRoot.transform.SetParent(transform, false);
            Material mat = ChoralCodeVFX.GetLineMaterial();

            // Soft expanding lantern light cone / outer laser glow
            GameObject coneGo = new GameObject("LanternCone");
            coneGo.transform.SetParent(_lanternRigRoot.transform, false);
            _lanternConeLine = coneGo.AddComponent<LineRenderer>();
            _lanternConeLine.useWorldSpace = true;
            _lanternConeLine.sortingLayerName = GameConstants.SortVFX;
            _lanternConeLine.sortingOrder = 14;
            if (mat != null) _lanternConeLine.sharedMaterial = mat;
            _lanternConeLine.positionCount = 2;

            // Narrow aim sightline / blazing white laser core
            GameObject coreGo = new GameObject("LaserCore");
            coreGo.transform.SetParent(_lanternRigRoot.transform, false);
            _laserCoreLine = coreGo.AddComponent<LineRenderer>();
            _laserCoreLine.useWorldSpace = true;
            _laserCoreLine.sortingLayerName = GameConstants.SortVFX;
            _laserCoreLine.sortingOrder = 17;
            if (mat != null) _laserCoreLine.sharedMaterial = mat;
            _laserCoreLine.positionCount = 2;

            // Lantern emitter glow ring
            GameObject haloGo = new GameObject("LanternHalo");
            haloGo.transform.SetParent(_lanternRigRoot.transform, false);
            _lanternHaloLine = haloGo.AddComponent<LineRenderer>();
            _lanternHaloLine.useWorldSpace = false;
            _lanternHaloLine.sortingLayerName = GameConstants.SortVFX;
            _lanternHaloLine.sortingOrder = 18;
            if (mat != null) _lanternHaloLine.sharedMaterial = mat;
            _lanternHaloLine.positionCount = 13;
            for (int i = 0; i < 13; i++)
            {
                float a = (float)i / 12f * Mathf.PI * 2f;
                _lanternHaloLine.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.22f, Mathf.Sin(a) * 0.22f, 0f));
            }

            // Hand-drawn 2D cel-shaded LaserCharge flare (Row 1 F1-2) & stretchable LaserFire beam (Row 1 F3-5)
            Material spriteMat = VFXManager.Instance != null ? VFXManager.Instance.DefaultSpriteMaterial : null;

            GameObject flareGo = new GameObject("LaserChargeFlareVFX");
            flareGo.transform.SetParent(_lanternRigRoot.transform, false);
            _laserFlareVfx = flareGo.AddComponent<SpriteSheetVfxPlayer>();
            _laserFlareVfx.ConfigureSorting(ENEMY_VFX_CONFIG.ServitorLaserCharge.SortingOrder, spriteMat);
            flareGo.SetActive(false);

            GameObject beamGo = new GameObject("LaserFireBeamVFX");
            beamGo.transform.SetParent(_lanternRigRoot.transform, false);
            _laserBeamVfx = beamGo.AddComponent<SpriteSheetVfxPlayer>();
            _laserBeamVfx.ConfigureSorting(ENEMY_VFX_CONFIG.ServitorLaserBeam.SortingOrder, spriteMat);
            beamGo.SetActive(false);
        }

        private void BuildSpikeRingVisual()
        {
            if (_spikeRingObj != null) return;
            bool isLarge = archetype == MountFayArchetype.Mnemonord;
            _spikeBaseScale = isLarge ? ENEMY_VFX_CONFIG.MnemonordSpikeScale : ENEMY_VFX_CONFIG.MnemonidSpikeScale;

            // Prefer shared 2D hand-drawn ice spikes sprite sheet (Row 2) centered over body!
            Sprite[] activeFrames = VFXManager.Instance != null ? VFXManager.Instance.GetIceSpikesActiveFrames() : null;
            if (activeFrames != null && activeFrames.Length > 0)
            {
                _spikeRingObj = new GameObject("HandDrawnVFX_IceSpikesOverlay");
                _spikeRingObj.transform.SetParent(transform, false);
                _spikeRingObj.transform.localPosition = Vector3.zero;
                _spikeRingObj.transform.localScale = Vector3.one * _spikeBaseScale;
                _spikeSpriteVfx = _spikeRingObj.AddComponent<SpriteSheetVfxPlayer>();
                _spikeSpriteVfx.ConfigureSorting(
                    ENEMY_VFX_CONFIG.MnemonSpikesActive.SortingOrder,
                    VFXManager.Instance.DefaultSpriteMaterial);
                _spikeRingObj.SetActive(false);
                return;
            }

            Color baseCol = isLarge
                ? new Color(0.72f, 0.90f, 1.0f, 0.96f)
                : new Color(0.80f, 0.95f, 1.0f, 0.96f);
            Color tipCol = Color.white;
            _spikeRingObj = ChoralCodeVFX.CreateRetaliationSpikeRing(
                transform,
                _profile.SpikeCount,
                _profile.SpikeInnerRadius,
                _profile.SpikeOuterRadius,
                baseCol,
                tipCol);
        }

        private Vector3 GetLanternMuzzlePos()
        {
            int dir = _enemy != null ? _enemy.FacingDirection : 1;
            return transform.position + new Vector3(dir * 0.36f, 1.54f, 0f);
        }

        private float RaycastBeamLength(Vector3 origin, Vector2 dir, float maxLen, out Vector2 hitPoint, out Vector2 hitNormal, out bool hitTerrain)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxLen, _solidMask);
            if (hit.collider != null)
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;
                hitTerrain = true;
                return Mathf.Max(0.4f, hit.distance);
            }
            hitPoint = (Vector2)origin + dir * maxLen;
            hitNormal = -dir;
            hitTerrain = false;
            return maxLen;
        }

        private void SetLanternVisualsActive(bool active)
        {
            if (_lanternRigRoot != null) _lanternRigRoot.SetActive(active);
        }

        private void RenderLanternBeam(Vector2 dir, float maxLen, int mode, float progress)
        {
            if (_lanternRigRoot == null || _lanternConeLine == null || _laserCoreLine == null) return;
            _lanternRigRoot.SetActive(true);

            Vector3 muzzle = GetLanternMuzzlePos();
            Vector2 hitPt;
            Vector2 hitNorm;
            bool hitTerrain;
            float len = RaycastBeamLength(muzzle, dir, maxLen, out hitPt, out hitNorm, out hitTerrain);
            Vector3 endPos = muzzle + (Vector3)(dir * len);

            if (_lanternHaloLine != null)
            {
                _lanternHaloLine.transform.position = muzzle;
            }

            if (mode == 0)
            {
                // LanternScan: soft translucent lantern gradient cone
                _lanternConeLine.gameObject.SetActive(true);
                _laserCoreLine.gameObject.SetActive(true);
                if (_lanternHaloLine != null) _lanternHaloLine.gameObject.SetActive(true);
                if (_laserFlareVfx != null) _laserFlareVfx.gameObject.SetActive(false);
                if (_laserBeamVfx != null) _laserBeamVfx.gameObject.SetActive(false);

                _lanternConeLine.SetPosition(0, muzzle);
                _lanternConeLine.SetPosition(1, endPos);
                _lanternConeLine.startWidth = 0.24f;
                _lanternConeLine.endWidth = Mathf.Lerp(1.6f, 2.75f, len / Mathf.Max(1f, maxLen));
                _lanternConeLine.startColor = new Color(0.96f, 0.94f, 0.72f, 0.38f);
                _lanternConeLine.endColor = new Color(0.76f, 0.92f, 1.0f, 0.03f);

                // Very faint center ray
                _laserCoreLine.SetPosition(0, muzzle);
                _laserCoreLine.SetPosition(1, muzzle + (Vector3)(dir * (len * 0.55f)));
                _laserCoreLine.startWidth = 0.05f;
                _laserCoreLine.endWidth = 0.015f;
                _laserCoreLine.startColor = new Color(1.0f, 0.98f, 0.84f, 0.42f);
                _laserCoreLine.endColor = new Color(0.84f, 0.96f, 1.0f, 0.02f);

                if (_lanternHaloLine != null)
                {
                    _lanternHaloLine.startWidth = 0.04f;
                    _lanternHaloLine.endWidth = 0.04f;
                    Color h = new Color(1.0f, 0.94f, 0.68f, 0.52f);
                    _lanternHaloLine.startColor = h;
                    _lanternHaloLine.endColor = h;
                    _lanternHaloLine.transform.localScale = Vector3.one * (0.85f + Mathf.Sin(_animPhase * 3.5f) * 0.08f);
                }
            }
            else if (mode == 1)
            {
                // LaserCharge (0.55s): lantern cone narrows into crisp locked telegraph sightline + Row 1 F1-2 Charge Flare!
                float narrow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
                float pulse = 0.68f + 0.32f * Mathf.Sin(_animPhase * 42f);
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                _lanternConeLine.gameObject.SetActive(true);
                _laserCoreLine.gameObject.SetActive(true);
                if (_laserBeamVfx != null) _laserBeamVfx.gameObject.SetActive(false);

                _lanternConeLine.SetPosition(0, muzzle);
                _lanternConeLine.SetPosition(1, endPos);
                _lanternConeLine.startWidth = Mathf.Lerp(0.24f, 0.12f, narrow);
                _lanternConeLine.endWidth = Mathf.Lerp(2.10f, 0.14f, narrow);
                _lanternConeLine.startColor = new Color(1.0f, 0.86f, 0.58f, Mathf.Lerp(0.36f, 0.78f, narrow));
                _lanternConeLine.endColor = new Color(0.76f, 0.95f, 1.0f, Mathf.Lerp(0.10f, 0.52f, narrow));

                _laserCoreLine.SetPosition(0, muzzle);
                _laserCoreLine.SetPosition(1, endPos);
                _laserCoreLine.startWidth = Mathf.Lerp(0.035f, 0.095f, narrow) * pulse;
                _laserCoreLine.endWidth = Mathf.Lerp(0.02f, 0.075f, narrow) * pulse;
                Color sightCol = Color.Lerp(new Color(0.95f, 0.98f, 1.0f, 0.82f), Color.white, narrow);
                _laserCoreLine.startColor = sightCol;
                _laserCoreLine.endColor = new Color(sightCol.r, sightCol.g, sightCol.b, 0.62f);

                Sprite[] chargeFrames = VFXManager.Instance != null ? VFXManager.Instance.GetLaserChargeFrames() : null;
                if (_laserFlareVfx != null && chargeFrames != null && chargeFrames.Length > 0)
                {
                    if (_lanternHaloLine != null) _lanternHaloLine.gameObject.SetActive(false);
                    _laserFlareVfx.gameObject.SetActive(true);
                    _laserFlareVfx.transform.position = muzzle;
                    _laserFlareVfx.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
                    float flareScale = ENEMY_VFX_CONFIG.ServitorLaserCharge.DefaultScale * Mathf.Lerp(0.55f, 1.18f, narrow);
                    _laserFlareVfx.transform.localScale = Vector3.one * flareScale;
                    _laserFlareVfx.Play(chargeFrames, ENEMY_VFX_CONFIG.ServitorLaserCharge.Fps, true, false);
                }
                else if (_lanternHaloLine != null)
                {
                    _lanternHaloLine.gameObject.SetActive(true);
                    _lanternHaloLine.startWidth = 0.065f;
                    _lanternHaloLine.endWidth = 0.065f;
                    Color h = Color.Lerp(new Color(1.0f, 0.86f, 0.54f, 0.85f), Color.white, narrow);
                    _lanternHaloLine.startColor = h;
                    _lanternHaloLine.endColor = h;
                    _lanternHaloLine.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 0.65f, narrow);
                }
            }
            else
            {
                // LaserFire (0.35s): blazing 2-damage straight laser beam using Row 1 F3-5 stretched to exact raycast len!
                float flicker = 0.90f + 0.14f * Mathf.Sin(_animPhase * 85f);
                float fadeOut = progress > 0.78f ? Mathf.Lerp(1f, 0.25f, (progress - 0.78f) / 0.22f) : 1f;
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _lastLaserLength = len;

                Sprite[] beamFrames = VFXManager.Instance != null ? VFXManager.Instance.GetLaserBeamFrames() : null;
                if (_laserBeamVfx != null && beamFrames != null && beamFrames.Length > 0)
                {
                    // Replace temporary fallback LineRenderer laser with crisp 2D hand-drawn cel-shaded beam!
                    _lanternConeLine.gameObject.SetActive(false);
                    _laserCoreLine.gameObject.SetActive(false);
                    if (_lanternHaloLine != null) _lanternHaloLine.gameObject.SetActive(false);

                    _laserBeamVfx.gameObject.SetActive(true);
                    _laserBeamVfx.transform.position = muzzle + (Vector3)(dir * (len * 0.5f));
                    _laserBeamVfx.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
                    float sx = Mathf.Max(0.25f, len / ENEMY_VFX_CONFIG.CellWorldSize);
                    float sy = ENEMY_VFX_CONFIG.ServitorLaserBeam.DefaultScale * flicker * fadeOut;
                    _laserBeamVfx.transform.localScale = new Vector3(sx, sy, 1f);
                    _laserBeamVfx.SetTint(new Color(1f, 1f, 1f, fadeOut));
                    _laserBeamVfx.Play(beamFrames, ENEMY_VFX_CONFIG.ServitorLaserBeam.Fps, true, false);

                    Sprite[] flareFrames = VFXManager.Instance.GetLaserChargeFrames();
                    if (_laserFlareVfx != null && flareFrames != null && flareFrames.Length > 0)
                    {
                        _laserFlareVfx.gameObject.SetActive(true);
                        _laserFlareVfx.transform.position = muzzle;
                        _laserFlareVfx.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
                        _laserFlareVfx.transform.localScale = Vector3.one * (ENEMY_VFX_CONFIG.ServitorLaserCharge.DefaultScale * 1.15f * flicker);
                        _laserFlareVfx.Play(flareFrames, ENEMY_VFX_CONFIG.ServitorLaserCharge.Fps, true, false);
                    }
                }
                else
                {
                    _lanternConeLine.gameObject.SetActive(true);
                    _laserCoreLine.gameObject.SetActive(true);
                    _lanternConeLine.SetPosition(0, muzzle);
                    _lanternConeLine.SetPosition(1, endPos);
                    _lanternConeLine.startWidth = 0.92f * flicker * fadeOut;
                    _lanternConeLine.endWidth = 0.78f * flicker * fadeOut;
                    _lanternConeLine.startColor = new Color(0.78f, 0.95f, 1.0f, 0.78f * fadeOut);
                    _lanternConeLine.endColor = new Color(1.0f, 0.92f, 0.68f, 0.64f * fadeOut);

                    _laserCoreLine.SetPosition(0, muzzle);
                    _laserCoreLine.SetPosition(1, endPos);
                    _laserCoreLine.startWidth = 0.36f * flicker * fadeOut;
                    _laserCoreLine.endWidth = 0.30f * flicker * fadeOut;
                    _laserCoreLine.startColor = new Color(1f, 1f, 1f, fadeOut);
                    _laserCoreLine.endColor = new Color(0.96f, 0.99f, 1f, fadeOut);

                    if (_lanternHaloLine != null)
                    {
                        _lanternHaloLine.gameObject.SetActive(true);
                        _lanternHaloLine.startWidth = 0.11f;
                        _lanternHaloLine.endWidth = 0.11f;
                        _lanternHaloLine.startColor = Color.white;
                        _lanternHaloLine.endColor = new Color(0.82f, 0.96f, 1f, fadeOut);
                        _lanternHaloLine.transform.localScale = Vector3.one * (1.65f * flicker);
                    }
                }

                if (hitTerrain && Time.time >= _nextWallSparkTime)
                {
                    _nextWallSparkTime = Time.time + 0.085f;
                    ChoralCodeVFX.SpawnLaserImpactSparks(hitPt, hitNorm);
                }
            }
        }

        private void CheckLaserPlayerHit(Vector2 dir, float maxLen)
        {
            if (_laserHitPlayerThisShot || !IsPlayerAlive()) return;
            Vector3 muzzle = GetLanternMuzzlePos();
            Vector2 hitPt;
            Vector2 hitNorm;
            bool hitTerrain;
            float beamLen = RaycastBeamLength(muzzle, dir, maxLen, out hitPt, out hitNorm, out hitTerrain);

            int playerMask = 1 << GameConstants.LayerPlayer;
            RaycastHit2D pHit = Physics2D.CircleCast(muzzle, 0.28f, dir, beamLen, playerMask);
            if (pHit.collider != null)
            {
                PlayerController pc = pHit.collider.GetComponentInParent<PlayerController>();
                if (pc != null && pc.IsAlive && !pc.IsInvulnerable)
                {
                    _laserHitPlayerThisShot = true;
                    Vector2 kbDir = new Vector2(dir.x >= 0f ? 1f : -1f, 0.35f).normalized;
                    DamageInfo info = new DamageInfo(
                        _profile.LaserDamage, // 2 Damage Laser!
                        kbDir,
                        _profile.LaserKnockback,
                        0.26f,
                        gameObject,
                        pHit.point,
                        false);
                    pc.TakeDamage(info);
                    ChoralCodeVFX.SpawnLaserImpactSparks(pHit.point, -dir);
                    GameEvents.RaiseCameraShake(0.068f, 0.10f);
                }
            }
        }

        private void ResolvePlayer()
        {
            GameObject pObj = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
            if (pObj != null)
            {
                _playerTransform = pObj.transform;
                _playerController = pObj.GetComponent<PlayerController>();
            }
        }

        private void TransitionTo(IEnemyState next)
        {
            _fsm.ChangeState(next);
            if (_enemy != null && next != null)
            {
                _enemy.SetAIState(next.StateType);
            }
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (!_enemy.IsAlive) return;

            if (archetype == MountFayArchetype.ServitorBoran)
            {
                // Knockback-resistant pressure turret: never interrupts its lantern/laser burst cycle!
                return;
            }

            if (archetype == MountFayArchetype.Driftlin)
            {
                _pogoRecoilDuration = _profile.PogoRecoilDuration > 0.05f ? _profile.PogoRecoilDuration : 0.20f;
                _pogoRecoilOffsetY = _profile.PogoRecoilOffsetY > 0.05f ? _profile.PogoRecoilOffsetY : 0.58f;
                _pogoRecoilTimer = _pogoRecoilDuration;

                if (info.IsDownwardPogo)
                {
                    ChoralCodeVFX.SpawnMossPogoBurst(transform.position + new Vector3(0f, 0.45f, 0f), new Color(0.84f, 0.96f, 1.0f, 0.96f));
                }
                TransitionTo(_pogoBounceState);
                return;
            }

            // Mnemonid & Mnemonord: safe pogo on hit #1 -> brief 0.12s delay -> SpikeActive!
            _pogoRecoilDuration = _profile.PogoRecoilDuration > 0.05f ? _profile.PogoRecoilDuration : 0.18f;
            _pogoRecoilOffsetY = _profile.PogoRecoilOffsetY > 0.05f ? _profile.PogoRecoilOffsetY : 0.40f;
            _pogoRecoilTimer = _pogoRecoilDuration;

            if (info.IsDownwardPogo)
            {
                ChoralCodeVFX.SpawnMossPogoBurst(
                    transform.position + new Vector3(0f, archetype == MountFayArchetype.Mnemonord ? 0.85f : 0.45f, 0f),
                    new Color(0.76f, 0.94f, 1.0f, 0.96f));
            }

            if (_fsm.CurrentState == _dormantFloatState)
            {
                TransitionTo(_spikeTriggeredState);
            }
        }

        private void HandleDied()
        {
            if ((archetype == MountFayArchetype.Mnemonid || archetype == MountFayArchetype.Mnemonord) && VFXManager.Instance != null)
            {
                // Shatter ice crystal burst (Row 2 F5-6) when Mnemonid / Mnemonord is defeated!
                VFXManager.Instance.SpawnIceSpikeShatter(transform.position, _spikeBaseScale);
            }
            _isLaserCharging = false;
            _isLaserFiring = false;
            _isSpikeTelegraph = false;
            _isSpikeActive = false;
            SetLanternVisualsActive(false);
            if (_spikeRingObj != null) _spikeRingObj.SetActive(false);
            if (_enemy != null) _enemy.SetAllSideSpikeShield(false);

            TransitionTo(_deathState);
            if (visualTransform != null)
            {
                visualTransform.localRotation = Quaternion.identity;
            }
        }

        private void HandleRespawned()
        {
            _anchorPosition = _enemy != null ? _enemy.SpawnPosition : transform.position;
            _pogoRecoilTimer = 0f;
            _isSpikeTelegraph = false;
            _isSpikeActive = false;
            if (_spikeRingObj != null) _spikeRingObj.SetActive(false);
            if (_enemy != null) _enemy.SetAllSideSpikeShield(false);
            if (_contactHazard != null) _contactHazard.SetContactDamage(_profile.ContactDamage);

            if (archetype == MountFayArchetype.Driftlin)
            {
                TransitionTo(_stationaryFloatState);
            }
            else if (archetype == MountFayArchetype.Mnemonid || archetype == MountFayArchetype.Mnemonord)
            {
                TransitionTo(_dormantFloatState);
            }
            else
            {
                TransitionTo(_lanternScanState);
            }
        }

        private bool IsPlayerAlive()
        {
            return _playerTransform != null && (_playerController == null || _playerController.IsAlive);
        }

        private Vector2 PlayerChestPos()
        {
            return _playerTransform != null ? (Vector2)_playerTransform.position + new Vector2(0f, 0.98f) : (Vector2)transform.position;
        }

        private float GetDistToPlayer()
        {
            return _playerTransform != null ? Vector2.Distance(transform.position, _playerTransform.position) : float.MaxValue;
        }

        private int DirToPlayer()
        {
            if (_playerTransform == null) return _enemy.FacingDirection;
            return _playerTransform.position.x >= transform.position.x ? 1 : -1;
        }

        private void FacePlayer()
        {
            if (!IsPlayerAlive()) return;
            _enemy.SetFacing(DirToPlayer());
        }

        private bool CanTrackPlayerWithLantern()
        {
            return IsPlayerAlive() && GetDistToPlayer() <= _profile.DetectRange;
        }

        private bool CanFireLaserAtPlayer()
        {
            if (!IsPlayerAlive()) return false;
            if (GetDistToPlayer() > _profile.AttackRange) return false;
            Vector3 muzzle = GetLanternMuzzlePos();
            Vector2 target = PlayerChestPos();
            Vector2 delta = target - (Vector2)muzzle;
            float dist = delta.magnitude;
            if (dist < 0.25f) return true;
            // Require clear line-of-sight so Boran doesn't blindly laser solid walls from across the map
            RaycastHit2D wall = Physics2D.Raycast(muzzle, delta * (1f / dist), dist - 0.35f, _solidMask);
            return wall.collider == null;
        }

        private void ApplyProceduralVisualJuice()
        {
            if (visualTransform == null) return;
            EnemyAIStateType st = _fsm.CurrentState != null ? _fsm.CurrentState.StateType : EnemyAIStateType.Idle;
            int dir = _enemy != null ? _enemy.FacingDirection : 1;

            float sx = _baseVisualScale.x;
            float sy = _baseVisualScale.y;
            float rotZ = 0f;
            Vector3 offset = Vector3.zero;

            if (st != EnemyAIStateType.Death)
            {
                if (archetype == MountFayArchetype.ServitorBoran)
                {
                    offset.y = Mathf.Sin(_animPhase * hoverFrequency) * hoverAmplitude;
                    if (_isLaserCharging)
                    {
                        // Light mechanical charge tremor while narrowing laser sightline
                        float amp = Mathf.Lerp(0.015f, 0.055f, _laserChargeProgress);
                        offset.x += Mathf.Sin(_animPhase * 72f) * amp;
                        offset.y += Mathf.Cos(_animPhase * 64f) * (amp * 0.6f);
                        sx *= 1f - 0.05f * _laserChargeProgress;
                        sy *= 1f + 0.05f * _laserChargeProgress;
                    }
                    else if (_isLaserFiring)
                    {
                        // Crisp recoil kickback lean against firing direction
                        offset.x -= _lockedLaserDir.x * 0.10f;
                        rotZ = -_lockedLaserDir.x * 5.5f;
                        sx *= 1.07f;
                        sy *= 0.94f;
                    }
                }
                else
                {
                    // Driftlin, Mnemonid, Mnemonord sinusoidal hover + elastic pogo recoil squash
                    offset.y = Mathf.Sin(_animPhase * hoverFrequency) * 0.045f;

                    if (_pogoRecoilTimer > 0f && _pogoRecoilDuration > 0.01f)
                    {
                        float u = 1f - Mathf.Clamp01(_pogoRecoilTimer / _pogoRecoilDuration);
                        float bounceWave = Mathf.Sin(u * Mathf.PI);
                        sx *= 1f + 0.22f * bounceWave;
                        sy *= 1f - 0.20f * bounceWave;
                    }

                    if (_isSpikeTelegraph)
                    {
                        // Tense crystalline tremor right before spikes erupt
                        offset.x += Mathf.Sin(_animPhase * 86f) * 0.06f;
                        offset.y += Mathf.Cos(_animPhase * 78f) * 0.04f;
                    }
                    else if (_isSpikeActive)
                    {
                        // Slight pressurized pulse while spikes are extended
                        float pulse = 1f + 0.045f * Mathf.Sin(_animPhase * 18f);
                        sx *= pulse;
                        sy *= pulse;
                    }
                }
            }

            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            // Animate Mnemonid / Mnemonord 360-degree spike ring pop-out & retract (scaled 1.0x Mnemonid / 1.62x Mnemonord)
            if (_spikeRingObj != null && _spikeRingObj.activeSelf)
            {
                float popScale = 1f;
                if (_isSpikeActive)
                {
                    if (_spikeAnimProgress < 0.12f)
                    {
                        popScale = Mathf.Lerp(0.75f, 1.08f, _spikeAnimProgress / 0.12f);
                    }
                    else if (_spikeAnimProgress < 0.20f)
                    {
                        popScale = Mathf.Lerp(1.08f, 1.0f, (_spikeAnimProgress - 0.12f) / 0.08f);
                    }
                    else if (_spikeAnimProgress > 0.88f)
                    {
                        popScale = Mathf.Lerp(1.0f, 0.12f, (_spikeAnimProgress - 0.88f) / 0.12f);
                    }
                }
                _spikeRingObj.transform.localScale = Vector3.one * (_spikeBaseScale * popScale);
                _spikeRingObj.transform.localRotation = Quaternion.Euler(0f, 0f, _animPhase * 14f);
            }
        }

        // =========================================================================
        // 1. SERVITOR BORAN STATES (LanternScan -> LaserCharge -> LaserFire -> BurstPause)
        // =========================================================================

        private sealed class LanternScanState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public LanternScanState(MountFayEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isLaserCharging = false;
                _ai._isLaserFiring = false;
                _ai._burstShotsFired = 0;
                _ai._enemy.StopHorizontal();
                _ai._enemy.SetChargeState(false);
            }

            public void Tick(float dt)
            {
                Vector2 aimDir;
                if (_ai.CanTrackPlayerWithLantern())
                {
                    _ai.FacePlayer();
                    Vector2 delta = _ai.PlayerChestPos() - (Vector2)_ai.GetLanternMuzzlePos();
                    float targetAng = Mathf.Atan2(delta.y, delta.x);
                    _ai._lanternAngleRad = LerpAngleRad(_ai._lanternAngleRad, targetAng, Mathf.Clamp01(dt * 6.5f));
                    aimDir = new Vector2(Mathf.Cos(_ai._lanternAngleRad), Mathf.Sin(_ai._lanternAngleRad));
                    _ai.RenderLanternBeam(aimDir, _ai._profile.LanternConeLength, 0, 0f);

                    if (_ai._cooldownTimer <= 0f && _ai.CanFireLaserAtPlayer())
                    {
                        _ai._lockedLaserDir = delta.sqrMagnitude > 0.001f ? delta.normalized : aimDir;
                        _ai.TransitionTo(_ai._laserChargeState);
                    }
                }
                else
                {
                    // Gentle atmospheric turret lighthouse sweep when idle
                    float baseAng = _ai._enemy.FacingDirection >= 0 ? 0f : Mathf.PI;
                    float sweep = Mathf.Sin(_ai._animPhase * 1.05f) * 0.36f;
                    _ai._lanternAngleRad = LerpAngleRad(_ai._lanternAngleRad, baseAng + sweep, Mathf.Clamp01(dt * 3.0f));
                    aimDir = new Vector2(Mathf.Cos(_ai._lanternAngleRad), Mathf.Sin(_ai._lanternAngleRad));
                    _ai.RenderLanternBeam(aimDir, _ai._profile.LanternConeLength * 0.72f, 0, 0f);
                }
            }

            public void FixedTick(float fixedDt)
            {
                _ai._enemy.StopHorizontal();
            }

            public void Exit() { }
        }

        private sealed class LaserChargeState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            private float _timer;
            private float _total;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public LaserChargeState(MountFayEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.StopHorizontal();
                _ai._isLaserCharging = true;
                _ai._isLaserFiring = false;
                _total = _ai._profile.LaserChargeDuration > 0.1f ? _ai._profile.LaserChargeDuration : 0.55f;
                _timer = _total;
                _ai._laserChargeProgress = 0f;

                if (_ai.IsPlayerAlive())
                {
                    _ai.FacePlayer();
                    Vector2 delta = _ai.PlayerChestPos() - (Vector2)_ai.GetLanternMuzzlePos();
                    if (delta.sqrMagnitude > 0.001f)
                    {
                        _ai._lockedLaserDir = delta.normalized;
                        _ai._lanternAngleRad = Mathf.Atan2(_ai._lockedLaserDir.y, _ai._lockedLaserDir.x);
                    }
                }

                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                _ai._laserChargeProgress = Mathf.Clamp01(1f - (_timer / _total));

                // Track briefly in first 0.14s, then LOCK strictly on player's last angle for fair dodge timing!
                if (_ai._laserChargeProgress < 0.25f && _ai.IsPlayerAlive())
                {
                    _ai.FacePlayer();
                    Vector2 delta = _ai.PlayerChestPos() - (Vector2)_ai.GetLanternMuzzlePos();
                    if (delta.sqrMagnitude > 0.001f)
                    {
                        _ai._lockedLaserDir = delta.normalized;
                        _ai._lanternAngleRad = Mathf.Atan2(_ai._lockedLaserDir.y, _ai._lockedLaserDir.x);
                    }
                }

                _ai.RenderLanternBeam(_ai._lockedLaserDir, _ai._profile.LaserLength, 1, _ai._laserChargeProgress);

                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._laserFireState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                _ai._enemy.StopHorizontal();
            }

            public void Exit()
            {
                _ai._isLaserCharging = false;
            }
        }

        private sealed class LaserFireState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            private float _timer;
            private float _total;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public LaserFireState(MountFayEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.StopHorizontal();
                _ai._isLaserCharging = false;
                _ai._isLaserFiring = true;
                _ai._laserHitPlayerThisShot = false;
                _total = _ai._profile.LaserFireDuration > 0.08f ? _ai._profile.LaserFireDuration : 0.35f;
                _timer = _total;
                _ai._burstShotsFired++;

                ChoralCodeVFX.SpawnLaserImpactSparks(_ai.GetLanternMuzzlePos(), _ai._lockedLaserDir);
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySilkSpear();
                GameEvents.RaiseCameraShake(0.045f, 0.085f);
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                float u = Mathf.Clamp01(1f - (_timer / _total));
                _ai.RenderLanternBeam(_ai._lockedLaserDir, _ai._profile.LaserLength, 2, u);

                if (_timer <= 0f)
                {
                    bool canContinueBurst = _ai._burstShotsFired < _ai._profile.LaserBurstCount && _ai.CanFireLaserAtPlayer();
                    if (canContinueBurst)
                    {
                        _ai.TransitionTo(_ai._burstPauseState);
                    }
                    else
                    {
                        _ai._cooldownTimer = _ai._profile.LaserCooldown;
                        _ai.TransitionTo(_ai._lanternScanState);
                    }
                }
            }

            public void FixedTick(float fixedDt)
            {
                _ai._enemy.StopHorizontal();
                _ai.CheckLaserPlayerHit(_ai._lockedLaserDir, _ai._profile.LaserLength);
            }

            public void Exit()
            {
                _ai._isLaserFiring = false;
                if (_ai._laserBeamVfx != null) _ai._laserBeamVfx.gameObject.SetActive(false);
                if (_ai._laserFlareVfx != null) _ai._laserFlareVfx.gameObject.SetActive(false);
                if (VFXManager.Instance != null && _ai._lastLaserLength > 0.25f)
                {
                    // Spawn Row 1 F6 (Beam Dissipate) stretched cleanly along final beam vector!
                    VFXManager.Instance.SpawnLaserDissipate(_ai.GetLanternMuzzlePos(), _ai._lockedLaserDir, _ai._lastLaserLength);
                }
            }
        }

        private sealed class BurstPauseState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public BurstPauseState(MountFayEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isLaserCharging = false;
                _ai._isLaserFiring = false;
                _timer = _ai._profile.LaserBurstInterval > 0.1f ? _ai._profile.LaserBurstInterval : 0.60f;
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_ai.IsPlayerAlive())
                {
                    _ai.FacePlayer();
                    Vector2 delta = _ai.PlayerChestPos() - (Vector2)_ai.GetLanternMuzzlePos();
                    float targetAng = Mathf.Atan2(delta.y, delta.x);
                    _ai._lanternAngleRad = LerpAngleRad(_ai._lanternAngleRad, targetAng, Mathf.Clamp01(dt * 9.0f));
                    Vector2 dir = new Vector2(Mathf.Cos(_ai._lanternAngleRad), Mathf.Sin(_ai._lanternAngleRad));
                    _ai.RenderLanternBeam(dir, _ai._profile.LanternConeLength, 0, 0f);
                }

                if (_timer <= 0f)
                {
                    if (_ai.CanFireLaserAtPlayer())
                    {
                        _ai.TransitionTo(_ai._laserChargeState);
                    }
                    else
                    {
                        _ai._cooldownTimer = _ai._profile.LaserCooldown;
                        _ai.TransitionTo(_ai._lanternScanState);
                    }
                }
            }

            public void FixedTick(float fixedDt)
            {
                _ai._enemy.StopHorizontal();
            }

            public void Exit() { }
        }

        // =========================================================================
        // 2. DRIFTLIN STATES (StationaryFloat <-> PogoBounce)
        // =========================================================================

        private sealed class StationaryFloatState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public StationaryFloatState(MountFayEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.SetChargeState(false);
            }

            public void Tick(float dt) { }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                Vector2 target = new Vector2(
                    _ai._anchorPosition.x,
                    _ai._anchorPosition.y + Mathf.Sin(_ai._animPhase * _ai.hoverFrequency) * _ai.hoverAmplitude);
                Vector2 toTarget = target - _ai._rb.position;
                Vector2 vel = toTarget * _ai._profile.AnchorReturnSpeed;
                _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 8.5f));
            }

            public void Exit() { }
        }

        private sealed class PogoBounceState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            private float _elapsed;
            private float _duration;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public PogoBounceState(MountFayEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _elapsed = 0f;
                _duration = _ai._profile.PogoRecoilDuration > 0.05f ? _ai._profile.PogoRecoilDuration : 0.30f;
                if (_ai._enemy != null) _ai._enemy.SetChargeState(true);
            }

            public void Tick(float dt)
            {
                _elapsed += dt;
                if (_elapsed >= _duration)
                {
                    _ai.TransitionTo(_ai._stationaryFloatState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                float u = Mathf.Clamp01(_elapsed / _duration);
                // Elastic dip downward (-recoilY) then smooth spring return to anchorY
                float dip = Mathf.Sin(u * Mathf.PI) * _ai._pogoRecoilOffsetY;
                Vector2 desiredPos = new Vector2(_ai._anchorPosition.x, _ai._anchorPosition.y - dip);
                Vector2 vel = (desiredPos - _ai._rb.position) * 14.0f;
                _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 14f));
            }

            public void Exit()
            {
                if (_ai._enemy != null) _ai._enemy.SetChargeState(false);
            }
        }

        // =========================================================================
        // 3. MNEMONID & MNEMONORD STATES (DormantFloat -> SpikeTriggered -> SpikeActive)
        // =========================================================================

        private sealed class DormantFloatState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public DormantFloatState(MountFayEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isSpikeTelegraph = false;
                _ai._isSpikeActive = false;
                if (_ai._spikeRingObj != null) _ai._spikeRingObj.SetActive(false);
                if (_ai._enemy != null) _ai._enemy.SetAllSideSpikeShield(false);
            }

            public void Tick(float dt) { }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                Vector2 target = new Vector2(
                    _ai._anchorPosition.x,
                    _ai._anchorPosition.y + Mathf.Sin(_ai._animPhase * _ai.hoverFrequency) * _ai.hoverAmplitude);
                Vector2 vel = (target - _ai._rb.position) * _ai._profile.AnchorReturnSpeed;
                _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 7.5f));
            }

            public void Exit() { }
        }

        private sealed class SpikeTriggeredState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            private float _timer;
            private float _duration;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public SpikeTriggeredState(MountFayEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isSpikeTelegraph = true;
                _ai._isSpikeActive = false;
                _duration = _ai._profile.SpikeTriggerDelay > 0.04f ? _ai._profile.SpikeTriggerDelay : 0.12f;
                _timer = _duration;
                if (_ai._spikeRingObj != null && _ai._spikeSpriteVfx != null && VFXManager.Instance != null)
                {
                    Sprite[] emFrames = VFXManager.Instance.GetIceSpikesEmergingFrames();
                    if (emFrames != null && emFrames.Length > 0)
                    {
                        _ai._spikeRingObj.SetActive(true);
                        _ai._spikeRingObj.transform.localScale = Vector3.one * _ai._spikeBaseScale;
                        _ai._spikeSpriteVfx.Play(emFrames, ENEMY_VFX_CONFIG.MnemonSpikesEmerging.Fps, false, false);
                    }
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.60f);
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._spikeActiveState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                float u = 1f - Mathf.Clamp01(_timer / _duration);
                float dip = Mathf.Sin(u * Mathf.PI * 0.5f) * _ai._pogoRecoilOffsetY;
                Vector2 desiredPos = new Vector2(_ai._anchorPosition.x, _ai._anchorPosition.y - dip);
                _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, (desiredPos - _ai._rb.position) * 12.0f, Mathf.Clamp01(fixedDt * 12f));
            }

            public void Exit()
            {
                _ai._isSpikeTelegraph = false;
            }
        }

        private sealed class SpikeActiveState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            private float _elapsed;
            private float _duration;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public SpikeActiveState(MountFayEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _elapsed = 0f;
                _duration = _ai._profile.SpikeActiveDuration > 0.2f ? _ai._profile.SpikeActiveDuration : 1.20f;
                _ai._isSpikeActive = true;
                _ai._spikeAnimProgress = 0f;

                if (_ai._spikeRingObj != null)
                {
                    _ai._spikeRingObj.SetActive(true);
                    _ai._spikeRingObj.transform.localScale = Vector3.one * (_ai._spikeBaseScale * 0.75f);
                    if (_ai._spikeSpriteVfx != null && VFXManager.Instance != null)
                    {
                        Sprite[] actFrames = VFXManager.Instance.GetIceSpikesActiveFrames();
                        if (actFrames != null && actFrames.Length > 0)
                        {
                            _ai._spikeSpriteVfx.Play(actFrames, ENEMY_VFX_CONFIG.MnemonSpikesActive.Fps, true, false);
                        }
                    }
                }
                if (_ai._enemy != null) _ai._enemy.SetAllSideSpikeShield(true);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(BreakablePropKind.IceCrystalCluster, 0.80f);
            }

            public void Tick(float dt)
            {
                _elapsed += dt;
                _ai._spikeAnimProgress = Mathf.Clamp01(_elapsed / _duration);
                if (_elapsed >= _duration)
                {
                    _ai.TransitionTo(_ai._dormantFloatState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb != null)
                {
                    Vector2 target = new Vector2(
                        _ai._anchorPosition.x,
                        _ai._anchorPosition.y + Mathf.Sin(_ai._animPhase * _ai.hoverFrequency) * (_ai.hoverAmplitude * 0.65f));
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, (target - _ai._rb.position) * _ai._profile.AnchorReturnSpeed, Mathf.Clamp01(fixedDt * 8f));
                }

                // Active 360-degree spike touch hazard (1 damage)
                if (Time.time < _ai._nextSpikeTouchDamageTime) return;
                Collider2D[] hits = Physics2D.OverlapCircleAll(_ai.transform.position, _ai._profile.SpikeOuterRadius, 1 << GameConstants.LayerPlayer);
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D c = hits[i];
                    if (c == null) continue;
                    PlayerController pc = c.GetComponentInParent<PlayerController>();
                    if (pc != null && pc.IsAlive && !pc.IsInvulnerable)
                    {
                        Vector2 away = ((Vector2)pc.transform.position - (Vector2)_ai.transform.position);
                        if (away.sqrMagnitude < 0.01f) away = Vector2.up;
                        DamageInfo spikeDmg = new DamageInfo(
                            _ai._profile.SpikeContactDamage,
                            away.normalized,
                            _ai._profile.ContactKnockback,
                            0.18f,
                            _ai.gameObject,
                            pc.transform.position,
                            false);
                        if (pc.TakeDamage(spikeDmg))
                        {
                            _ai._nextSpikeTouchDamageTime = Time.time + 0.45f;
                            break;
                        }
                    }
                }
            }

            public void Exit()
            {
                if (_ai._isSpikeActive && _ai._enemy != null && _ai._enemy.IsAlive && VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnIceSpikeShatter(_ai.transform.position, _ai._spikeBaseScale);
                }
                _ai._isSpikeActive = false;
                if (_ai._spikeRingObj != null) _ai._spikeRingObj.SetActive(false);
                if (_ai._enemy != null) _ai._enemy.SetAllSideSpikeShield(false);
            }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly MountFayEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(MountFayEnemyAI ai) { _ai = ai; }
            public void Enter()
            {
                _ai._enemy.DeactivateAttackHitbox();
                _ai._enemy.StopHorizontal();
            }
            public void Tick(float dt) { }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }

        private static float LerpAngleRad(float a, float b, float t)
        {
            float diff = b - a;
            while (diff > Mathf.PI) diff -= Mathf.PI * 2f;
            while (diff < -Mathf.PI) diff += Mathf.PI * 2f;
            return a + diff * t;
        }
    }
}
