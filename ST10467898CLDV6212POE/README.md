# CoffeeNChill - Cloud Development POE Part 3

## Live Public URL
The application is hosted live and can be accessed at:
**Swagger UI:** [https://eedb360259449245-20-164-129-206.serveousercontent.com/swagger](https://eedb360259449245-20-164-129-206.serveousercontent.com/swagger)

*(Note: This URL is served via an SSH tunnel to the running Docker containers. The video demonstration shows the application running via `docker-compose up` and successfully handling requests).*

## Architecture
1. **API Gateway (ASP.NET Core Web API):** Acts as the public entry point. Handles JWT authentication, password hashing (BCrypt), and Role-Based Access Control (RBAC). Routes requests to the backend Azure Functions.
2. **Azure Functions (.NET 8 Isolated):** Handles backend business logic (Menu Management, Document Uploads, Order Queuing).
3. **Azurite (Azure Storage Emulator):** Emulates Azure Tables (Users, Orders), Queues (order-processing-queue), and Blobs.
4. **Docker Compose:** Orchestrates all three services into a custom bridge network (`coffeenchill-network`), ensuring they can communicate internally while exposing only the Gateway to the host.

## Technologies Used
* C# / .NET 8
* ASP.NET Core Web API
* Azure Functions (Isolated Worker)
* Azure Table Storage, Queue Storage
* JWT Authentication & BCrypt
* Docker & Docker Compose
* Swagger / OpenAPI

## Setup Instructions (Local)
1. Clone the repository.
2. Ensure Docker Desktop is running.
3. Run the following command in the root directory:
   ```bash
   docker compose up -d