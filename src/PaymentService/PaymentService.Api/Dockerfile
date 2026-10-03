# 1. Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

EXPOSE 8083
ENV ASPNETCORE_URLS=http://+:8083


# 2. Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for Docker layer caching
COPY ["src/PaymentService/PaymentService.Api/PaymentService.Api.csproj", "src/PaymentService/PaymentService.Api/"]
COPY ["src/PaymentService/PaymentService.Application/PaymentService.Application.csproj", "src/PaymentService/PaymentService.Application/"]
COPY ["src/PaymentService/PaymentService.Domain/PaymentService.Domain.csproj", "src/PaymentService/PaymentService.Domain/"]
COPY ["src/PaymentService/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj", "src/PaymentService/PaymentService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/PaymentService/PaymentService.Api/PaymentService.Api.csproj"

# Copy source code
COPY src/ ./src/

# Build
WORKDIR "/src/src/PaymentService/PaymentService.Api"
RUN dotnet build "PaymentService.Api.csproj" -c Release -o /app/build


# 3. Publish
FROM build AS publish

RUN dotnet publish "PaymentService.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# 4. Final image
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PaymentService.Api.dll"]