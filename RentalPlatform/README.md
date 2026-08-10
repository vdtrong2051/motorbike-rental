# RentalPlatform

Nền tảng cho thuê xe máy & ô tô tự lái — đồ án môn **Công nghệ lập trình web**.

Người dùng tìm xe theo khu vực/loại xe/mức giá, đặt xe, ký hợp đồng điện tử với chủ xe, đặt cọc và thanh toán, rồi xác nhận giao/nhận xe — toàn bộ luồng thuê xe tự lái từ đầu đến cuối.

## Tính năng theo vai trò

**Khách thuê (Customer)**
- Tìm & lọc xe theo khu vực, loại xe, khoảng giá (có phân trang)
- Đặt xe theo khoảng ngày — hệ thống tự kiểm tra trùng lịch
- Ký hợp đồng điện tử, đặt cọc và thanh toán phần còn lại (mô phỏng)
- Xem lịch sử đặt xe, huỷ đơn khi còn ở trạng thái chờ/đã xác nhận

**Chủ xe (Owner)**
- Đăng xe cho thuê kèm nhiều ảnh, chỉnh sửa / xoá xe, chuyển trạng thái Sẵn sàng ↔ Bảo trì
- Xem & xác nhận đơn đặt xe, xác nhận giao xe / nhận lại xe
- Ký hợp đồng điện tử phía chủ xe

**Quản trị viên (Admin)**
- Duyệt hoặc từ chối xe mới đăng (kèm lý do từ chối)
- Xem chi tiết mọi đơn đặt xe / hợp đồng trong hệ thống

## Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core MVC (.NET 10) |
| ORM | Entity Framework Core 10 |
| Database | MySQL 8.0+ (`MySql.EntityFrameworkCore` 10.0.9) |
| Xác thực | Cookie Authentication (không dùng ASP.NET Identity đầy đủ) |
| Mật khẩu | `PasswordHasher<T>` (băm, không lưu plain text) |
| Giao diện | Bootstrap 5.3 + hệ thống thiết kế riêng (`site.css`) |
| Font | Oswald · Be Vietnam Pro · JetBrains Mono |

## Cấu trúc thư mục

```
RentalPlatform/
├── Controllers/        Account, Vehicle, Booking, Contract, Payment, Admin, Home
├── Data/                AppDbContext
├── MySqlMigrations/     Migration đang sử dụng cho MySQL
├── SqlServerMigrationsArchive/ Migration SQL Server cũ, không được biên dịch
├── Models/
│   ├── Entities/        User, Vehicle, VehicleImage, Booking, Contract, Payment
│   └── ViewModels/       Theo từng nhóm chức năng (Account/Vehicle/Booking/Payment)
├── Repositories/        Truy vấn dữ liệu (tách khỏi Service)
├── Services/            Nghiệp vụ chính: kiểm tra trùng lịch, sinh hợp đồng, xử lý thanh toán...
├── Views/               Razor Views tương ứng từng Controller
├── wwwroot/             CSS, ảnh xe upload
└── Program.cs           Cấu hình DI, Cookie Auth, seed tài khoản Admin
```

## Yêu cầu môi trường

- .NET 10 SDK (`dotnet --list-sdks` để kiểm tra — nếu chỉ có .NET 8, xem phần **Ghi chú** bên dưới)
- MySQL Server 8.0 trở lên

## Cài đặt & chạy

```powershell
# 1. Khôi phục package
dotnet restore

# 2. Khôi phục dotnet-ef 10.0.9 cục bộ
dotnet tool restore

# 3. Cấu hình kết nối MySQL (khuyến nghị dùng biến môi trường)
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Port=3306;Database=RentalPlatformDb;User=root;Password=MAT_KHAU_MYSQL;CharSet=utf8mb4;SslMode=Disabled"

# 4. Áp migration MySQL để tạo database
dotnet dotnet-ef database update

# 5. Chạy project
dotnet run
```

Connection string mẫu cũng nằm trong `appsettings.json`. Hãy thay `ChangeMe` bằng
mật khẩu thật hoặc dùng biến môi trường như trên; không commit mật khẩu production.
Tài khoản MySQL cần quyền tạo/cập nhật schema `RentalPlatformDb`.

Lần chạy đầu tiên, hệ thống tự tạo sẵn 1 tài khoản Admin nếu database chưa có Admin nào:

| Email | Mật khẩu |
|---|---|
| `admin@rentalplatform.vn` | `Admin@123` |

