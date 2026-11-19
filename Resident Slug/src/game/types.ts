export const TILE_SIZE = 16;
export const VIRTUAL_WIDTH = 480;
export const VIRTUAL_HEIGHT = 270;
export const FIXED_DT = 1 / 60;

export type HeroId = 'leon' | 'ada';
export type DifficultyId = 'easy' | 'normal' | 'hardcore' | 'professional';
export type WeaponId = 'pistol' | 'shotgun' | 'mg' | 'rpg';
export type BiomeId = 'village' | 'plaza' | 'tower' | 'church';

export interface DifficultyConfig {
  id: DifficultyId;
  label: string;
  shortLabel: string;
  desc: string;
  enemyHpMult: number;
  bossHp: number;
  enemySpeedMult: number;
  rewardMult: number;
  color: string;
}

export const DIFFICULTY_ORDER: DifficultyId[] = ['easy', 'normal', 'hardcore', 'professional'];

export const DIFFICULTY_CONFIG: Record<DifficultyId, DifficultyConfig> = {
  easy: {
    id: 'easy',
    label: 'EASY',
    shortLabel: 'EASY',
    desc: 'Musuh lebih sedikit • HP Musuh 0.75× • Boss 16 HP',
    enemyHpMult: 0.75,
    bossHp: 16,
    enemySpeedMult: 0.85,
    rewardMult: 1.0,
    color: '#5ce67e',
  },
  normal: {
    id: 'normal',
    label: 'NORMAL',
    shortLabel: 'NORMAL',
    desc: 'Standar seimbang • HP Musuh 1× • Boss 24 HP',
    enemyHpMult: 1.0,
    bossHp: 24,
    enemySpeedMult: 1.0,
    rewardMult: 1.0,
    color: '#4deeea',
  },
  hardcore: {
    id: 'hardcore',
    label: 'HARDCORE',
    shortLabel: 'HARD',
    desc: 'Musuh ekstra • HP Musuh 1.5× • Boss 36 HP • PTAS 1.25×',
    enemyHpMult: 1.5,
    bossHp: 36,
    enemySpeedMult: 1.15,
    rewardMult: 1.25,
    color: '#fb923c',
  },
  professional: {
    id: 'professional',
    label: 'PROFESSIONAL',
    shortLabel: 'PRO',
    desc: 'Serbuan penuh! • HP Musuh 2× • Boss 48 HP • PTAS 1.5×',
    enemyHpMult: 2.0,
    bossHp: 48,
    enemySpeedMult: 1.28,
    rewardMult: 1.5,
    color: '#ff5b6e',
  },
};

export type TileKind =
  | 'empty'
  | 'solid'
  | 'oneway'
  | 'spike_up'
  | 'spike_down'
  | 'spike_left'
  | 'spike_right'
  | 'spring';

export type EnemyKind = 'ganado_fork' | 'ganado_axe' | 'boss_salvador';

export type PickupKind =
  | 'coin'
  | 'medkit'
  | 'grenade_box'
  | 'shotgun'
  | 'mg'
  | 'rpg'
  | 'ammo'
  | 'ammo_shotgun'
  | 'ammo_mg'
  | 'shard';

export interface Rect {
  x: number;
  y: number;
  w: number;
  h: number;
}

export interface PlayerUpgrades {
  maxHpLevel: number;
  damageLevel: number;
  agilityLevel: number;
  pouchLevel: number;
}

export interface InputState {
  left: boolean;
  right: boolean;
  up: boolean;
  down: boolean;
  jumpHeld: boolean;
  jumpPressed: boolean;
  shootHeld: boolean;
  shootPressed: boolean;
  meleePressed: boolean;
  grenadePressed: boolean;
  dashPressed: boolean;
  healPressed: boolean;
  switchWeaponPressed: boolean;
  selectWeaponSlot?: 0 | 1 | 2 | 3 | 4;
  switchWeaponDir?: -1 | 1;
  reloadPressed?: boolean;
  interactPressed: boolean;
}

export interface HeroStats {
  id: HeroId;
  name: string;
  role: string;
  maxHp: number;
  speedMult: number;
  jumpMult: number;
  startGrenades: number;
  startMedkits: number;
  meleeName: string;
}

