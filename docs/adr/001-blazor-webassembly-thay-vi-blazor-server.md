# ADR-001 — Blazor WebAssembly thay vì Blazor Server

**Trạng thái:** chấp nhận · quyết định gốc trong [`docs/00`](../00-ke-hoach-tong-the.md) §5.1
· ghi thành ADR 2026-09-09

> Quyết định này có từ khi lập kế hoạch tổng thể và **đã được thi hành trong mã**
> (`src/dotnet/Xnk.Web/Xnk.Web.csproj` dùng `Microsoft.NET.Sdk.BlazorWebAssembly`). ADR viết
> muộn vì slot 001 được đặt trước ở `docs/01` §8.1 mà chưa ai điền — đó chính là thứ khiến
> mười một trích dẫn trong mã trỏ vào những số hiệu không tồn tại cho tới 09/09/2026.

## Bối cảnh

Hệ thống hướng tới **10.000 tài khoản**. Hai mô hình Blazor khác nhau ở chỗ đặt vòng lặp
render:

- **Blazor Server** giữ một kết nối SignalR thường trực cho **mỗi tab đang mở**, cộng state
  của tab đó trong bộ nhớ server.
- **Blazor WebAssembly** tải bộ nhị phân .NET về trình duyệt và chạy hoàn toàn ở đó, gọi
  REST như một SPA thông thường.

Prototype chạy trên AWS Free Tier với ngân sách kiểm soát chặt (`docs/00` §3.5).

## Quyết định

**Dùng Blazor WebAssembly.** `Xnk.Web` là `Microsoft.NET.Sdk.BlazorWebAssembly`, publish ra
bộ tĩnh và được nginx phục vụ ở local, S3 + CloudFront trên môi trường thật (`docs/00` §4.1).

## Hệ quả

- Backend scale theo số **request**, không theo số **tab đang mở**. Một người mở năm tab
  không tốn thêm gì của server.
- Gián đoạn mạng không làm mất phiên làm việc: không có circuit nào để đứt.
- ALB **không** cần sticky session cho tầng giao diện.
- **Cái giá: mọi thứ trong bộ nhị phân đều công khai.** Không đặt bí mật, không đặt logic
  phân quyền quyết định trong `Xnk.Web`. Danh tính hiển thị trên `MainLayout` giải mã từ
  payload JWT **chưa xác minh** — dùng để hiển thị, và mọi quyết định phân quyền thật nằm ở
  phía service, nơi chữ ký được kiểm.
- Bộ nhị phân phải tải về, nên **kích thước là một ràng buộc vận hành**, không phải chuyện
  thẩm mỹ: `ci-blazor` đo bundle Brotli và so với ngưỡng trong `.github/quality-gates.yml`.
- Lần tải đầu chậm hơn Blazor Server. Chấp nhận vì người dùng nội bộ mở ứng dụng cả ngày.

## Phương án đã cân nhắc và loại bỏ

**Blazor Server.** Nhanh khi khởi động, không lộ mã, gọi thẳng service phía sau. Loại bỏ vì
chi phí và độ phức tạp tăng theo **số phiên đồng thời** chứ không theo lượng việc thật, và vì
mọi gián đoạn mạng làm mất phiên làm việc của người đang nhập dở một tờ khai.

**SPA JavaScript (React/Vue) + API .NET.** Hệ sinh thái lớn hơn, bundle nhỏ hơn. Loại bỏ vì
đội chỉ có người .NET, và vì Telerik UI for Blazor (`docs/00` §5) là bộ điều khiển đã chọn
cho bảng biểu nghiệp vụ — đổi sang JS là đổi luôn cả bộ đó.

**Blazor Server cho trang quản trị, WASM cho trang người dùng.** Loại bỏ vì hai mô hình render
trong một sản phẩm nghĩa là hai cách quản lý state, hai cách xác thực, và hai lớp lỗi mà người
trực đêm phải phân biệt được lúc 2 giờ sáng.
