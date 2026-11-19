import { RetroAudio } from './audio';
import { ControlAction, ControlBindings, loadKeyBindings } from './controls';
import { LEVELS } from './levels';
import { renderWorld } from './renderer';
import { purchaseShopOffer, ShopOfferId } from './shop';
import { GameAssets } from './sprites';
import { createDefaultUpgrades, loadSaveData, saveLevelVictory, saveSelectedDifficulty } from './storage';
import {
  DifficultyId,
  FIXED_DT,
  HeroId,
  InputState,
  VIRTUAL_HEIGHT,
  VIRTUAL_WIDTH,
  WorldState,
} from './types';
import {
  advanceMissionRun,
  createNextMissionWorld,
  createRunProgressState,
  createWorld,
  exitMissionRun,
  respawnPlayerAtCheckpoint,
  retryCurrentMissionRun,
  RunProgressState,
  startFreshMissionRun,
  stepWorld,
  switchHeroInRun,
} from './world';

export type EngineMode = 'title' | 'playing' | 'paused' | 'shop' | 'victory' | 'gameover';

export interface EngineCallbacks {
  onModeChange: (mode: EngineMode, world: WorldState) => void;
  onNearMerchantChange?: (near: boolean) => void;
  onTitleTick?: (elapsedTime: number) => void;
  onShopTick?: (elapsedTime: number) => void;
}

export class GameEngine {
  private canvas: HTMLCanvasElement;
  private ctx: CanvasRenderingContext2D;
  private assets: GameAssets;
  private audio: RetroAudio;
  private callbacks: EngineCallbacks;

  private bindings: ControlBindings;
  private capturingKey = false;
  private selectedHero: HeroId = 'leon';
  private selectedDifficulty: DifficultyId;
  private runState: RunProgressState;
  private world: WorldState;
  private mode: EngineMode = 'title';
  private rafId: number | null = null;
  private lastTimestamp = 0;
  private accumulator = 0;
  private shopAnimTime = 0;
  private prevNearMerchant = false;

  private keysDown = new Set<string>();
  private queued = {
    jump: false,
    shoot: false,
    melee: false,
    grenade: false,
    dash: false,
    heal: false,
    reload: false,
    switchWeapon: false,
    switchWeaponDir: 1 as -1 | 1,
    selectWeaponSlot: 0 as 0 | 1 | 2 | 3 | 4,
    interact: false,
  };

  constructor(
    canvas: HTMLCanvasElement,
    assets: GameAssets,
    audio: RetroAudio,
    callbacks: EngineCallbacks
  ) {
    this.canvas = canvas;
    const ctx = canvas.getContext('2d');
    if (!ctx) {
      throw new Error('Could not initialize 2D canvas context');
    }
    this.ctx = ctx;
    this.assets = assets;
    this.audio = audio;
    this.callbacks = callbacks;
    this.bindings = loadKeyBindings();
    this.selectedDifficulty = loadSaveData().selectedDifficulty;
    this.runState = createRunProgressState(this.selectedHero);
    this.world = createWorld(LEVELS[0], this.selectedHero, {
      difficulty: this.selectedDifficulty,
      upgrades: createDefaultUpgrades(),
    });

    this.handleKeyDown = this.handleKeyDown.bind(this);
    this.handleKeyUp = this.handleKeyUp.bind(this);
    this.handleWheel = this.handleWheel.bind(this);
    this.syncDisplayResolution = this.syncDisplayResolution.bind(this);
    this.loop = this.loop.bind(this);
  }

  public start(): void {
    window.addEventListener('keydown', this.handleKeyDown);
    window.addEventListener('keyup', this.handleKeyUp);
    window.addEventListener('wheel', this.handleWheel, { passive: false });
    window.addEventListener('resize', this.syncDisplayResolution);
    document.addEventListener('fullscreenchange', this.syncDisplayResolution);
    this.syncDisplayResolution();
    this.lastTimestamp = performance.now();
    this.rafId = window.requestAnimationFrame(this.loop);
  }

