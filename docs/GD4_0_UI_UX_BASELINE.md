# GĐ4.0 — UI/UX Audit & Baseline (Motorbike Rental)

**Repository:** `vdtrong2051/motorbike-rental`
**Baseline đã đọc:** `develop` @ `22b7263320de2ec2e56228146042eaec01fffc86`
**Ngày khảo sát mã nguồn:** 07/11/2026
**Trạng thái:** *Khảo sát mã nguồn và thiết kế giải pháp; chưa tích hợp template, chưa thay UI.*

## 1. Kết luận điều hành

Ưu tiên GĐ4 là **giảm khó khăn khi hoàn tất nghiệp vụ thuê xe**, không phải chuyển màu/CSS theo một mẫu. Hướng thiết kế hai hệ thống mặt tiền trên một design system:

- **Marketplace + Customer:** khám phá, tìm xe, chi tiết, giỏ cùng Owner, checkout, theo dõi chuyến đi, thanh toán và đánh giá. Tham khảo cấu trúc car-rental (ứng viên Cental), nhưng **không nhập toàn bộ HTML/CSS**.
- **Owner + Admin workspace:** xử lý công việc, bảng dữ liệu, sidebar, calendar và dashboard. Tham khảo Tabler Bootstrap 5 (ứng viên). Chi tiết giấy phép và phiên bản **phải được xác nhận ở GĐ4.1** trước khi phân phối assets.
- **Ứng dụng giữ nguyên ASP.NET Core MVC .NET 10 + Razor + Bootstrap;** GĐ4.0 chưa có bundle/vendor mới, migration/schema hay sửa database.

Đã xác nhận trong source: **16 controller**, **38 ứng viên trang GET sử dụng Razor Views** (bao gồm trang legacy, loại trừ partial/layout và API), **21 hành động trọng yếu để bảo vệ**, **26 kịch bản nghiệm thu thiết kế**. Đây là *phạm vi kiểm kê có chủ đích*, không phải tuyên bố toàn bộ endpoint đã được kiểm thử runtime.

## 2. Nguồn khảo sát và giới hạn

Dựa trên cấu trúc source tree, `Program.cs`, 16 controller, các Razor Views chính (`Home`, `Vehicle`, `Cart`, `RentalOrder`, `OrderPayment`, `OrderReview`, `OwnerPortal`, `Admin`, `Account`, `Booking` cũ), các ViewModel tìm kiếm/đơn hàng, domain service và 4 workflow GD0–GD3.

- `docs/GD4_0_PAGE_INVENTORY.csv`: route GET -> role -> view -> nhiệm vụ -> giai đoạn.
- `docs/GD4_0_ACTION_INVENTORY.csv`: các thao tác có nguy cơ UX/security -> server guard phải giữ.
- `docs/GD4_0_ACCEPTANCE_MATRIX.csv`: kịch bản nghiệm thu.
- `audit/gd4_0_baseline.json`: manifest dùng cho static check, snapshot status/route.

**Chưa làm:** mở ứng dụng production/local trong trình duyệt thực, chụp ảnh giao diện, đo Lighthouse/CLS/LCP, audit WCAG, usability test với người dùng, kiểm thử trên điện thoại thật. Các nhận định hiển thị dưới đây là **suy luận từ code** hoặc điều kiện thiết kế cần kiểm chứng ở GĐ4.6, không phải kết quả đo UX.

**Baseline regression:** 4 GitHub Actions GD0/GD1/GD2/GD3 đều `success` tại SHA nêu trên. Sau mỗi mốc 4.1+ phải chạy lại trên SHA mới trước khi nghiệm thu.

## 3. Bản đồ kiến trúc hiện tại

