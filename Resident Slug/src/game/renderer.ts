import { getTileAt, isSolidTile } from './physics';
import { ATLAS_CELL, ATLAS_COLS, CHARACTER_FRAMES, GameAssets } from './sprites';
import {
  BiomeId,
  DIFFICULTY_CONFIG,
  Enemy,
  HeroId,
  ParsedLevel,
  PlayerState,
  TILE_SIZE,
  VIRTUAL_HEIGHT,
  VIRTUAL_WIDTH,
  WorldState,
} from './types';
import { WEAPON_META, WEAPON_ORDER } from './world';

interface BiomePalette {
  tileBody: string;
  tileDark: string;
  tileCap: string;
  tileCapAccent: string;
  onewayTop: string;
  onewayBody: string;
  tintOverlay: string;
}

const BIOME_PALETTES: Record<BiomeId, BiomePalette> = {
  village: {
    tileBody: '#4a3d34',
    tileDark: '#302721',
    tileCap: '#5c6b42',
    tileCapAccent: '#82945a',
    onewayTop: '#b0713c',
    onewayBody: '#633d1e',
    tintOverlay: 'rgba(18, 22, 30, 0.25)',
  },
  plaza: {
    tileBody: '#453732',
    tileDark: '#2c211e',
    tileCap: '#6e5849',
    tileCapAccent: '#947864',
    onewayTop: '#a1622e',
    onewayBody: '#573317',
    tintOverlay: 'rgba(35, 18, 12, 0.26)',
  },
  tower: {
    tileBody: '#3d4452',
    tileDark: '#262b36',
    tileCap: '#64748b',
    tileCapAccent: '#94a3b8',
    onewayTop: '#b47844',
    onewayBody: '#6b421f',
    tintOverlay: 'rgba(12, 18, 32, 0.28)',
  },
  church: {
    tileBody: '#3b303a',
    tileDark: '#241c24',
    tileCap: '#6b4450',
    tileCapAccent: '#9c6373',
    onewayTop: '#9c5c30',
    onewayBody: '#542f16',
    tintOverlay: 'rgba(34, 10, 18, 0.30)',
  },
};

const RENDER_SCALE = 2;
let offscreenCanvas: HTMLCanvasElement | null = null;
let offscreenCtx: CanvasRenderingContext2D | null = null;

function getRenderBuffer(): { canvas: HTMLCanvasElement; ctx: CanvasRenderingContext2D } | null {
  if (typeof document === 'undefined') return null;
  if (!offscreenCanvas) {
    offscreenCanvas = document.createElement('canvas');
    offscreenCanvas.width = VIRTUAL_WIDTH * RENDER_SCALE;
    offscreenCanvas.height = VIRTUAL_HEIGHT * RENDER_SCALE;
    offscreenCtx = offscreenCanvas.getContext('2d');
  }
  if (!offscreenCtx) return null;
  offscreenCtx.imageSmoothingEnabled = false;
  return { canvas: offscreenCanvas, ctx: offscreenCtx };
}

function drawAtlasFrame(
  ctx: CanvasRenderingContext2D,
  atlas: HTMLImageElement,
  frameIndex: number,
  footX: number,
  footY: number,
  facing: 1 | -1,
  scale = 1
): void {
  const col = frameIndex % ATLAS_COLS;
  const row = Math.floor(frameIndex / ATLAS_COLS);
  const sx = col * ATLAS_CELL;
  const sy = row * ATLAS_CELL;

  ctx.save();
  ctx.translate(Math.round(footX), Math.round(footY));
  ctx.scale(facing * scale, scale);
  ctx.drawImage(
    atlas,
    sx,
    sy,
    ATLAS_CELL,
    ATLAS_CELL,
    -28,
    -62,
    64,
    64
  );
  ctx.restore();
}

interface WeaponLocomotionClips {
  walk: readonly number[];
  run: readonly number[];
  jump: readonly number[];
  drop: readonly number[];
}

function getHeroWeaponLocomotionClips(
  hero: PlayerState['hero'],
  weapon: PlayerState['activeWeapon']
): WeaponLocomotionClips | null {
  if (hero === 'leon') {
    const lf = CHARACTER_FRAMES.leon;
    if (weapon === 'shotgun') {
      return {
        walk: lf.walk_run_shotgun,
        run: lf.walk_run_shotgun,
        jump: lf.leon_shotgun_jump,
        drop: lf.leon_shotgun_drop,
      };
    }
    if (weapon === 'mg') {
      return {
        walk: lf.walk_run_cqbr,
        run: lf.walk_run_cqbr,
        jump: lf.leon_cqbr_jump,
        drop: lf.leon_cqbr_drop,
      };
    }
    if (weapon === 'rpg') {
      return {
        walk: lf.walk,
        run: lf.run,
        jump: lf.leon_rpg_jump,
        drop: lf.leon_rpg_drop,
      };
    }
    return null;
  }

  const af = CHARACTER_FRAMES.ada;
  if (weapon === 'shotgun') {
    return {
      walk: af.ada_shotgun_walk,
      run: af.ada_shotgun_run,
      jump: af.ada_shotgun_jump,
      drop: af.ada_shotgun_drop,
    };
  }
  if (weapon === 'mg') {
    return {
      walk: af.ada_cqbr_walk,
      run: af.ada_cqbr_run,
      jump: af.ada_cqbr_jump,
      drop: af.ada_cqbr_drop,
    };
  }
  return {
    walk: af.walk,
    run: af.run,
    jump: af.ada_jump,
    drop: af.ada_drop_platform,
  };
}

/**
 * Resolves weapon-aware walk, run, jump, drop-through, and landing crouch frames
 * uniformly for Leon & Ada while leaving firing, crouching, kicks, and upward aiming untouched.
 */
function getWeaponLocomotionPose(
  player: PlayerState
): { frameIndex: number; usesBakedWeapon: boolean } | null {
  if (player.aimDirY <= -0.45 || player.shootTimer > 0) {
    return null;
  }

  const clips = getHeroWeaponLocomotionClips(player.hero, player.activeWeapon);
  if (!clips) {
    return null;
  }

  if (!player.onGround) {
    const isDropOrFall = player.dropThroughTimer > 0 || player.vy >= -20;
    if (isDropOrFall) {
      const fastFallIdx = clips.drop.length >= 3 && player.vy > 130 ? 1 : 0;
      return { frameIndex: clips.drop[fastFallIdx], usesBakedWeapon: true };
    }
    const jumpIdx = Math.floor(player.animTime * 8) % clips.jump.length;
    return { frameIndex: clips.jump[jumpIdx], usesBakedWeapon: true };
  }

  if (player.crouching) {
    return null;
  }

  if (player.landingTimer > 0 && Math.abs(player.vx) <= 25) {
    return { frameIndex: clips.drop[clips.drop.length - 1], usesBakedWeapon: true };
  }

  if (Math.abs(player.vx) > 10) {
    // Leon with RPG-7 uses base run + overlay RPG launcher while moving horizontally
    if (player.hero === 'leon' && player.activeWeapon === 'rpg') {
      return null;
    }
    const isRun = Math.abs(player.vx) > 95;
    const fps = isRun ? 12 : 9;
    const list = isRun ? clips.run : clips.walk;
    return {
      frameIndex: list[Math.floor(player.animTime * fps) % list.length],
      usesBakedWeapon: true,
    };
  }

  return null;
}

