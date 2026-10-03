# 1. Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

EXPOSE 8083
ENV ASPNETCORE_URLS=http://+:8083


# 2. Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for Docker layer caching
COPY ["src/TripService/TripService.Api/TripService.Api.csproj", "src/TripService/TripService.Api/"]
COPY ["src/TripService/TripService.Application/TripService.Application.csproj", "src/TripService/TripService.Application/"]
COPY ["src/TripService/TripService.Domain/TripService.Domain.csproj", "src/TripService/TripService.Domain/"]
COPY ["src/TripService/TripService.Infrastructure/TripService.Infrastructure.csproj", "src/TripService/TripService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/TripService/TripService.Api/TripService.Api.csproj"

# Copy source code
COPY src/ ./src/

# Build
WORKDIR "/src/src/TripService/TripService.Api"
RUN dotnet build "TripService.Api.csproj" -c Release -o /app/build


# 3. Publish
FROM build AS publish

RUN dotnet publish "TripService.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# 4. Final image
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TripService.Api.dll"]