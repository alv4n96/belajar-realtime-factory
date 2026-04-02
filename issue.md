# Perencanaan Implementasi Fitur Realtime Factory

## Tujuan Dokumen

Dokumen ini berisi rencana implementasi fitur untuk project realtime factory `EQFR`.

Dokumen ini ditulis untuk dipakai oleh:
- junior programmer
- AI model yang lebih murah
- contributor yang perlu arahan kerja yang jelas dan berurutan

Prinsip utama dokumen ini:
- kerjakan **satu issue per satu issue**
- setiap issue harus selesai dalam kondisi **buildable**
- jangan loncat ke UI sebelum fondasi engine inti siap
- semua state runtime harus tetap **in-memory**
- semua konfigurasi harus tetap berasal dari file JSON di folder `/config`

---

## Ringkasan Kondisi Project Saat Ini

Saat ini fondasi awal project sudah tersedia:

- solution `EQFR` sudah ada
- project utama sudah dibuat:
  - `EQFR.Common`
  - `EQFR.EIFData`
  - `EQFR.IO`
  - `EQFR.Biz`
  - `EQFR.UI`
- referensi antar project dasar sudah terpasang
- `EQFR.UI` sudah berupa host app Blazor
- folder `config/` sudah tersedia
- sudah ada placeholder awal untuk:
  - primitive dasar di `EQFR.Common`
  - sebagian DTO layout di `EQFR.EIFData`
  - stub background service dan SignalR hub di `EQFR.UI`

Yang **belum selesai** dan masih perlu dikerjakan:

- shared primitives domain yang lengkap
- config DTO yang lengkap
- JSON loader dan validasi config
- runtime state model
- route graph dan reservation
- transport engine
- machine engine Roll Press
- dispatcher dan orchestrator simulasi
- snapshot realtime untuk UI
- UI dashboard factory
- panel machine, transport, event log, legend
- tombol start, pause, reset

Kesimpulan kondisi saat ini:
- fondasi bootstrap project **sudah ada**
- engine inti **belum ada**
- UI bisnis yang sebenarnya **belum ada**
- project masih berada di tahap awal dan harus dilanjutkan secara bertahap

---

## Urutan Pengerjaan yang Direkomendasikan

Urutan kerja yang direkomendasikan adalah:

1. rapikan shared primitives dan kontrak dasar
2. lengkapi config DTO dan struktur JSON
3. buat loader config dan validasi
4. buat runtime state model
5. buat route graph dan reservation
6. buat transport engine
7. buat machine engine Roll Press
8. buat dispatcher dan simulation orchestrator
9. buat snapshot realtime dan backend host flow
10. buat UI shell dashboard
11. buat factory canvas dan legend
12. buat panel machine, transport, dan event log
13. buat start, pause, reset
14. lakukan integrasi end-to-end dan rapikan dokumentasi

Urutan ini penting karena:
- UI tidak boleh menjadi tempat business logic
- engine harus stabil dulu sebelum data realtime dikirim ke UI
- snapshot UI harus dibangun dari state engine yang sudah benar

---

## Aturan Kerja untuk Junior Programmer / AI

Gunakan aturan berikut saat mengerjakan setiap issue:

- Kerjakan **hanya issue yang sedang aktif**.
- Jangan mengerjakan issue berikutnya walaupun kelihatannya mudah.
- Jangan menambahkan database.
- Jangan menambahkan authentication.
- Jangan menambahkan message broker, queue, atau event bus eksternal.
- Jangan menambahkan integrasi PLC, MES, atau sistem eksternal lain.
- Semua state runtime harus tetap in-memory.
- Semua sumber konfigurasi harus tetap berasal dari file JSON di `/config`.
- Jaga pemisahan tanggung jawab antar project.
- Jangan memindahkan business logic ke Blazor component.
- Gunakan implementasi sederhana dan deterministik terlebih dahulu.
- Tambahkan hanya dependency minimum yang benar-benar diperlukan.
- Pastikan solution masih bisa di-build setelah issue selesai.
- Bila perlu membuat stub, buat stub yang minimum dan jelas.
- Tulis kode yang mudah dibaca, jangan terlalu abstrak.
- Jangan redesign arsitektur yang tidak diminta issue.

Checklist wajib sebelum menutup issue:

- [ ] hanya scope issue ini yang dikerjakan
- [ ] solution berhasil build
- [ ] tidak ada fitur masa depan yang ikut diimplementasikan
- [ ] file yang diubah sesuai kebutuhan issue
- [ ] acceptance criteria issue terpenuhi

