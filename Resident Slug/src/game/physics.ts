import {
  HERO_CONFIG,
  InputState,
  MovingPlatform,
  ParsedLevel,
  PlayerState,
  Rect,
  TILE_SIZE,
  TileKind,
} from './types';

export const PHYSICS = {
  GRAVITY: 900,
  SHORT_HOP_GRAVITY_MULT: 1.95,
  MAX_FALL_SPEED: 320,
  WALL_SLIDE_SPEED: 56,
  MAX_RUN_SPEED: 132,
  CROUCH_MOVE_MULT: 0.45,
  GROUND_ACCEL: 1250,
  GROUND_DECEL: 1400,
  AIR_ACCEL: 880,
  AIR_DECEL: 400,
  JUMP_VEL: -325,
  DOUBLE_JUMP_VEL: -295,
  WALL_JUMP_VX: 165,
  WALL_JUMP_VY: -300,
  SPRING_VEL: -440,
  DASH_SPEED: 320,
  DASH_DURATION: 0.16,
  DASH_COOLDOWN: 0.28,
  COYOTE_TIME: 0.11,
  JUMP_BUFFER_TIME: 0.12,
  WALL_JUMP_LOCK: 0.14,
  CORNER_CORRECTION_PX: 4,
} as const;

export function rectsOverlap(a: Rect, b: Rect): boolean {
  return (
    a.x < b.x + b.w &&
    a.x + a.w > b.x &&
    a.y < b.y + b.h &&
    a.y + a.h > b.y
  );
}

export function getTileAt(level: ParsedLevel, col: number, row: number): TileKind {
  if (col < 0 || col >= level.cols) {
    return 'solid';
  }
  if (row < 0 || row >= level.rows) {
    return 'empty';
  }
  return level.tiles[row][col];
}

export function isSolidTile(kind: TileKind): boolean {
  return kind === 'solid';
}

export function isSpikeTile(kind: TileKind): boolean {
  return (
    kind === 'spike_up' ||
    kind === 'spike_down' ||
    kind === 'spike_left' ||
    kind === 'spike_right'
  );
}

export function getSpikeRect(col: number, row: number, kind: TileKind): Rect | null {
  const x = col * TILE_SIZE;
  const y = row * TILE_SIZE;
  switch (kind) {
    case 'spike_up':
      return { x: x + 3, y: y + 9, w: 10, h: 7 };
    case 'spike_down':
      return { x: x + 3, y, w: 10, h: 7 };
    case 'spike_left':
      return { x: x + 9, y: y + 3, w: 7, h: 10 };
    case 'spike_right':
      return { x, y: y + 3, w: 7, h: 10 };
    default:
      return null;
  }
}

export function checkSpikeCollision(level: ParsedLevel, rect: Rect): boolean {
  const minCol = Math.floor(rect.x / TILE_SIZE);
  const maxCol = Math.floor((rect.x + rect.w - 0.01) / TILE_SIZE);
  const minRow = Math.floor(rect.y / TILE_SIZE);
  const maxRow = Math.floor((rect.y + rect.h - 0.01) / TILE_SIZE);

  for (let r = minRow; r <= maxRow; r++) {
    for (let c = minCol; c <= maxCol; c++) {
      const tile = getTileAt(level, c, r);
      const spikeRect = getSpikeRect(c, r, tile);
      if (spikeRect && rectsOverlap(rect, spikeRect)) {
        return true;
      }
    }
  }
  return false;
}

export function checkSpringCollision(level: ParsedLevel, rect: Rect): boolean {
  const minCol = Math.floor(rect.x / TILE_SIZE);
  const maxCol = Math.floor((rect.x + rect.w - 0.01) / TILE_SIZE);
  const minRow = Math.floor(rect.y / TILE_SIZE);
  const maxRow = Math.floor((rect.y + rect.h - 0.01) / TILE_SIZE);

  for (let r = minRow; r <= maxRow; r++) {
    for (let c = minCol; c <= maxCol; c++) {
      if (getTileAt(level, c, r) === 'spring') {
        const springRect: Rect = {
          x: c * TILE_SIZE + 2,
          y: r * TILE_SIZE + 8,
          w: 12,
          h: 8,
        };
        if (rectsOverlap(rect, springRect)) {
          return true;
        }
      }
    }
  }
  return false;
}

