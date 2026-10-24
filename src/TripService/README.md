# TripService

## 1. Overview

TripService là một service trong hệ thống SwiftRide, chịu trách nhiệm xử lý các thao tác liên quan đến chuyến đi.

Các chức năng hiện có:

- Tạo chuyến đi.
- Lấy thông tin chuyến đi theo ID.
- Gán tài xế cho chuyến đi.
- Cập nhật trạng thái khi đón khách.
- Cập nhật trạng thái khi trả khách.

## 2. Project Structure

```text
TripService/
├── Postman/
│   └── TripService.postman_collection.json
├── TripService.Api/
│   ├── Controllers/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── TripService.Api.csproj
├── TripService.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
├── TripService.Domain/
│   ├── Entities/
│   └── ValueObjects/
├── TripService.Infrastructure/
│   ├── Migrations/
│   └── Persistence/
└── README.md
```

## 3. Database Configuration

TripService sử dụng MySQL với database mặc định trong hướng dẫn này là `swiftride_trips`.

Tạo database bằng MySQL Workbench:

```sql
CREATE DATABASE swiftride_trips
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;
```

### Connection string

Mở file `TripService.Api/appsettings.Development.json` và cấu hình:

```json
{
  "ConnectionStrings": {
    "TripServiceDb": "Server=localhost;Port=3306;Database=swiftride_trips;User=YOUR_MYSQL_USER;Password=YOUR_MYSQL_PASSWORD;"
  }
}
```

Thay `YOUR_MYSQL_USER` và `YOUR_MYSQL_PASSWORD` bằng thông tin tài khoản MySQL trên máy của bạn.

## 4. Apply Database Migrations

Mở terminal tại thư mục gốc repository `software-architecture`.

Chọn môi trường Development:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

Chạy migration:

```powershell
dotnet ef database update `
  --project .\src\TripService\TripService.Infrastructure\TripService.Infrastructure.csproj `
  --startup-project .\src\TripService\TripService.Api\TripService.Api.csproj `
  --context TripDbContext
```

Lệnh này áp dụng các migration hiện có vào database được cấu hình trong connection string.

Migration khởi tạo tạo bảng `Trips` và bảng lịch sử migration của Entity Framework Core.

## 5. Run the Service Locally

Từ thư mục gốc repository, chạy:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"

dotnet run --project .\src\TripService\TripService.Api\TripService.Api.csproj
```

Kiểm tra thông báo `Now listening on` trong terminal để biết địa chỉ và cổng API thực tế.

Ví dụ, nếu API chạy ở cổng 5000:

```text
http://localhost:5000
```

Nếu cổng khác, hãy dùng cổng được hiển thị trong terminal và cập nhật URL tương ứng trong Postman.

## 6. API Endpoints

Base URL minh họa:

```text
http://localhost:5000
```

| Method | Endpoint | Description | Expected status |
|---|---|---|---|
| POST | `/api/Trip` | Tạo chuyến đi | 201 Created |
| GET | `/api/Trip/{tripId}` | Lấy chuyến đi theo ID | 200 OK |
| POST | `/api/Trip/{tripId}/driver-accept` | Gán tài xế | 201 Created |
| POST | `/api/Trip/{tripId}/pickup` | Cập nhật trạng thái đón khách | 201 Created |
| POST | `/api/Trip/{tripId}/dropoff` | Cập nhật trạng thái trả khách | 201 Created |

Các mã HTTP trên phản ánh cách controller hiện tại trả về kết quả khi service xử lý thành công.

### 6.1. Create Trip

**Request**

```http
POST /api/Trip
Content-Type: application/json
```

Body mẫu:

```json
{
  "riderId": "11111111-1111-1111-1111-111111111111",
  "startLocation": {
    "latitude": 10.7769,
    "longitude": 106.7009,
    "address": "Quận 1, TP. Hồ Chí Minh"
  },
  "endLocation": {
    "latitude": 10.8231,
    "longitude": 106.6297,
    "address": "Quận Tân Bình, TP. Hồ Chí Minh"
  }
}
```
Thay `riderId` bằng ID hợp lệ nếu hệ thống yêu cầu.