| Lớp | Hiện trạng kiểm chứng từ source | Hệ quả UX |
|---|---|---|
| MVC routing | `Program.cs` mặc định `{controller=Home}/{action=Index}/{id?}`; một số API dùng attribute routes | Cần giữ URL/link hiện tại và URL token/QR riêng |
| Layout | `Views/_ViewStart.cshtml` trỏ `_Layout`; toàn bộ màn hình có layout chung trừ trang demo mobile đặt `Layout = null` | Navbar chung nhiều vai trò; nên tách theo use case nhưng không tách thiết kế/token |
| Auth | Cookie role `Customer`, `Owner`, `Admin`, `AccessDenied` | Menu ẩn không thay thế `[Authorize]`; phải duy trì ownership check |
| Login | `AccountController.Login` và `GoogleCallback` trả về `Home/Index` khi không có returnUrl | Cần landing theo vai trò, giữ returnUrl nếu hợp lệ |
| Discover | `Home/Index` form khu vực/loại xe; `VehicleSearchViewModel` đã có ngày nhận/trả | Homepage chưa chuyển đủ ngữ cảnh thời gian sang search |
| Cart | `CartService.AddAsync` chỉ chấp nhận **cùng một Owner**, session lưu IDs | Cần giải thích trước khi thêm xe Owner khác, không âm thầm xóa giỏ |
| Order | `RentalOrder` nhiều item cùng Owner; legacy `Booking` riêng | UI cần phân biệt nhưng giữ cả hai luồng chạy được |
| Payment | `OrderPayment`, QR demo và polling; signed-token mobile checkout | Demo phải được gắn nhãn rõ; không sao chép token vào analytics/log |
| Review | `Booking` Reviews cũ + `RentalOrderItemReviews` mới | Kết hợp rating nhưng moderation chỉ áp dụng review mới |
| Owner | OwnerPortal Dashboard/Calendar + Vehicles/OwnerOrders/OwnerReviews | Cần điều hướng theo việc chờ xử lý, không theo kiến trúc controller |
| Admin | Users/Payments/PendingVehicles/Violations/Policies/... đang cùng navbar | Cần workspace riêng, ưu tiên hàng đợi và thao tác trên bảng |

## 4. UX backlog ưu tiên (bằng chứng -> hướng giải quyết)

| ID | Ưu tiên | Bằng chứng nguồn | Vấn đề/điểm nghẽn | Yêu cầu GĐ4 | Mốc |
|---|---|---|---|---|---|
| F-01 | P0 | `Views/Home/Index.cshtml` + `VehicleSearchViewModel.cs` | Home chưa chọn ngày dù lọc xe hỗ trợ thời gian | Search bar khu vực, ngày nhận/trả, loại xe; preserve query/context | 4.2 |
| F-02 | P0 | `CartService.AddAsync` + `Vehicle/Details.cshtml` | Rule cùng Owner chỉ rõ khi thêm vào giỏ thất bại | Nêu điều kiện trước CTA, thông báo đổi chủ, giữ giỏ cũ | 4.2/4.3 |
| F-03 | P0 | `RentalOrder/Details.cshtml` | Raw enum trạng thái, nhiều thao tác trên chi tiết đơn | Status timeline + 1 primary CTA theo state, actor, payment | 4.3 |
| F-04 | P0 | `OrderPayment/Index` + `PaymentDemo/Checkout` | Nguy cơ gây hiểu lầm demo/real khi đổi giao diện | Nhãn DEMO, phân tách VietQR thật, polling, expiry, error/retry | 4.3 |
| F-05 | P0 | `RentalOrder` + `Booking` controllers | Hai dòng nghiệp vụ cùng tồn tại | Chốt RentalOrder là luồng chính trong IA, không xóa legacy | 4.3/4.6 |
| F-06 | P0 | `OrderReview` + `Vehicle/Details` | Hai nguồn đánh giá có policy khác nhau | Giao diện review thống nhất, ẩn/hiện không sai quyền | 4.3/4.5 |
| F-07 | P0 | `Admin/Users` + `Account/Profile` | CCCD/hồ sơ xác thực chứa dữ liệu nhạy cảm | Chỉ hiển thị trong vùng có quyền; không hiển thị bản xem trước công khai | 4.5 |
| F-08 | P1 | `AccountController.Login` | Owner/Admin được đưa về Home sau login | Landing theo role khi không có returnUrl, không phá deep-link | 4.1/4.4/4.5 |
| F-09 | P1 | `_Layout.cshtml` | Menu chức năng Customer/Owner/Admin chen trên navbar ngang | Layout public/customer; workspace sidebar Owner/Admin | 4.1 |
| F-10 | P1 | `Vehicle/Index.cshtml` | Form lọc nhiều hàng luôn mở | Sidebar desktop + filter offcanvas mobile, active chips, clear filter | 4.2 |
| F-11 | P1 | `Vehicle/Details.cshtml` | Hình/giá/điều kiện đặt chưa theo bố cục CTA liên tục | Gallery + key facts + giá/ngày + sticky CTA trên mobile | 4.2 |
| F-12 | P1 | `RentalOrder/Checkout.cshtml` | Personal fields + nhiều time ranges + chi phí trong một form lớn | Nhóm form, lỗi gắn field, tính tiền rõ, tóm tắt không mất khi fail | 4.3 |
| F-13 | P1 | `OwnerPortal/Dashboard` + `RentalOrder/OwnerOrders` | Chủ xe phải xem danh sách để lọc việc cần làm | Dashboard actionable queue: pending/confirm/cash/handover/return | 4.4 |
| F-14 | P1 | `Admin/*` | Hệ quản trị nhiều bảng thao tác inline | Filter, status chips, confirm destructive, empty/loading/feedback | 4.5 |
| F-15 | P1 | `_Layout` + site.css | Component và class tùy biến xen lẫn Bootstrap/CDN | Design tokens chung; phân lớp CSS có kiểm soát | 4.1 |
| F-16 | P1 | `Violation/Report`, `MyReports` | Báo cáo vi phạm tách khỏi journey đơn thuê | Entry theo chi tiết đơn; điều hướng về đơn hoặc danh sách | 4.3/4.4 |
| F-17 | P1 | Nhiều `Views/*` | Hiện trạng focus/responsive chưa được audit bằng trình duyệt | Đo và sửa 375, 768, 1024, 1440; keyboard/screenreader | 4.6 |
| F-18 | P1 | `Account/Profile` + `VehicleService` + `RentalOrderService` | User cần được xác thực trước giao dịch nhưng thường phát hiện muộn | Bảng chỉ dẫn hoàn tất hồ sơ và CTA quay lại ý định trước đó | 4.3 |

