# ADR-011 — API định nghĩa code-first, tài liệu OpenAPI sinh từ mã

**Trạng thái:** chấp nhận · 2026-08-16

## Bối cảnh

Bảy service gọi nhau qua REST/JSON trên ECS Service Connect
([`docs/00`](../00-ke-hoach-tong-the.md) §4.3), và frontend Blazor gọi vào
`chat-orchestrator`. Cần chốt hợp đồng API được định nghĩa ở đâu.

Với 3 sprint và một đội kiêm nhiệm, chi phí nghi thức là một yếu tố thật, không phải cái
cớ.

## Quyết định

**Code-first.** Controller và endpoint là nguồn định nghĩa; tài liệu OpenAPI sinh ra từ
mã: `Microsoft.AspNetCore.OpenApi` cho .NET (`MapOpenApi`), FastAPI sinh sẵn cho Python.

Không thêm Swashbuckle. Bộ sinh có sẵn trong ASP.NET Core 9 đủ để xuất tài liệu; thêm một
thư viện phải theo kịp từng bản .NET chỉ để có một giao diện web là chi phí không tương
xứng ở giai đoạn này.

Kiểu trả về phải tường minh (ví dụ `HealthStatus` thay vì `dict`), nếu không tài liệu sinh
ra sẽ mô tả một hợp đồng rỗng.

## Hệ quả

- Hai đội không làm song song trên một API chưa viết được. Chấp nhận: prototype có một đội.
- **Thay đổi phá vỡ tương thích không hiện ra trong diff.** Đây là cái giá thật của lựa
  chọn này và cần nói thẳng.

  Biện pháp giảm nhẹ, chưa làm, cần làm khi có service thứ hai gọi service thứ nhất: xuất
  `openapi.json` trong CI, so với bản đã commit, lệch thì cảnh báo. Rẻ, và biến một thay
  đổi vô hình thành một dòng trong diff.
- Client gọi chéo service phải viết tay. Chấp nhận khi số lời gọi chéo còn đếm được trên
  đầu ngón tay; xem lại khi không còn như vậy.

## Phương án đã cân nhắc và loại bỏ

**Contract-first bằng file OpenAPI trong repo, sinh client cho cả hai ngôn ngữ.** Đúng hơn
về mặt kỹ thuật: hợp đồng có trước, thay đổi phải qua PR và CODEOWNERS thấy, hai đội làm
song song được. Loại bỏ vì thêm một bước sinh mã vào build và một bộ công cụ phải bảo trì,
trong khi lợi ích lớn nhất của nó — làm song song — chưa dùng tới ở prototype.

Đây là quyết định **có thể đảo ngược**: khi đội lớn hơn hoặc khi hợp đồng bắt đầu lệch,
chuyển sang contract-first là việc làm được, và ADR này nên được thay thế chứ không sửa.

**DTO dùng chung trong `Xnk.Shared`.** Đơn giản nhất cho phía .NET, nhưng xoá ranh giới
giữa các service — đổi một DTO là buộc mọi service phải deploy lại — và không ràng buộc gì
được phía Python.

## Cách thi hành — và chỗ hiện chưa thi hành được

Review 03/09/2026 nêu đúng: *"API code-first với không một contract test, không client sinh
ra, không consumer. Quyết định là thật; việc thi hành thì mới là nguyện vọng."*

**Đang thi hành được:**

| Điều | Ai kiểm |
|---|---|
| Tài liệu OpenAPI sinh từ mã, không viết tay | `AddOpenApi()` trong `Program.cs` của mỗi service .NET; FastAPI sinh sẵn cho ba service Python |
| Tài liệu nằm dưới tiền tố của service | Test `Duong_dan_co_tien_to_la_duong_duoc_phuc_vu` và test 404-đường-trần bên Python |
| Tên trường theo hợp đồng, không theo mặc định của serializer | `PhanHoiToken` trong `TokenEndpointTests` khai `JsonPropertyName` và test đọc bằng đúng tên đó |

**Chưa thi hành được, và không giả vờ ngược lại:**

Chưa có contract test giữa `chat` và hai service phía sau. Hôm nay `chat` là consumer duy
nhất, và `ChatApiFactory` giả lập phản hồi bằng chuỗi JSON viết tay — nghĩa là đổi tên một
trường ở `retrieval` sẽ **không** làm bộ test của `chat` đỏ. Nó chỉ đỏ khi hai service gặp
nhau thật, tức là ở `smoke_test.sh` sau khi deploy.

Đóng lỗ này cần một trong hai: sinh client từ tài liệu OpenAPI rồi để `chat` dùng client
đó, hoặc một bộ contract test chạy hai service thật trong CI. Cả hai đều là story riêng —
ghi ra đây để nó không biến mất sau khi ADR được đọc lướt.
