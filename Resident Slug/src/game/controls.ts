export type ControlAction =
  | 'left'
  | 'right'
  | 'up'
  | 'down'
  | 'jump'
  | 'shoot'
  | 'melee'
  | 'grenade'
  | 'dash'
  | 'heal'
  | 'reload'
  | 'switchWeapon'
  | 'interact'
  | 'pause'
  | 'restartCp';

export type ControlBindings = Record<ControlAction, string[]>;

export interface ControlActionMeta {
  action: ControlAction;
  label: string;
  desc: string;
}

export const CONTROL_ACTIONS: ControlActionMeta[] = [
  { action: 'left', label: 'Gerak Kiri', desc: 'Berjalan/lari ke kiri' },
  { action: 'right', label: 'Gerak Kanan', desc: 'Berjalan/lari ke kanan' },
  { action: 'up', label: 'Bidik Atas', desc: 'Tahan untuk menembak ke atas / diagonal' },
  { action: 'down', label: 'Runduk / Turun', desc: 'Menunduk atau turun dari platform kayu' },
  { action: 'jump', label: 'Lompat', desc: 'Lompat, Double Jump, & Wall Jump' },
  { action: 'shoot', label: 'Tembak Senjata', desc: 'Menembakkan senjata aktif' },
  { action: 'melee', label: 'Melee / Parry', desc: 'Knife / Kick jarak dekat & tangkis kapak' },
  { action: 'grenade', label: 'Lempar Granat', desc: 'Melempar Frag Grenade ledakan area' },
  { action: 'dash', label: 'Dash / Dodge', desc: 'Gerak cepat menghindari serangan & jurang' },
  { action: 'heal', label: 'Pakai Medkit', desc: 'Gunakan First-Aid Medkit (+2 HP)' },
  { action: 'reload', label: 'Reload Senjata', desc: 'Isi ulang magasin senjata aktif' },
  { action: 'switchWeapon', label: 'Ganti Senjata', desc: 'Putar senjata (atau pakai 1-4 & Scroll Mouse)' },
  { action: 'interact', label: 'Buka Toko Merchant', desc: 'Interaksi saat berada di dekat gerobak toko' },
  { action: 'pause', label: 'Pause / Menu', desc: 'Menjeda permainan' },
  { action: 'restartCp', label: 'Ulang Checkpoint', desc: 'Kembali cepat ke checkpoint terakhir' },
];

export const DEFAULT_BINDINGS: ControlBindings = {
  left: ['ArrowLeft', 'KeyA'],
  right: ['ArrowRight', 'KeyD'],
  up: ['ArrowUp', 'KeyW'],
  down: ['ArrowDown', 'KeyS'],
  jump: ['Space', 'KeyK'],
  shoot: ['KeyJ', 'KeyZ'],
  melee: ['KeyL', 'KeyX', 'KeyF'],
  grenade: ['KeyC', 'KeyG', 'KeyU'],
  dash: ['ShiftLeft', 'ShiftRight'],
  heal: ['KeyE', 'KeyH'],
  reload: ['KeyR'],
  switchWeapon: ['KeyQ', 'Tab'],
  interact: ['KeyB', 'KeyV', 'Enter'],
  pause: ['Escape', 'KeyP'],
  restartCp: ['Backspace'],
};

const CONTROLS_STORAGE_KEY = 'resident_slug_controls_v2';

export function cloneDefaultBindings(): ControlBindings {
  const copy = {} as ControlBindings;
  for (const key of Object.keys(DEFAULT_BINDINGS) as ControlAction[]) {
    copy[key] = [...DEFAULT_BINDINGS[key]];
  }
  return copy;
}

export function loadKeyBindings(): ControlBindings {
  const defaults = cloneDefaultBindings();
  if (typeof window === 'undefined' || !window.localStorage) {
    return defaults;
  }
  try {
    const raw = window.localStorage.getItem(CONTROLS_STORAGE_KEY);
    if (!raw) return defaults;
    const parsed = JSON.parse(raw) as Partial<Record<ControlAction, unknown>>;
    for (const meta of CONTROL_ACTIONS) {
      const val = parsed[meta.action];
      if (Array.isArray(val) && val.length > 0 && val.every((v) => typeof v === 'string')) {
        defaults[meta.action] = val as string[];
      }
    }
    return defaults;
  } catch {
    return defaults;
  }
}

export function saveKeyBindings(bindings: ControlBindings): void {
  if (typeof window === 'undefined' || !window.localStorage) return;
  try {
    window.localStorage.setItem(CONTROLS_STORAGE_KEY, JSON.stringify(bindings));
  } catch {
    // Ignore storage errors
  }
}

export function resetKeyBindings(): ControlBindings {
  const defaults = cloneDefaultBindings();
  saveKeyBindings(defaults);
  return defaults;
}

export function rebindAction(
  bindings: ControlBindings,
  action: ControlAction,
  newCode: string
): ControlBindings {
  const prevPrimary = bindings[action][0];
  const updated = {} as ControlBindings;
  for (const meta of CONTROL_ACTIONS) {
    if (meta.action === action) {
      updated[action] = [newCode];
      continue;
    }
    const filtered = bindings[meta.action].filter((c) => c !== newCode);
    if (filtered.length > 0) {
      updated[meta.action] = filtered;
    } else if (prevPrimary && prevPrimary !== newCode) {
      updated[meta.action] = [prevPrimary];
    } else {
      updated[meta.action] = [];
    }
  }
  saveKeyBindings(updated);
  return updated;
}

export function formatKeyCode(code: string): string {
  if (code.startsWith('Key')) return code.slice(3);
  if (code.startsWith('Digit')) return code.slice(5);
  switch (code) {
    case 'ArrowLeft':
      return '←';
    case 'ArrowRight':
      return '→';
    case 'ArrowUp':
      return '↑';
    case 'ArrowDown':
      return '↓';
    case 'Space':
      return 'Space';
    case 'ShiftLeft':
      return 'L-Shift';
    case 'ShiftRight':
      return 'R-Shift';
    case 'ControlLeft':
      return 'L-Ctrl';
    case 'ControlRight':
      return 'R-Ctrl';
    case 'AltLeft':
      return 'L-Alt';
    case 'AltRight':
      return 'R-Alt';
    case 'Escape':
      return 'Esc';
    case 'Enter':
      return 'Enter';
    case 'Tab':
      return 'Tab';
    case 'Backspace':
      return 'Bksp';
    default:
      return code;
  }
}

export function formatActionKeys(codes: string[]): string {
  return codes.map(formatKeyCode).join(' / ');
}
