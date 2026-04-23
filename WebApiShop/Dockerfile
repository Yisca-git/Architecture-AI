# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["WebApiShop/EventDressRental.csproj", "WebApiShop/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["Repositories/Repositories.csproj", "Repositories/"]
COPY ["Entities/Entities.csproj", "Entities/"]
COPY ["DTOs/DTOs.csproj", "DTOs/"]

# Restore dependencies
RUN dotnet restore "WebApiShop/EventDressRental.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/WebApiShop"
RUN dotnet build "EventDressRental.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "EventDressRental.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published files
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EventDressRental.dll"]
