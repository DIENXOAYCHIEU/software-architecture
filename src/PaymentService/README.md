# PaymentService

PaymentService xử lý thanh toán cho chuyến đi, sổ cái kế toán kép (double-entry ledger), hoàn tiền, tính bất biến khi xử lý yêu cầu lặp lại (idempotency), thử lại khi gọi cổng thanh toán (gateway retry) và trao đổi sự kiện tích hợp thông qua RabbitMQ.

## Chức năng chính

- Tạo giao dịch thanh toán với `Idempotency-Key`.
- Không tạo giao dịch thanh toán hoặc thực hiện charge gateway lần thứ hai khi nhận lại cùng một yêu cầu.
- Trả về `409 Conflict` khi cùng một idempotency key nhưng nội dung payload khác nhau.
- Theo dõi các trạng thái `Pending`, `Succeeded`, `Failed` và `Refunded`.
- Lưu các bút toán ghi nợ (debit) và ghi có (credit), đồng thời đảm bảo tổng ghi nợ bằng tổng ghi có.
- Thử lại các yêu cầu gateway khi xảy ra lỗi tạm thời, sử dụng timeout, exponential backoff và jitter.
- Ghi sự kiện tích hợp vào transactional outbox.
- Tiếp nhận sự kiện `RideCompleted` và phát ra các sự kiện `PaymentSettled`, `PaymentFailed`, `PaymentRefunded`.

## Chạy service

Địa chỉ API mặc định khi chạy cục bộ:

```text
http://localhost:8084
```

Chạy bằng Docker:

```powershell
docker compose --env-file .env up --build -d rabbitmq payment_service
```

PaymentService sử dụng một cơ sở dữ liệu MySQL riêng thông qua cấu hình `ConnectionStrings__PaymentServiceDb`.

## Bộ sưu tập Postman

Nhập collection:

[PaymentService.postman_collection.json](./Postman/PaymentService.postman_collection.json)

Collection có sẵn các request để:

- Tạo payment.
- Gửi lại yêu cầu để kiểm tra idempotency.
- Kiểm tra xung đột payload.
- Lấy thông tin payment.
- Lấy danh sách payment theo chuyến đi (trip).
- Xem sổ cái kế toán (ledger).
- Hoàn tiền.
- Kiểm tra các endpoint health và metrics của Giai đoạn 4.

Base URL mặc định là `http://localhost:8084`.

## Các API endpoint

### 1. Kiểm tra trạng thái service (Health check)

```http
GET /health
```

Kiểm tra service đang chạy và sẵn sàng tiếp nhận các request cơ bản.

Response `200 OK`:

```json
{
  "status": "ok",
  "service": "payment"
}
```

### 2. Tạo payment

```http
POST /payments
Content-Type: application/json
Idempotency-Key: payment-001
X-Correlation-ID: optional-correlation-id
```

Request:

```json
{
  "tripId": "11111111-1111-1111-1111-111111111111",
  "riderId": "22222222-2222-2222-2222-222222222222",
  "amount": 85000,
  "currency": "VND",
  "method": "Card",
  "paymentToken": "tok_event_demo"
}
```

**Chức năng:** Tạo payment cho một chuyến đi (trip), lưu token thử nghiệm thông qua lớp trừu tượng hóa nhà cung cấp (provider abstraction), gọi cổng thanh toán (gateway) và tạo các bút toán ledger khi thanh toán thành công.

Response lần đầu `201 Created`:

```json
{
  "id": "33333333-3333-3333-3333-333333333333",
  "tripId": "11111111-1111-1111-1111-111111111111",
  "riderId": "22222222-2222-2222-2222-222222222222",
  "amount": 85000,
  "currency": "VND",
  "method": "Card",
  "status": "Succeeded",
  "failureReason": null,
  "createdAt": "2026-10-22T08:00:00Z",
  "updatedAt": "2026-10-22T08:00:01Z",
  "attemptCount": 1,
  "nextRetryAtUtc": null
}
```

### 3. Gửi lại yêu cầu payment (Idempotent replay)

```http
POST /payments
Idempotency-Key: payment-001
```

Gửi lại đúng payload với cùng một key sẽ trả về `200 OK` và cùng giá trị `id`. Hệ thống không tạo giao dịch mới và không thực hiện charge gateway lần thứ hai.

Nếu sử dụng cùng một key nhưng thay đổi payload, response sẽ là `409 Conflict`.

### 4. Lấy payment theo ID

```http
GET /payments/{id}
```

**Chức năng:** Lấy thông tin chi tiết của một payment theo `paymentId`.

Response `200 OK` có cùng cấu trúc dữ liệu (schema) với response khi tạo payment.

### 5. Lấy danh sách payment theo chuyến đi

```http
GET /payments/by-trip/{tripId}
```

**Chức năng:** Lấy danh sách payment của một chuyến đi, phục vụ việc đối soát hoặc kiểm tra chuyến đi đã được thanh toán hay chưa.

Response `200 OK`:

```json
[
  {
    "id": "33333333-3333-3333-3333-333333333333",
    "tripId": "11111111-1111-1111-1111-111111111111",
    "riderId": "22222222-2222-2222-2222-222222222222",
    "amount": 85000,
    "currency": "VND",
    "method": "Card",
    "status": "Succeeded",
    "failureReason": null,
    "attemptCount": 1,
    "nextRetryAtUtc": null
  }
]
```

