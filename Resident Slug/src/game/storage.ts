import { LEVELS } from './levels';
import { DIFFICULTY_ORDER, DifficultyId, LevelProgress, PlayerUpgrades, SaveData } from './types';

const STORAGE_KEY = 'resident_slug_pueblo_save_v2';

export function createDefaultUpgrades(): PlayerUpgrades {
  return {
    maxHpLevel: 0,
    damageLevel: 0,
    agilityLevel: 0,
    pouchLevel: 0,
  };
}

function createDefaultSave(): SaveData {
  const levels: Record<string, LevelProgress> = {};
  for (const lvl of LEVELS) {
    levels[lvl.id] = {
      completed: false,
      bestTime: null,
      bestScore: 0,
      shards: [false, false, false],
    };
  }
  return {
    unlockedLevelIndex: 0,
    selectedDifficulty: 'normal',
    pesetas: 0,
    upgrades: createDefaultUpgrades(),
    levels,
  };
}

export function loadSaveData(): SaveData {
  const defaults = createDefaultSave();
  if (typeof window === 'undefined' || !window.localStorage) {
    return defaults;
  }
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return defaults;
    const parsed = JSON.parse(raw) as Partial<SaveData>;
    const diff = DIFFICULTY_ORDER.includes(parsed.selectedDifficulty as DifficultyId)
      ? (parsed.selectedDifficulty as DifficultyId)
      : 'normal';
    const merged: SaveData = {
      unlockedLevelIndex: Math.max(
        0,
        Math.min(LEVELS.length - 1, Number(parsed.unlockedLevelIndex ?? 0))
      ),
      selectedDifficulty: diff,
      pesetas: Math.max(0, Number(parsed.pesetas ?? 0)),
      // Merchant character stat upgrades never persist across full game exit/reload
      upgrades: createDefaultUpgrades(),
      levels: { ...defaults.levels },
    };
    if (parsed.levels && typeof parsed.levels === 'object') {
      for (const lvl of LEVELS) {
        const entry = parsed.levels[lvl.id];
        if (entry) {
          merged.levels[lvl.id] = {
            completed: Boolean(entry.completed),
            bestTime: typeof entry.bestTime === 'number' ? entry.bestTime : null,
            bestScore: Number(entry.bestScore ?? 0),
            shards: [
              Boolean(entry.shards?.[0]),
              Boolean(entry.shards?.[1]),
              Boolean(entry.shards?.[2]),
            ],
          };
        }
      }
    }
    return merged;
  } catch {
    return defaults;
  }
}

export function saveSelectedDifficulty(difficulty: DifficultyId): void {
  if (typeof window === 'undefined' || !window.localStorage) return;
  try {
    const data = loadSaveData();
    data.selectedDifficulty = difficulty;
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  } catch {
    // Ignore storage quota errors
  }
}

export function saveWalletAndUpgrades(pesetas: number, _upgrades?: PlayerUpgrades): void {
  if (typeof window === 'undefined' || !window.localStorage) return;
  try {
    const data = loadSaveData();
    data.pesetas = Math.max(0, Math.floor(pesetas));
    data.upgrades = createDefaultUpgrades();
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  } catch {
    // Ignore storage quota errors
  }
}

export function saveLevelVictory(
  levelIndex: number,
  levelId: string,
  elapsedTime: number,
  score: number,
  shards: boolean[],
  pesetas: number,
  _upgrades?: PlayerUpgrades
): SaveData {
  const data = loadSaveData();
  const prev = data.levels[levelId] ?? {
    completed: false,
    bestTime: null,
    bestScore: 0,
    shards: [false, false, false] as [boolean, boolean, boolean],
  };

  data.levels[levelId] = {
    completed: true,
    bestTime: prev.bestTime === null ? elapsedTime : Math.min(prev.bestTime, elapsedTime),
    bestScore: Math.max(prev.bestScore, score),
    shards: [
      prev.shards[0] || Boolean(shards[0]),
      prev.shards[1] || Boolean(shards[1]),
      prev.shards[2] || Boolean(shards[2]),
    ],
  };

  data.pesetas = Math.max(0, Math.floor(pesetas));
  data.upgrades = createDefaultUpgrades();

  if (levelIndex + 1 < LEVELS.length) {
    data.unlockedLevelIndex = Math.max(data.unlockedLevelIndex, levelIndex + 1);
  }

  if (typeof window !== 'undefined' && window.localStorage) {
    try {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
    } catch {
      // Ignore storage quota errors
    }
  }
  return data;
}
