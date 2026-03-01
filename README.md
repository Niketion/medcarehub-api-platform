# MedCareHub — Full-Stack API-Based Healthcare MVP

MedCareHub is a full-stack, API-based application designed for the digitalization of essential healthcare workflows in a clinic/polyclinic scenario: publishing medical availability (slots), booking visits, managing clinical reports, and providing an operational dashboard. The project is developed as an academic Project Work (PW16) and emphasizes typical healthcare constraints such as access control, traceability, and service reproducibility.

## Objectives and Scope

**Primary goals (MVP):**
- Slot publication and consultation
- Patient bookings with protection against double bookings (consistency under concurrent requests)
- Clinical report upload/download with role-based access and ownership checks
- Operational dashboard with synthetic indicators (slots, bookings, reports)
- Reproducible local environment via containers for evaluation and demonstration

This repository contains:
- **Backend:** ASP.NET Core Web API (.NET 8), EF Core, PostgreSQL, Keycloak (OIDC/JWT), MinIO (S3-compatible object storage)
- **Frontend:** Angular SPA (TypeScript) served by Nginx, with authentication via Keycloak
- **Infrastructure:** Docker Compose for consistent local execution

## Logical Architecture

**SPA (Angular) → REST API (ASP.NET Core) → PostgreSQL (relational data) + MinIO (files)**  
Authentication and authorization are handled via **Keycloak** using **OpenID Connect / JWT** with **RBAC (role-based access control)**.

### Data and Storage Strategy
- **Relational data (PostgreSQL):** users’ operational data (slots, bookings, report metadata, audit logs)
- **Files (MinIO):** report binaries are stored in object storage; the database stores only metadata and object references

## Features

### Roles and Permissions (RBAC)
The system models typical healthcare actors:
- **Patient:** browse slots, create/cancel own bookings, list and download own reports (ownership enforced)
- **Operator/Doctor/Admin (staff):** create slots, upload reports, view operational dashboard (with staff visibility)

### Consistency and Concurrency
Booking creation is treated as an atomic operation and includes checks that prevent double booking of the same slot under concurrent requests.

### Traceability (Audit)
Relevant operations (e.g., booking/cancellation, upload/download, access denied events) are recorded as audit events with actor and outcome metadata.

### API Contract and Documentation
The backend exposes a REST API documented through **Swagger/OpenAPI**, which also supports manual testing during evaluation.

## Technology Stack

- **Backend:** ASP.NET Core Web API (.NET 8), Entity Framework Core, Npgsql provider
- **Database:** PostgreSQL 16
- **Identity:** Keycloak (OIDC/JWT), RBAC
- **Object Storage:** MinIO (S3-compatible)
- **Frontend:** Angular (TypeScript), Keycloak JS
- **Delivery/Runtime:** Docker, Docker Compose, Nginx

## Repository Structure (high level)

- `docker-compose.yml` — local environment (PostgreSQL, MinIO, Keycloak, optional API container, Web container)
- `backend/src/MedCareHub.Api/` — ASP.NET Core API (Dockerfile included)
- `frontend/` — Angular application + Nginx runtime packaging
- `keycloak/realm-medcarehub.json` — realm import with demo users and roles

## Quick Start (Docker Compose)

### Prerequisites
- Docker Desktop (or Docker Engine) with Docker Compose

### Start the infrastructure (DB + MinIO + Keycloak)
```bash
docker compose up -d
```
This starts:
- PostgreSQL on `localhost:5432`
- MinIO on `localhost:9000` (console `localhost:9001`)
- Keycloak on `localhost:8081` (importing the `medcarehub` realm automatically)

### Start the full stack (including API container)
```bash
docker compose --profile full up -d --build
```
Services and ports (default):
- API: `http://localhost:8080`
- Web: `http://localhost:4200`
- Keycloak: `http://localhost:8081`
- MinIO: `http://localhost:9000` (console `http://localhost:9001`)
- PostgreSQL: `localhost:5432`

## Configuration

