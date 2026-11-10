# MatchingService

## 1. Overview

MatchingService là một service trong hệ thống SwiftRide, chịu trách nhiệm tìm kiếm và quản lý việc ghép tài xế với chuyến đi. Service tiếp nhận thông tin chuyến đi, tìm tài xế phù hợp dựa trên vị trí hiện tại, quản lý các lượt đề xuất và xử lý phản hồi chấp nhận chuyến đi.

Trong cấu hình hiện tại, MatchingService còn host consumer xử lý sự kiện tính giá chuyến đi (Pricing) và tích hợp với OpenRouteService để lấy thông tin tuyến đường.

Các chức năng chính:

- Quản lý thông tin tài xế và vị trí hiện tại của tài xế.
- Tạo yêu cầu Matching cho chuyến đi.
- Tìm tài xế đang Available gần vị trí đón khách.
- Tạo và theo dõi các lượt đề xuất tài xế (`MatchingAttempt`).
- Xử lý tài xế chấp nhận chuyến đi và cập nhật trạng thái liên quan.
- Nhận sự kiện `TripCreated` từ RabbitMQ.
- Publish sự kiện `DriverAssigned` sau khi tài xế chấp nhận chuyến đi thành công.
- Nhận `TripCreated`, gọi OpenRouteService, tính giá và publish `PriceCalculated`.

## 2. Project Structure

Cấu trúc chính của MatchingService:

```text
MatchingService/
├── MatchingService.Api/
│   ├── Controllers/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── MatchingService.Api.csproj
├── MatchingService.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
├── MatchingService.Domain/
│   ├── Entities/
│   └── ValueObjects/
└── MatchingService.Infrastructure/
    ├── Integration/
    │   ├── RabbitMqIntegration.cs
    │   ├── TripPricingConsumer.cs
    │   └── RabbitMqIntegrationEventPublisher.cs
    ├── Persistence/
    └── Migrations/
```

Vai trò các layer:

| Project | Trách nhiệm |
|---|---|
| `MatchingService.Api` | Expose HTTP API, cấu hình Dependency Injection, Swagger và khởi chạy service. |
| `MatchingService.Application` | Chứa nghiệp vụ ứng dụng, DTO, interface và các service xử lý Matching/Pricing. |
| `MatchingService.Domain` | Chứa entity, enum và quy tắc nghiệp vụ cốt lõi. |
| `MatchingService.Infrastructure` | Triển khai lưu trữ MySQL, EF Core và tích hợp RabbitMQ/OpenRouteService. |

## 3. Business Workflow

### 3.1. Matching workflow

1. TripService tạo chuyến đi và publish sự kiện `TripCreated` lên RabbitMQ.
2. `TripCreatedConsumer` của MatchingService nhận sự kiện.
3. MatchingService tạo `MatchingRequest` cho chuyến đi.
4. MatchingService lấy danh sách tài xế đang Available và có vị trí hiện tại.
5. Hệ thống tính khoảng cách từ tài xế đến điểm đón, sau đó ưu tiên tài xế gần nhất chưa được đề xuất trước đó.
6. Hệ thống tạo `MatchingAttempt` với trạng thái `Offered` và cập nhật `MatchingRequest` sang `Searching`.
7. Tài xế phản hồi thông qua API Accept.
8. Khi Accept hợp lệ, MatchingService cập nhật `MatchingAttempt` thành `Accepted`, `MatchingRequest` thành `Matched` và trạng thái tài xế thành `Busy`.
9. Sau khi lưu dữ liệu thành công, MatchingService publish sự kiện `DriverAssigned` để service liên quan nhận thông tin tài xế được gán.

Các trạng thái MatchingRequest đang sử dụng:

| Trạng thái | Ý nghĩa |
|---|---|
| `Pending` | Yêu cầu Matching vừa được tạo, chưa bắt đầu tìm tài xế. |
| `Searching` | Đang tìm kiếm/đã có lượt đề xuất tài xế và chờ phản hồi. |
| `Matched` | Tài xế đã chấp nhận chuyến đi. |
| `Failed` | Không tìm được tài xế phù hợp hoặc đã hết số lượt thử. |

Các trạng thái MatchingAttempt:

| Trạng thái | Ý nghĩa |
|---|---|
| `Offered` | Chuyến đi đã được đề xuất cho tài xế, đang chờ phản hồi. |
| `Accepted` | Tài xế đã chấp nhận. |
| `Rejected` | Tài xế từ chối. |

Trạng thái tài xế:

| Trạng thái | Ý nghĩa |
|---|---|
| `Available` | Tài xế sẵn sàng nhận chuyến. |
| `Busy` | Tài xế đang bận/đã nhận chuyến. |