---

## Daftar Issue yang Sudah Ada Fondasinya

Issue/fondasi yang sudah tersedia:

- `Bootstrap solution EQFR`
  - solution dan project utama sudah dibuat
  - baseline Blazor host sudah ada
  - folder `config/` sudah ada
  - sebagian placeholder awal sudah tersedia

Fondasi parsial yang sudah bisa dipakai:

- `EQFR.Common`
  - `Result`
  - `ErrorDetail`
  - `Point2D`

- `EQFR.EIFData`
  - `FactoryLayoutConfig`
  - `LocationConfig`
  - `PortConfig`

- `EQFR.UI`
  - host Blazor dasar
  - `FactoryHub` placeholder
  - `SimulationBackgroundService` placeholder

Catatan:
- fondasi ini hanya titik awal
- implementasi fitur inti masih harus dibuat melalui issue berikutnya

---

## Daftar Issue yang Perlu Dikerjakan Berikutnya

Issue berikut direkomendasikan untuk dikerjakan setelah fondasi bootstrap:

1. lengkapi shared primitives domain
2. lengkapi config DTO dan sample config JSON
3. implementasikan JSON loader dan validasi config
4. buat runtime state model factory
5. implementasikan route graph dan reservation manager
6. implementasikan transport engine
7. implementasikan machine engine Roll Press
8. implementasikan dispatcher dan orchestrator
9. implementasikan snapshot realtime dan event flow backend
10. buat UI shell dashboard
11. buat factory canvas dan legend
12. buat machine panel, transport panel, dan event log panel
13. tambahkan kontrol start, pause, reset
14. lakukan integrasi end-to-end MVP dan dokumentasi

---

## Detail Issue Implementasi

## Issue 1 - Lengkapi Shared Primitives Domain

### Tujuan

Menyiapkan tipe dasar yang akan dipakai oleh seluruh layer project, terutama untuk status, enum, constant, dan object kecil yang bersifat lintas project.

### Alasan Kenapa Issue Ini Dikerjakan

Engine, config loader, dan UI akan sulit berkembang jika belum ada tipe dasar yang konsisten. Issue ini menjadi pondasi agar nama status, state, dan kontrak dasar tidak menyebar secara acak di banyak tempat.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.Common`
- mungkin update referensi atau namespace kecil bila diperlukan

### Langkah Implementasi Step by Step

1. Tinjau tipe dasar yang sudah ada di `EQFR.Common`.
2. Tambahkan enum dan tipe dasar yang benar-benar dibutuhkan untuk MVP.
3. Tambahkan status dasar untuk machine, transport, lot, port, dan task.
4. Tambahkan constant atau option sederhana bila memang membantu keterbacaan.
5. Pastikan naming konsisten dengan domain factory.
6. Pastikan belum ada logic bisnis besar di project ini.

### Acceptance Criteria / Definition of Done

- `EQFR.Common` memiliki tipe dasar yang cukup untuk issue berikutnya.
- Tidak ada dependency ke UI atau engine.
- Nama enum dan tipe mudah dipahami.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan menambahkan helper yang belum dibutuhkan.
- Jangan memasukkan logic dispatch, route, atau simulation ke `EQFR.Common`.
- Fokus hanya pada shared primitives.

---

## Issue 2 - Lengkapi Config DTO dan Sample Config JSON

### Tujuan

Melengkapi kontrak DTO untuk semua file config yang dibutuhkan MVP dan menyiapkan file JSON awal di folder `/config`.

### Alasan Kenapa Issue Ini Dikerjakan

Seluruh runtime harus dibentuk dari config JSON. Sebelum membuat loader dan engine, struktur data config harus jelas lebih dulu.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.EIFData`
- `config/`

### Langkah Implementasi Step by Step

1. Tinjau DTO config yang sudah tersedia.
2. Tambahkan DTO untuk:
   - layout
   - route
   - process/machine step
   - simulation
   - transport
3. Pastikan struktur DTO cukup untuk menggambarkan skenario MVP.
4. Buat sample file JSON awal di folder `config/`.
5. Isi sample config dengan skenario sederhana:
   - `WH_IN`
   - `CS_1`
   - `ROLL_PRESS_1`
   - `CS_2`
   - `WH_OUT`
   - `TR_01`
   - `TR_02`
