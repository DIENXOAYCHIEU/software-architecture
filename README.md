# SwiftRide API Gateway

## 1. Giới thiệu

API Gateway là điểm truy cập duy nhất để client gửi request đến các microservice trong hệ thống SwiftRide. Gateway được xây dựng bằng **ASP.NET Core và YARP (Yet Another Reverse Proxy)**.

Các chức năng chính:

- Định tuyến request đến microservice tương ứng.
- Xác thực JWT Access Token.
- Phân quyền truy cập các API thông qua Authorization Policy.
- Chuyển tiếp thông tin ID người dùng đến các service thông qua header `X-User-Id`.
- Tập trung xử lý xác thực và kiểm soát truy cập tại Gateway.

## 2. Kiến trúc hệ thống

```text
                  Client / Postman
                         |
                         | HTTP Request
                         | Authorization: Bearer <JWT>
                         v
                +---------------------+
                |     API Gateway    |
                |    ASP.NET Core    |
                |        YARP        |
                +---------------------+
                         |
                         v
                +---------------------+
                | JWT Authentication  |
                | - Kiểm tra chữ ký   |
                | - Kiểm tra Issuer   |
                | - Kiểm tra Audience |
                | - Kiểm tra thời hạn |
                +---------------------+
                         |
                         v
                +---------------------+
                | Authorization       |
                | AuthenticatedUser   |
                +---------------------+
                         |
              +----------+----------+
              |                     |
              v                     v
       +---------------+     +---------------+
       | Auth Service  |     | Trip Service  |
       | Port 8081     |     | Port 8082     |
       +---------------+     +---------------+
```

API Gateway lắng nghe tại port `5000` và giao tiếp với các microservice thông qua Docker network.

## 3. Cơ chế định tuyến request

Gateway sử dụng YARP để định tuyến request dựa trên URL được khai báo trong `appsettings.json`.

| Route | Service đích | Chính sách xác thực |
|---|---|---|
| `/api/auth/{**catch-all}` | `auth_service:8081` | Anonymous |
| `/api/trip/{**catch-all}` | `trip_service:8082` | AuthenticatedUser |
| `/api/matching/{**catch-all}` | `matching_service:8083` | AuthenticatedUser |
| `/api/payments/{**catch-all}` | `payment-service:8084` | AuthenticatedUser |

Trong cấu hình Docker Compose hiện tại, Auth Service và Trip Service đang được bật. Matching Service và Payment Service đang bị comment nên chưa thể sử dụng cho đến khi được bật và cấu hình đầy đủ.

Ví dụ cấu hình route Trip:

```json
"trip-route": {
  "ClusterId": "trip-cluster",
  "AuthorizationPolicy": "AuthenticatedUser",
  "Match": {
    "Path": "/api/trip/{**catch-all}"
  }
}
```

Khi client gửi request đến `/api/trip/...`, Gateway chuyển tiếp request đến `http://trip_service:8082`.

## 4. Cơ chế xác thực JWT

Gateway sử dụng JWT Bearer Authentication của ASP.NET Core để xác thực Access Token.

Khi gọi các API được bảo vệ, client phải gửi JWT trong header `Authorization`.

```http
Authorization: Bearer <access_token>
```

Gateway thực hiện kiểm tra:

- **Signing Key:** Xác minh chữ ký của token.
- **Issuer (`iss`):** Phải khớp với giá trị `AuthService`.
- **Audience (`aud`):** Phải khớp với giá trị `AuthServiceUsers`.
- **Expiration (`exp`):** Đảm bảo token chưa hết hạn.

Các thông số xác thực tại Gateway phải tương thích với cấu hình mà Auth Service sử dụng để phát hành token.

### Kết quả xác thực

| Trường hợp | Kết quả mong đợi |
|---|---|
| Token hợp lệ | Request được chuyển tiếp |
| Không gửi token đến route được bảo vệ | `401 Unauthorized` |
| Token sai hoặc hết hạn | `401 Unauthorized` |
| Đã xác thực nhưng không đủ quyền theo policy bổ sung | `403 Forbidden` |
| Service đích không hoạt động | Thường là `502 Bad Gateway` |

## 5. Phân quyền bằng Authorization Policy

Gateway đăng ký policy `AuthenticatedUser` trong `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "AuthenticatedUser",
        policy => policy.RequireAuthenticatedUser()
    );
});
```

Policy này yêu cầu request phải có người dùng đã được xác thực.

Policy được áp dụng cho các route thông qua cấu hình YARP. Route Auth được cấu hình `Anonymous` để cho phép client truy cập các API như đăng nhập hoặc đăng ký mà không cần Access Token trước đó.

