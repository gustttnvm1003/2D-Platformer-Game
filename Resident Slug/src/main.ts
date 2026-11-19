import './style.css';
import { RetroAudio } from './game/audio';
import {
  CONTROL_ACTIONS,
  ControlAction,
  formatActionKeys,
  rebindAction,
  resetKeyBindings,
} from './game/controls';
import { EngineMode, GameEngine } from './game/engine';
import { LEVELS } from './game/levels';
import { renderHeroPreview, renderMerchantPreview } from './game/renderer';
import { getShopOffers } from './game/shop';
import { CHARACTER_FRAMES, loadGameAssets } from './game/sprites';
import { loadSaveData } from './game/storage';
import { DIFFICULTY_CONFIG, DifficultyId, HeroId, WorldState } from './game/types';

function byId<T extends HTMLElement>(id: string): T {
  const el = document.getElementById(id);
  if (!el) throw new Error(`Missing DOM element #${id}`);
  return el as T;
}

const gameShell = byId<HTMLDivElement>('game-shell');
const canvas = byId<HTMLCanvasElement>('game-canvas');
const crtOverlay = byId<HTMLDivElement>('crt-overlay');

const screenTitle = byId<HTMLElement>('screen-title');
const screenLevels = byId<HTMLElement>('screen-levels');
const screenControls = byId<HTMLElement>('screen-controls');
const screenShop = byId<HTMLElement>('screen-shop');
const screenPause = byId<HTMLElement>('screen-pause');
const screenVictory = byId<HTMLElement>('screen-victory');
const screenGameover = byId<HTMLElement>('screen-gameover');

const previewCanvasMerchant = byId<HTMLCanvasElement>('preview-canvas-merchant');
const shopWalletLabel = byId<HTMLSpanElement>('shop-wallet-label');
const shopQuote = byId<HTMLParagraphElement>('shop-quote');
const shopPlayerStats = byId<HTMLDivElement>('shop-player-stats');
const shopCatalog = byId<HTMLDivElement>('shop-catalog');
const btnShopClose = byId<HTMLButtonElement>('btn-shop-close');

const heroCardLeon = byId<HTMLButtonElement>('hero-card-leon');
const heroCardAda = byId<HTMLButtonElement>('hero-card-ada');
const previewCanvasLeon = byId<HTMLCanvasElement>('preview-canvas-leon');
const previewCanvasAda = byId<HTMLCanvasElement>('preview-canvas-ada');
const difficultyButtonsWrap = byId<HTMLDivElement>('difficulty-buttons');
const difficultyDesc = byId<HTMLSpanElement>('difficulty-desc');
const difficultyButtons = Array.from(
  difficultyButtonsWrap.querySelectorAll<HTMLButtonElement>('.diff-btn')
);

const totalProgressLabel = byId<HTMLSpanElement>('total-progress-label');
const levelGrid = byId<HTMLDivElement>('level-grid');
const pauseLevelName = byId<HTMLParagraphElement>('pause-level-name');
const victoryBadge = byId<HTMLDivElement>('victory-badge');
const victoryTitle = byId<HTMLHeadingElement>('victory-title');
const victoryStats = byId<HTMLDivElement>('victory-stats');
const btnNextLevel = byId<HTMLButtonElement>('btn-next-level');
const gameoverReason = byId<HTMLParagraphElement>('gameover-reason');
const gameoverStats = byId<HTMLDivElement>('gameover-stats');

const controlsList = byId<HTMLDivElement>('controls-list');
const controlsStatus = byId<HTMLParagraphElement>('controls-status');
const btnControlsReset = byId<HTMLButtonElement>('btn-controls-reset');
const btnPause = byId<HTMLButtonElement>('btn-pause');

const audio = new RetroAudio();
let crtEnabled = true;
let freePlayUnlocked = false;

function hideAllScreens(): void {
  screenTitle.hidden = true;
  screenLevels.hidden = true;
  screenControls.hidden = true;
  screenShop.hidden = true;
  screenPause.hidden = true;
  screenVictory.hidden = true;
  screenGameover.hidden = true;
}

function updateTitleProgress(): void {
  const save = loadSaveData();
  let completedCount = 0;
  let totalShards = 0;
  for (const lvl of LEVELS) {
    const prog = save.levels[lvl.id];
    if (prog?.completed) completedCount++;
    if (prog?.shards) {
      totalShards += prog.shards.filter(Boolean).length;
    }
  }
  totalProgressLabel.textContent = `Progres: ${completedCount}/${LEVELS.length} Stage • ★ ${totalShards}/${LEVELS.length * 3} Medallion`;
}

