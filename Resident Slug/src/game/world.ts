import { damageEnemy, updateEnemies } from './enemies';
import { parseLevel } from './levels';
import {
  checkSpikeCollision,
  getTileAt,
  isSolidTile,
  rectsOverlap,
  stepPlayerPhysics,
  updateMovingPlatforms,
} from './physics';
import { createDefaultUpgrades, loadSaveData, saveWalletAndUpgrades } from './storage';
import {
  DIFFICULTY_CONFIG,
  DifficultyId,
  Enemy,
  HERO_CONFIG,
  HeroId,
  InputState,
  LevelDefinition,
  PlayerState,
  PlayerUpgrades,
  Rect,
  TILE_SIZE,
  VIRTUAL_HEIGHT,
  VIRTUAL_WIDTH,
  WeaponId,
  WorldState,
} from './types';

export const WEAPON_META: Record<
  WeaponId,
  {
    label: string;
    short: string;
    slot: 1 | 2 | 3 | 4;
    magSize: number;
    cooldown: number;
    reloadTime: number;
    color: string;
  }
> = {
  pistol: { label: 'HANDGUN', short: '1', slot: 1, magSize: 12, cooldown: 0.2, reloadTime: 0.45, color: '#ffd447' },
  shotgun: { label: 'SHOTGUN', short: '2', slot: 2, magSize: 8, cooldown: 0.54, reloadTime: 0.8, color: '#fb923c' },
  mg: { label: 'CQBR AR', short: '3', slot: 3, magSize: 30, cooldown: 0.085, reloadTime: 0.85, color: '#4deeea' },
  rpg: { label: 'RPG-7', short: '4', slot: 4, magSize: 1, cooldown: 0.65, reloadTime: 0.36, color: '#ff5b6e' },
};

export const WEAPON_ORDER: WeaponId[] = ['pistol', 'shotgun', 'mg', 'rpg'];

export function syncWeaponAmmo(p: PlayerState, wId: WeaponId): void {
  if (wId === 'pistol') {
    p.unlockedWeapons.pistol = true;
    p.reserve.pistol = Infinity;
    p.ammo.pistol = Infinity;
    p.mag.pistol = Math.max(0, Math.min(WEAPON_META.pistol.magSize, p.mag.pistol));
    return;
  }

  const combined = p.mag[wId] + p.reserve[wId];
  if (p.ammo[wId] !== combined) {
    const total = Math.max(0, p.ammo[wId]);
    if (total > 0) p.unlockedWeapons[wId] = true;
    const magCap = WEAPON_META[wId].magSize;
    p.mag[wId] = Math.min(magCap, total);
    p.reserve[wId] = Math.max(0, total - p.mag[wId]);
  }
  if (p.mag[wId] + p.reserve[wId] > 0) {
    p.unlockedWeapons[wId] = true;
  }
  p.ammo[wId] = p.mag[wId] + p.reserve[wId];
}

export function grantWeaponAmmo(
  p: PlayerState,
  wId: WeaponId,
  amount: number,
  equipIfUnlocked = false,
  unlock = true
): void {
  if (wId === 'pistol') return;
  syncWeaponAmmo(p, wId);
  if (unlock) {
    p.unlockedWeapons[wId] = true;
  }
  if (p.unlockedWeapons[wId]) {
    const magCap = WEAPON_META[wId].magSize;
    const spaceInMag = Math.max(0, magCap - p.mag[wId]);
    const toMag = Math.min(amount, spaceInMag);
    p.mag[wId] += toMag;
    p.reserve[wId] += amount - toMag;
  } else {
    p.reserve[wId] += amount;
  }
  p.ammo[wId] = p.mag[wId] + p.reserve[wId];
  if (equipIfUnlocked && p.unlockedWeapons[wId]) {
    p.activeWeapon = wId;
    p.reloadTimer = 0;
    p.reloadDuration = 0;
  }
}

function createInitialPlayer(
  hero: HeroId,
  spawn: { x: number; y: number },
  upgrades: PlayerUpgrades
): PlayerState {
  const stats = HERO_CONFIG[hero];
  const maxHp = stats.maxHp + upgrades.maxHpLevel;
  const maxMedkits = 3 + upgrades.pouchLevel;
  const startGrenades = stats.startGrenades + upgrades.pouchLevel * 2;
  const agilityMult = 1 + upgrades.agilityLevel * 0.1;

  return {
    hero,
    x: spawn.x,
    y: spawn.y,
    vx: 0,
    vy: 0,
    w: 14,
    h: 34,
    standingH: 34,
    crouchH: 22,
    crouching: false,
    facing: 1,
    aimDirX: 1,
    aimDirY: 0,
    onGround: false,
    onWall: 0,
    coyoteTimer: 0,
    jumpBufferTimer: 0,
    airJumpsLeft: 1,
    maxAirJumps: 1,
    canDash: true,
    dashTimer: 0,
    dashCooldown: 0,
    dashDirX: 1,
    dashDirY: 0,
    wallJumpLockTimer: 0,
    dropThroughTimer: 0,
    invulnTimer: 0,
    hurtTimer: 0,
    shootTimer: 0,
    shootCooldown: 0,
    grenadeTimer: 0,
    meleeTimer: 0,
    meleeCooldown: 0,
    meleeDidHit: false,
    hp: maxHp,
    maxHp,
    medkits: stats.startMedkits,
    maxMedkits,
    grenades: startGrenades,
    damageBonus: upgrades.damageLevel,
    agilityMult,
    activeWeapon: 'pistol',
    unlockedWeapons: {
      pistol: true,
      shotgun: false,
      mg: false,
      rpg: false,
    },
    mag: {
      pistol: WEAPON_META.pistol.magSize,
      shotgun: 0,
      mg: 0,
      rpg: 0,
    },
    reserve: {
      pistol: Infinity,
      shotgun: 0,
      mg: 0,
      rpg: 0,
    },
    ammo: {
      pistol: Infinity,
      shotgun: 0,
      mg: 0,
      rpg: 0,
    },
    reloadTimer: 0,
    reloadDuration: 0,
    animTime: 0,
    idleTime: 0,
    landingTimer: 0,
    standingPlatformId: null,
  };
}

export interface WorldInitOptions {
  pesetas?: number;
  upgrades?: PlayerUpgrades;
  difficulty?: DifficultyId;
}

