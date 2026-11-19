# RESIDENT SLUG: El Pueblo Village

**RESIDENT SLUG: El Pueblo Village** adalah gim **2D Action Platformer & Retro Pixel Shooter** berbasis web untuk **desktop** (*crossover* gaya *Resident Evil 4 × Metal Slug*), dibangun dengan `TypeScript`, `HTML5 Canvas`, `Web Audio API`, dan `Vite`.

---

## Fitur Utama

- **2 Karakter Hero dengan Statistik & Jurus Khas:**
  - **Leon S. Kennedy:** `5 HP`, kecepatan & lompatan seimbang (`1.0×`), `2` granat awal, *melee* **Roundhouse Kick**.
  - **Ada Wong:** `4 HP`, gerak & lompatan lebih lincah (`1.12×` speed / `1.08×` jump), `3` granat awal, *melee* **Corkscrew Kick**.
- **Sistem Fisika & Platformer 2D Responsif (`FIXED_DT = 1/60s`):**
  - Mendukung *crouch* (dengan pengecekan celah langit-langit), *short-hop*, *double jump*, *coyote time*, *jump buffer*, *wall slide / wall jump*, *dash*, serta **one-way platform drop-through** (`Down + Jump`).
  - Resolusi virtual `480×270` piksel dengan scaling *pixel-crisp* layar penuh dan kursor tersembunyi saat *fullscreen*.
- **4 Senjata + Granat & Animasi Penuh Per Senjata:**
  - **Slot `1` — Handgun:** Magasin `12`, amunisi tak terbatas.
  - **Slot `2` — Pump-Action Shotgun:** Magasin `8`, tembakan *cone spread* (`5–7` *pellet*) dengan *knockback* kuat & selongsong merah.
  - **Slot `3` — CQBR Assault Rifle:** Magasin `30`, *full-auto* beruntun dengan selongsong kuningan.
  - **Slot `4` — RPG-7 Rocket Launcher:** Magasin `1`, ledakan area & efek *backblast*.
  - Kalibrasi koordinat laras (`getPlayerMuzzle`) agar peluru dan percikan keluar tepat dari moncong senjata, animasi lempar granat bebas senjata, animasi gerak lengkap per senjata, serta animasi *AFK Idle* berjeda (`2.8` detik).
- **4 Stage Desa *El Pueblo* & Pertarungan Boss:**
  1. `village-outskirts` — *El Pueblo: Outskirts* (Gerbang Desa)
  2. `plaza-bonfire` — *La Plaza: Village Square* (Alun-Alun & Cerobong *Wall-Jump*)
  3. `watchtower-siege` — *Torre del Pueblo* (Menara Vertikal)
  4. `church-courtyard` — *La Iglesia: Dr. Salvador* (Arena Boss & Lonceng Gereja)
- **AI Musuh Berbasis *Finite State Machine* (FSM) & 4 Tingkat Kesulitan:**
  - Agen musuh (`ganado_fork`, `ganado_axe`, dan Boss `boss_salvador`) memiliki 8 status FSM (`patrol`, `chase`, `windup`, `attack`, `recover`, `hurt`, `dying`, `dead`) lengkap dengan sensor tepi & dinding.
  - **Boss Dr. Salvador** masuk fase *Enraged* saat HP $\le 50\%$, dan kapak lempar *Ganado* dapat ditangkis (*parry*) dengan serangan *Melee*.
  - Pilihan kesulitan: **Easy**, **Normal**, **Hardcore**, dan **Professional** yang menskalakan jumlah spawn, HP musuh & bos, kecepatan, serta hadiah *Pesetas*.
- **Ekonomi *Pesetas*, NPC *The Merchant* & Audio Prosedural:**
  - *The Merchant* (kebal serangan) menjual *Medkit*, *Grenades*, senjata baru, paket amunisi, serta *upgrade* permanen (*Body Armor*, *Firepower*, *Tactical Boots*, *Ammo Pouch*) yang disimpan ke `localStorage`.
  - Synthesizer musik tema untuk 4 bioma dan efek suara via `Web Audio API`.

---

## Kontrol Keyboard Default (Desktop)

Kontrol dapat diubah secara manual kapan saja lewat menu **Kontrol & Senjata** di dalam gim (atau di menu *Pause*):

| Aksi | Tombol Default |
| :--- | :--- |
| Gerak Kiri / Kanan | `ArrowLeft` / `A` • `ArrowRight` / `D` |
| Bidik Atas | `ArrowUp` / `W` |
| Runduk / Turun Platform (*Drop-Through* dengan Lompat) | `ArrowDown` / `S` |
| Lompat / *Double Jump* / *Wall Jump* | `Space` / `K` |
| Tembak Senjata | `J` / `Z` |
| Melee (*Roundhouse* / *Corkscrew Kick* & *Parry*) | `L` / `X` / `F` |
| Lempar *Frag Grenade* | `C` / `G` / `U` |
| *Dash* / Menghindar | `Shift` |
| Gunakan *Medkit* (+2 HP) | `E` / `H` |
| Reload Senjata | `R` |
| Ganti Senjata / Pilih Langsung Slot | `Q` / `Tab` / `1`–`4` |
| Buka Toko *Merchant* | `B` / `V` / `Enter` |
| Pause / Kembali ke Checkpoint | `Esc` / `P` • `Backspace` |

---

## Struktur Direktori

```text
Resident Slug/
├── index.html             # Antarmuka canvas, menu judul, pilihan hero/stage, toko & kontrol
├── package.json           # Dependensi & skrip dev, build, test
├── tsconfig.json
├── vite.config.ts
├── public/assets/         # Atlas sprite 128x128 (Leon, Ada, Ganado, Merchant, Dr. Salvador), senjata & latar bg_village.png
├── src/
│   ├── main.ts            # Entry point aplikasi
│   ├── style.css          # Styling antarmuka pixel desktop
│   └── game/
│       ├── types.ts       # Konstanta virtual canvas, Hero & Difficulty config, tipe state
│       ├── physics.ts     # Fisika 2D fixed-timestep, AABB, crouch, wall-jump & drop-through
│       ├── levels.ts      # Peta 4 stage El Pueblo & difficulty filtering
│       ├── enemies.ts     # AI musuh FSM, sensor medan & Boss Dr. Salvador
│       ├── world.ts       # Simulasi dunia, metadata senjata, getPlayerMuzzle & combat loop
│       ├── shop.ts        # Katalog & transaksi toko Merchant
│       ├── sprites.ts     # Indeks frame atlas karakter/musuh & sprite item prosedural
│       ├── renderer.ts    # Rendering kanvas, weapon locomotion, AFK idle, latar & HUD kompak
│       ├── controls.ts    # Manajemen keybinding & rebind manual
│       ├── audio.ts       # Web Audio API synthesizer BGM & SFX
│       ├── storage.ts     # Persistensi localStorage
│       └── engine.ts      # Game loop utama & manajemen screen overlay
└── tests/
    └── game.test.ts       # Suite pengujian otomatis Vitest (13 skenario uji)
```

---

## Cara Menjalankan, Menguji & Build

Pastikan **Node.js** sudah terpasang di perangkat Anda, lalu jalankan perintah berikut dari dalam direktori `Resident Slug`:

```bash
# 1. Pasang dependensi
npm install

# 2. Jalankan server pengembangan lokal
npm run dev

# 3. Jalankan suite unit & integrasi (Vitest)
npm test

# 4. Verifikasi TypeScript & bangun berkas produksi (dist/)
npm run build
```
