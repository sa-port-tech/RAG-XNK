# ADR-003 — Bảy microservice, và tiêu chí để tách thêm

**Trạng thái:** chấp nhận · quyết định gốc trong [`docs/00`](../00-ke-hoach-tong-the.md) §4.2
· ghi thành ADR 2026-09-09

> Bảy service này là danh sách trong `.github/services.json` — nguồn sự thật duy nhất cho cấu
> trúc monorepo theo [ADR-009](009-services-json-nguon-su-that.md). ADR viết muộn: xem ghi chú
> ở ADR-001.

## Bối cảnh

Chia quá nhỏ ngay từ đầu là nguyên nhân thất bại phổ biến hơn chia quá to (`docs/00` §16). Chia
quá to thì phải làm cuộc tách monolith sau này — việc đắt và rủi ro. Cần một con số và một tiêu
chí, không phải một cảm giác.

## Quyết định

**Bảy service**, tách theo **ranh giới nghiệp vụ và nhịp thay đổi**, không theo tầng kỹ thuật:

| Service | Ngôn ngữ | Trách nhiệm | Sở hữu dữ liệu |
|---|---|---|---|
| `identity-tenant` | .NET | Xác thực, tổ chức, người dùng, RBAC, quota | `identity` |
| `chat` | .NET | Vòng đời hội thoại, điều phối `retrieval` → `generation` | `conversation` |
| `corpus` | .NET | CRUD văn bản, hàng đợi review, API tra cứu có cấu trúc | `corpus`, `lookup` |
| `workflow-worker` | .NET | External Task Worker cho các bước .NET trong BPMN | không sở hữu |
| `ingestion` | Python | Crawler, parser, OCR, phân rã Điều/Khoản | ghi qua API `corpus` |
| `retrieval` | Python | Embedding, hybrid search, lọc hiệu lực, rerank | đọc `corpus`, `vector` |
| `generation` | Python | Gọi mô hình ngôn ngữ, guardrail XNK, chạy eval | không sở hữu |

`Xnk.Web` (giao diện) và `nginx` không nằm trong danh sách này: một cái là bộ tĩnh, một cái là
cổng vào — cả hai không sở hữu dữ liệu và không deploy theo nhịp của service.

**Tiêu chí tách thêm — chỉ tách khi có ít nhất một lý do ĐO ĐƯỢC:**

1. **Đội ngũ sở hữu khác nhau** — hai nhóm người phải chờ nhau để merge.
2. **Nhịp deploy khác nhau** — một phần đổi hàng ngày, phần kia hàng quý.
3. **Đặc tính scale khác nhau** — một phần cần CPU/GPU theo đợt, phần kia cần bộ nhớ thường trực.
4. **Yêu cầu tuân thủ khác nhau** — một phần chạm dữ liệu bị ràng buộc pháp lý, phần kia không.

Không có lý do nào trong bốn cái trên thì **không tách**, kể cả khi thư mục đang lớn.

## Hệ quả

- Mỗi service có **DB user riêng**, chỉ được cấp quyền trên schema của mình (`db/roles.sql`).
  Service khác muốn đọc thì gọi API, không JOIN chéo schema. Đây là thứ biến ranh giới trên
  giấy thành ranh giới PostgreSQL từ chối vượt qua.
- **Một ngoại lệ có chủ đích:** `retrieval` được cấp quyền **đọc** `corpus`, vì lọc hiệu lực
  phải nằm trong cùng một câu SQL với vector search — chốt ở [ADR-012](012-cach-ly-tenant-va-so-huu-vector.md).
- Bảy service nghĩa là bảy Dockerfile, bảy mục trong compose, bảy job CI. Chi phí lặp này là
  có thật và được trả bằng `services.json` làm nguồn sự thật cho ma trận build.
- Camunda dùng **cụm RDS riêng hoàn toàn** — engine DB có mô hình khoá và nhịp ghi rất khác.

## Phương án đã cân nhắc và loại bỏ

**Monolith trước, tách sau.** Nhanh nhất trong ba tháng đầu. Loại bỏ vì ranh giới nghiệp vụ ở
đây đã rõ từ trước (`docs/00` §4.2) và vì cuộc tách sau này phải trả bằng tiền, trong khi làm
đúng từ đầu chỉ trả bằng ~1–2 tuần tốc độ phát triển.

**Tách 15–20 service ngay từ đầu.** Loại bỏ vì chia quá nhỏ giai đoạn đầu là nguyên nhân thất
bại phổ biến hơn chia quá to (`docs/00` §16) — mỗi ranh giới sai là một cuộc gộp lại về sau.

**Tách theo tầng kỹ thuật (api / business / data).** Loại bỏ vì mọi thay đổi nghiệp vụ khi ấy
chạm cả ba service cùng lúc: ba PR, ba lần deploy, một tính năng.
