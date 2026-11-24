using System.Collections;
using UnityEngine;

namespace SilkenGrotto
{
    public enum BreakablePropKind
    {
        TallGrass,
        WoodenBarrel,
        AncientUrn,
        BriarBramble,
        RottenLog,
        SilkSpool,
        DrySilkCocoon,
        WebBarricade,
        HollowBugShell,
        FragileWoodPillar,
        DryCoralFormation,
        RustedGear,
        AncientBonePile,
        CrackedBell,
        TownCrate,
        OldLantern,
        IceCrystalCluster,
        FrozenWoodPost,
        CrackedSentinelStatue,
        GildedPalaceUrn,
        RustedChain,
        ChoirBench,
        BrassCandelabra,
        CeremonialUrn
    }

    /// <summary>
    /// Destructible stage obstacles & environmental clutter across all 8 Silken Grotto biomes.
    /// Supports 1-2 hit durability, crisp white hit-flash + recoil, material-specific shatter
    /// particles & audio, optional solid barrier clearing, and popping Silk/Essence orb drops.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BreakableProp : MonoBehaviour, IDamageable
    {
        [SerializeField] private BreakablePropKind kind = BreakablePropKind.TallGrass;
        [SerializeField] private int maxHits = 1;
        [SerializeField] private int rewardEssence;
        [SerializeField] private float rewardSilk;
        [SerializeField] private int orbDropCount;
        [SerializeField] private int essencePerOrb = 6;
        [SerializeField] private GameObject orbDropPrefab;
        [SerializeField] private bool blocksMovement;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private BoxCollider2D _collider;
        private int _currentHits;
        private bool _broken;
        private float _swayPhase;
        private float _rustleTilt;
        private float _rustleCooldown;
        private Coroutine _hitRoutine;

        public BreakablePropKind Kind { get { return kind; } }
        public bool IsAlive { get { return !_broken; } }
        public bool IsInvulnerable { get { return false; } }
        public bool AllowPogoBounce
        {
            get
            {
                return kind == BreakablePropKind.BriarBramble
                    || kind == BreakablePropKind.WoodenBarrel
                    || kind == BreakablePropKind.WebBarricade
                    || kind == BreakablePropKind.IceCrystalCluster
                    || kind == BreakablePropKind.CrackedBell;
            }
        }

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _currentHits = Mathf.Max(1, maxHits);
            _swayPhase = (transform.position.x * 0.73f) + (transform.position.y * 0.31f);
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Configure(
            BreakablePropKind propKind,
            int hits,
            int essenceReward,
            float silkReward,
            bool solidObstacle,
            SpriteRenderer sr,
            GameObject dropPrefab = null,
            int dropCount = 0,
            int perOrbEssence = 6)
        {
            kind = propKind;
            maxHits = Mathf.Max(1, hits);
            _currentHits = maxHits;
            rewardEssence = Mathf.Max(0, essenceReward);
            rewardSilk = Mathf.Max(0f, silkReward);
            orbDropPrefab = dropPrefab;
            orbDropCount = Mathf.Max(0, dropCount);
            essencePerOrb = Mathf.Max(1, perOrbEssence);
            blocksMovement = solidObstacle;
            spriteRenderer = sr;
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
            if (_collider != null) _collider.isTrigger = !solidObstacle;
        }

        private void Update()
        {
            if (_broken || spriteRenderer == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_rustleCooldown > 0f) _rustleCooldown -= Time.deltaTime;

            if (kind == BreakablePropKind.TallGrass || kind == BreakablePropKind.BriarBramble || kind == BreakablePropKind.RustedChain)
            {
                float breezeAmp = kind == BreakablePropKind.TallGrass ? 3.2f : (kind == BreakablePropKind.RustedChain ? 2.4f : 0.9f);
                float breeze = Mathf.Sin(Time.time * 2.15f + _swayPhase) * breezeAmp;
                _rustleTilt = Mathf.MoveTowards(_rustleTilt, 0f, 36f * Time.deltaTime);
                spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, breeze + _rustleTilt);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_broken || blocksMovement || other == null || other.isTrigger) return;
            if (kind != BreakablePropKind.TallGrass && kind != BreakablePropKind.BriarBramble) return;
            if (!other.CompareTag(GameConstants.TagPlayer) && other.gameObject.layer != GameConstants.LayerEnemy) return;
            if (_rustleCooldown > 0f) return;

            _rustleCooldown = 0.28f;
            float pushDir = other.transform.position.x <= transform.position.x ? -1f : 1f;
            _rustleTilt = pushDir * (kind == BreakablePropKind.TallGrass ? 14f : 5f);

            if (other.CompareTag(GameConstants.TagPlayer) && VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnFoliageLeafBurst(transform.position + Vector3.up * 0.35f, 10, -pushDir * 0.8f);
            }
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (_broken) return false;
            _currentHits--;

            Vector3 burstOrigin = transform.position + Vector3.up * 0.45f;
            if (_currentHits > 0)
            {
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnHitImpact(info.HitPoint, Color.white, false, kind == BreakablePropKind.CrackedBell || kind == BreakablePropKind.RustedGear);
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(kind, 0.72f);
                GameEvents.RaiseCameraShake(0.022f, 0.04f);

                if (spriteRenderer != null)
                {
                    if (_hitRoutine != null) StopCoroutine(_hitRoutine);
                    _hitRoutine = StartCoroutine(HitFlashAndRecoilRoutine(info.KnockbackDirection.x));
                }
                return true;
            }

            Shatter(info);
            return true;
        }