export function createWorld(
  levelDef: LevelDefinition,
  hero: HeroId = 'leon',
  options?: WorldInitOptions
): WorldState {
  const save = options?.pesetas !== undefined && options?.upgrades !== undefined && options?.difficulty !== undefined ? null : loadSaveData();
  const difficulty: DifficultyId = options?.difficulty ?? save?.selectedDifficulty ?? 'normal';
  const upgrades: PlayerUpgrades = options?.upgrades
    ? { ...options.upgrades }
    : save
      ? { ...save.upgrades }
      : createDefaultUpgrades();
  const initialPesetas = options?.pesetas ?? save?.pesetas ?? 0;

  const level = parseLevel(levelDef, difficulty);
  const player = createInitialPlayer(hero, level.spawn, upgrades);
  const hasBoss = level.enemies.some((e) => e.kind === 'boss_salvador');
  const totalCoins = level.pickups.filter((c) => c.kind === 'coin').length;

  const camX = Math.max(0, Math.min(level.width - VIRTUAL_WIDTH, player.x - VIRTUAL_WIDTH * 0.35));
  const camY = Math.max(0, Math.min(level.height - VIRTUAL_HEIGHT, player.y - VIRTUAL_HEIGHT * 0.6));

  return {
    difficulty,
    level,
    player,
    respawnPoint: { ...level.spawn },
    score: 0,
    coins: 0,
    pesetas: initialPesetas,
    totalCoins,
    shardsCollected: [false, false, false],
    enemiesDefeated: 0,
    elapsedTime: 0,
    upgrades,
    nearMerchantId: null,
    shopOpenRequested: false,
    camera: {
      x: camX,
      y: camY,
      shakeTimer: 0,
      shakeMagnitude: 0,
    },
    hitstopTimer: 0,
    status: 'playing',
    gameOverReason: '',
    projectiles: [],
    explosions: [],
    particles: [],
    afterimages: [],
    floatingTexts: [],
    pendingSounds: [],
    goalUnlocked: !hasBoss,
    nextEntitySeq: 1,
  };
}

function nextId(world: WorldState, prefix: string): string {
  return `${prefix}-${world.nextEntitySeq++}`;
}

function spawnParticles(
  world: WorldState,
  x: number,
  y: number,
  count: number,
  colors: string[],
  speedMin: number,
  speedMax: number,
  gravity = 240,
  life = 0.4,
  size = 2
): void {
  for (let i = 0; i < count; i++) {
    const angle = (Math.PI * 2 * i) / count + (Math.random() - 0.5) * 0.6;
    const speed = speedMin + Math.random() * (speedMax - speedMin);
    world.particles.push({
      x,
      y,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed - 25,
      size,
      color: colors[i % colors.length],
      life: life * (0.75 + Math.random() * 0.5),
      maxLife: life,
      gravity,
    });
  }
}

function addFloatingText(world: WorldState, x: number, y: number, text: string, color: string): void {
  world.floatingTexts.push({
    x,
    y,
    vy: -26,
    text,
    color,
    life: 0.9,
    maxLife: 0.9,
  });
}

function triggerShake(world: WorldState, duration: number, magnitude: number): void {
  world.camera.shakeTimer = Math.max(world.camera.shakeTimer, duration);
  world.camera.shakeMagnitude = Math.max(world.camera.shakeMagnitude, magnitude);
}

export function respawnPlayerAtCheckpoint(world: WorldState): void {
  const p = world.player;
  p.x = world.respawnPoint.x;
  p.y = world.respawnPoint.y;
  p.h = p.standingH;
  p.crouching = false;
  p.vx = 0;
  p.vy = 0;
  p.dashTimer = 0;
  p.onGround = false;
  p.standingPlatformId = null;
  p.canDash = true;
  p.airJumpsLeft = p.maxAirJumps;
  p.reloadTimer = 0;
  p.reloadDuration = 0;
  p.grenadeTimer = 0;
  p.idleTime = 0;
  p.landingTimer = 0;
  p.invulnTimer = 1.4;
  spawnParticles(world, p.x + p.w / 2, p.y + p.h / 2, 12, ['#4deeea', '#ffd447', '#ffffff'], 40, 100, 60, 0.45);
}

function damagePlayer(
  world: WorldState,
  reason: string,
  respawnOnHit: boolean,
  knockbackDir = 0
): void {
  const p = world.player;
  if (p.invulnTimer > 0 && !respawnOnHit) return;

  p.hp = Math.max(0, p.hp - 1);
  p.hurtTimer = 0.28;
  p.idleTime = 0;
  triggerShake(world, 0.24, 4.5);
  world.hitstopTimer = 0.05;
  spawnParticles(world, p.x + p.w / 2, p.y + p.h / 2, 14, ['#ff5b6e', '#ffd447', '#ffffff'], 45, 120, 160, 0.45);

  if (p.hp <= 0) {
    world.status = 'gameover';
    world.gameOverReason = reason;
    world.pendingSounds.push('gameover');
    return;
  }

  world.pendingSounds.push('hurt');
  if (respawnOnHit) {
    respawnPlayerAtCheckpoint(world);
  } else {
    p.invulnTimer = 1.3;
    p.vy = -190;
    p.vx = (knockbackDir || -p.facing) * 135;
    p.dashTimer = 0;
  }
}

function maybeSpawnAmmoDrop(world: WorldState, enemy: Enemy): void {
  if (enemy.kind === 'boss_salvador') return;
  const p = world.player;
  const hasShotgun = p.unlockedWeapons.shotgun;
  const hasMg = p.unlockedWeapons.mg;
  if (!hasShotgun && !hasMg) return;

  // Deterministic/fair drop cadence (~50% on standard Ganados, guaranteed on Axe Brutes)
  if (enemy.kind !== 'ganado_axe' && world.enemiesDefeated % 2 === 0) return;

  let dropKind: 'ammo_shotgun' | 'ammo_mg';
  if (hasShotgun && hasMg) {
    const sgRatio = p.ammo.shotgun / 16;
    const mgRatio = p.ammo.mg / 60;
    dropKind = sgRatio <= mgRatio ? 'ammo_shotgun' : 'ammo_mg';
  } else {
    dropKind = hasShotgun ? 'ammo_shotgun' : 'ammo_mg';
  }

  world.level.pickups.push({
    id: nextId(world, 'drop'),
    kind: dropKind,
    x: Math.max(16, Math.min(world.level.width - 32, Math.round(enemy.x + enemy.w * 0.5 - 7))),
    y: Math.round(enemy.y + enemy.h - 11),
    w: 15,
    h: 11,
    collected: false,
    bobOffset: world.enemiesDefeated * 0.9,
  });
}

