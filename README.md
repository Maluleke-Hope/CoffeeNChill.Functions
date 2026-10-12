# CoffeeNChill - Azure Functions & Azurite Containerized API

## 1. Project Overview

CoffeeNChill is a Canteen Management System developed using C#, .NET 8, Azure Functions and Azure Storage.

The application manages menu items, staff documents and customer orders using cloud-based storage services and serverless functions.

The project is divided into three parts:

- **Part 1:** Azure Functions, Azure Table Storage, staff document management and Docker.
- **Part 2:** Azure Queue Storage, asynchronous order processing, order status management and Docker Compose.
- **Part 3:** ASP.NET Core Web API Gateway, JWT authentication and deployment.

The application uses Azurite to emulate Azure Table, Blob and Queue Storage locally.

## 2. Technologies Used

- C# and .NET 8
- Azure Functions v4 (.NET Isolated Worker)
- Azure Table Storage
- Azure Queue Storage
- Azure Storage Files SDK
- Azurite Storage Emulator
- Docker Desktop
- Docker Compose
- Docker Hub
- Visual Studio
- Postman
- Microsoft Azure Storage Explorer
- Git and GitHub

## 3. Part 1: Menu Management and Staff Documents

### 3.1 Menu Management

The application uses Azure Table Storage to manage menu items through HTTP-triggered Azure Functions.

The following endpoints have been implemented:

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/menu` | Create a menu item |
| GET | `/api/menu` | Retrieve all menu items |
| GET | `/api/menu/{category}/{sku}` | Retrieve a specific menu item |
| GET | `/api/menu/category/{category}` | Retrieve items by category |
| PUT | `/api/menu/{category}/{sku}` | Update a menu item |
| DELETE | `/api/menu/{category}/{sku}` | Delete a menu item |

During Part 2 improvements, duplicated Menu Function registrations were removed to prevent conflicting Azure Function endpoints.

### 3.2 Staff Document Management

The project contains Azure File Share functionality for uploading and retrieving staff documents.

The registered endpoints include:

- `POST /api/staff-documents`
- `GET /api/staff-documents/{staffName}/{fileName}`

The Staff Document functionality requires further integration verification because Azure File Shares are not supported by the Azurite emulator.

### 3.3 Part 1 Docker Hub Images

The original Part 1 Docker Hub images were:

- Azure Functions API: `malulekehope/coffeenchill-functions:v1.0`
- Azurite Storage: `malulekehope/coffeenchill-azurite:v1.0`

These references are retained for documentation of the original Part 1 implementation.

## 4. Part 2: Azure Queue Storage and Asynchronous Order Processing

### 4.1 Overview

Part 2 introduces asynchronous customer order processing through Azure Queue Storage.

Instead of completing order processing within the initial HTTP request, the application queues the order and allows a background Azure Function to process it.

This separates order submission from background processing.

The application uses the queue:

`order-processing-queue`

### 4.2 Order Processing Architecture

The complete order-processing workflow is:

1. A customer submits an order through an HTTP POST request.
2. The Order Producer validates the submitted JSON.
3. A unique OrderId and UTC OrderTimestamp are generated.
4. The order is serialized and sent to Azure Queue Storage.
5. The queue-triggered processor automatically detects the message.
6. The processor validates and saves the order into Azure Table Storage.
7. The order is recorded with the initial Received status.
8. The processor changes the status to Preparing.
9. The Order Status Update Function allows the status to advance to Ready.
10. A subsequent valid request advances the order to Collected.
11. Azure Table Storage maintains the current order information and status history.

The complete lifecycle is:

**Received → Preparing → Ready → Collected**

## 5. HTTP Order Producer

### 5.1 Function Details

**Function name:** `QueueOrder`

**File:** `Functions/OrderProducerFunction.cs`

**HTTP method:** POST

**Endpoint:** `/api/orders/queue`

The Order Producer is responsible for:

- Receiving JSON order requests.
- Validating customer information.
- Verifying selected menu item SKUs.
- Rejecting empty item lists and invalid total prices.
- Generating a unique OrderId using a GUID.
- Generating a UTC OrderTimestamp.
- Serializing the order into JSON.
- Sending the order to Azure Queue Storage.
- Returning HTTP 202 Accepted after successful queue placement.
- Returning HTTP 400 Bad Request for invalid input.
- Logging processing errors.

### 5.2 Example Order Request

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

### 5.3 Order Message Structure

The order message contains five required fields:

| Field | Description |
|---|---|
| OrderId | Unique GUID generated for each order |
| CustomerName | Name of the customer |
| SelectedItemSKUs | List of selected menu item codes |
| TotalPrice | Total cost of the order |
| OrderTimestamp | UTC date and time of order creation |

Azure Queue Storage is used to store the message until the background processor retrieves it.

### 5.4 Azure Queue Storage Configuration

The application registers `QueueServiceClient` in `Program.cs` using the `AzureWebJobsStorage` connection string.

During local Visual Studio testing, the storage connection is configured using:

`UseDevelopmentStorage=true`

Azurite provides local Azure Queue Storage functionality.

The local queue endpoint is:

`http://127.0.0.1:10001/devstoreaccount1/order-processing-queue`

