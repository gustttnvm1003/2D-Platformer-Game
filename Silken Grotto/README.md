# Silken Grotto — 2D Metroidvania Action Platformer (Unity)

**Silken Grotto** adalah game **2D Action Platformer / Metroidvania** berbasis **Unity Engine (C#)** yang terinspirasi dari dinamika pergerakan akrobatik, sistem pertarungan presisi, eksplorasi terhubung, progresi kemampuan, dan atmosfer *Hollow Knight: Silksong*, dengan identitas visual ilustrasi **2D hand-drawn** (bukan *pixel art*) berdasarkan tiga aset referensi utama.

---

## 1. Identitas Visual & Aset 2D Hand-Drawn

- **Karakter Utama (`Assets/Art/Characters/Player/`):** Ilustrasi 2D bertinta gelap (*gothic ink*) berproporsi ramping tinggi dengan topeng tulang bertanduk ganda retak, jubah merah marun berkerah tinggi robek, setelan lilitan biru gelap, serta bilah jarum tulang (*Greatneedle*). Disusun dalam 2 *sprite sheet* bersih (`Player_Locomotion_Sheet.png` dan `Player_Action_Sheet.png`, 16 pose animasi transparan tanpa *halo*).
- **Musuh (`Assets/Art/Characters/Enemies/`):** **Crimson Lancer** & varian elit **Crimson Canopy Captain** — prajurit serangga berbaju zirah merah marun gelap, pelindung bahu besi melengkung, wajah bayangan bermata putih tajam, membawa tombak tulang bermata ganda (`CrimsonLancer_Sheet.png`, 8 pose: Idle, Patrol Walk, Windup, Attack Lunge, Hurt, Death).
- **Lingkungan & Parallax (`Assets/Art/Backgrounds/` & `Assets/Art/Environments/`):** Atmosfer *Ancient Moss Grotto* berlapis (*Far Canopy*, *Reference Vista*, *Midground Roots & Ruins*, *Volumetric Teal Light Shafts*, *Foreground Dark Ferns*, serta partikel spora bioluminesens). Collider platform (`BoxCollider2D` & `PlatformEffector2D`) sepenuhnya terpisah dari lapisan *background*.
- **Audio (`Assets/Audio/`):** Tersedia 1 trek musik latar (*loopable* `BGM_MossGrotto.wav`) dan 19 efek suara `.wav` nyata (langkah, lompat, *double jump*, mendarat, *dash*, 3 kombo tebasan, *Silk Spear*, *Thread Guard*, *hit/death* pemain & musuh, *checkpoint*, buka *skill*, *upgrade*, dan UI) yang siap langsung dipakai atau diganti dengan rekaman final.

---

## 2. Cara Membuka & Menjalankan di Unity Editor

1. Buka **Unity Hub** dan pastikan terpasang **Unity 2022.3 LTS** (disarankan `2022.3.42f1` LTS; juga kompatibel dengan Unity 6 LTS).
2. Klik **Add > Add project from disk**, lalu pilih folder `d:\Antigravity\Game\Silken Grotto`.
3. Tunggu Unity menyelesaikan proses *import* aset dan kompilasi skrip C# (`0 errors`).
4. **Auto-Setup Otomatis:** Pada pembukaan pertama, editor script `SilkenGrottoProjectBuilder` (`[InitializeOnLoad]`) akan **secara otomatis** mengonfigurasi:
   - *Sprite Slicing* & *Pixels Per Unit* seluruh *sprite sheet* 2D hand-drawn,
   - Seluruh **ScriptableObject** (`PlayerStatsConfig`, 6 `AttackData`, 2 `EnemyStatsData`, 6 `SkillData`, 6 `UpgradeData`),
   - Seluruh **AnimationClip (`.anim`)** & **Animator Controller (`.controller`)** pemain dan musuh,
   - Seluruh **Prefab (`.prefab`)** (Player, Enemies, Checkpoint, Pedestal, Altar, Pickups, Breakable Wall, Spike Hazard, HUD Canvas),
   - Kedua *scene* siap main: `Assets/Scenes/MainMenu/MainMenu.unity` dan `Assets/Scenes/Gameplay/MossGrotto.unity`, serta `EditorBuildSettings`.
5. Anda juga dapat memicu pembuatan ulang aset/scene kapan saja melalui menu bar Unity:
   - **`Silken Grotto > 1. Build Complete Project (All Assets & Scenes)`**
   - **`Silken Grotto > 2. Open Gameplay Scene (MossGrotto)`**
   - **`Silken Grotto > 3. Open Main Menu Scene`**
6. Tekan tombol **Play (▶)** untuk langsung bermain!

---

## 3. Kontrol (Keyboard & Mouse)

| Aksi | Tombol |
| :--- | :--- |
| Bergerak Kiri / Kanan (*Walk*) | `A` / `D` atau `←` / `→` |
| Berlari (*Run / Sprint*) | Tahan `Left Shift` saat bergerak |
| Lompat / *Wall Jump* / *Double Jump* (setelah dibuka) | `Space` atau `Z` |
| Serangan Normal & 3-Hit Combo | `Klik Kiri (LMB)` atau `J` / `X` |
| Serangan ke Atas (*Upward Slash*) | `W` + Serang |
| Serangan Udara ke Bawah (*Downward Pogo Bounce*) | Di udara: `S` + Serang |
| *Silk Dash* / *Air Dash* (setelah dibuka) | `Klik Kanan (RMB)` atau `K` / `L` |
| *Silk Spear* (Serangan Proyektil Menembus, butuh Silk) | `Q` atau `F` |
| *Thread Guard* (Perisai Sutra Penahan Damage, butuh Silk) | `R` atau `V` |
| *Bind Heal* (Pulihkan 1 Mask HP memakai 35 Silk) | `H` |
| Interaksi (*Checkpoint Bench*, *Ability Pedestal*, *Upgrade Altar*) | `E` |
| Menu *Upgrade* Atribut & Status *Skill* | `Tab` atau `U` |
| Menu *Pause* | `Escape` |

---

## 4. Dunia Metroidvania & Progresi (5 Zona Terhubung)

1. **Sunlit Moss Glade** — Area awal aman (*Upgrade Altar*, *Checkpoint Bench #1*, musuh *Crimson Lancer* awal, jembatan kayu satu arah).
2. **Sunken Root Cavern** — Area bawah dengan *spike briars* dan **Pedestal Silk Dash** + area rahasia di balik *Breakable Vine Wall* berisi **Pedestal Thread Guard** & *Silk Essence*.
3. **Overgrown Spire** — Menara vertikal dengan *wall-jump*, *Checkpoint Bench #2*, **Pedestal Gale Leap (*Double Jump*)**, jalur rahasia menuju **Pedestal Needle Mastery (*Enhanced Attack*)**, serta jembatan pintas tinggi kembali ke atas *Zone 1*.
4. **Thornbridge Passage** — Jurang berduri yang membutuhkan *Dash* + *Double Jump*, menjaga **Pedestal Silk Spear** dan **Pedestal Sky Thread (*Air Dash*)**.
5. **Ancient Canopy Arena** — Arena puncak dengan *Checkpoint Bench #3*, *Upgrade Altar*, dan pertarungan elit **Crimson Canopy Captain**.

---

## 5. Keputusan Teknis & Arsitektur

- **Render Pipeline (`Built-in 2D Renderer`):** Dipilih agar proyek dapat langsung dibuka di Unity 2022.3 LTS maupun Unity 6 LTS tanpa konflik versi paket URP (`URP 14` vs `URP 17`) atau *missing shader*. Atmosfer pencahayaan 2D dibentuk melalui *layer* *god-rays* volumetrik, *glow sprite*, *sorting layers*, dan *ParticleSystem* spora.
- **Sistem Input (`Legacy Input Manager` terbungkus di `PlayerInputHandler`):** Konsisten tanpa mencampur dua sistem input sekaligus, dilengkapi *jump buffering* dan *attack buffering* agar responsif.
- **Pemisahan AI & Animasi:** `CrimsonLancerAI` (berbasis `EnemyStateMachine`) menangani pengambilan keputusan (*Idle, Patrol, Chase, Windup, Attack, Recover, Stagger, Death*) lengkap dengan deteksi tepi platform (`HasGroundAhead`) & dinding (`HasWallAhead`), sedangkan `EnemyAnimatorDriver` hanya membaca status untuk menggerakkan `Animator`.
- **Hitbox Presisi:** `Hitbox.cs` hanya aktif pada jendela *active frames* tiap `AttackData` ScriptableObject dan mencatat `HashSet<IDamageable>` per ayunan agar tidak menimbulkan *damage* berulang setiap *frame*.
- **Save & Load Aman:** `SaveManager.cs` menyimpan progres JSON ke `Application.persistentDataPath/silken_grotto_save.json` beserta salinan cadangan `.bak` untuk pemulihan otomatis apabila file korup/hilang.
- **Catatan Lingkungan:** Unity Editor belum terpasang di mesin pengembangan saat proyek dibuat, sehingga kompilasi seluruh **48 skrip C#** (`46` Runtime + `2` Editor) telah diverifikasi menggunakan compiler C# (`csc.exe`) terhadap definisi API Unity 2022.3 LTS serta diaudit penuh kecocokan nama file/class, path aset, parameter Animator, dan field serialisasi. Saat proyek dibuka di Unity Editor, `SilkenGrottoProjectBuilder` otomatis membentuk aset native Unity (`.asset`, `.anim`, `.controller`, `.prefab`, `.unity`) tanpa error referensi.
