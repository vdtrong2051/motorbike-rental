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
| Database | SQL Server |
| Xác thực | Cookie Authentication (không dùng ASP.NET Identity đầy đủ) |
| Mật khẩu | `PasswordHasher<T>` (băm, không lưu plain text) |
| Giao diện | Bootstrap 5.3 + hệ thống thiết kế riêng (`site.css`) |
| Font | Oswald · Be Vietnam Pro · JetBrains Mono |

## Cấu trúc thư mục

```
RentalPlatform/
├── Controllers/        Account, Vehicle, Booking, Contract, Payment, Admin, Home
├── Data/                AppDbContext
├── Migrations/          Lịch sử migration EF Core
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
- SQL Server (LocalDB / Express / Developer đều được)

## Cài đặt & chạy

```powershell
# 1. Khôi phục package
dotnet restore

# 2. Cập nhật connection string trong appsettings.json nếu không dùng LocalDB mặc định

# 3. Áp migration để tạo database
dotnet ef database update

# 4. Chạy project
dotnet run
```

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
- Nếu `dotnet ef` báo thiếu công cụ: `dotnet tool install --global dotnet-ef`.
- Toàn bộ enum trạng thái (`TrangThaiXe`, `TrangThaiBooking`, `TrangThaiThanhToan`...) được lưu dạng chuỗi trong database (không phải số), dễ đọc trực tiếp khi kiểm tra dữ liệu bằng SQL.

## Nhóm thực hiện

Đồ án môn Công nghệ lập trình web — Khoa Công nghệ thông tin, CMC University.