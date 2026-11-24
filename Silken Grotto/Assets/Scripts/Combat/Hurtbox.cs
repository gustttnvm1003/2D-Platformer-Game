using System.Collections;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Attached to Player or Enemy body to receive incoming Hitbox strikes,
    /// handle invincibility frames, and play visual hit-flash feedback.
    /// </summary>
    public class Hurtbox : MonoBehaviour
    {
        [Header("Owner & Visuals")]
        [SerializeField] private MonoBehaviour damageableComponent;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.42f, 1f);
        [SerializeField] private float hitFlashDuration = 0.12f;
        [SerializeField] private float whiteHitFlashDuration = 0.10f;
        [SerializeField] private bool useWhiteShaderFlash = true;

        private static readonly int ShaderFlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int ShaderFlashColorId = Shader.PropertyToID("_FlashColor");

        private IDamageable _owner;
        private float _invulnTimer;
        private Color _originalColor = Color.white;
        private Coroutine _flashCoroutine;
        private MaterialPropertyBlock _mpb;
        private bool _supportsFlashShader;

        public IDamageable Owner { get { return _owner; } }
        public bool IsInvulnerable { get { return _invulnTimer > 0f; } }

        private void Awake()
        {
            ResolveOwner();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                _originalColor = spriteRenderer.color;
                _supportsFlashShader = spriteRenderer.sharedMaterial != null
                    && spriteRenderer.sharedMaterial.HasProperty(ShaderFlashAmountId);
                if (_supportsFlashShader)
                {
                    _mpb = new MaterialPropertyBlock();
                }
            }
        }

        private void ResolveOwner()
        {
            if (damageableComponent != null)
            {
                _owner = damageableComponent as IDamageable;
            }
            if (_owner == null)
            {
                _owner = GetComponentInParent<IDamageable>();
            }
        }

        private void Update()
        {
            if (_invulnTimer > 0f)
            {
                _invulnTimer -= Time.deltaTime;
                if (spriteRenderer != null)
                {
                    // Subtle i-frame flicker while invulnerable
                    float blink = Mathf.PingPong(Time.time * 18f, 1f);
                    Color c = _originalColor;
                    c.a = Mathf.Lerp(0.45f, 1f, blink);
                    spriteRenderer.color = c;
                }
                if (_invulnTimer <= 0f && spriteRenderer != null)
                {
                    spriteRenderer.color = _originalColor;
                }
            }
        }

        public void SetInvulnerable(float duration)
        {
            if (duration > _invulnTimer)
            {
                _invulnTimer = duration;
            }
        }

        public void ClearInvulnerability()
        {
            _invulnTimer = 0f;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = _originalColor;
            }
        }

        public bool ReceiveHit(DamageInfo info)
        {
            if (_owner == null) ResolveOwner();
            if (_owner == null || !_owner.IsAlive) return false;
            if (_invulnTimer > 0f || _owner.IsInvulnerable) return false;

            bool damaged = _owner.TakeDamage(info);
            if (damaged)
            {
                TriggerFlash();
            }
            return damaged;
        }

        public void TriggerFlash()
        {
            if (spriteRenderer == null || !gameObject.activeInHierarchy) return;
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            Color targetColor = (_supportsFlashShader && useWhiteShaderFlash) ? Color.white : hitFlashColor;
            float dur = (_supportsFlashShader && useWhiteShaderFlash) ? whiteHitFlashDuration : hitFlashDuration;
            _flashCoroutine = StartCoroutine(FlashRoutine(targetColor, dur));
        }

        public void TriggerWhiteSilhouetteFlash(float duration = 0.14f)
        {
            if (spriteRenderer == null || !gameObject.activeInHierarchy) return;
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine(Color.white, Mathf.Max(0.05f, duration)));
        }

        private IEnumerator FlashRoutine(Color flashCol, float duration)
        {
            if (_supportsFlashShader && spriteRenderer != null)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                spriteRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(ShaderFlashColorId, flashCol);
                _mpb.SetFloat(ShaderFlashAmountId, 1f);
                spriteRenderer.SetPropertyBlock(_mpb);

                yield return new WaitForSeconds(duration);

                if (spriteRenderer != null)
                {
                    spriteRenderer.GetPropertyBlock(_mpb);
                    _mpb.SetFloat(ShaderFlashAmountId, 0f);
                    spriteRenderer.SetPropertyBlock(_mpb);
                }
            }
            else
            {
                spriteRenderer.color = flashCol;
                yield return new WaitForSeconds(duration);
                if (spriteRenderer != null && _invulnTimer <= 0f)
                {
                    spriteRenderer.color = _originalColor;
                }
            }
            _flashCoroutine = null;
        }
    }
}
