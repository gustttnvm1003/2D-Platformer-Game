using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Reusable frame-by-frame sprite sheet VFX player running cleanly at 18-24 FPS
    /// independent of Unity's 60 FPS FixedUpdate combat physics.
    /// Supports both self-destroying one-shot bursts and persistent looping/state-driven overlays.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteSheetVfxPlayer : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Sprite[] _frames;
        private float _fps = 20f;
        private bool _loop;
        private bool _destroyOnComplete;
        private float _timer;
        private int _index;
        private bool _playing;
        private float _minHoldDuration;
        private float _elapsed;

        public SpriteRenderer Renderer
        {
            get
            {
                if (_sr == null) _sr = GetComponent<SpriteRenderer>();
                return _sr;
            }
        }

        public bool IsPlaying { get { return _playing; } }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void ConfigureSorting(int sortingOrder, Material sharedMaterial = null)
        {
            SpriteRenderer sr = Renderer;
            sr.sortingLayerName = GameConstants.SortVFX;
            sr.sortingOrder = sortingOrder;
            if (sharedMaterial != null)
            {
                sr.sharedMaterial = sharedMaterial;
            }
        }

        public void SetTint(Color color)
        {
            Renderer.color = color;
        }

        public void Play(Sprite[] frames, float fps, bool loop, bool destroyOnComplete, float minHoldSeconds = 0f)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null)
            {
                _playing = false;
                if (destroyOnComplete) Destroy(gameObject);
                return;
            }

            bool sameClip = _playing && _loop && loop && _frames == frames;
            if (sameClip)
            {
                _fps = Mathf.Clamp(fps, 1f, 60f);
                return;
            }

            _frames = frames;
            _fps = Mathf.Clamp(fps, 1f, 60f);
            _loop = loop;
            _destroyOnComplete = destroyOnComplete;
            _minHoldDuration = Mathf.Max(0f, minHoldSeconds);
            _timer = 0f;
            _elapsed = 0f;
            _index = 0;
            _playing = true;
            Renderer.sprite = _frames[0];
        }

        public void StopAndClear()
        {
            _playing = false;
            _frames = null;
        }

        private void Update()
        {
            if (!_playing || _frames == null || _frames.Length == 0) return;

            float dt = Time.deltaTime;
            _elapsed += dt;
            _timer += dt;

            float frameDur = 1f / _fps;
            while (_timer >= frameDur)
            {
                _timer -= frameDur;
                _index++;

                if (_index >= _frames.Length)
                {
                    if (_loop)
                    {
                        _index = 0;
                    }
                    else
                    {
                        _index = _frames.Length - 1;
                        if (_elapsed >= _minHoldDuration)
                        {
                            _playing = false;
                            if (_destroyOnComplete)
                            {
                                Destroy(gameObject);
                            }
                            return;
                        }
                        break;
                    }
                }
            }

            Sprite sp = _frames[_index];
            if (sp != null)
            {
                Renderer.sprite = sp;
            }
        }
    }
}