function recordEnemyKill(world: WorldState, enemy: Enemy): void {
  world.enemiesDefeated += 1;
  const pts = enemy.kind === 'boss_salvador' ? 2500 : enemy.kind === 'ganado_axe' ? 250 : 150;
  const basePtas = enemy.kind === 'boss_salvador' ? 350 : enemy.kind === 'ganado_axe' ? 65 : 40;
  const ptasReward = Math.round(basePtas * DIFFICULTY_CONFIG[world.difficulty].rewardMult);
  world.score += pts;
  world.pesetas += ptasReward;

  const deathSound =
    enemy.kind === 'boss_salvador'
      ? 'enemy_die_boss'
      : enemy.kind === 'ganado_axe'
        ? 'enemy_die_axe'
        : 'enemy_die_fork';
  world.pendingSounds.push(deathSound);

  maybeSpawnAmmoDrop(world, enemy);

  addFloatingText(
    world,
    enemy.x + enemy.w * 0.5,
    enemy.y - 4,
    enemy.kind === 'boss_salvador' ? `DR. SALVADOR DEFEATED! +${ptasReward}P` : `+${ptasReward} PTAS`,
    '#ffd447'
  );

  if (enemy.kind === 'boss_salvador') {
    world.goalUnlocked = true;
    addFloatingText(world, world.level.goal.x + 9, world.level.goal.y - 6, 'LONCENG TERBUKA!', '#ffd447');
    spawnParticles(
      world,
      world.level.goal.x + 9,
      world.level.goal.y + 12,
      20,
      ['#ffd447', '#ffffff', '#fb923c'],
      40,
      125,
      30,
      0.7
    );
  }
}

export function triggerExplosion(
  world: WorldState,
  cx: number,
  cy: number,
  radius: number,
  damage: number,
  isBarrel = false
): void {
  world.explosions.push({ x: cx, y: cy, radius, life: 0.36, maxLife: 0.36 });
  world.pendingSounds.push(isBarrel ? 'barrel_explosion' : 'explosion');
  triggerShake(world, isBarrel ? 0.3 : 0.24, isBarrel ? 6 : 5);
  spawnParticles(world, cx, cy, 24, ['#ffd447', '#f97316', '#ef4444', '#ffffff'], 55, 175, 180, 0.55, 3);

  // Damage enemies in blast radius (Merchant is completely immune)
  for (const enemy of world.level.enemies) {
    if (enemy.state === 'dying' || enemy.state === 'dead') continue;
    const ex = enemy.x + enemy.w * 0.5;
    const ey = enemy.y + enemy.h * 0.5;
    if (Math.hypot(ex - cx, ey - cy) <= radius + 14) {
      const knock = ex >= cx ? 1 : -1;
      const killed = damageEnemy(enemy, damage, knock);
      if (killed) {
        recordEnemyKill(world, enemy);
      }
    }
  }

  // Chain-detonate nearby explosive barrels
  for (const barrel of world.level.barrels) {
    if (barrel.exploded) continue;
    const bx = barrel.x + barrel.w * 0.5;
    const by = barrel.y + barrel.h * 0.5;
    if (Math.hypot(bx - cx, by - cy) <= radius + 12) {
      barrel.exploded = true;
      world.score += 100;
      world.pesetas += 25;
      triggerExplosion(world, bx, by, 54, 6 + world.player.damageBonus, true);
    }
  }
}

function cycleWeapon(player: PlayerState, dir: -1 | 1 = 1): void {
  for (const w of WEAPON_ORDER) {
    syncWeaponAmmo(player, w);
  }
  const idx = WEAPON_ORDER.indexOf(player.activeWeapon);
  for (let step = 1; step <= WEAPON_ORDER.length; step++) {
    const offset = ((idx + step * dir) % WEAPON_ORDER.length + WEAPON_ORDER.length) % WEAPON_ORDER.length;
    const candidate = WEAPON_ORDER[offset];
    if (candidate === 'pistol' || player.ammo[candidate] > 0) {
      if (player.activeWeapon !== candidate) {
        player.activeWeapon = candidate;
        player.reloadTimer = 0;
        player.reloadDuration = 0;
        player.idleTime = 0;
      }
      return;
    }
  }
}

function selectWeaponBySlot(world: WorldState, slot: 1 | 2 | 3 | 4): void {
  const p = world.player;
  const candidate = WEAPON_ORDER[slot - 1];
  if (!candidate) return;
  syncWeaponAmmo(p, candidate);
  if (candidate !== 'pistol' && !p.unlockedWeapons[candidate]) {
    addFloatingText(world, p.x + p.w * 0.5, p.y - 8, `${WEAPON_META[candidate].label} TERKUNCI!`, '#94a3b8');
    return;
  }
  if (p.activeWeapon !== candidate) {
    p.activeWeapon = candidate;
    p.reloadTimer = 0;
    p.reloadDuration = 0;
    p.idleTime = 0;
    if (candidate !== 'pistol' && p.ammo[candidate] <= 0) {
      addFloatingText(world, p.x + p.w * 0.5, p.y - 8, 'AMMO HABIS!', '#ef4444');
    } else if (p.mag[candidate] === 0 && (candidate === 'pistol' || p.reserve[candidate] > 0)) {
      startReload(world, candidate);
    }
  }
}

export function startReload(world: WorldState, wId: WeaponId = world.player.activeWeapon, silent = false): boolean {
  const p = world.player;
  syncWeaponAmmo(p, wId);
  if (p.reloadTimer > 0) return false;
  const magCap = WEAPON_META[wId].magSize;
  if (p.mag[wId] >= magCap) return false;
  if (wId !== 'pistol' && p.reserve[wId] <= 0) return false;

  p.reloadTimer = WEAPON_META[wId].reloadTime;
  p.reloadDuration = p.reloadTimer;
  p.idleTime = 0;
  if (!silent) {
    world.pendingSounds.push('reload');
  }
  return true;
}

function finishReload(p: PlayerState): void {
  const wId = p.activeWeapon;
  const magCap = WEAPON_META[wId].magSize;
  if (wId === 'pistol') {
    p.mag.pistol = magCap;
    p.ammo.pistol = Infinity;
  } else {
    const needed = Math.max(0, magCap - p.mag[wId]);
    const take = Math.min(needed, p.reserve[wId]);
    p.mag[wId] += take;
    p.reserve[wId] -= take;
    p.ammo[wId] = p.mag[wId] + p.reserve[wId];
  }
  p.reloadTimer = 0;
  p.reloadDuration = 0;
}

/**
 * Computes world coordinates for the active gun muzzle, brass ejection port,
 * and RPG-7 backblast cone matched to each character's sprite silhouette.
 */