function buildStatsHtml(world: WorldState): string {
  const shards = world.shardsCollected.filter(Boolean).length;
  const diffCfg = DIFFICULTY_CONFIG[world.difficulty];
  return `
    <div class="stat-row"><span>Agen</span><strong>${world.player.hero === 'leon' ? 'Leon S. Kennedy' : 'Ada Wong'}</strong></div>
    <div class="stat-row"><span>Kesulitan</span><strong style="color: ${diffCfg.color}">${diffCfg.label}</strong></div>
    <div class="stat-row"><span>Skor Akhir</span><strong>${world.score}</strong></div>
    <div class="stat-row"><span>Waktu</span><strong>${world.elapsedTime.toFixed(1)}s</strong></div>
    <div class="stat-row"><span>Pesetas</span><strong>${world.coins} / ${world.totalCoins}</strong></div>
    <div class="stat-row"><span>Blue Medallions</span><strong>★ ${shards} / 3</strong></div>
    <div class="stat-row"><span>Ganado Dikalahkan</span><strong>${world.enemiesDefeated}</strong></div>
  `;
}

function toggleMute(): void {
  const next = !audio.isMuted();
  audio.setMuted(next);
}

function toggleCrt(): void {
  crtEnabled = !crtEnabled;
  crtOverlay.classList.toggle('off', !crtEnabled);
}