export function overlapsAnySolid(level: ParsedLevel, rect: Rect): boolean {
  const minCol = Math.floor(rect.x / TILE_SIZE);
  const maxCol = Math.floor((rect.x + rect.w - 0.01) / TILE_SIZE);
  const minRow = Math.floor(rect.y / TILE_SIZE);
  const maxRow = Math.floor((rect.y + rect.h - 0.01) / TILE_SIZE);

  for (let r = minRow; r <= maxRow; r++) {
    for (let c = minCol; c <= maxCol; c++) {
      if (isSolidTile(getTileAt(level, c, r))) {
        return true;
      }
    }
  }
  return false;
}

export function updateMovingPlatforms(platforms: MovingPlatform[], dt: number): void {
  for (const plat of platforms) {
    plat.prevX = plat.x;
    plat.prevY = plat.y;

    if (plat.pauseTimer > 0) {
      plat.pauseTimer = Math.max(0, plat.pauseTimer - dt);
      continue;
    }

    const dxTotal = plat.endX - plat.startX;
    const dyTotal = plat.endY - plat.startY;
    const dist = Math.hypot(dxTotal, dyTotal);
    if (dist < 0.001) continue;

    const deltaProgress = (plat.speed * dt) / dist;
    plat.progress += deltaProgress * plat.direction;

    if (plat.progress >= 1) {
      plat.progress = 1;
      plat.direction = -1;
      plat.pauseTimer = plat.pauseDuration;
    } else if (plat.progress <= 0) {
      plat.progress = 0;
      plat.direction = 1;
      plat.pauseTimer = plat.pauseDuration;
    }

    plat.x = plat.startX + dxTotal * plat.progress;
    plat.y = plat.startY + dyTotal * plat.progress;
  }
}

export function resolveHorizontalTiles(level: ParsedLevel, entity: Rect & { vx: number }): boolean {
  let hitWall = false;
  const minCol = Math.floor(entity.x / TILE_SIZE);
  const maxCol = Math.floor((entity.x + entity.w - 0.01) / TILE_SIZE);
  const minRow = Math.floor(entity.y / TILE_SIZE);
  const maxRow = Math.floor((entity.y + entity.h - 0.01) / TILE_SIZE);

  for (let r = minRow; r <= maxRow; r++) {
    for (let c = minCol; c <= maxCol; c++) {
      if (!isSolidTile(getTileAt(level, c, r))) continue;
      const tileRect: Rect = {
        x: c * TILE_SIZE,
        y: r * TILE_SIZE,
        w: TILE_SIZE,
        h: TILE_SIZE,
      };
      if (!rectsOverlap(entity, tileRect)) continue;
      hitWall = true;
      if (entity.vx > 0) {
        entity.x = tileRect.x - entity.w;
      } else if (entity.vx < 0) {
        entity.x = tileRect.x + tileRect.w;
      }
      entity.vx = 0;
    }
  }
  return hitWall;
}

function detectWallTouch(level: ParsedLevel, player: PlayerState): -1 | 0 | 1 {
  const probeLeft: Rect = { x: player.x - 1, y: player.y + 4, w: 1, h: player.h - 8 };
  if (overlapsAnySolid(level, probeLeft)) return -1;

  const probeRight: Rect = { x: player.x + player.w, y: player.y + 4, w: 1, h: player.h - 8 };
  if (overlapsAnySolid(level, probeRight)) return 1;

  return 0;
}

function isStandingOnlyOnOneWay(level: ParsedLevel, player: PlayerState): boolean {
  if (!player.onGround) return false;
  const footRow = Math.floor((player.y + player.h + 1) / TILE_SIZE);
  const minCol = Math.floor(player.x / TILE_SIZE);
  const maxCol = Math.floor((player.x + player.w - 0.01) / TILE_SIZE);

  let foundOneWay = false;
  for (let c = minCol; c <= maxCol; c++) {
    const tile = getTileAt(level, c, footRow);
    if (isSolidTile(tile)) return false;
    if (tile === 'oneway') foundOneWay = true;
  }
  return foundOneWay;
}

export interface PlayerStepOutcome {
  jumped: boolean;
  doubleJumped: boolean;
  wallJumped: boolean;
  dashed: boolean;
  landed: boolean;
  sprung: boolean;
  wallDirection: -1 | 0 | 1;
}

