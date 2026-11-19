import { BiomeId, GameSoundEvent } from './types';

function midiToFreq(midi: number): number {
  return 440 * Math.pow(2, (midi - 69) / 12);
}

const BIOME_BGM: Record<BiomeId, { tempoMs: number; lead: number[]; bass: number[] }> = {
  village: {
    tempoMs: 145,
    lead: [64, 0, 67, 68, 71, 0, 68, 67, 64, 0, 69, 67, 64, 62, 60, 0],
    bass: [40, 40, 47, 40, 43, 43, 47, 43, 40, 40, 45, 40, 43, 42, 40, 40],
  },
  plaza: {
    tempoMs: 132,
    lead: [64, 67, 71, 0, 72, 71, 67, 64, 65, 69, 72, 0, 71, 69, 67, 64],
    bass: [40, 47, 40, 47, 43, 50, 43, 50, 41, 48, 41, 48, 47, 43, 40, 38],
  },
  tower: {
    tempoMs: 125,
    lead: [67, 0, 70, 74, 72, 0, 70, 67, 65, 0, 69, 72, 70, 69, 67, 0],
    bass: [43, 43, 50, 43, 46, 46, 53, 46, 41, 41, 48, 41, 46, 45, 43, 43],
  },
  church: {
    tempoMs: 118,
    lead: [64, 64, 68, 71, 72, 0, 71, 68, 64, 68, 71, 76, 75, 72, 71, 68],
    bass: [40, 40, 47, 40, 44, 44, 51, 44, 40, 40, 47, 40, 45, 47, 44, 40],
  },
};

export class RetroAudio {
  private ctx: AudioContext | null = null;
  private masterGain: GainNode | null = null;
  private muted = false;
  private bgmTimer: number | null = null;
  private bgmBiome: BiomeId = 'village';
  private bgmStep = 0;
  private bgmPlaying = false;

  public isMuted(): boolean {
    return this.muted;
  }

  public setMuted(muted: boolean): void {
    this.muted = muted;
    if (this.masterGain && this.ctx) {
      this.masterGain.gain.setValueAtTime(muted ? 0 : 0.38, this.ctx.currentTime);
    }
  }

  public unlock(): void {
    if (typeof window === 'undefined') return;
    const AudioCtx =
      window.AudioContext ||
      (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtx) return;

    if (!this.ctx) {
      this.ctx = new AudioCtx();
      this.masterGain = this.ctx.createGain();
      this.masterGain.gain.value = this.muted ? 0 : 0.38;
      this.masterGain.connect(this.ctx.destination);
    }

    if (this.ctx.state === 'suspended') {
      void this.ctx.resume();
    }
  }

  public startBgm(biome: BiomeId): void {
    this.bgmBiome = biome;
    if (this.bgmPlaying) return;
    this.bgmPlaying = true;
    this.bgmStep = 0;
    this.scheduleNextBgmTick();
  }

  public stopBgm(): void {
    this.bgmPlaying = false;
    if (this.bgmTimer !== null && typeof window !== 'undefined') {
      window.clearTimeout(this.bgmTimer);
      this.bgmTimer = null;
    }
  }

  private scheduleNextBgmTick(): void {
    if (!this.bgmPlaying || typeof window === 'undefined') return;
    const pattern = BIOME_BGM[this.bgmBiome];

    if (this.ctx && this.masterGain && !this.muted && this.ctx.state === 'running') {
      const idx = this.bgmStep % pattern.lead.length;
      const leadNote = pattern.lead[idx];
      const bassNote = pattern.bass[idx];
      const now = this.ctx.currentTime;

      if (leadNote > 0) {
        this.playToneAt(now, midiToFreq(leadNote), 0.1, 'square', 0.05);
      }
      if (bassNote > 0) {
        this.playToneAt(now, midiToFreq(bassNote), 0.11, 'triangle', 0.08);
      }
      if (idx % 4 === 2) {
        this.playNoiseAt(now, 0.035, 0.025, 3200);
      }
    }

    this.bgmStep++;
    this.bgmTimer = window.setTimeout(() => {
      this.scheduleNextBgmTick();
    }, pattern.tempoMs);
  }