**Ưu tiên:** P0 = phải bảo vệ/ngăn dead end trước lúc triển khai thay đổi lớn; P1 = cần tối ưu và kiểm tra trong mốc GĐ4 liên quan. Không gọi “đã lỗi UI” nếu chưa đo trên trình duyệt.

## 5. Information Architecture đích (đề xuất, chưa code)

### Public / Customer top navigation

`Trang chủ` · `Tìm xe` · `Giỏ xe` (Customer) · `Đơn thuê` (Customer) · `Tài khoản`.

- Guest bấm thuê: đăng nhập với **returnUrl hợp lệ**; vào đúng chi tiết xe/giỏ tùy ngữ cảnh.
- Customer đã đăng nhập: `Đơn thuê của tôi` và trạng thái việc cần làm; **không dashboard admin hóa trải nghiệm khách thuê**.
- Search/filter + date context dùng chung giữa Home, listing, detail, Cart, Checkout (bất cứ dữ liệu nào xử lý lại phải xác nhận server).

### Owner workspace sidebar

`Tổng quan` · `Đơn cần xử lý` · `Xe của tôi` · `Đăng xe` · `Lịch thuê` · `Đánh giá` · `Doanh thu` · `Hồ sơ`.

### Admin workspace sidebar

`Tổng quan` · `Duyệt xe` · `Xe` · `Người dùng & xác thực` · `Thanh toán & hoàn tiền` · `Vi phạm` · `Đánh giá` · `Chính sách`.

**Không thêm màn hình/API chưa tồn tại chỉ để vẽ UI.** Các mục "Đơn cần xử lý", "Thanh toán & hoàn tiền" là các nhóm/bộ lọc phải được thực thi trong mốc tương ứng; nếu chưa có thì link vào màn hình đã tồn tại.

## 6. Trạng thái đơn và chỉ dẫn UX (không sửa business rules)

Hệ thống có **hai trục trạng thái khác nhau**: `TrangThaiDonThue` và `TrangThaiThanhToanDon`. UI không được suy diễn từ một trục duy nhất.

| Order state | Customer đọc được | Owner đọc được | Primary next action gợi ý (chỉ khi backend cho phép) |
|---|---|---|---|
| `ChoChuXeXacNhan` | Đang chờ chủ xe xác nhận | Có đơn cần xem xét | Owner: Xác nhận / Từ chối-hủy theo nghiệp vụ |
| `DaXacNhan` | Chủ xe đã xác nhận | Đơn đã nhận | Hai bên: xem và ký hợp đồng; Customer: chuẩn bị thanh toán |
| `ChoThanhToan` | Cần hoàn tất thanh toán | Chờ khách thanh toán | Customer: chọn/tiếp tục thanh toán |
| `DaThanhToan` | Đã ghi nhận thanh toán | Chuẩn bị giao xe | Owner: giao xe khi thỏa điều kiện |
| `DangThue` | Đang sử dụng xe; thời hạn trả | Xe đang thuê | Owner: nhận lại và hoàn tất, nếu đủ điều kiện |
| `HoanThanh` | Đơn hoàn thành | Đã nhận xe | Customer: đánh giá từng xe chưa đánh giá |
| `DaHuy` | Đã hủy; kiểm tra hoàn tiền | Đã hủy | Hiển thị lý do, số tiền hoàn dự kiến nếu có |

