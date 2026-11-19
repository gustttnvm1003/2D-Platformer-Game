import { describe, expect, it } from 'vitest';
import {
  cloneDefaultBindings,
  formatActionKeys,
  rebindAction,
  resetKeyBindings,
} from '../src/game/controls';
import { LEVELS, parseLevel } from '../src/game/levels';
import { getTileAt, isSolidTile, isSpikeTile, overlapsAnySolid, rectsOverlap } from '../src/game/physics';
import { CHARACTER_FRAMES } from '../src/game/sprites';
import { purchaseShopOffer } from '../src/game/shop';
import {
  DIFFICULTY_CONFIG,
  DIFFICULTY_ORDER,
  FIXED_DT,
  HERO_CONFIG,
  InputState,
  TILE_SIZE,
} from '../src/game/types';
import { createWorld, stepWorld } from '../src/game/world';

function emptyInput(overrides: Partial<InputState> = {}): InputState {
  return {
    left: false,
    right: false,
    up: false,
    down: false,
    jumpHeld: false,
    jumpPressed: false,
    shootHeld: false,
    shootPressed: false,
    meleePressed: false,
    grenadePressed: false,
    dashPressed: false,
    healPressed: false,
    reloadPressed: false,
    switchWeaponPressed: false,
    selectWeaponSlot: 0,
    switchWeaponDir: 1,
    interactPressed: false,
    ...overrides,
  };
}

describe('RE4 Village Level Integrity', () => {
  it('validates dimensions, spawn clearance, medallions, barrels, and enemies across all 4 stages', () => {
    expect(LEVELS.length).toBe(4);

    for (const def of LEVELS) {
      const rowLength = def.grid[0].length;
      expect(rowLength).toBeGreaterThanOrEqual(80);
      for (const row of def.grid) {
        expect(row.length).toBe(rowLength);
      }

      const flat = def.grid.join('');
      expect((flat.match(/@/g) ?? []).length).toBe(1);
      expect((flat.match(/G/g) ?? []).length).toBe(1);
      expect((flat.match(/1/g) ?? []).length).toBe(1);
      expect((flat.match(/2/g) ?? []).length).toBe(1);
      expect((flat.match(/3/g) ?? []).length).toBe(1);

      const parsed = parseLevel(def);

      // Spawn clearance for 34px tall hero & solid floor underneath
      const spawnCol = Math.floor((parsed.spawn.x + 7) / TILE_SIZE);
      const footRow = Math.floor((parsed.spawn.y + 34) / TILE_SIZE);
      expect(isSolidTile(getTileAt(parsed, spawnCol, footRow))).toBe(true);
      expect(isSolidTile(getTileAt(parsed, spawnCol, footRow - 1))).toBe(false);
      expect(isSolidTile(getTileAt(parsed, spawnCol, footRow - 2))).toBe(false);
      expect(isSpikeTile(getTileAt(parsed, spawnCol, footRow - 1))).toBe(false);

      // Pickups & barrels not embedded inside solid walls
      for (const item of parsed.pickups) {
        const col = Math.floor((item.x + item.w / 2) / TILE_SIZE);
        const row = Math.floor((item.y + item.h / 2) / TILE_SIZE);
        expect(isSolidTile(getTileAt(parsed, col, row))).toBe(false);
      }
      for (const barrel of parsed.barrels) {
        expect(barrel.h).toBe(28);
        const col = Math.floor((barrel.x + barrel.w / 2) / TILE_SIZE);
        const row = Math.floor((barrel.y + barrel.h / 2) / TILE_SIZE);
        expect(isSolidTile(getTileAt(parsed, col, row))).toBe(false);
      }

      // Merchant stall present and placed on solid floor without wall overlap
      expect(parsed.merchants.length, `${def.id} merchant`).toBeGreaterThanOrEqual(1);
      for (const merchant of parsed.merchants) {
        expect(overlapsAnySolid(parsed, merchant), `${def.id} merchant overlaps solid`).toBe(false);
      }

      // Enemies within map bounds and clear of walls
      for (const enemy of parsed.enemies) {
        expect(enemy.x, `${def.id} ${enemy.id}`).toBeGreaterThanOrEqual(0);
        expect(enemy.x + enemy.w, `${def.id} ${enemy.id}`).toBeLessThanOrEqual(parsed.width);
        expect(overlapsAnySolid(parsed, enemy), `${def.id} ${enemy.id} overlaps solid`).toBe(false);
      }
    }
  });
});

