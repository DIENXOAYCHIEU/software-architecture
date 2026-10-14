## Kiểm thử API với Postman

Dự án đã được cấu hình sẵn một bộ mã nguồn kiểm thử (Postman Collection) chứa đầy đủ các yêu cầu (Requests) mẫu và kịch bản kiểm thử luồng.

File cấu hình được lưu tại thư mục:
 `src/AuthService/docs/AuthService.postman_collection.json`

---

### 1. Hướng dẫn Tích hợp (Import) vào Postman

1. Mở phần mềm **Postman** trên máy tính của bạn.
2. Bấm vào nút **Import** ở góc trên cùng bên trái giao diện.
3. Kéo thả hoặc chọn đường dẫn đến file `AuthService.postman_collection.json` nêu trên.
4. Một thư mục Collection mới tên là `SwiftRide - AuthService` sẽ xuất hiện ở cột bên trái.

---

### 2. Chi tiết các Requests mẫu trong cấu hình

#### Dịch vụ Đăng ký tài khoản (POST /api/auth/register)
* **URL:** `{{auth_url}}/api/auth/register`
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


#### Dịch vụ Đăng nhập tài khoản (POST /api/auth/login)
* **URL:** `{{auth_url}}/api/auth/login`
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


#### Dịch vụ lấy tài khoản (GET /api/auth/{{userId}})
* **URL:** `{{auth_url}}/api/auth/99ff5b78-bc57-479f-a9ed-b56cf84ba5d9`

* **Dữ liệu phản hồi mẫu (Response - 200 OK):**
```json
"user": {
    "id": "99ff5b78-bc57-479f-a9ed-b56cf84ba5d9",
    "username": "user1",
    "password": "$2a$11$weI/UNZyT0xOr919am9XQuiSAOm6ZhUul6rKM9t/9rKYkSnhUE7MW"
}
```


#### Dịch vụ làm mới token (POST /api/auth/refresh)
* **URL:** `{{auth_url}}/api/auth/refresh`
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

#### Dịch vụ thu hồi token (POST /api/auth/revoke)
* **URL:** `{{auth_url}}/api/auth/revoke`
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

#### Dịch vụ Xác thực vùng cấm (GET /api/auth/protected)
* **URL:** `{{auth_url}}/api/auth/protected`
* **Xác thực (Authorization):** Chọn loại `Bearer Token` và truyền vào biến `{{accessToken}}`.
* **Dữ liệu phản hồi mẫu (Response - 200 OK):**
```json
"Hello, chuannguyen! This is a protected endpoint."
```