function getPlayerMuzzle(
  p: PlayerState,
  wId: WeaponId
): { muzzleX: number; muzzleY: number; ejectX: number; ejectY: number; backX: number; backY: number } {
  const cx = p.x + p.w * 0.5;
  const footY = p.y + p.h;
  const pivotX = cx + p.facing * 3;
  const pivotY = footY - (p.crouching ? 18.5 : 26.5) + (wId === 'rpg' ? -3.5 : 0);

  if (Math.abs(p.aimDirY) < 0.25) {
    let dx: number;
    let dyAboveFeet: number;
    if (p.crouching) {
      dx = wId === 'pistol' ? 18 : 22;
      dyAboveFeet = p.hero === 'ada' && wId === 'mg' ? 23.5 : wId === 'rpg' ? 22 : 19;
    } else if (p.hero === 'leon') {
      const table: Record<WeaponId, [number, number]> = {
        pistol: [19.5, 27.5],
        shotgun: [22.0, 29.0],
        mg: [20.0, 29.5],
        rpg: [23.0, 32.0],
      };
      [dx, dyAboveFeet] = table[wId];
    } else {
      const table: Record<WeaponId, [number, number]> = {
        pistol: [21.0, 31.0],
        shotgun: [22.0, 28.5],
        mg: [18.0, 25.5],
        rpg: [24.0, 32.5],
      };
      [dx, dyAboveFeet] = table[wId];
    }

    const muzzleX = cx + p.facing * dx;
    const muzzleY = footY - dyAboveFeet;
    return {
      muzzleX,
      muzzleY,
      ejectX: cx + p.facing * (wId === 'pistol' ? 6 : 8),
      ejectY: muzzleY - 1,
      backX: cx - p.facing * 15,
      backY: muzzleY,
    };
  }

  const reach = wId === 'pistol' ? 14 : wId === 'rpg' ? 21 : 19;
  const muzzleX = pivotX + p.aimDirX * reach;
  const muzzleY = pivotY + p.aimDirY * reach;
  return {
    muzzleX,
    muzzleY,
    ejectX: pivotX + p.aimDirX * 5,
    ejectY: pivotY + p.aimDirY * 5 - 1,
    backX: pivotX - p.aimDirX * 15,
    backY: pivotY - p.aimDirY * 15,
  };
}

function fireActiveWeapon(world: WorldState): void {
  const p = world.player;
  let wId = p.activeWeapon;
  syncWeaponAmmo(p, wId);

  if (wId !== 'pistol' && p.ammo[wId] <= 0) {
    wId = 'pistol';
    p.activeWeapon = 'pistol';
    syncWeaponAmmo(p, 'pistol');
  }

  if (p.mag[wId] <= 0) {
    if (wId === 'pistol' || p.reserve[wId] > 0) {
      startReload(world, wId);
      return;
    }
  }

  const { muzzleX, muzzleY, ejectX, ejectY, backX, backY } = getPlayerMuzzle(p, wId);
  const dmgBonus = p.damageBonus;

  p.shootTimer = 0.16;
  p.shootCooldown = WEAPON_META[wId].cooldown;
  p.idleTime = 0;
  p.mag[wId] = Math.max(0, p.mag[wId] - 1);
  p.ammo[wId] = wId === 'pistol' ? Infinity : p.mag[wId] + p.reserve[wId];

  if (wId === 'pistol') {
    world.projectiles.push({
      id: nextId(world, 'b'),
      kind: 'pistol',
      fromPlayer: true,
      x: muzzleX - 3,
      y: muzzleY - 2,
      vx: p.aimDirX * 370,
      vy: p.aimDirY * 370,
      w: 6,
      h: 3,
      damage: 1 + dmgBonus,
      life: 1.1,
      bouncesLeft: 0,
      rotation: 0,
    });
    world.pendingSounds.push('shoot_pistol');
    spawnParticles(world, muzzleX, muzzleY, 3, ['#ffd447', '#ffffff'], 25, 60, 30, 0.14, 2);
    world.particles.push({
      x: ejectX,
      y: ejectY,
      vx: -p.facing * (35 + Math.random() * 25),
      vy: -75 - Math.random() * 25,
      size: 2,
      color: '#fbbf24',
      life: 0.4,
      maxLife: 0.4,
      gravity: 420,
    });
  } else if (wId === 'shotgun') {
    const baseAngle = Math.atan2(p.aimDirY, p.aimDirX);
    for (const spread of [-0.22, -0.132, -0.044, 0.044, 0.132, 0.22]) {
      const a = baseAngle + spread;
      world.projectiles.push({
        id: nextId(world, 's'),
        kind: 'pellet',
        fromPlayer: true,
        x: muzzleX - 2,
        y: muzzleY - 2,
        vx: Math.cos(a) * 350,
        vy: Math.sin(a) * 350,
        w: 4,
        h: 4,
        damage: 1 + dmgBonus,
        life: 0.44,
        bouncesLeft: 0,
        rotation: 0,
        knockbackMult: 2.3,
      });
    }
    triggerShake(world, 0.12, 2.8);
    world.pendingSounds.push('shoot_shotgun');
    spawnParticles(world, muzzleX, muzzleY, 7, ['#ffd447', '#fb923c', '#ffffff'], 40, 110, 40, 0.18, 2);
    world.particles.push({
      x: ejectX,
      y: ejectY,
      vx: -p.facing * (44 + Math.random() * 24),
      vy: -102 - Math.random() * 26,
      size: 5,
      color: '#dc2626',
      life: 0.55,
      maxLife: 0.55,
      gravity: 430,
      sprite: 'shell_shotgun',
      rotation: Math.random() * 0.6,
      spin: -p.facing * (14 + Math.random() * 6),
    });
  } else if (wId === 'mg') {
    const baseAngle = Math.atan2(p.aimDirY, p.aimDirX);
    const recoilAngle = baseAngle + (Math.random() - 0.5) * 0.07;
    world.projectiles.push({
      id: nextId(world, 'm'),
      kind: 'mg',
      fromPlayer: true,
      x: muzzleX - 3,
      y: muzzleY - 2,
      vx: Math.cos(recoilAngle) * 425,
      vy: Math.sin(recoilAngle) * 425,
      w: 7,
      h: 3,
      damage: 1 + dmgBonus,
      life: 1.0,
      bouncesLeft: 0,
      rotation: recoilAngle,
    });
    triggerShake(world, 0.04, 0.7);
    world.pendingSounds.push('shoot_mg');
    spawnParticles(world, muzzleX, muzzleY, 4, ['#ffd447', '#ffffff', '#fb923c'], 30, 75, 25, 0.12, 2);
    world.particles.push({
      x: ejectX,
      y: ejectY,
      vx: -p.facing * (34 + Math.random() * 30),
      vy: -94 - Math.random() * 28,
      size: 4,
      color: '#fbbf24',
      life: 0.48,
      maxLife: 0.48,
      gravity: 440,
      sprite: 'shell_cqbr',
      rotation: Math.random() * 0.6,
      spin: -p.facing * (18 + Math.random() * 8),
    });
  } else if (wId === 'rpg') {
    const aimAngle = Math.atan2(p.aimDirY, p.aimDirX);
    world.projectiles.push({
      id: nextId(world, 'r'),
      kind: 'rpg',
      fromPlayer: true,
      x: muzzleX - 6,
      y: muzzleY - 3,
      vx: p.aimDirX * 265,
      vy: p.aimDirY * 265,
      w: 14,
      h: 6,
      damage: 7 + dmgBonus * 2,
      life: 1.7,
      bouncesLeft: 0,
      rotation: aimAngle,
    });
    triggerShake(world, 0.16, 3.6);
    world.pendingSounds.push('shoot_rpg');
    // Front muzzle blast & sparks
    spawnParticles(world, muzzleX, muzzleY, 8, ['#ffd447', '#f97316', '#ffffff'], 45, 115, 40, 0.22, 2);
    // Backblast flame & smoke jet from rear exhaust cone
    for (let b = 0; b < 9; b++) {
      const spd = 65 + Math.random() * 80;
      const spread = (Math.random() - 0.5) * 0.55;
      const ba = Math.atan2(-p.aimDirY, -p.aimDirX) + spread;
      world.particles.push({
        x: backX,
        y: backY,
        vx: Math.cos(ba) * spd,
        vy: Math.sin(ba) * spd,
        size: b % 2 === 0 ? 3 : 2,
        color: b < 4 ? '#fb923c' : b < 7 ? '#ffd447' : '#94a3b8',
        life: 0.28 + Math.random() * 0.14,
        maxLife: 0.4,
        gravity: -25,
      });
    }
  }

  if (p.mag[wId] <= 0) {
    if (wId === 'pistol' || p.reserve[wId] > 0) {
      startReload(world, wId, true);
    } else if (p.ammo[wId] <= 0) {
      p.activeWeapon = 'pistol';
    }
  }
}

