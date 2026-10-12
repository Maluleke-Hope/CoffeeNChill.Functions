# CoffeeNChill - Azure Functions & Azurite Containerized API

## Project Overview

CoffeeNChill is a Canteen Management System developed using C#, .NET 8, Azure Functions and Azure Storage.

The system helps manage menu items, staff documents and customer orders.

The project is divided into three parts:

- **Part 1:** Azure Functions, Azure Table Storage, staff document management and Docker.
- **Part 2:** Azure Queue Storage, asynchronous order processing and Docker Compose.
- **Part 3:** ASP.NET Core Web API Gateway, JWT authentication and live deployment.

The application uses Azurite to emulate Azure Tables, Blobs and Queues locally.

## Technologies Used

- C# and .NET 8
- Azure Functions v4 (Isolated Worker)
- Azure Table Storage
- Azure Queue Storage
- Azure Storage Files SDK
- Azurite Storage Emulator
- Docker Desktop
- Docker Hub
- Visual Studio
- Postman
- Microsoft Azure Storage Explorer
- Git and GitHub

## Part 1: Menu Management and Staff Documents

### Menu Management

The application uses Azure Table Storage to manage menu items through HTTP-triggered Azure Functions.

Implemented endpoints:

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/menu` | Create a menu item |
| GET | `/api/menu` | Retrieve all menu items |
| GET | `/api/menu/{category}/{sku}` | Retrieve a specific menu item |
| GET | `/api/menu/category/{category}` | Retrieve items by category |
| PUT | `/api/menu/{category}/{sku}` | Update a menu item |
| DELETE | `/api/menu/{category}/{sku}` | Delete a menu item |

The duplicated Menu Functions identified during Part 1 assessment were removed.

### Staff Document Management

The project contains Azure File Share functionality for staff documents, including upload and retrieval methods.

The registered routes include:

- `POST /api/staff-documents`
- `GET /api/staff-documents/{staffName}/{fileName}`

The existing functionality requires further integration verification because Azure File Shares are not supported by the local Azurite emulator.

### Part 1 Improvements

Following lecturer feedback, the following improvement was completed:

- Removed duplicated Menu Functions to prevent conflicting function registrations.

Additional improvements to Staff Document integration remain under review.

## Docker Hub Images

The Part 1 project references the following Docker Hub images:

- Azure Functions API: `malulekehope/coffeenchill-functions:v1.0`
- Azurite Storage: `malulekehope/coffeenchill-azurite:v1.0`

These image references are retained from Part 1 documentation. Docker Hub publishing and execution must be verified again for the final Part 2 demonstration.

## Running the Application Locally

### 1. Start Azurite

Start Docker Desktop and run the existing Azurite container.

Example command for an existing container:

```powershell
docker start coffeenchill-azurite
```

Alternatively, a new standard Azurite container can be created with:

```powershell
docker run -d --name coffeenchill-azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

Do not create another container if one with the same name already exists.

### 2. Configure Local Settings

The local `local.settings.json` file uses the following configuration:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "FUNCTIONS_WORKER_RUNTIME_VERSION": "8.0"
  }
}
```

This file is excluded from Git tracking to prevent local settings and possible credentials from being committed.

### 3. Start Azure Functions

Open the solution in Visual Studio.

Select:

**Debug → Start Debugging**

The application runs locally using the Azure Functions host.

The order endpoint is available at:

`http://localhost:7071/api/orders/queue`

## Part 2: Azure Queue Storage and Asynchronous Orders

### Overview

Part 2 introduces asynchronous order processing to the CoffeeNChill system.

Instead of processing a customer order entirely during an HTTP request, the application places an order message into Azure Queue Storage.

A queue-triggered Azure Function is responsible for retrieving and processing the messages.

### Order Processing Architecture

The intended workflow is:

1. A customer submits an order using HTTP POST.
2. The Order Producer validates the request.
3. The producer generates a unique order ID and timestamp.
4. The order is sent to Azure Queue Storage.
5. The queue-triggered function processes the message.
6. The order is recorded in Azure Table Storage.
7. The order progresses through its preparation statuses.

The queue is named:

`order-processing-queue`

### Order Producer

**Function name:** `QueueOrder`

**Method:** POST

**Endpoint:** `/api/orders/queue`

The Order Producer is implemented in:

`Functions/OrderProducerFunction.cs`

The producer:

- Accepts JSON order requests.
- Validates customer names.
- Validates selected menu item SKUs.
- Rejects invalid total prices.
- Generates a unique OrderId using a GUID.
- Generates a UTC OrderTimestamp.
- Serializes the order into JSON.
- Sends the message to Azure Queue Storage using Base64 encoding.
- Returns HTTP 202 Accepted when the message is queued successfully.
- Returns HTTP 400 Bad Request for invalid order information.
- Logs queue submission failures.

### Example Order Request

```json
{
  "CustomerName": "Jane Smith",
  "SelectedItemSKUs": [
    "COF-001",
    "PAS-104"
  ],
  "TotalPrice": 65.00
}
```

### Order Message Structure

The Azure Storage Queue message contains:

