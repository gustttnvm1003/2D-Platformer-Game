import { getTileAt, isSolidTile, overlapsAnySolid, rectsOverlap, resolveHorizontalTiles } from './physics';
import { DIFFICULTY_CONFIG, DifficultyId, Enemy, ParsedLevel, PlayerState, Rect, TILE_SIZE } from './types';

function hasGroundAhead(level: ParsedLevel, enemy: Enemy, dir: 1 | -1): boolean {
  const probeX = dir > 0 ? enemy.x + enemy.w + 2 : enemy.x - 2;
  const probeY = enemy.y + enemy.h + 2;
  const col = Math.floor(probeX / TILE_SIZE);
  const row = Math.floor(probeY / TILE_SIZE);
  const tile = getTileAt(level, col, row);
  return tile === 'solid' || tile === 'oneway';
}

function hitsWallAhead(level: ParsedLevel, enemy: Enemy, dir: 1 | -1): boolean {
  const probeX = dir > 0 ? enemy.x + enemy.w + 1 : enemy.x - 1;
  const col = Math.floor(probeX / TILE_SIZE);
  const topRow = Math.floor((enemy.y + 4) / TILE_SIZE);
  const botRow = Math.floor((enemy.y + enemy.h - 4) / TILE_SIZE);
  for (let r = topRow; r <= botRow; r++) {
    if (isSolidTile(getTileAt(level, col, r))) {
      return true;
    }
  }
  return false;
}

export interface EnemyUpdateEvents {
  thrownAxes: Array<{ x: number; y: number; vx: number; vy: number; facing: 1 | -1 }>;
  meleeHitsPlayer: Enemy[];
  bossChainsawRevs: Enemy[];
  bossLandings: Enemy[];
}

export function damageEnemy(
  enemy: Enemy,
  amount: number,
  knockDir: number,
  knockbackMult = 1
): boolean {
  if (enemy.state === 'dying' || enemy.state === 'dead') return false;
  enemy.hp = Math.max(0, enemy.hp - amount);
  enemy.hurtFlashTimer = 0.16;

  if (enemy.hp <= 0) {
    enemy.state = 'dying';
    enemy.stateTimer = 0.45;
    enemy.deathFadeTimer = 1.6;
    enemy.vx = knockDir * Math.round(55 * Math.min(2.2, knockbackMult));
    enemy.vy = Math.min(enemy.vy, -80);
    return true;
  }

  if (enemy.kind !== 'boss_salvador') {
    enemy.state = 'hurt';
    enemy.stateTimer = knockbackMult > 1.4 ? 0.28 : 0.22;
    enemy.vx = knockDir * Math.round(70 * knockbackMult);
    if (knockbackMult > 1.4 && enemy.vy >= 0) {
      enemy.vy = -55;
    }
  } else if (enemy.state === 'patrol' || enemy.state === 'chase') {
    enemy.vx = knockDir * Math.round(25 * knockbackMult);
  }
  return false;
}