describe('Heroes, Platforming & Crouch Physics', () => {
  it('applies Leon vs Ada stats, crouch, jump, double jump, coyote time, and dash', () => {
    const worldLeon = createWorld(LEVELS[0], 'leon');
    const worldAda = createWorld(LEVELS[0], 'ada');

    expect(worldLeon.player.maxHp).toBe(5);
    expect(worldLeon.player.grenades).toBe(2);
    expect(worldAda.player.maxHp).toBe(4);
    expect(worldAda.player.grenades).toBe(3);

    // Land on ground
    for (let i = 0; i < 6; i++) {
      stepWorld(worldLeon, emptyInput(), FIXED_DT);
    }
    expect(worldLeon.player.onGround).toBe(true);

    // Crouch reduces hitbox height from 34 to 22 and stands back up cleanly
    stepWorld(worldLeon, emptyInput({ down: true }), FIXED_DT);
    expect(worldLeon.player.crouching).toBe(true);
    expect(worldLeon.player.h).toBe(22);

    stepWorld(worldLeon, emptyInput({ down: false }), FIXED_DT);
    expect(worldLeon.player.crouching).toBe(false);
    expect(worldLeon.player.h).toBe(34);

    // Jump + Double Jump
    stepWorld(worldLeon, emptyInput({ jumpPressed: true, jumpHeld: true }), FIXED_DT);
    expect(worldLeon.player.vy).toBeLessThan(-300);
    stepWorld(worldLeon, emptyInput({ jumpPressed: true, jumpHeld: true }), FIXED_DT);
    expect(worldLeon.player.airJumpsLeft).toBe(0);

    // Dash
    stepWorld(worldLeon, emptyInput({ right: true, dashPressed: true }), FIXED_DT);
    expect(worldLeon.player.dashTimer).toBeGreaterThan(0);
    expect(worldLeon.player.canDash).toBe(false);
  });

  it('supports wall slide and wall jump in Stage 2 chimney', () => {
    const world = createWorld(LEVELS[1], 'ada');
    // Stage 2 chimney has right wall at col 28 (# at rows 6..16)
    world.player.x = 28 * TILE_SIZE - world.player.w;
    world.player.y = 10 * TILE_SIZE;
    world.player.onGround = false;
    world.player.vy = 140;

    stepWorld(world, emptyInput({ right: true }), FIXED_DT);
    expect(world.player.onWall).toBe(1);
    expect(world.player.vy).toBeLessThanOrEqual(57);

    stepWorld(world, emptyInput({ right: true, jumpPressed: true, jumpHeld: true }), FIXED_DT);
    expect(world.player.vx).toBeLessThan(-120);
    expect(world.player.vy).toBeLessThan(-250);
  });
});