> Nên đổi mật khẩu này hoặc gỡ đoạn seed trong `Program.cs` trước khi nộp bài / demo thật.

## Luồng test nhanh gợi ý

1. Đăng ký 1 tài khoản **Owner** → đăng xe cho thuê (kèm ảnh)
2. Đăng nhập **Admin** → duyệt xe vừa đăng ở `Admin/PendingVehicles`
3. Đăng ký 1 tài khoản **Customer** → tìm xe, đặt xe theo khoảng ngày
4. Đăng nhập lại **Owner** → xác nhận đơn ở `Booking/Requests` (hợp đồng điện tử tự sinh)
5. Cả 2 bên vào `Contract/Details` ký hợp đồng
6. **Customer** đặt cọc rồi thanh toán phần còn lại ở `Booking/Details`
7. **Owner** xác nhận giao xe, rồi xác nhận nhận lại xe khi kết thúc

## Ghi chú

- Nếu máy bạn chỉ có **.NET 8 SDK**: mở `RentalPlatform.csproj`, đổi `<TargetFramework>net10.0</TargetFramework>` → `net8.0`, và hạ version 3 package `Microsoft.EntityFrameworkCore.*` từ `10.0.9` xuống bản `8.0.x` tương ứng.
- Dự án có `dotnet-tools.json` khóa `dotnet-ef` ở phiên bản 10.0.9. Dùng
  `dotnet tool restore` rồi chạy `dotnet dotnet-ef ...`.
- Toàn bộ enum trạng thái (`TrangThaiXe`, `TrangThaiBooking`, `TrangThaiThanhToan`...) được lưu dạng chuỗi trong database (không phải số), dễ đọc trực tiếp khi kiểm tra dữ liệu bằng SQL.
- `database-init-mysql.sql` là script tạo toàn bộ schema MySQL tương đương migration.
- `SqlServerMigrationsArchive` và `database-upgrade-sqlserver-archive.sql` chỉ để
  tham khảo lịch sử; không chạy các file này trên MySQL.
- Việc đổi provider chỉ tạo schema MySQL, không tự sao chép dữ liệu cũ từ LocalDB.
  Nếu cần giữ dữ liệu SQL Server cũ, cần thực hiện ETL/export-import riêng.

## Nhóm thực hiện

Đồ án môn Công nghệ lập trình web — Khoa Công nghệ thông tin, CMC University.

## Nâng cấp luồng thuê nhiều xe

Phiên bản này bổ sung luồng mới song song với dữ liệu `Booking` cũ:

- `Cart`: giỏ xe lưu trong Session, hỗ trợ nhiều xe của cùng một chủ trong một lần checkout.
- `RentalOrder` + `RentalOrderItem`: đơn thuê tổng và từng xe, mỗi xe có thời gian nhận/trả riêng.
- `RentalOrderContract`: hợp đồng snapshot có thông tin khách, CCCD, GPLX, địa chỉ, danh sách xe, biển số, tình trạng, thời gian và giá thuê.
- Xuất hợp đồng Word `.docx`: mẫu A4 có thông tin hai bên, bảng nhiều xe, giá tiền bằng số/bằng chữ, 10 điều khoản, trạng thái ký điện tử và phụ lục bàn giao/hoàn trả.
- Hồ sơ cá nhân bổ sung CCCD, GPLX và địa chỉ; thông tin chủ xe được snapshot tại thời điểm lập hợp đồng.
- `RentalOrderPayment`: một giao dịch tổng cho toàn đơn, hỗ trợ chuyển khoản QR hoặc thanh toán khi nhận xe.
- `OwnerPortal/Calendar`: lịch cho thuê xe theo tháng/tuần/danh sách.
- `OwnerPortal/Dashboard`: doanh thu ngày, tháng, năm và biểu đồ 12 tháng.
- Bộ lọc xe nâng cao: từ khoá, khu vực, loại, giá, hộp số, nhiên liệu, số chỗ, thời gian còn trống và sắp xếp.

Sau khi lấy code mới, chạy:

```powershell
dotnet restore
dotnet tool restore
dotnet dotnet-ef database update
dotnet run
```

### Cấu hình QR và webhook

Cập nhật `Payment` trong `appsettings.json` hoặc dùng biến môi trường/User Secrets:

- `BankId`: mã BIN ngân hàng.
- `AccountNumber`, `AccountName`: tài khoản nhận tiền.
- `WebhookSecret`: chuỗi bí mật mạnh, không dùng giá trị mẫu khi triển khai thật.

