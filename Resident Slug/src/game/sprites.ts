export const ATLAS_CELL = 128;
export const ATLAS_COLS = 10;

export const CHARACTER_FRAMES = {
  leon: {
    idle: [0, 1, 2, 3],
    afkIdle: [42, 43, 44, 45, 46, 47],
    walk: [4, 5, 6, 7, 8],
    run: [9, 10, 11, 12],
    shoot: [17, 18],
    melee: [37, 38, 39, 40, 41],
    knifeCombo: [48, 49, 50, 51],
    parry: [52, 53],
    grenade: [24, 25, 26, 27],
    dash: [29, 28],
    crouch: [13],
    jump: [14, 15],
    fall: [16],
    hurt: [54, 55],
    death: [21, 22, 23],
    walk_run_cqbr: [60, 61, 62, 63, 64, 65, 66, 67],
    walk_run_shotgun: [68, 69, 70, 71, 72, 73, 74, 75],
    leon_cqbr_jump: [77, 78],
    leon_cqbr_drop: [79, 80, 81],
    leon_shotgun_jump: [84, 85],
    leon_shotgun_drop: [86, 87, 88],
    leon_rpg_jump: [91, 92],
    leon_rpg_drop: [93, 94, 95],
    weapons: {
      shotgun: {
        idle: [31],
        run: [68, 69, 70, 71, 72, 73, 74, 75],
        jump: [84, 85],
        drop: [86, 87, 88],
        shoot: [32, 31],
      },
      rpg: {
        idle: [33],
        jump: [91, 92],
        drop: [93, 94, 95],
        shoot: [34, 33],
      },
      mg: {
        idle: [35],
        run: [60, 61, 62, 63, 64, 65, 66, 67],
        jump: [77, 78],
        drop: [79, 80, 81],
        shoot: [36, 35],
      },
    },
  },
  ada: {
    idle: [0, 1, 2, 3],
    afkIdle: [40, 41, 42, 43, 44, 45],
    walk: [4, 5, 6, 7],
    run: [8, 9, 10, 11],
    shoot: [48, 49],
    melee: [29, 30, 31, 32],
    knifeCombo: [29, 30, 31, 32],
    parry: [30],
    grenade: [54, 55],
    dash: [20],
    crouch: [12],
    jump: [50],
    fall: [51],
    hurt: [34, 35, 36],
    death: [37, 38, 39],
    ada_jump: [67],
    ada_drop_platform: [68, 69],
    ada_shotgun_walk: [60, 64],
    ada_shotgun_run: [61, 62, 63],
    ada_shotgun_jump: [65],
    ada_shotgun_drop: [66, 66],
    ada_cqbr_walk: [70, 71, 72],
    ada_cqbr_run: [73, 74, 75],
    ada_cqbr_jump: [77, 78, 79],
    ada_cqbr_drop: [80, 81],
    weapons: {
      shotgun: {
        idle: [16],
        walk: [60, 64],
        run: [61, 62, 63],
        jump: [65],
        drop: [66, 66],
        shoot: [17, 18],
      },
      rpg: { idle: [22], shoot: [24, 23] },
      mg: {
        idle: [25],
        walk: [70, 71, 72],
        run: [73, 74, 75],
        jump: [77, 78, 79],
        drop: [80, 81],
        shoot: [25],
        crouchShoot: [26],
      },
    },
  },
  ganado_fork: {
    idle: [0, 1, 2, 3],
    walk: [4, 5, 6, 7, 8, 9],
    run: [10, 11, 12, 13, 14],
    windup: [15, 16],
    attack: [17, 18],
    throw: [18],
    jump: [19, 20, 21, 22],
    hurt: [23, 24, 25, 26],
    death: [27, 28, 29],
  },
  ganado_axe: {
    idle: [0, 1, 2, 3],
    walk: [4, 5, 6, 7, 8, 9],
    run: [10, 11, 12, 13, 14],
    windup: [15, 16],
    attack: [17],
    throw: [18],
    jump: [19, 20, 21, 22],
    hurt: [23, 24, 25, 26],
    death: [27, 28, 29],
  },
  merchant: {
    idle: [0, 1, 2, 3],
    openCoat: [5, 6, 7, 8, 10, 11],
    wave: [9, 13],
    herb: [17],
    spray: [18],
    gold: [21],
    chuckle: [26, 27, 28, 29],
  },
  salvador: {
    idle: [0, 1, 2, 3],
    walk: [4, 5, 6, 7, 8, 9],
    run: [10, 11, 12, 13, 14],
    windup: [15, 16],
    attack: [17, 18],
    leap: [19, 20, 21],
    land: [22],
    hurt: [23, 24, 25, 26],
    death: [27, 28, 29],
  },
} as const;

