using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Silksong-style Pale Nail homing silk claw (00:30:02):
    /// Hovers briefly behind the player's shoulder tethered by luminous silk, then locks onto
    /// the nearest alive enemy and steers aggressively with agile homing curvature.
    /// Self-rendered via crisp tapered LineRenderers so zero extra prefabs are required.
    /// </summary>
    public class PaleNailProjectile : MonoBehaviour
    {
        private static Material _sharedMat;

        private GameObject _owner;
        private Transform _followRoot;
        private Vector3 _hoverOffset;
        private int _facingDir = 1;
        private int _damage = 12;
        private float _hoverDuration = 0.18f;
        private float _launchSpeed = 19.5f;
        private float _turnRateRad = 9.2f;
        private float _lifetime = 2.2f;
        private LayerMask _targetMask;
        private LayerMask _wallMask;

        private bool _launched;
        private float _elapsed;
        private float _postLaunchTime;
        private Vector2 _velocity;
        private Transform _lockedTarget;
        private int _preferredSlotIndex;

        private LineRenderer _clawBody;
        private LineRenderer _silkTrail;
        private readonly Vector3[] _trailHistory = new Vector3[7];

        public static PaleNailProjectile Spawn(
            Transform ownerRoot,
            Vector3 hoverLocalOffset,
            int facingDir,
            int damage,
            float hoverDelay,
            int slotIndex,
            LayerMask targetMask,
            LayerMask wallMask)
        {
            GameObject go = new GameObject("PaleNail_" + slotIndex);
            go.transform.position = ownerRoot != null ? ownerRoot.position + hoverLocalOffset : hoverLocalOffset;
            PaleNailProjectile nail = go.AddComponent<PaleNailProjectile>();
            nail.Initialize(ownerRoot, hoverLocalOffset, facingDir, damage, hoverDelay, slotIndex, targetMask, wallMask);
            return nail;
        }

        private static Material GetMat()
        {
            if (_sharedMat == null)
            {
                Shader sh = Shader.Find("Sprites/Default");
                if (sh != null) _sharedMat = new Material(sh);
            }
            return _sharedMat;
        }

        private void Initialize(
            Transform ownerRoot,
            Vector3 hoverOffset,
            int facingDir,
            int damage,
            float hoverDelay,
            int slotIndex,
            LayerMask targetMask,
            LayerMask wallMask)
        {
            _owner = ownerRoot != null ? ownerRoot.gameObject : null;
            _followRoot = ownerRoot;
            _hoverOffset = hoverOffset;
            _facingDir = facingDir >= 0 ? 1 : -1;
            _damage = Mathf.Max(1, damage);
            _hoverDuration = Mathf.Max(0.06f, hoverDelay);
            _preferredSlotIndex = slotIndex;
            _targetMask = targetMask;
            _wallMask = wallMask;
            _velocity = new Vector2(_facingDir * _launchSpeed, 0f);

            BuildVisualLines();
            Vector3 startPos = transform.position;
            for (int i = 0; i < _trailHistory.Length; i++) _trailHistory[i] = startPos;
            UpdateClawVisuals(new Vector2(_facingDir, 0f));
        }

        private void BuildVisualLines()
        {
            Material mat = GetMat();

            GameObject bodyObj = new GameObject("ClawBlade");
            bodyObj.transform.SetParent(transform, false);
            _clawBody = bodyObj.AddComponent<LineRenderer>();
            _clawBody.useWorldSpace = true;
            _clawBody.sortingLayerName = GameConstants.SortVFX;
            _clawBody.sortingOrder = 22;
            if (mat != null) _clawBody.sharedMaterial = mat;
            _clawBody.positionCount = 3;
            _clawBody.startWidth = 0.03f;
            _clawBody.endWidth = 0.14f;
            _clawBody.startColor = new Color(0.76f, 0.96f, 1.0f, 0.96f);
            _clawBody.endColor = Color.white;

            GameObject trailObj = new GameObject("SilkTail");
            trailObj.transform.SetParent(transform, false);
            _silkTrail = trailObj.AddComponent<LineRenderer>();
            _silkTrail.useWorldSpace = true;
            _silkTrail.sortingLayerName = GameConstants.SortVFX;
            _silkTrail.sortingOrder = 21;
            if (mat != null) _silkTrail.sharedMaterial = mat;
            _silkTrail.positionCount = _trailHistory.Length;
            _silkTrail.startWidth = 0.09f;
            _silkTrail.endWidth = 0.01f;
            _silkTrail.startColor = new Color(0.84f, 0.98f, 1.0f, 0.90f);
            _silkTrail.endColor = new Color(0.68f, 0.92f, 1.0f, 0.05f);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float dt = Time.deltaTime;
            _elapsed += dt;

            if (!_launched)
            {
                if (_followRoot != null)
                {
                    float bob = Mathf.Sin((_elapsed + _preferredSlotIndex * 0.25f) * 16f) * 0.08f;
                    transform.position = _followRoot.position + _hoverOffset + new Vector3(0f, bob, 0f);
                }

                _lockedTarget = FindBestEnemyTarget();
                Vector2 aimDir = ComputeAimDirection();
                UpdateTrail(transform.position);
                UpdateClawVisuals(aimDir);

                if (_elapsed >= _hoverDuration)
                {
                    _launched = true;
                    _velocity = aimDir * _launchSpeed;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayAttackSlash((_preferredSlotIndex % 3) + 1);
                    ChoralCodeVFX.SpawnTelegraphGlint(transform.position, new Color(0.82f, 0.98f, 1.0f, 0.95f), 0.36f, 0.12f);
                }
                return;
            }

            _postLaunchTime += dt;
            if (_postLaunchTime >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Re-acquire live enemy if current target died or escaped
            if (_lockedTarget == null || !IsTargetAlive(_lockedTarget))
            {
                _lockedTarget = FindBestEnemyTarget();
            }

            // Agile homing turn toward locked enemy
            if (_lockedTarget != null)
            {
                Vector2 desired = ((Vector2)(_lockedTarget.position + Vector3.up * 0.78f) - (Vector2)transform.position).normalized;
                Vector2 curDir = _velocity.sqrMagnitude > 0.01f ? _velocity.normalized : new Vector2(_facingDir, 0f);
                float curAng = Mathf.Atan2(curDir.y, curDir.x);
                float desAng = Mathf.Atan2(desired.y, desired.x);
                float delta = desAng - curAng;
                while (delta > Mathf.PI) delta -= Mathf.PI * 2f;
                while (delta < -Mathf.PI) delta += Mathf.PI * 2f;
                float maxStep = _turnRateRad * dt;
                float nextAng = curAng + Mathf.Clamp(delta, -maxStep, maxStep);
                _velocity = new Vector2(Mathf.Cos(nextAng), Mathf.Sin(nextAng)) * _launchSpeed;
            }

            Vector3 step = new Vector3(_velocity.x * dt, _velocity.y * dt, 0f);
            transform.position += step;

            Vector2 dirNorm = _velocity.sqrMagnitude > 0.01f ? _velocity.normalized : new Vector2(_facingDir, 0f);
            UpdateTrail(transform.position);
            UpdateClawVisuals(dirNorm);

            if (CheckTargetHit(dirNorm))
            {
                Destroy(gameObject);
                return;
            }

            if (_postLaunchTime > 0.10f)
            {
                Collider2D[] walls = Physics2D.OverlapCircleAll(transform.position, 0.22f, _wallMask);
                if (walls != null && walls.Length > 0)
                {
                    if (VFXManager.Instance != null) VFXManager.Instance.SpawnHitImpact(transform.position, Color.white, false, true);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.55f);
                    Destroy(gameObject);
                }
            }
        }

        private bool CheckTargetHit(Vector2 dirNorm)
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 0.48f, _targetMask);
            if (cols == null || cols.Length == 0) return false;

            for (int i = 0; i < cols.Length; i++)
            {
                Collider2D col = cols[i];
                if (col == null) continue;
                if (_owner != null && (col.gameObject == _owner || col.transform.IsChildOf(_owner.transform))) continue;

                Hurtbox hb = col.GetComponent<Hurtbox>();
                IDamageable target = hb != null ? hb.Owner : col.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive) continue;

                Vector3 hitPt = col.ClosestPoint(transform.position);
                DamageInfo info = new DamageInfo(
                    _damage,
                    new Vector2(dirNorm.x, Mathf.Max(0.22f, dirNorm.y)).normalized,
                    4.4f,
                    0.18f,
                    _owner,
                    hitPt,
                    false,
                    false);

                bool dealt = hb != null ? hb.ReceiveHit(info) : target.TakeDamage(info);
                if (dealt)
                {
                    bool isEnemy = target is EnemyController;
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.SpawnHitImpact(hitPt, new Color(0.86f, 0.99f, 1.0f, 1f), isEnemy, !isEnemy);
                    }
                    ChoralCodeVFX.SpawnSlashArc(hitPt, dirNorm.x >= 0f ? 1 : -1, 0.72f, 0.55f, 95f, 0.11f, Color.white, 0.11f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyHit();
                    GameEvents.RaiseCameraShake(0.032f, 0.05f);
                    return true;
                }
            }
            return false;
        }

        private bool IsTargetAlive(Transform t)
        {
            if (t == null) return false;
            EnemyController ec = t.GetComponent<EnemyController>();
            return ec != null && ec.IsAlive;
        }

        private Transform FindBestEnemyTarget()
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 16.5f, 1 << GameConstants.LayerEnemy);
            if (cols == null || cols.Length == 0) return null;

            Transform best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null) continue;
                EnemyController ec = cols[i].GetComponentInParent<EnemyController>();
                if (ec == null || !ec.IsAlive) continue;
                float dist = Vector2.Distance(transform.position, ec.transform.position);
                float dx = ec.transform.position.x - transform.position.x;
                // Mild preference for enemies in front of the player, while still locking onto flanking flyers!
                float frontBonus = (dx * _facingDir >= -1.0f) ? 0f : 3.2f;
                float score = dist + frontBonus;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = ec.transform;
                }
            }
            return best;
        }

        private Vector2 ComputeAimDirection()
        {
            if (_lockedTarget != null)
            {
                Vector2 diff = (Vector2)(_lockedTarget.position + Vector3.up * 0.78f) - (Vector2)transform.position;
                if (diff.sqrMagnitude > 0.04f) return diff.normalized;
            }
            float fanY = (_preferredSlotIndex - 1) * 0.12f;
            return new Vector2(_facingDir, fanY).normalized;
        }

        private void UpdateTrail(Vector3 headPos)
        {
            for (int i = _trailHistory.Length - 1; i > 0; i--)
            {
                _trailHistory[i] = _trailHistory[i - 1];
            }
            _trailHistory[0] = headPos;
            if (_silkTrail != null)
            {
                for (int i = 0; i < _trailHistory.Length; i++)
                {
                    _silkTrail.SetPosition(i, _trailHistory[i]);
                }
            }
        }

        private void UpdateClawVisuals(Vector2 dirNorm)
        {
            if (_clawBody == null) return;
            Vector3 p = transform.position;
            Vector3 d = new Vector3(dirNorm.x, dirNorm.y, 0f);
            _clawBody.SetPosition(0, p - d * 0.34f);
            _clawBody.SetPosition(1, p + d * 0.08f);
            _clawBody.SetPosition(2, p + d * 0.56f);
        }
    }
}