function throwGrenade(world: WorldState): void {
  const p = world.player;
  if (p.grenades <= 0) return;
  p.grenades -= 1;
  p.grenadeTimer = 0.28;
  p.shootTimer = 0;
  p.idleTime = 0;

  world.projectiles.push({
    id: nextId(world, 'g'),
    kind: 'grenade',
    fromPlayer: true,
    x: p.x + p.w * 0.5 + p.facing * 6,
    y: p.y + 8,
    vx: p.facing * 175 + p.vx * 0.3,
    vy: p.aimDirY < -0.5 ? -260 : -175,
    w: 6,
    h: 6,
    damage: 5 + p.damageBonus,
    life: 1.15,
    bouncesLeft: 2,
    rotation: 0,
  });
  world.pendingSounds.push('grenade_throw');
}

function performMelee(world: WorldState): void {
  const p = world.player;
  p.meleeTimer = 0.28;
  p.meleeCooldown = 0.36;
  p.meleeDidHit = false;
  p.idleTime = 0;
  world.pendingSounds.push('melee');

  const reach = p.hero === 'ada' ? 32 : 30;
  const hitBox: Rect = {
    x: p.facing > 0 ? p.x + p.w - 2 : p.x - reach + 2,
    y: p.y + 4,
    w: reach,
    h: p.h - 4,
  };

  // Parry incoming enemy axes!
  for (let i = world.projectiles.length - 1; i >= 0; i--) {
    const proj = world.projectiles[i];
    if (!proj.fromPlayer && rectsOverlap(hitBox, proj)) {
      world.projectiles.splice(i, 1);
      world.score += 50;
      world.pesetas += 15;
      world.pendingSounds.push('parry');
      spawnParticles(world, proj.x + 4, proj.y + 4, 8, ['#4deeea', '#ffffff', '#ffd447'], 45, 110, 80, 0.3);
      addFloatingText(world, proj.x, proj.y - 4, 'PARRY!', '#4deeea');
    }
  }

  // Hit enemies (Merchant is immune and never listed in enemies)
  for (const enemy of world.level.enemies) {
    if (enemy.state === 'dying' || enemy.state === 'dead') continue;
    if (rectsOverlap(hitBox, enemy)) {
      const killed = damageEnemy(enemy, 2 + p.damageBonus, p.facing);
      world.hitstopTimer = 0.04;
      triggerShake(world, 0.1, 2);
      spawnParticles(world, enemy.x + enemy.w * 0.5, enemy.y + 14, 8, ['#ffffff', '#ef4444'], 45, 110, 140, 0.35);
      if (killed) {
        recordEnemyKill(world, enemy);
      } else {
        world.pendingSounds.push('enemy_hit');
      }
    }
  }

  // Smash barrels
  for (const barrel of world.level.barrels) {
    if (barrel.exploded) continue;
    if (rectsOverlap(hitBox, barrel)) {
      barrel.exploded = true;
      world.score += 100;
      world.pesetas += 25;
      triggerExplosion(world, barrel.x + barrel.w * 0.5, barrel.y + barrel.h * 0.5, 54, 6 + p.damageBonus, true);
    }
  }
}