  public playSound(event: GameSoundEvent): void {
    if (this.muted) return;
    this.unlock();
    if (!this.ctx || !this.masterGain) return;

    const now = this.ctx.currentTime;

    switch (event) {
      case 'jump_male':
        // Leon: deep heroic exertion grunt + crisp jump sweep
        this.playFormantSweep(now, 145, 225, 0.11, 'sawtooth', 750, 0.13);
        this.playSweep(now, 210, 440, 0.09, 'square', 0.08);
        break;
      case 'double_jump_male':
        this.playFormantSweep(now, 175, 275, 0.11, 'sawtooth', 900, 0.13);
        this.playSweep(now, 310, 620, 0.1, 'square', 0.09);
        break;
      case 'jump_female':
        // Ada: higher agile vocal exertion + sleek upward chirp
        this.playFormantSweep(now, 330, 510, 0.11, 'triangle', 1450, 0.15);
        this.playSweep(now, 390, 740, 0.09, 'sine', 0.09);
        break;
      case 'double_jump_female':
        this.playFormantSweep(now, 390, 620, 0.11, 'triangle', 1650, 0.15);
        this.playSweep(now, 480, 920, 0.1, 'sine', 0.1);
        break;
      case 'wall_jump':
        this.playSweep(now, 290, 580, 0.09, 'triangle', 0.13);
        break;
      case 'dash':
        this.playNoiseAt(now, 0.12, 0.13, 1800);
        this.playSweep(now, 400, 170, 0.12, 'sawtooth', 0.08);
        break;
      case 'shoot_pistol':
        // Crisp 9mm gunfire crack + metallic transient
        this.playNoiseAt(now, 0.08, 0.19, 3200);
        this.playSweep(now, 780, 110, 0.085, 'square', 0.13);
        this.playToneAt(now + 0.03, 1450, 0.03, 'triangle', 0.05);
        break;
      case 'shoot_shotgun':
        // Heavy 12-gauge thunderous blast + pump-action rack
        this.playNoiseAt(now, 0.22, 0.28, 1700);
        this.playSweep(now, 240, 52, 0.2, 'sawtooth', 0.19);
        this.playNoiseAt(now + 0.24, 0.04, 0.08, 2800);
        this.playNoiseAt(now + 0.3, 0.04, 0.09, 3400);
        break;
      case 'shoot_mg':
        // Punchy Metal Slug heavy machine gun bark
        this.playNoiseAt(now, 0.055, 0.15, 3000);
        this.playSweep(now, 680, 175, 0.055, 'sawtooth', 0.1);
        break;
      case 'shoot_rpg':
        // Roaring rocket ignition whoosh
        this.playNoiseAt(now, 0.25, 0.22, 1300);
        this.playSweep(now, 260, 88, 0.24, 'sawtooth', 0.17);
        break;
      case 'reload':
        // Crisp magazine click + bolt rack clack
        this.playToneAt(now, 740, 0.04, 'triangle', 0.1);
        this.playNoiseAt(now + 0.04, 0.035, 0.09, 3200);
        this.playToneAt(now + 0.14, 1180, 0.05, 'square', 0.09);
        break;
      case 'melee':
        this.playNoiseAt(now, 0.09, 0.14, 3600);
        this.playSweep(now, 480, 210, 0.08, 'triangle', 0.11);
        break;
      case 'parry':
        this.playToneAt(now, 1174.66, 0.06, 'square', 0.16);
        this.playToneAt(now + 0.04, 1760, 0.14, 'triangle', 0.15);
        break;
      case 'grenade_throw':
        this.playSweep(now, 220, 420, 0.09, 'triangle', 0.11);
        break;
      case 'grenade_bounce':
        this.playToneAt(now, 260, 0.04, 'triangle', 0.12);
        break;
      case 'explosion':
        this.playNoiseAt(now, 0.38, 0.28, 850);
        this.playSweep(now, 145, 32, 0.36, 'sawtooth', 0.22);
        break;
      case 'barrel_explosion':
        // Huge barrel detonation: deep sub-bass boom + metallic clang + roaring fireball
        this.playNoiseAt(now, 0.48, 0.32, 950);
        this.playSweep(now, 120, 24, 0.45, 'sawtooth', 0.25);
        this.playToneAt(now, 520, 0.08, 'square', 0.09);
        this.playSweep(now + 0.06, 280, 55, 0.32, 'triangle', 0.16);
        break;
      case 'coin':
        this.playToneAt(now, 987.77, 0.06, 'square', 0.11);
        this.playToneAt(now + 0.055, 1318.51, 0.13, 'square', 0.11);
        break;
      case 'shard':
        this.playToneAt(now, 587.33, 0.08, 'square', 0.11);
        this.playToneAt(now + 0.07, 739.99, 0.08, 'square', 0.11);
        this.playToneAt(now + 0.14, 880, 0.08, 'square', 0.11);
        this.playToneAt(now + 0.21, 1174.66, 0.2, 'square', 0.12);
        break;
      case 'medkit':
        this.playSweep(now, 440, 880, 0.22, 'sine', 0.15);
        this.playToneAt(now + 0.1, 659.25, 0.16, 'triangle', 0.12);
        break;
      case 'weapon_pickup':
        this.playToneAt(now, 523.25, 0.06, 'square', 0.13);
        this.playToneAt(now + 0.06, 659.25, 0.06, 'square', 0.13);
        this.playToneAt(now + 0.12, 783.99, 0.14, 'square', 0.14);
        break;
      case 'enemy_hit':
        this.playNoiseAt(now, 0.05, 0.1, 1400);
        this.playSweep(now, 220, 110, 0.07, 'sawtooth', 0.11);
        break;
      case 'enemy_die_fork':
        // Raspy eerie female Ganado death groan
        this.playFormantSweep(now, 260, 82, 0.34, 'sawtooth', 950, 0.16);
        this.playNoiseAt(now + 0.18, 0.1, 0.08, 600);
        break;
      case 'enemy_die_axe':
        // Deep gravelly male Ganado death groan
        this.playFormantSweep(now, 155, 48, 0.4, 'sawtooth', 620, 0.18);
        this.playNoiseAt(now + 0.22, 0.12, 0.1, 500);
        break;
      case 'boss_chainsaw':
        // Dr. Salvador chainsaw revving roar
        this.playFormantSweep(now, 85, 195, 0.22, 'sawtooth', 780, 0.17);
        this.playFormantSweep(now + 0.18, 195, 110, 0.22, 'sawtooth', 850, 0.16);
        this.playNoiseAt(now, 0.36, 0.12, 1600);
        break;
      case 'enemy_die_boss':
        // Massive echoing Dr. Salvador collapse roar + chainsaw clatter
        this.playFormantSweep(now, 125, 32, 0.72, 'sawtooth', 520, 0.24);
        this.playNoiseAt(now + 0.2, 0.38, 0.18, 650);
        this.playToneAt(now + 0.35, 340, 0.08, 'square', 0.09);
        break;
      case 'hurt':
        this.playSweep(now, 260, 85, 0.18, 'sawtooth', 0.17);
        this.playNoiseAt(now, 0.1, 0.12, 1000);
        break;
      case 'checkpoint':
        this.playToneAt(now, 523.25, 0.08, 'triangle', 0.14);
        this.playToneAt(now + 0.08, 659.25, 0.08, 'triangle', 0.14);
        this.playToneAt(now + 0.16, 783.99, 0.18, 'triangle', 0.14);
        break;
      case 'spring':
        this.playSweep(now, 200, 740, 0.18, 'triangle', 0.15);
        break;
      case 'merchant_greet':
        this.playToneAt(now, 329.63, 0.12, 'triangle', 0.12);
        this.playToneAt(now + 0.08, 392.0, 0.12, 'triangle', 0.12);
        this.playToneAt(now + 0.16, 493.88, 0.24, 'triangle', 0.13);
        break;
      case 'merchant_buy':
        this.playToneAt(now, 1046.5, 0.06, 'square', 0.12);
        this.playToneAt(now + 0.055, 1318.5, 0.07, 'square', 0.12);
        this.playToneAt(now + 0.12, 1567.98, 0.16, 'triangle', 0.14);
        break;
      case 'victory':
        this.playToneAt(now, 523.25, 0.1, 'square', 0.12);
        this.playToneAt(now + 0.1, 659.25, 0.1, 'square', 0.12);
        this.playToneAt(now + 0.2, 783.99, 0.1, 'square', 0.12);
        this.playToneAt(now + 0.3, 1046.5, 0.34, 'square', 0.14);
        break;
      case 'gameover':
        this.playToneAt(now, 392, 0.14, 'sawtooth', 0.12);
        this.playToneAt(now + 0.15, 349.23, 0.14, 'sawtooth', 0.12);
        this.playToneAt(now + 0.3, 311.13, 0.16, 'sawtooth', 0.12);
        this.playToneAt(now + 0.46, 261.63, 0.32, 'sawtooth', 0.13);
        break;
    }
  }

