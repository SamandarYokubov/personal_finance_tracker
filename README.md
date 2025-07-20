# Personal Finance Tracker Microservices

## Overview
This project is a microservices-based personal finance tracker built with .NET 8, PostgreSQL, Redis, Ocelot API Gateway, and Docker Compose. It features:
- Authentication & Role Management
- Category, Transaction, Audit, and Statistics Services
- Role-based JWT Authorization
- Redis Caching
- Unified Swagger UI at the API Gateway

## Prerequisites
- [Docker](https://www.docker.com/products/docker-desktop)
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (for local development/testing)

## Running the Application

1. **Clone the repository**

2. **Start all services with Docker Compose:**
   ```sh
   docker compose up --build
   ```
   This will build and start all microservices, databases, Redis, and the API Gateway.

3. **Access the services:**
   - **API Gateway & Unified Swagger UI:** [http://localhost:5000/swagger](http://localhost:5000/swagger)
   - **Auth Service:** [http://localhost:5001/swagger](http://localhost:5001/swagger)
   - **Category Service:** [http://localhost:5002/swagger](http://localhost:5002/swagger)
   - **Finance Service:** [http://localhost:5003/swagger](http://localhost:5003/swagger)
   - **Audit Service:** [http://localhost:5004/swagger](http://localhost:5004/swagger)
   - **Statistics Service:** [http://localhost:5005/swagger](http://localhost:5005/swagger)

4. **Default Environment Variables** (see `docker-compose.yml`):
   - PostgreSQL: user `postgres`, password `postgres`
   - Redis: `redis:6379`
   - JWT Secret: `YourSuperSecretKey123!` (change in production!)

## Features
- **Role-based Access:** Only Admins can access sensitive endpoints (delete, audit, statistics, etc.)
- **Caching:** Redis caching for frequently accessed endpoints
- **Audit Logging:** All CRUD actions are logged
- **Statistics:** Analytics and Excel export endpoints
- **Pagination, Filtering, Sorting:** On all main list endpoints

## Useful Endpoints
- **Register/Login:** `/api/user` (Auth Service)
- **Categories:** `/api/category` (Category Service)
- **Transactions:** `/api/transaction` (Finance Service)
- **Audit Logs:** `/api/auditlog` (Audit Service, Admin only)
- **Statistics:** `/api/statistics` (Statistics Service, Admin only)

## Running Tests
Navigate to the `tests/` folder and run:
```sh
dotnet test
```

## Notes
- Update the JWT secret in all services for production use.
- For local development, you can use the Swagger UI to try out endpoints and see the OpenAPI docs.
- All services are orchestrated via Docker Compose for easy local development and testing.

---

Enjoy your personal finance tracker microservices platform! 