function getPlayerPose(player: PlayerState): { frameIndex: number; usesBakedWeapon: boolean } {
  const frames = CHARACTER_FRAMES[player.hero];
  if (player.hurtTimer > 0) {
    const idx = Math.floor((0.28 - player.hurtTimer) * 14) % frames.hurt.length;
    return { frameIndex: frames.hurt[idx], usesBakedWeapon: true };
  }
  if (player.grenadeTimer > 0) {
    const progress = Math.max(0, Math.min(0.999, 1 - player.grenadeTimer / 0.28));
    const idx = Math.floor(progress * frames.grenade.length);
    return { frameIndex: frames.grenade[idx], usesBakedWeapon: true };
  }
  if (player.meleeTimer > 0) {
    const progress = Math.max(0, Math.min(0.999, (0.28 - player.meleeTimer) / 0.28));
    const idx = Math.floor(progress * frames.melee.length);
    return { frameIndex: frames.melee[idx], usesBakedWeapon: true };
  }
  if (player.dashTimer > 0) {
    const idx = Math.floor(player.animTime * 16) % frames.dash.length;
    return { frameIndex: frames.dash[idx], usesBakedWeapon: true };
  }

  // Weapon-aware locomotion (walk/run/jump/drop/landing) for both Ada and Leon
  const weaponLoco = getWeaponLocomotionPose(player);
  if (weaponLoco) {
    return weaponLoco;
  }

  // Play idle fidget once after a pause, then pause again in calm stance (Leon & Ada)
  if (
    player.onGround &&
    !player.crouching &&
    Math.abs(player.vx) <= 2 &&
    player.landingTimer <= 0 &&
    player.shootTimer <= 0 &&
    player.reloadTimer <= 0 &&
    frames.afkIdle.length > 0
  ) {
    const idlePauseSec = 2.8;
    const afkFps = 4.2;
    const afkDuration = frames.afkIdle.length / afkFps;
    const cycleDuration = idlePauseSec + afkDuration;
    const cyclePos = player.idleTime % cycleDuration;
    if (cyclePos >= idlePauseSec) {
      const afkIdx = Math.min(
        frames.afkIdle.length - 1,
        Math.floor((cyclePos - idlePauseSec) * afkFps)
      );
      return {
        frameIndex: frames.afkIdle[afkIdx],
        usesBakedWeapon: true,
      };
    }
  }

  // Ground horizontal stances & weapon-specific movement loops
  if (player.activeWeapon !== 'pistol' && player.aimDirY > -0.45 && player.onGround) {
    const isFiring = player.shootTimer > 0;
    const isMovingHoriz = Math.abs(player.vx) > 10;
    if (
      player.crouching &&
      player.hero === 'ada' &&
      player.activeWeapon === 'mg' &&
      isFiring
    ) {
      return { frameIndex: CHARACTER_FRAMES.ada.weapons.mg.crouchShoot[0], usesBakedWeapon: true };
    }

    if (!player.crouching) {
      if (isFiring || !isMovingHoriz) {
        const wepSet = frames.weapons[player.activeWeapon];
        if (!isFiring && player.hero === 'ada' && player.activeWeapon === 'rpg' && player.mag.rpg <= 0) {
          return { frameIndex: CHARACTER_FRAMES.ada.weapons.rpg.shoot[1], usesBakedWeapon: true };
        }
        const list = isFiring ? wepSet.shoot : wepSet.idle;
        const idx = isFiring && list.length > 1 && player.shootTimer <= 0.07 ? 1 : 0;
        return { frameIndex: list[Math.min(idx, list.length - 1)], usesBakedWeapon: true };
      }
    }
  }

  if (
    player.shootTimer > 0 &&
    player.activeWeapon === 'pistol' &&
    player.aimDirY > -0.45 &&
    player.onGround &&
    !player.crouching
  ) {
    const idx = player.shootTimer > 0.08 ? 0 : Math.min(1, frames.shoot.length - 1);
    return { frameIndex: frames.shoot[idx], usesBakedWeapon: true };
  }
  if (!player.onGround) {
    if (player.vy < -20) {
      const idx = Math.floor(player.animTime * 8) % frames.jump.length;
      return { frameIndex: frames.jump[idx], usesBakedWeapon: false };
    }
    return { frameIndex: frames.fall[0], usesBakedWeapon: false };
  }
  if (player.crouching) {
    return { frameIndex: frames.crouch[0], usesBakedWeapon: false };
  }
  if (Math.abs(player.vx) > 95) {
    return {
      frameIndex: frames.run[Math.floor(player.animTime * 12) % frames.run.length],
      usesBakedWeapon: player.hero === 'ada' && player.activeWeapon === 'pistol' && player.aimDirY > -0.45,
    };
  }
  if (Math.abs(player.vx) > 10) {
    return {
      frameIndex: frames.walk[Math.floor(player.animTime * 9) % frames.walk.length],
      usesBakedWeapon: player.hero === 'ada' && player.activeWeapon === 'pistol' && player.aimDirY > -0.45,
    };
  }
  return {
    frameIndex: frames.idle[0],
    usesBakedWeapon: false,
  };
}

function drawHandgunOverlay(ctx: CanvasRenderingContext2D, isFiring: boolean): void {
  // Slide & barrel
  ctx.fillStyle = '#e2e8f0';
  ctx.fillRect(-1, -4, 12, 2);
  ctx.fillStyle = '#64748b';
  ctx.fillRect(-1, -2, 11, 1);
  // Frame & grip
  ctx.fillStyle = '#1e293b';
  ctx.fillRect(0, -1, 7, 2);
  ctx.fillRect(-1, 0, 3, 4);
  if (isFiring) {
    ctx.fillStyle = '#ffd447';
    ctx.fillRect(11, -5, 4, 3);
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(12, -4, 3, 1);
  }
}