export const HERO_CONFIG: Record<HeroId, HeroStats> = {
  leon: {
    id: 'leon',
    name: 'Leon S. Kennedy',
    role: 'Tactical Agent • HP Tinggi & Knife Parry',
    maxHp: 5,
    speedMult: 1.0,
    jumpMult: 1.0,
    startGrenades: 2,
    startMedkits: 1,
    meleeName: 'Roundhouse Kick',
  },
  ada: {
    id: 'ada',
    name: 'Ada Wong',
    role: 'Espionage • Lincah, Lompat Tinggi & Kick',
    maxHp: 4,
    speedMult: 1.12,
    jumpMult: 1.08,
    startGrenades: 3,
    startMedkits: 1,
    meleeName: 'Corkscrew Kick',
  },
};

export interface PlayerState {
  hero: HeroId;
  x: number;
  y: number;
  vx: number;
  vy: number;
  w: number;
  h: number;
  standingH: number;
  crouchH: number;
  crouching: boolean;
  facing: 1 | -1;
  aimDirX: number;
  aimDirY: number;
  onGround: boolean;
  onWall: -1 | 0 | 1;
  coyoteTimer: number;
  jumpBufferTimer: number;
  airJumpsLeft: number;
  maxAirJumps: number;
  canDash: boolean;
  dashTimer: number;
  dashCooldown: number;
  dashDirX: number;
  dashDirY: number;
  wallJumpLockTimer: number;
  dropThroughTimer: number;
  invulnTimer: number;
  hurtTimer: number;
  shootTimer: number;
  shootCooldown: number;
  grenadeTimer: number;
  meleeTimer: number;
  meleeCooldown: number;
  meleeDidHit: boolean;
  hp: number;
  maxHp: number;
  medkits: number;
  maxMedkits: number;
  grenades: number;
  damageBonus: number;
  agilityMult: number;
  activeWeapon: WeaponId;
  unlockedWeapons: Record<WeaponId, boolean>;
  mag: Record<WeaponId, number>;
  reserve: Record<WeaponId, number>;
  ammo: Record<WeaponId, number>;
  reloadTimer: number;
  reloadDuration: number;
  animTime: number;
  idleTime: number;
  landingTimer: number;
  standingPlatformId: string | null;
}

export interface MovingPlatform {
  id: string;
  startX: number;
  startY: number;
  endX: number;
  endY: number;
  x: number;
  y: number;
  prevX: number;
  prevY: number;
  w: number;
  h: number;
  speed: number;
  progress: number;
  direction: 1 | -1;
  pauseTimer: number;
  pauseDuration: number;
}

export type EnemyAIState =
  | 'patrol'
  | 'chase'
  | 'windup'
  | 'attack'
  | 'recover'
  | 'hurt'
  | 'dying'
  | 'dead';

export interface Enemy {
  id: string;
  kind: EnemyKind;
  x: number;
  y: number;
  vx: number;
  vy: number;
  w: number;
  h: number;
  facing: 1 | -1;
  hp: number;
  maxHp: number;
  patrolMinX: number;
  patrolMaxX: number;
  animTime: number;
  state: EnemyAIState;
  stateTimer: number;
  attackCooldown: number;
  attackDidHit: boolean;
  hurtFlashTimer: number;
  deathFadeTimer: number;
  scale: number;
}

export interface MerchantStall {
  id: string;
  x: number;
  y: number;
  w: number;
  h: number;
  animTime: number;
}

export type ProjectileKind = 'pistol' | 'pellet' | 'mg' | 'rpg' | 'grenade' | 'enemy_axe';

export interface Projectile {
  id: string;
  kind: ProjectileKind;
  fromPlayer: boolean;
  x: number;
  y: number;
  vx: number;
  vy: number;
  w: number;
  h: number;
  damage: number;
  life: number;
  bouncesLeft: number;
  rotation: number;
  knockbackMult?: number;
}

export interface ExplosiveBarrel {
  id: string;
  x: number;
  y: number;
  w: number;
  h: number;
  hp: number;
  exploded: boolean;
  flashTimer: number;
}

export interface PickupItem {
  id: string;
  kind: PickupKind;
  x: number;
  y: number;
  w: number;
  h: number;
  collected: boolean;
  shardIndex?: number;
  bobOffset: number;
}

export interface Checkpoint {
  id: string;
  x: number;
  y: number;
  w: number;
  h: number;
  activated: boolean;
}