function updateProjectiles(world: WorldState, dt: number): void {
  const p = world.player;

  for (let i = world.projectiles.length - 1; i >= 0; i--) {
    const proj = world.projectiles[i];
    proj.life -= dt;
    proj.rotation += dt * 14;

    if (proj.kind === 'grenade') {
      proj.vy += 580 * dt;
      proj.x += proj.vx * dt;
      const colX = Math.floor((proj.vx > 0 ? proj.x + proj.w : proj.x) / TILE_SIZE);
      const rowMid = Math.floor((proj.y + proj.h * 0.5) / TILE_SIZE);
      if (isSolidTile(getTileAt(world.level, colX, rowMid))) {
        proj.vx = -proj.vx * 0.55;
        proj.x += proj.vx * dt * 2;
      }

      proj.y += proj.vy * dt;
      const rowBot = Math.floor((proj.y + proj.h) / TILE_SIZE);
      const colMid = Math.floor((proj.x + proj.w * 0.5) / TILE_SIZE);
      const tileBot = getTileAt(world.level, colMid, rowBot);
      if (proj.vy > 0 && (tileBot === 'solid' || tileBot === 'oneway')) {
        proj.y = rowBot * TILE_SIZE - proj.h;
        if (proj.bouncesLeft > 0) {
          proj.vy = -Math.abs(proj.vy) * 0.52;
          proj.vx *= 0.72;
          proj.bouncesLeft--;
          world.pendingSounds.push('grenade_bounce');
        } else {
          proj.vy = 0;
          proj.vx *= 0.85;
        }
      }

      let directHit = false;
      for (const enemy of world.level.enemies) {
        if (enemy.state !== 'dying' && enemy.state !== 'dead' && rectsOverlap(proj, enemy)) {
          directHit = true;
          break;
        }
      }
      if (!directHit) {
        for (const barrel of world.level.barrels) {
          if (!barrel.exploded && rectsOverlap(proj, barrel)) {
            directHit = true;
            break;
          }
        }
      }

      if (proj.life <= 0 || directHit) {
        triggerExplosion(world, proj.x + 3, proj.y + 3, 52, proj.damage, false);
        world.projectiles.splice(i, 1);
      }
      continue;
    }

    if (proj.kind === 'enemy_axe') {
      proj.vy += 290 * dt;
      proj.x += proj.vx * dt;
      proj.y += proj.vy * dt;

      const col = Math.floor((proj.x + proj.w * 0.5) / TILE_SIZE);
      const row = Math.floor((proj.y + proj.h * 0.5) / TILE_SIZE);
      if (proj.life <= 0 || isSolidTile(getTileAt(world.level, col, row))) {
        spawnParticles(world, proj.x + 4, proj.y + 4, 5, ['#94a3b8', '#e2e8f0'], 25, 65, 160, 0.25);
        world.projectiles.splice(i, 1);
        continue;
      }

      if (rectsOverlap(proj, p) && p.dashTimer <= 0) {
        const knockDir = proj.vx >= 0 ? 1 : -1;
        world.projectiles.splice(i, 1);
        damagePlayer(world, 'Terkena kapak Ganado!', false, knockDir);
      }
      continue;
    }

    proj.x += proj.vx * dt;
    proj.y += proj.vy * dt;

    if (proj.kind === 'rpg') {
      const tailX = proj.vx >= 0 ? proj.x : proj.x + proj.w;
      const tailY = proj.y + proj.h * 0.5;
      world.particles.push({
        x: tailX,
        y: tailY,
        vx: -proj.vx * 0.08 + (Math.random() - 0.5) * 18,
        vy: (Math.random() - 0.5) * 18 - 6,
        size: Math.random() < 0.45 ? 3 : 2,
        color: Math.random() < 0.35 ? '#fb923c' : '#cbd5e1',
        life: 0.26,
        maxLife: 0.26,
        gravity: -16,
      });
    }

    const col = Math.floor((proj.x + proj.w * 0.5) / TILE_SIZE);
    const row = Math.floor((proj.y + proj.h * 0.5) / TILE_SIZE);
    if (proj.life <= 0 || isSolidTile(getTileAt(world.level, col, row))) {
      if (proj.kind === 'rpg') {
        triggerExplosion(world, proj.x + proj.w * 0.5, proj.y + proj.h * 0.5, 62, proj.damage, false);
      } else {
        spawnParticles(world, proj.x, proj.y, 4, ['#ffd447', '#cbd5e1'], 20, 60, 120, 0.2);
      }
      world.projectiles.splice(i, 1);
      continue;
    }

    // Check explosive barrels
    let consumed = false;
    for (const barrel of world.level.barrels) {
      if (barrel.exploded) continue;
      if (rectsOverlap(proj, barrel)) {
        barrel.hp -= proj.damage;
        barrel.flashTimer = 0.1;
        if (proj.kind === 'rpg' || barrel.hp <= 0) {
          barrel.exploded = true;
          world.score += 100;
          world.pesetas += 25;
          triggerExplosion(
            world,
            barrel.x + barrel.w * 0.5,
            barrel.y + barrel.h * 0.5,
            54,
            6 + p.damageBonus,
            true
          );
        } else {
          spawnParticles(world, proj.x, proj.y, 5, ['#f97316', '#ffd447'], 25, 75, 120, 0.25);
        }
        world.projectiles.splice(i, 1);
        consumed = true;
        break;
      }
    }
    if (consumed) continue;

    // Check enemies (Merchant is completely immune & bullets pass through harmlessly)
    for (const enemy of world.level.enemies) {
      if (enemy.state === 'dying' || enemy.state === 'dead') continue;
      if (rectsOverlap(proj, enemy)) {
        if (proj.kind === 'rpg') {
          triggerExplosion(world, proj.x + proj.w * 0.5, proj.y + proj.h * 0.5, 62, proj.damage, false);
        } else {
          const knockDir = proj.vx >= 0 ? 1 : -1;
          const killed = damageEnemy(enemy, proj.damage, knockDir, proj.knockbackMult ?? 1);
          spawnParticles(world, proj.x, proj.y, 6, ['#ef4444', '#ffd447'], 30, 85, 140, 0.28);
          if (killed) {
            recordEnemyKill(world, enemy);
          } else {
            world.pendingSounds.push('enemy_hit');
          }
        }
        world.projectiles.splice(i, 1);
        break;
      }
    }
  }
}

function updateVisualParticles(world: WorldState, dt: number): void {
  for (let i = world.explosions.length - 1; i >= 0; i--) {
    const ex = world.explosions[i];
    ex.life -= dt;
    if (ex.life <= 0) world.explosions.splice(i, 1);
  }

  for (let i = world.particles.length - 1; i >= 0; i--) {
    const pt = world.particles[i];
    pt.life -= dt;
    if (pt.life <= 0) {
      world.particles.splice(i, 1);
      continue;
    }
    pt.vy += pt.gravity * dt;
    pt.x += pt.vx * dt;
    pt.y += pt.vy * dt;
    if (pt.rotation !== undefined && pt.spin !== undefined) {
      pt.rotation += pt.spin * dt;
    }
  }

  for (let i = world.afterimages.length - 1; i >= 0; i--) {
    const img = world.afterimages[i];
    img.life -= dt;
    if (img.life <= 0) world.afterimages.splice(i, 1);
  }

  for (let i = world.floatingTexts.length - 1; i >= 0; i--) {
    const ft = world.floatingTexts[i];
    ft.life -= dt;
    if (ft.life <= 0) {
      world.floatingTexts.splice(i, 1);
      continue;
    }
    ft.y += ft.vy * dt;
  }
}

function updateCamera(world: WorldState, dt: number): void {
  const p = world.player;
  const lookAheadX = p.facing * 38;
  const targetX = p.x + p.w * 0.5 + lookAheadX - VIRTUAL_WIDTH * 0.5;
  const targetY = p.y + p.h * 0.5 - VIRTUAL_HEIGHT * 0.58;

  const clampedX = Math.max(0, Math.min(world.level.width - VIRTUAL_WIDTH, targetX));
  const clampedY = Math.max(0, Math.min(world.level.height - VIRTUAL_HEIGHT, targetY));

  world.camera.x += (clampedX - world.camera.x) * Math.min(1, dt * 7.5);
  world.camera.y += (clampedY - world.camera.y) * Math.min(1, dt * 6.5);

  if (world.camera.shakeTimer > 0) {
    world.camera.shakeTimer = Math.max(0, world.camera.shakeTimer - dt);
    if (world.camera.shakeTimer === 0) {
      world.camera.shakeMagnitude = 0;
    }
  }
}