export interface GameAssets {
  atlases: {
    leon: HTMLImageElement;
    ada: HTMLImageElement;
    ganado_fork: HTMLImageElement;
    ganado_axe: HTMLImageElement;
    merchant: HTMLImageElement;
    salvador: HTMLImageElement;
  };
  thrownAxe: HTMLImageElement;
  bgVillage: HTMLImageElement;
  weapons: {
    shotgunIdle: HTMLImageElement;
    shotgunFire: HTMLImageElement;
    cqbrIdle: HTMLImageElement;
    cqbrFire: HTMLImageElement;
    rpgIdle: HTMLImageElement;
    rpgFire: HTMLImageElement;
    rpgRocket: HTMLImageElement;
    shellShotgun: HTMLImageElement;
    shellCqbr: HTMLImageElement;
    ammoShotgun: HTMLImageElement;
    ammoCqbr: HTMLImageElement;
  };
  items: {
    coin: HTMLCanvasElement[];
    medkit: HTMLCanvasElement;
    grenadeBox: HTMLCanvasElement;
    crateShotgun: HTMLCanvasElement;
    crateMg: HTMLCanvasElement;
    crateRpg: HTMLCanvasElement;
    crateAmmo: HTMLCanvasElement;
    shard: HTMLCanvasElement;
    barrel: HTMLCanvasElement;
    merchantStall: HTMLCanvasElement;
    spring: HTMLCanvasElement;
    checkpointOff: HTMLCanvasElement;
    checkpointOn: HTMLCanvasElement[];
    bell: HTMLCanvasElement[];
    bellLocked: HTMLCanvasElement;
  };
}

function makePixelSprite(rows: string[], palette: Record<string, string>): HTMLCanvasElement {
  const height = rows.length;
  const width = rows[0]?.length ?? 1;
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext('2d');
  if (!ctx) return canvas;

  for (let y = 0; y < height; y++) {
    const row = rows[y];
    for (let x = 0; x < width; x++) {
      const ch = row[x];
      if (ch === '.' || ch === ' ') continue;
      const color = palette[ch];
      if (!color) continue;
      ctx.fillStyle = color;
      ctx.fillRect(x, y, 1, 1);
    }
  }
  return canvas;
}

function makeWeaponCrate(letterPattern: string[], badgeBg: string, badgeFg: string): HTMLCanvasElement {
  const rows = [
    'OOOOOOOOOOOOOO',
    'OWWWWWWWWWWWWO',
    'OWBBBBBBBBBBWO',
    'OWBCCCCCCCCBWO',
    'OWBCCCCCCCCBWO',
    'OWBCCCCCCCCBWO',
    'OWBCCCCCCCCBWO',
    'OWBCCCCCCCCBWO',
    'OWBCCCCCCCCBWO',
    'OWBCCCCCCCCBWO',
    'OWBBBBBBBBBBWO',
    'ODDDDDDDDDDDDO',
    'OOOOOOOOOOOOOO',
  ];
  const base = makePixelSprite(rows, {
    O: '#1a120b',
    W: '#a16207',
    D: '#713f12',
    B: '#451a03',
    C: badgeBg,
  });
  const ctx = base.getContext('2d');
  if (ctx) {
    ctx.fillStyle = badgeFg;
    for (let y = 0; y < letterPattern.length; y++) {
      for (let x = 0; x < letterPattern[y].length; x++) {
        if (letterPattern[y][x] === '#') {
          ctx.fillRect(4 + x, 4 + y, 1, 1);
        }
      }
    }
  }
  return base;
}