  private playFormantSweep(
    startTime: number,
    startFreq: number,
    endFreq: number,
    duration: number,
    type: OscillatorType,
    filterHz: number,
    volume: number
  ): void {
    if (!this.ctx || !this.masterGain) return;
    const osc = this.ctx.createOscillator();
    const filter = this.ctx.createBiquadFilter();
    const gain = this.ctx.createGain();

    osc.type = type;
    osc.frequency.setValueAtTime(startFreq, startTime);
    osc.frequency.exponentialRampToValueAtTime(Math.max(20, endFreq), startTime + duration);

    filter.type = 'bandpass';
    filter.frequency.setValueAtTime(filterHz, startTime);
    filter.Q.setValueAtTime(2.2, startTime);

    gain.gain.setValueAtTime(0.0001, startTime);
    gain.gain.linearRampToValueAtTime(volume, startTime + 0.015);
    gain.gain.exponentialRampToValueAtTime(0.0001, startTime + duration);

    osc.connect(filter);
    filter.connect(gain);
    gain.connect(this.masterGain);
    osc.start(startTime);
    osc.stop(startTime + duration + 0.02);
  }

  private playToneAt(
    startTime: number,
    freq: number,
    duration: number,
    type: OscillatorType,
    volume: number
  ): void {
    if (!this.ctx || !this.masterGain) return;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    osc.type = type;
    osc.frequency.setValueAtTime(freq, startTime);
    gain.gain.setValueAtTime(0.0001, startTime);
    gain.gain.linearRampToValueAtTime(volume, startTime + 0.008);
    gain.gain.exponentialRampToValueAtTime(0.0001, startTime + duration);
    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(startTime);
    osc.stop(startTime + duration + 0.02);
  }