export function stepWorld(world: WorldState, input: InputState, dt: number): void {
  if (world.status !== 'playing') {
    updateVisualParticles(world, dt);
    return;
  }

  if (world.hitstopTimer > 0) {
    world.hitstopTimer = Math.max(0, world.hitstopTimer - dt);
    updateVisualParticles(world, dt);
    updateCamera(world, dt);
    return;
  }

  world.elapsedTime += dt;

  // 1. Moving platforms
  updateMovingPlatforms(world.level.platforms, dt);

  // 2. Player physics
  const p = world.player;
  const isFemale = p.hero === 'ada';
  const outcome = stepPlayerPhysics(p, world.level, input, dt);

  if (outcome.jumped) {
    world.pendingSounds.push(isFemale ? 'jump_female' : 'jump_male');
    spawnParticles(world, p.x + p.w / 2, p.y + p.h, 5, ['#cbd5e1', '#94a3b8'], 20, 55, 80, 0.28);
  }
  if (outcome.doubleJumped) {
    world.pendingSounds.push(isFemale ? 'double_jump_female' : 'double_jump_male');
    spawnParticles(world, p.x + p.w / 2, p.y + p.h, 8, ['#4deeea', '#ffffff'], 35, 85, 40, 0.35);
  }
  if (outcome.wallJumped) {
    world.pendingSounds.push('wall_jump');
    const wallX = outcome.wallDirection < 0 ? p.x : p.x + p.w;
    spawnParticles(world, wallX, p.y + p.h * 0.5, 6, ['#e2e8f0', '#cbd5e1'], 25, 65, 90, 0.3);
  }
  if (outcome.dashed) {
    world.pendingSounds.push('dash');
    triggerShake(world, 0.08, 1.5);
    spawnParticles(world, p.x + p.w / 2, p.y + p.h / 2, 8, ['#4deeea', '#ffd447'], 35, 90, 30, 0.3);
  }
  if (outcome.landed) {
    spawnParticles(world, p.x + p.w / 2, p.y + p.h, 4, ['#cbd5e1', '#94a3b8'], 15, 40, 100, 0.22);
  }
  if (outcome.sprung) {
    world.pendingSounds.push('spring');
    spawnParticles(world, p.x + p.w / 2, p.y + p.h, 10, ['#ffd447', '#fb923c', '#ffffff'], 45, 110, 100, 0.38);
  }

  if (p.dashTimer > 0) {
    world.afterimages.push({
      x: p.x,
      y: p.y,
      facing: p.facing,
      frameIndex: p.hero === 'ada' ? 20 : 29,
      life: 0.15,
      maxLife: 0.15,
      color: p.hero === 'ada' ? '#ff4d6d' : '#4deeea',
    });
  }

  // 3. Merchant proximity & shop interaction
  let nearId: string | null = null;
  const pCx = p.x + p.w * 0.5;
  const pCy = p.y + p.h * 0.5;
  for (const m of world.level.merchants) {
    m.animTime += dt;
    const mCx = m.x + m.w * 0.5;
    const mCy = m.y + m.h * 0.5;
    if (Math.hypot(pCx - mCx, pCy - mCy) <= 44) {
      nearId = m.id;
    }
  }
  world.nearMerchantId = nearId;
  if (nearId && input.interactPressed) {
    world.shopOpenRequested = true;
    world.pendingSounds.push('merchant_greet');
  }

  // 4. Player combat actions, reload & items
  if (p.reloadTimer > 0) {
    p.reloadTimer = Math.max(0, p.reloadTimer - dt);
    if (p.reloadTimer === 0) {
      finishReload(p);
    }
  }

  if (input.selectWeaponSlot && input.selectWeaponSlot >= 1 && input.selectWeaponSlot <= 4) {
    selectWeaponBySlot(world, input.selectWeaponSlot);
  } else if (input.switchWeaponPressed) {
    cycleWeapon(p, input.switchWeaponDir ?? 1);
  }

  if (input.reloadPressed) {
    startReload(world, p.activeWeapon);
  }

  if (input.healPressed && p.medkits > 0 && p.hp < p.maxHp) {
    p.medkits -= 1;
    p.hp = Math.min(p.maxHp, p.hp + 2);
    p.idleTime = 0;
    world.pendingSounds.push('medkit');
    spawnParticles(world, p.x + p.w * 0.5, p.y + p.h * 0.5, 12, ['#4ade80', '#bbf7d0', '#ffffff'], 30, 85, 40, 0.5);
    addFloatingText(world, p.x + 7, p.y - 6, '+2 HP MEDKIT', '#4ade80');
  }

  if (input.meleePressed && p.meleeCooldown <= 0 && p.dashTimer <= 0) {
    performMelee(world);
  }

  if (input.grenadePressed && p.grenades > 0 && p.dashTimer <= 0) {
    throwGrenade(world);
  }

  const wantsShoot =
    p.activeWeapon === 'mg' ? input.shootHeld : input.shootPressed || input.shootHeld;
  if (
    wantsShoot &&
    p.shootCooldown <= 0 &&
    p.reloadTimer <= 0 &&
    p.grenadeTimer <= 0 &&
    p.meleeTimer <= 0 &&
    p.dashTimer <= 0
  ) {
    fireActiveWeapon(world);
  }

  // 5. Barrels flash decay
  for (const barrel of world.level.barrels) {
    if (barrel.flashTimer > 0) {
      barrel.flashTimer = Math.max(0, barrel.flashTimer - dt);
    }
  }

  // 6. Projectiles & Explosions
  updateProjectiles(world, dt);

  // 7. Pickups
  for (const item of world.level.pickups) {
    if (item.collected || !rectsOverlap(p, item)) continue;
    item.collected = true;

    if (item.kind === 'coin') {
      world.coins += 1;
      world.pesetas += 50;
      world.score += 100;
      world.pendingSounds.push('coin');
      spawnParticles(world, item.x + 5, item.y + 5, 6, ['#ffd447', '#fff3b0'], 25, 70, 60, 0.32);
      addFloatingText(world, item.x + 5, item.y - 2, '+50 PTAS', '#ffd447');
    } else if (item.kind === 'shard') {
      if (item.shardIndex !== undefined) {
        world.shardsCollected[item.shardIndex] = true;
      }
      world.score += 500;
      world.pesetas += 100;
      world.pendingSounds.push('shard');
      spawnParticles(world, item.x + 6, item.y + 6, 14, ['#ff75c3', '#ffd447', '#4deeea'], 40, 110, 30, 0.55);
      addFloatingText(world, item.x + 6, item.y - 4, 'BLUE MEDALLION +100P', '#ff9ce3');
    } else if (item.kind === 'medkit') {
      if (p.medkits < p.maxMedkits) {
        p.medkits += 1;
        addFloatingText(world, item.x + 7, item.y - 4, '+1 MEDKIT', '#4ade80');
      } else {
        p.hp = Math.min(p.maxHp, p.hp + 2);
        addFloatingText(world, item.x + 7, item.y - 4, '+2 HP!', '#4ade80');
      }
      world.score += 200;
      world.pendingSounds.push('medkit');
      spawnParticles(world, item.x + 7, item.y + 6, 10, ['#4ade80', '#ffffff'], 30, 85, 40, 0.45);
    } else if (item.kind === 'grenade_box') {
      p.grenades += 2;
      world.score += 200;
      world.pendingSounds.push('weapon_pickup');
      spawnParticles(world, item.x + 7, item.y + 6, 10, ['#ffd447', '#84cc16'], 30, 85, 40, 0.45);
      addFloatingText(world, item.x + 7, item.y - 4, '+2 GRENADE', '#a3e635');
    } else if (item.kind === 'shotgun') {
      grantWeaponAmmo(p, 'shotgun', 12, true);
      world.score += 250;
      world.pendingSounds.push('weapon_pickup');
      addFloatingText(world, item.x + 7, item.y - 4, 'PUMP SHOTGUN [2]!', '#fb923c');
    } else if (item.kind === 'mg') {
      grantWeaponAmmo(p, 'mg', 60, true);
      world.score += 250;
      world.pendingSounds.push('weapon_pickup');
      addFloatingText(world, item.x + 7, item.y - 4, 'CQBR ASSAULT RIFLE [3]!', '#4deeea');
    } else if (item.kind === 'rpg') {
      grantWeaponAmmo(p, 'rpg', 4, true);
      world.score += 300;
      world.pendingSounds.push('weapon_pickup');
      addFloatingText(world, item.x + 7, item.y - 4, 'RPG-7 [4]!', '#ff5b6e');
    } else if (item.kind === 'ammo') {
      grantWeaponAmmo(p, 'shotgun', 6, false, p.unlockedWeapons.shotgun);
      grantWeaponAmmo(p, 'mg', 30, false, p.unlockedWeapons.mg);
      grantWeaponAmmo(p, 'rpg', 2, false, p.unlockedWeapons.rpg);
      p.grenades += 1;
      world.score += 200;
      world.pendingSounds.push('weapon_pickup');
      addFloatingText(world, item.x + 7, item.y - 4, 'AMMO SUPPLY!', '#bbf7d0');
    } else if (item.kind === 'ammo_shotgun') {
      grantWeaponAmmo(p, 'shotgun', 6, false, true);
      world.score += 120;
      world.pendingSounds.push('weapon_pickup');
      spawnParticles(world, item.x + 7, item.y + 5, 8, ['#ef4444', '#ffd447'], 25, 70, 50, 0.35);
      addFloatingText(world, item.x + 7, item.y - 4, '+6 SHELLS', '#fb923c');
    } else if (item.kind === 'ammo_mg') {
      grantWeaponAmmo(p, 'mg', 20, false, true);
      world.score += 120;
      world.pendingSounds.push('weapon_pickup');
      spawnParticles(world, item.x + 7, item.y + 5, 8, ['#4deeea', '#ffd447'], 25, 70, 50, 0.35);
      addFloatingText(world, item.x + 7, item.y - 4, '+20 RIFLE AMMO', '#4deeea');
    }
  }

  // 8. Checkpoints
  for (const cp of world.level.checkpoints) {
    if (!cp.activated && rectsOverlap(p, cp)) {
      cp.activated = true;
      world.respawnPoint = {
        x: cp.x,
        y: cp.y + cp.h - p.standingH,
      };
      world.score += 100;
      saveWalletAndUpgrades(world.pesetas, world.upgrades);
      world.pendingSounds.push('checkpoint');
      spawnParticles(world, cp.x + 6, cp.y + 4, 14, ['#fb923c', '#ffd447', '#ffffff'], 35, 95, 50, 0.55);
      addFloatingText(world, cp.x + 6, cp.y - 4, 'CHECKPOINT!', '#ffd447');
    }
  }

  // 9. Enemies AI & attacks
  const enemyEvents = updateEnemies(world.level.enemies, world.level, p, dt, world.difficulty);
  for (const ax of enemyEvents.thrownAxes) {
    world.projectiles.push({
      id: nextId(world, 'axe'),
      kind: 'enemy_axe',
      fromPlayer: false,
      x: ax.x - 5,
      y: ax.y - 5,
      vx: ax.vx,
      vy: ax.vy,
      w: 11,
      h: 11,
      damage: 1,
      life: 2.6,
      bouncesLeft: 0,
      rotation: 0,
    });
  }

  for (const _boss of enemyEvents.bossChainsawRevs) {
    world.pendingSounds.push('boss_chainsaw');
  }

  for (const boss of enemyEvents.bossLandings) {
    triggerShake(world, 0.18, 3.5);
    spawnParticles(
      world,
      boss.x + boss.w * 0.5,
      boss.y + boss.h,
      10,
      ['#e7e5e4', '#a8a29e', '#ffd447'],
      35,
      90,
      140,
      0.35
    );
  }

  for (const attacker of enemyEvents.meleeHitsPlayer) {
    if (world.status !== 'playing') break;
    if (p.dashTimer > 0) continue;
    const knock = p.x + p.w * 0.5 >= attacker.x + attacker.w * 0.5 ? 1 : -1;
    const msg = attacker.kind === 'boss_salvador' ? 'Terkena gergaji Dr. Salvador!' : 'Diserang Ganado!';
    damagePlayer(world, msg, false, knock);
  }

  if (world.status === 'playing' && p.invulnTimer <= 0 && p.dashTimer <= 0) {
    for (const enemy of world.level.enemies) {
      if (enemy.state === 'dying' || enemy.state === 'dead' || enemy.state === 'hurt') continue;
      if (rectsOverlap(p, enemy)) {
        const knock = p.x + p.w * 0.5 >= enemy.x + enemy.w * 0.5 ? 1 : -1;
        damagePlayer(world, 'Disergap Ganado!', false, knock);
        break;
      }
    }
  }

  // 10. Hazards (pit & spikes)
  if (world.status === 'playing') {
    if (p.y > world.level.height + 24) {
      damagePlayer(world, 'Terjatuh ke jurang!', true);
    } else if (checkSpikeCollision(world.level, p)) {
      damagePlayer(world, 'Terkena perangkap duri!', true);
    }
  }

  // 11. Goal Bell / Extraction
  if (world.status === 'playing' && world.goalUnlocked && rectsOverlap(p, world.level.goal)) {
    world.score += 600 + p.hp * 150;
    world.pesetas += 150;
    world.status = 'victory';
    world.pendingSounds.push('victory');
    spawnParticles(
      world,
      world.level.goal.x + 9,
      world.level.goal.y + 12,
      24,
      ['#ffd447', '#fb923c', '#ffffff'],
      50,
      145,
      40,
      0.7
    );
  }

  updateVisualParticles(world, dt);
  updateCamera(world, dt);
}