export function updateEnemies(
  enemies: Enemy[],
  level: ParsedLevel,
  player: PlayerState,
  dt: number,
  difficulty: DifficultyId = 'normal'
): EnemyUpdateEvents {
  const speedMult = DIFFICULTY_CONFIG[difficulty].enemySpeedMult;
  const events: EnemyUpdateEvents = {
    thrownAxes: [],
    meleeHitsPlayer: [],
    bossChainsawRevs: [],
    bossLandings: [],
  };

  const playerCx = player.x + player.w * 0.5;
  const playerCy = player.y + player.h * 0.5;

  for (const enemy of enemies) {
    if (enemy.state === 'dead') continue;

    enemy.animTime += dt;
    enemy.hurtFlashTimer = Math.max(0, enemy.hurtFlashTimer - dt);
    enemy.attackCooldown = Math.max(0, enemy.attackCooldown - dt);

    if (enemy.state === 'dying') {
      enemy.stateTimer = Math.max(0, enemy.stateTimer - dt);
      enemy.deathFadeTimer -= dt;
      enemy.vy += 800 * dt;
      enemy.vx *= Math.max(0, 1 - dt * 5);
      enemy.x += enemy.vx * dt;
      resolveHorizontalTiles(level, enemy);
      enemy.y += enemy.vy * dt;
      if (overlapsAnySolid(level, enemy)) {
        const footRow = Math.floor((enemy.y + enemy.h - 0.01) / TILE_SIZE);
        enemy.y = footRow * TILE_SIZE - enemy.h;
        enemy.vy = 0;
      }
      if (enemy.deathFadeTimer <= 0) {
        enemy.state = 'dead';
      }
      continue;
    }

    const enemyCx = enemy.x + enemy.w * 0.5;
    const enemyCy = enemy.y + enemy.h * 0.5;
    const dx = playerCx - enemyCx;
    const dy = playerCy - enemyCy;
    const absDx = Math.abs(dx);
    const absDy = Math.abs(dy);

    // Apply gravity if airborne (especially boss leap)
    enemy.vy += 820 * dt;
    if (enemy.vy > 320) enemy.vy = 320;

    if (enemy.state === 'hurt') {
      enemy.stateTimer -= dt;
      enemy.vx *= Math.max(0, 1 - dt * 7);
      if (enemy.stateTimer <= 0) {
        enemy.state = 'chase';
      }
    } else if (enemy.state === 'windup') {
      enemy.vx = 0;
      enemy.stateTimer -= dt;
      if (enemy.stateTimer <= 0) {
        enemy.state = 'attack';
        enemy.attackDidHit = false;
        if (enemy.kind === 'boss_salvador') {
          const enraged = enemy.hp <= enemy.maxHp * 0.5;
          if (absDx > 52 && (absDy > 24 || Math.floor(enemy.animTime * 10) % 2 === 0)) {
            // Overhead Chainsaw Leap Slam
            enemy.vx = enemy.facing * Math.round((enraged ? 150 : 132) * speedMult);
            enemy.vy = -290;
            enemy.stateTimer = 0.72;
            enemy.attackCooldown = enraged ? 1.15 : 1.55;
          } else {
            // Relentless Chainsaw Rush Slash
            enemy.vx = enemy.facing * Math.round((enraged ? 160 : 132) * speedMult);
            enemy.stateTimer = 0.48;
            enemy.attackCooldown = enraged ? 0.9 : 1.25;
          }
        } else if (enemy.kind === 'ganado_axe' && absDx > 36) {
          events.thrownAxes.push({
            x: enemyCx + enemy.facing * 8,
            y: enemy.y + 10,
            vx: enemy.facing * 145,
            vy: -85,
            facing: enemy.facing,
          });
          enemy.stateTimer = 0.22;
          enemy.attackCooldown = 2.0;
        } else {
          // Melee lunge
          enemy.vx = enemy.facing * Math.round((enemy.kind === 'ganado_fork' ? 75 : 50) * speedMult);
          enemy.stateTimer = 0.24;
          enemy.attackCooldown = 1.0;
        }
      }
    } else if (enemy.state === 'attack') {
      enemy.stateTimer -= dt;
      // Check melee / chainsaw strike box during attack
      if (!enemy.attackDidHit && absDx < 44) {
        const strikeBox: Rect = {
          x: enemy.facing > 0 ? enemy.x + enemy.w * 0.5 : enemy.x - 18,
          y: enemy.y + 4,
          w: enemy.kind === 'boss_salvador' ? 28 : 22,
          h: enemy.h - 6,
        };
        if (rectsOverlap(strikeBox, player)) {
          enemy.attackDidHit = true;
          events.meleeHitsPlayer.push(enemy);
        }
      }
      if (enemy.stateTimer <= 0) {
        enemy.vx = 0;
        enemy.state = 'recover';
        enemy.stateTimer = enemy.kind === 'boss_salvador' ? 0.52 : 0.42;
      }
    } else if (enemy.state === 'recover') {
      enemy.vx = 0;
      enemy.stateTimer -= dt;
      if (enemy.stateTimer <= 0) {
        enemy.state = 'chase';
      }
    } else if (enemy.state === 'patrol') {
      const speed = (enemy.kind === 'ganado_fork' ? 32 : 26) * speedMult;
      enemy.vx = enemy.facing * speed;
      if (
        enemy.x + enemy.vx * dt <= enemy.patrolMinX ||
        enemy.x + enemy.vx * dt >= enemy.patrolMaxX ||
        hitsWallAhead(level, enemy, enemy.facing) ||
        !hasGroundAhead(level, enemy, enemy.facing)
      ) {
        enemy.facing = (-enemy.facing) as 1 | -1;
        enemy.vx = 0;
      }

      const aggroDist = enemy.kind === 'boss_salvador' ? 240 : 165;
      if (absDx < aggroDist && absDy < 80) {
        enemy.state = 'chase';
      }
    } else if (enemy.state === 'chase') {
      enemy.facing = dx >= 0 ? 1 : -1;
      const enraged = enemy.kind === 'boss_salvador' && enemy.hp <= enemy.maxHp * 0.5;
      const chaseSpeed =
        (enemy.kind === 'ganado_fork'
          ? 68
          : enemy.kind === 'boss_salvador'
            ? enraged
              ? 88
              : 72
            : 52) * speedMult;

      if (absDx <= 32 && absDy < 34) {
        enemy.state = 'windup';
        enemy.stateTimer = enemy.kind === 'boss_salvador' ? 0.34 : 0.28;
        enemy.vx = 0;
        if (enemy.kind === 'boss_salvador') events.bossChainsawRevs.push(enemy);
      } else if (
        (enemy.kind === 'ganado_axe' || enemy.kind === 'boss_salvador') &&
        absDx > 48 &&
        absDx < 175 &&
        absDy < 72 &&
        enemy.attackCooldown <= 0
      ) {
        enemy.state = 'windup';
        enemy.stateTimer = enemy.kind === 'boss_salvador' ? 0.36 : 0.36;
        enemy.vx = 0;
        if (enemy.kind === 'boss_salvador') events.bossChainsawRevs.push(enemy);
      } else {
        const canStep =
          enemy.x + enemy.facing * chaseSpeed * dt >= enemy.patrolMinX &&
          enemy.x + enemy.facing * chaseSpeed * dt <= enemy.patrolMaxX &&
          !hitsWallAhead(level, enemy, enemy.facing) &&
          hasGroundAhead(level, enemy, enemy.facing);
        enemy.vx = canStep ? enemy.facing * chaseSpeed : 0;
      }

      if (absDx > 270) {
        enemy.state = 'patrol';
      }
    }

    // Integrate X with patrol & tile bounds
    const nextX = enemy.x + enemy.vx * dt;
    if (nextX >= enemy.patrolMinX && nextX <= enemy.patrolMaxX) {
      enemy.x = nextX;
    } else {
      enemy.x = Math.max(enemy.patrolMinX, Math.min(enemy.patrolMaxX, nextX));
      enemy.vx = 0;
    }
    resolveHorizontalTiles(level, enemy);

    // Integrate Y onto solid/oneway floor
    const prevBot = enemy.y + enemy.h;
    const wasAirborne = enemy.vy > 80;
    enemy.y += enemy.vy * dt;
    const minCol = Math.floor(enemy.x / TILE_SIZE);
    const maxCol = Math.floor((enemy.x + enemy.w - 0.01) / TILE_SIZE);
    const botRow = Math.floor((enemy.y + enemy.h - 0.01) / TILE_SIZE);
    for (let c = minCol; c <= maxCol; c++) {
      const t = getTileAt(level, c, botRow);
      if (t === 'solid' || (t === 'oneway' && prevBot <= botRow * TILE_SIZE + 5)) {
        const top = botRow * TILE_SIZE;
        if (enemy.y + enemy.h >= top) {
          enemy.y = top - enemy.h;
          enemy.vy = 0;
          if (wasAirborne && enemy.kind === 'boss_salvador') {
            events.bossLandings.push(enemy);
          }
          break;
        }
      }
    }
  }

  return events;
}