async function bootstrap(): Promise<void> {
  const assets = await loadGameAssets();
  const leonCtx = previewCanvasLeon.getContext('2d');
  const adaCtx = previewCanvasAda.getContext('2d');
  const merchantCtx = previewCanvasMerchant.getContext('2d');
  let merchantAnimKey: keyof typeof CHARACTER_FRAMES.merchant = 'openCoat';

  const SHOP_OFFER_ICONS: Partial<Record<string, string>> = {
    shotgun: './assets/weapon_shotgun_idle.png',
    mg: './assets/weapon_cqbr_idle.png',
    rpg: './assets/weapon_rpg_idle.png',
    ammo_shotgun: './assets/pickup_shotgun_ammo.png',
    ammo_mg: './assets/pickup_cqbr_ammo.png',
    ammo_rpg: './assets/projectile_rpg_rocket.png',
  };

  function renderShopUi(world: WorldState, quoteOverride?: string): void {
    const p = world.player;
    shopWalletLabel.textContent = `${world.pesetas} PTAS`;
    if (quoteOverride) {
      shopQuote.textContent = quoteOverride;
    }

    shopPlayerStats.innerHTML = `
      <div><span>HP</span><strong>${p.hp}/${p.maxHp}</strong></div>
      <div><span>Firepower</span><strong>+${p.damageBonus} DMG</strong></div>
      <div><span>Agility</span><strong>${Math.round(p.agilityMult * 100)}%</strong></div>
      <div><span>Medkit</span><strong>${p.medkits}/${p.maxMedkits}</strong></div>
      <div><span>Granat</span><strong>${p.grenades}</strong></div>
      <div><span>Shotgun</span><strong>${p.ammo.shotgun}</strong></div>
      <div><span>CQBR AR</span><strong>${p.ammo.mg}</strong></div>
      <div><span>RPG-7</span><strong>${p.ammo.rpg}</strong></div>
    `;

    shopCatalog.innerHTML = '';
    for (const offer of getShopOffers(world)) {
      const card = document.createElement('div');
      card.className = `shop-offer-card${offer.category === 'upgrade' ? ' upgrade' : ''}`;
      const iconSrc = SHOP_OFFER_ICONS[offer.id];
      card.innerHTML = `
        <div class="shop-offer-head">
          <strong>${offer.name}</strong>
          ${offer.levelText ? `<span class="shop-offer-tag">${offer.levelText}</span>` : ''}
        </div>
        ${iconSrc ? `<div class="shop-offer-icon"><img src="${iconSrc}" alt="${offer.name}" /></div>` : ''}
        <p class="shop-offer-desc">${offer.desc}</p>
        <div class="shop-offer-foot">
          <span class="shop-price">${offer.maxed ? 'MAX' : `${offer.price} PTAS`}</span>
        </div>
      `;

      const buyBtn = document.createElement('button');
      buyBtn.type = 'button';
      buyBtn.className = offer.canBuy ? 'pixel-btn primary small' : 'pixel-btn small';
      buyBtn.textContent = offer.maxed ? '✓ MAKS' : offer.canBuy ? 'BELI' : 'KURANG PTAS';
      buyBtn.disabled = !offer.canBuy || offer.maxed;
      buyBtn.addEventListener('click', () => {
        const res = engine.buyShopOffer(offer.id);
        if (res.ok) {
          merchantAnimKey = offer.merchantAnim;
          renderShopUi(engine.getWorld(), res.message);
        } else {
          renderShopUi(engine.getWorld(), res.message);
        }
      });

      card.querySelector('.shop-offer-foot')?.appendChild(buyBtn);
      shopCatalog.appendChild(card);
    }
  }

  let controlsReturnScreen: 'title' | 'pause' = 'title';
  let rebindTarget: ControlAction | null = null;
  let cursorIdleTimer: number | null = null;

  function syncCursorVisibility(mode: EngineMode): void {
    const isFull = Boolean(document.fullscreenElement);
    gameShell.classList.toggle('is-fullscreen', isFull);
    if (cursorIdleTimer !== null) {
      window.clearTimeout(cursorIdleTimer);
      cursorIdleTimer = null;
    }
    if (!isFull) {
      gameShell.classList.remove('hide-cursor');
      return;
    }
    if (mode === 'playing') {
      gameShell.classList.add('hide-cursor');
    } else {
      gameShell.classList.add('hide-cursor');
    }
  }

  function cancelRebind(): void {
    rebindTarget = null;
    engine.setCapturingKey(false);
    controlsStatus.innerHTML =
      'Klik tombol <strong>UBAH</strong> lalu tekan tombol keyboard baru yang diinginkan.';
  }

  function renderControlsList(): void {
    const bindings = engine.getBindings();
    controlsList.innerHTML = '';

    for (const meta of CONTROL_ACTIONS) {
      const row = document.createElement('div');
      row.className = 'rebind-row';

      const info = document.createElement('div');
      info.className = 'rebind-info';
      info.innerHTML = `<strong>${meta.label}</strong><span>${meta.desc}</span>`;

      const right = document.createElement('div');
      right.className = 'rebind-controls';

      const cap = document.createElement('span');
      cap.className = 'keycap';
      cap.textContent = formatActionKeys(bindings[meta.action]);

      const btn = document.createElement('button');
      btn.type = 'button';
      const isWaiting = rebindTarget === meta.action;
      btn.className = `pixel-btn small rebind-btn${isWaiting ? ' waiting' : ''}`;
      btn.textContent = isWaiting ? 'TEKAN TOMBOL...' : 'UBAH';
      btn.addEventListener('click', () => {
        rebindTarget = meta.action;
        engine.setCapturingKey(true);
        controlsStatus.textContent = `Menunggu input untuk "${meta.label}"... (tekan Esc untuk batal)`;
        renderControlsList();
      });

      right.appendChild(cap);
      right.appendChild(btn);
      row.appendChild(info);
      row.appendChild(right);
      controlsList.appendChild(row);
    }
  }

  const engine = new GameEngine(canvas, assets, audio, {
    onModeChange(mode, world) {
      cancelRebind();
      hideAllScreens();
      btnPause.hidden = mode !== 'playing';
      syncCursorVisibility(mode);

      if (mode === 'title') {
        updateTitleProgress();
        screenTitle.hidden = false;
      } else if (mode === 'shop') {
        merchantAnimKey = 'openCoat';
        renderShopUi(world, `"Got somethin' that might interest ya, stranger!"`);
        screenShop.hidden = false;
      } else if (mode === 'paused') {
        pauseLevelName.textContent = `${world.level.def.name} — ${world.level.def.subtitle}`;
        screenPause.hidden = false;
      } else if (mode === 'victory') {
        const isFinalStage = world.level.def.index >= LEVELS.length - 1;
        victoryBadge.textContent = isFinalStage
          ? 'MISI PEDESAAN SELESAI!'
          : 'STAGE SELESAI!';
        victoryTitle.textContent = world.level.def.name.toUpperCase();
        victoryStats.innerHTML = buildStatsHtml(world);
        btnNextLevel.hidden = isFinalStage;
        screenVictory.hidden = false;
      } else if (mode === 'gameover') {
        gameoverReason.textContent = world.gameOverReason || 'Nyawa habis. Coba lagi!';
        gameoverStats.innerHTML = buildStatsHtml(world);
        screenGameover.hidden = false;
      }
    },
    onTitleTick(elapsedTime) {
      if (leonCtx) renderHeroPreview(leonCtx, assets, 'leon', elapsedTime);
      if (adaCtx) renderHeroPreview(adaCtx, assets, 'ada', elapsedTime + 0.5);
    },
    onShopTick(elapsedTime) {
      if (merchantCtx) renderMerchantPreview(merchantCtx, assets, merchantAnimKey, elapsedTime);
    },
  });

  function selectHero(hero: HeroId): void {
    engine.setSelectedHero(hero);
    heroCardLeon.classList.toggle('selected', hero === 'leon');
    heroCardAda.classList.toggle('selected', hero === 'ada');
  }

  function renderLevelCards(): void {
    const save = loadSaveData();
    levelGrid.innerHTML = '';

    for (const lvl of LEVELS) {
      const prog = save.levels[lvl.id];
      const unlocked = freePlayUnlocked || lvl.index <= save.unlockedLevelIndex;
      const shardCount = prog ? prog.shards.filter(Boolean).length : 0;
      const bestTimeStr = prog && prog.bestTime !== null ? `${prog.bestTime.toFixed(1)}s` : '--';

      const card = document.createElement('div');
      card.className = `level-card${unlocked ? '' : ' locked'}`;
      card.innerHTML = `
        <div class="level-card-head">
          <span class="level-card-title">${lvl.index + 1}. ${lvl.name}</span>
          <span class="level-card-biome">${lvl.biome}</span>
        </div>
        <p class="level-card-desc">${lvl.description}</p>
        <div class="level-card-meta">
          <span>★ ${shardCount}/3 Medallion</span>
          <span>Skor: ${prog?.bestScore ?? 0}</span>
          <span>Waktu: ${bestTimeStr}</span>
        </div>
      `;

      const playBtn = document.createElement('button');
      playBtn.type = 'button';
      playBtn.className = unlocked ? 'pixel-btn primary small' : 'pixel-btn small';
      playBtn.textContent = unlocked ? '▶ MAINKAN' : '🔒 TERKUNCI';
      playBtn.disabled = !unlocked;
      playBtn.addEventListener('click', () => {
        engine.startLevel(lvl.index);
      });

      card.appendChild(playBtn);
      levelGrid.appendChild(card);
    }

    if (!freePlayUnlocked && save.unlockedLevelIndex < LEVELS.length - 1) {
      const unlockAllBtn = document.createElement('button');
      unlockAllBtn.type = 'button';
      unlockAllBtn.className = 'pixel-btn small';
      unlockAllBtn.textContent = '🔓 Buka Semua Stage (Mode Bebas)';
      unlockAllBtn.addEventListener('click', () => {
        freePlayUnlocked = true;
        renderLevelCards();
      });
      levelGrid.appendChild(unlockAllBtn);
    }
  }

  function syncDifficultyUi(difficulty: DifficultyId): void {
    const cfg = DIFFICULTY_CONFIG[difficulty];
    for (const btn of difficultyButtons) {
      btn.classList.toggle('selected', btn.dataset.difficulty === difficulty);
    }
    difficultyDesc.textContent = cfg.desc;
    difficultyDesc.style.color = cfg.color;
  }

  function selectDifficulty(difficulty: DifficultyId): void {
    engine.setSelectedDifficulty(difficulty);
    syncDifficultyUi(difficulty);
  }

  heroCardLeon.addEventListener('click', () => selectHero('leon'));
  heroCardAda.addEventListener('click', () => selectHero('ada'));

  for (const btn of difficultyButtons) {
    btn.addEventListener('click', () => {
      const diff = btn.dataset.difficulty as DifficultyId | undefined;
      if (diff && diff in DIFFICULTY_CONFIG) {
        selectDifficulty(diff);
      }
    });
  }
  syncDifficultyUi(engine.getSelectedDifficulty());

  byId<HTMLButtonElement>('btn-play').addEventListener('click', () => {
    const save = loadSaveData();
    engine.startLevel(save.unlockedLevelIndex);
  });

  byId<HTMLButtonElement>('btn-levels').addEventListener('click', () => {
    hideAllScreens();
    renderLevelCards();
    screenLevels.hidden = false;
  });

  byId<HTMLButtonElement>('btn-controls').addEventListener('click', () => {
    controlsReturnScreen = 'title';
    hideAllScreens();
    renderControlsList();
    screenControls.hidden = false;
  });

  byId<HTMLButtonElement>('btn-levels-back').addEventListener('click', () => {
    hideAllScreens();
    updateTitleProgress();
    screenTitle.hidden = false;
  });

  byId<HTMLButtonElement>('btn-controls-back').addEventListener('click', () => {
    cancelRebind();
    hideAllScreens();
    if (controlsReturnScreen === 'pause') {
      screenPause.hidden = false;
    } else {
      screenTitle.hidden = false;
    }
  });

  btnControlsReset.addEventListener('click', () => {
    cancelRebind();
    engine.setBindings(resetKeyBindings());
    controlsStatus.textContent = 'Semua kontrol dikembalikan ke default!';
    renderControlsList();
  });

  document.addEventListener('fullscreenchange', () => {
    engine.syncDisplayResolution();
    syncCursorVisibility(engine.getMode());
  });
  window.addEventListener('mousemove', () => {
    if (!document.fullscreenElement) return;
    if (engine.getMode() === 'playing') {
      gameShell.classList.add('hide-cursor');
      return;
    }
    // In fullscreen menus, show cursor briefly on move then hide again
    gameShell.classList.remove('hide-cursor');
    if (cursorIdleTimer !== null) window.clearTimeout(cursorIdleTimer);
    cursorIdleTimer = window.setTimeout(() => {
      if (document.fullscreenElement) {
        gameShell.classList.add('hide-cursor');
      }
    }, 1200);
  });
  btnShopClose.addEventListener('click', () => engine.closeShop());
  btnPause.addEventListener('click', () => engine.togglePause());

  byId<HTMLButtonElement>('btn-resume').addEventListener('click', () => engine.togglePause());
  byId<HTMLButtonElement>('btn-pause-controls').addEventListener('click', () => {
    controlsReturnScreen = 'pause';
    hideAllScreens();
    renderControlsList();
    screenControls.hidden = false;
  });
  byId<HTMLButtonElement>('btn-pause-cp').addEventListener('click', () => engine.restartFromCheckpoint());
  byId<HTMLButtonElement>('btn-pause-restart').addEventListener('click', () => engine.restartCurrentLevel());
  byId<HTMLButtonElement>('btn-pause-levels').addEventListener('click', () => {
    engine.returnToTitle();
    hideAllScreens();
    renderLevelCards();
    screenLevels.hidden = false;
  });
  byId<HTMLButtonElement>('btn-pause-title').addEventListener('click', () => engine.returnToTitle());

  btnNextLevel.addEventListener('click', () => {
    engine.advanceToNextLevel();
  });
  byId<HTMLButtonElement>('btn-victory-replay').addEventListener('click', () => {
    engine.startLevel(engine.getWorld().level.def.index);
  });
  byId<HTMLButtonElement>('btn-victory-title').addEventListener('click', () => engine.returnToTitle());

  byId<HTMLButtonElement>('btn-retry-level').addEventListener('click', () => engine.restartCurrentLevel());
  byId<HTMLButtonElement>('btn-gameover-levels').addEventListener('click', () => {
    engine.returnToTitle();
    hideAllScreens();
    renderLevelCards();
    screenLevels.hidden = false;
  });
  byId<HTMLButtonElement>('btn-gameover-title').addEventListener('click', () => engine.returnToTitle());

  window.addEventListener(
    'keydown',
    (e) => {
      if (rebindTarget !== null) {
        e.preventDefault();
        e.stopPropagation();
        if (e.repeat) return;
        if (e.code === 'Escape') {
          cancelRebind();
          renderControlsList();
          return;
        }
        const updated = rebindAction(engine.getBindings(), rebindTarget, e.code);
        engine.setBindings(updated);
        const doneAction = rebindTarget;
        cancelRebind();
        controlsStatus.textContent = `Tombol untuk "${doneAction}" berhasil diubah!`;
        renderControlsList();
        return;
      }

      if (e.repeat) return;
      if (e.code === 'KeyM' && engine.getMode() !== 'playing') {
        toggleMute();
      } else if (e.code === 'KeyC' && engine.getMode() !== 'playing') {
        toggleCrt();
      }
    },
    true
  );

  updateTitleProgress();
  engine.start();
}

void bootstrap();
