## Auth Service — Dịch vụ xác thực và phân quyền

Auth Service là dịch vụ chịu trách nhiệm quản lý tài khoản và xác thực danh tính người dùng trong hệ thống SwiftRide. Dịch vụ cung cấp các API đăng ký, đăng nhập, truy vấn tài khoản, làm mới token, thu hồi token và xác thực quyền truy cập vào các tài nguyên được bảo vệ.

Auth Service sử dụng **JWT (JSON Web Token)** để xác thực các yêu cầu từ client và **Refresh Token** để cấp lại Access Token khi cần thiết. Các service khác có thể sử dụng Access Token để xác minh danh tính người dùng trước khi xử lý những yêu cầu cần xác thực.

* File cấu hình được lưu tại thư mục:
 `src/AuthService/docs/AuthService.postman_collection.json`

### 1. Chức năng chính

- **Đăng ký tài khoản:** Tạo tài khoản người dùng mới và cấp token sau khi đăng ký thành công.
- **Đăng nhập:** Kiểm tra thông tin đăng nhập và cấp Access Token cùng Refresh Token.
- **Lấy thông tin tài khoản:** Truy vấn tài khoản theo `userId`.
- **Làm mới token:** Cấp Access Token mới thông qua Refresh Token hợp lệ.
- **Thu hồi token:** Vô hiệu hóa Refresh Token khi người dùng đăng xuất hoặc cần kết thúc phiên đăng nhập.
- **Xác thực endpoint được bảo vệ:** Kiểm tra Access Token để xác định yêu cầu có được phép truy cập tài nguyên hay không.

### 2. Danh sách API

| Method | Endpoint | Chức năng |
|---|---|---|
| POST | `/api/auth/register` | Đăng ký tài khoản |
| POST | `/api/auth/login` | Đăng nhập |
| GET | `/api/auth/{userId}` | Lấy thông tin tài khoản |
| POST | `/api/auth/refresh` | Làm mới token |
| POST | `/api/auth/revoke` | Thu hồi Refresh Token |
| GET | `/api/auth/protected` | Kiểm tra xác thực bằng Access Token |

### 3. Mô tả chi tiết các API

#### 3.1. Đăng ký tài khoản

**`POST /api/auth/register`**

Tạo tài khoản mới từ thông tin đăng ký. Nếu đăng ký thành công, hệ thống có thể trả về thông tin người dùng, Refresh Token và Access Token để người dùng bắt đầu sử dụng hệ thống.

* **Định dạng dữ liệu gửi (Body - JSON):**
```json
{
  "username": "user1",
  "password": "password"
}
```
* **Dữ liệu phản hồi mẫu (Response - 200 OK):**
```json
{
    "user": {
        "id": "99ff5b78-bc57-479f-a9ed-b56cf84ba5d9",
        "username": "user1",
        "password": "$2a$11$weI/UNZyT0xOr919am9XQuiSAOm6ZhUul6rKM9t/9rKYkSnhUE7MW"
    },
    "refreshToken": {
        "id": "6746c788-d4b6-4b3b-a338-a09b98c029fa",
        "token": "0b7d14a8-b3c1-47cb-b7fb-11f5a4d2fe2a",
        "userId": "99ff5b78-bc57-479f-a9ed-b56cf84ba5d9",
        "expires": "2026-12-13T09:29:05.6007934Z",
        "isCreatedByServer": true,
        "isRevoked": false,
        "isUsed": false,
        "isExpired": false,
        "isActive": true
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI5OWZmNWI3OC1iYzU3LTQ3OWYtYTllZC1iNTZjZjg0YmE1ZDkiLCJuYW1lIjoidXNlcjEiLCJqdGkiOiJhNWEzNWUxYy00ZjUyLTQzNmEtOWJkNi0zYTg3MmYzMjY5MGEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijk5ZmY1Yjc4LWJjNTctNDc5Zi1hOWVkLWI1NmNmODRiYTVkOSIsImV4cCI6MTc5MTk3MDc0NSwiaXNzIjoiQXV0aFNlcnZpY2UiLCJhdWQiOiJBdXRoU2VydmljZVVzZXJzIn0.oKAKXtoZCPWv0UvAtv3bwFgA2sAASE9vwj7zq7oRIYY"
}
```

#### 3.2. Đăng nhập

**`POST /api/auth/login`**

* **Định dạng dữ liệu gửi (Body - JSON):**
```json
{
  "username": "user1",
  "password": "password"
}
```
* **Dữ liệu phản hồi mẫu (Response - 200 OK):**
```json
{
    "user": {
        "id": "99ff5b78-bc57-479f-a9ed-b56cf84ba5d9",
        "username": "user1",
        "password": "$2a$11$weI/UNZyT0xOr919am9XQuiSAOm6ZhUul6rKM9t/9rKYkSnhUE7MW"
    },
    "refreshToken": {
        "id": "6746c788-d4b6-4b3b-a338-a09b98c029fa",
        "token": "0b7d14a8-b3c1-47cb-b7fb-11f5a4d2fe2a",
        "userId": "99ff5b78-bc57-479f-a9ed-b56cf84ba5d9",
        "expires": "2026-12-13T09:29:05.6007934Z",
        "isCreatedByServer": true,
        "isRevoked": false,
        "isUsed": false,
        "isExpired": false,
        "isActive": true
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI5OWZmNWI3OC1iYzU3LTQ3OWYtYTllZC1iNTZjZjg0YmE1ZDkiLCJuYW1lIjoidXNlcjEiLCJqdGkiOiJhNWEzNWUxYy00ZjUyLTQzNmEtOWJkNi0zYTg3MmYzMjY5MGEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijk5ZmY1Yjc4LWJjNTctNDc5Zi1hOWVkLWI1NmNmODRiYTVkOSIsImV4cCI6MTc5MTk3MDc0NSwiaXNzIjoiQXV0aFNlcnZpY2UiLCJhdWQiOiJBdXRoU2VydmljZVVzZXJzIn0.oKAKXtoZCPWv0UvAtv3bwFgA2sAASE9vwj7zq7oRIYY"
}
```