6. Pastikan sample JSON mudah dibaca dan konsisten dengan DTO.

### Acceptance Criteria / Definition of Done

- DTO config MVP sudah tersedia.
- Folder `config/` memiliki sample file JSON yang relevan.
- Struktur JSON cukup untuk dipakai issue loader berikutnya.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan membuat parser dulu di issue ini.
- Jangan menggabungkan DTO config dengan runtime model.
- Jangan menambahkan format config yang terlalu fleksibel atau kompleks.

---

## Issue 3 - Implementasikan JSON Loader dan Validasi Config

### Tujuan

Membuat mekanisme membaca file JSON dari folder `config/` dan melakukan validasi dasar sebelum runtime state dibuat.

### Alasan Kenapa Issue Ini Dikerjakan

Runtime state tidak boleh dibangun dari data yang tidak valid. Validasi lebih awal akan mengurangi error yang sulit dilacak saat simulasi berjalan.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.IO`
- mungkin sebagian kecil `src/EQFR.Common`
- `config/` jika ada sample yang perlu dirapikan

### Langkah Implementasi Step by Step

1. Buat kontrak loader config yang sederhana.
2. Implementasikan pembacaan file JSON dari folder `config/`.
3. Tambahkan validasi dasar untuk:
   - file wajib ada
   - id tidak kosong
   - referensi route valid
   - machine step valid
   - transport startup valid
4. Pastikan hasil validasi mudah dipahami.
5. Gagal lebih awal jika config tidak valid.
6. Pastikan issue ini belum membuat runtime engine.

### Acceptance Criteria / Definition of Done

- Config dapat dibaca dari file JSON.
- Error config yang tidak valid dapat dilaporkan dengan jelas.
- Loader bisa dipakai issue runtime state berikutnya.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan memulai simulation loop.
- Jangan mencampur loader dengan logic bisnis engine.
- Validasi cukup untuk MVP, tidak perlu over-engineering.

---

## Issue 4 - Buat Runtime State Model Factory

### Tujuan

Membuat model state runtime in-memory yang akan mewakili lokasi, port, route, transport, machine, lot, task, reservation, dan event log.

### Alasan Kenapa Issue Ini Dikerjakan

Engine butuh representasi runtime yang jelas dan terpisah dari config DTO. Tanpa ini, business logic akan sulit dijaga dan diuji.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.Biz`
- mungkin sedikit `src/EQFR.Common`

### Langkah Implementasi Step by Step

1. Tentukan model runtime inti yang dibutuhkan oleh MVP.
2. Pisahkan model runtime dari DTO config.
3. Buat struktur state utama factory.
4. Tambahkan model untuk machine, transport, lot, route edge, task, dan event log.
5. Buat factory/builder sederhana untuk membentuk runtime state dari config.
6. Pastikan state masih murni in-memory.

### Acceptance Criteria / Definition of Done

- Runtime state model tersedia dan dapat dibentuk dari config.
- DTO config dan runtime model terpisah jelas.
- Struktur state siap dipakai oleh engine berikutnya.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan membuat logic transport atau machine yang lengkap di issue ini.
- Jangan mengirim state ini langsung ke UI.
- Fokus pada representasi state, bukan perilaku.

---

## Issue 5 - Implementasikan Route Graph dan Reservation Manager

### Tujuan

Membuat representasi route graph dan mekanisme reservasi edge sederhana agar transport hanya bergerak pada jalur yang valid dan tidak menabrak secara logika.

### Alasan Kenapa Issue Ini Dikerjakan

Pergerakan transport adalah inti domain project ini. Sebelum transport engine dibuat, jalur pergerakan dan aturan reservasi harus sudah jelas.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.Biz`
- mungkin sedikit `src/EQFR.Common`

### Langkah Implementasi Step by Step

1. Buat representasi graph dari config route.
2. Buat cara mencari jalur sederhana untuk MVP.
3. Buat reservation manager yang hanya mengizinkan satu transport pada satu edge reserved.
4. Buat aturan reserve, wait, dan release.
5. Tambahkan testability pada komponen ini melalui desain yang sederhana.
6. Pastikan hasilnya bisa dipakai transport engine berikutnya.

### Acceptance Criteria / Definition of Done

- Transport dapat divalidasi hanya berjalan pada route yang legal.
- Reservation manager dapat reserve dan release edge.
- Kondisi blocked route dapat dibedakan dari route valid.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan membuat collision avoidance yang rumit.
- Gunakan aturan yang deterministik dan sederhana.
- Tidak perlu optimasi routing di tahap ini.

---

## Issue 6 - Implementasikan Transport Engine

### Tujuan

Membuat engine yang menggerakkan transport unit berdasarkan task, route, reservation, dan status lot.

### Alasan Kenapa Issue Ini Dikerjakan

Setelah route dan reservation siap, transport engine menjadi langkah berikutnya agar material benar-benar bisa dipindahkan dari satu titik ke titik lain.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.Biz`
- mungkin sedikit `src/EQFR.Common`