### 3.2. Pricing workflow

Trong cấu hình hiện tại, `TripPricingConsumer` được đăng ký chạy cùng MatchingService:

1. Consumer nhận bản sao sự kiện `TripCreated` từ queue `pricing.events`.
2. Consumer gọi OpenRouteService để lấy quãng đường và thời gian di chuyển.
3. `PricingService` sử dụng `IPricingStrategy` để tính giá.
4. Consumer publish sự kiện `PriceCalculated` lên exchange `swiftride.events`.

Công thức `BasePrice` hiện được tính trong `PricingService`:

```text
BasePrice = 10,000 × DistanceKm + 2,000 × DurationMinutes
```

Giá cuối cùng (`FinalPrice`) được tính thông qua pricing strategy cùng các tham số như surge multiplier, toll, promotion multiplier và tax rate.

## 4. Database Configuration

MatchingService sử dụng MySQL với database mặc định:

```text
swiftride_matching
```

Tạo database bằng MySQL Workbench hoặc MySQL CLI:

```sql
CREATE DATABASE swiftride_matching
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;
```

Các bảng chính:

| Table | Mô tả |
|---|---|
| `Drivers` | Lưu thông tin tài xế, trạng thái và vị trí hiện tại. |
| `MatchingRequests` | Lưu yêu cầu ghép tài xế theo chuyến đi. |
| `MatchingAttempts` | Lưu lịch sử các lượt đề xuất tài xế cho một yêu cầu Matching. |
| `__EFMigrationsHistory` | Lưu lịch sử migration của Entity Framework Core. |

### Connection string

Mở `MatchingService.Api/appsettings.Development.json` và cấu hình connection string theo key mà `Program.cs` đang đọc. Ví dụ nếu service sử dụng key `MatchingDb`:

```json
{
  "ConnectionStrings": {
    "MatchingDb": "Server=localhost;Port=3308;Database=swiftride_matching;User=YOUR_MYSQL_USER;Password=YOUR_MYSQL_PASSWORD;"
  }
}
```

Thay `YOUR_MYSQL_USER` và `YOUR_MYSQL_PASSWORD` bằng tài khoản MySQL của bạn. Port `3308` là port host được sử dụng trong cấu hình Docker Compose hiện tại của repository; nếu MySQL chạy trực tiếp trên máy ở port khác, hãy điều chỉnh lại.

> Lưu ý: Tên key connection string (`MatchingDb` trong ví dụ) phải trùng với tên được truyền vào `builder.Configuration.GetConnectionString(...)` trong `Program.cs`. Nếu project đang dùng tên key khác, hãy giữ nguyên tên key của project.

## 5. RabbitMQ Configuration

MatchingService sử dụng RabbitMQ để giao tiếp bất đồng bộ với các service khác.

Thông tin kết nối thường được cấu hình trong section `RabbitMq`:

| Key | Giá trị mặc định/ý nghĩa |
|---|---|
| `Host` | `localhost` khi chạy API trực tiếp trên máy; `rabbitmq` khi chạy trong Docker Compose. |
| `Port` | `5672` |
| `UserName` | Username RabbitMQ được cấu hình trong môi trường. |
| `Password` | Password RabbitMQ được cấu hình trong môi trường. |
| `Exchange` | `swiftride.events` |
| `Queue` | `matching.events` |
| `DeadLetterExchange` | `swiftride.events.dlx` |
| `DeadLetterQueue` | `matching.events.dlq` |
| `PricingQueue` | `pricing.events` |
| `PricingDeadLetterExchange` | `swiftride.pricing.events.dlx` |
| `PricingDeadLetterQueue` | `pricing.events.dlq` |