function drawEquippedWeapon(
  ctx: CanvasRenderingContext2D,
  player: PlayerState,
  assets: GameAssets,
  usesBakedWeapon: boolean
): void {
  if (
    usesBakedWeapon ||
    player.grenadeTimer > 0 ||
    player.meleeTimer > 0 ||
    player.hurtTimer > 0 ||
    player.dashTimer > 0
  ) {
    return;
  }

  if (
    player.activeWeapon === 'pistol' &&
    player.aimDirY > -0.45 &&
    ((player.onGround && !player.crouching) || player.shootTimer <= 0)
  ) {
    return;
  }

  const isFiring = player.shootTimer > (player.activeWeapon === 'mg' ? 0.035 : 0.055);
  const runBob =
    player.onGround && Math.abs(player.vx) > 20 ? Math.round(Math.sin(player.animTime * 14)) : 0;
  const baseHandOffsetY = player.crouching ? -18.5 : -26.5 + runBob;
  const shoulderLift = player.activeWeapon === 'rpg' ? -3.5 : 0;
  const recoilPush = isFiring
    ? player.activeWeapon === 'shotgun'
      ? -1.8
      : player.activeWeapon === 'rpg'
        ? -1.5
        : -1.0
    : 0;

  const pivotX = Math.round(player.x + player.w * 0.5 + player.facing * (3 + recoilPush));
  const pivotY = Math.round(player.y + player.h + baseHandOffsetY + shoulderLift);
  const aimAngle = Math.atan2(player.aimDirY, Math.abs(player.aimDirX) || 1);
  const gloveColor = player.hero === 'ada' ? '#1c1917' : '#d6a07e';

  if (player.activeWeapon === 'pistol') {
    ctx.save();
    ctx.translate(pivotX, pivotY);
    ctx.scale(player.facing, 1);
    ctx.rotate(aimAngle);
    drawHandgunOverlay(ctx, isFiring);
    ctx.fillStyle = gloveColor;
    ctx.fillRect(-1, 0, 2, 2);
    ctx.restore();
    return;
  }

  let sprite: HTMLImageElement;
  let anchorX: number;
  let anchorY: number;

  if (player.activeWeapon === 'shotgun') {
    sprite = isFiring ? assets.weapons.shotgunFire : assets.weapons.shotgunIdle;
    anchorX = 11;
    anchorY = 5.5;
  } else if (player.activeWeapon === 'mg') {
    sprite = isFiring ? assets.weapons.cqbrFire : assets.weapons.cqbrIdle;
    anchorX = 10.5;
    anchorY = 5.5;
  } else {
    sprite = isFiring ? assets.weapons.rpgFire : assets.weapons.rpgIdle;
    anchorX = 17;
    anchorY = 5;
  }

  const drawW = sprite.width * 0.5;
  const drawH = sprite.height * 0.5;

  ctx.save();
  ctx.translate(pivotX, pivotY);
  ctx.scale(player.facing, 1);
  ctx.rotate(aimAngle);
  ctx.drawImage(sprite, -anchorX, -anchorY, drawW, drawH);

  // Subtle gloved/hand grip dots clasping trigger grip and foregrip
  ctx.fillStyle = gloveColor;
  ctx.fillRect(-1, 1, 2, 2);
  ctx.fillRect(6, 1, 2, 2);
  ctx.restore();
}

function getEnemyFrameIndex(enemy: Enemy): number {
  const key =
    enemy.kind === 'boss_salvador'
      ? 'salvador'
      : enemy.kind === 'ganado_fork'
        ? 'ganado_fork'
        : 'ganado_axe';
  const frames = CHARACTER_FRAMES[key];

  if (enemy.state === 'dying' || enemy.state === 'dead') {
    const progress = Math.max(0, Math.min(1, (0.45 - enemy.stateTimer) / 0.45));
    const idx = Math.min(frames.death.length - 1, Math.floor(progress * frames.death.length));
    return frames.death[idx];
  }
  if (enemy.state === 'hurt') {
    return frames.hurt[Math.floor(enemy.animTime * 16) % frames.hurt.length];
  }
  if (enemy.state === 'windup') {
    return frames.windup[Math.floor(enemy.animTime * 10) % frames.windup.length];
  }
  if (enemy.kind === 'boss_salvador') {
    const salvFrames = CHARACTER_FRAMES.salvador;
    if (enemy.vy < -30 || enemy.vy > 40) {
      return salvFrames.leap[Math.floor(enemy.animTime * 9) % salvFrames.leap.length];
    }
    if (enemy.state === 'recover') {
      return salvFrames.land[0];
    }
    if (enemy.state === 'attack') {
      return salvFrames.attack[Math.floor(enemy.animTime * 14) % salvFrames.attack.length];
    }
  } else {
    const gFrames = CHARACTER_FRAMES[enemy.kind === 'ganado_fork' ? 'ganado_fork' : 'ganado_axe'];
    if (enemy.state === 'attack') {
      if (enemy.kind === 'ganado_axe' && enemy.vx === 0 && enemy.vy === 0) {
        return gFrames.throw[0];
      }
      return gFrames.attack[Math.floor(enemy.animTime * 14) % gFrames.attack.length];
    }
    if (enemy.vy < -30 || enemy.vy > 40) {
      return gFrames.jump[Math.floor(enemy.animTime * 8) % gFrames.jump.length];
    }
  }
  if (Math.abs(enemy.vx) > 45) {
    return frames.run[Math.floor(enemy.animTime * 11) % frames.run.length];
  }
  if (Math.abs(enemy.vx) > 6) {
    return frames.walk[Math.floor(enemy.animTime * 8) % frames.walk.length];
  }
  return frames.idle[Math.floor(enemy.animTime * 3.2) % frames.idle.length];
}

function drawVillageBackground(
  ctx: CanvasRenderingContext2D,
  assets: GameAssets,
  biome: BiomeId,
  camX: number,
  time: number
): void {
  const bg = assets.bgVillage;
  const palette = BIOME_PALETTES[biome];

  // Seamless alternating mirrored parallax tiling of the pixelated RE4 village image
  const bgW = VIRTUAL_WIDTH;
  const scroll = (camX * 0.14) % (bgW * 2);

  for (let tileIdx = -1; tileIdx <= 2; tileIdx++) {
    const drawX = Math.floor(tileIdx * bgW - scroll);
    if (drawX + bgW < 0 || drawX > VIRTUAL_WIDTH) continue;

    ctx.save();
    if (Math.abs(tileIdx) % 2 === 1) {
      ctx.translate(drawX + bgW, 0);
      ctx.scale(-1, 1);
      ctx.drawImage(bg, 0, 0, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);
    } else {
      ctx.drawImage(bg, drawX, 0, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);
    }
    ctx.restore();
  }

  // Animated bonfire embers & eerie drifting village mist
  for (let i = 0; i < 16; i++) {
    const ex = ((i * 71 + Math.sin(time * 1.4 + i) * 16 - camX * 0.25) % VIRTUAL_WIDTH + VIRTUAL_WIDTH) % VIRTUAL_WIDTH;
    const ey = ((210 - ((time * 22 + i * 29) % 140)) + VIRTUAL_HEIGHT) % VIRTUAL_HEIGHT;
    ctx.globalAlpha = 0.45 + 0.3 * Math.sin(time * 5 + i);
    ctx.fillStyle = i % 3 === 0 ? '#ffd447' : '#f97316';
    ctx.fillRect(Math.floor(ex), Math.floor(ey), 2, 2);
  }
  ctx.globalAlpha = 1;

  // Subtle biome atmosphere tint so foreground sprites & projectiles stand out sharply
  ctx.fillStyle = palette.tintOverlay;
  ctx.fillRect(0, 0, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);
}

