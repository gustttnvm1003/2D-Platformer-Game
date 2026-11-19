import { saveWalletAndUpgrades } from './storage';
import { WorldState } from './types';
import { grantWeaponAmmo } from './world';

export type ShopOfferId =
  | 'medkit'
  | 'grenades'
  | 'shotgun'
  | 'mg'
  | 'rpg'
  | 'ammo_shotgun'
  | 'ammo_mg'
  | 'ammo_rpg'
  | 'up_hp'
  | 'up_dmg'
  | 'up_agi'
  | 'up_pouch';

export interface ShopOfferView {
  id: ShopOfferId;
  category: 'item' | 'upgrade';
  name: string;
  desc: string;
  price: number;
  maxed: boolean;
  canBuy: boolean;
  levelText?: string;
  merchantAnim: 'herb' | 'spray' | 'openCoat' | 'gold' | 'chuckle';
}

const HP_PRICES = [200, 350, 500];
const DMG_PRICES = [220, 400, 600];
const AGI_PRICES = [180, 320];
const POUCH_PRICES = [200, 350];

export function getShopOffers(world: WorldState): ShopOfferView[] {
  const p = world.player;
  const u = world.upgrades;
  const ptas = world.pesetas;

  const medkitFull = p.medkits >= p.maxMedkits && p.hp >= p.maxHp;

  const offers: ShopOfferView[] = [
    {
      id: 'medkit',
      category: 'item',
      name: 'First-Aid Medkit',
      desc: p.medkits < p.maxMedkits ? '+1 Medkit ke tas (Tekan E/H)' : 'Pulihkan +2 HP langsung',
      price: 100,
      maxed: medkitFull,
      canBuy: !medkitFull && ptas >= 100,
      merchantAnim: 'spray',
    },
    {
      id: 'grenades',
      category: 'item',
      name: 'Frag Grenade ×3',
      desc: 'Tambah 3 granat ledakan area (Tekan C/G)',
      price: 120,
      maxed: false,
      canBuy: ptas >= 120,
      merchantAnim: 'herb',
    },
    {
      id: 'shotgun',
      category: 'item',
      name: 'Pump-Action Shotgun [2]',
      desc: 'Senjata close-range 6 pellet + knockback tinggi (+12 Shells)',
      price: 180,
      maxed: false,
      canBuy: ptas >= 180,
      merchantAnim: 'openCoat',
    },
    {
      id: 'mg',
      category: 'item',
      name: 'CQBR Assault Rifle [3]',
      desc: 'Full-auto rapid fire magasin 30 peluru (+60 Rifle Ammo)',
      price: 220,
      maxed: false,
      canBuy: ptas >= 220,
      merchantAnim: 'openCoat',
    },
    {
      id: 'rpg',
      category: 'item',
      name: 'RPG-7 Rocket Launcher [4]',
      desc: 'Peluncur roket ledakan area besar (+5 Warheads)',
      price: 320,
      maxed: false,
      canBuy: ptas >= 320,
      merchantAnim: 'openCoat',
    },
    {
      id: 'ammo_shotgun',
      category: 'item',
      name: 'Shotgun Shells ×8',
      desc: 'Paket +8 peluru merah untuk Pump-Action Shotgun',
      price: 75,
      maxed: false,
      canBuy: ptas >= 75,
      merchantAnim: 'herb',
    },
    {
      id: 'ammo_mg',
      category: 'item',
      name: 'Rifle Ammo 5.56 ×30',
      desc: 'Paket +30 peluru penuh 1 magasin CQBR AR',
      price: 80,
      maxed: false,
      canBuy: ptas >= 80,
      merchantAnim: 'herb',
    },
    {
      id: 'ammo_rpg',
      category: 'item',
      name: 'RPG-7 Warheads ×2',
      desc: 'Paket +2 roket peledak area untuk RPG-7',
      price: 140,
      maxed: false,
      canBuy: ptas >= 140,
      merchantAnim: 'spray',
    },
  ];

  const hpMaxed = u.maxHpLevel >= HP_PRICES.length;
  const hpPrice = hpMaxed ? 0 : HP_PRICES[u.maxHpLevel];
  offers.push({
    id: 'up_hp',
    category: 'upgrade',
    name: 'Body Armor (+Max HP)',
    desc: '+1 Max HP & langsung sembuh +1 HP (terbawa ke misi berikutnya)',
    price: hpPrice,
    maxed: hpMaxed,
    canBuy: !hpMaxed && ptas >= hpPrice,
    levelText: `Lv ${u.maxHpLevel}/${HP_PRICES.length}`,
    merchantAnim: 'chuckle',
  });

  const dmgMaxed = u.damageLevel >= DMG_PRICES.length;
  const dmgPrice = dmgMaxed ? 0 : DMG_PRICES[u.damageLevel];
  offers.push({
    id: 'up_dmg',
    category: 'upgrade',
    name: 'Firepower (+Damage)',
    desc: '+1 Damage semua senjata & melee (terbawa ke misi berikutnya)',
    price: dmgPrice,
    maxed: dmgMaxed,
    canBuy: !dmgMaxed && ptas >= dmgPrice,
    levelText: `Lv ${u.damageLevel}/${DMG_PRICES.length}`,
    merchantAnim: 'openCoat',
  });

  const agiMaxed = u.agilityLevel >= AGI_PRICES.length;
  const agiPrice = agiMaxed ? 0 : AGI_PRICES[u.agilityLevel];
  offers.push({
    id: 'up_agi',
    category: 'upgrade',
    name: 'Tactical Boots (+Speed)',
    desc: '+10% kecepatan lari & lompat (terbawa ke misi berikutnya)',
    price: agiPrice,
    maxed: agiMaxed,
    canBuy: !agiMaxed && ptas >= agiPrice,
    levelText: `Lv ${u.agilityLevel}/${AGI_PRICES.length}`,
    merchantAnim: 'gold',
  });

  const pouchMaxed = u.pouchLevel >= POUCH_PRICES.length;
  const pouchPrice = pouchMaxed ? 0 : POUCH_PRICES[u.pouchLevel];
  offers.push({
    id: 'up_pouch',
    category: 'upgrade',
    name: 'Supply Pouch (+Kapasitas)',
    desc: '+1 Max Medkit & +2 Granat (terbawa ke misi berikutnya)',
    price: pouchPrice,
    maxed: pouchMaxed,
    canBuy: !pouchMaxed && ptas >= pouchPrice,
    levelText: `Lv ${u.pouchLevel}/${POUCH_PRICES.length}`,
    merchantAnim: 'gold',
  });

  return offers;
}

