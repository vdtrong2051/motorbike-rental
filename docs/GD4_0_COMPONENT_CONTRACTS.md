# GĐ4.0 — Component contracts cho GĐ4.1–4.7

Các contract ở đây là **đề xuất**, chưa tạo code/partial trong GĐ4.0.

## Layout và ownership

| Layout đích | Trang dùng | Menu | Chức năng ưu tiên |
|---|---|---|---|
| `_PublicLayout` | Home, Vehicle search/details, Account login/register | Trang chủ, tìm xe, đăng nhập | Discovery/decision |
| `_CustomerLayout` | Cart, Checkout, MyOrders, Order Details, OrderPayment, OrderReview, Profile | Tìm xe, giỏ, đơn, tài khoản | Conversion/progress |
| `_WorkspaceLayout` (Owner) | OwnerPortal, MyVehicles, OwnerOrders, OwnerReviews | Dashboard, công việc, xe, lịch, đánh giá, doanh thu | Work queue |
| `_WorkspaceLayout` (Admin) | Admin/*, AdminReviews | Dashboard, duyệt, users, payments, policies | Triage + security |
| Standalone checkout | PaymentDemo/Checkout | Không có nav thông thường | Signed-token DEMO, mobile-first |

Chuyển sang layout mới theo từng route: phải giữ `_ViewImports`, TagHelpers, `@RenderSectionAsync("Scripts", required: false)`, anti-forgery, validation partial, script payment/QR, bootstrap offcanvas/modal và tracking state.

## Component reusable contracts

| Component | Inputs tối thiểu | Hành vi/semantics | Phục vụ |
|---|---|---|---|
| VehicleCard | Id, tên, ảnh, khu vực, giá/ngày, số chỗ, status, review summary | CTA theo role; alt text; ảnh fallback | Search, favorites (nếu triển khai sau) |
| SearchBar | Location, type, pickup/return datetime | Preserves query, server revalidates | Home/list |
| FilterPanel | SearchViewModel + applied filters | Apply/reset; mobile offcanvas, count/empty state | Listing |
| PriceSummary | Rent, deposit, service fee, total | Đồng nhất số tiền server, không suy diễn tiền hoàn | Checkout/payment/order |
| OrderStateBadge | Order state, payment state | Text + icon + màu phù hợp; không raw enum | Order list/detail |
| OrderNextAction | actor, order state, payment state, signature state, ownership | Chỉ 1 CTA chính; nếu không có -> "Đang chờ ..." | Customer/Owner order detail |
| OrderTimeline | Timestamps/statuses từ server | Không đánh dấu hoàn tất khi chưa có bằng chứng | Order detail |
| FormFeedback | ModelState + pending/success/error | Error summary, field error, focus/aria-describedby | All forms |
| ActionConfirm | Label, consequence, post action | Anti-forgery, xác nhận với nội dung cụ thể | Cancel, handover, admin actions |
| OwnerQueueCard | Pending count, target filter | Không giả lập số liệu | Owner dashboard |
| AdminDataTable | Item list, filter, role/action, status | Empty/loading/pagination khi có backend | Admin |
| ReviewSummary | Legacy+GD3 public reviews | Respect moderation, no duplicate counting | Vehicle details |

## Design tokens đề xuất

- Giữ `Be Vietnam Pro` cho nội dung; font display/mono dùng có kiểm soát, không ép uppercase vào mọi label.
- Bảng màu hiện có `--color-asphalt`, `--color-concrete`, `--color-route`, `--color-signal`, `--color-stop` được đánh giá tương phản trước khi chuyển layout.
- Hệ thống spacing 4/8/12/16/24/32; breakpoint mobile-first; card image aspect ratio ổn định; icon chung một thư viện.
- Áp dụng `prefers-reduced-motion`, `:focus-visible`, form label, thông báo lỗi không chỉ biểu thị màu.
- Không dùng template demo ảnh không rõ license; không tải hai phiên bản Bootstrap hoặc hai bundle Bootstrap JS vào cùng layout.

## Lựa chọn template: trạng thái 4.0

**Tham khảo có điều kiện**: Marketplace theo tinh thần Cental (cấu trúc content), Workspace theo Tabler (component/UX), chung bộ token. Quyết định cuối, pin phiên bản, liệt kê license/transitive assets và screenshot trước/sau thuộc **GĐ4.1**. GĐ4.0 **không copy template**.

## Quy tắc xác thực và bảo mật trong UI

- Role chỉ dùng để *hiển thị*, server `[Authorize]`, ownership/authorization không được nới lỏng.
- POST ký hợp đồng, đơn, thanh toán, đánh giá, kiểm duyệt, hồ sơ phải giữ anti-forgery tương ứng; webhook/token API giữ xác thực riêng.
- Không truyền signed payment bearer trong `href` bên thứ ba, analytics, console hoặc log.
- Hồ sơ CCCD không hiển thị trong component sử dụng chung ở public; ảnh identity được lấy qua endpoint đã bảo vệ.
- Link redirect có `returnUrl` phải là local URL.