| Field | Description |
|---|---|
| OrderId | Unique GUID identifying the order |
| CustomerName | Customer placing the order |
| SelectedItemSKUs | Selected menu item codes |
| TotalPrice | Total cost of the order |
| OrderTimestamp | UTC timestamp of order creation |

### Queue Storage Configuration

The application registers `QueueServiceClient` in `Program.cs` using the `AzureWebJobsStorage` connection string.

Azurite emulates Azure Queue Storage locally.

The queue uses the endpoint:

`http://127.0.0.1:10001/devstoreaccount1/order-processing-queue`

The Order Producer creates the queue if it does not already exist.

### Order Producer Testing

The Order Producer was tested using Postman.

The following scenarios were covered:

| Test | Expected Status |
|---|---|
| Valid order | 202 Accepted |
| Invalid JSON | 400 Bad Request |
| Missing CustomerName | 400 Bad Request |
| Invalid TotalPrice | 400 Bad Request |
| Empty SelectedItemSKUs | 400 Bad Request |

The successful order submission passed six automated Postman assertions.

The invalid-order scenarios were also tested using automated Postman assertions.

### Azure Storage Explorer Verification

Microsoft Azure Storage Explorer was used to inspect the queue.

The verification confirmed:

- The `order-processing-queue` exists.
- The producer successfully places messages into Azure Queue Storage.
- Messages contain the required order fields.
- The message can be viewed as decoded JSON.
- The tested message had a dequeue count of zero before processor integration.

This confirms that the producer can place orders into the queue.

### Postman Collection

The Part 2 Postman collection is stored in the repository's `docs` folder.

Collection filename:

`CoffeeNChill-Part2-Postman-Collection.json`

The collection uses the reusable variable:

`{{baseUrl}}`

For local testing:

`baseUrl = http://localhost:7071`

The collection contains requests for successful order submission and invalid-order scenarios.

## Queue-Triggered Order Processor

**Integration status: Pending verification**

The queue-triggered processor is responsible for:

1. Automatically triggering when a new order enters `order-processing-queue`.
2. Reading and deserializing the queue message.
3. Creating an order record in the Azure Orders Table.
4. Using the order date as PartitionKey.
5. Using OrderId as RowKey.
6. Tracking the order through its preparation lifecycle.
7. Logging processing results and handling errors.

The required order statuses are:

Received → Preparing → Ready → Collected

The producer and processor must use the same JSON field structure and compatible queue message encoding.

The complete end-to-end workflow will be verified during group integration.

## Docker Compose Integration

**Status: Pending group verification**

Part 2 requires a `docker-compose.yml` file to orchestrate the application's containers.

The required services are:

- Azurite Storage Emulator
- CoffeeNChill Azure Functions application

The final Docker Compose configuration must support:

- Docker Hub image references.
- Correct container networking.
- Environment variables.
- Persistent Azurite storage.
- Reliable container startup.
- The `docker-compose up` startup command required by the assignment.

The final configuration and verified startup instructions will be documented after integration.

## GitHub Version Control

GitHub is used to maintain the project source code and record individual contributions.

Each group member must make at least five meaningful commits per assignment part, using their own Git identity.

Git commit history is used to verify development progress and individual participation.

## Group Responsibilities

### Sothoane-Mamello

Responsible for:

- Removing duplicated Menu Functions.
- Implementing the HTTP Order Producer.
- Configuring Azure Queue Storage integration.
- Implementing order validation and error handling.
- Creating and running Postman tests.
- Verifying messages using Azure Storage Explorer.
- Maintaining Part 2 API testing documentation.

### Partner

Responsible for:

- Dockerfile development and improvements.
- Docker Compose configuration.
- Queue-triggered Azure Function.
- Azure Orders Table integration.
- Order lifecycle status tracking.
- Docker Hub publishing.
- Container orchestration.

### Shared Responsibilities

Both group members are responsible for:

- Integration testing.
- Maintaining GitHub contributions.
- Reviewing the README.
- Testing the complete order workflow.
- Recording the final demonstration video.
- Checking deliverables against the marking rubric.

## Part 2 Changelog

- Removed duplicate menu function registrations.
- Registered Azure Queue Storage in `Program.cs`.
- Created the HTTP-triggered Order Producer.
- Added unique order identifiers and timestamps.
- Improved request validation and error handling.
- Updated the queue message format to match the assignment requirements.
- Created automated Postman validation tests.
- Verified queue message placement using Azure Storage Explorer.
- Added Part 2 documentation.

## Demonstration Video

Part 1 video: Available through the group's original submission.

Part 2 video: Pending recording and upload.

The final Part 2 demonstration must show the application running through Docker Compose, order submission in Postman, queue processing, order tracking in Azure Table Storage, and an explanation of the implementation and design decisions.

The final unlisted YouTube link will be added here before submission.

## Final Verification Status

| Component | Status |
|---|---|
| Menu HTTP Functions | Implemented |
| Order Producer | Implemented and tested |
| Order validation | Tested |
| Azure Queue message placement | Verified |
| Postman producer tests | Completed |
| Queue-triggered processor | Awaiting integration |
| Orders Table status tracking | Awaiting verification |
| Docker Compose | Awaiting verification |
| Full end-to-end testing | Pending |
| Part 2 demonstration video | Pending |

This README will be updated as the remaining group functionality is completed and verified.