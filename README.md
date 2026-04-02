# MedCareHub — Full-Stack API-Based Healthcare MVP

MedCareHub is a full-stack, API-based application designed for the digitalization of essential healthcare workflows in a clinic/polyclinic scenario: publishing medical availability (slots), booking visits, managing clinical reports, and providing operational and economic dashboards. The project is developed as an academic Project Work (PW16) and emphasizes typical healthcare constraints such as access control, traceability, service reproducibility, scheduling consistency, and protection of clinical documents.

## Objectives and Scope

**Primary goals (MVP):**
- Slot publication and consultation
- Patient bookings with protection against double bookings
- Validation against overlapping slots for the same doctor
- Clinical report upload/download with role-based access and ownership checks
- Operational and economic dashboard with synthetic indicators
- Payment lifecycle tracking for bookings
- Reproducible local environment via containers for evaluation and demonstration
- Automated backend verification through a dedicated test service

This repository contains:
- **Backend:** ASP.NET Core Web API (.NET 8), EF Core, PostgreSQL, Keycloak (OIDC/JWT), MinIO (S3-compatible object storage)
- **Frontend:** Angular SPA (TypeScript) served by Nginx, with authentication via Keycloak
- **Infrastructure:** Docker Compose for consistent local execution
- **Tests:** xUnit-based backend test suite executed locally and optionally through Docker Compose

## Logical Architecture

**SPA (Angular) → REST API (ASP.NET Core) → PostgreSQL (relational data) + MinIO (files)**  
Authentication and authorization are handled via **Keycloak** using **OpenID Connect / JWT** with **RBAC (role-based access control)**.

### Data and Storage Strategy
- **Relational data (PostgreSQL):** slots, bookings, economic data, payment state, report metadata, audit logs
- **Files (MinIO):** report binaries are stored in object storage; the database stores only metadata and object references

## Features

### Roles and Permissions (RBAC)
The system models typical healthcare actors:
- **Patient:** browse slots, create/cancel own bookings, list and download own reports
- **Operator/Doctor/Admin (staff):** create slots, create prestazioni, upload reports, complete bookings, mark bookings as paid, and consult operational/economic dashboards

### Scheduling Consistency
The system applies business rules on slot creation:
- slot end must be after slot start
- the same doctor cannot have overlapping active slots
- cancelled slots are excluded from overlap checks

### Booking Consistency and Concurrency
Booking creation is treated as an atomic operation and includes checks that prevent double booking of the same slot under concurrent requests.

### Economic Features
The MVP includes an economic layer suitable for a clinic/polyclinic scenario:
- Prestazione base price
- Booked price snapshot stored on each booking at creation time
- Payment status tracking
- **Economic dashboard** with:
  - estimated revenue
  - realized revenue
  - paid revenue
  - average ticket
  - breakdown by doctor
  - breakdown by prestazione
  - revenue trend visualization

### Clinical Reports
Report management supports:
- upload by staff
- download by the owner patient or staff
- metadata such as report type, document date, author, author role, and signature timestamp
- **PDF-only validation** on upload (file extension and content type whitelist)

### Traceability (Audit)
Relevant operations are recorded as audit events with actor, target, outcome, and metadata. Examples include:
- booking creation/cancellation/completion/payment
- report upload/download
- denied access attempts
- slot creation
- prestazione creation

### API Contract and Documentation
The backend exposes a REST API documented through **Swagger/OpenAPI**, which also supports manual testing during evaluation.

## Technology Stack

- **Backend:** ASP.NET Core Web API (.NET 8), Entity Framework Core, Npgsql provider
- **Database:** PostgreSQL 16
- **Identity:** Keycloak (OIDC/JWT), RBAC
- **Object Storage:** MinIO (S3-compatible)
- **Frontend:** Angular (TypeScript), Keycloak JS, Nginx
- **Tests:** xUnit, FluentAssertions, EF Core InMemory (test project)
- **Delivery/Runtime:** Docker, Docker Compose

## Repository Structure (High Level)

- `docker-compose.yml` — local environment and full profile orchestration
- `backend/src/MedCareHub.Api/` — ASP.NET Core API (Dockerfile included)
- `tests/MedCareHub.Api.Tests/` — backend unit tests
- `frontend/` — Angular application + Nginx runtime packaging
- `keycloak/realm-medcarehub.json` — realm import with demo users and roles

## Quick Start (Docker Compose)

