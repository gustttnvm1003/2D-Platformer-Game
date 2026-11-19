import {
  Checkpoint,
  DIFFICULTY_CONFIG,
  DifficultyId,
  Enemy,
  ExplosiveBarrel,
  LevelDefinition,
  MerchantStall,
  MovingPlatform,
  ParsedLevel,
  PickupItem,
  TILE_SIZE,
  TileKind,
} from './types';

function charToTile(ch: string): TileKind {
  switch (ch) {
    case '#':
      return 'solid';
    case '-':
      return 'oneway';
    case '^':
      return 'spike_up';
    case 'v':
      return 'spike_down';
    case '<':
      return 'spike_left';
    case '>':
      return 'spike_right';
    case 'J':
      return 'spring';
    default:
      return 'empty';
  }
}

export const LEVELS: LevelDefinition[] = [
  {
    index: 0,
    id: 'village-outskirts',
    name: 'El Pueblo: Outskirts',
    subtitle: 'Stage 1 • Gerbang Desa',
    biome: 'village',
    description: 'Masuki pinggiran desa Spanyol, tembak Ganado, ledakkan barel merah, dan rebut Shotgun!',
    // Ruler:  0         1         2         3         4         5         6         7         8         9         0         1
    //         0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901
    grid: [
      '################################################################################################################',
      '#..............................................................................................................#',
      '#..............................................................................................................#',
      '#.............................................1.......................................................3........#',
      '#............................................###.....................................................###.......#',
      '#..............................................................................................................#',
      '#.......................................M........................................................C.C...........#',
      '#......................................-----..........C..C.........................B............-----..........#',
      '#....................................................######.......................###..........................#',
      '#..............................................................................................................#',
      '#....................C..C.........S............................................................................#',
      '#...................------.......###.......................H...........-----.............M.....................#',
      '#.........................................................###...........................###....................#',
      '#...........B........................................................................................J.........#',
      '#..........###.....................C....C..................................C....C...................###........#',
      '#.........................---.....--------................................--------.............................#',
      '#..............................................................................................................#',
      '#..............................................................................................................#',
      '#..@....C.C.....E........................E........P..W.C..C........E..................E.........P.............G#',
      '#######..###############..#####..^^..###########..############.........###################..^^..################',
      '#######..###############..#####..##..###########..############....2....###################..##..################',
      '##############################################################..#####..#########################################',
    ],
    platforms: [
      {
        id: 'v-plat-1',
        startCol: 62,
        startRow: 19,
        endCol: 68,
        endRow: 19,
        widthTiles: 3,
        speed: 48,
        pauseDuration: 0.45,
      },
    ],
    enemies: [
      { id: 'v-fork-1', kind: 'ganado_fork', col: 18, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3 },
      { id: 'v-fork-2', kind: 'ganado_fork', col: 28, row: 18, patrolLeftTiles: 2, patrolRightTiles: 2, easySkip: true },
      { id: 'v-axe-1', kind: 'ganado_axe', col: 42, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3 },
      { id: 'v-fork-3', kind: 'ganado_fork', col: 37, row: 14, patrolLeftTiles: 2, patrolRightTiles: 3 },
      { id: 'v-axe-2', kind: 'ganado_axe', col: 78, row: 18, patrolLeftTiles: 4, patrolRightTiles: 4 },
      { id: 'v-fork-4', kind: 'ganado_fork', col: 87, row: 18, patrolLeftTiles: 4, patrolRightTiles: 3 },
      { id: 'v-hc-fork-1', kind: 'ganado_fork', col: 12, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3, minDifficulty: 'hardcore' },
      { id: 'v-hc-axe-1', kind: 'ganado_axe', col: 100, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3, minDifficulty: 'hardcore' },
      { id: 'v-hc-fork-2', kind: 'ganado_fork', col: 55, row: 7, patrolLeftTiles: 1, patrolRightTiles: 2, minDifficulty: 'hardcore' },
      { id: 'v-pro-axe-1', kind: 'ganado_axe', col: 39, row: 18, patrolLeftTiles: 1, patrolRightTiles: 3, minDifficulty: 'professional' },
      { id: 'v-pro-fork-1', kind: 'ganado_fork', col: 73, row: 18, patrolLeftTiles: 1, patrolRightTiles: 3, minDifficulty: 'professional' },
      { id: 'v-pro-axe-2', kind: 'ganado_axe', col: 105, row: 18, patrolLeftTiles: 2, patrolRightTiles: 2, minDifficulty: 'professional' },
    ],
    signs: [
      { x: 4 * TILE_SIZE, y: 18 * TILE_SIZE, text: 'J/Z Tembak • X/L Knife/Kick • C/G Granat • E/H Medkit' },
      { x: 14 * TILE_SIZE, y: 18 * TILE_SIZE, text: 'Tembak Barel Merah untuk ledakan area!' },
      { x: 47 * TILE_SIZE, y: 18 * TILE_SIZE, text: 'Pegang W/↑ untuk tembak ke atas! Q ganti senjata' },
    ],
  },
  {
    index: 1,
    id: 'plaza-bonfire',
    name: 'La Plaza: Village Square',
    subtitle: 'Stage 2 • Alun-Alun Api Unggun',
    biome: 'plaza',
    description: 'Pertempuran seru di alun-alun desa! Hadapi lemparan kapak Ganado dan panjat menara kayu.',
    // Ruler:  0         1         2         3         4         5         6         7         8         9         0         1
    //         01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345
    grid: [
      '####################################################################################################################',
      '#v...v............................................v...v..................................................v...v.....#',
      '#..................................................................................................................#',
      '#.............................1.......................................................................3............#',
      '#............................###.....................................................................###...........#',
      '#........................................C..C......................................................................#',
      '#......................#....#...........------...............H...................C..C..............................#',
      '#......................#....#...............................###.................------........B....................#',
      '#...............B......#....#................................................................###...................#',
      '#..............###.....#....#....S.................................................................................#',
      '#......................#....#...###.................M......................................A.......................#',
      '#.......C..C...........#....#......................---...............P....................###.........J............#',
      '#......------..........#....#.......................................###..............................###...........#',
      '#......................#....#.........C..C.........................................................................#',
      '#...........J..........#....#........------.....................E..................................................#',
      '#..........###.........#....#.................................#####..............-----.............................#',
      '#......................#....#......................................................................................#',
      '#......................#...........................................................................................#',
      '#@....C.C...E.................................E...........C.....W.......E...............E.........P...............G#',
      '######..###########..#####..#####..^^..###########..##########.....############..^^..###########..##################',
      '######..###########..#####..#####..##..###########..##########..2..############..##..###########..##################',
      '##############################################################.###.#################################################',
    ],
    platforms: [
      {
        id: 'p-plat-1',
        startCol: 62,
        startRow: 19,
        endCol: 66,
        endRow: 15,
        widthTiles: 3,
        speed: 52,
        pauseDuration: 0.4,
      },
    ],
    enemies: [
      { id: 'p-fork-1', kind: 'ganado_fork', col: 14, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3 },
      { id: 'p-axe-1', kind: 'ganado_axe', col: 32, row: 18, patrolLeftTiles: 2, patrolRightTiles: 2, easySkip: true },
      { id: 'p-fork-2', kind: 'ganado_fork', col: 44, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3 },
      { id: 'p-axe-2', kind: 'ganado_axe', col: 56, row: 18, patrolLeftTiles: 3, patrolRightTiles: 2 },
      { id: 'p-fork-3', kind: 'ganado_fork', col: 74, row: 18, patrolLeftTiles: 2, patrolRightTiles: 2 },
      { id: 'p-axe-3', kind: 'ganado_axe', col: 88, row: 18, patrolLeftTiles: 3, patrolRightTiles: 4 },
      { id: 'p-fork-4', kind: 'ganado_fork', col: 92, row: 14, patrolLeftTiles: 1, patrolRightTiles: 2, easySkip: true },
      { id: 'p-hc-fork-1', kind: 'ganado_fork', col: 10, row: 18, patrolLeftTiles: 1, patrolRightTiles: 3, minDifficulty: 'hardcore' },
      { id: 'p-hc-axe-1', kind: 'ganado_axe', col: 68, row: 18, patrolLeftTiles: 1, patrolRightTiles: 3, minDifficulty: 'hardcore' },
      { id: 'p-hc-fork-2', kind: 'ganado_fork', col: 100, row: 18, patrolLeftTiles: 2, patrolRightTiles: 3, minDifficulty: 'hardcore' },
      { id: 'p-pro-axe-1', kind: 'ganado_axe', col: 42, row: 5, patrolLeftTiles: 1, patrolRightTiles: 2, minDifficulty: 'professional' },
      { id: 'p-pro-fork-1', kind: 'ganado_fork', col: 85, row: 18, patrolLeftTiles: 1, patrolRightTiles: 3, minDifficulty: 'professional' },
      { id: 'p-pro-axe-2', kind: 'ganado_axe', col: 107, row: 18, patrolLeftTiles: 2, patrolRightTiles: 2, minDifficulty: 'professional' },
    ],
    signs: [
      { x: 20 * TILE_SIZE, y: 18 * TILE_SIZE, text: 'Wall-Jump di cerobong batu atau pakai Spring!' },
      { x: 35 * TILE_SIZE, y: 18 * TILE_SIZE, text: 'Tekan S/↓ untuk runduk menghindari kapak!' },
    ],
  },
  {
    index: 2,
    id: 'watchtower-siege',
    name: 'Torre del Pueblo',
    subtitle: 'Stage 3 • Menara Pengawas',
    biome: 'tower',
    description: 'Lompati tebing menara, rebut Rocket Launcher [R], dan bertahan dari serbuan Ganado!',
    // Ruler:  0         1         2         3         4         5         6         7         8         9         0         1
    //         012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
    grid: [
      '........................................................................................................................',
      '........................................................................................................................',
      '.......................................................2................................................................',
      '......................................................###...............................................................',
      '.......................................................................................................3................',
      '.............................................R........................................................###...............',
      '............................................###.........................................................................',
      '...............................C..C........................................C..C......................-----..............',
      '..............................######.................M....................######........................................',
      '....................1...............................###.....................................B.......................G...',
      '...................###..........................................P..........................###.....................###..',
      '...............................................................###................................................#####.',
      '............H......................-----...........................................J..............................#####.',
      '...........###............................................E.......................###.....................M.......#####.',
      '........B.............J..................................###.............................................###......#####.',
      '.......###...........###...................C..C..........................C..C.....................................#####.',
      '..........................................------........................------....................................#####.',
      '..................................................................................................................#####.',
      '..@....C.C......E...................................P..........W...E..........................E...................#####.',
      '#####..###############......#####......###########.###......############......#####..^^..###########......##############',
      '#####..###############......#####......###########.###......############......#####..##..###########......##############',
      '........................................................................................................................',
    ],
    platforms: [
      {
        id: 't-plat-1',
        startCol: 22,
        startRow: 19,
        endCol: 27,
        endRow: 19,
        widthTiles: 3,
        speed: 55,
        pauseDuration: 0.4,
      },
      {
        id: 't-plat-2',
        startCol: 33,
        startRow: 19,
        endCol: 38,
        endRow: 15,
        widthTiles: 3,
        speed: 55,
        pauseDuration: 0.4,
      },
      {
        id: 't-plat-3',
        startCol: 72,
        startRow: 19,
        endCol: 77,
        endRow: 19,
        widthTiles: 3,
        speed: 62,
        pauseDuration: 0.35,
      },
      {
        id: 't-plat-4',
        startCol: 99,
        startRow: 18,
        endCol: 105,
        endRow: 13,
        widthTiles: 3,
        speed: 58,
        pauseDuration: 0.4,
      },
    ],
    enemies: [
      { id: 't-fork-1', kind: 'ganado_fork', col: 13, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3 },
      { id: 't-axe-1', kind: 'ganado_axe', col: 44, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3 },
      { id: 't-fork-2', kind: 'ganado_fork', col: 69, row: 18, patrolLeftTiles: 2, patrolRightTiles: 2 },
      { id: 't-axe-2', kind: 'ganado_axe', col: 71, row: 18, patrolLeftTiles: 2, patrolRightTiles: 1, easySkip: true },
      { id: 't-axe-3', kind: 'ganado_axe', col: 91, row: 18, patrolLeftTiles: 2, patrolRightTiles: 3 },
      { id: 't-fork-3', kind: 'ganado_fork', col: 97, row: 18, patrolLeftTiles: 3, patrolRightTiles: 2 },
      { id: 't-hc-fork-1', kind: 'ganado_fork', col: 18, row: 18, patrolLeftTiles: 2, patrolRightTiles: 2, minDifficulty: 'hardcore' },
      { id: 't-hc-axe-1', kind: 'ganado_axe', col: 41, row: 18, patrolLeftTiles: 1, patrolRightTiles: 3, minDifficulty: 'hardcore' },
      { id: 't-hc-fork-2', kind: 'ganado_fork', col: 94, row: 18, patrolLeftTiles: 2, patrolRightTiles: 2, minDifficulty: 'hardcore' },
      { id: 't-pro-axe-1', kind: 'ganado_axe', col: 32, row: 7, patrolLeftTiles: 1, patrolRightTiles: 2, minDifficulty: 'professional' },
      { id: 't-pro-fork-1', kind: 'ganado_fork', col: 75, row: 7, patrolLeftTiles: 1, patrolRightTiles: 2, minDifficulty: 'professional' },
      { id: 't-pro-axe-2', kind: 'ganado_axe', col: 10, row: 18, patrolLeftTiles: 2, patrolRightTiles: 2, minDifficulty: 'professional' },
    ],
    signs: [
      { x: 3 * TILE_SIZE, y: 18 * TILE_SIZE, text: 'Gunakan Shift untuk Dash melewati jurang antar menara!' },
    ],
  },
  {
    index: 3,
    id: 'church-courtyard',
    name: 'La Iglesia: Dr. Salvador',
    subtitle: 'Stage 4 • Halaman Gereja & Boss',
    biome: 'church',
    description: 'Terobos pertahanan desa dan kalahkan Dr. Salvador si pemegang gergaji mesin untuk membunyikan Lonceng Gereja!',
    // Ruler:  0         1         2         3         4         5         6         7         8         9         0         1         2
    //         0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123
    grid: [
      '############################################################################################################################',
      '#v...v......................................v...v...............................................................v......v...#',
      '#..........................................................................................................................#',
      '#...........................1................................................2.....................................3.......#',
      '#..........................###..............................................###...................................###......#',
      '#..........................................................................................................................#',
      '#....................S.............................................R.......................................................#',
      '#...................###...........C..C............................###......................................................#',
      '#................................------........................................................B..................M........#',
      '#.............................................................................................###................###.......#',
      '#............H...........................................M....................P............................................#',
      '#...........###.................................J.......###..................###..........................................G#',
      '#..............................................###........................................................................##',
      '#...............................E..................................................................-----................####',
      '#..............................###...................................................-----..............................####',
      '#.....................---..................................C..C........................................-----............####',
      '#.........................................................------........................................................####',
      '#.......................................................................................................................####',
      '#@...C.C....E.....................................E................W.....M...A.......E.................E............B...####',
      '######..##########..^^^^..############.....##################..^^..#########################################################',
      '######..##########..####..############..E..##################..##..#########################################################',
      '############################################################################################################################',
    ],
    platforms: [
      {
        id: 'c-plat-1',
        startCol: 38,
        startRow: 19,
        endCol: 42,
        endRow: 19,
        widthTiles: 3,
        speed: 62,
        pauseDuration: 0.35,
      },
    ],
    enemies: [
      { id: 'c-fork-1', kind: 'ganado_fork', col: 11, row: 18, patrolLeftTiles: 2, patrolRightTiles: 3 },
      { id: 'c-axe-1', kind: 'ganado_axe', col: 30, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3 },
      { id: 'c-fork-2', kind: 'ganado_fork', col: 51, row: 18, patrolLeftTiles: 4, patrolRightTiles: 4 },
      { id: 'c-axe-2', kind: 'ganado_axe', col: 58, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3, easySkip: true },
      { id: 'c-boss-1', kind: 'boss_salvador', col: 100, row: 18, patrolLeftTiles: 14, patrolRightTiles: 12 },
      { id: 'c-hc-fork-1', kind: 'ganado_fork', col: 15, row: 18, patrolLeftTiles: 2, patrolRightTiles: 1, minDifficulty: 'hardcore' },
      { id: 'c-hc-axe-1', kind: 'ganado_axe', col: 34, row: 7, patrolLeftTiles: 1, patrolRightTiles: 2, minDifficulty: 'hardcore' },
      { id: 'c-hc-fork-2', kind: 'ganado_fork', col: 88, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3, minDifficulty: 'hardcore' },
      { id: 'c-pro-axe-1', kind: 'ganado_axe', col: 28, row: 18, patrolLeftTiles: 1, patrolRightTiles: 2, minDifficulty: 'professional' },
      { id: 'c-pro-fork-1', kind: 'ganado_fork', col: 108, row: 18, patrolLeftTiles: 3, patrolRightTiles: 3, minDifficulty: 'professional' },
      { id: 'c-pro-axe-2', kind: 'ganado_axe', col: 114, row: 18, patrolLeftTiles: 3, patrolRightTiles: 2, minDifficulty: 'professional' },
    ],
    signs: [
      { x: 75 * TILE_SIZE, y: 18 * TILE_SIZE, text: 'Awas Dr. Salvador! Gunakan Granat, Shotgun & Rocket!' },
    ],
  },
];