#### 3.3. Lấy thông tin tài khoản

**`GET /api/auth/{userId}`**
* **Dữ liệu phản hồi mẫu (Response - 200 OK):**
```json
"user": {
    "id": "99ff5b78-bc57-479f-a9ed-b56cf84ba5d9",
    "username": "user1",
    "password": "$2a$11$weI/UNZyT0xOr919am9XQuiSAOm6ZhUul6rKM9t/9rKYkSnhUE7MW"
}
```

#### 3.4. Làm mới token

**`POST /api/auth/refresh`**
* **Định dạng dữ liệu gửi (Body - JSON):**
```json
{
    "refreshToken": "2367aae2-0649-4db0-a1d0-002606e013e8"
}
```
* **Dữ liệu phản hồi mẫu (Response - 200 OK):**
```json
{
    "user": {
        "id": "9d5a8ed1-3ec1-4413-a773-81ae5bd9c727",
        "username": "user5",
        "password": "$2a$11$OsdT.0XD.Rsw0Z6E3Mmd1.QvY.X6HYSC9/F9iSFbKOfG1iXYsTI2u"
    },
    "refreshToken": {
        "id": "dd118110-9f7a-4d1b-bb1b-5f744c1658c9",
        "token": "50282312-cefc-4c78-a4c3-141a4a548ce8",
        "userId": "9d5a8ed1-3ec1-4413-a773-81ae5bd9c727",
        "expires": "2026-12-13T08:54:08.0854845Z",
        "isCreatedByServer": true,
        "isRevoked": false,
        "isUsed": false,
        "isExpired": false,
        "isActive": true
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI5ZDVhOGVkMS0zZWMxLTQ0MTMtYTc3My04MWFlNWJkOWM3MjciLCJuYW1lIjoidXNlcjUiLCJqdGkiOiIzZDRkZDAwNy1kOTZmLTQyNzktYWFjYS0zZmU3M2ViMDE2MzEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjlkNWE4ZWQxLTNlYzEtNDQxMy1hNzczLTgxYWU1YmQ5YzcyNyIsImV4cCI6MTc5MTk2ODY0OCwiaXNzIjoiQXV0aFNlcnZpY2UiLCJhdWQiOiJBdXRoU2VydmljZVVzZXJzIn0.YTC12syNVid-G7Y3MYK4Zl6PXATtr5MsbRscdNfg300"
}
```

#### 3.5. Thu hồi token

**`POST /api/auth/revoke`**

* **Định dạng dữ liệu gửi (Body - JSON):**
```json
{
    "refreshToken": "2367aae2-0649-4db0-a1d0-002606e013e8"
}
```
* **Dữ liệu phản hồi mẫu (Response - 200 OK):**
```json
Đã thu hồi token thành công.
```
#### 3.6. Xác thực endpoint được bảo vệ

**`GET /api/auth/protected`**
* **Xác thực (Authorization):** Chọn loại `Bearer Token` và truyền vào biến `{{accessToken}}`.
* **Dữ liệu phản hồi mẫu (Response - 200 OK):**
```json
"Hello, chuannguyen! This is a protected endpoint."
```

### 4. Luồng xác thực

```mermaid
flowchart TD
    A[Client] --> B[Đăng ký hoặc đăng nhập]
    B --> C[Auth Service kiểm tra thông tin]
    C --> D{Thông tin hợp lệ?}
    D -->|Không| E[Trả về lỗi]
    D -->|Có| F[Cấp Access Token và Refresh Token]
    F --> G[Client gọi API được bảo vệ]
    G --> H{Access Token hợp lệ?}
    H -->|Có| I[Cho phép xử lý yêu cầu]
    H -->|Không hoặc hết hạn| J[Yêu cầu làm mới token]
    J --> K[Auth Service kiểm tra Refresh Token]
    K --> L{Refresh Token hợp lệ?}
    L -->|Có| M[Cấp token mới]
    L -->|Không| N[Yêu cầu đăng nhập lại]
```

### 5. Vai trò của Access Token và Refresh Token

| Tiêu chí | Access Token | Refresh Token |
|---|---|---|
| Mục đích | Xác thực các yêu cầu API | Xin cấp token mới |
| Cách sử dụng | Gửi trong header Bearer | Gửi đến API refresh |
| Thời hạn | Thường ngắn | Thường dài hơn |
| Khi hết hạn | Cần làm mới hoặc đăng nhập lại | Phải đăng nhập lại nếu không còn hợp lệ |
| Bảo vệ | Không để lộ ra ngoài | Lưu trữ và truyền tải đặc biệt cẩn thận |