function drawTiles(
  ctx: CanvasRenderingContext2D,
  level: ParsedLevel,
  assets: GameAssets,
  camX: number,
  camY: number
): void {
  const theme = BIOME_PALETTES[level.def.biome];
  const startCol = Math.max(0, Math.floor(camX / TILE_SIZE));
  const endCol = Math.min(level.cols - 1, Math.ceil((camX + VIRTUAL_WIDTH) / TILE_SIZE));
  const startRow = Math.max(0, Math.floor(camY / TILE_SIZE));
  const endRow = Math.min(level.rows - 1, Math.ceil((camY + VIRTUAL_HEIGHT) / TILE_SIZE));

  for (let r = startRow; r <= endRow; r++) {
    for (let c = startCol; c <= endCol; c++) {
      const tile = level.tiles[r][c];
      if (tile === 'empty') continue;

      const x = c * TILE_SIZE;
      const y = r * TILE_SIZE;

      if (tile === 'solid') {
        const aboveEmpty = !isSolidTile(getTileAt(level, c, r - 1));
        ctx.fillStyle = theme.tileBody;
        ctx.fillRect(x, y, TILE_SIZE, TILE_SIZE);

        // Cobblestone / brick mortar detail
        ctx.fillStyle = theme.tileDark;
        ctx.fillRect(x, y + 7, TILE_SIZE, 1);
        ctx.fillRect(x, y + 15, TILE_SIZE, 1);
        ctx.fillRect(x + (r % 2) * 8, y, 1, 8);
        ctx.fillRect(x + ((r + 1) % 2) * 8, y + 8, 1, 8);

        if (aboveEmpty) {
          ctx.fillStyle = theme.tileCap;
          ctx.fillRect(x, y, TILE_SIZE, 4);
          ctx.fillStyle = theme.tileCapAccent;
          ctx.fillRect(x, y, TILE_SIZE, 1);
        }
      } else if (tile === 'oneway') {
        ctx.fillStyle = theme.onewayTop;
        ctx.fillRect(x, y, TILE_SIZE, 3);
        ctx.fillStyle = theme.onewayBody;
        ctx.fillRect(x, y + 3, TILE_SIZE, 3);
        ctx.fillStyle = '#1c1917';
        ctx.fillRect(x + 2, y + 1, 2, 2);
        ctx.fillRect(x + 12, y + 1, 2, 2);
      } else if (tile === 'spring') {
        ctx.drawImage(assets.items.spring, x, y);
      } else if (tile === 'spike_up') {
        for (let i = 0; i < 2; i++) {
          const sx = x + i * 8;
          ctx.fillStyle = '#78716c';
          ctx.beginPath();
          ctx.moveTo(sx + 1, y + 16);
          ctx.lineTo(sx + 4, y + 5);
          ctx.lineTo(sx + 7, y + 16);
          ctx.closePath();
          ctx.fill();
          ctx.fillStyle = '#e7e5e4';
          ctx.fillRect(sx + 3, y + 7, 1, 7);
        }
      } else if (tile === 'spike_down') {
        for (let i = 0; i < 2; i++) {
          const sx = x + i * 8;
          ctx.fillStyle = '#78716c';
          ctx.beginPath();
          ctx.moveTo(sx + 1, y);
          ctx.lineTo(sx + 4, y + 11);
          ctx.lineTo(sx + 7, y);
          ctx.closePath();
          ctx.fill();
          ctx.fillStyle = '#e7e5e4';
          ctx.fillRect(sx + 3, y + 2, 1, 7);
        }
      } else if (tile === 'spike_left') {
        for (let i = 0; i < 2; i++) {
          const sy = y + i * 8;
          ctx.fillStyle = '#78716c';
          ctx.beginPath();
          ctx.moveTo(x + 16, sy + 1);
          ctx.lineTo(x + 5, sy + 4);
          ctx.lineTo(x + 16, sy + 7);
          ctx.closePath();
          ctx.fill();
          ctx.fillStyle = '#e7e5e4';
          ctx.fillRect(x + 7, sy + 3, 7, 1);
        }
      } else if (tile === 'spike_right') {
        for (let i = 0; i < 2; i++) {
          const sy = y + i * 8;
          ctx.fillStyle = '#78716c';
          ctx.beginPath();
          ctx.moveTo(x, sy + 1);
          ctx.lineTo(x + 11, sy + 4);
          ctx.lineTo(x, sy + 7);
          ctx.closePath();
          ctx.fill();
          ctx.fillStyle = '#e7e5e4';
          ctx.fillRect(x + 2, sy + 3, 7, 1);
        }
      }
    }
  }
}

function drawPistolHudIcon(ctx: CanvasRenderingContext2D, cx: number, cy: number): void {
  const ox = Math.round(cx - 5);
  const oy = Math.round(cy - 3);
  // Slide & barrel
  ctx.fillStyle = '#e2e8f0';
  ctx.fillRect(ox + 1, oy, 9, 2);
  ctx.fillStyle = '#94a3b8';
  ctx.fillRect(ox + 1, oy + 2, 8, 1);
  // Frame & grip
  ctx.fillStyle = '#334155';
  ctx.fillRect(ox + 2, oy + 2, 5, 1);
  ctx.fillStyle = '#1e293b';
  ctx.fillRect(ox + 1, oy + 3, 2, 3);
}