Ví dụ cấu hình trong `appsettings.Development.json` (điền username/password phù hợp với môi trường của bạn):

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "UserName": "YOUR_RABBITMQ_USER",
    "Password": "YOUR_RABBITMQ_PASSWORD",
    "Exchange": "swiftride.events",
    "Queue": "matching.events",
    "DeadLetterExchange": "swiftride.events.dlx",
    "DeadLetterQueue": "matching.events.dlq",
    "PricingQueue": "pricing.events",
    "PricingDeadLetterExchange": "swiftride.pricing.events.dlx",
    "PricingDeadLetterQueue": "pricing.events.dlq"
  }
}
```

RabbitMQ Management UI theo cấu hình hiện tại:

```text
http://localhost:15672
```

### Events

**Event nhận: `TripCreated`**

Routing key: `TripCreated`.

Envelope và payload mẫu:

```json
{
  "MessageId": "a1b2c3d4-1111-4222-8333-123456789abc",
  "EventName": "TripCreated",
  "Payload": "{\"TripId\":\"8d5e7f20-4b61-4a82-9c31-5e7f2a6d8012\",\"PickupLatitude\":10.784773712059229,\"PickupLongitude\":106.64521051657154,\"DropoffLatitude\":10.8231,\"DropoffLongitude\":106.6297}"
}
```

Matching consumer sử dụng `TripId`, `PickupLatitude` và `PickupLongitude` để tạo yêu cầu Matching và tìm tài xế. Pricing consumer sử dụng thêm tọa độ điểm đến để tính tuyến đường và giá.

**Event phát: `DriverAssigned`**

Routing key: `DriverAssigned`.

Sự kiện được publish sau khi API Accept xử lý thành công và các thay đổi Matching đã được lưu vào database. Payload theo contract hiện tại:

```json
{
  "MessageId": "generated-message-id",
  "EventName": "DriverAssigned",
  "Payload": "{\"TripId\":\"8d5e7f20-4b61-4a82-9c31-5e7f2a6d8012\",\"DriverId\":\"3fb37e8e-5934-4427-a9a2-8c1b1542aa63\"}"
}
```

**Event phát: `PriceCalculated`**

Routing key: `PriceCalculated`. Sự kiện chứa thông tin chuyến đi, quãng đường, thời gian di chuyển, giá cơ bản, các hệ số và giá cuối cùng. Các service cần nhận sự kiện này phải bind queue vào exchange `swiftride.events` với routing key tương ứng.

## 6. Apply Database Migrations

Mở terminal tại thư mục gốc repository `software-architecture`.

Chọn môi trường Development:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

Áp dụng migration (giả sử DbContext có tên `MatchingDbContext`):

```powershell
dotnet ef database update `
  --project .\src\MatchingService\MatchingService.Infrastructure\MatchingService.Infrastructure.csproj `
  --startup-project .\src\MatchingService\MatchingService.Api\MatchingService.Api.csproj `
  --context MatchingDbContext
```

Nếu DbContext trong source có tên khác, thay giá trị `--context` bằng tên DbContext thực tế.

## 7. Run the Service Locally

Từ thư mục gốc repository:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"