**Response — `201 Created`**

```json
{
  "id": "dc25f55e-49c2-42dd-9aa6-96259886e89f",
  "riderId": "11111111-1111-1111-1111-111111111111",
  "status": 0
}
```

### 6.2. Get Trip by ID

**Request**

```http
GET /api/Trip/{tripId}
```
Thay `{tripId}` bằng ID chuyến đi thực tế đã được tạo.

**Response — `200 OK`**

```json
{
  "id": "dc25f55e-49c2-42dd-9aa6-96259886e89f",
  "riderId": "11111111-1111-1111-1111-111111111111",
  "driverId": null,
  "startLocation": {
    "latitude": 10.7769,
    "longitude": 106.7009,
    "address": "Quan 1"
  },
  "endLocation": {
    "latitude": 10.8231,
    "longitude": 106.6297,
    "address": "San bay Tan Son Nhat"
  },
  "amountToPay": null,
  "created": "2026-10-24T09:00:00Z",
  "updated": "2026-10-24T09:00:00Z",
  "status": 0
}
```

### 6.3. Assign Driver

**Request**

```http
POST /api/Trip/{tripId}/driver-accept
Content-Type: application/json
```

Body là một GUID được gửi dưới dạng chuỗi JSON:

```json
"22222222-2222-2222-2222-222222222222"
```

Thay GUID mẫu bằng ID tài xế hợp lệ theo dữ liệu và quy tắc của hệ thống.

**Response — `201 Created`**

```json
{
  "id": "dc25f55e-49c2-42dd-9aa6-96259886e89f",
  "driverId": "22222222-2222-2222-2222-222222222222",
  "status": 1
}
```

### 6.4. Pickup

**Request**

```http
POST /api/Trip/{tripId}/pickup
```

Endpoint này cập nhật trạng thái chuyến đi khi đón khách. Không cần gửi request body theo controller hiện tại.

**Response — `201 Created`**

```json
{
  "id": "dc25f55e-49c2-42dd-9aa6-96259886e89f",
  "driverId": "22222222-2222-2222-2222-222222222222",
  "status": 2
}
```

### 8.5. Dropoff
### 6.5. Dropoff

**Request**

```http
POST /api/Trip/{tripId}/dropoff
```

Endpoint này cập nhật trạng thái chuyến đi khi trả khách. Không cần gửi request body theo controller hiện tại.

**Response — `201 Created`**

```json
{
  "id": "dc25f55e-49c2-42dd-9aa6-96259886e89f",
  "driverId": "22222222-2222-2222-2222-222222222222",
  "status": 3
}
```

## 7. Postman Collection

Postman Collection được lưu tại:

[TripService Postman Collection](./Postman/TripService.postman_collection.json)

### Import Collection

1. Mở Postman.
2. Chọn **Import**.
3. Chọn file `TripService.postman_collection.json`.
4. Chọn request cần kiểm thử.
5. Cập nhật Base URL theo cổng API thực tế.
6. Nhấn **Send** và kiểm tra status code cùng response body.

Nên kiểm thử theo thứ tự:

1. Create Trip.
2. Get Trip bằng ID trả về từ Create Trip.
3. Assign Driver.
4. Pickup.
5. Dropoff.

Các request phụ thuộc vào chuyến đi hoặc tài xế phải sử dụng ID phù hợp với dữ liệu thực tế.

## 8. Environment Variables and Docker

* Khi chạy bằng Docker, cấu hình biến `TRIP_DB_CONNECTION` trong file `.env` tại thư mục gốc repository theo cấu hình Docker Compose.

Ví dụ minh họa:

```dotenv
TRIP_DB_CONNECTION="Server=host.docker.internal;Port=3306;Database=swiftride_trips;User=YOUR_MYSQL_USER;Password=YOUR_MYSQL_PASSWORD;"
```
* Build và chạy `trip_service`:

```cmd
docker compose up -d --build <trip_service>
```