function drawHud(ctx: CanvasRenderingContext2D, world: WorldState, assets: GameAssets): void {
  const p = world.player;
  const wMeta = WEAPON_META[p.activeWeapon];

  // Compact top-left character status card (154x20)
  ctx.fillStyle = 'rgba(12, 14, 22, 0.84)';
  ctx.fillRect(5, 4, 154, 20);
  ctx.strokeStyle = '#574c45';
  ctx.lineWidth = 1;
  ctx.strokeRect(5.5, 4.5, 153, 19);

  ctx.font = 'bold 6px "Courier New", monospace';
  ctx.textBaseline = 'top';
  ctx.fillStyle = p.hero === 'leon' ? '#60a5fa' : '#fb7185';
  ctx.fillText(p.hero === 'leon' ? 'LEON' : 'ADA', 8, 7);

  // Segmented HP bar
  for (let i = 0; i < p.maxHp; i++) {
    const bx = 26 + i * 5;
    ctx.fillStyle = i < p.hp ? (p.hp <= 1 ? '#ef4444' : '#4ade80') : '#27272a';
    ctx.fillRect(bx, 7, 4, 4);
    if (i < p.hp) {
      ctx.fillStyle = 'rgba(255,255,255,0.35)';
      ctx.fillRect(bx, 7, 4, 1);
    }
  }

  // Row 1 right: Grenade, Medkit, Score
  ctx.fillStyle = p.grenades > 0 ? '#a3e635' : '#64748b';
  ctx.fillText(`GRN:${p.grenades}`, 69, 7);

  ctx.fillStyle = p.medkits > 0 ? '#4ade80' : '#64748b';
  ctx.fillText(`MED:${p.medkits}`, 91, 7);

  ctx.fillStyle = '#e2e8f0';
  ctx.fillText(`SCR:${String(world.score).padStart(5, '0')}`, 114, 7);

  // Row 2: Dash, Medallions, Pesetas, Timer
  ctx.fillStyle = p.canDash ? '#4deeea' : '#475569';
  ctx.fillText(p.canDash ? 'DASH:RDY' : 'DASH:...', 8, 15);

  const shards = world.shardsCollected.filter(Boolean).length;
  ctx.fillStyle = '#ff9ce3';
  ctx.fillText(`★${shards}/3`, 41, 15);

  ctx.fillStyle = '#ffd447';
  ctx.fillText(`PTAS:${String(world.pesetas).padStart(4, '0')}`, 61, 15);

  ctx.fillStyle = '#94a3b8';
  ctx.fillText(`${world.elapsedTime.toFixed(1)}s`, 114, 15);

  // Compact top-right Weapon HUD (79x25), leaving space on right for matching-height Pause shape
  const wx = VIRTUAL_WIDTH - 107;
  const wy = 4;
  const ww = 79;
  const wh = 25;
  ctx.fillStyle = 'rgba(12, 14, 22, 0.86)';
  ctx.fillRect(wx, wy, ww, wh);
  ctx.strokeStyle = wMeta.color;
  ctx.lineWidth = 1;
  ctx.strokeRect(wx + 0.5, wy + 0.5, ww - 1, wh - 1);

  // Compact Weapon Icon Box (22x13)
  ctx.fillStyle = '#181e30';
  ctx.fillRect(wx + 2, wy + 2, 22, 13);
  ctx.strokeStyle = '#334155';
  ctx.strokeRect(wx + 2.5, wy + 2.5, 21, 12);

  const iconCx = wx + 13;
  const iconCy = wy + 8.5;
  if (p.activeWeapon === 'pistol') {
    drawPistolHudIcon(ctx, iconCx, iconCy);
  } else {
    const iconImg =
      p.activeWeapon === 'shotgun'
        ? assets.weapons.shotgunIdle
        : p.activeWeapon === 'mg'
          ? assets.weapons.cqbrIdle
          : assets.weapons.rpgIdle;
    const scale = Math.min(18 / iconImg.width, 10 / iconImg.height);
    const dw = Math.round(iconImg.width * scale);
    const dh = Math.round(iconImg.height * scale);
    ctx.drawImage(iconImg, Math.round(iconCx - dw * 0.5), Math.round(iconCy - dh * 0.5), dw, dh);
  }

  // Active Weapon Label + Mag / Reserve
  ctx.fillStyle = wMeta.color;
  ctx.font = 'bold 6px "Courier New", monospace';
  ctx.fillText(wMeta.label, wx + 27, wy + 3);

  if (p.reloadTimer > 0 && p.reloadDuration > 0) {
    ctx.fillStyle = '#ffd447';
    ctx.fillText('RELOAD...', wx + 27, wy + 9);
    const prog = 1 - p.reloadTimer / p.reloadDuration;
    ctx.fillStyle = '#1e293b';
    ctx.fillRect(wx + 27, wy + 15, 48, 1);
    ctx.fillStyle = '#ffd447';
    ctx.fillRect(wx + 27, wy + 15, Math.round(48 * prog), 1);
  } else {
    const magStr = String(p.mag[p.activeWeapon]).padStart(2, '0');
    const resStr =
      p.activeWeapon === 'pistol'
        ? '∞'
        : String(Math.max(0, p.reserve[p.activeWeapon])).padStart(3, '0');
    ctx.fillStyle = p.mag[p.activeWeapon] === 0 ? '#ef4444' : '#ffffff';
    ctx.fillText(`${magStr}`, wx + 27, wy + 9);
    ctx.fillStyle = '#94a3b8';
    ctx.fillText(`/${resStr}`, wx + 36, wy + 9);
  }

  // Bottom row: Compact Weapon Slot Pills [1] [2] [3] [4]
  for (let s = 0; s < WEAPON_ORDER.length; s++) {
    const slotW = WEAPON_ORDER[s];
    const sm = WEAPON_META[slotW];
    const owned = slotW === 'pistol' || p.unlockedWeapons[slotW];
    const active = p.activeWeapon === slotW;
    const hasAmmo = slotW === 'pistol' || p.ammo[slotW] > 0;
    const bx = wx + 2 + s * 19;
    const by = wy + 17;

    ctx.fillStyle = active ? 'rgba(255, 255, 255, 0.14)' : '#111524';
    ctx.fillRect(bx, by, 17, 6);
    ctx.strokeStyle = active ? sm.color : owned ? '#475569' : '#1e293b';
    ctx.strokeRect(bx + 0.5, by + 0.5, 16, 5);

    ctx.font = 'bold 6px "Courier New", monospace';
    ctx.fillStyle = active ? sm.color : owned && hasAmmo ? '#cbd5e1' : owned ? '#ef4444' : '#475569';
    const shortTag = slotW === 'pistol' ? 'HG' : slotW === 'shotgun' ? 'SG' : slotW === 'mg' ? 'AR' : 'RL';
    ctx.fillText(`${s + 1}:${shortTag}`, bx + 1, by + 0.5);
  }
  ctx.font = 'bold 6px "Courier New", monospace';

  // Compact stage label + difficulty badge bottom-left
  const diffCfg = DIFFICULTY_CONFIG[world.difficulty];
  ctx.fillStyle = 'rgba(12, 14, 22, 0.78)';
  ctx.fillRect(5, VIRTUAL_HEIGHT - 13, 154, 9);
  ctx.fillStyle = '#d6d3d1';
  ctx.fillText(world.level.def.name.toUpperCase(), 8, VIRTUAL_HEIGHT - 11);
  ctx.fillStyle = diffCfg.color;
  ctx.fillText(`[${diffCfg.shortLabel}]`, 126, VIRTUAL_HEIGHT - 11);
}