export function stepPlayerPhysics(
  player: PlayerState,
  level: ParsedLevel,
  input: InputState,
  dt: number
): PlayerStepOutcome {
  const outcome: PlayerStepOutcome = {
    jumped: false,
    doubleJumped: false,
    wallJumped: false,
    dashed: false,
    landed: false,
    sprung: false,
    wallDirection: 0,
  };

  const heroStats = HERO_CONFIG[player.hero];
  const wasOnGround = player.onGround;

  // Carry player with standing moving platform
  if (player.onGround && player.standingPlatformId) {
    const plat = level.platforms.find((p) => p.id === player.standingPlatformId);
    if (plat) {
      const dx = plat.x - plat.prevX;
      const dy = plat.y - plat.prevY;
      if (dx !== 0) {
        player.x += dx;
        const carryChecker = { x: player.x, y: player.y, w: player.w, h: player.h, vx: dx };
        resolveHorizontalTiles(level, carryChecker);
        player.x = carryChecker.x;
      }
      if (dy !== 0) {
        player.y += dy;
      }
    }
  }

  // Update timers
  player.animTime += dt;
  player.coyoteTimer = Math.max(0, player.coyoteTimer - dt);
  player.jumpBufferTimer = Math.max(0, player.jumpBufferTimer - dt);
  player.wallJumpLockTimer = Math.max(0, player.wallJumpLockTimer - dt);
  player.dropThroughTimer = Math.max(0, player.dropThroughTimer - dt);
  player.landingTimer = Math.max(0, player.landingTimer - dt);
  player.invulnTimer = Math.max(0, player.invulnTimer - dt);
  player.hurtTimer = Math.max(0, player.hurtTimer - dt);
  player.shootTimer = Math.max(0, player.shootTimer - dt);
  player.shootCooldown = Math.max(0, player.shootCooldown - dt);
  player.grenadeTimer = Math.max(0, player.grenadeTimer - dt);
  player.meleeTimer = Math.max(0, player.meleeTimer - dt);
  player.meleeCooldown = Math.max(0, player.meleeCooldown - dt);
  player.dashCooldown = Math.max(0, player.dashCooldown - dt);

  if (input.jumpPressed) {
    player.jumpBufferTimer = PHYSICS.JUMP_BUFFER_TIME;
  }

  const moveDir = (input.right ? 1 : 0) - (input.left ? 1 : 0);
  if (moveDir !== 0 && player.wallJumpLockTimer <= 0 && player.dashTimer <= 0) {
    player.facing = moveDir > 0 ? 1 : -1;
  }

  // Update crouch state on ground (or drop through one-way platforms)
  const wantsCrouch = player.onGround && input.down && player.dashTimer <= 0;
  if (wantsCrouch && isStandingOnlyOnOneWay(level, player) && input.jumpPressed) {
    player.dropThroughTimer = 0.16;
    player.landingTimer = 0;
    player.onGround = false;
    player.coyoteTimer = 0;
    player.jumpBufferTimer = 0;
    player.y += 2;
    player.vy = Math.max(player.vy, 60);
  }

  if (wantsCrouch && !isStandingOnlyOnOneWay(level, player)) {
    if (!player.crouching) {
      player.y += player.standingH - player.crouchH;
      player.h = player.crouchH;
      player.crouching = true;
    }
  } else if (player.crouching) {
    // Check ceiling clearance before standing up
    const standRect: Rect = {
      x: player.x,
      y: player.y - (player.standingH - player.crouchH),
      w: player.w,
      h: player.standingH,
    };
    if (!overlapsAnySolid(level, standRect)) {
      player.y = standRect.y;
      player.h = player.standingH;
      player.crouching = false;
    }
  }

  // Aim direction (horizontal, straight up, or 45-deg diagonal up like Metal Slug)
  if (input.up) {
    if (moveDir !== 0) {
      const inv = 1 / Math.SQRT2;
      player.aimDirX = moveDir * inv;
      player.aimDirY = -inv;
    } else {
      player.aimDirX = 0;
      player.aimDirY = -1;
    }
  } else {
    player.aimDirX = player.facing;
    player.aimDirY = 0;
  }

  const agiJumpMult = heroStats.jumpMult * (1 + (player.agilityMult - 1) * 0.5);
  const agiSpeedMult = heroStats.speedMult * player.agilityMult;

  // Trigger Dash
  if (input.dashPressed && player.canDash && player.dashCooldown <= 0 && player.dashTimer <= 0) {
    const dirX = moveDir !== 0 ? moveDir : player.facing;
    player.dashDirX = dirX;
    player.dashDirY = 0;
    player.facing = dirX > 0 ? 1 : -1;
    player.vx = dirX * PHYSICS.DASH_SPEED * agiSpeedMult;
    player.vy = 0;
    player.dashTimer = PHYSICS.DASH_DURATION;
    player.dashCooldown = PHYSICS.DASH_COOLDOWN;
    player.canDash = false;
    player.onGround = false;
    player.standingPlatformId = null;
    outcome.dashed = true;
  }

  if (player.dashTimer > 0) {
    player.dashTimer = Math.max(0, player.dashTimer - dt);
    player.vx = player.dashDirX * PHYSICS.DASH_SPEED * agiSpeedMult;
    player.vy = 0;
  } else {
    const speedLimit =
      PHYSICS.MAX_RUN_SPEED *
      agiSpeedMult *
      (player.crouching ? PHYSICS.CROUCH_MOVE_MULT : 1);

    if (player.wallJumpLockTimer <= 0) {
      if (moveDir !== 0) {
        const accel = player.onGround ? PHYSICS.GROUND_ACCEL : PHYSICS.AIR_ACCEL;
        const targetVx = moveDir * speedLimit;
        if (player.vx < targetVx) {
          player.vx = Math.min(targetVx, player.vx + accel * dt);
        } else if (player.vx > targetVx) {
          player.vx = Math.max(targetVx, player.vx - accel * dt);
        }
      } else {
        const decel = player.onGround ? PHYSICS.GROUND_DECEL : PHYSICS.AIR_DECEL;
        if (player.vx > 0) {
          player.vx = Math.max(0, player.vx - decel * dt);
        } else if (player.vx < 0) {
          player.vx = Math.min(0, player.vx + decel * dt);
        }
      }
    }

    player.onWall = player.onGround ? 0 : detectWallTouch(level, player);

    // Jump / Coyote / Wall Jump / Double Jump
    if (player.jumpBufferTimer > 0) {
      if (player.onGround || player.coyoteTimer > 0) {
        player.vy = PHYSICS.JUMP_VEL * agiJumpMult;
        player.onGround = false;
        player.coyoteTimer = 0;
        player.jumpBufferTimer = 0;
        player.standingPlatformId = null;
        outcome.jumped = true;
      } else if (player.onWall !== 0) {
        const kickDir = (-player.onWall) as 1 | -1;
        player.vx = kickDir * PHYSICS.WALL_JUMP_VX;
        player.vy = PHYSICS.WALL_JUMP_VY * agiJumpMult;
        player.facing = kickDir;
        player.wallJumpLockTimer = PHYSICS.WALL_JUMP_LOCK;
        player.jumpBufferTimer = 0;
        outcome.wallJumped = true;
        outcome.wallDirection = player.onWall;
      } else if (player.airJumpsLeft > 0) {
        player.vy = PHYSICS.DOUBLE_JUMP_VEL * agiJumpMult;
        player.airJumpsLeft--;
        player.jumpBufferTimer = 0;
        outcome.doubleJumped = true;
      }
    }

    const gravMult = !input.jumpHeld && player.vy < 0 ? PHYSICS.SHORT_HOP_GRAVITY_MULT : 1;
    player.vy += PHYSICS.GRAVITY * gravMult * dt;

    const pressingTowardWall =
      (player.onWall === -1 && input.left) || (player.onWall === 1 && input.right);
    if (!player.onGround && player.onWall !== 0 && pressingTowardWall && player.vy > PHYSICS.WALL_SLIDE_SPEED) {
      player.vy = PHYSICS.WALL_SLIDE_SPEED;
    } else if (player.vy > PHYSICS.MAX_FALL_SPEED) {
      player.vy = PHYSICS.MAX_FALL_SPEED;
    }
  }

  // Move X & resolve solid tiles
  player.x += player.vx * dt;
  resolveHorizontalTiles(level, player);

  // Move Y & resolve solid + one-way + moving platforms
  const prevBottom = player.y + player.h;
  player.y += player.vy * dt;
  player.onGround = false;
  player.standingPlatformId = null;

  const minCol = Math.floor(player.x / TILE_SIZE);
  const maxCol = Math.floor((player.x + player.w - 0.01) / TILE_SIZE);
  const minRow = Math.floor(player.y / TILE_SIZE);
  const maxRow = Math.floor((player.y + player.h - 0.01) / TILE_SIZE);

  for (let r = minRow; r <= maxRow; r++) {
    for (let c = minCol; c <= maxCol; c++) {
      if (!isSolidTile(getTileAt(level, c, r))) continue;
      const tileRect: Rect = {
        x: c * TILE_SIZE,
        y: r * TILE_SIZE,
        w: TILE_SIZE,
        h: TILE_SIZE,
      };
      if (!rectsOverlap(player, tileRect)) continue;

      if (player.vy >= 0) {
        player.y = tileRect.y - player.h;
        player.vy = 0;
        player.onGround = true;
      } else if (player.vy < 0) {
        const overlapLeft = player.x + player.w - tileRect.x;
        const overlapRight = tileRect.x + tileRect.w - player.x;
        let nudged = false;
        if (overlapLeft > 0 && overlapLeft <= PHYSICS.CORNER_CORRECTION_PX) {
          const testRect: Rect = { x: tileRect.x - player.w, y: player.y, w: player.w, h: player.h };
          if (!overlapsAnySolid(level, testRect)) {
            player.x = testRect.x;
            nudged = true;
          }
        } else if (overlapRight > 0 && overlapRight <= PHYSICS.CORNER_CORRECTION_PX) {
          const testRect: Rect = { x: tileRect.x + tileRect.w, y: player.y, w: player.w, h: player.h };
          if (!overlapsAnySolid(level, testRect)) {
            player.x = testRect.x;
            nudged = true;
          }
        }
        if (!nudged) {
          player.y = tileRect.y + tileRect.h;
          player.vy = 0;
        }
      }
    }
  }

  if (player.vy >= 0 && player.dropThroughTimer <= 0) {
    const onewayMinCol = Math.floor(player.x / TILE_SIZE);
    const onewayMaxCol = Math.floor((player.x + player.w - 0.01) / TILE_SIZE);
    const footRow = Math.floor((player.y + player.h - 0.01) / TILE_SIZE);

    for (let c = onewayMinCol; c <= onewayMaxCol; c++) {
      if (getTileAt(level, c, footRow) === 'oneway') {
        const platTop = footRow * TILE_SIZE;
        if (prevBottom <= platTop + 4 && player.y + player.h >= platTop) {
          player.y = platTop - player.h;
          player.vy = 0;
          player.onGround = true;
          break;
        }
      }
    }
  }

  if (player.vy >= 0) {
    for (const plat of level.platforms) {
      const horizOverlap = player.x < plat.x + plat.w && player.x + player.w > plat.x;
      if (!horizOverlap) continue;
      if (prevBottom <= plat.prevY + 5 && player.y + player.h >= plat.y) {
        player.y = plat.y - player.h;
        player.vy = 0;
        player.onGround = true;
        player.standingPlatformId = plat.id;
        break;
      }
    }
  }

  player.onWall = player.onGround ? 0 : detectWallTouch(level, player);

  if (player.onGround) {
    player.coyoteTimer = PHYSICS.COYOTE_TIME;
    player.airJumpsLeft = player.maxAirJumps;
    player.canDash = true;
    if (!wasOnGround) {
      outcome.landed = true;
      player.landingTimer = 0.12;
    }
  }

  if (checkSpringCollision(level, player) && player.vy >= -40) {
    player.vy = PHYSICS.SPRING_VEL * heroStats.jumpMult;
    player.onGround = false;
    player.coyoteTimer = 0;
    player.airJumpsLeft = player.maxAirJumps;
    player.canDash = true;
    player.standingPlatformId = null;
    outcome.sprung = true;
  }

  const isActingOrMoving =
    moveDir !== 0 ||
    input.up ||
    input.down ||
    input.jumpHeld ||
    input.jumpPressed ||
    input.shootHeld ||
    input.shootPressed ||
    input.meleePressed ||
    input.grenadePressed ||
    input.dashPressed ||
    input.healPressed ||
    input.switchWeaponPressed ||
    Boolean(input.selectWeaponSlot) ||
    Boolean(input.reloadPressed) ||
    input.interactPressed ||
    player.crouching ||
    player.hurtTimer > 0 ||
    player.shootTimer > 0 ||
    player.grenadeTimer > 0 ||
    player.meleeTimer > 0 ||
    player.dashTimer > 0 ||
    player.reloadTimer > 0;

  if (player.onGround && Math.abs(player.vx) <= 2 && !isActingOrMoving) {
    player.idleTime += dt;
  } else {
    player.idleTime = 0;
  }

  return outcome;
}
