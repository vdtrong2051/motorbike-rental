# GĐ3 — Reviews: đặc tả triển khai và nghiệm thu

Baseline: nhánh `develop`, commit `f50ed26f3f695df3a43bb334fb84734ad428a58a`.
Không tạo nhánh khác. Không merge `main`.

## Phạm vi

- Giữ `Reviews` cũ và luồng `/Review/Create?bookingId=...` cho `Booking`.
- Bổ sung `RentalOrderItemReviews` để mỗi dòng xe trong một `RentalOrder` hoàn thành có tối đa một đánh giá.
- Customer là người thuê thực tế; từ UI tới service luôn lấy CustomerId/VehicleId từ dữ liệu server.
- Trang xe hợp nhất rating/nhận xét từ `Reviews` và các `RentalOrderItemReviews` đang hiển thị.
- Owner xem đánh giá thuộc xe của mình; Admin lọc và ẩn/hiện đánh giá **mới**.
- Đánh giá `Booking` cũ vẫn được hiển thị và đưa vào thống kê; Admin chỉ xem chúng theo chế độ read-only vì không có cột ẩn/hiện trong schema cũ.
- Chưa làm reply, chỉnh sửa đánh giá, ảnh nhận xét, đánh giá chủ xe, phân trang/caching. GĐ4 phụ trách thiết kế UI tổng thể.

## Quan hệ dữ liệu

Bảng mới `RentalOrderItemReviews`:

| Cột | Kiểu | Ý nghĩa |
|---|---|---|
| Id | char(36), PK | Review ID |
| RentalOrderItemId | char(36), UNIQUE, FK | Mỗi lần thuê một chiếc xe chỉ một review |
| VehicleId | char(36), FK | Xe được đánh giá |
| CustomerId | char(36), FK | Người thuê thực tế |
| SoSao | int, CHECK 1–5 | Sao |
| NhanXet | varchar(1000) NULL | Bình luận |
| IsVisible | tinyint(1) | Admin ẩn/hiện |
| CreatedAt | datetime(6) UTC | Thời điểm gửi |

Không sửa dữ liệu hay cấu trúc `Reviews`, `Bookings`, `RentalOrders`, `RentalOrderItems`, `Vehicles`, `Users`.

**Không có EF migration mới**. SQL bổ sung: `db/gd3_order_item_reviews.sql`.

### Cần chuẩn bị database hiện tại

1. Xác định đúng tên database/connection string của dự án (`RentalPlatformDb` theo mặc định). **Không dùng database của RealEstateManagement**.
2. Tạo backup MySQL và thử khôi phục trong database kiểm thử. Đừng chạy nếu chưa có backup hợp lệ.
3. Mở script `db/gd3_order_item_reviews.sql` và chạy bằng DBeaver / MySQL client trên database của Motorbike Rental.
4. Xác nhận table có các khóa PK, UNIQUE và FOREIGN KEY như script.
5. Sau đó mới mở ứng dụng và sử dụng trang đánh giá. Nếu không tạo bảng, các đường dẫn review mới sẽ không hoạt động.

Ví dụ PowerShell gọi MySQL CLI, **chỉ sau khi sao lưu và xác nhận đúng database**:

```powershell
$repo = 'E:\Projects\motorbike-rental'
# Không tự chạy từ bộ cài: cần backup và chủ động xác nhận database.
cmd /c 'mysql -h 127.0.0.1 -u root -p RentalPlatformDb < E:\Projects\motorbike-rental\db\gd3_order_item_reviews.sql'
```

Không đưa mật khẩu DB vào lệnh, repo hoặc file báo cáo. Gói dữ liệu sẵn có tiếp tục được giữ nguyên. Bảng mới rỗng lúc đầu.

## Các luồng

- `GET /OrderReview/Create?itemId=...`: Customer đúng đơn hoàn thành, chưa đánh giá.
- `POST /OrderReview/Create`: CSRF-protected; Customer gửi 1–5 sao và nhận xét tối đa 1000 ký tự.
- `GET /OrderReview/OwnerReviews`: chỉ Owner, lọc theo `Vehicle.OwnerId`.
- `GET /OrderReview/AdminReviews?stars=1..5`: chỉ Admin, xem cả đánh giá mới và Review Booking cũ.
- `POST /OrderReview/SetVisibility`: chỉ Admin và chống CSRF; ẩn/hiện đánh giá mới, không xóa lịch sử.
- `/Vehicle/Details/{vehicleId}`: rating tổng hợp từ hai nguồn; bản ghi đã ẩn không tham gia trung bình.

Mọi thuộc tính từ phía client (CustomerId, VehicleId) bị bỏ qua; chỉ chấp nhận `RentalOrderItemId`, `SoSao`, `NhanXet` và xác định các FK còn lại từ DB.

## CI và EF Core 10

EF Core 9+ sẽ báo lỗi `PendingModelChangesWarning` khi áp dụng migration cũ bằng model đã thay đổi. GĐ3 không tắt cảnh báo này và không tạo migration giả. Thay vào đó:

- `scripts/ci/init-baseline-schema.sh` khởi tạo schema **cũ** bằng snapshot code đúng commit GĐ2 đã được nghiệm thu (detached worktree, không tạo branch).
- Workflow GĐ0/GĐ1/GĐ2 đổi cách khởi tạo database thử nghiệm sang script trên. Phần kiểm thử cũ giữ nguyên.
- Workflow `GD3 Reviews` tạo database MySQL 8 tạm mới, khởi tạo schema GĐ2, áp dụng SQL GĐ3 rồi chạy integration tests và HTTP smoke theo vai trò.
- Khi chạy app bình thường, không có lệnh tự nâng cấp schema hay khôi phục dữ liệu.

## Các kịch bản kiểm thử

- Customer chưa hoàn tất đơn không đánh giá được; khách khác và Owner bị chặn.
- Customer của đơn `HoanThanh` gửi một đánh giá có sao hợp lệ cho từng `RentalOrderItem`.
- Unique index MySQL từ chối gửi trùng, bao gồm đồng thời; frontend cũng chặn bằng kiểm tra trước.
- 0/6 sao và nhận xét quá 1000 ký tự bị từ chối.
- Review của `Booking` cũ còn nguyên và vẫn hiển thị trong danh sách Admin.
- Owner chỉ xem đánh giá xe do mình sở hữu, không thể thao tác quyền Admin.
- Admin ẩn review mới, trang công khai loại review đó khỏi danh sách và trung bình; có thể khôi phục.
- GĐ0, GĐ1, GĐ2, GĐ3 trên commit mới đều phải PASS.

## Trạng thái nghiệm thu

Kit cài đặt chỉ kiểm tra và build mã nguồn. Không tự chạy SQL trên DB dữ liệu người dùng. GĐ3 đạt nghiệm thu khi `dotnet build` PASS, bốn workflow CI PASS và bảng mới được áp dụng thành công trên môi trường ứng dụng cần dùng. Bước smoke UI trên dữ liệu thật cần thực hiện sau khi áp dụng schema.