The Order Producer creates the queue if it does not already exist.

## 6. Queue-Triggered Order Processor

### 6.1 Function Details

**Function name:** `ProcessOrderQueue`

**File:** `Functions/ProcessOrderQueue.cs`

**Trigger:** Azure Storage Queue Trigger

**Queue:** `order-processing-queue`

**Status:** Implemented and successfully tested locally.

The processor automatically executes when an order message becomes available in Azure Queue Storage.

### 6.2 Processing Responsibilities

The processor performs the following operations:

1. Receives the queued message.
2. Deserializes the JSON message into the OrderQueueMessage model.
3. Validates the order ID, customer name, selected SKUs, price and timestamp.
4. Connects to the Orders Azure Table.
5. Creates the table if it does not exist.
6. Uses the UTC order date as the PartitionKey.
7. Uses OrderId as the RowKey.
8. Stores the customer's name, selected items, price and timestamp.
9. Records the Received status.
10. Updates the order to Preparing.
11. Logs successful processing and errors.

### 6.3 Azure Orders Table Structure

The Orders Table stores the following information:

| Property | Description |
|---|---|
| PartitionKey | UTC order date in yyyy-MM-dd format |
| RowKey | Unique OrderId |
| CustomerName | Customer's name |
| ItemsJson | Selected item SKUs serialized as JSON |
| TotalPrice | Total price of the order |
| OrderTimestamp | Original UTC order creation timestamp |
| Status | Current order status |
| StatusHistory | Recorded status progression |
| Timestamp | Azure Table Storage system metadata |
| ETag | Azure Table Storage concurrency metadata |

### 6.4 Queue Processor Testing

Local integration testing was performed using Visual Studio, Postman, Azurite and Microsoft Azure Storage Explorer.

Testing confirmed that:

- The HTTP Order Producer successfully queued an order.
- The queue-triggered processor detected the new message.
- The processor successfully deserialized the order.
- A new entity was created in the Orders Azure Table.
- The correct date-based PartitionKey was stored.
- The RowKey matched the generated order ID.
- CustomerName and selected items were stored correctly.
- The order advanced from Received to Preparing.

The Azure Functions console confirmed successful queue-trigger execution without processing errors during the verified test.

## 7. Order Status Management

### 7.1 Update Order Status Function

**Function name:** `UpdateOrderStatus`

**File:** `Functions/UpdateOrderStatusFunction.cs`

**HTTP method:** PUT

**Endpoint:** `/api/orders/{orderDate}/{orderId}/status`

The UpdateOrderStatus Function allows existing orders to advance through their lifecycle.

The function retrieves the order using its date-based PartitionKey and OrderId RowKey.

It validates the requested status before updating the existing Azure Table Storage entity.

### 7.2 Valid Status Transitions

| Current status | Permitted next status |
|---|---|
| Received | Preparing, handled by the processor |
| Preparing | Ready |
| Ready | Collected |
| Collected | No further transitions |

The HTTP status-update endpoint handles the Ready and Collected transitions.

Invalid transitions are rejected to preserve the correct order lifecycle.

### 7.3 Example Status Update Request

**Method:** PUT

