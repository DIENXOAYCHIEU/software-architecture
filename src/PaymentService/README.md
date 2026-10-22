# PaymentService

PaymentService xu ly payment cho chuyen di, double-entry ledger, refund,
idempotency, retry gateway va integration event qua RabbitMQ.

## Chuc nang chinh

- Tao payment voi `Idempotency-Key`.
- Khong tao payment hoac charge gateway lan hai khi replay cung request.
- Tu choi `409 Conflict` khi cung idempotency key nhung payload khac.
- Theo doi trang thai `Pending`, `Succeeded`, `Failed` va `Refunded`.
- Luu ledger debit/credit va dam bao tong debit bang tong credit.
- Retry gateway tam thoi voi timeout, exponential backoff va jitter.
- Ghi integration event vao transactional outbox.
- Consume `RideCompleted` va phat `PaymentSettled`, `PaymentFailed`,
  `PaymentRefunded`.

## Chay service

Local API mac dinh:

```text
http://localhost:8084
```

Docker:

```powershell
docker compose --env-file .env up --build -d rabbitmq payment_service
```

PaymentService dung MySQL database rieng qua
`ConnectionStrings__PaymentServiceDb`.

## Postman Collection

Import collection:

[PaymentService.postman_collection.json](./Postman/PaymentService.postman_collection.json)

Collection co san cac request create, replay idempotency, payload conflict,
get payment, get payments by trip, ledger va refund.
Base URL mac dinh la `http://localhost:8084`.

## API endpoints

### Health check

```http
GET /health
```

Kiem tra service dang san sang xu ly request.

Response `200 OK`:

```json
{
  "status": "ok",
  "service": "payment"
}
```

### Tao payment

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

Chuc nang: tao payment cho mot trip, luu token demo o provider abstraction,
goi gateway va tao ledger khi thanh cong.

Response lan dau `201 Created`:

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

### Replay payment idempotent

```http
POST /payments
Idempotency-Key: payment-001
```

Gui lai dung payload va dung key se tra `200 OK`, cung `id`, khong tao giao
dich moi va khong charge gateway lan hai.

Neu cung key nhung thay doi payload, response la `409 Conflict`.

### Lay payment theo ID

```http
GET /payments/{id}
```

Chuc nang: lay chi tiet mot payment theo `paymentId`.

Response `200 OK`: cung schema voi response tao payment.

### Lay payments theo trip

```http
GET /payments/by-trip/{tripId}
```

Chuc nang: lay danh sach payment cua mot trip, dung de doi soat hoac kiem tra
payment da thanh toan.

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

### Lay ledger cua payment

```http
GET /payments/{id}/ledger
```

Chuc nang: xem cac but toan debit/credit cua payment. Tong debit va credit
phai bang nhau.

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

### Refund payment

```http
POST /payments/{id}/refund
Idempotency-Key: refund-001
```

Chuc nang: hoan tien mot payment `Succeeded` va phat event
`PaymentRefunded`.

Response `200 OK`: payment voi `status` la `Refunded`.

Replay cung refund key se tra ket qua idempotent va khong goi refund gateway
lan hai. Dung refund key khac cho payment da refund se tra `409 Conflict`.

## Error responses

```json
{
  "error": "Idempotency-Key da duoc dung cho payload khac."
}
```

Ma loi thuong gap:

- `400`: thieu header hoac request khong hop le.
- `404`: khong tim thay payment.
- `409`: state transition hoac idempotency conflict.
- `502`: gateway tu choi request.

## Cau hinh

Local dung `appsettings.Development.json` hoac environment variables.
Docker dung file `.env` tao tu `.env.example`. Khong commit file `.env`,
payment token that, database password hoac JWT secret.

RabbitMQ dung exchange `swiftride.events`; queue PaymentService mac dinh la
`payment.events`.

## Production concerns

- Gateway ho tro JWT symmetric cho development va OIDC authority qua
  `JWT_AUTHORITY` khi deploy production.
- OpenTelemetry trace/metrics duoc expose tai `GET /metrics`.
- `GET /health/live` la liveness check; `GET /health/ready` kiem tra ket noi
  den Payment MySQL.
- Message loi cua Payment va Trip consumer duoc day vao dead-letter queue
  (`payment.events.dlq`, `trip.events.dlq`) thay vi requeue vo han.
- Audit trail duoc luu trong bang `audit_trails`, gom payment, action, status,
  correlation ID va timestamp.
- Secret production chi dua qua environment/secret manager, khong ghi vao
  source code hoac `.env` commit vao Git.
