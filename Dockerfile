FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["ST10467898CLDV6212POE/ST10467898CLDV6212POE.csproj", "ST10467898CLDV6212POE/"]
RUN dotnet restore "ST10467898CLDV6212POE/ST10467898CLDV6212POE.csproj"

# Copy full repository source and publish
COPY . .
WORKDIR "/src/ST10467898CLDV6212POE"
RUN dotnet publish "ST10467898CLDV6212POE.csproj" -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__logging__console__isEnabled=true