function makeMerchantStallSprite(): HTMLCanvasElement {
  const canvas = document.createElement('canvas');
  canvas.width = 72;
  canvas.height = 58;
  const ctx = canvas.getContext('2d');
  if (!ctx) return canvas;

  // Back timber frame & wooden cart shadow
  ctx.fillStyle = '#24170f';
  ctx.fillRect(8, 14, 56, 42);

  // Wooden booth posts
  ctx.fillStyle = '#5c3a21';
  ctx.fillRect(8, 12, 5, 44);
  ctx.fillRect(59, 12, 5, 44);
  ctx.fillStyle = '#3b2414';
  ctx.fillRect(12, 14, 1, 42);
  ctx.fillRect(59, 14, 1, 42);

  // Tattered striped canopy roof
  ctx.fillStyle = '#1c1917';
  ctx.fillRect(3, 6, 66, 10);
  for (let x = 4; x < 68; x += 8) {
    ctx.fillStyle = ((x / 8) % 2 === 0) ? '#6d28d9' : '#312e81';
    ctx.fillRect(x, 7, 8, 7);
    ctx.fillStyle = ((x / 8) % 2 === 0) ? '#8b5cf6' : '#4338ca';
    ctx.fillRect(x, 7, 8, 2);
  }
  // Roof wooden trim & scalloped edge
  ctx.fillStyle = '#78350f';
  ctx.fillRect(4, 14, 64, 3);

  // Wooden "SHOP" / Merchant signboard on roof
  ctx.fillStyle = '#451a03';
  ctx.fillRect(21, 0, 30, 8);
  ctx.strokeStyle = '#d97706';
  ctx.strokeRect(21.5, 0.5, 29, 7);
  ctx.fillStyle = '#ffd447';
  ctx.font = 'bold 6px "Courier New", monospace';
  ctx.textBaseline = 'top';
  ctx.fillText('SHOP', 28, 1);

  // Wooden counter & shelf crates on sides
  ctx.fillStyle = '#78350f';
  ctx.fillRect(6, 43, 60, 4);
  ctx.fillStyle = '#92400e';
  ctx.fillRect(6, 43, 60, 2);
  ctx.fillStyle = '#451a03';
  ctx.fillRect(9, 47, 54, 9);

  // Cart wheels on lower sides (wooden wagon look!)
  ctx.fillStyle = '#1c1917';
  ctx.fillRect(10, 48, 10, 10);
  ctx.fillRect(52, 48, 10, 10);
  ctx.fillStyle = '#78350f';
  ctx.fillRect(11, 49, 8, 8);
  ctx.fillRect(53, 49, 8, 8);
  ctx.fillStyle = '#292524';
  ctx.fillRect(13, 51, 4, 4);
  ctx.fillRect(55, 51, 4, 4);

  // Hanging RE4 Blue Flame Lanterns on left & right posts
  for (const lx of [5, 64]) {
    ctx.fillStyle = '#1e293b';
    ctx.fillRect(lx, 17, 3, 2);
    ctx.fillStyle = '#38bdf8';
    ctx.fillRect(lx - 1, 19, 5, 6);
    ctx.fillStyle = '#e0f2fe';
    ctx.fillRect(lx, 20, 3, 4);
  }

  return canvas;
}