**Endpoint:**

`/api/orders/2026-10-12/{orderId}/status`

**Request body:**

```json
{
  "Status": "Ready"
}
```

### 7.4 HTTP Responses

| Scenario | HTTP status |
|---|---|
| Successful status update | 200 OK |
| Invalid order date or ID | 400 Bad Request |
| Invalid JSON | 400 Bad Request |
| Unsupported requested status | 400 Bad Request |
| Order not found | 404 Not Found |
| Invalid status transition | 409 Conflict |
| Concurrent update conflict | 409 Conflict |

### 7.5 Status History

The application maintains a StatusHistory property for each order.

A successfully completed order has the following history:

`Received -> Preparing -> Ready -> Collected`

This makes it possible to verify the order's progression during testing.

### 7.6 Order Lifecycle Testing

The complete order lifecycle was successfully tested locally using Postman.

The following results were verified:

| Test | Expected response | Verified result |
|---|---|---|
| Create order | 202 Accepted | Passed |
| Preparing to Ready | 200 OK | Passed |
| Ready to Collected | 200 OK | Passed |
| Collected back to Ready | 409 Conflict | Passed |

The final order status was also verified as Collected in Microsoft Azure Storage Explorer.

## 8. Postman API Testing

### 8.1 Postman Collection

The final Part 2 Postman collection is stored at:

`docs/CoffeeNChill-Part2-Final.postman_collection.json`

The collection contains automated tests for:

- Valid order submission.
- Invalid JSON.
- Missing customer name.
- Invalid total price.
- Empty selected item list.
- Updating order status to Ready.
- Updating order status to Collected.
- Rejecting invalid order status transitions.

### 8.2 Automated Test Results

The successful Order Producer request passed six automated Postman assertions.

The Ready status update was tested successfully using HTTP 200 OK.

The Collected status update was tested successfully using HTTP 200 OK.

The invalid transition test returned the expected HTTP 409 Conflict with three passing Postman assertions.

### 8.3 Reusable Postman Variables

The collection uses the following variables:

| Variable | Description |
|---|---|
| baseUrl | Base address of the Azure Functions API |
| orderId | Generated OrderId from the successful POST response |
| orderDate | Date extracted from the generated OrderTimestamp |

The successful POST request automatically stores orderId and orderDate using an After response script.

Subsequent status-update requests use:

`{{baseUrl}}/api/orders/{{orderDate}}/{{orderId}}/status`

This allows new orders to be tested without manually entering a new order ID each time.

### 8.4 Recommended Test Execution Order

To test the full lifecycle on a new order, run the requests in the following sequence:

1. Queue Order - Success.
2. Update Order Status - Ready.
3. Update Order Status - Collected.
4. Update Order Status - Invalid Transition.

The queued order must first be processed into the Orders Table before the status-update requests execute.

Because queue processing is asynchronous, a short wait or retry may be necessary before the Ready update.

### 8.5 Postman Base URL Configuration

For local testing through Visual Studio:

`baseUrl = http://localhost:7071`

For Docker Compose testing, when mapped to host port 8080:

`baseUrl = http://localhost:8080`

## 9. Azure Storage Explorer Verification

Microsoft Azure Storage Explorer was used to inspect the local Azurite storage services.

The verification confirmed:

- The order-processing-queue was created successfully.
- Order messages were stored in Azure Queue Storage.
- The queue message contained the required order fields.
- The queue-triggered processor retrieved and processed order messages.
- The Orders Azure Table was created.
- New orders were saved using the date-based PartitionKey.
- Customer names and menu item SKUs were correctly stored.
- Status changes were persisted in Azure Table Storage.
- The final Collected status was successfully verified.

These results demonstrate successful local integration between the HTTP Order Producer, Azure Queue Storage, queue-triggered processor, status-update endpoint and Orders Table.

## 10. Local Development Setup

### 10.1 Prerequisites

To run the project locally, the following software is required:

- Visual Studio with .NET development support.
- .NET 8 SDK.
- Azure Functions Core Tools.
- Docker Desktop.
- Postman.
- Microsoft Azure Storage Explorer.
- Git.

### 10.2 Start Azurite