dotnet run --project .\src\MatchingService\MatchingService.Api\MatchingService.Api.csproj
```

Kiểm tra dòng `Now listening on` trong terminal để biết địa chỉ và port thực tế.

Mở Swagger theo base URL được hiển thị, ví dụ:

```text
http://localhost:8083/swagger
```

Nếu API chạy ở port khác, hãy thay port tương ứng. Khi chạy trực tiếp trên máy, RabbitMQ và MySQL cũng phải có thể truy cập được từ host.

## 8. API Endpoints

MatchingService cung cấp các thao tác API liên quan đến tài xế và Matching. Tên route, HTTP method, request body và response cụ thể được khai báo trong các Controller.

Các nhóm chức năng cần kiểm thử:

| Nhóm | Chức năng |
|---|---|
| Driver | Tạo/lấy thông tin tài xế và cập nhật vị trí hiện tại (theo các action được expose trong `DriverController`). |
| Matching | Tạo MatchingRequest và lấy thông tin Matching theo ID. |
| Matching | Bắt đầu tìm kiếm tài xế (`StartMatching`). |
| Matching | Chấp nhận đề xuất tài xế (`AcceptDriver`). |

> Mở Swagger tại `/swagger` để lấy chính xác route và schema theo Controller hiện tại. Không nên tự suy đoán route vì URL thực tế phụ thuộc vào `[Route]`, `[HttpGet]`, `[HttpPost]` và các attribute trong source code.

### 8.1. Kiểm thử Accept Driver

Sau khi một MatchingRequest đã có tài xế được đề xuất, sử dụng API Accept tương ứng trong Swagger với:

- `matchingId`: ID của MatchingRequest.
- `driverId`: ID tài xế đang được đề xuất trong MatchingRequest.

Khi xử lý thành công, dữ liệu mong đợi:

| Table | Field | Expected value |
|---|---|---|
| `MatchingRequests` | `Status` | `Matched` |
| `MatchingRequests` | `DriverId` | ID tài xế đã chấp nhận |
| `MatchingRequests` | `MatchedAt` | Có giá trị thời gian |
| `MatchingAttempts` | `Status` | `Accepted` (`1`) |
| `MatchingAttempts` | `RespondedAt` | Có giá trị thời gian |
| `Drivers` | `Status` | `Busy` (`2`) |

Sau khi lưu thành công, MatchingService publish `DriverAssigned` lên RabbitMQ.

## 9. Run with Docker Compose

Từ thư mục gốc repository `software-architecture`, build và chạy MatchingService:

```powershell
docker compose up -d --build matching_service
```

Kiểm tra trạng thái các container:

```powershell
docker compose ps
```

Theo dõi log MatchingService:

```powershell
docker compose logs -f --since=1m matching_service
```

Trong Docker network, MatchingService kết nối đến các service bằng service name, ví dụ:

- MySQL: `mysql:3306`.
- RabbitMQ: `rabbitmq:5672`.

Không sử dụng `localhost` để trỏ đến container MySQL hoặc RabbitMQ khác trong cùng Docker network. `localhost` bên trong container là chính container đó.

### Environment variables

Các biến cấu hình RabbitMQ có thể được truyền qua .NET environment variables, ví dụ:

```dotenv
RabbitMq__Host=rabbitmq
RabbitMq__Port=5672
RabbitMq__UserName=YOUR_RABBITMQ_USER
RabbitMq__Password=YOUR_RABBITMQ_PASSWORD
```

Pricing cần API key của OpenRouteService. Nếu `docker-compose.yml` ánh xạ biến `OPENROUTESERVICE_API_KEY` sang cấu hình .NET `OpenRouteService__ApiKey`, hãy khai báo trong file `.env` ở thư mục gốc:

```dotenv
OPENROUTESERVICE_API_KEY=YOUR_OPENROUTESERVICE_API_KEY
```

Tên biến trong `.env` cần khớp với phần ánh xạ `environment` của service trong `docker-compose.yml`.

Connection string trong container phải sử dụng hostname và port nội bộ của MySQL, ví dụ `Server=mysql;Port=3306;Database=swiftride_matching;...`. Tên environment variable dùng để truyền connection string phải khớp với cấu hình trong `docker-compose.yml` và `Program.cs` của repository.

Không commit password, API key hoặc thông tin bí mật vào Git. Nên lưu các giá trị này trong file `.env` cục bộ và bảo đảm file `.env` được khai báo trong `.gitignore`.

## 10. Testing the RabbitMQ Workflow

Có thể kiểm thử luồng Matching bằng cách publish thủ công một message `TripCreated` từ RabbitMQ Management UI:

1. Mở `http://localhost:15672` và đăng nhập bằng tài khoản RabbitMQ đã cấu hình.
2. Mở exchange `swiftride.events`.
3. Publish message với routing key `TripCreated` và payload theo mẫu ở mục 5.
4. Theo dõi log bằng `docker compose logs -f --since=1m matching_service`.
5. Kiểm tra database `swiftride_matching` để xác nhận `MatchingRequests` và `MatchingAttempts` được tạo.
6. Gọi API Accept trong Swagger bằng `matchingId` và `driverId` thực tế.
7. Kiểm tra queue được bind với routing key `DriverAssigned` để xác nhận sự kiện chỉ được phát sau khi Accept thành công.

Đối với Pricing, kiểm tra log có các bước nhận `TripCreated`, gọi OpenRouteService thành công, tính giá và publish `PriceCalculated`. Có thể tạo queue capture riêng và bind vào exchange `swiftride.events` với routing key `PriceCalculated` để quan sát message trong môi trường kiểm thử.

Các queue chính:

| Queue | Mục đích |
|---|---|
| `matching.events` | Nhận `TripCreated` cho Matching. |
| `matching.events.dlq` | Lưu message Matching bị reject/dead-letter theo cấu hình queue. |
| `pricing.events` | Nhận `TripCreated` cho Pricing. |
| `pricing.events.dlq` | Lưu message Pricing bị reject/dead-letter theo cấu hình queue. |

## 11. Notes

- Mỗi chuyến đi cần sử dụng `TripId` duy nhất khi kiểm thử để tránh trùng dữ liệu với các lần publish trước.
- `DriverAssigned` là sự kiện xác nhận tài xế đã chấp nhận chuyến đi; không publish sự kiện này ngay khi mới tạo `MatchingAttempt` ở trạng thái `Offered`.
- `DriverId` trong `DriverAssigned` phải là ID tài xế thực tế được lưu trong `MatchingRequest`, không tạo GUID ngẫu nhiên.
- `PriceCalculated` và `DriverAssigned` là hai sự kiện độc lập, cùng được liên kết với chuyến đi thông qua `TripId`.
- Publish RabbitMQ sau khi `SaveChangesAsync` thành công là cách triển khai hiện tại. Để tăng độ tin cậy khi triển khai production, có thể cân nhắc Transactional Outbox Pattern nhằm tránh trường hợp database đã commit nhưng publish message thất bại.
