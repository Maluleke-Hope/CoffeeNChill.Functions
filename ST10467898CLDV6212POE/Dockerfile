FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the entire solution folder into the container
COPY . .

# Restore dependencies for the project
RUN dotnet restore "ST10467898CLDV6212POE/ST10467898CLDV6212POE.csproj"

# Publish the application
WORKDIR "/src/ST10467898CLDV6212POE"
RUN dotnet publish -c Release -o /app/publish

# Runtime image for Azure Functions Isolated Worker
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS runtime
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    WEBSITE_RUN_FROM_PACKAGE=1