### 6. Lấy sổ cái của payment

```http
GET /payments/{id}/ledger
```

**Chức năng:** Xem các bút toán ghi nợ (debit) và ghi có (credit) của payment. Tổng số tiền ghi nợ phải bằng tổng số tiền ghi có.

Response `200 OK`:

```json
[
  {
    "id": "44444444-4444-4444-4444-444444444444",
    "account": "Cash",
    "debitAmount": 85000,
    "creditAmount": 0,
    "type": "Payment",
    "createdAt": "2026-10-22T08:00:01Z"
  },
  {
    "id": "55555555-5555-5555-5555-555555555555",
    "account": "Revenue",
    "debitAmount": 0,
    "creditAmount": 85000,
    "type": "Payment",
    "createdAt": "2026-10-22T08:00:01Z"
  }
]
```

### 7. Hoàn tiền payment

```http
POST /payments/{id}/refund
Idempotency-Key: refund-001
```

**Chức năng:** Hoàn tiền cho một payment có trạng thái `Succeeded` và phát ra sự kiện `PaymentRefunded`.

Response `200 OK`: payment có trạng thái `status` là `Refunded`.

Nếu gửi lại yêu cầu hoàn tiền với cùng một refund key, hệ thống trả về kết quả idempotent và không gọi gateway hoàn tiền lần thứ hai.

Nếu sử dụng refund key khác cho một payment đã được hoàn tiền, hệ thống sẽ trả về `409 Conflict`.

### 8. Kiểm tra tiến trình còn hoạt động (Liveness check)

```http
GET /health/live
```

Kiểm tra tiến trình PaymentService còn đang hoạt động hay không. Endpoint này không yêu cầu cơ sở dữ liệu phải sẵn sàng.

Response `200 OK`:

```json
{
  "status": "live",
  "service": "payment"
}
```

### 9. Kiểm tra khả năng sẵn sàng (Readiness check)

```http
GET /health/ready
```

Kiểm tra PaymentService đã kết nối thành công tới MySQL hay chưa.

Response khi service sẵn sàng `200 OK`:

```json
{
  "status": "ready",
  "service": "payment"
}
```

Nếu không thể kết nối tới cơ sở dữ liệu, endpoint trả về `503 Service Unavailable`.

### 10. Chỉ số Prometheus (Prometheus metrics)

```http
GET /metrics
```

Trả về các chỉ số theo định dạng Prometheus, bao gồm chỉ số của ASP.NET Core, runtime và các chỉ số thanh toán tùy chỉnh:

- `payment.created`: Số lượng payment được tạo.
- `payment.refunded`: Số lượng payment được hoàn tiền.
- `payment.gateway.failures`: Số lần gateway gặp lỗi.

## Các phản hồi lỗi (Error responses)

Ví dụ phản hồi lỗi:

```json
{
  "error": "Idempotency-Key đã được sử dụng cho một payload khác."
}
```

Các mã lỗi thường gặp:

- `400 Bad Request`: Thiếu header bắt buộc hoặc request không hợp lệ.
- `404 Not Found`: Không tìm thấy payment.
- `409 Conflict`: Xung đột trong quá trình chuyển đổi trạng thái hoặc xung đột idempotency.
- `502 Bad Gateway`: Gateway từ chối request hoặc trả về lỗi được ánh xạ thành mã này.

## Cấu hình

Khi chạy cục bộ, service sử dụng `appsettings.Development.json` hoặc các biến môi trường (environment variables).

Khi chạy bằng Docker, service sử dụng file `.env` được tạo từ `.env.example`.

**Không commit các thông tin nhạy cảm vào Git**, bao gồm:

- File `.env`.
- Payment token thật.
- Mật khẩu cơ sở dữ liệu.
- JWT secret.

RabbitMQ sử dụng exchange `swiftride.events`. Queue mặc định của PaymentService là `payment.events`.

## Các vấn đề cần lưu ý khi triển khai production

- Gateway hỗ trợ JWT đối xứng (symmetric JWT) trong môi trường development và hỗ trợ OIDC authority thông qua biến `JWT_AUTHORITY` khi triển khai production.
- Trace và metrics của OpenTelemetry được cung cấp thông qua endpoint `GET /metrics`.
- `GET /health/live` dùng để kiểm tra tiến trình còn hoạt động; `GET /health/ready` kiểm tra khả năng kết nối tới cơ sở dữ liệu MySQL của PaymentService.
- Các thông điệp lỗi từ consumer của Payment và Trip được chuyển vào dead-letter queue (`payment.events.dlq`, `trip.events.dlq`) thay vì được đưa vào hàng đợi để xử lý lại vô hạn.
- Nhật ký kiểm toán (audit trail) được lưu trong bảng `audit_trails`, bao gồm thông tin payment, hành động, trạng thái, correlation ID và dấu thời gian (timestamp).
- Secret dùng trong production chỉ được truyền thông qua biến môi trường hoặc hệ thống quản lý bí mật (secret manager). Không ghi secret trực tiếp vào mã nguồn hoặc commit file `.env` lên Git.
