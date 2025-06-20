# Dockerfile in TaskManagementAPI root folder
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files
COPY ./Task-Management-API/Task-Management-API.csproj Task-Management-API/
COPY ./Task-Management-Api.Application/Task-Management-API.Application.csproj Task-Management-Api.Application/
COPY ./Task-Management-API.Domain/Task-Management-API.Domain.csproj Task-Management-API.Domain/
COPY ./Task-Management-API.Infrastructure/Task-Management-API.Infrastructure.csproj Task-Management-API.Infrastructure/

# Restore
RUN dotnet restore Task-Management-API/Task-Management-API.csproj

# Copy the whole solution
COPY . .

# Build & publish
WORKDIR /src/Task-Management-API
RUN dotnet build Task-Management-API.csproj -c Release -o /app/build
RUN dotnet publish Task-Management-API.csproj -c Release -o /app/publish

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Task-Management-API.dll"]