function createProceduralItems(): GameAssets['items'] {
  const coinPalette = { O: '#451a03', G: '#ffd447', L: '#fef08a', D: '#d97706' };
  const coin0 = makePixelSprite(
    [
      '..OOOOOO..',
      '.OGLLLLGO.',
      'OGLLGGGGDO',
      'OGLGDDGGDO',
      'OGLGDDGGDO',
      'OGLGDDGGDO',
      'OGLGDDGGDO',
      'OGGGGGDDDO',
      '.ODDDDDDO.',
      '..OOOOOO..',
    ],
    coinPalette
  );
  const coin1 = makePixelSprite(
    [
      '...OOOO...',
      '..OGLLGO..',
      '.OGLGDDGO.',
      '.OGLGDDGO.',
      '.OGLGDDGO.',
      '.OGLGDDGO.',
      '.OGLGDDGO.',
      '.OGGDDDGO.',
      '..ODDDDO..',
      '...OOOO...',
    ],
    coinPalette
  );
  const coin2 = makePixelSprite(
    [
      '....OO....',
      '...OGGO...',
      '...OLGO...',
      '...OLDO...',
      '...OLDO...',
      '...OLDO...',
      '...OLDO...',
      '...OGDO...',
      '...ODDO...',
      '....OO....',
    ],
    coinPalette
  );

  const medkit = makePixelSprite(
    [
      '....OOOOOO....',
      '...OWWWWWWO...',
      '.OOOOOOOOOOOO.',
      'OWWWWWWWWWWWWO',
      'OWWWWWRRWWWWWO',
      'OWWWWWRRWWWWWO',
      'OWWWRRRRRRWWWO',
      'OWWWRRRRRRWWWO',
      'OWWWWWRRWWWWWO',
      'OWWWWWRRWWWWWO',
      'OGGGGGGGGGGGGO',
      '.OOOOOOOOOOOO.',
    ],
    {
      O: '#111827',
      W: '#f8fafc',
      G: '#cbd5e1',
      R: '#ef4444',
    }
  );

  const grenadeBox = makePixelSprite(
    [
      '.....OOOO.....',
      '....OYYYYO....',
      '...OOGGGGOO...',
      '..OGGGGGGGGO..',
      '..OGDDDDDDGO..',
      '..OGGGGGGGGO..',
      '..OGDDDDDDGO..',
      '..OGGGGGGGGO..',
      '...OGDDDDGO...',
      '....OOOOOO....',
    ],
    {
      O: '#0f172a',
      Y: '#ffd447',
      G: '#4d7c0f',
      D: '#365314',
    }
  );

  const crateShotgun = makeWeaponCrate(
    ['.#####', '#.....', '.####.', '.....#', '#####.'],
    '#7f1d1d',
    '#ffd447'
  );
  const crateMg = makeWeaponCrate(
    ['#....#', '#....#', '######', '#....#', '#....#'],
    '#1e3a8a',
    '#4deeea'
  );
  const crateRpg = makeWeaponCrate(
    ['#####.', '#....#', '#####.', '#..#..', '#...##'],
    '#581c87',
    '#ff75c3'
  );
  const crateAmmo = makeWeaponCrate(
    ['..##..', '.#..#.', '######', '#....#', '#....#'],
    '#14532d',
    '#bbf7d0'
  );

  const shard = makePixelSprite(
    [
      '.....OO.....',
      '....OYYO....',
      '...OYWWYO...',
      '.OOYWWWWYOO.',
      'OYYWPPPPWYYO',
      'OWWPPPPPPWWO',
      'OYYPPPPPPYYO',
      '.OOYPPPPYOO.',
      '...OYPPYO...',
      '..OYYOOYYO..',
      '..OO....OO..',
      '............',
    ],
    {
      O: '#2e1065',
      Y: '#ffd447',
      W: '#ffffff',
      P: '#f472b6',
    }
  );

  // Taller 18x28 Explosive Red Barrel (easily shot while standing!)
  const barrel = makePixelSprite(
    [
      '..OOOOOOOOOOOOOO..',
      '.OKKKKKKKKKKKKKKO.',
      '.ORRRRRRRRRRRRRRO.',
      '.ORRWWWRRRRRRWWRO.',
      '.ORRRRRRRRRRRRRRO.',
      '.ORRRRRRRRRRRRRRO.',
      'OOKKKKKKKKKKKKKKOO',
      'OKKKKKKKKKKKKKKKKO',
      'ORRRRRRYYYYRRRRRRO',
      'ORRRRRYYFFYYRRRRRO',
      'ORRRRYYFFFFYYRRRRO',
      'ORRRRYYFFFFYYRRRRO',
      'ORRRRRYFFFFYRRRRRO',
      'ORRRRRRYYYYRRRRRRO',
      'ORRRRRRRRRRRRRRRRO',
      'OOKKKKKKKKKKKKKKOO',
      'OKKKKKKKKKKKKKKKKO',
      '.ORRRRRRRRRRRRRRO.',
      '.ORRWWWRRRRRRWWRO.',
      '.ORRRRRRRRRRRRRRO.',
      '.ORRRRRRRRRRRRRRO.',
      '.ODDDDDDDDDDDDDDO.',
      'OOKKKKKKKKKKKKKKOO',
      '.ODDDDDDDDDDDDDDO.',
      '.ODDDDDDDDDDDDDDO.',
      '.OKKKKKKKKKKKKKKO.',
      '..OOOOOOOOOOOOOO..',
      '..................',
    ],
    {
      O: '#1c1917',
      K: '#57534e',
      R: '#dc2626',
      D: '#991b1b',
      W: '#fca5a5',
      Y: '#ffd447',
      F: '#f97316',
    }
  );

  const spring = makePixelSprite(
    [
      '................',
      '................',
      '................',
      '................',
      '................',
      '................',
      '................',
      '................',
      '..OOOOOOOOOOOO..',
      '.OWWWWWWWWWWWWO.',
      '.OYYYYYYYYYYYYO.',
      '..OOOOOOOOOOOO..',
      '....OKKKKKKO....',
      '...OKKKKKKKKO...',
      '..OOOOOOOOOOOO..',
      '.OGGGGGGGGGGGGO.',
    ],
    {
      O: '#1e293b',
      W: '#b45309',
      Y: '#ffd447',
      K: '#94a3b8',
      G: '#475569',
    }
  );

  const checkpointOff = makePixelSprite(
    [
      '....OOOOOO....',
      '...OCCCCCCCO..',
      '...OCDDDDDCO..',
      '...OCCCCCCCO..',
      '...OOOOOO.....',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '....OOOO......',
      '...OOOOOO.....',
      '..OOOOOOOO....',
      '..OOOOOOOO....',
    ],
    { O: '#1c1917', C: '#64748b', D: '#475569' }
  );

  const checkpointOn0 = makePixelSprite(
    [
      '....OOOOOO....',
      '...OCYYYYYCO..',
      '...OYWWWWYYO..',
      '...OCYYYYYCO..',
      '...OOOOOO.....',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '....OOOO......',
      '...OBBBBBO....',
      '..OBBBBBBBBO..',
      '..OOOOOOOOOO..',
    ],
    { O: '#1c1917', C: '#fb923c', Y: '#ffd447', W: '#ffffff', B: '#78350f' }
  );

  const checkpointOn1 = makePixelSprite(
    [
      '....OOOOOO....',
      '...OYCCCCCCO..',
      '...OCWWWWCCO..',
      '...OYCCCCCCO..',
      '...OOOOOO.....',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '.....OO.......',
      '....OOOO......',
      '...OBBBBBO....',
      '..OBBBBBBBBO..',
      '..OOOOOOOOOO..',
    ],
    { O: '#1c1917', C: '#f97316', Y: '#ffd447', W: '#ffffff', B: '#78350f' }
  );

  const bell0 = makePixelSprite(
    [
      '..OOOOOOOOOOOOOO..',
      '.OBBBBBBBBBBBBBBO.',
      '.OBBWWWWWWWWWWBBO.',
      '..O..OOOOOOOO..O..',
      '..O..OYYYYYYO..O..',
      '..O.OYYLLLLYYO.O..',
      '..O.OYLLLLLLYO.O..',
      '..O.OYYYYYYYYO.O..',
      '..O.OYYDDDDYYO.O..',
      '..O.OYYYYYYYYO.O..',
      '..O.OYYYYYYYYO.O..',
      '..O.OYDDDDDDYO.O..',
      '..OOYYYYYYYYYYOO..',
      '..OYYYYWWWWYYYYO..',
      '..OOOOOOOOOOOOOO..',
      '..O.....OO.....O..',
      '..O.....YY.....O..',
      '..O............O..',
      '..O............O..',
      '..O............O..',
      '.OOOOOOOOOOOOOOOO.',
      'OGGGGGGGGGGGGGGGGO',
      'OGDDDDDDDDDDDDDDGO',
      'OOOOOOOOOOOOOOOOOO',
    ],
    { O: '#1c1917', B: '#78350f', W: '#b45309', Y: '#ffd447', L: '#fef08a', D: '#b45309', G: '#57534e' }
  );

  const bell1 = makePixelSprite(
    [
      '..OOOOOOOOOOOOOO..',
      '.OBBBBBBBBBBBBBBO.',
      '.OBBWWWWWWWWWWBBO.',
      '..O..OOOOOOOO..O..',
      '..O..OYYYYYYO..O..',
      '..O.OYYLLLLYYO.O..',
      '..O.OYLLLLLLYO.O..',
      '..O.OYYYYYYYYO.O..',
      '..O.OYYDDDDYYO.O..',
      '..O.OYYYYYYYYO.O..',
      '..O.OYYYYYYYYO.O..',
      '..O.OYDDDDDDYO.O..',
      '..OOYYYYYYYYYYOO..',
      '..OYYYYWWWWYYYYO..',
      '..OOOOOOOOOOOOOO..',
      '..O....OO......O..',
      '..O....YY......O..',
      '..O............O..',
      '..O............O..',
      '..O............O..',
      '.OOOOOOOOOOOOOOOO.',
      'OGGGGGGGGGGGGGGGGO',
      'OGDDDDDDDDDDDDDDGO',
      'OOOOOOOOOOOOOOOOOO',
    ],
    { O: '#1c1917', B: '#78350f', W: '#b45309', Y: '#fde047', L: '#ffffff', D: '#d97706', G: '#57534e' }
  );

  const bellLocked = makePixelSprite(
    [
      '..OOOOOOOOOOOOOO..',
      '.OBBBBBBBBBBBBBBO.',
      '.OBBWWWWWWWWWWBBO.',
      '..O..OOOOOOOO..O..',
      '..O..OKKKKKKKO.O..',
      '..O.OKKRRRRKKO.O..',
      '..O.OKRRRRRRKO.O..',
      '..O.OKKKKKKKKO.O..',
      '..O.OKKRRRRKKO.O..',
      '..O.OKKKKKKKKO.O..',
      '..O.OKKKKKKKKO.O..',
      '..O.OKDDDDDDKO.O..',
      '..OOKKKKKKKKKKOO..',
      '..OKKKKRRRRKKKKO..',
      '..OOOOOOOOOOOOOO..',
      '..O.....OO.....O..',
      '..O.....RR.....O..',
      '..O............O..',
      '..O............O..',
      '..O............O..',
      '.OOOOOOOOOOOOOOOO.',
      'OGGGGGGGGGGGGGGGGO',
      'OGDDDDDDDDDDDDDDGO',
      'OOOOOOOOOOOOOOOOOO',
    ],
    { O: '#1c1917', B: '#44403c', W: '#57534e', K: '#52525b', D: '#3f3f46', R: '#ef4444', G: '#27272a' }
  );

  return {
    coin: [coin0, coin1, coin2, coin1],
    medkit,
    grenadeBox,
    crateShotgun,
    crateMg,
    crateRpg,
    crateAmmo,
    shard,
    barrel,
    merchantStall: makeMerchantStallSprite(),
    spring,
    checkpointOff,
    checkpointOn: [checkpointOn0, checkpointOn1],
    bell: [bell0, bell1],
    bellLocked,
  };
}

