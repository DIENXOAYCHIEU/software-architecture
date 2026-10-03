# Solution Setup

The project uses a `.slnx` solution file.

Create the solution:

```cmd
dotnet new sln -n SwiftRide --format slnx
```

If `SwiftRide.slnx` already exists, you do not need to create it again.

# Add TripService Projects

```cmd
dotnet sln SwiftRide.slnx add src\TripService\TripService.Api\TripService.Api.csproj
dotnet sln SwiftRide.slnx add src\TripService\TripService.Application\TripService.Application.csproj
dotnet sln SwiftRide.slnx add src\TripService\TripService.Domain\TripService.Domain.csproj
dotnet sln SwiftRide.slnx add src\TripService\TripService.Infrastructure\TripService.Infrastructure.csproj
```

# Add PaymentService Projects

```cmd
dotnet sln SwiftRide.slnx add src\PaymentService\PaymentService.Api\PaymentService.Api.csproj
dotnet sln SwiftRide.slnx add src\PaymentService\PaymentService.Application\PaymentService.Application.csproj
dotnet sln SwiftRide.slnx add src\PaymentService\PaymentService.Domain\PaymentService.Domain.csproj
dotnet sln SwiftRide.slnx add src\PaymentService\PaymentService.Infrastructure\PaymentService.Infrastructure.csproj
```

---

# Restore Dependencies

From the SwiftRide root directory:

```cmd
dotnet restore SwiftRide.slnx
```

---

# Build the Solution

```cmd
dotnet build SwiftRide.slnx
```

---

# TripService Migrations

## Create Migration

```cmd
dotnet ef migrations add InitialCreate --project src\TripService\TripService.Infrastructure\TripService.Infrastructure.csproj --startup-project src\TripService\TripService.Api\TripService.Api.csproj
```

## Apply Migration

```cmd
dotnet ef database update --project src\TripService\TripService.Infrastructure\TripService.Infrastructure.csproj --startup-project src\TripService\TripService.Api\TripService.Api.csproj
```

---

# PaymentService Migrations

PaymentService uses .NET 9 and EF Core 9.

## Create Migration

```cmd
dotnet ef migrations add InitialCreate --project src\PaymentService\PaymentService.Infrastructure\PaymentService.Infrastructure.csproj --startup-project src\PaymentService\PaymentService.Api\PaymentService.Api.csproj
```

## Apply Migration

```cmd
dotnet ef database update --project src\PaymentService\PaymentService.Infrastructure\PaymentService.Infrastructure.csproj --startup-project src\PaymentService\PaymentService.Api\PaymentService.Api.csproj
```

---