### Prerequisites
- Docker Desktop (or Docker Engine) with Docker Compose

### Start Infrastructure Only (DB + MinIO + Keycloak)
```bash
docker compose up -d
```

This starts:
- PostgreSQL on `localhost:5432`
- MinIO on `localhost:9000` (console `localhost:9001`)
- Keycloak on `localhost:8081` (importing the `medcarehub` realm automatically)

### Start the Full Stack (API + Web + test gate)
```bash
docker compose --profile full up -d --build
```

Services and ports (default):
- API: `http://localhost:8080`
- Web: `http://localhost:4200`
- Keycloak: `http://localhost:8081`
- MinIO: `http://localhost:9000` (console `http://localhost:9001`)
- PostgreSQL: `localhost:5432`

### Stop the Full Stack
```bash
docker compose --profile full down
```

## Docker Compose Notes

The Compose file supports two usage modes:

1. **Infrastructure only**
   - `postgres`
   - `minio`
   - `keycloak-db`
   - `keycloak`

2. **Full profile**
   - `api-tests`
   - `api`
   - `web`

The `full` profile is configured so that the backend test service runs before the API container. This provides a simple local quality gate for demonstrations and validation.

## Configuration

### Backend (API container)
The Compose profile `full` configures the API through environment variables, including:
- `ConnectionStrings__Default` (PostgreSQL)
- `Auth__Authority` (Keycloak realm issuer)
- `Storage__Endpoint`, `Storage__AccessKey`, `Storage__SecretKey`, `Storage__Bucket` (MinIO)
- `Database__ApplyMigrationsOnStartup=true` to apply EF Core migrations automatically on startup

### Frontend Runtime Configuration
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

> Note: for simplified local execution, audience/issuer validation may be relaxed in development configuration; in production, enable full validation and configure appropriate Keycloak client/realm mappings.

## Main MVP Endpoints

Indicative endpoints implemented by the MVP include:

### Prestazioni
- `GET /api/prestazioni`
- `POST /api/prestazioni` (staff)

### Slots
- `GET /api/slots`
- `POST /api/slots` (staff)

### Bookings
- `POST /api/bookings` (patient)
- `GET /api/bookings/my` (patient)
- `GET /api/bookings` (staff)
- `DELETE /api/bookings/{id}` (patient)
- `POST /api/bookings/{id}/complete` (staff)
- `POST /api/bookings/{id}/mark-paid` (staff)

### Reports
- `POST /api/reports/upload` (staff, `multipart/form-data`, PDF only)
- `GET /api/reports/my` (patient)
- `GET /api/reports` (staff)
- `GET /api/reports/{id}/download` (patient with ownership, or staff)

### Analytics
- `GET /api/analytics/economics` (staff)

## Development Workflow (Local)

### Backend (Run in IDE)
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

### Frontend (Angular Dev Server)
1. Ensure the API is running (local or Docker profile `full`)
2. Start Angular:
   ```bash
   cd frontend
   npm install
   npm start
   ```
The dev server uses a proxy configuration to forward `/api/*` to the backend.

### Backend Tests
Run the automated backend test suite locally:
```bash
dotnet test tests/MedCareHub.Api.Tests/MedCareHub.Api.Tests.csproj
```

## Validation and Evaluation Notes

Functional verification is designed around repeatable scenarios:
- Staff creates prestazioni and slots
- Slot overlap attempt for the same doctor is rejected
- Patient performs a single booking and attempts a double booking
- Staff completes a booking and marks it as paid
- Staff uploads a PDF report for a booking
- Patient downloads own report; access is denied for unauthorized users
- Dashboard consultation and audit log verification
- Full stack startup through Docker Compose with backend tests executed before the API in the `full` profile

## Limitations and Future Work

The MVP is intentionally focused on core workflows. Planned/possible extensions include:
- Expanded automated test coverage (integration/end-to-end)
- Advanced calendar features (recurrence, multi-doctor scheduling)
- Notification system (email/SMS)
- Real payment gateway integration
- Workflow approval/signature and qualified digital signature support
- Stronger database-level scheduling constraints (in addition to application-level validation)
- Production-grade hardening for secrets, TLS, monitoring, and deployment

## Academic Disclaimer

This project is a didactic and demonstrative implementation aimed at validating design and integration choices in an API-based architecture for the healthcare domain. It is not intended for production use without a dedicated security hardening phase, operational monitoring, and compliance verification (including but not limited to privacy and regulatory requirements).