export function parseLevel(
  def: LevelDefinition,
  difficulty: DifficultyId = 'normal'
): ParsedLevel {
  const rows = def.grid.length;
  const cols = def.grid[0]?.length ?? 0;
  const tiles: TileKind[][] = [];

  let spawn = { x: 2 * TILE_SIZE, y: 15 * TILE_SIZE };
  let goal = { x: (cols - 3) * TILE_SIZE, y: 15 * TILE_SIZE, w: 18, h: 24 };
  const checkpoints: Checkpoint[] = [];
  const merchants: MerchantStall[] = [];
  const pickups: PickupItem[] = [];
  const barrels: ExplosiveBarrel[] = [];

  let coinCount = 0;
  let itemCount = 0;
  let cpCount = 0;
  let merchantCount = 0;
  let barrelCount = 0;

  for (let r = 0; r < rows; r++) {
    const rowStr = def.grid[r];
    const tileRow: TileKind[] = [];
    for (let c = 0; c < cols; c++) {
      const ch = rowStr[c] ?? '.';
      tileRow.push(charToTile(ch));

      const px = c * TILE_SIZE;
      const py = r * TILE_SIZE;

      if (ch === '@') {
        spawn = { x: px + 1, y: (r + 1) * TILE_SIZE - 34 };
      } else if (ch === 'G') {
        goal = { x: px - 1, y: (r + 1) * TILE_SIZE - 24, w: 18, h: 24 };
      } else if (ch === 'P') {
        cpCount++;
        checkpoints.push({
          id: `${def.id}-cp-${cpCount}`,
          x: px + 2,
          y: py - 4,
          w: 12,
          h: 20,
          activated: false,
        });
      } else if (ch === 'C') {
        coinCount++;
        pickups.push({
          id: `${def.id}-coin-${coinCount}`,
          kind: 'coin',
          x: px + 3,
          y: py + 3,
          w: 10,
          h: 10,
          collected: false,
          bobOffset: (c + r) * 0.6,
        });
      } else if (ch === '1' || ch === '2' || ch === '3') {
        const shardIndex = Number(ch) - 1;
        pickups.push({
          id: `${def.id}-shard-${ch}`,
          kind: 'shard',
          x: px + 2,
          y: py + 2,
          w: 12,
          h: 12,
          collected: false,
          shardIndex,
          bobOffset: shardIndex * 1.2,
        });
      } else if (ch === 'M' || ch === 'B' || ch === 'S' || ch === 'H' || ch === 'R' || ch === 'A') {
        itemCount++;
        const kind =
          ch === 'M'
            ? 'medkit'
            : ch === 'B'
              ? 'grenade_box'
              : ch === 'S'
                ? 'shotgun'
                : ch === 'H'
                  ? 'mg'
                  : ch === 'R'
                    ? 'rpg'
                    : 'ammo';
        pickups.push({
          id: `${def.id}-item-${itemCount}`,
          kind,
          x: px + 1,
          y: py + 2,
          w: 14,
          h: 13,
          collected: false,
          bobOffset: itemCount * 0.7,
        });
      } else if (ch === 'W') {
        merchantCount++;
        merchants.push({
          id: `${def.id}-merchant-${merchantCount}`,
          x: px - 12,
          y: (r + 1) * TILE_SIZE - 48,
          w: 44,
          h: 48,
          animTime: 0,
        });
      } else if (ch === 'E') {
        barrelCount++;
        barrels.push({
          id: `${def.id}-barrel-${barrelCount}`,
          x: px - 1,
          y: (r + 1) * TILE_SIZE - 28,
          w: 18,
          h: 28,
          hp: 2,
          exploded: false,
          flashTimer: 0,
        });
      }
    }
    tiles.push(tileRow);
  }

  const platforms: MovingPlatform[] = def.platforms.map((spec) => {
    const startX = spec.startCol * TILE_SIZE;
    const startY = spec.startRow * TILE_SIZE;
    return {
      id: spec.id,
      startX,
      startY,
      endX: spec.endCol * TILE_SIZE,
      endY: spec.endRow * TILE_SIZE,
      x: startX,
      y: startY,
      prevX: startX,
      prevY: startY,
      w: spec.widthTiles * TILE_SIZE,
      h: 8,
      speed: spec.speed ?? 50,
      progress: 0,
      direction: 1,
      pauseTimer: 0,
      pauseDuration: spec.pauseDuration ?? 0.4,
    };
  });

  const diffCfg = DIFFICULTY_CONFIG[difficulty];
  const filteredSpecs = def.enemies.filter((spec) => {
    if (difficulty === 'easy' && spec.easySkip) return false;
    if (spec.minDifficulty === 'hardcore' && difficulty !== 'hardcore' && difficulty !== 'professional') {
      return false;
    }
    if (spec.minDifficulty === 'professional' && difficulty !== 'professional') {
      return false;
    }
    return true;
  });

  const enemies: Enemy[] = filteredSpecs.map((spec) => {
    const isBoss = spec.kind === 'boss_salvador';
    const scale = isBoss ? 1.25 : 1.0;
    const w = isBoss ? 22 : 16;
    const h = isBoss ? 44 : 34;
    const x = spec.col * TILE_SIZE;
    const y = (spec.row + 1) * TILE_SIZE - h;
    const leftTiles = spec.patrolLeftTiles ?? 3;
    const rightTiles = spec.patrolRightTiles ?? 3;
    const baseHp = spec.kind === 'ganado_axe' ? 5 : 3;
    const maxHp = isBoss ? diffCfg.bossHp : Math.max(2, Math.round(baseHp * diffCfg.enemyHpMult));

    return {
      id: spec.id,
      kind: spec.kind,
      x,
      y,
      vx: 0,
      vy: 0,
      w,
      h,
      facing: -1,
      hp: maxHp,
      maxHp,
      patrolMinX: (spec.col - leftTiles) * TILE_SIZE,
      patrolMaxX: (spec.col + rightTiles) * TILE_SIZE + TILE_SIZE - w,
      animTime: 0,
      state: 'patrol',
      stateTimer: 0,
      attackCooldown: 0.4,
      attackDidHit: false,
      hurtFlashTimer: 0,
      deathFadeTimer: 0,
      scale,
    };
  });

  return {
    def,
    cols,
    rows,
    width: cols * TILE_SIZE,
    height: rows * TILE_SIZE,
    tiles,
    spawn,
    goal,
    checkpoints,
    merchants,
    pickups,
    barrels,
    platforms,
    enemies,
    signs: def.signs ?? [],
  };
}
