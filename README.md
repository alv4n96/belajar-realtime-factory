# EQFR (EQ Factory Realtime) - MVP Realtime Factory

Repo ini adalah MVP simulasi factory realtime berbasis in-memory dan config JSON.

## Struktur Solution

- `EQFR.Common` (class library): shared types, enums, contracts.
- `EQFR.EIFData` (class library): model/config data (layout, routes, process, transport, simulation).
- `EQFR.IO` (class library): loader config JSON dari folder `config/`.
- `EQFR.Biz` (class library): runtime state, routing, dispatching, machine + transport engines, snapshot builder.
- `EQFR.UI` (host app): Blazor (Interactive Server) untuk menampilkan factory dashboard realtime.

## Prinsip MVP

- Tidak menggunakan database.
- Semua runtime state wajib in-memory.
- Semua config harus berasal dari file JSON di folder `config/`.

## Menjalankan Aplikasi

Prasyarat:
- .NET SDK (repo ini menggunakan target `net10.0`, sehingga idealnya pakai SDK yang sesuai).

Perintah:

```powershell
dotnet run --project .\src\EQFR.UI
```

Lalu buka:
- `https://localhost:<port>/dashboard`

Catatan:
- Service simulasi mencari folder `config/` mulai dari `AppContext.BaseDirectory` lalu naik beberapa parent folder.
- Status awal simulasi adalah `Stopped`. Tekan `Start` untuk mulai tick engine.

## Verifikasi Flow End-to-End (MVP)

Dengan config default, flow yang diharapkan:
- `LOT_001` tersedia di `WH_IN:OUT`
- Machine `ROLL_PRESS_1` akan request input
- Dispatcher membuat task: deliver input `WH_IN:OUT -> ROLL_PRESS_1:IN`
- Machine memproses step `Load -> Press -> Unload`
- Output ready, lalu dispatcher membuat task: move output `ROLL_PRESS_1:OUT -> WH_OUT:IN`

Yang harus terlihat di Dashboard:
- Canvas/layout menampilkan lokasi, edge, dan posisi transport bergerak per tick.
- Panel `Machine` menampilkan status, lot id, step, flags.
- Panel `Transport` menampilkan node saat ini, task id, dan lot yang dibawa.
- `Event Log` menampilkan rangkaian event (dispatch, pickup/dropoff, machine step).
- Tombol:
  - `Start`: menjalankan simulasi.
  - `Pause`: menghentikan tick (state tidak berubah, tapi UI tetap update snapshot).
  - `Reset`: rebuild runtime state dari config (kembali ke kondisi awal).

## Konfigurasi

Folder: `config/`

File utama:
- `factory-layout.json`: lokasi, posisi, dan port.
- `factory-routes.json`: edge/route yang boleh dilewati transport.
- `factory-transport.json`: daftar transport dan start node.
- `factory-process.json`: machine process steps (MVP: Roll Press).
- `factory-simulation.json`: tick interval, max event, initial lots.

## Troubleshooting Cepat

- Transport tidak bergerak:
  - Pastikan `factory-routes.json` membentuk graph yang terhubung dari start node transport ke pickup/dropoff node.
- UI tidak update:
  - Pastikan `dotnet run --project .\src\EQFR.UI` sukses tanpa error dan akses `/dashboard`.

