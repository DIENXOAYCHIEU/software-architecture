# PaymentService

PaymentService xử lý thanh toán, double-entry ledger, refund, idempotency và
phát integration event qua RabbitMQ.

## Postman

Import collection [`PaymentService.postman_collection.json`](./Postman/PaymentService.postman_collection.json)
vào Postman. Collection mô tả request/response cho create, replay idempotency,
get payment, ledger và refund.

Mặc định API chạy tại `http://localhost:8084`.

## Cấu hình

- Chạy local: dùng `PaymentService.Api/appsettings.Development.json` hoặc
  biến môi trường ASP.NET Core.
- Chạy Docker: copy `.env.example` thành `.env`, điền
  `PAYMENT_DB_CONNECTION` và các biến RabbitMQ, sau đó chạy:

```bash
docker compose up --build payment_service rabbitmq
```

Payment database dùng MySQL riêng qua connection string
`ConnectionStrings__PaymentServiceDb`. RabbitMQ dùng exchange
`swiftride.events`.

## Integration flow

Trip phát `RideCompleted`; Payment consume và phát `PaymentSettled`,
`PaymentFailed` hoặc `PaymentRefunded`. Các event được ghi vào transactional
outbox và consumer lưu message đã xử lý để chống xử lý lặp.
