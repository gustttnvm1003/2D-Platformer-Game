# 2D-Platformer-Game

Koleksi proyek **Game 2D Platformer** multi-platform (*Web HTML5 Canvas + TypeScript* dan *Unity Engine + C#*) yang menggabungkan desain gameplay klasik hingga modern, fisika platformer responsif, ilustrasi *pixel art* maupun *2D hand-drawn*, serta sistem kecerdasan buatan (*Game AI* & pengolahan aset visual).

Repositori ini dirancang sebagai **wadah multi-proyek (*monorepo showcase*)** yang memuat proyek-proyek game **2D Platformer** beserta mekanik, bioma, dan genre turunan yang terus dikembangkan.

---

## Daftar Game dalam Repositori

| No | Judul Game | Folder Proyek | Engine & Subgenre / Fitur Utama | Status |
| :---: | :--- | :--- | :--- | :---: |
| **1** | **RESIDENT SLUG: El Pueblo Village** | [`Resident Slug/`](./Resident%20Slug/README.md) | **TypeScript / Vite / HTML5 Canvas** — 2D *Action Platformer / Pixel Shooter* (*RE4 × Metal Slug*): 2 Hero (*Leon* & *Ada*), 4 Senjata + Granat, 4 Stage, AI Musuh FSM & Boss *Dr. Salvador*, *Merchant Shop*, & Audio Web Audio API | Selesai & Teruji |
| **2** | **Silken Grotto** | [`Silken Grotto/`](./Silken%20Grotto/README.md) | **Unity 2022.3 LTS / C#** — 2D *Metroidvania Action Platformer* bergaya ilustrasi *hand-drawn*: 8 Stage Terhubung (5 Tema: Hutan, Gunung Salju/Es Licin, Gua Gelap, Gurun Pasir, Istana Kerajaan), 3 *Combat Crest*, 6 *Silk Skills*, 35 Prefab Musuh AI FSM, Cuaca Dinamis, & *Save/Load JSON* | Selesai & Teruji |

---

## Struktur Repositori

```text
2D-Platformer-Game/
├── .gitignore
├── README.md                  # Dokumentasi utama repositori koleksi 2D-Platformer-Game
├── Resident Slug/             # Game #1: RESIDENT SLUG: El Pueblo Village (TypeScript / Vite)
│   ├── README.md
│   ├── index.html
│   ├── package.json
│   ├── public/assets/         # Sprite atlas karakter, musuh, latar desa & efek senjata
│   ├── src/                   # Kode sumber TypeScript (engine, physics, enemies FSM, renderer, audio)
│   └── tests/                 # Suite pengujian otomatis Vitest
└── Silken Grotto/             # Game #2: Silken Grotto (Unity 2022.3 LTS / C#)
    ├── README.md              # Dokumentasi lengkap, kontrol, bioma & cara menjalankan di Unity
    ├── .gitignore             # Aturan pengecualian cache Unity (Library, Logs, UserSettings)
    ├── Assets/                # 99 Skrip C#, 2 Scene, 8 StageConfig, 35 Prefab Musuh, Art 2D & 43 Audio
    ├── Packages/              # Manifest dependensi Unity
    └── ProjectSettings/       # Konfigurasi proyek Unity 2022.3 LTS
```

---

## Cara Menjalankan Game

### 1. RESIDENT SLUG (Web / Vite)
```bash
cd "Resident Slug"
npm install
npm run dev
```
Kemudian buka URL lokal Vite di browser desktop (bawaan `http://localhost:5173`). Detail lengkap tersedia di [README Resident Slug](./Resident%20Slug/README.md).

### 2. Silken Grotto (Unity Engine)
1. Buka **Unity Hub** (disarankan **Unity `2022.3` LTS**).
2. Klik **Add > Add project from disk**, lalu pilih folder [`Silken Grotto/`](./Silken%20Grotto/README.md).
3. Buka scene `Assets/Scenes/MainMenu/MainMenu.unity` atau `Assets/Scenes/Gameplay/MossGrotto.unity`, lalu tekan **Play (▶)**. Detail lengkap tersedia di [README Silken Grotto](./Silken%20Grotto/README.md).