### Backend (API container)
The Compose profile `full` configures the API through environment variables, including:
- `ConnectionStrings__Default` (PostgreSQL)
- `Auth__Authority` (Keycloak realm issuer)
- `Storage__Endpoint`, `Storage__AccessKey`, `Storage__SecretKey`, `Storage__Bucket` (MinIO)
- `Database__ApplyMigrationsOnStartup=true` to apply EF Core migrations automatically on startup

### Frontend runtime configuration
The Web container writes `/assets/config.json` at runtime (via an Nginx entrypoint script) using:
- `API_BASE_URL` (default `/api`)
- `KEYCLOAK_URL` (default `http://localhost:8081`)
- `KEYCLOAK_REALM` (default `medcarehub`)
- `KEYCLOAK_CLIENT_ID` (default `medcarehub-web`)

## Keycloak (Realm Import and Demo Users)

Keycloak is started with automatic realm import (`start-dev --import-realm`). The provided realm includes:
- Realm: `medcarehub`
- Frontend client: `medcarehub-web` (public, OIDC)
- Roles: `patient`, `operator`, `doctor`, `admin`
- Demo users:
  - `patient1` / `Password!23` (role: `patient`)
  - `operator1` / `Password!23` (role: `operator`)
- Keycloak admin console:
  - URL: `http://localhost:8081`
  - Admin credentials: `admin` / `admin`

## API Documentation (Swagger)

When the API is running, Swagger UI is available at:
- `http://localhost:8080/swagger`

## Authentication for API Testing (Development)

For local testing, a password grant example is available (development only). Obtain a token from Keycloak:

```bash
curl -X POST "http://localhost:8081/realms/medcarehub/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=medcarehub-web" \
  -d "username=patient1" \
  -d "password=Password!23" \
  -d "grant_type=password"
```

Then use Swagger **Authorize** with:
- `Bearer <access_token>`

> Note: for simplified local execution, audience/issuer validation may be relaxed in development configuration; in production, enable full validation and configure appropriate mappers in Keycloak.

## Main MVP Endpoints

Indicative endpoints implemented by the MVP include:
- Slots:
  - `GET /api/slots`
  - `POST /api/slots` (staff)
- Bookings:
  - `POST /api/bookings` (patient)
  - `GET /api/bookings/my` (patient)
  - `DELETE /api/bookings/{id}` (patient)
- Reports:
  - `POST /api/reports/upload` (staff, `multipart/form-data`)
  - `GET /api/reports/my` (patient)
  - `GET /api/reports/{id}/download` (patient with ownership, or staff)

## Development Workflow (Local)

### Backend (run in IDE)
1. Start infrastructure:
   ```bash
   docker compose up -d
   ```
2. Run the API from source (Debug):
   ```bash
   cd backend/src/MedCareHub.Api
   dotnet restore
   dotnet run
   ```
Swagger will be exposed on the port printed at startup (Docker profile uses `8080`).

### Frontend (Angular dev server)
1. Ensure the API is running (local or Docker profile `full`)
2. Start Angular:
   ```bash
   cd frontend
   npm install
   npm start
   ```
The dev server uses a proxy configuration to forward `/api/*` to the backend (adjust the proxy target if your API port differs).

## Validation and Evaluation Notes

Functional verification is designed around repeatable scenarios:
- Staff creates slots
- Patient performs a single booking and attempts a double booking
- Staff uploads a report for a booking
- Patient downloads own report; access is denied for unauthorized users
- Dashboard consultation and audit log verification

## Limitations and Future Work

The MVP is intentionally focused on core workflows. Planned/possible extensions include:
- Automated test suite expansion (unit/integration tests)
- Advanced calendar features (recurrence, multi-doctor scheduling)
- Notifications (email/SMS)
- Payment integration
- Workflow approval/signature and qualified digital signature support

## Academic Disclaimer

This project is a didactic and demonstrative implementation aimed at validating design and integration choices in an API-based architecture for the healthcare domain. It is not intended for production use without a dedicated security hardening phase, operational monitoring, and compliance verification (including but not limited to privacy and regulatory requirements).