function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = () => reject(new Error(`Failed to load asset: ${src}`));
    img.src = src;
  });
}

export async function loadGameAssets(): Promise<GameAssets> {
  const [
    leon,
    ada,
    ganadoFork,
    ganadoAxe,
    merchant,
    salvador,
    thrownAxe,
    bgVillage,
    shotgunIdle,
    shotgunFire,
    cqbrIdle,
    cqbrFire,
    rpgIdle,
    rpgFire,
    rpgRocket,
    shellShotgun,
    shellCqbr,
    ammoShotgun,
    ammoCqbr,
  ] = await Promise.all([
    loadImage('./assets/sprites_leon.png'),
    loadImage('./assets/sprites_ada.png'),
    loadImage('./assets/sprites_ganado_fork.png'),
    loadImage('./assets/sprites_ganado_axe.png'),
    loadImage('./assets/sprites_merchant.png'),
    loadImage('./assets/sprites_salvador.png'),
    loadImage('./assets/sprite_thrown_axe.png'),
    loadImage('./assets/bg_village.png'),
    loadImage('./assets/weapon_shotgun_idle.png'),
    loadImage('./assets/weapon_shotgun_fire.png'),
    loadImage('./assets/weapon_cqbr_idle.png'),
    loadImage('./assets/weapon_cqbr_fire.png'),
    loadImage('./assets/weapon_rpg_idle.png'),
    loadImage('./assets/weapon_rpg_fire.png'),
    loadImage('./assets/projectile_rpg_rocket.png'),
    loadImage('./assets/shell_shotgun.png'),
    loadImage('./assets/shell_cqbr.png'),
    loadImage('./assets/pickup_shotgun_ammo.png'),
    loadImage('./assets/pickup_cqbr_ammo.png'),
  ]);

  return {
    atlases: {
      leon,
      ada,
      ganado_fork: ganadoFork,
      ganado_axe: ganadoAxe,
      merchant,
      salvador,
    },
    thrownAxe,
    bgVillage,
    weapons: {
      shotgunIdle,
      shotgunFire,
      cqbrIdle,
      cqbrFire,
      rpgIdle,
      rpgFire,
      rpgRocket,
      shellShotgun,
      shellCqbr,
      ammoShotgun,
      ammoCqbr,
    },
    items: createProceduralItems(),
  };
}