  public destroy(): void {
    window.removeEventListener('keydown', this.handleKeyDown);
    window.removeEventListener('keyup', this.handleKeyUp);
    window.removeEventListener('wheel', this.handleWheel);
    window.removeEventListener('resize', this.syncDisplayResolution);
    document.removeEventListener('fullscreenchange', this.syncDisplayResolution);
    if (this.rafId !== null) {
      window.cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
    this.audio.stopBgm();
  }

  public syncDisplayResolution(): void {
    const rect = this.canvas.getBoundingClientRect();
    const dpr = Math.max(1, window.devicePixelRatio || 1);
    const targetW = Math.max(VIRTUAL_WIDTH * 2, Math.round(rect.width * dpr));
    const targetH = Math.max(VIRTUAL_HEIGHT * 2, Math.round(rect.height * dpr));
    if (this.canvas.width !== targetW || this.canvas.height !== targetH) {
      this.canvas.width = targetW;
      this.canvas.height = targetH;
      this.ctx.imageSmoothingEnabled = false;
    }
  }

  public getBindings(): ControlBindings {
    return this.bindings;
  }

  public setBindings(bindings: ControlBindings): void {
    this.bindings = bindings;
    this.keysDown.clear();
    this.clearQueuedInputs();
  }

  public setCapturingKey(capturing: boolean): void {
    this.capturingKey = capturing;
    if (capturing) {
      this.keysDown.clear();
      this.clearQueuedInputs();
    }
  }

  private matchesAction(action: ControlAction, code: string): boolean {
    return this.bindings[action].includes(code);
  }

  private isActionDown(action: ControlAction): boolean {
    for (const code of this.bindings[action]) {
      if (this.keysDown.has(code)) return true;
    }
    return false;
  }

  public getMode(): EngineMode {
    return this.mode;
  }

  public getWorld(): WorldState {
    return this.world;
  }

  public getSelectedHero(): HeroId {
    return this.selectedHero;
  }

  public getSelectedDifficulty(): DifficultyId {
    return this.selectedDifficulty;
  }

  public setSelectedDifficulty(difficulty: DifficultyId): void {
    this.selectedDifficulty = difficulty;
    saveSelectedDifficulty(difficulty);
    if (this.mode === 'title') {
      this.world = createWorld(LEVELS[0], this.selectedHero, {
        difficulty,
        upgrades: createDefaultUpgrades(),
      });
    }
  }

  public setSelectedHero(hero: HeroId): void {
    switchHeroInRun(this.runState, hero);
    this.selectedHero = hero;
    if (this.mode === 'title') {
      this.world = createWorld(LEVELS[0], hero, {
        difficulty: this.selectedDifficulty,
        upgrades: createDefaultUpgrades(),
      });
    }
  }

  private enterPlayingWorld(world: WorldState): void {
    this.world = world;
    this.clearQueuedInputs();
    this.prevNearMerchant = false;
    this.callbacks.onNearMerchantChange?.(false);
    this.mode = 'playing';
    this.audio.unlock();
    this.audio.startBgm(world.level.def.biome);
    this.callbacks.onModeChange('playing', this.world);
  }

  public startLevel(levelIndex: number): void {
    const def = LEVELS[levelIndex] ?? LEVELS[0];
    startFreshMissionRun(this.runState, def.index);
    const nextWorld = createWorld(def, this.selectedHero, {
      difficulty: this.selectedDifficulty,
      upgrades: createDefaultUpgrades(),
    });
    this.enterPlayingWorld(nextWorld);
  }

  public advanceToNextLevel(): void {
    const nextIndex = this.world.level.def.index + 1;
    const nextDef = LEVELS[nextIndex];
    if (!nextDef) {
      this.returnToTitle();
      return;
    }
    advanceMissionRun(this.runState, this.world, nextIndex);
    const nextWorld = createNextMissionWorld(this.world, nextDef);
    this.enterPlayingWorld(nextWorld);
  }

  public restartCurrentLevel(): void {
    const def = this.world.level.def;
    retryCurrentMissionRun(this.runState);
    const retryWorld = createWorld(def, this.selectedHero, {
      difficulty: this.selectedDifficulty,
      pesetas: this.runState.entryPesetas,
      upgrades: { ...this.runState.entryUpgrades },
    });
    this.enterPlayingWorld(retryWorld);
  }

  public restartFromCheckpoint(): void {
    if (this.mode !== 'playing' && this.mode !== 'paused') return;
    respawnPlayerAtCheckpoint(this.world);
    this.clearQueuedInputs();
    this.mode = 'playing';
    this.audio.unlock();
    this.audio.startBgm(this.world.level.def.biome);
    this.callbacks.onModeChange('playing', this.world);
  }

  public togglePause(): void {
    if (this.mode === 'playing') {
      this.mode = 'paused';
      this.audio.stopBgm();
      this.callbacks.onModeChange('paused', this.world);
    } else if (this.mode === 'paused') {
      this.mode = 'playing';
      this.audio.startBgm(this.world.level.def.biome);
      this.callbacks.onModeChange('playing', this.world);
    } else if (this.mode === 'shop') {
      this.closeShop();
    }
  }

  public openShop(): void {
    if (this.mode !== 'playing' || !this.world.nearMerchantId) return;
    this.world.shopOpenRequested = false;
    this.mode = 'shop';
    this.shopAnimTime = 0;
    this.audio.unlock();
    this.audio.playSound('merchant_greet');
    this.callbacks.onModeChange('shop', this.world);
  }

  public closeShop(): void {
    if (this.mode !== 'shop') return;
    this.clearQueuedInputs();
    this.mode = 'playing';
    this.audio.startBgm(this.world.level.def.biome);
    this.callbacks.onModeChange('playing', this.world);
  }

  public buyShopOffer(offerId: ShopOfferId): { ok: boolean; message: string } {
    const res = purchaseShopOffer(this.world, offerId);
    if (res.ok) {
      this.runState.currentPesetas = this.world.pesetas;
      this.runState.currentUpgrades = { ...this.world.upgrades };
    }
    while (this.world.pendingSounds.length > 0) {
      const sound = this.world.pendingSounds.shift();
      if (sound) this.audio.playSound(sound);
    }
    return res;
  }

  public returnToTitle(): void {
    exitMissionRun(this.runState);
    this.mode = 'title';
    this.audio.stopBgm();
    this.prevNearMerchant = false;
    this.callbacks.onNearMerchantChange?.(false);
    this.world = createWorld(LEVELS[0], this.selectedHero, {
      difficulty: this.selectedDifficulty,
      upgrades: createDefaultUpgrades(),
    });
    this.callbacks.onModeChange('title', this.world);
  }

  private clearQueuedInputs(): void {
    this.queued.jump = false;
    this.queued.shoot = false;
    this.queued.melee = false;
    this.queued.grenade = false;
    this.queued.dash = false;
    this.queued.heal = false;
    this.queued.reload = false;
    this.queued.switchWeapon = false;
    this.queued.switchWeaponDir = 1;
    this.queued.selectWeaponSlot = 0;
    this.queued.interact = false;
  }

  private handleWheel(e: WheelEvent): void {
    if (this.mode !== 'playing' || this.capturingKey) return;
    if (Math.abs(e.deltaY) < 1) return;
    e.preventDefault();
    this.audio.unlock();
    this.queued.switchWeapon = true;
    this.queued.switchWeaponDir = e.deltaY > 0 ? 1 : -1;
  }

  private handleKeyDown(e: KeyboardEvent): void {
    if (this.capturingKey) return;

    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Space', 'Tab'].includes(e.code)) {
      e.preventDefault();
    }
    if (e.repeat) return;

    this.audio.unlock();
    this.keysDown.add(e.code);

    if (this.mode === 'playing') {
      if (e.code === 'Digit1') this.queued.selectWeaponSlot = 1;
      else if (e.code === 'Digit2') this.queued.selectWeaponSlot = 2;
      else if (e.code === 'Digit3') this.queued.selectWeaponSlot = 3;
      else if (e.code === 'Digit4') this.queued.selectWeaponSlot = 4;
    }

    if (this.matchesAction('jump', e.code)) {
      this.queued.jump = true;
    }
    if (this.matchesAction('shoot', e.code)) {
      this.queued.shoot = true;
    }
    if (this.matchesAction('melee', e.code)) {
      this.queued.melee = true;
    }
    if (this.matchesAction('grenade', e.code)) {
      this.queued.grenade = true;
    }
    if (this.matchesAction('dash', e.code)) {
      this.queued.dash = true;
    }
    if (this.matchesAction('heal', e.code)) {
      this.queued.heal = true;
    }
    if (this.matchesAction('reload', e.code)) {
      this.queued.reload = true;
    }
    if (this.matchesAction('switchWeapon', e.code)) {
      this.queued.switchWeapon = true;
      this.queued.switchWeaponDir = 1;
    }
    if (this.matchesAction('interact', e.code)) {
      if (this.mode === 'playing') {
        this.queued.interact = true;
      } else if (this.mode === 'shop') {
        this.closeShop();
      }
    }

    if (this.matchesAction('pause', e.code)) {
      if (this.mode === 'playing' || this.mode === 'paused' || this.mode === 'shop') {
        this.togglePause();
      }
    } else if (this.matchesAction('restartCp', e.code)) {
      if (this.mode === 'playing' || this.mode === 'paused') {
        this.restartFromCheckpoint();
      } else if (this.mode === 'gameover') {
        this.restartCurrentLevel();
      }
    }
  }