describe('Weapons, Explosives, Medkits, Melee & Ganado Combat', () => {
  it('fires pistol with directional aiming, switches weapons, and falls back when out of ammo', () => {
    const world = createWorld(LEVELS[0], 'leon');
    stepWorld(world, emptyInput(), FIXED_DT);

    // Aim up & shoot
    stepWorld(world, emptyInput({ up: true, shootPressed: true, shootHeld: true }), FIXED_DT);
    expect(world.projectiles.length).toBe(1);
    expect(world.projectiles[0].vy).toBeLessThan(-300);
    expect(world.projectiles[0].vx).toBeCloseTo(0, 1);

    // Pick up Shotgun crate -> equips shotgun and shoots 6 spread pellets
    const shotgunCrate = world.level.pickups.find((p) => p.kind === 'shotgun')!;
    world.player.x = shotgunCrate.x;
    world.player.y = shotgunCrate.y;
    world.player.shootCooldown = 0;
    stepWorld(world, emptyInput(), FIXED_DT);
    expect(world.player.activeWeapon).toBe('shotgun');
    expect(world.player.mag.shotgun).toBe(8);
    expect(world.player.reserve.shotgun).toBe(4);
    expect(world.player.ammo.shotgun).toBe(12);

    world.projectiles = [];
    stepWorld(world, emptyInput({ shootPressed: true, shootHeld: true }), FIXED_DT);
    expect(world.projectiles.length).toBeGreaterThanOrEqual(5);
    expect(world.projectiles.length).toBeLessThanOrEqual(7);
    expect(world.particles.some((pt) => pt.sprite === 'shell_shotgun')).toBe(true);
    expect(world.player.mag.shotgun).toBe(7);
    expect(world.player.ammo.shotgun).toBe(11);

    // Switch back to handgun with Q
    stepWorld(world, emptyInput({ switchWeaponPressed: true }), FIXED_DT);
    expect(world.player.activeWeapon).toBe('pistol');
  });

  it('throws Frag Grenade, detonates Red Barrel, and defeats Ganados with dying animation', () => {
    const world = createWorld(LEVELS[0], 'leon');
    const barrel = world.level.barrels[0];
    const forkEnemy = world.level.enemies[0];

    // Move Ganado next to barrel to test explosion blast
    forkEnemy.x = barrel.x + 18;
    forkEnemy.y = barrel.y + barrel.h - forkEnemy.h;

    // Shoot barrel until it explodes
    world.player.x = barrel.x - 45;
    world.player.y = barrel.y + barrel.h - world.player.h;
    world.player.facing = 1;
    world.player.activeWeapon = 'rpg';
    world.player.ammo.rpg = 2;

    stepWorld(world, emptyInput({ shootPressed: true, shootHeld: true }), FIXED_DT);
    for (let i = 0; i < 15; i++) {
      stepWorld(world, emptyInput(), FIXED_DT);
    }

    expect(barrel.exploded).toBe(true);
    expect(forkEnemy.state === 'dying' || forkEnemy.state === 'dead').toBe(true);
    expect(world.enemiesDefeated).toBeGreaterThanOrEqual(1);

    // Throw Frag Grenade (hides gun & enters pure hand-throw motion without triggering shootTimer)
    const startGrenades = world.player.grenades;
    stepWorld(world, emptyInput({ grenadePressed: true }), FIXED_DT);
    expect(world.player.grenades).toBe(startGrenades - 1);
    expect(world.player.grenadeTimer).toBeGreaterThan(0);
    expect(world.player.shootTimer).toBe(0);
    expect(world.projectiles.some((p) => p.kind === 'grenade')).toBe(true);
  });

  it('uses Medkit to restore HP and parries thrown Ganado axes with Melee', () => {
    const world = createWorld(LEVELS[0], 'leon');
    world.player.hp = 2;
    expect(world.player.medkits).toBe(1);

    stepWorld(world, emptyInput({ healPressed: true }), FIXED_DT);
    expect(world.player.hp).toBe(4);
    expect(world.player.medkits).toBe(0);

    // Spawn incoming enemy axe right in front of Leon and parry with melee
    world.projectiles.push({
      id: 'test-axe',
      kind: 'enemy_axe',
      fromPlayer: false,
      x: world.player.x + world.player.w + 8,
      y: world.player.y + 10,
      vx: -100,
      vy: 0,
      w: 11,
      h: 11,
      damage: 1,
      life: 2,
      bouncesLeft: 0,
      rotation: 0,
    });
    const prevScore = world.score;
    stepWorld(world, emptyInput({ meleePressed: true }), FIXED_DT);
    expect(world.projectiles.some((p) => p.id === 'test-axe')).toBe(false);
    expect(world.score).toBe(prevScore + 50);
  });

  it('unlocks Stage 4 Church Bell after defeating Dr. Salvador Boss', () => {
    const world = createWorld(LEVELS[3], 'ada', {
      pesetas: 0,
      upgrades: { maxHpLevel: 0, damageLevel: 0, agilityLevel: 0, pouchLevel: 0 },
      difficulty: 'normal',
    });
    expect(world.goalUnlocked).toBe(false);

    const boss = world.level.enemies.find((e) => e.kind === 'boss_salvador')!;
    expect(boss).toBeDefined();
    expect(boss.hp).toBe(24);

    // Hit Dr. Salvador with RPG rockets until defeated
    world.player.x = boss.x - 60;
    world.player.y = boss.y + boss.h - world.player.h;
    world.player.facing = 1;
    world.player.activeWeapon = 'rpg';
    world.player.ammo.rpg = 10;

    for (let shot = 0; shot < 4; shot++) {
      world.hitstopTimer = 0;
      world.player.shootCooldown = 0;
      stepWorld(world, emptyInput({ shootPressed: true, shootHeld: true }), FIXED_DT);
      for (let f = 0; f < 25; f++) {
        stepWorld(world, emptyInput(), FIXED_DT);
      }
    }

    expect(boss.state === 'dying' || boss.state === 'dead').toBe(true);
    expect(world.goalUnlocked).toBe(true);
    expect(world.pendingSounds).toContain('enemy_die_boss');

    // Touch Church Bell Goal to clear stage
    world.player.x = world.level.goal.x;
    world.player.y = world.level.goal.y;
    stepWorld(world, emptyInput(), FIXED_DT);
    expect(world.status).toBe('victory');
    expect(rectsOverlap(world.player, world.level.goal)).toBe(true);
  });

  it('shoots taller red barrels while standing without crouching and emits distinct audio events', () => {
    const worldLeon = createWorld(LEVELS[0], 'leon', { pesetas: 0 });
    const worldAda = createWorld(LEVELS[0], 'ada', { pesetas: 0 });

    // Distinct male vs female jump sounds
    stepWorld(worldLeon, emptyInput(), FIXED_DT);
    stepWorld(worldLeon, emptyInput({ jumpPressed: true, jumpHeld: true }), FIXED_DT);
    expect(worldLeon.pendingSounds).toContain('jump_male');

    stepWorld(worldAda, emptyInput(), FIXED_DT);
    stepWorld(worldAda, emptyInput({ jumpPressed: true, jumpHeld: true }), FIXED_DT);
    expect(worldAda.pendingSounds).toContain('jump_female');

    // Standing pistol shots hit tall barrel (h=28) directly without crouching
    const barrel = worldLeon.level.barrels[0];
    worldLeon.player.x = barrel.x - 45;
    worldLeon.player.y = barrel.y + barrel.h - worldLeon.player.standingH;
    worldLeon.player.crouching = false;
    worldLeon.player.facing = 1;
    worldLeon.player.vy = 0;
    worldLeon.player.onGround = true;
    worldLeon.pendingSounds = [];

    for (let s = 0; s < 2; s++) {
      worldLeon.player.shootCooldown = 0;
      stepWorld(worldLeon, emptyInput({ shootPressed: true, shootHeld: true }), FIXED_DT);
      for (let f = 0; f < 10; f++) {
        stepWorld(worldLeon, emptyInput(), FIXED_DT);
      }
    }

    expect(barrel.exploded).toBe(true);
    expect(worldLeon.pendingSounds).toContain('shoot_pistol');
    expect(worldLeon.pendingSounds).toContain('barrel_explosion');
  });

  it('supports Merchant stall interaction, invulnerability, and buying weapons & upgrades with Pesetas', () => {
    const world = createWorld(LEVELS[0], 'leon', { pesetas: 500 });
    const merchant = world.level.merchants[0];
    expect(merchant).toBeDefined();

    // Stand near Merchant and shoot/melee — Merchant takes zero harm and opens shop on interact
    world.player.x = merchant.x - 10;
    world.player.y = merchant.y + merchant.h - world.player.standingH;
    world.player.facing = 1;
    stepWorld(world, emptyInput({ shootPressed: true, shootHeld: true, meleePressed: true }), FIXED_DT);
    expect(world.nearMerchantId).toBe(merchant.id);
    expect(world.level.merchants.length).toBe(1);

    stepWorld(world, emptyInput({ interactPressed: true }), FIXED_DT);
    expect(world.shopOpenRequested).toBe(true);
    expect(world.pendingSounds).toContain('merchant_greet');

    // Buy Shotgun + Armor Upgrade + Damage Upgrade
    const buyShotgun = purchaseShopOffer(world, 'shotgun');
    expect(buyShotgun.ok).toBe(true);
    expect(world.player.activeWeapon).toBe('shotgun');
    expect(world.player.ammo.shotgun).toBe(12);

    const buyArmor = purchaseShopOffer(world, 'up_hp');
    expect(buyArmor.ok).toBe(true);
    expect(world.player.maxHp).toBe(6);
    expect(world.upgrades.maxHpLevel).toBe(1);
    expect(world.pesetas).toBe(500 - 180 - 200);
    expect(world.pendingSounds).toContain('merchant_buy');
  });

  it('supports manual key rebinding, conflict swapping, and resetting controls to default', () => {
    let bindings = cloneDefaultBindings();
    expect(formatActionKeys(bindings.jump)).toBe('Space / K');

    // Rebind jump to KeyW (which was bound to up); up keeps ArrowUp without conflict
    bindings = rebindAction(bindings, 'jump', 'KeyW');
    expect(bindings.jump).toEqual(['KeyW']);
    expect(bindings.up).toEqual(['ArrowUp']);

    // Rebind shoot to KeyW -> swaps jump cleanly back to KeyW's replaced primary or previous key
    bindings = rebindAction(bindings, 'shoot', 'KeyW');
    expect(bindings.shoot).toEqual(['KeyW']);
    expect(bindings.jump).toEqual(['KeyJ']);

    const reset = resetKeyBindings();
    expect(reset.jump).toEqual(['Space', 'KeyK']);
    expect(reset.shoot).toEqual(['KeyJ', 'KeyZ']);
  });

  it('scales enemy count, enemy HP, and Dr. Salvador Boss HP across Easy to Professional', () => {
    const bossHpByDiff: Record<string, number> = {};

    for (const def of LEVELS) {
      let prevCount = 0;
      let prevTotalHp = 0;

      for (const diff of DIFFICULTY_ORDER) {
        const parsed = parseLevel(def, diff);
        const count = parsed.enemies.length;
        const totalHp = parsed.enemies.reduce((acc, e) => acc + e.maxHp, 0);

        expect(count, `${def.id} ${diff} count`).toBeGreaterThan(prevCount);
        expect(totalHp, `${def.id} ${diff} total HP`).toBeGreaterThan(prevTotalHp);
        prevCount = count;
        prevTotalHp = totalHp;

        for (const enemy of parsed.enemies) {
          expect(enemy.x, `${def.id} ${diff} ${enemy.id} x`).toBeGreaterThanOrEqual(0);
          expect(enemy.x + enemy.w, `${def.id} ${diff} ${enemy.id} maxX`).toBeLessThanOrEqual(parsed.width);
          expect(overlapsAnySolid(parsed, enemy), `${def.id} ${diff} ${enemy.id} overlaps solid`).toBe(false);

          const footRow = Math.floor((enemy.y + enemy.h + 2) / TILE_SIZE);
          const leftCol = Math.floor((enemy.x + 2) / TILE_SIZE);
          const rightCol = Math.floor((enemy.x + enemy.w - 2) / TILE_SIZE);
          const hasImmediateFloor =
            getTileAt(parsed, leftCol, footRow) === 'solid' ||
            getTileAt(parsed, leftCol, footRow) === 'oneway' ||
            getTileAt(parsed, rightCol, footRow) === 'solid' ||
            getTileAt(parsed, rightCol, footRow) === 'oneway';

          if (enemy.id.includes('-hc-') || enemy.id.includes('-pro-')) {
            expect(hasImmediateFloor, `${def.id} ${diff} ${enemy.id} lacks immediate floor`).toBe(true);
          } else {
            let hasFloorBelow = hasImmediateFloor;
            for (let r = footRow; r < parsed.rows && !hasFloorBelow; r++) {
              const tL = getTileAt(parsed, leftCol, r);
              const tR = getTileAt(parsed, rightCol, r);
              if (tL === 'solid' || tL === 'oneway' || tR === 'solid' || tR === 'oneway') {
                hasFloorBelow = true;
              }
            }
            expect(hasFloorBelow, `${def.id} ${diff} ${enemy.id} over pit`).toBe(true);
          }

          if (enemy.kind === 'boss_salvador') {
            bossHpByDiff[diff] = enemy.maxHp;
            expect(enemy.maxHp).toBe(DIFFICULTY_CONFIG[diff].bossHp);
          }
        }
      }
    }

    expect(bossHpByDiff).toEqual({
      easy: 16,
      normal: 24,
      hardcore: 36,
      professional: 48,
    });
  });

  it('supports Shotgun knockback/shells, CQBR 30-mag full-auto/reload, RPG-7 backblast/trail, hotkeys 1-4, Merchant ammo packs, and enemy loot drops', () => {
    const world = createWorld(LEVELS[0], 'leon', {
      pesetas: 1500,
      upgrades: { maxHpLevel: 0, damageLevel: 0, agilityLevel: 0, pouchLevel: 0 },
      difficulty: 'normal',
    });

    // Buy Shotgun, CQBR AR, RPG-7, plus ammo packages from Merchant
    expect(purchaseShopOffer(world, 'shotgun').ok).toBe(true);
    expect(purchaseShopOffer(world, 'ammo_shotgun').ok).toBe(true);
    expect(world.player.mag.shotgun).toBe(8);
    expect(world.player.reserve.shotgun).toBe(12);

    expect(purchaseShopOffer(world, 'mg').ok).toBe(true);
    expect(purchaseShopOffer(world, 'ammo_mg').ok).toBe(true);
    expect(world.player.mag.mg).toBe(30);
    expect(world.player.reserve.mg).toBe(60);

    expect(purchaseShopOffer(world, 'rpg').ok).toBe(true);
    expect(purchaseShopOffer(world, 'ammo_rpg').ok).toBe(true);
    expect(world.player.mag.rpg).toBe(1);
    expect(world.player.reserve.rpg).toBe(6);

    // Hotkey switching 1..4
    stepWorld(world, emptyInput({ selectWeaponSlot: 3 }), FIXED_DT);
    expect(world.player.activeWeapon).toBe('mg');

    // CQBR full-auto rapid-fire (aim up) with brass casing ejection & manual reload
    world.particles = [];
    for (let f = 0; f < 20; f++) {
      stepWorld(world, emptyInput({ up: true, shootHeld: true }), FIXED_DT);
    }
    expect(world.player.mag.mg).toBeLessThan(30);
    expect(world.particles.some((pt) => pt.sprite === 'shell_cqbr')).toBe(true);

    const spentMg = 30 - world.player.mag.mg;
    stepWorld(world, emptyInput({ reloadPressed: true }), FIXED_DT);
    expect(world.player.reloadTimer).toBeGreaterThan(0);
    for (let f = 0; f < 60; f++) {
      stepWorld(world, emptyInput(), FIXED_DT);
    }
    expect(world.player.mag.mg).toBe(30);
    expect(world.player.reserve.mg).toBe(60 - spentMg);

    // Switch to Shotgun via hotkey [2], blast enemy with high knockback, verify loot drop when killed
    stepWorld(world, emptyInput({ selectWeaponSlot: 2 }), FIXED_DT);
    expect(world.player.activeWeapon).toBe('shotgun');

    const enemy = world.level.enemies[0];
    enemy.hp = 8;
    world.player.x = enemy.x - 28;
    world.player.y = enemy.y + enemy.h - world.player.h;
    world.player.facing = 1;
    world.player.shootCooldown = 0;
    const prevPickupsCount = world.level.pickups.length;
    stepWorld(world, emptyInput({ shootPressed: true, shootHeld: true }), FIXED_DT);
    stepWorld(world, emptyInput(), FIXED_DT);
    expect(Math.abs(enemy.vx)).toBeGreaterThan(100);

    // Finish off Ganados so enemy loot drop spawns ammo pickup (owns Shotgun & CQBR)
    enemy.hp = 1;
    world.player.shootCooldown = 0;
    stepWorld(world, emptyInput({ shootPressed: true, shootHeld: true }), FIXED_DT);
    for (let f = 0; f < 6; f++) {
      stepWorld(world, emptyInput(), FIXED_DT);
    }
    expect(world.level.pickups.length).toBeGreaterThan(prevPickupsCount);
    const dropped = world.level.pickups[world.level.pickups.length - 1];
    expect(dropped.kind === 'ammo_shotgun' || dropped.kind === 'ammo_mg').toBe(true);

    // Switch to RPG-7 [4], fire rocket -> spawns backblast particles + rocket smoke trail
    stepWorld(world, emptyInput({ selectWeaponSlot: 4 }), FIXED_DT);
    expect(world.player.activeWeapon).toBe('rpg');
    world.player.shootCooldown = 0;
    world.player.reloadTimer = 0;
    world.particles = [];
    stepWorld(world, emptyInput({ shootPressed: true, shootHeld: true }), FIXED_DT);
    expect(world.projectiles.some((pr) => pr.kind === 'rpg')).toBe(true);
    expect(world.particles.length).toBeGreaterThanOrEqual(8);
  });

  it('tracks AFK idle animation timer, signature kicks, and barrel-aligned projectile spawns', () => {
    expect(HERO_CONFIG.leon.meleeName).toBe('Roundhouse Kick');
    expect(HERO_CONFIG.ada.meleeName).toBe('Corkscrew Kick');
    expect(CHARACTER_FRAMES.leon.afkIdle.length).toBe(6);
    expect(CHARACTER_FRAMES.ada.afkIdle.length).toBe(6);
    expect(CHARACTER_FRAMES.leon.melee).toEqual([37, 38, 39, 40, 41]);
    expect(CHARACTER_FRAMES.leon.walk_run_cqbr).toEqual([60, 61, 62, 63, 64, 65, 66, 67]);
    expect(CHARACTER_FRAMES.leon.walk_run_shotgun).toEqual([68, 69, 70, 71, 72, 73, 74, 75]);
    expect(CHARACTER_FRAMES.leon.leon_cqbr_jump).toEqual([77, 78]);
    expect(CHARACTER_FRAMES.leon.leon_cqbr_drop).toEqual([79, 80, 81]);
    expect(CHARACTER_FRAMES.leon.leon_shotgun_jump).toEqual([84, 85]);
    expect(CHARACTER_FRAMES.leon.leon_shotgun_drop).toEqual([86, 87, 88]);
    expect(CHARACTER_FRAMES.leon.leon_rpg_jump).toEqual([91, 92]);
    expect(CHARACTER_FRAMES.leon.leon_rpg_drop).toEqual([93, 94, 95]);
    expect(CHARACTER_FRAMES.leon.weapons.mg.run).toEqual(CHARACTER_FRAMES.leon.walk_run_cqbr);
    expect(CHARACTER_FRAMES.leon.weapons.shotgun.run).toEqual(CHARACTER_FRAMES.leon.walk_run_shotgun);
    // Preserve all original Ada slices alongside newly sliced locomotion clips
    expect(CHARACTER_FRAMES.ada.idle).toEqual([0, 1, 2, 3]);
    expect(CHARACTER_FRAMES.ada.walk).toEqual([4, 5, 6, 7]);
    expect(CHARACTER_FRAMES.ada.run).toEqual([8, 9, 10, 11]);
    expect(CHARACTER_FRAMES.ada.crouch).toEqual([12]);
    expect(CHARACTER_FRAMES.ada.melee).toEqual([29, 30, 31, 32]);
    expect(CHARACTER_FRAMES.ada.dash).toEqual([20]);
    expect(CHARACTER_FRAMES.ada.weapons.rpg.idle).toEqual([22]);
    expect(CHARACTER_FRAMES.ada.ada_jump).toEqual([67]);
    expect(CHARACTER_FRAMES.ada.ada_drop_platform).toEqual([68, 69]);
    expect(CHARACTER_FRAMES.ada.ada_shotgun_walk).toEqual([60, 64]);
    expect(CHARACTER_FRAMES.ada.ada_shotgun_run).toEqual([61, 62, 63]);
    expect(CHARACTER_FRAMES.ada.ada_shotgun_jump).toEqual([65]);
    expect(CHARACTER_FRAMES.ada.ada_shotgun_drop).toEqual([66, 66]);
    expect(CHARACTER_FRAMES.ada.ada_cqbr_walk).toEqual([70, 71, 72]);
    expect(CHARACTER_FRAMES.ada.ada_cqbr_run).toEqual([73, 74, 75]);
    expect(CHARACTER_FRAMES.ada.ada_cqbr_jump).toEqual([77, 78, 79]);
    expect(CHARACTER_FRAMES.ada.ada_cqbr_drop).toEqual([80, 81]);
    expect(CHARACTER_FRAMES.ada.weapons.shotgun.run).toEqual([61, 62, 63]);
    expect(CHARACTER_FRAMES.ada.weapons.mg.run).toEqual([73, 74, 75]);

    // Verify Ada one-way platform drop-through & landing transition state
    const adaWorld = createWorld(LEVELS[0], 'ada', { pesetas: 0 });
    let oneWayCol = -1;
    let oneWayRow = -1;
    for (let r = 0; r < adaWorld.level.rows - 2; r++) {
      for (let c = 0; c < adaWorld.level.cols; c++) {
        if (adaWorld.level.tiles[r][c] === 'oneway' && adaWorld.level.tiles[r - 1][c] === 'empty') {
          oneWayCol = c;
          oneWayRow = r;
          break;
        }
      }
      if (oneWayCol >= 0) break;
    }
    expect(oneWayCol).toBeGreaterThanOrEqual(0);
    adaWorld.player.x = oneWayCol * TILE_SIZE + 1;
    adaWorld.player.y = oneWayRow * TILE_SIZE - adaWorld.player.h;
    adaWorld.player.onGround = true;
    adaWorld.player.vy = 0;
    stepWorld(adaWorld, emptyInput({ down: true, jumpPressed: true, jumpHeld: true }), FIXED_DT);
    expect(adaWorld.player.dropThroughTimer).toBeGreaterThan(0);
    expect(adaWorld.player.onGround).toBe(false);
    for (let i = 0; i < 60 && !adaWorld.player.onGround; i++) {
      stepWorld(adaWorld, emptyInput(), FIXED_DT);
    }
    expect(adaWorld.player.onGround).toBe(true);
    expect(adaWorld.player.landingTimer).toBeGreaterThan(0);

    const world = createWorld(LEVELS[0], 'leon', { pesetas: 0 });
    // Let player settle onto ground first
    for (let i = 0; i < 15; i++) {
      stepWorld(world, emptyInput(), FIXED_DT);
    }
    expect(world.player.onGround).toBe(true);

    // Standing idle for ~3 seconds builds AFK idle timer
    for (let i = 0; i < 185; i++) {
      stepWorld(world, emptyInput(), FIXED_DT);
    }
    expect(world.player.idleTime).toBeGreaterThan(2.8);

    // Firing resets idleTime and spawns bullet at Leon's handgun barrel (~27.5px above feet)
    const footY = world.player.y + world.player.h;
    const centerX = world.player.x + world.player.w * 0.5;
    stepWorld(world, emptyInput({ shootPressed: true, shootHeld: true }), FIXED_DT);
    expect(world.player.idleTime).toBe(0);

    const bullet = world.projectiles.find((pr) => pr.kind === 'pistol');
    expect(bullet).toBeDefined();
    if (bullet) {
      // Bullet X spawns forward of barrel rather than from body center
      expect(bullet.x).toBeGreaterThan(centerX + 14);
      // Bullet Y center sits near barrel height (~27.5 px above feet)
      const bulletCenterAboveFeet = footY - (bullet.y + bullet.h * 0.5);
      expect(bulletCenterAboveFeet).toBeGreaterThanOrEqual(25);
      expect(bulletCenterAboveFeet).toBeLessThanOrEqual(30);
    }

    // Signature Roundhouse Kick duration matches multi-frame animation
    stepWorld(world, emptyInput({ meleePressed: true }), FIXED_DT);
    expect(world.player.meleeTimer).toBeGreaterThan(0.24);
  });
});
