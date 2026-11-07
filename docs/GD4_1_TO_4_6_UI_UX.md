# Motorbike Rental — GĐ4.1 → GĐ4.6: Thiết kế & nghiệm thu UI/UX

**Baseline:** `develop` @ `22b7263320de2ec2e56228146042eaec01fffc86`.
**Không sửa:** controller, service, EF migrations, SQL, dữ liệu; giữ tương thích GD0–GD3.
**Điều kiện:** Áp dụng GĐ4.0 trước. Kit có bước xử lý đúng lỗi trailing whitespace đang chặn GD4.0.

## Triển khai theo commit

| Commit dự kiến | Nội dung | Những file chính |
|---|---|---|
| GĐ4.0 | Khóa audit baseline | docs, audit, scripts/ci, workflow |
| GĐ4.1 | Cấu trúc layout/design tokens | `_Gd4MarketplaceLayout`, `_Gd4WorkspaceLayout`, nav, CSS, JS |
| GĐ4.2 | Marketplace | Trang chủ, danh sách xe, trang chi tiết |
| GĐ4.3 | Customer journey | Giỏ, checkout, chi tiết đơn, thanh toán, Review, hồ sơ |
| GĐ4.4 | Owner workspace | Dashboard, lịch, đơn và đội xe, Review |
| GĐ4.5 | Admin workspace | Dashboard, xe, người dùng, thanh toán, vi phạm, chính sách, Reviews |
| GĐ4.6 | Quality & live browser acceptance | Python static, Playwright, GitHub Actions, report |

## Quyết định thiết kế

- **Marketplace**: markup/CSS nguyên bản lấy cảm hứng từ pattern đặt xe; không sao chép HTML/ảnh Cental. Bootstrap 5.3.3 CDN (đã có trước GD4), chỉ một lần.
- **Owner/Admin**: Tabler Core **1.7.3** qua CDN cố định; Tabler tích hợp Bootstrap nên **không** được nạp `bootstrap.bundle.min.js`/`bootstrap.min.css` riêng trong workspace.
- **Màu sắc**: emerald `#126755`, chữ xanh đen `#132a2b`, nền sáng `#f8faf9`, vàng nhấn `#f3b64b`; chữ Be Vietnam Pro.
- **Razor**: giữ server rendering và các mẫu form hiện có; JS chỉ progressive enhancement.
- **Login**: Không đổi server redirect (hiện login mặc định về `/`) để giữ hành vi đã được GD0–GD3 kiểm thử. Thay vào đó, navbar/trang chủ hiển thị lối vào khu Owner/Admin rõ ràng; thay đổi redirect là phạm vi nghiệp vụ khác cần testcase riêng.
- **Hai hệ thống**: `Booking` cũ vẫn được render trong bố cục mới; `RentalOrder` là hành trình chính. Không gộp hoặc sửa bảng.
- **Dữ liệu**: GD3 cần bảng `RentalOrderItemReviews` đã chạy từ SQL riêng trên DB môi trường ứng dụng. Kit GD4 tuyệt đối không chạy SQL thật.

## Customer journey

1. Trang chủ: khu vực + ngày nhận/trả → `GET /Vehicle/Index` (giữ `KhuVuc`, `NgayNhanXe`, `NgayTraXe`).
2. Bộ lọc desktop thành cột trái; mobile là disclosure (có thể mở bằng bàn phím).
3. Trang xe giữ gallery, rating, thông số, thông tin chủ xe, nút đặt xe.
4. Giỏ giải thích **mỗi đơn chỉ chứa xe của một chủ** trước khi tiếp tục.
5. Checkout hiển thị từng xe, ngày thuê, ước tính tổng tiền và lỗi validation.
6. Chi tiết đơn có chỉ báo tiến trình, giải thích bước tiếp theo, giữ tất cả hành động gốc và quyền.
7. QR DEMO vẫn phân biệt giao dịch giả lập và VietQR ngân hàng. Mobile checkout DEMO **không sửa** vì có các ràng buộc token/CSP và smoke GD2.
8. GĐ3 Review giữ workflow chống đánh giá trùng, role check và Admin moderation.

## Owner / Admin UX