  private playSweep(
    startTime: number,
    startFreq: number,
    endFreq: number,
    duration: number,
    type: OscillatorType,
    volume: number
  ): void {
    if (!this.ctx || !this.masterGain) return;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    osc.type = type;
    osc.frequency.setValueAtTime(startFreq, startTime);
    osc.frequency.exponentialRampToValueAtTime(Math.max(20, endFreq), startTime + duration);
    gain.gain.setValueAtTime(0.0001, startTime);
    gain.gain.linearRampToValueAtTime(volume, startTime + 0.008);
    gain.gain.exponentialRampToValueAtTime(0.0001, startTime + duration);
    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(startTime);
    osc.stop(startTime + duration + 0.02);
  }

  private playNoiseAt(
    startTime: number,
    duration: number,
    volume: number,
    cutoffHz: number
  ): void {
    if (!this.ctx || !this.masterGain) return;
    const sampleCount = Math.max(1, Math.floor(this.ctx.sampleRate * duration));
    const buffer = this.ctx.createBuffer(1, sampleCount, this.ctx.sampleRate);
    const data = buffer.getChannelData(0);
    for (let i = 0; i < sampleCount; i++) {
      data[i] = Math.random() * 2 - 1;
    }

    const source = this.ctx.createBufferSource();
    source.buffer = buffer;

    const filter = this.ctx.createBiquadFilter();
    filter.type = 'lowpass';
    filter.frequency.setValueAtTime(cutoffHz, startTime);

    const gain = this.ctx.createGain();
    gain.gain.setValueAtTime(volume, startTime);
    gain.gain.exponentialRampToValueAtTime(0.0001, startTime + duration);

    source.connect(filter);
    filter.connect(gain);
    gain.connect(this.masterGain);
    source.start(startTime);
    source.stop(startTime + duration + 0.01);
  }
}
