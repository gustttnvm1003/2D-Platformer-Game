using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Layered subterranean cavern audio engine:
    /// - Sparse melancholic chamber BGM smoothly breathing into ambient silence
    /// - Continuous low-pass-filtered cavern water-drip & underground stream ambient loop
    /// - Crisp dry-front gameplay SFX with subtle subterranean acoustic depth
    /// - Surface-aware footsteps (moss vs bare carved stone), enemy insectoid cues,
    ///   metallic clinks, and delicate harp/bell UI navigation sounds.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Layered Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource musicCrossfadeSource;
        [SerializeField] private AudioSource ambientCavernSource;
        [SerializeField] private AudioSource ambientCrossfadeSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Music & Subterranean Ambience")]
        [SerializeField] private AudioClip bgmMossGrotto;
        [SerializeField] private AudioClip[] stageBgmClips;
        [SerializeField] private AudioClip ambientCavernLoop;
        [SerializeField] private AudioClip ambientDarkStoneLoop;
        [Range(0f, 1f)] [SerializeField] private float ambientCavernLevel = 0.52f;
        [SerializeField] private bool enableDynamicMusicBreathing = true;

        [Header("Movement & Surface Footstep SFX")]
        [SerializeField] private AudioClip sfxFootstep;
        [SerializeField] private AudioClip sfxFootstepMoss;
        [SerializeField] private AudioClip sfxFootstepStone;
        [SerializeField] private AudioClip sfxJump;
        [SerializeField] private AudioClip sfxDoubleJump;
        [SerializeField] private AudioClip sfxLand;
        [SerializeField] private AudioClip sfxDash;
        [SerializeField] private AudioClip sfxLedgeMantle;

        [Header("Combat & Skill SFX")]
        [SerializeField] private AudioClip sfxAttackSlash1;
        [SerializeField] private AudioClip sfxAttackSlash2;
        [SerializeField] private AudioClip sfxAttackSlash3;
        [SerializeField] private AudioClip sfxSilkSpear;
        [SerializeField] private AudioClip sfxThreadGuard;
        [SerializeField] private AudioClip sfxBindCharge;
        [SerializeField] private AudioClip sfxBindBurst;
        [SerializeField] private AudioClip sfxEnemyHit;
        [SerializeField] private AudioClip sfxArmorClink;
        [SerializeField] private AudioClip sfxEnemyChirp;
        [SerializeField] private AudioClip sfxEnemyWindup;
        [SerializeField] private AudioClip sfxEnemyDeath;
        [SerializeField] private AudioClip sfxPlayerHit;
        [SerializeField] private AudioClip sfxPlayerDeath;

        [Header("World & UI SFX")]
        [SerializeField] private AudioClip sfxWaterDrip;
        [SerializeField] private AudioClip sfxBreakWood;
        [SerializeField] private AudioClip sfxBreakUrn;
        [SerializeField] private AudioClip sfxBreakFoliage;
        [SerializeField] private AudioClip sfxBellChime;
        [SerializeField] private AudioClip sfxIceShatter;
        [SerializeField] private AudioClip sfxCheckpoint;
        [SerializeField] private AudioClip sfxSkillUnlock;
        [SerializeField] private AudioClip sfxUpgrade;
        [SerializeField] private AudioClip sfxUIClick;
        [SerializeField] private AudioClip sfxUIHover;

        private float _masterVolume = 0.85f;
        private float _musicVolume = 0.65f;
        private float _sfxVolume = 0.85f;
        private float _lastChirpTime = -10f;
        private float _lastDripTime = -10f;
        private float _ambientCrossfadeBalance;
        private float _musicCrossfadeScale = 1f;
        private float _activeMusicFadeWeight = 1f;
        private float _outgoingMusicFadeWeight;
        private bool _usingDarkStoneAmbience;
        private Coroutine _ambienceCrossfadeCoroutine;
        private Coroutine _musicCrossfadeCoroutine;

        public float MasterVolume { get { return _masterVolume; } }
        public float MusicVolume { get { return _musicVolume; } }
        public float SfxVolume { get { return _sfxVolume; } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureAudioSources();
            LoadVolumeSettings();
        }

        private void Start()
        {
            if (bgmMossGrotto != null && musicSource != null && musicSource.clip == null && !musicSource.isPlaying)
            {
                PlayMusic(bgmMossGrotto);
            }
            if (ambientCavernLoop != null && ambientCavernSource != null && !ambientCavernSource.isPlaying)
            {
                ambientCavernSource.clip = ambientCavernLoop;
                ambientCavernSource.loop = true;
                ambientCavernSource.volume = _masterVolume * _musicVolume * ambientCavernLevel;
                ambientCavernSource.Play();
            }
        }

        private void Update()
        {
            if (musicSource == null) return;

            // Gentle organic swell so sparse chamber melodies fade delicately into cavern drips during exploration
            float breath = enableDynamicMusicBreathing ? (0.84f + 0.16f * Mathf.Sin(Time.unscaledTime * 0.16f)) : 1.0f;
            float stateMult = (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused) ? 0.55f : 1.0f;
            float baseMusicVol = _masterVolume * _musicVolume * breath * stateMult * _musicCrossfadeScale;

            if (musicSource.isPlaying)
            {
                musicSource.volume = baseMusicVol * _activeMusicFadeWeight;
            }
            if (musicCrossfadeSource != null && musicCrossfadeSource.isPlaying)
            {
                musicCrossfadeSource.volume = baseMusicVol * _outgoingMusicFadeWeight;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            if (musicCrossfadeSource == null)
            {
                musicCrossfadeSource = gameObject.AddComponent<AudioSource>();
                musicCrossfadeSource.loop = true;
                musicCrossfadeSource.playOnAwake = false;
            }
            if (ambientCavernSource == null)
            {
                ambientCavernSource = gameObject.AddComponent<AudioSource>();
                ambientCavernSource.loop = true;
                ambientCavernSource.playOnAwake = false;
            }
            if (ambientCrossfadeSource == null)
            {
                ambientCrossfadeSource = gameObject.AddComponent<AudioSource>();
                ambientCrossfadeSource.loop = true;
                ambientCrossfadeSource.playOnAwake = false;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
                uiSource.loop = false;
                uiSource.playOnAwake = false;
            }
        }

        public void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(GameConstants.PrefMasterVolume, 0.85f);
            _musicVolume = PlayerPrefs.GetFloat(GameConstants.PrefMusicVolume, 0.65f);
            _sfxVolume = PlayerPrefs.GetFloat(GameConstants.PrefSfxVolume, 0.85f);
            ApplyVolumes();
        }

        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(GameConstants.PrefMasterVolume, _masterVolume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(GameConstants.PrefMusicVolume, _musicVolume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(GameConstants.PrefSfxVolume, _sfxVolume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = _masterVolume * _musicVolume * _musicCrossfadeScale * _activeMusicFadeWeight;
            }
            if (musicCrossfadeSource != null)
            {
                musicCrossfadeSource.volume = _masterVolume * _musicVolume * _musicCrossfadeScale * _outgoingMusicFadeWeight;
            }
            float ambBase = _masterVolume * Mathf.Max(_musicVolume, _sfxVolume * 0.65f) * ambientCavernLevel;
            if (ambientCavernSource != null)
            {
                ambientCavernSource.volume = ambBase * (1f - _ambientCrossfadeBalance);
            }
            if (ambientCrossfadeSource != null)
            {
                ambientCrossfadeSource.volume = ambBase * _ambientCrossfadeBalance;
            }
            if (sfxSource != null)
            {
                sfxSource.volume = _masterVolume * _sfxVolume;
            }
            if (uiSource != null)
            {
                uiSource.volume = _masterVolume * _sfxVolume;
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;
            if (_musicCrossfadeCoroutine != null)
            {
                StopCoroutine(_musicCrossfadeCoroutine);
                _musicCrossfadeCoroutine = null;
            }
            if (musicCrossfadeSource != null && musicCrossfadeSource.isPlaying)
            {
                musicCrossfadeSource.Stop();
            }
            _activeMusicFadeWeight = 1f;
            _outgoingMusicFadeWeight = 0f;
            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                ApplyVolumes();
                return;
            }
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = _masterVolume * _musicVolume * _musicCrossfadeScale;
            musicSource.Play();
        }

        public void CrossfadeStageMusic(AudioClip stageClip, int stageIndex, float duration = 1.15f)
        {
            AudioClip resolved = stageClip;
            if (resolved == null && stageBgmClips != null && stageIndex >= 0 && stageIndex < stageBgmClips.Length)
            {
                resolved = stageBgmClips[stageIndex];
            }
            if (resolved == null)
            {
                resolved = bgmMossGrotto;
            }
            CrossfadeMusic(resolved, duration);
        }

        public void CrossfadeMusic(AudioClip targetClip, float duration = 1.15f)
        {
            if (targetClip == null) return;
            EnsureAudioSources();
            if (musicSource == null) return;

            if (musicSource.clip == targetClip && musicSource.isPlaying && _musicCrossfadeCoroutine == null)
            {
                _activeMusicFadeWeight = 1f;
                _outgoingMusicFadeWeight = 0f;
                return;
            }

            if (!musicSource.isPlaying || musicSource.clip == null || duration <= 0.05f || musicCrossfadeSource == null)
            {
                PlayMusic(targetClip);
                return;
            }

            if (_musicCrossfadeCoroutine != null)
            {
                StopCoroutine(_musicCrossfadeCoroutine);
                _musicCrossfadeCoroutine = null;
            }

            // Swap AudioSources so the outgoing track keeps playing seamlessly without any seek/click glitch
            AudioSource outgoing = musicSource;
            musicSource = musicCrossfadeSource;
            musicCrossfadeSource = outgoing;

            _outgoingMusicFadeWeight = Mathf.Clamp01(_activeMusicFadeWeight);
            musicCrossfadeSource.volume = _masterVolume * _musicVolume * _musicCrossfadeScale * _outgoingMusicFadeWeight;

            // Start new map BGM on the active musicSource fading in smoothly from 0 -> 1
            musicSource.Stop();
            musicSource.clip = targetClip;
            musicSource.loop = true;
            _activeMusicFadeWeight = 0f;
            musicSource.volume = 0f;
            musicSource.Play();

            _musicCrossfadeCoroutine = StartCoroutine(CrossfadeMusicRoutine(Mathf.Clamp(duration, 0.25f, 3.0f)));
        }

        private System.Collections.IEnumerator CrossfadeMusicRoutine(float duration)
        {
            float startOutgoing = _outgoingMusicFadeWeight;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                _activeMusicFadeWeight = u;
                _outgoingMusicFadeWeight = startOutgoing * (1f - u);
                yield return null;
            }
            _activeMusicFadeWeight = 1f;
            _outgoingMusicFadeWeight = 0f;
            if (musicCrossfadeSource != null)
            {
                musicCrossfadeSource.volume = 0f;
                musicCrossfadeSource.Stop();
            }
            _musicCrossfadeCoroutine = null;
        }

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0.05f)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * _masterVolume * _sfxVolume);
        }

        private void PlayUIOneShot(AudioClip clip, float volumeScale, float pitchVariance)
        {
            AudioSource src = uiSource != null ? uiSource : sfxSource;
            if (clip == null || src == null) return;
            src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            src.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * _masterVolume * _sfxVolume);
        }

        public void PlayFootstep()
        {
            PlayFootstep(false);
        }

        public void PlayFootstep(bool onBareStone)
        {
            AudioClip chosen = onBareStone
                ? (sfxFootstepStone != null ? sfxFootstepStone : sfxFootstep)
                : (sfxFootstepMoss != null ? sfxFootstepMoss : sfxFootstep);
            bool inTransition = CaveTransitionManager.Instance != null && CaveTransitionManager.Instance.IsTransitioning;
            float baseVol = onBareStone ? 0.48f : 0.44f;
            PlayOneShot(chosen, inTransition ? baseVol * 0.58f : baseVol, 0.09f);
        }

        public void CrossfadeCavernAmbience(bool darkStoneAcoustics, float duration = 0.65f)
        {
            if (ambientCrossfadeSource != null && ambientDarkStoneLoop != null && !ambientCrossfadeSource.isPlaying)
            {
                ambientCrossfadeSource.clip = ambientDarkStoneLoop;
                ambientCrossfadeSource.loop = true;
                ambientCrossfadeSource.volume = 0f;
                ambientCrossfadeSource.Play();
            }

            _usingDarkStoneAmbience = darkStoneAcoustics;
            if (_ambienceCrossfadeCoroutine != null) StopCoroutine(_ambienceCrossfadeCoroutine);
            _ambienceCrossfadeCoroutine = StartCoroutine(CrossfadeAmbienceRoutine(darkStoneAcoustics ? 1f : 0f, Mathf.Clamp(duration, 0.50f, 0.80f)));
        }

        private System.Collections.IEnumerator CrossfadeAmbienceRoutine(float targetWeight, float duration)
        {
            float startWeight = _ambientCrossfadeBalance;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                _ambientCrossfadeBalance = Mathf.Lerp(startWeight, targetWeight, u);
                _musicCrossfadeScale = Mathf.Lerp(0.76f, _usingDarkStoneAmbience ? 0.88f : 1.0f, u);
                ApplyVolumes();
                yield return null;
            }
            _ambientCrossfadeBalance = targetWeight;
            _musicCrossfadeScale = _usingDarkStoneAmbience ? 0.88f : 1.0f;
            ApplyVolumes();
            _ambienceCrossfadeCoroutine = null;
        }

        public void PlayJump() { PlayOneShot(sfxJump, 0.74f, 0.06f); }
        public void PlayDoubleJump() { PlayOneShot(sfxDoubleJump, 0.84f, 0.05f); }
        public void PlayLand() { PlayOneShot(sfxLand, 0.66f, 0.08f); }
        public void PlayDash() { PlayOneShot(sfxDash, 0.84f, 0.06f); }
        public void PlayLedgeMantle() { PlayOneShot(sfxLedgeMantle != null ? sfxLedgeMantle : sfxJump, 0.72f, 0.05f); }
        public void PlayBindCharge() { PlayOneShot(sfxBindCharge != null ? sfxBindCharge : sfxThreadGuard, 0.82f, 0.02f); }
        public void PlayBindBurst() { PlayOneShot(sfxBindBurst != null ? sfxBindBurst : sfxCheckpoint, 0.94f, 0.02f); }

        public void PlayAttackSlash(int comboIndex)
        {
            if (comboIndex == 2 && sfxAttackSlash2 != null) PlayOneShot(sfxAttackSlash2, 0.90f, 0.05f);
            else if (comboIndex >= 3 && sfxAttackSlash3 != null) PlayOneShot(sfxAttackSlash3, 0.96f, 0.04f);
            else PlayOneShot(sfxAttackSlash1, 0.86f, 0.06f);
        }

        public void PlaySilkSpear() { PlayOneShot(sfxSilkSpear, 0.94f, 0.04f); }
        public void PlayThreadGuard() { PlayOneShot(sfxThreadGuard, 0.90f, 0.03f); }
        public void PlayEnemyHit() { PlayOneShot(sfxEnemyHit, 0.92f, 0.08f); }

        /// <summary>
        /// Ethereal pitched crystalline pentatonic string-chime chord for Needolin (00:06:29).
        /// </summary>
        public void PlayNeedolinChord(int phraseIndex)
        {
            if (sfxSource == null) return;
            AudioClip baseChime = sfxBellChime != null ? sfxBellChime : (sfxSkillUnlock != null ? sfxSkillUnlock : sfxCheckpoint);
            if (baseChime == null) return;
            float[] pentatonicPitches = new float[] { 1.00f, 1.122f, 1.260f, 1.498f, 1.335f, 1.122f };
            int safeIdx = phraseIndex < 0 ? -phraseIndex : phraseIndex;
            float pitch = pentatonicPitches[safeIdx % pentatonicPitches.Length];
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(baseChime, 0.68f * _masterVolume * _sfxVolume);
        }

        public void PlayArmorClink(float volumeScale = 0.72f)
        {
            AudioClip clip = sfxArmorClink != null ? sfxArmorClink : sfxUIHover;
            PlayOneShot(clip, volumeScale, 0.07f);
        }

        /// <summary>
        /// Layered brittle shell-cracking "coral crunch" SFX for Coral Furm impacts & defeats
        /// using existing cavern audio hooks (no external audio assets modified).
        /// </summary>
        public void PlayCoralCrunch(bool isDeath = false)
        {
            if (sfxSource == null) return;
            AudioClip crispCrack = sfxIceShatter != null ? sfxIceShatter : (sfxBreakUrn != null ? sfxBreakUrn : sfxArmorClink);
            AudioClip bodyCrunch = sfxBreakUrn != null ? sfxBreakUrn : (sfxBreakWood != null ? sfxBreakWood : sfxEnemyHit);

            float volScale = isDeath ? 1.0f : 0.84f;
            if (bodyCrunch != null)
            {
                sfxSource.pitch = (isDeath ? 0.92f : 1.08f) + Random.Range(-0.07f, 0.07f);
                sfxSource.PlayOneShot(bodyCrunch, volScale * _masterVolume * _sfxVolume);
            }
            if (crispCrack != null && crispCrack != bodyCrunch)
            {
                sfxSource.pitch = (isDeath ? 1.18f : 1.28f) + Random.Range(-0.08f, 0.08f);
                sfxSource.PlayOneShot(crispCrack, (volScale * 0.72f) * _masterVolume * _sfxVolume);
            }
        }

        public void PlayPropBreak(BreakablePropKind kind, float volumeScale = 1f)
        {
            AudioClip chosen;
            float baseVol;
            switch (kind)
            {
                case BreakablePropKind.CrackedBell:
                case BreakablePropKind.BrassCandelabra:
                    chosen = sfxBellChime != null ? sfxBellChime : sfxArmorClink;
                    baseVol = 0.94f;
                    break;
                case BreakablePropKind.RustedGear:
                case BreakablePropKind.RustedChain:
                    chosen = sfxArmorClink != null ? sfxArmorClink : sfxBellChime;
                    baseVol = 0.86f;
                    break;
                case BreakablePropKind.IceCrystalCluster:
                case BreakablePropKind.FrozenWoodPost:
                    chosen = sfxIceShatter != null ? sfxIceShatter : sfxBreakUrn;
                    baseVol = 0.92f;
                    break;
                case BreakablePropKind.WoodenBarrel:
                case BreakablePropKind.RottenLog:
                case BreakablePropKind.FragileWoodPillar:
                case BreakablePropKind.TownCrate:
                case BreakablePropKind.ChoirBench:
                    chosen = sfxBreakWood != null ? sfxBreakWood : sfxEnemyHit;
                    baseVol = 0.88f;
                    break;
                case BreakablePropKind.AncientUrn:
                case BreakablePropKind.HollowBugShell:
                case BreakablePropKind.DryCoralFormation:
                case BreakablePropKind.AncientBonePile:
                case BreakablePropKind.OldLantern:
                case BreakablePropKind.CrackedSentinelStatue:
                case BreakablePropKind.GildedPalaceUrn:
                case BreakablePropKind.CeremonialUrn:
                    chosen = sfxBreakUrn != null ? sfxBreakUrn : sfxArmorClink;
                    baseVol = 0.90f;
                    break;
                case BreakablePropKind.BriarBramble:
                case BreakablePropKind.WebBarricade:
                case BreakablePropKind.SilkSpool:
                case BreakablePropKind.DrySilkCocoon:
                    chosen = sfxBreakFoliage != null ? sfxBreakFoliage : (sfxBreakWood != null ? sfxBreakWood : sfxEnemyHit);
                    baseVol = 0.82f;
                    break;
                default:
                    chosen = sfxBreakFoliage != null ? sfxBreakFoliage : sfxFootstepMoss;
                    baseVol = 0.68f;
                    break;
            }
            PlayOneShot(chosen, baseVol * volumeScale, 0.09f);
        }

        public void PlayEnemyChirp()
        {
            if (Time.time - _lastChirpTime < 1.8f) return;
            _lastChirpTime = Time.time;
            PlayOneShot(sfxEnemyChirp, 0.62f, 0.10f);
        }

        public void PlayEnemyWindup()
        {
            PlayOneShot(sfxEnemyWindup, 0.74f, 0.06f);
        }

        public void PlayEnemyDeath() { PlayOneShot(sfxEnemyDeath, 0.94f, 0.05f); }
        public void PlayPlayerHit() { PlayOneShot(sfxPlayerHit, 0.96f, 0.04f); }
        public void PlayPlayerDeath() { PlayOneShot(sfxPlayerDeath, 0.98f, 0.02f); }

        public void PlayWaterDrip(float volumeScale = 0.20f)
        {
            if (Time.time - _lastDripTime < 0.35f) return;
            _lastDripTime = Time.time;
            PlayOneShot(sfxWaterDrip, volumeScale, 0.14f);
        }

        public void PlayCheckpoint() { PlayOneShot(sfxCheckpoint, 0.90f, 0.02f); }
        public void PlaySkillUnlock() { PlayOneShot(sfxSkillUnlock, 0.94f, 0.01f); }
        public void PlayUpgrade() { PlayOneShot(sfxUpgrade, 0.90f, 0.02f); }
        public void PlayUIClick() { PlayUIOneShot(sfxUIClick, 0.72f, 0.03f); }
        public void PlayUIHover() { PlayUIOneShot(sfxUIHover, 0.44f, 0.03f); }
    }
}