        private void Shatter(DamageInfo info)
        {
            _broken = true;
            if (_hitRoutine != null)
            {
                StopCoroutine(_hitRoutine);
                _hitRoutine = null;
            }

            Vector3 burstPos = transform.position + Vector3.up * 0.48f;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnPropBreakDebris(burstPos, kind, info.KnockbackDirection.x);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPropBreak(kind, 1.0f);
            }

            if (kind != BreakablePropKind.TallGrass)
            {
                GameEvents.RaiseCameraShake(blocksMovement ? 0.044f : 0.032f, 0.055f);
            }

            GrantReward(burstPos, info.KnockbackDirection.x);

            if (_collider != null) _collider.enabled = false;
            gameObject.SetActive(false);
        }

        private void GrantReward(Vector3 burstPos, float dirX)
        {
            if (orbDropPrefab != null && orbDropCount > 0)
            {
                for (int i = 0; i < orbDropCount; i++)
                {
                    GameObject orb = Instantiate(orbDropPrefab, burstPos, Quaternion.identity);
                    SilkOrbPickup pickup = orb.GetComponent<SilkOrbPickup>();
                    if (pickup != null)
                    {
                        float spread = Random.Range(-3.2f, 3.2f) + Mathf.Clamp(dirX, -1f, 1f) * 1.4f;
                        pickup.InitializeDrop(essencePerOrb, new Vector2(spread, Random.Range(4.0f, 6.8f)));
                    }
                }
            }

            if (rewardEssence <= 0 && rewardSilk <= 0.01f) return;

            PlayerController player = FindObjectOfType<PlayerController>();
            if (player == null) return;

            if (rewardEssence > 0 && (orbDropPrefab == null || orbDropCount <= 0) && player.Upgrades != null)
            {
                player.Upgrades.AddSilkEssence(rewardEssence);
            }
            if (rewardSilk > 0.01f && player.Stats != null)
            {
                player.Stats.AddSilk(rewardSilk);
            }
        }

        private IEnumerator HitFlashAndRecoilRoutine(float dirX)
        {
            Vector3 baseLocalPos = spriteRenderer.transform.localPosition;
            Color baseCol = spriteRenderer.color;
            // Pure white bright hit-flash clearly visible against dark atmospheric scenes
            spriteRenderer.color = new Color(2.2f, 2.2f, 2.2f, 1f);
            float push = Mathf.Abs(dirX) > 0.05f ? Mathf.Sign(dirX) : 1f;
            for (int i = 0; i < 4; i++)
            {
                float off = (i % 2 == 0 ? 0.08f : -0.05f) * push;
                spriteRenderer.transform.localPosition = baseLocalPos + new Vector3(off, 0f, 0f);
                yield return new WaitForSeconds(0.025f);
                if (i == 1) spriteRenderer.color = Color.Lerp(Color.white, baseCol, 0.5f);
            }
            spriteRenderer.transform.localPosition = baseLocalPos;
            spriteRenderer.color = baseCol;
            _hitRoutine = null;
        }
    }
}