Start Docker Desktop.

If an existing Azurite container is available, start it using Docker Desktop or the command:

```powershell
docker start coffeenchill-azurite
```

Alternatively, create a standard Azurite container:

```powershell
docker run -d --name coffeenchill-azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

Do not create a second container with the same name or use the same host ports while another Azurite instance is already running.

### 10.3 Configure Local Settings

Create or update `local.settings.json` with the following local development configuration:

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

The local settings file is excluded from Git tracking through `.gitignore`.

### 10.4 Start Azure Functions

Open the project in Visual Studio.

Select:

**Debug → Start Debugging**

The HTTP functions are hosted locally on port 7071.

Examples include:

- `POST http://localhost:7071/api/orders/queue`
- `PUT http://localhost:7071/api/orders/{orderDate}/{orderId}/status`

The ProcessOrderQueue function runs automatically when an order message arrives in Azure Queue Storage.

## 11. Docker and Docker Compose

### 11.1 Docker Overview

Docker is used to package the application and its dependencies into a container image.

Docker Compose is used to orchestrate the Azure Functions application and the Azurite storage emulator.

### 11.2 Dockerfile

The Dockerfile uses a .NET 8 SDK build stage and an Azure Functions .NET 8 isolated runtime stage.

The build stage restores dependencies and publishes the application.

The runtime stage copies the published files into the Azure Functions container.

### 11.3 Local Docker Image Build

The application was successfully built locally using:

```powershell
docker build -t coffeenchill-functions:v2.0 .
```

This confirmed that Docker could build the CoffeeNChill .NET 8 application image.

### 11.4 Docker Compose Configuration

The project includes a Compose configuration file:

`compose.yml`

The Compose deployment is intended to run:

- The Azure Functions application.
- The Azurite Storage Emulator.

The configuration supplies the storage connection settings and maps the API to host port 8080.

The functions container must use Docker's internal service hostname to connect to Azurite instead of localhost.

### 11.5 Running Docker Compose

From the folder containing `compose.yml`, validate the configuration using:

```powershell
docker compose -f compose.yml config
```

Start the containers using:

```powershell
docker compose -f compose.yml up --build
```

Check the container status using:

```powershell
docker compose -f compose.yml ps
```

For a detached deployment, use:

```powershell
docker compose -f compose.yml up -d --build
```

The project's containerized API is configured to be accessed through:

`http://localhost:8080`

The exact Compose configuration, ports and image references must be checked against the final working deployment.

### 11.6 Containerized Integration Status

The project partner reported successfully verifying:

- Docker Compose startup.
- The Azure Functions application running on port 8080.
- Azurite Queue and Table Storage connectivity.
- Successful POST order submission.
- HTTP 202 Accepted responses.
- Automatic queue processing.
- Orders Table persistence.

Final joint verification is still required for the Ready and Collected status-update endpoints through the containerized deployment.

### 11.7 Docker Hub Publishing

The original Part 1 Docker Hub images were published under:

`malulekehope/coffeenchill-functions:v1.0`

and

`malulekehope/coffeenchill-azurite:v1.0`

The final Part 2 application image and published tag must be confirmed with the project partner before submission.

The locally built image `coffeenchill-functions:v2.0` is not, by itself, proof of Docker Hub publication.

## 12. GitHub Version Control

The project uses Git and GitHub for collaborative development and version control.

Each group member is required to contribute at least five meaningful commits per assignment part under their own Git identity.

The repository history records:

- Azure Queue Storage Order Producer implementation.
- Postman order validation testing.
- Order Producer schema improvements.
- Part 2 README documentation.
- Order lifecycle integration fixes.
- Final Postman collection and automated lifecycle testing.
- Partner Docker and repository improvements.

Both group members' contributions are visible through Git history.

## 13. Group Contributions

### 13.1 Sothoane-Mamello

Responsibilities and contributions:

- Removed duplicated Menu Functions.
- Implemented the HTTP Order Producer.
- Registered Azure Queue Storage integration.
- Added JSON validation and error handling.
- Generated unique order IDs and timestamps.
- Created and tested Postman requests.
- Verified queue messages through Azure Storage Explorer.
- Corrected order message and entity mapping during integration.
- Implemented the UpdateOrderStatus HTTP Function.
- Tested the complete order lifecycle.
- Tested invalid status transitions.
- Added reusable Postman collection variables.
- Exported and committed the final Postman testing collection.
- Updated Part 2 documentation.