### Langkah Implementasi Step by Step

1. Buat kontrak dan implementasi transport engine.
2. Definisikan alur task dasar:
   - idle
   - menuju pickup
   - pickup
   - menuju dropoff
   - dropoff
   - kembali ke charging station
3. Hubungkan transport engine dengan route graph dan reservation manager.
4. Update state transport pada setiap tick.
5. Update lot state ketika pickup dan dropoff terjadi.
6. Tambahkan event log dasar untuk pergerakan penting.

### Acceptance Criteria / Definition of Done

- Transport dapat berpindah sesuai route yang valid.
- Transport dapat pickup dan dropoff lot.
- Transport dapat kembali ke charging station setelah task selesai.
- Event penting transport tercatat.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan membuat dispatch pintar dulu.
- Jangan menambahkan battery logic.
- Fokus pada perilaku transport MVP saja.

---

## Issue 7 - Implementasikan Machine Engine Roll Press

### Tujuan

Membuat machine engine untuk `Roll Press` yang dapat meminta input, memproses step, dan menghasilkan output ready.

### Alasan Kenapa Issue Ini Dikerjakan

Machine adalah pusat alur produksi. Setelah transport bisa bergerak, machine perlu bisa memproses material secara bertahap.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.Biz`
- mungkin sedikit `src/EQFR.Common`

### Langkah Implementasi Step by Step

1. Buat kontrak dan implementasi machine engine khusus MVP.
2. Tambahkan logika kebutuhan input ketika machine kosong.
3. Mulai processing ketika lot masuk ke machine.
4. Jalankan machine step secara berurutan berdasarkan duration config.
5. Setelah step terakhir selesai, ubah machine menjadi output ready.
6. Tambahkan event penting untuk perubahan state machine.

### Acceptance Criteria / Definition of Done

- Machine bisa meminta input saat kosong.
- Machine bisa memproses lot melalui beberapa step.
- Machine bisa menandai output ready setelah selesai.
- Event step change dan completion tercatat.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Hanya satu machine aktif untuk MVP.
- Satu input menghasilkan satu output dengan lot id yang sama.
- Jangan menambahkan scrap, rework, atau branching.

---

## Issue 8 - Implementasikan Dispatcher dan Simulation Orchestrator

### Tujuan

Membuat komponen yang menghubungkan machine engine dan transport engine dalam loop simulasi yang deterministik.

### Alasan Kenapa Issue Ini Dikerjakan

Machine dan transport tidak akan berjalan bersama tanpa dispatcher dan orchestrator. Komponen ini menjadi pusat koordinasi runtime.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.Biz`
- mungkin sedikit `src/EQFR.Common`

### Langkah Implementasi Step by Step

1. Buat dispatcher untuk memilih transport idle yang cocok.
2. Buat aturan dispatch sederhana untuk input dan output.
3. Buat simulation orchestrator dengan urutan tick yang jelas.
4. Jalankan machine engine, dispatcher, transport engine, dan route update pada setiap tick.
5. Simpan recent event log di state runtime.
6. Pastikan hasil simulasi deterministik untuk config yang sama.

### Acceptance Criteria / Definition of Done

- Request input dari machine dapat menghasilkan task transport.
- Output ready dari machine dapat menghasilkan pickup output.
- Loop simulasi dapat berjalan end-to-end pada level backend.
- Event log dasar tersedia.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan membuat scheduler rumit.
- Pilih transport dengan aturan sederhana dan konsisten.
- Fokus pada satu skenario MVP dulu.

---

## Issue 9 - Implementasikan Snapshot Realtime dan Event Flow Backend

### Tujuan

Membuat snapshot ringan untuk UI dan menyalurkan update realtime dari backend host ke UI.

### Alasan Kenapa Issue Ini Dikerjakan

