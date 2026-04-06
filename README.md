# EQFR (EQ Factory Realtime) - MVP Realtime Factory

Repo ini adalah MVP realtime factory untuk demo/testing dengan backend mock in-memory dan UI dashboard realtime.

## Struktur Solution

- `EQFR.Common` (class library): shared types, enums, contracts.
- `EQFR.EIFData` (class library): model/config data (layout, routes, process, transport, simulation).
- `EQFR.IO` (class library): loader config JSON dari folder `config/`.
- `EQFR.Biz` (class library): kontrak snapshot dan logika domain yang dipakai UI.
- `EQFR.UI` (host app): Blazor (Interactive Server) untuk menampilkan factory dashboard realtime.
- `backend-eqfr` (Node/Express): mock backend tanpa database yang generate snapshot factory otomatis dari folder `config/`.

## Prinsip MVP

- Tidak menggunakan database.
- Semua runtime state in-memory.
- Semua config berasal dari file JSON di folder `config/`.
- Backend mock adalah source data untuk UI.

## Menjalankan Aplikasi

Prasyarat:
- .NET SDK (`net10.0`)
- Node.js

1. Jalankan backend mock:

```powershell
cd .\backend-eqfr
npm.cmd start
```

Default backend listen di `http://localhost:3001`.

2. Jalankan UI:

```powershell
cd ..\src\EQFR.UI
dotnet run
```

3. Buka dashboard:
- `http://localhost:5042/dashboard`
- jika port default UI sedang dipakai, aplikasi otomatis pindah ke port berikutnya (`5043`, `5044`, dst.)

## Flow Saat Ini

- Backend membaca `config/` saat startup.
- Backend generate snapshot factory secara otomatis tanpa database.
- UI polling `http://localhost:3001/api/snapshot` lalu menampilkan hasilnya di dashboard realtime.
- Dashboard berjalan otomatis tanpa tombol start/pause/reset.

## Endpoint Backend Mock

- `GET /health`
- `GET /api/config`
- `GET /api/snapshot`
- `GET /api/controls`
- `POST /api/controls/start`
- `POST /api/controls/pause`
- `POST /api/controls/stop`
- `POST /api/controls/reset`
- `POST /api/controls/consume-reset`

## Troubleshooting Cepat

- Dashboard kosong atau `Menunggu snapshot realtime...`
  - pastikan backend `npm.cmd start` berjalan di `http://localhost:3001`
- `address already in use` saat menjalankan UI
  - instance kedua UI akan fallback ke port lokal berikutnya; lihat URL di console
- Backend gagal start
  - pastikan folder `config/` ada di root repo dan file JSON valid
