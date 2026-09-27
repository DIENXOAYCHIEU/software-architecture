# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["MatchingService.csproj", "./"]

RUN dotnet restore "MatchingService.csproj"

COPY . .

RUN dotnet publish "MatchingService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MatchingService.dll"]
