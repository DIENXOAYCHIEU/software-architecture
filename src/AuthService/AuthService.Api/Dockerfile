# 1. Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081

# 2. Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for Docker layer caching
COPY ["src/AuthService/AuthService.Api/AuthService.Api.csproj", "src/AuthService/AuthService.Api/"]
COPY ["src/AuthService/AuthService.Application/AuthService.Application.csproj", "src/AuthService/AuthService.Application/"]
COPY ["src/AuthService/AuthService.Domain/AuthService.Domain.csproj", "src/AuthService/AuthService.Domain/"]
COPY ["src/AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "src/AuthService/AuthService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/AuthService/AuthService.Api/AuthService.Api.csproj"

# Copy source code
COPY src/ ./src/

# Build
WORKDIR "/src/src/AuthService/AuthService.Api"
RUN dotnet build "AuthService.Api.csproj" -c Release -o /app/build


# 3. Publish
FROM build AS publish

RUN dotnet publish "AuthService.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# 4. Final image
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AuthService.Api.dll"]