function drawBossHealthBar(ctx: CanvasRenderingContext2D, boss: Enemy): void {
  const barW = 180;
  const barH = 9;
  const bx = Math.floor((VIRTUAL_WIDTH - barW) * 0.5);
  const by = VIRTUAL_HEIGHT - 23;

  ctx.fillStyle = 'rgba(14, 10, 16, 0.88)';
  ctx.fillRect(bx - 4, by - 12, barW + 8, barH + 16);
  ctx.strokeStyle = '#ef4444';
  ctx.lineWidth = 1;
  ctx.strokeRect(bx - 3.5, by - 11.5, barW + 7, barH + 15);

  ctx.font = 'bold 8px "Courier New", monospace';
  ctx.fillStyle = '#ffd447';
  ctx.textAlign = 'center';
  ctx.fillText('DR. SALVADOR — CHAINSAW MAN', VIRTUAL_WIDTH * 0.5, by - 10);
  ctx.textAlign = 'left';

  ctx.fillStyle = '#2a1218';
  ctx.fillRect(bx, by, barW, barH);
  const ratio = Math.max(0, boss.hp / boss.maxHp);
  ctx.fillStyle = boss.hp <= boss.maxHp * 0.5 ? '#f97316' : '#ef4444';
  ctx.fillRect(bx + 1, by + 1, Math.floor((barW - 2) * ratio), barH - 2);
}

export function renderHeroPreview(
  ctx: CanvasRenderingContext2D,
  assets: GameAssets,
  hero: HeroId,
  time: number
): void {
  ctx.imageSmoothingEnabled = false;
  ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);
  const frames = CHARACTER_FRAMES[hero].idle;
  const frameIdx = frames[Math.floor(time * 4) % frames.length];
  drawAtlasFrame(ctx, assets.atlases[hero], frameIdx, ctx.canvas.width * 0.5, ctx.canvas.height - 6, 1, 1.3);
}

export function renderMerchantPreview(
  ctx: CanvasRenderingContext2D,
  assets: GameAssets,
  animKey: keyof typeof CHARACTER_FRAMES.merchant,
  time: number
): void {
  ctx.imageSmoothingEnabled = false;
  ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);
  const frames = CHARACTER_FRAMES.merchant[animKey];
  const frameIdx = frames[Math.floor(time * 4) % frames.length];
  drawAtlasFrame(ctx, assets.atlases.merchant, frameIdx, ctx.canvas.width * 0.5, ctx.canvas.height - 6, 1, 1.35);
}

