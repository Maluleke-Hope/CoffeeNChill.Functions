# CoffeeNChill - Azure Functions & Azurite Containerized API

## Overview
CoffeeNChill is a .NET 8 Isolated Worker Azure Functions application containerized using Docker and configured to run alongside Azurite for local storage emulation (Azure Table and Blob Storage).

## Public Docker Hub Images
- **Azure Functions API:** `malulekehope/coffeenchill-functions:v1.0`
- **Azurite Storage:** `malulekehope/coffeenchill-azurite:v1.0`

## Quick Start & Running the Containers

### 1. Run Azurite Storage Container
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 --name azurite malulekehope/coffeenchill-azurite:v1.0

### 2. Run CoffeeNChill Functions Container
docker run -d -p 7071:80 --name coffeenchill-api -e AzureWebJobsStorage="UseDevelopmentStorage=true" malulekehope/coffeenchill-functions:v1.0

## API Endpoints
- `POST /api/menu` - Create Menu Item (Table Storage)
- `GET /api/menu` - Retrieve All Menu Items (Table Storage)
- `GET /api/documents` - List Staff Documents (Blob Storage)

## Testing
Import the Postman collection located at `/docs/CoffeeNChill.postman_collection.json` into Postman and utilize the `{{baseUrl}}` variable set to `http://localhost:7071`.

## Video Demonstration
[Video Link Pending Upload]