export interface SignHint {
  x: number;
  y: number;
  text: string;
}

export interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  size: number;
  color: string;
  life: number;
  maxLife: number;
  gravity: number;
  sprite?: 'shell_shotgun' | 'shell_cqbr';
  rotation?: number;
  spin?: number;
}

export interface ExplosionEffect {
  x: number;
  y: number;
  radius: number;
  life: number;
  maxLife: number;
}

export interface Afterimage {
  x: number;
  y: number;
  facing: 1 | -1;
  frameIndex: number;
  life: number;
  maxLife: number;
  color: string;
}

export interface FloatingText {
  x: number;
  y: number;
  vy: number;
  text: string;
  color: string;
  life: number;
  maxLife: number;
}

export interface LevelPlatformSpec {
  id: string;
  startCol: number;
  startRow: number;
  endCol: number;
  endRow: number;
  widthTiles: number;
  speed?: number;
  pauseDuration?: number;
}

export interface LevelEnemySpec {
  id: string;
  kind: EnemyKind;
  col: number;
  row: number;
  patrolLeftTiles?: number;
  patrolRightTiles?: number;
  minDifficulty?: 'hardcore' | 'professional';
  easySkip?: boolean;
}

export interface LevelDefinition {
  index: number;
  id: string;
  name: string;
  subtitle: string;
  biome: BiomeId;
  description: string;
  grid: string[];
  platforms: LevelPlatformSpec[];
  enemies: LevelEnemySpec[];
  signs?: SignHint[];
}

export interface ParsedLevel {
  def: LevelDefinition;
  cols: number;
  rows: number;
  width: number;
  height: number;
  tiles: TileKind[][];
  spawn: { x: number; y: number };
  goal: Rect;
  checkpoints: Checkpoint[];
  merchants: MerchantStall[];
  pickups: PickupItem[];
  barrels: ExplosiveBarrel[];
  platforms: MovingPlatform[];
  enemies: Enemy[];
  signs: SignHint[];
}

export type GameSoundEvent =
  | 'jump_male'
  | 'jump_female'
  | 'double_jump_male'
  | 'double_jump_female'
  | 'wall_jump'
  | 'dash'
  | 'shoot_pistol'
  | 'shoot_shotgun'
  | 'shoot_mg'
  | 'shoot_rpg'
  | 'reload'
  | 'melee'
  | 'parry'
  | 'grenade_throw'
  | 'grenade_bounce'
  | 'explosion'
  | 'barrel_explosion'
  | 'coin'
  | 'shard'
  | 'medkit'
  | 'weapon_pickup'
  | 'enemy_hit'
  | 'enemy_die_fork'
  | 'enemy_die_axe'
  | 'enemy_die_boss'
  | 'boss_chainsaw'
  | 'hurt'
  | 'checkpoint'
  | 'spring'
  | 'merchant_greet'
  | 'merchant_buy'
  | 'victory'
  | 'gameover';

export interface WorldState {
  level: ParsedLevel;
  difficulty: DifficultyId;
  player: PlayerState;
  respawnPoint: { x: number; y: number };
  score: number;
  coins: number;
  pesetas: number;
  stageEntryPesetas: number;
  totalCoins: number;
  shardsCollected: boolean[];
  enemiesDefeated: number;
  elapsedTime: number;
  upgrades: PlayerUpgrades;
  nearMerchantId: string | null;
  shopOpenRequested: boolean;
  camera: {
    x: number;
    y: number;
    shakeTimer: number;
    shakeMagnitude: number;
  };
  hitstopTimer: number;
  status: 'playing' | 'victory' | 'gameover';
  gameOverReason: string;
  projectiles: Projectile[];
  explosions: ExplosionEffect[];
  particles: Particle[];
  afterimages: Afterimage[];
  floatingTexts: FloatingText[];
  pendingSounds: GameSoundEvent[];
  goalUnlocked: boolean;
  nextEntitySeq: number;
}

export interface LevelProgress {
  completed: boolean;
  bestTime: number | null;
  bestScore: number;
  shards: [boolean, boolean, boolean];
}

export interface SaveData {
  unlockedLevelIndex: number;
  selectedDifficulty: DifficultyId;
  pesetas: number;
  upgrades: PlayerUpgrades;
  levels: Record<string, LevelProgress>;
}
