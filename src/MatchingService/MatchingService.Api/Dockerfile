# =========================================================
# Build stage
# =========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy các file project để cache lớp Nuget Restore
COPY ["src/MatchingService/MatchingService.Api/MatchingService.Api.csproj", "src/MatchingService/MatchingService.Api/"]
COPY ["src/MatchingService/MatchingService.Application/MatchingService.Application.csproj", "src/MatchingService/MatchingService.Application/"]
COPY ["src/MatchingService/MatchingService.Domain/MatchingService.Domain.csproj", "src/MatchingService/MatchingService.Domain/"]
COPY ["src/MatchingService/MatchingService.Infrastructure/MatchingService.Infrastructure.csproj", "src/MatchingService/MatchingService.Infrastructure/"]

RUN dotnet restore "src/MatchingService/MatchingService.Api/MatchingService.Api.csproj"

# Copy toàn bộ mã nguồn và build
COPY . .
WORKDIR "/src/src/MatchingService/MatchingService.Api"
RUN dotnet publish "MatchingService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================================================
# Runtime stage
# =========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8084
ENTRYPOINT ["dotnet", "MatchingService.Api.dll"]