### 13.2 Partner

Responsibilities and contributions:

- Dockerfile implementation and maintenance.
- Docker Compose configuration.
- Azurite container networking.
- Queue-triggered processor implementation.
- Orders Azure Table integration.
- Error handling and logging for background processing.
- Containerized integration testing.
- Docker Hub image management and publishing.
- Repository and deployment documentation.
- Final demonstration video preparation.

### 13.3 Shared Responsibilities

Both group members are responsible for:

- Reviewing the integration between the producer and processor.
- Verifying order message compatibility.
- Testing the complete order lifecycle.
- Reviewing Docker deployment results.
- Maintaining meaningful GitHub contributions.
- Checking deliverables against the assignment rubric.
- Providing explanations of their individual work for the final demonstration.

## 14. Part 2 Development Changelog

The following improvements were completed during Part 2 development:

1. Removed duplicate Azure Menu Function registrations.
2. Registered Azure Queue Storage in Program.cs.
3. Implemented the HTTP Order Producer.
4. Added validation for customer details, selected menu items and total price.
5. Generated unique order identifiers and UTC timestamps.
6. Added asynchronous message placement in Azure Queue Storage.
7. Updated the queue message model to match the Part 2 specification.
8. Corrected Orders Table data mapping.
9. Implemented date-based PartitionKey and OrderId RowKey storage.
10. Verified queue-triggered processing.
11. Added Received and Preparing status processing.
12. Implemented HTTP status updates for Ready and Collected.
13. Added validation for invalid status transitions.
14. Implemented order status history tracking.
15. Tested the complete local order lifecycle.
16. Added reusable Postman collection variables.
17. Exported the final automated Postman collection.
18. Updated technical documentation.

## 15. Demonstration Video

**Part 1:** Demonstration included in the original group submission.

**Part 2:** Pending final recording and upload.

The final Part 2 demonstration should show:

1. Starting the application through Docker Compose.
2. The Azure Functions application and Azurite containers running.
3. Submitting a new order using Postman.
4. Receiving HTTP 202 Accepted.
5. Confirming automatic queue-triggered processing.
6. Verifying the order in Azure Table Storage.
7. Updating the order to Ready.
8. Updating the order to Collected.
9. Demonstrating invalid status-transition validation.
10. Explaining the technical implementation and individual contributions.

**Part 2 Video Link:** Pending upload.

The final demonstration video link will be added before submission.

## 16. Final Verification Status

| Component | Status |
|---|---|
| Menu HTTP Functions | Implemented |
| Duplicate Menu Function correction | Completed |
| HTTP Order Producer | Implemented and tested |
| Order validation | Tested |
| Azure Queue Storage message placement | Verified |
| Queue-triggered processor | Locally verified |
| Orders Table integration | Locally verified |
| Date-based PartitionKey | Verified |
| Customer and selected items persistence | Verified |
| Preparing → Ready | Verified |
| Ready → Collected | Verified |
| Invalid status-transition rejection | Verified |
| Reusable Postman order variables | Tested |
| Final Postman collection | Exported and committed |
| Local Docker image build | Successful |
| Docker Compose deployment | Partner reports successful |
| Containerized status lifecycle | Awaiting joint verification |
| Final Docker Hub publication | Awaiting confirmation |
| GitHub individual commit requirement | Verified for Order Producer contributor |
| Part 2 demonstration video | Pending |

## 17. Conclusion

CoffeeNChill Part 2 successfully introduces asynchronous order processing using Azure Functions and Azure Storage Queue.

Local testing confirms that orders can be submitted, queued, processed, saved in Azure Table Storage and advanced through the required order lifecycle.

The application also rejects invalid order requests and invalid status transitions.

The final Postman collection provides reusable automated tests for order submission and order management.

The remaining final submission activities include confirming the complete Docker Compose integration, verifying Docker Hub publication, reviewing the partner's deployment results and uploading the demonstration video.