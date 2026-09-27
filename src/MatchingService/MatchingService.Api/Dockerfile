FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["MatchingService.Api/MatchingService.Api.csproj", "MatchingService.Api/"]
COPY ["MatchingService.Application/MatchingService.Application.csproj", "MatchingService.Application/"]
COPY ["MatchingService.Domain/MatchingService.Domain.csproj", "MatchingService.Domain/"]
COPY ["MatchingService.Infrastructure/MatchingService.Infrastructure.csproj", "MatchingService.Infrastructure/"]

RUN dotnet restore "MatchingService.Api/MatchingService.Api.csproj"

COPY . .

RUN dotnet publish "MatchingService.Api/MatchingService.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MatchingService.Api.dll"]