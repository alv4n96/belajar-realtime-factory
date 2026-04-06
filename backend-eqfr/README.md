# Backend EQFR (Express.js)

Backend ini menyediakan HTTP API sederhana agar bisa diakses oleh EQFR (atau client lain) untuk:
- membaca config dari folder `/config`
- membaca/mengirim snapshot runtime (in-memory)
- mengirim command kontrol (start/pause/reset)

## Prinsip MVP

- Tanpa database (semua state backend in-memory).
- Config hanya dibaca dari folder `/config` di root repo.

## Menjalankan

Dari root repo:

```powershell
cd .\backend-eqfr
npm.cmd install
npm.cmd start
```

Default listen di `http://localhost:3001`.

Env vars:
- `PORT` (default 3001)
- `HOST` (default 0.0.0.0)
- `CONFIG_DIR` (opsional) override path folder config (default: mencari folder `config/` dari parent directory)

## Endpoint

- `GET /health`
- `GET /api/config`
- `GET /api/snapshot`
- `POST /api/snapshot` (push snapshot dari producer)
- `GET /api/controls`
- `POST /api/controls/start`
- `POST /api/controls/pause`
- `POST /api/controls/reset`
- `POST /api/controls/consume-reset`

## Contoh curl

```powershell
curl http://localhost:3001/health
curl http://localhost:3001/api/config
curl http://localhost:3001/api/controls
```

