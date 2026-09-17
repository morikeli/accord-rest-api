# Accord API

## Overview
This repository contains a two-service backend for processing and managing ACORD-based intake data. It combines a .NET API for XML intake and persistence with a Django REST API for authentication, schema documentation, and admin-facing endpoints, all backed by a shared PostgreSQL database.

The project is designed for teams working with insurance and financial data interchange, where ACORD XML payloads need to be validated, stored, retrieved, and managed through a modern API stack.

### Key features
- 🔐 **JWT-based authentication** via Django REST Framework and SimpleJWT
- 📄 **ACORD XML intake processing** in the .NET service
- 💾 **PostgreSQL persistence** for stored records and XML workflows
- 🧾 **Schema and API documentation** through DRF Spectacular (Swagger and Redoc)
- 🧩 **Modular multi-service architecture** with separate .NET and Django services
- 🐳 **Dockerized local development** using Docker Compose

### Tech Stack
- .NET 10 / ASP.NET Core
- Django 6.1 / Django REST Framework
- PostgreSQL 16
- Docker + Docker Compose
- Python 3.14 + uv

#### .NET packages
- Npgsql

#### Django packages
- djangorestframework
- drf-spectacular
- django-cors-headers
- django-environ
- psycopg

### Product thinking
The project aims to bridge legacy ACORD-based data exchange with a modern application architecture.
- the .NET service handles XML intake and record processing
- the Django app mimics the workflow of .NET app, exposes API documentation, and JWT-authenticated endpoints
- the database acts as the shared source of truth

This structure keeps the solution easier to evolve, easier to test, and easier to run consistently across development environments.

## Developer instructions
---
> [!WARNING]
> The recommended way to run this project is with Docker Compose. If you use Docker Compose only, you do not need to install .NET, Python, or uv on your machine.
>
> Requirements for Docker-based setup:
> - Docker Desktop(or Docker Engine or Docker Desktop CLI) + Compose
>
---

#### Prerequisites

1. Install Docker
- Download and install Docker Desktop for your OS:
  - [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Ensure Docker is running before starting the app.

2. Create the environment file
- Copy the example environment file:
```bash
cp .env.example .env
```
- Update the values in `.env` before running the services.

Example:
```env
DB_USER=postgres
DB_NAME=accord
DB_PASSWORD=your-db-admin-password-here
DB_PORT=5434
DB_HOST=localhost
SECRET_KEY=your-secret-key-here
```

> [!TIP]
> For Docker Compose, the app containers use the host name `postgres` internally. For non-Docker local development, use `localhost` and the mapped local port.

---

#### Run the project with Docker (recommended)

From the root of the repository:

```bash
docker compose up --build
```

This will start:
- PostgreSQL on `localhost:5434`
- .NET API on `http://localhost:5173`
- Django API on `http://localhost:8001`

Useful commands:
```bash
docker compose up -d

docker compose logs -f

docker compose down

docker compose down -v   # removes database volumes too
```

#### Service endpoints
- .NET health check: `http://localhost:5173/health`
- .NET XML intake endpoint: `http://localhost:5173/intake/xml`
- Django Swagger docs: `http://localhost:8001/api/docs/`
- Django admin: `http://localhost:8001/admin/`

---

#### Database setup
The project uses PostgreSQL and includes a startup SQL file at:
- `db/init.sql`

When running via Docker Compose, the database is created automatically from the environment variables in `.env`.

---

## 🤝 Contributor expectations
If you want to contribute to the project:
1. Create a new branch from the main branch
2. Make the relevant changes
3. Run the service checks locally
4. Open a pull request and wait for review

Please keep environment configuration in `.env` and do not commit production secrets.

## 🙏 Request
If this project helps your workflow, consider giving the repo a star and sharing useful feedback.

## 🐞 Known issues
1. Local environment values must be set correctly in `.env` before running the Django app.
2. Some service endpoints may vary slightly depending on whether you run the apps through Docker or directly on your machine.
3. The current setup is intended for development workflows and not hardened for production deployment.