Middleware được cấu hình theo thứ tự:

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
```

Trong đó:

- `UseAuthentication()` xác thực JWT và thiết lập thông tin người dùng.
- `UseAuthorization()` kiểm tra quyền truy cập theo policy.
- `MapReverseProxy()` ánh xạ các endpoint proxy của YARP.

Thứ tự này đảm bảo xác thực diễn ra trước khi kiểm tra phân quyền.

## 6. Chuyển tiếp ID người dùng đến microservice

Sau khi JWT được xác thực, Gateway có thể lấy ID người dùng từ các claim trong token và chuyển tiếp đến service đích thông qua header `X-User-Id`.

Ví dụ:

```csharp
builderContext.AddRequestTransform(context =>
{
    var user = context.HttpContext.User;

    if (user.Identity?.IsAuthenticated == true)
    {
        var userId =
            user.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("id")?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            context.ProxyRequest.Headers.Remove("X-User-Id");

            context.ProxyRequest.Headers.TryAddWithoutValidation(
                "X-User-Id", userId);
        }
    }

    return ValueTask.CompletedTask;
});
```

ID người dùng được lấy từ claim tương ứng với cấu trúc JWT do Auth Service phát hành.

Ví dụ header được chuyển tiếp:

```http
X-User-Id: <user_id>
```

Trip Service có thể sử dụng thông tin này để xác định người dùng thực hiện request, chẳng hạn khi tạo chuyến đi hoặc kiểm tra quyền truy cập chuyến đi.

**Lưu ý bảo mật:** `X-User-Id` không tự chứng minh danh tính người dùng. Cần giới hạn truy cập trực tiếp đến các microservice và chỉ tin tưởng header này từ nguồn đáng tin cậy. Nếu microservice cần tự xác thực người dùng, hãy cấu hình JWT Authentication tại chính service đó.

## 7. Triển khai bằng Docker Compose

API Gateway và các microservice được triển khai dưới dạng những Docker container riêng biệt, kết nối qua mạng bridge `swiftride-network`.

Cấu hình liên quan:

```yaml
api_gateway:
  environment:
    ASPNETCORE_ENVIRONMENT: Development
    ASPNETCORE_URLS: http://+:5000
    Jwt__Key: ${JWT_SECRET_KEY}
    Jwt__Issuer: "AuthService"
    Jwt__Audience: "AuthServiceUsers"
  ports:
    - "5000:5000"

auth_service:
  environment:
    ASPNETCORE_URLS: http://+:8081
    Jwt__Key: ${JWT_SECRET_KEY}
    Jwt__Issuer: "AuthService"
    Jwt__Audience: "AuthServiceUsers"

trip_service:
  environment:
    ASPNETCORE_URLS: http://+:8082
```

Các thông số JWT và chuỗi kết nối cơ sở dữ liệu được truyền qua biến môi trường.

Trong Docker network, Gateway gọi service bằng tên service và port nội bộ:

- Auth Service: `http://auth_service:8081`
- Trip Service: `http://trip_service:8082`

Từ máy tính cá nhân, client truy cập Gateway thông qua `http://localhost:5000`.

Các thông tin nhạy cảm như JWT secret và chuỗi kết nối cơ sở dữ liệu cần được bảo vệ, không commit trực tiếp lên Git.

## 8. Kiểm thử bằng Postman

### Ví dụ: Lấy thông tin chuyến đi theo ID

**Request:**

```http
GET http://localhost:5000/api/trip/{tripId}
```

**Header:**

```http
Authorization: Bearer <access_token>
```

Các bước thực hiện:

1. Chọn phương thức `GET`.
2. Nhập URL của Gateway kèm ID chuyến đi.
3. Mở tab **Authorization**.
4. Chọn loại **Bearer Token**.
5. Dán Access Token do Auth Service cấp.
6. Nhấn **Send** để gửi request.

Nếu token hợp lệ, Gateway chuyển tiếp request đến Trip Service. Trip Service xử lý nghiệp vụ và trả response thông qua Gateway.

### Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân cần kiểm tra |
|---|---|
| `401 Unauthorized` | Token, signing key, issuer, audience, thời hạn hoặc middleware xác thực |
| `403 Forbidden` | Authorization Policy hoặc quyền truy cập |
| `502 Bad Gateway` | Service đích, URL, port nội bộ hoặc Docker network |
| Không có `X-User-Id` | Tên claim trong JWT hoặc request transform |
| API Auth bất ngờ yêu cầu token | Cấu hình authorization của route và endpoint metadata |

Các lệnh kiểm tra Docker hữu ích:

```bash
docker compose ps
docker compose logs -f api_gateway
docker compose logs -f auth_service
docker compose logs -f trip_service
```

## 9. Tổng kết

API Gateway của SwiftRide đóng vai trò là cổng giao tiếp trung tâm giữa client và các microservice.

Quy trình xử lý request:

1. Client gửi HTTP request đến API Gateway.
2. Gateway xác định service đích dựa trên URL.
3. Với route được bảo vệ, Gateway xác thực JWT Access Token.
4. Authorization Policy kiểm tra request đã được xác thực hay chưa.
5. Gateway chuyển tiếp request đến microservice tương ứng.
6. Nếu có claim ID người dùng phù hợp, Gateway bổ sung header `X-User-Id`.
7. Microservice xử lý nghiệp vụ và trả kết quả về client thông qua Gateway.

Thiết kế này giúp tập trung việc định tuyến và xác thực tại một đầu mối, đồng thời duy trì sự độc lập về nghiệp vụ giữa các microservice.