Payment state: `ChuaChon`, `ChoThanhToan`, `ChoThuTienKhiNhanXe`, `DaThanhToan`, `ThatBai`, `ChoHoanTien`, `DaHoanTien`.

- Khi hợp đồng chỉ một bên ký: hiển thị **bên nào đã ký và ai còn chờ**. Không tự tạo hành động cho bên không được phép.
- Khi `ChoThuTienKhiNhanXe`: chỉ Owner xác nhận tiền mặt; Customer không được tự đánh dấu đã thanh toán.
- Khi `ThatBai`: cho phép retry/đổi phương thức theo `PaymentCore`, không tái sử dụng token cũ.
- Khi `ChoHoanTien`: không báo đã hoàn tiền, phân biệt dự kiến và đã hoàn.
- Mọi CTA phải được điều kiện hóa dựa trên ownership, trạng thái, hợp đồng, payment và API hiện có. Bảng trên là **UX content plan**, không phải đặc tả lại state machine.

## 7. Guardrails kỹ thuật GĐ4.1–4.7

1. Giữ Razor Controller/Action/Route, model binding và anti-forgery; không thay business service, EF migrations, cấu trúc MySQL khi chỉ đổi UI.
2. Bảo tồn HTTP flows: GD0 role/login, GD1 payment core, GD2 QR/callback/polling, GD3 Reviews, legacy Booking.
3. Không load chồng Bootstrap 5 hiện tại với bundle chứa Bootstrap của Tabler; phải pin phiên bản, giấy phép và asset source ở 4.1.
4. QR DEMO phải luôn có nhãn rõ; VietQR ngân hàng (nếu bật) không tự xác nhận giao dịch; token không đi vào analytics.
5. Nhãn trạng thái tiếng Việt, không dựa độc nhất vào màu; chỉ một primary CTA trong mỗi state.
6. Responsive 375/768/1024/1440; ngăn scroll ngang; form label, focus-visible, keyboard, reduced motion.
7. Nội dung hợp đồng, CCCD, profile, thanh toán thuộc màn hình có quyền; CSS/layout không lộ nội dung qua navbar hoặc cache.
8. Không xóa Booking/Review cũ; ghi rõ nhãn "Đơn thuê mới"/"Đơn cũ" nếu cả hai cần hiện đồng thời.
9. `README`/audit baseline phải được cập nhật khi thêm mới màn hình hoặc thay thế luồng, không tự sửa snapshot ban đầu.

## 8. Hợp đồng nghiệm thu GĐ4.0

**Đạt khi:**

- [x] Kiểm kê source, vai trò, route GET quan trọng và hành động nghiệp vụ.
- [x] Đề xuất IA, tách Customer/Owner/Admin và phân định legacy.
- [x] Đưa thành UX backlog ưu tiên, có nguồn code và mốc giải quyết.
- [x] Khóa danh sách 26 bài nghiệm thu ở `GD4_0_ACCEPTANCE_MATRIX.csv`.
- [x] Chuẩn bị script kiểm tra tĩnh read-only và workflow riêng, không thay runtime/database.
- [ ] Chạy script trên workspace thực và workflow GĐ4.0 PASS trên GitHub (sau khi apply).
- [ ] Xác nhận 4 workflow GD0–GD3 vẫn PASS trên commit GĐ4.0 (sau khi apply).

**Điều kiện bắt đầu 4.1:** các checkbox cuối được xác minh; chưa cần cài template cho 4.0.

## 9. GĐ4.1 cần quyết định những gì

1. Pin phiên bản và kiểm tra license của Tabler và nguồn tham khảo Marketplace; không mang demo images có license chưa xác minh vào repo.
2. Quyết định `PublicLayout` / `CustomerLayout` / `WorkspaceLayout` và mapping trang; không cắt `_Layout` trước khi có smoke test.
3. Design tokens và partial contract (VehicleCard, MoneyBreakdown, OrderState, OrderNextAction, EmptyState, FormFeedback).
4. Prototype 4 màn hình lõi: listing, order detail customer, owner queue, admin users, thử mobile rồi mới mở rộng.
5. Chọn cách xử lý đăng nhập theo vai trò khi **không có** returnUrl; bảo toàn deep-link và login regression.

**Tài liệu liên quan:** `GD4_0_USER_JOURNEYS.md`, `GD4_0_COMPONENT_CONTRACTS.md`, 3 CSV, `audit/gd4_0_baseline.json`.