  private handleKeyUp(e: KeyboardEvent): void {
    this.keysDown.delete(e.code);
  }

  private sampleInput(): InputState {
    const left = this.isActionDown('left');
    const right = this.isActionDown('right');
    const up = this.isActionDown('up');
    const down = this.isActionDown('down');
    const jumpHeld = this.isActionDown('jump');
    const shootHeld = this.isActionDown('shoot');

    const input: InputState = {
      left,
      right,
      up,
      down,
      jumpHeld,
      jumpPressed: this.queued.jump,
      shootHeld,
      shootPressed: this.queued.shoot,
      meleePressed: this.queued.melee,
      grenadePressed: this.queued.grenade,
      dashPressed: this.queued.dash,
      healPressed: this.queued.heal,
      reloadPressed: this.queued.reload,
      switchWeaponPressed: this.queued.switchWeapon,
      switchWeaponDir: this.queued.switchWeaponDir,
      selectWeaponSlot: this.queued.selectWeaponSlot,
      interactPressed: this.queued.interact,
    };

    this.clearQueuedInputs();
    return input;
  }

  private loop(timestamp: number): void {
    const dt = Math.min(0.1, Math.max(0, (timestamp - this.lastTimestamp) / 1000));
    this.lastTimestamp = timestamp;

    if (this.mode === 'title') {
      this.world.elapsedTime += dt;
      this.world.camera.x =
        (Math.sin(this.world.elapsedTime * 0.14) * 0.5 + 0.5) *
        Math.max(0, this.world.level.width - 480);
      renderWorld(this.ctx, this.world, this.assets, false);
      this.callbacks.onTitleTick?.(this.world.elapsedTime);
    } else if (this.mode === 'shop') {
      this.shopAnimTime += dt;
      renderWorld(this.ctx, this.world, this.assets, true);
      this.callbacks.onShopTick?.(this.shopAnimTime);
    } else if (this.mode === 'playing') {
      this.accumulator += dt;
      while (this.accumulator >= FIXED_DT) {
        const input = this.sampleInput();
        stepWorld(this.world, input, FIXED_DT);
        this.accumulator -= FIXED_DT;

        while (this.world.pendingSounds.length > 0) {
          const sound = this.world.pendingSounds.shift();
          if (sound) this.audio.playSound(sound);
        }

        const isNear = Boolean(this.world.nearMerchantId);
        if (isNear !== this.prevNearMerchant) {
          this.prevNearMerchant = isNear;
          this.callbacks.onNearMerchantChange?.(isNear);
        }

        if (this.world.shopOpenRequested) {
          this.world.shopOpenRequested = false;
          this.mode = 'shop';
          this.shopAnimTime = 0;
          this.callbacks.onModeChange('shop', this.world);
          break;
        }

        if (this.world.status === 'victory') {
          this.mode = 'victory';
          this.audio.stopBgm();
          saveLevelVictory(
            this.world.level.def.index,
            this.world.level.def.id,
            this.world.elapsedTime,
            this.world.score,
            this.world.shardsCollected,
            this.world.pesetas,
            this.world.upgrades
          );
          this.callbacks.onModeChange('victory', this.world);
          break;
        } else if (this.world.status === 'gameover') {
          this.mode = 'gameover';
          this.audio.stopBgm();
          this.callbacks.onModeChange('gameover', this.world);
          break;
        }
      }
      renderWorld(this.ctx, this.world, this.assets, true);
    } else {
      renderWorld(this.ctx, this.world, this.assets, true);
    }

    this.rafId = window.requestAnimationFrame(this.loop);
  }
}
