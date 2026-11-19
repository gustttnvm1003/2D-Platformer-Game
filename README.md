# 2D-Platformer-Game

Koleksi proyek **Game 2D Platformer** berbasis web modern (*TypeScript*, *HTML5 Canvas*, dan *Vite*) yang menggabungkan desain gameplay klasik *pixel art retro*, fisika platformer responsif, serta sistem kecerdasan buatan (*Game AI* & pengolahan aset visual).

Repositori ini dirancang sebagai **wadah multi-proyek (*monorepo showcase*)** — dimulai dari proyek pertama ***RESIDENT SLUG: El Pueblo Village***, dan ke depannya akan terus ditambahkan dengan berbagai game **2D Platformer** baru beserta mekanik dan genre turunan lainnya.

---

## Daftar Game dalam Repositori

| No | Judul Game | Folder Proyek | Subgenre & Fitur Utama | Status |
| :---: | :--- | :--- | :--- | :---: |
| **1** | **RESIDENT SLUG: El Pueblo Village** | [`Resident Slug/`](./Resident%20Slug/README.md) | 2D *Action Platformer / Pixel Shooter* (*RE4 × Metal Slug*): 2 Hero (*Leon* & *Ada*), 4 Senjata + Granat, 4 Stage, AI Musuh FSM & Boss *Dr. Salvador*, *Merchant Shop*, & Audio Web Audio API | Selesai & Teruji |
| **2** | *Game 2D Platformer Berikutnya* | *Segera Hadir* | Proyek game 2D platformer baru yang akan ditambahkan pada pembaruan mendatang | Dalam Perencanaan |

---

## Struktur Repositori

```text
2D-Platformer-Game/
├── .gitignore
├── README.md                  # Dokumentasi utama repositori koleksi 2D-Platformer-Game
└── Resident Slug/             # Game #1: RESIDENT SLUG: El Pueblo Village
    ├── README.md              # Dokumentasi lengkap, kontrol, arsitektur & cara menjalankan game
    ├── index.html
    ├── package.json
    ├── tsconfig.json
    ├── vite.config.ts
    ├── public/assets/         # Sprite atlas karakter, musuh, latar desa & efek senjata
    ├── src/                   # Kode sumber TypeScript (engine, physics, enemies FSM, renderer, audio)
    └── tests/                 # Suite pengujian otomatis Vitest
```

---

## Cara Menjalankan Game Saat Ini

Masuk ke folder game yang ingin dimainkan (saat ini [`Resident Slug`](./Resident%20Slug/README.md)):

```bash
cd "Resident Slug"
npm install
npm run dev
```

Kemudian buka URL lokal yang ditampilkan Vite di browser desktop (bawaan `http://localhost:5173`). Untuk detail kontrol, daftar fitur, pengujian (`npm test`), dan kompilasi produksi (`npm run build`), silakan baca [README Resident Slug](./Resident%20Slug/README.md).
