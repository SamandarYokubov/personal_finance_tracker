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