UI tidak boleh membaca engine internals secara langsung. UI harus menerima data yang sudah diringkas dan aman untuk rendering.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.Biz`
- `src/EQFR.UI`
- mungkin sedikit `src/EQFR.Common`

### Langkah Implementasi Step by Step

1. Tentukan bentuk snapshot untuk UI.
2. Buat builder yang mengubah runtime state menjadi snapshot ringan.
3. Hubungkan orchestrator dengan background service di host app.
4. Kirim update snapshot melalui SignalR atau jalur realtime yang sudah disiapkan.
5. Pastikan event log ringkas ikut masuk ke snapshot.
6. Pastikan UI tidak menerima seluruh internal state engine.

### Acceptance Criteria / Definition of Done

- Backend dapat menghasilkan snapshot realtime.
- Snapshot berisi state machine, transport, lot, route occupancy, dan event log ringkas.
- Host app dapat mengalirkan update ke UI.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan memindahkan engine ke project UI.
- Snapshot harus fokus pada kebutuhan rendering UI.
- Hindari payload yang terlalu besar.

---

## Issue 10 - Buat UI Shell Dashboard

### Tujuan

Membuat shell dashboard Blazor yang menjadi wadah utama semua panel dan canvas factory.

### Alasan Kenapa Issue Ini Dikerjakan

Setelah data backend siap, UI butuh struktur layar utama yang jelas agar pengembangan panel berikutnya tidak berantakan.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.UI`

### Langkah Implementasi Step by Step

1. Tentukan layout dashboard utama.
2. Buat page dan component dasar untuk shell dashboard.
3. Buat ViewModel dasar untuk page state.
4. Hubungkan shell dengan realtime service/presentation service.
5. Sediakan placeholder area untuk:
   - canvas
   - machine panel
   - transport panel
   - event log
   - legend
   - control panel
6. Pastikan belum ada business logic di Razor component.

### Acceptance Criteria / Definition of Done

- Dashboard shell tersedia dan bisa dibuka.
- Struktur UI siap untuk panel-panel berikutnya.
- State UI berasal dari ViewModel/presentation layer.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan langsung membuat semua detail UI dalam issue ini.
- Jangan menaruh logic simulation di UI component.
- Fokus pada shell dan struktur presentasi.

---

## Issue 11 - Buat Factory Canvas dan Legend

### Tujuan

Membuat visualisasi layout factory, route, node, transport, dan legend status dasar.

### Alasan Kenapa Issue Ini Dikerjakan

Canvas adalah tampilan utama sistem monitoring. Setelah shell siap, visualisasi factory menjadi prioritas UI pertama.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.UI`

### Langkah Implementasi Step by Step

1. Buat ViewModel untuk canvas.
2. Render location/node berdasarkan snapshot.
3. Render route line berdasarkan data route.
4. Render posisi transport dan status dasarnya.
5. Tambahkan selected state bila diperlukan untuk perkembangan berikutnya.
6. Buat legend sederhana untuk mapping status.

### Acceptance Criteria / Definition of Done

- Canvas menampilkan lokasi utama factory.
- Route terlihat dengan jelas.
- Transport dan machine state dasar bisa terlihat di canvas.
- Legend tersedia dan mudah dipahami.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan membuat animasi kompleks dulu.
- Fokus pada kejelasan informasi.
- Jangan menambahkan logic bisnis di komponen canvas.

---

## Issue 12 - Buat Machine Panel, Transport Panel, dan Event Log Panel

### Tujuan

Menampilkan detail informasi machine, transport, current lot, dan event log dalam panel yang terpisah.

### Alasan Kenapa Issue Ini Dikerjakan

Canvas saja tidak cukup untuk operasional. User butuh detail state yang lebih mudah dibaca di panel samping atau panel bawah.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.UI`

### Langkah Implementasi Step by Step

1. Buat ViewModel untuk panel machine.
2. Buat ViewModel untuk panel transport.
3. Buat ViewModel untuk event log.
4. Tampilkan current lot id, status, step, task, dan lokasi penting.
5. Tampilkan event log terbaru di urutan paling baru.
6. Pastikan panel menerima data dari snapshot/presentation layer.

### Acceptance Criteria / Definition of Done

- Machine panel menampilkan state machine utama.
- Transport panel menampilkan status transport.
- Event log menampilkan event terbaru dengan timestamp yang mudah dibaca.
- Current lot id terlihat jelas.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan membaca engine langsung dari komponen.
- Jangan mencampur state panel dengan state shell secara berlebihan.
- Jaga struktur MVVM-inspired tetap rapi.