- Owner: sidebar chức năng, lối tắt đến đơn chờ xử lý, quản lý đội xe, lịch, đánh giá và hồ sơ.
- Admin: nhóm Tổng quan, Vận hành, Tài chính & Kiểm soát, Hệ thống; lối tắt xử lý xe chờ duyệt, người dùng, thanh toán và Review.
- Trên cả hai, bảng dài có cuộn trong vùng bảng; thanh điều hướng mobile thu gọn.
- Không cho phép UI vượt server authorization: backend vẫn là nguồn quyết định quyền.

## GĐ4.6 — Hai vòng nghiệm thu

**Vòng A — CI** chạy trên **MySQL tạm**: GD4.0 static → GD4 UI static → build Razor .NET 10 → bootstrap GD2 schema → áp dụng GD3 SQL trên DB test → seed Customer/Owner/Admin → Chromium Playwright tại 375/390/768/1440px, tổng hợp report/screenshot; workflow `GD4 UI UX browser acceptance`. Đồng thời GĐ0–GĐ3 phải PASS cùng commit. Không dùng dữ liệu thực.

**Vòng B — Trực tiếp khi chạy dự án của bạn**:

1. **Sao lưu database thực** và xác minh bảng GD3 tồn tại trước khi dùng trang đánh giá. Không chạy migration để phục vụ UI.
2. Chạy ASP.NET Core ứng dụng tại localhost (thay URL nếu cổng khác).
3. Dựa trên `scripts/qa/gd4-fixture.example.json`, tạo JSON fixture **ở ngoài repository** chứa email/password của 3 tài khoản thử nghiệm do bạn kiểm soát: `email`, `ownerEmail`, `adminEmail`, `password`, `orderId`, `vehicleId`. Nên dùng DB test clone, không dùng tài khoản hay ảnh CCCD thật.
4. PowerShell:
   ```powershell
   & 'E:\Projects\motorbike-rental\scripts\qa\run-gd4-live.ps1' `
       -RepositoryPath 'E:\Projects\motorbike-rental' `
       -BaseUrl 'http://127.0.0.1:5119' `
       -FixturePath 'E:\Projects\_setup\gd4-qa-fixture.json' `
       -InstallDependencies
   ```
5. Xem `artifacts/gd4-live/report.json` và ảnh ở 375/390/768/1440px, ưu tiên xem lại checkout, thanh toán, màn Owner/Admin.
6. Thực hiện thủ công **một giao dịch thuê hoàn chỉnh trên DB thử nghiệm**: chọn xe → giỏ → checkout → chủ xác nhận → ký → thanh toán DEMO → bàn giao → nhận lại → Review, cùng một vòng Admin duyệt và Owner xử lý. Playwright audit trên không tự tạo giao dịch, do đó bước thực tế này là tiêu chí bắt buộc độc lập.

**Không nghiệm thu B** nếu chưa chạy ứng dụng thật, chưa áp dụng bảng GD3 hoặc thiếu bằng chứng ảnh/report. Chưa có quyền truy cập máy của bạn nên bộ cài không thể tự khẳng định vòng B PASS.

### Tiêu chí chất lượng

- Mọi route cơ bản HTTP 2xx/3xx đúng quyền; không có JS exception.
- Không cuộn ngang toàn trang ở 375, 390, 768, 1440px (sai số tối đa 3px); cuộn riêng bảng dài được phép.
- Có `lang=vi`, skip link, main landmark, focus-visible, reduced-motion; form có server validation.
- Không trùng Bootstrap/Tabler bundle; việc mất kết nối CDN phải được kiểm tra khi chạy thực tế.
- Role isolation giữ ở server, public marketplace hiển thị bình thường với tài khoản Owner/Admin.
- Chức năng QR DEMO có nhãn rõ ràng, không giả nhận tiền thật.
- GĐ0, GĐ1, GĐ2, GĐ3 và GĐ4 CI cùng PASS trên commit mới.

## Rollback

Mỗi giai đoạn có commit riêng. Không `git reset --hard` khi chưa kiểm tra thay đổi cục bộ; dùng `git revert` theo commit cụ thể nếu cần khôi phục. Main không bị tác động.