export function purchaseShopOffer(
  world: WorldState,
  offerId: ShopOfferId
): { ok: boolean; message: string } {
  const offer = getShopOffers(world).find((o) => o.id === offerId);
  if (!offer || offer.maxed || !offer.canBuy) {
    return { ok: false, message: 'Pesetas tidak cukup atau sudah maksimal!' };
  }

  const p = world.player;
  const u = world.upgrades;
  world.pesetas -= offer.price;

  switch (offerId) {
    case 'medkit':
      if (p.medkits < p.maxMedkits) {
        p.medkits += 1;
      } else {
        p.hp = Math.min(p.maxHp, p.hp + 2);
      }
      break;
    case 'grenades':
      p.grenades += 3;
      break;
    case 'shotgun':
      grantWeaponAmmo(p, 'shotgun', 12, true);
      break;
    case 'mg':
      grantWeaponAmmo(p, 'mg', 60, true);
      break;
    case 'rpg':
      grantWeaponAmmo(p, 'rpg', 5, true);
      break;
    case 'ammo_shotgun':
      grantWeaponAmmo(p, 'shotgun', 8, false, p.unlockedWeapons.shotgun);
      break;
    case 'ammo_mg':
      grantWeaponAmmo(p, 'mg', 30, false, p.unlockedWeapons.mg);
      break;
    case 'ammo_rpg':
      grantWeaponAmmo(p, 'rpg', 2, false, p.unlockedWeapons.rpg);
      break;
    case 'up_hp':
      u.maxHpLevel += 1;
      p.maxHp += 1;
      p.hp = Math.min(p.maxHp, p.hp + 1);
      break;
    case 'up_dmg':
      u.damageLevel += 1;
      p.damageBonus += 1;
      break;
    case 'up_agi':
      u.agilityLevel += 1;
      p.agilityMult = 1 + u.agilityLevel * 0.1;
      break;
    case 'up_pouch':
      u.pouchLevel += 1;
      p.maxMedkits += 1;
      p.medkits = Math.min(p.maxMedkits, p.medkits + 1);
      p.grenades += 2;
      break;
  }

  world.pendingSounds.push('merchant_buy');
  saveWalletAndUpgrades(world.pesetas, world.upgrades);
  return { ok: true, message: `"Heh heh heh... Thank you!" (${offer.name})` };
}
