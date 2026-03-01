# MedCare Hub — Frontend (Angular)

## Dev
Prerequisiti: Node 20+

1) Backend API avviata (dotnet run) su `http://localhost:7070`
2) Keycloak avviato (docker compose) su `http://localhost:8081`
3) Frontend:
```bash
npm install
npm start
```
Apri: http://localhost:4200

`proxy.conf.json` inoltra `/api/*` verso `http://localhost:7070`.

## Docker (Nginx)
Build:
```bash
docker build -t medcarehub-web .
```

Run (proxy /api verso servizio docker chiamato `api`):
```bash
docker run --rm -p 8082:80 \
  -e KEYCLOAK_URL=http://localhost:8081 \
  -e API_BASE_URL=/api \
  medcarehub-web
```
Apri: http://localhost:8082