export function renderWorld(
  displayCtx: CanvasRenderingContext2D,
  world: WorldState,
  assets: GameAssets,
  showHud = true
): void {
  const buf = getRenderBuffer();
  const ctx = buf ? buf.ctx : displayCtx;
  ctx.imageSmoothingEnabled = false;

  ctx.save();
  if (buf) {
    ctx.setTransform(RENDER_SCALE, 0, 0, RENDER_SCALE, 0, 0);
  }

  const shakeX =
    world.camera.shakeTimer > 0
      ? Math.round((Math.random() * 2 - 1) * world.camera.shakeMagnitude)
      : 0;
  const shakeY =
    world.camera.shakeTimer > 0
      ? Math.round((Math.random() * 2 - 1) * world.camera.shakeMagnitude)
      : 0;

  const camX = Math.round(world.camera.x) + shakeX;
  const camY = Math.round(world.camera.y) + shakeY;

  drawVillageBackground(ctx, assets, world.level.def.biome, camX, world.elapsedTime);

  ctx.save();
  ctx.translate(-camX, -camY);

  // Signs
  for (const sign of world.level.signs) {
    const sx = Math.round(sign.x);
    const sy = Math.round(sign.y);
    ctx.fillStyle = '#78350f';
    ctx.fillRect(sx + 6, sy + 6, 3, 10);
    ctx.fillStyle = '#b45309';
    ctx.fillRect(sx + 1, sy, 13, 8);
    ctx.fillStyle = '#fde68a';
    ctx.fillRect(sx + 3, sy + 2, 9, 1);
    ctx.fillRect(sx + 3, sy + 5, 7, 1);

    const dist = Math.hypot(world.player.x - sign.x, world.player.y - sign.y);
    if (dist < 76) {
      ctx.font = 'bold 8px "Courier New", monospace';
      const tw = ctx.measureText(sign.text).width;
      const bx = Math.max(camX + 4, Math.round(sx + 7 - (tw + 10) * 0.5));
      const by = sy - 22;
      ctx.fillStyle = 'rgba(15, 23, 42, 0.92)';
      ctx.fillRect(bx, by, tw + 10, 14);
      ctx.strokeStyle = '#ffd447';
      ctx.lineWidth = 1;
      ctx.strokeRect(bx + 0.5, by + 0.5, tw + 9, 13);
      ctx.fillStyle = '#f8fafc';
      ctx.textBaseline = 'top';
      ctx.fillText(sign.text, bx + 5, by + 3);
    }
  }

  // Tiles
  drawTiles(ctx, world.level, assets, camX, camY);

  // Moving platforms
  for (const plat of world.level.platforms) {
    const px = Math.round(plat.x);
    const py = Math.round(plat.y);
    ctx.fillStyle = '#5c3d24';
    ctx.fillRect(px, py, plat.w, plat.h);
    ctx.fillStyle = '#b0713c';
    ctx.fillRect(px, py, plat.w, 3);
    ctx.fillStyle = '#94a3b8';
    ctx.fillRect(px + 4, py + 2, 3, 3);
    ctx.fillRect(px + plat.w - 7, py + 2, 3, 3);
  }

  // Checkpoints (Lantern Shrines)
  for (const cp of world.level.checkpoints) {
    const sprite = cp.activated
      ? assets.items.checkpointOn[Math.floor(world.elapsedTime * 5) % 2]
      : assets.items.checkpointOff;
    ctx.drawImage(sprite, Math.round(cp.x - 1), Math.round(cp.y));
  }

  // Goal (Church Bell Extraction)
  const goalSprite = world.goalUnlocked
    ? assets.items.bell[Math.floor(world.elapsedTime * 5) % 2]
    : assets.items.bellLocked;
  if (world.goalUnlocked) {
    ctx.fillStyle = 'rgba(255, 212, 71, 0.16)';
    ctx.fillRect(Math.round(world.level.goal.x - 3), Math.round(world.level.goal.y - 3), 24, 30);
  }
  ctx.drawImage(goalSprite, Math.round(world.level.goal.x), Math.round(world.level.goal.y));

  // Merchant Cart / Shop Stalls (Invincible Safe Zone)
  for (const m of world.level.merchants) {
    const stallX = Math.round(m.x + m.w * 0.5 - assets.items.merchantStall.width * 0.5);
    const stallY = Math.round(m.y + m.h - assets.items.merchantStall.height);
    const glowAlpha = 0.16 + 0.08 * Math.sin(world.elapsedTime * 6);
    ctx.fillStyle = `rgba(56, 189, 248, ${glowAlpha.toFixed(2)})`;
    ctx.fillRect(stallX - 4, stallY + 12, 18, 22);
    ctx.fillRect(stallX + assets.items.merchantStall.width - 14, stallY + 12, 18, 22);

    ctx.drawImage(assets.items.merchantStall, stallX, stallY);

    const mCx = m.x + m.w * 0.5;
    const dist = Math.hypot(world.player.x + world.player.w * 0.5 - mCx, world.player.y - m.y);
    const mFacing: 1 | -1 = world.player.x + world.player.w * 0.5 < mCx ? -1 : 1;
    const mFrames =
      dist < 64
        ? CHARACTER_FRAMES.merchant.openCoat
        : dist < 110
          ? CHARACTER_FRAMES.merchant.wave
          : CHARACTER_FRAMES.merchant.idle;
    const frameIdx = mFrames[Math.floor(world.elapsedTime * 4) % mFrames.length];
    drawAtlasFrame(ctx, assets.atlases.merchant, frameIdx, mCx, m.y + m.h, mFacing, 1);

    if (world.nearMerchantId === m.id) {
      const prompt = '[B] BUKA TOKO MERCHANT';
      ctx.font = 'bold 8px "Courier New", monospace';
      const tw = ctx.measureText(prompt).width;
      const bx = Math.round(mCx - (tw + 10) * 0.5);
      const by = stallY - 14 + Math.round(Math.sin(world.elapsedTime * 5) * 2);
      ctx.fillStyle = 'rgba(15, 23, 42, 0.94)';
      ctx.fillRect(bx, by, tw + 10, 13);
      ctx.strokeStyle = '#38bdf8';
      ctx.lineWidth = 1;
      ctx.strokeRect(bx + 0.5, by + 0.5, tw + 9, 12);
      ctx.fillStyle = '#ffd447';
      ctx.textBaseline = 'top';
      ctx.fillText(prompt, bx + 5, by + 3);
    }
  }

  // Explosive Red Barrels (taller 18x28)
  for (const barrel of world.level.barrels) {
    if (barrel.exploded) continue;
    ctx.drawImage(assets.items.barrel, Math.round(barrel.x), Math.round(barrel.y));
    if (barrel.flashTimer > 0) {
      ctx.fillStyle = 'rgba(255, 255, 255, 0.55)';
      ctx.fillRect(Math.round(barrel.x), Math.round(barrel.y), barrel.w, barrel.h);
    }
  }

  // Pickups & Crates
  for (const item of world.level.pickups) {
    if (item.collected) continue;
    const bob =
      item.kind === 'coin' || item.kind === 'shard'
        ? Math.round(Math.sin(world.elapsedTime * 4.5 + item.bobOffset) * 2)
        : 0;
    const ix = Math.round(item.x);
    const iy = Math.round(item.y + bob);

    if (item.kind === 'coin') {
      const frame = Math.floor(world.elapsedTime * 8 + item.bobOffset) % assets.items.coin.length;
      ctx.drawImage(assets.items.coin[frame], ix, iy);
    } else if (item.kind === 'shard') {
      ctx.fillStyle = 'rgba(244, 114, 182, 0.22)';
      ctx.fillRect(ix - 2, iy - 2, 16, 16);
      ctx.drawImage(assets.items.shard, ix, iy);
    } else if (item.kind === 'medkit') {
      ctx.drawImage(assets.items.medkit, ix, iy);
    } else if (item.kind === 'grenade_box') {
      ctx.drawImage(assets.items.grenadeBox, ix, iy);
    } else if (item.kind === 'shotgun') {
      ctx.drawImage(assets.items.crateShotgun, ix, iy);
    } else if (item.kind === 'mg') {
      ctx.drawImage(assets.items.crateMg, ix, iy);
    } else if (item.kind === 'rpg') {
      ctx.drawImage(assets.items.crateRpg, ix, iy);
    } else if (item.kind === 'ammo') {
      ctx.drawImage(assets.items.crateAmmo, ix, iy);
    } else if (item.kind === 'ammo_shotgun') {
      const dropBob = Math.round(Math.sin(world.elapsedTime * 5 + item.bobOffset) * 1.5);
      ctx.fillStyle = 'rgba(251, 146, 60, 0.2)';
      ctx.fillRect(ix - 1, iy + dropBob - 1, 17, 11);
      ctx.drawImage(assets.weapons.ammoShotgun, ix, iy + dropBob, 15, 9);
    } else if (item.kind === 'ammo_mg') {
      const dropBob = Math.round(Math.sin(world.elapsedTime * 5 + item.bobOffset) * 1.5);
      ctx.fillStyle = 'rgba(77, 238, 234, 0.2)';
      ctx.fillRect(ix - 1, iy + dropBob - 1, 17, 11);
      ctx.drawImage(assets.weapons.ammoCqbr, ix, iy + dropBob, 15, 9);
    }
  }

  // Enemies (Ganados & Dr. Salvador Boss)
  for (const enemy of world.level.enemies) {
    if (enemy.state === 'dead') continue;
    if (enemy.state === 'dying' && enemy.deathFadeTimer < 0.45 && Math.floor(enemy.deathFadeTimer * 20) % 2 === 0) {
      continue;
    }

    const atlasKey =
      enemy.kind === 'boss_salvador'
        ? 'salvador'
        : enemy.kind === 'ganado_fork'
          ? 'ganado_fork'
          : 'ganado_axe';
    const frameIdx = getEnemyFrameIndex(enemy);
    const footX = enemy.x + enemy.w * 0.5;
    const footY = enemy.y + enemy.h;

    if (enemy.kind === 'boss_salvador') {
      ctx.fillStyle =
        enemy.hp <= enemy.maxHp * 0.5 ? 'rgba(249, 115, 22, 0.22)' : 'rgba(239, 68, 68, 0.16)';
      ctx.fillRect(Math.round(enemy.x - 4), Math.round(enemy.y - 2), enemy.w + 8, enemy.h + 2);
    }

    drawAtlasFrame(ctx, assets.atlases[atlasKey], frameIdx, footX, footY, enemy.facing, enemy.scale);

    if (enemy.hurtFlashTimer > 0) {
      ctx.fillStyle = 'rgba(255, 255, 255, 0.45)';
      ctx.fillRect(Math.round(enemy.x), Math.round(enemy.y), enemy.w, enemy.h);
    }

    if (enemy.state === 'windup') {
      ctx.fillStyle = '#ffd447';
      ctx.font = 'bold 10px "Courier New", monospace';
      ctx.fillText('!', Math.round(footX - 3), Math.round(enemy.y - 12));
    }

    // Small HP bar above damaged multi-HP enemies
    if (enemy.state !== 'dying' && enemy.kind !== 'boss_salvador' && enemy.hp < enemy.maxHp) {
      const bw = 16;
      const bx = Math.round(footX - bw * 0.5);
      const by = Math.round(enemy.y - 6);
      ctx.fillStyle = '#18181b';
      ctx.fillRect(bx, by, bw, 3);
      ctx.fillStyle = '#ef4444';
      ctx.fillRect(bx, by, Math.ceil((enemy.hp / enemy.maxHp) * bw), 3);
    }
  }

  // Dash Afterimages
  const p = world.player;
  for (const img of world.afterimages) {
    ctx.globalAlpha = (img.life / img.maxLife) * 0.45;
    drawAtlasFrame(
      ctx,
      assets.atlases[p.hero],
      img.frameIndex,
      img.x + p.w * 0.5,
      img.y + p.h,
      img.facing,
      1
    );
  }
  ctx.globalAlpha = 1;

  // Player (Leon or Ada) + Equipped Weapon
  const invulnBlink = p.invulnTimer > 0 && Math.floor(p.invulnTimer * 18) % 2 === 0;
  if (!invulnBlink) {
    const pose = getPlayerPose(p);
    drawAtlasFrame(ctx, assets.atlases[p.hero], pose.frameIndex, p.x + p.w * 0.5, p.y + p.h, p.facing, 1);
    drawEquippedWeapon(ctx, p, assets, pose.usesBakedWeapon);

    // Subtle upward aim sight arrow when holding Up
    if (p.aimDirY < -0.5) {
      ctx.fillStyle = '#ffd447';
      ctx.fillRect(Math.round(p.x + p.w * 0.5 + p.aimDirX * 14), Math.round(p.y + 8 + p.aimDirY * 14), 2, 2);
    }
  }

  // Projectiles
  for (const proj of world.projectiles) {
    if (proj.kind === 'enemy_axe') {
      ctx.save();
      ctx.translate(Math.round(proj.x + proj.w * 0.5), Math.round(proj.y + proj.h * 0.5));
      ctx.rotate(proj.rotation);
      ctx.drawImage(assets.thrownAxe, -10, -10, 20, 20);
      ctx.restore();
    } else if (proj.kind === 'grenade') {
      const gx = Math.round(proj.x);
      const gy = Math.round(proj.y);
      ctx.fillStyle = '#365314';
      ctx.fillRect(gx, gy, 6, 6);
      ctx.fillStyle = Math.floor(proj.life * 16) % 2 === 0 ? '#ef4444' : '#ffd447';
      ctx.fillRect(gx + 2, gy - 1, 2, 2);
    } else if (proj.kind === 'rpg') {
      ctx.save();
      ctx.translate(Math.round(proj.x + proj.w * 0.5), Math.round(proj.y + proj.h * 0.5));
      ctx.rotate(proj.rotation || Math.atan2(proj.vy, proj.vx));
      ctx.drawImage(assets.weapons.rpgRocket, -10, -2, 16, 4);
      ctx.restore();
    } else if (proj.kind === 'pellet') {
      ctx.fillStyle = '#fb923c';
      ctx.fillRect(Math.round(proj.x), Math.round(proj.y), 4, 4);
      ctx.fillStyle = '#fef08a';
      ctx.fillRect(Math.round(proj.x + 1), Math.round(proj.y + 1), 2, 2);
    } else {
      ctx.fillStyle = proj.kind === 'mg' ? '#4deeea' : '#ffd447';
      ctx.fillRect(Math.round(proj.x), Math.round(proj.y), proj.w, proj.h);
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(Math.round(proj.x + 1), Math.round(proj.y + 1), proj.w - 2, 1);
    }
  }

  // Pixel Fire Explosions
  for (const ex of world.explosions) {
    const t = 1 - ex.life / ex.maxLife;
    const r = ex.radius * (0.4 + t * 0.75);
    ctx.fillStyle = t < 0.35 ? '#ffffff' : t < 0.7 ? '#ffd447' : '#f97316';
    ctx.globalAlpha = Math.max(0, 1 - t * 0.85);
    ctx.beginPath();
    ctx.arc(Math.round(ex.x), Math.round(ex.y), Math.round(r), 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = '#ef4444';
    ctx.beginPath();
    ctx.arc(Math.round(ex.x), Math.round(ex.y), Math.round(r * 0.65), 0, Math.PI * 2);
    ctx.fill();
    ctx.globalAlpha = 1;
  }

  // Particles & Ejected Shell Casings
  for (const pt of world.particles) {
    ctx.globalAlpha = Math.max(0, pt.life / pt.maxLife);
    if (pt.sprite === 'shell_shotgun' || pt.sprite === 'shell_cqbr') {
      const sImg =
        pt.sprite === 'shell_shotgun' ? assets.weapons.shellShotgun : assets.weapons.shellCqbr;
      const sz = pt.sprite === 'shell_shotgun' ? 5 : 4;
      ctx.save();
      ctx.translate(Math.round(pt.x), Math.round(pt.y));
      ctx.rotate(pt.rotation ?? 0);
      ctx.drawImage(sImg, -sz * 0.5, -sz * 0.5, sz, sz);
      ctx.restore();
    } else {
      ctx.fillStyle = pt.color;
      ctx.fillRect(Math.round(pt.x), Math.round(pt.y), pt.size, pt.size);
    }
  }
  ctx.globalAlpha = 1;

  // Floating Texts
  ctx.font = 'bold 8px "Courier New", monospace';
  ctx.textAlign = 'center';
  for (const ft of world.floatingTexts) {
    ctx.globalAlpha = Math.max(0, ft.life / ft.maxLife);
    ctx.fillStyle = '#0f172a';
    ctx.fillText(ft.text, Math.round(ft.x) + 1, Math.round(ft.y) + 1);
    ctx.fillStyle = ft.color;
    ctx.fillText(ft.text, Math.round(ft.x), Math.round(ft.y));
  }
  ctx.textAlign = 'left';
  ctx.globalAlpha = 1;

  ctx.restore();

  if (showHud) {
    drawHud(ctx, world, assets);
    const livingBoss = world.level.enemies.find(
      (e) => e.kind === 'boss_salvador' && e.state !== 'dying' && e.state !== 'dead'
    );
    if (livingBoss && world.player.x > livingBoss.patrolMinX - 110) {
      drawBossHealthBar(ctx, livingBoss);
    }
  }

  ctx.restore();

  if (buf) {
    displayCtx.imageSmoothingEnabled = false;
    displayCtx.clearRect(0, 0, displayCtx.canvas.width, displayCtx.canvas.height);
    displayCtx.drawImage(
      buf.canvas,
      0,
      0,
      displayCtx.canvas.width,
      displayCtx.canvas.height
    );
  }
}