---

## Issue 13 - Tambahkan Kontrol Start, Pause, Reset

### Tujuan

Menambahkan kontrol dasar untuk mengatur lifecycle simulasi dari dashboard.

### Alasan Kenapa Issue Ini Dikerjakan

MVP perlu bisa dijalankan, dihentikan sementara, dan di-reset. Tanpa kontrol ini, simulasi tidak akan nyaman diuji atau didemokan.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.UI`
- `src/EQFR.Biz`
- mungkin `src/EQFR.Common`

### Langkah Implementasi Step by Step

1. Tambahkan state simulasi yang jelas: running, paused, stopped/reset.
2. Buat command atau endpoint internal untuk start, pause, reset.
3. Hubungkan control panel UI dengan service/presentation layer.
4. Pastikan reset mengembalikan runtime state ke kondisi awal dari config.
5. Pastikan pause benar-benar menghentikan progress tick.
6. Tampilkan state kontrol di UI dengan jelas.

### Acceptance Criteria / Definition of Done

- Tombol start berfungsi menjalankan simulasi.
- Tombol pause berfungsi menghentikan progress sementara.
- Tombol reset mengembalikan state ke awal berdasarkan config.
- UI menampilkan state simulasi saat ini.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Belum perlu step-once atau speed control.
- Reset harus kembali ke config awal, bukan state acak terakhir.
- Jangan membuat kontrol yang belum dibutuhkan MVP.

---

## Issue 14 - Integrasi End-to-End MVP dan Dokumentasi

### Tujuan

Menyatukan semua bagian agar skenario MVP berjalan utuh dari config sampai UI realtime, lalu merapikan dokumentasi penggunaan.

### Alasan Kenapa Issue Ini Dikerjakan

Setelah semua bagian utama tersedia, perlu ada satu issue khusus untuk memastikan keseluruhan alur benar-benar terhubung dan dapat didemokan.

### File/Project yang Kemungkinan Akan Diubah

- `src/EQFR.Common`
- `src/EQFR.EIFData`
- `src/EQFR.IO`
- `src/EQFR.Biz`
- `src/EQFR.UI`
- `config/`
- dokumentasi repo jika diperlukan

### Langkah Implementasi Step by Step

1. Jalankan alur lengkap dari config sampai UI.
2. Pastikan machine meminta input saat kosong.
3. Pastikan transport mengambil input dan mengantar ke machine.
4. Pastikan machine memproses step sampai output ready.
5. Pastikan transport mengambil output dan mengantarnya ke warehouse tujuan.
6. Pastikan event log, current lot id, machine status, dan transport status tampil realtime.
7. Rapikan dokumentasi build dan run.

### Acceptance Criteria / Definition of Done

- Skenario MVP berjalan end-to-end.
- UI menampilkan state realtime yang sesuai.
- Event log terbaca dengan baik.
- Start, pause, reset bekerja.
- Solution tetap berhasil build.

### Catatan Penting / Batasan

- Jangan menambahkan fitur fase berikutnya.
- Fokus pada kestabilan alur MVP.
- Bila ada kekurangan minor visual, prioritaskan kestabilan engine dan data terlebih dahulu.

---

## Cara Menggunakan Dokumen Ini

Saat memulai satu issue:

1. pilih satu issue aktif
2. baca hanya scope issue tersebut
3. kerjakan langkah implementasinya
4. pastikan acceptance criteria terpenuhi
5. build solution
6. baru lanjut ke issue berikutnya

Jangan melakukan ini:

- mengerjakan dua issue sekaligus
- membuat UI final sebelum engine selesai
- menambahkan fitur di luar MVP
- membuat arsitektur terlalu rumit di tahap awal

---

## Ringkasan Akhir

Rencana implementasi yang disarankan untuk project realtime factory `EQFR` adalah:

- mulai dari fondasi domain dan config
- lanjut ke loader dan runtime state
- lanjut ke route, transport, machine, dispatcher, dan orchestrator
- setelah backend stabil, baru lanjut ke realtime snapshot dan UI
- tutup dengan integrasi end-to-end MVP

Kalau urutan ini diikuti dengan disiplin satu issue per satu issue, project akan lebih mudah dijaga, lebih mudah diuji, dan lebih aman untuk dikerjakan oleh junior programmer maupun AI model yang lebih murah.