Endpoint nhận thông báo là `POST /api/payments/webhook`, header
`X-Webhook-Secret` phải khớp cấu hình. Payload:

```json
{
  "orderId": "GUID_CUA_DON",
  "reference": "MA_GIAO_DICH_TREN_TRANG_THANH_TOAN",
  "status": "PAID",
  "amount": 1500000,
  "gatewayNote": "Bank transaction id"
}
```

Hệ thống kiểm tra secret, mã tham chiếu và đúng số tiền trước khi đánh dấu đã thanh toán.
VietQR chỉ tạo nội dung QR; để nhận thông báo giao dịch thật cần cấu hình webhook từ
nhà cung cấp banking/payment gateway đang sử dụng. Ở môi trường Development có nút
mô phỏng webhook để demo toàn bộ luồng.

### Tải hợp đồng Word

Khách thuê, chủ xe hoặc Admin có quyền xem đơn mở `RentalOrder/Details/{id}` và
chọn **Tải hợp đồng Word (.docx)**. File được sinh trực tiếp trên máy chủ bằng OOXML,
không yêu cầu cài Microsoft Word. Sau khi cập nhật phiên bản này, cần chạy
`dotnet dotnet-ef database update` để bổ sung các trường hồ sơ và snapshot chủ xe.

Mẫu hợp đồng là nền tảng kỹ thuật cho dự án, không thay thế việc luật sư rà soát
điều khoản trước khi sử dụng trong hoạt động kinh doanh thực tế.

## Nâng cấp xác thực, quản trị và thanh toán

Phiên bản mới bổ sung OAuth 2.0 Google, hồ sơ bắt buộc và quy trình Admin xác thực. Customer chỉ
được checkout, Owner chỉ được đăng xe/xác nhận đơn khi hồ sơ có họ tên, email, điện thoại, CCCD,
GPLX, địa chỉ, ngày sinh, giới tính và trạng thái `DaXacThuc`.

Admin có các trang `Admin/Users`, `Admin/Policies`, `Admin/Violations`, `Admin/Payments` và
`Admin/Dashboard` để xác thực/khóa tài khoản, đặt tỷ lệ cọc–phí dịch vụ–hoàn tiền, xử lý báo cáo,
đối soát giao dịch và xem doanh thu ngày/tháng/năm. Customer và Owner báo cáo bên còn lại từ
chi tiết đơn thuê. Tìm xe hỗ trợ hãng, địa điểm, lịch trống và sắp xếp theo độ phổ biến.

Nâng cấp database hiện có bằng một trong hai cách:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update
```

Hoặc mở `database-upgrade-advanced-features.sql` trong MySQL Workbench và chạy toàn bộ script.

Để bật Google OAuth, tạo OAuth Client loại Web application trong Google Cloud, thêm callback
`https://TEN-MIEN-CUA-BAN/signin-google`, sau đó đặt hai biến môi trường
`Authentication__Google__ClientId` và `Authentication__Google__ClientSecret`. Khi chạy localhost,
thêm callback HTTPS đúng cổng Visual Studio, ví dụ `https://localhost:64506/signin-google`.

Ví điện tử và thẻ đã có luồng tạo/ghi nhận giao dịch dùng chung webhook. Muốn thu tiền thật cần
kết nối nhà cung cấp thanh toán và để nhà cung cấp gọi `POST /api/payments/webhook`; hệ thống không
tự coi giao dịch là thành công nếu chưa nhận thông báo hợp lệ.

## Ảnh căn cước công dân

Trang hồ sơ cho phép chụp bằng camera điện thoại hoặc tải ảnh mặt trước/mặt sau CCCD. Chấp nhận
JPG, PNG, WebP hợp lệ và giới hạn 5 MB mỗi ảnh. Ảnh được lưu ngoài `wwwroot`, không có URL công
khai; chỉ chủ tài khoản và Admin có quyền xem qua endpoint đã xác thực. Hai ảnh là thành phần bắt
buộc của hồ sơ trước khi gửi Admin xác thực.

Mặc định ảnh nằm trong `App_Data/IdentityDocuments` (đã được gitignore). Khi triển khai Linux,
đặt `IdentityDocuments__StoragePath=/var/lib/rentalplatform/identity-documents` và cấp quyền ghi
thư mục này cho tài khoản chạy dịch vụ. Sau khi lấy code mới, chạy:

```powershell
dotnet tool run dotnet-ef database update
```
