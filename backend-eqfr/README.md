# Backend EQFR (Express.js)

Backend ini menyediakan mock API tanpa database untuk demo/testing EQFR.

## Fungsi

- membaca config dari folder `/config`
- generate snapshot runtime factory secara otomatis di memory
- expose snapshot dan control endpoint untuk diakses UI

## Prinsip MVP

- Tanpa database
- Semua state backend in-memory
- Config dibaca dari folder `/config` di root repo

## Menjalankan

Dari root repo:

```powershell
cd .\backend-eqfr
npm.cmd install
npm.cmd start
```

Default listen di `http://localhost:3001`.

## Env vars

- `PORT` (default 3001)
- `HOST` (default 0.0.0.0)
- `CONFIG_DIR` (opsional) override path folder config

## Endpoint

- `GET /health`
- `GET /api/config`
- `GET /api/snapshot`
- `GET /api/controls`
- `POST /api/controls/start`
- `POST /api/controls/pause`
- `POST /api/controls/stop`
- `POST /api/controls/reset`
- `POST /api/controls/consume-reset`

## Catatan

- Snapshot di-generate otomatis saat backend start.
- Status default mock runtime adalah `Running`.
- Endpoint control tetap ada agar mudah dipakai untuk test/reset manual.
