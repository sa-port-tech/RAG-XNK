# ADR-013 — ALB định tuyến theo tiền tố, ứng dụng tự phục vụ tiền tố

**Trạng thái:** chấp nhận · 2026-08-16

## Bối cảnh

`.github/scripts/smoke_test.sh` gọi healthcheck sau mỗi lần deploy theo công thức:

```
${BASE_URL}/${service_name}${health_path}
```

Tức là `https://.../corpus/health/ready`. Một ALB duy nhất đứng trước bảy service và định
tuyến theo tiền tố đường dẫn.

Điểm mấu chốt dễ bị bỏ qua: **AWS ALB không cắt tiền tố đường dẫn** trước khi chuyển tiếp.
Ứng dụng nhận nguyên `/corpus/health/ready`, không phải `/health/ready`. ALB cũng không có
chức năng rewrite đường dẫn.

## Quyết định

Mỗi service **tự phục vụ đường dẫn đầy đủ có tiền tố**, và tiền tố bằng đúng trường `name`
của service trong `.github/services.json`.

**.NET** — `app.UsePathBase("/corpus")` đặt trước mọi middleware định tuyến. `UsePathBase`
cắt tiền tố ra và đặt vào `PathBase`, nên route khai báo là `/health/ready` vẫn khớp.

**Python/FastAPI** — mọi route nằm trên `APIRouter(prefix="/retrieval")`, và
`openapi_url`/`docs_url` cũng đặt dưới tiền tố.

## Hệ quả

- Ứng dụng chạy giống nhau ở cả ba nơi: chạy trực tiếp lúc dev, trong container, và sau
  ALB. Không có môi trường nào cần một lớp proxy đặc biệt để hoạt động đúng.
- Đổi `name` trong `services.json` mà quên đổi tiền tố trong mã sẽ làm smoke test đỏ sau
  deploy. Để bắt sớm hơn, mỗi service Python có một test khẳng định đường dẫn **không**
  tiền tố trả về 404 — nếu ai đó thêm route trần, test đỏ tại chỗ thay vì đỏ trên dev.
- Tiền tố xuất hiện ở hai nơi (mã và `services.json`). Chấp nhận sự lặp này vì phương án
  thay thế — đọc `services.json` lúc chạy — buộc mọi service phải mang theo một file cấu
  hình của CI vào image.

## Phương án đã cân nhắc và loại bỏ

**Dùng `root_path` của FastAPI.** Đúng khi proxy có cắt tiền tố. ALB thì không cắt, nên
hành vi phụ thuộc vào chi tiết của từng phiên bản Starlette về việc có tự cắt hay không —
một chỗ dựa quá mỏng cho thứ mà smoke test sau mỗi lần deploy dựa vào.

**Một ALB (hoặc một listener) cho mỗi service.** Sạch nhất về mặt đường dẫn: mỗi service
phục vụ `/health/ready` trần. Loại bỏ vì chi phí — prototype chạy trên Free Tier với ngân
sách kiểm soát chặt ([`docs/00`](../00-ke-hoach-tong-the.md) §3.5), và bảy ALB là bảy hoá
đơn.

**Định tuyến theo subdomain thay vì tiền tố.** Mỗi service một hostname, đường dẫn sạch.
Loại bỏ vì cần chứng chỉ wildcard và bản ghi DNS cho từng service, trong khi
`smoke_test.sh` đã được viết theo mô hình tiền tố — đổi sang subdomain là sửa cả script
lẫn hạ tầng để đổi lấy một lợi ích thẩm mỹ.

## Bốn chỗ cài đặt cùng một quyết định

Review 03/09/2026 nêu: *"Định tuyến theo tiền tố quyết định ở đây; bốn service cài đặt nó
theo bốn cách khác nhau."* Đúng, và đó là **hệ quả bắt buộc** của việc dùng hai stack — chứ
không phải bốn ý tưởng khác nhau. Liệt kê ra để ai đọc một chỗ biết ba chỗ kia tồn tại:

| Chỗ | Cơ chế | Vì sao khác nhau |
|---|---|---|
| Bốn service .NET | `app.UsePathBase("/<tên>")` | ASP.NET Core cắt tiền tố vào `PathBase`, route vẫn khai `/health/ready` |
| Ba service Python | `APIRouter(prefix="/<tên>")` | FastAPI không có khái niệm `PathBase`; gắn thẳng vào router là cách duy nhất đúng ở cả ba môi trường |
| nginx ở local | `tools/local/render_nginx.py` sinh `location /<tên>/` | Tái hiện ALB, và **không** cắt tiền tố — đúng hành vi thật |
| ALB trên AWS | rule theo path pattern | Không cắt tiền tố, và không có chức năng rewrite |

**Điều giữ bốn chỗ này không lệch nhau:** tiền tố ở cả bốn đều bằng đúng trường `name`
trong [`.github/services.json`](../../.github/services.json) (ADR-009), và
`render_nginx.py` **đọc chính file đó** thay vì chép lại danh sách.

**Điều bắt lệch khi nó xảy ra:** mỗi service Python có một test khẳng định đường dẫn
**không** tiền tố trả 404, và `smoke_test.sh` lặp qua toàn bộ `services.json` sau mỗi lần
deploy. Đổi `name` mà quên đổi tiền tố trong mã thì test đỏ tại chỗ, không phải đỏ trên dev.

Cái chưa có: không gì bắt được việc một service .NET quên gọi `UsePathBase`. Bốn service
đó dựa vào test tích hợp của riêng chúng, và ba trong bốn có test ấy.
