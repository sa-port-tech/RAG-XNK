# ADR-002 — Camunda 7 chỉ dùng External Task pattern

**Trạng thái:** chấp nhận · quyết định gốc trong [`docs/00`](../00-ke-hoach-tong-the.md) §4.3
và §6 · ghi thành ADR 2026-09-09

> Quyết định này **đã được thi hành và đã có cổng chặn**: `.github/scripts/validate_bpmn.py`
> từ chối mọi `camunda:class`, `camunda:delegateExpression` và listener gắn class, và
> `ci-bpmn` chạy nó trên mọi PR chạm `bpmn/`. ADR viết muộn — xem ghi chú ở ADR-001.

## Bối cảnh

Camunda 7 cho phép Service Task gọi mã theo hai đường:

1. **Java Delegate** — engine gọi thẳng một class Java trong cùng JVM
   (`camunda:class`, `camunda:delegateExpression`, `camunda:expression`).
2. **External Task** — engine đẩy việc vào một topic; worker ở tiến trình bất kỳ, ngôn ngữ
   bất kỳ, chủ động lấy về (fetch-and-lock), làm xong thì báo hoàn thành.

Đội này viết .NET và Python. Không có service nào chạy trên JVM. Camunda 7 cũng đã hết vòng
đời hỗ trợ cộng đồng, nên khả năng thay thế engine là một yêu cầu chứ không phải mong muốn.

## Quyết định

**Mọi Service Task là External Task.** Java Delegate bị cấm tuyệt đối, và lệnh cấm được thi
hành bằng máy chứ không bằng lời dặn:

- `validate_bpmn.py` chặn `camunda:class`, `camunda:delegateExpression`, `camunda:expression`
  gọi bean, và `executionListener`/`taskListener` có gắn class.
- Topic đặt theo quy ước `<service>.<hành động>`.
- `Xnk.WorkflowWorker` là External Task Worker viết bằng .NET; `ingestion` cũng là worker.

## Hệ quả

- Quy trình nghiệp vụ **không khoá vào JVM**. Đổi engine là đổi lớp fetch-and-lock, không
  phải viết lại logic nghiệp vụ.
- Worker deploy độc lập với engine. Một worker chết không kéo theo engine, và ngược lại.
- **Cái giá: một vòng lặp polling và một cửa sổ lock.** Worker phải hoàn thành công việc
  trong `lockDuration`, nếu không Camunda phát lại việc đó cho worker khác — hai worker cùng
  làm một task là hỏng ngầm, không phải lỗi thấy ngay. `CamundaClient` chịu trách nhiệm giữ
  quan hệ giữa `MaxTasks`, `LockDurationMs` và thời gian handler chạy.
- Engine không biết worker còn sống hay không. Đây là lý do `workflow-worker` là một project
  `Microsoft.NET.Sdk.Web` dù không phục vụ request nào: nó cần endpoint health để ECS biết
  vòng lặp polling có còn chạy không.

## Phương án đã cân nhắc và loại bỏ

**Java Delegate.** Đơn giản nhất khi đội đã có JVM: gọi hàm là xong, không polling, không
lock. Loại bỏ vì đội không có JVM, và vì nó khoá quy trình vào Camunda 7 — một engine đã hết
vòng đời hỗ trợ.

**Không dùng BPMN engine, tự viết state machine.** Rẻ lúc đầu. Loại bỏ vì các quy trình ở
`docs/10` có bước con người, timer và escalation; tự viết nghĩa là tự viết lại lịch sử thực
thi, cơ chế retry và giao diện theo dõi — thứ mà engine đã có.

**Camunda 8 (Zeebe).** Còn hỗ trợ, kiến trúc job worker tương tự. Loại bỏ cho prototype vì chi
phí vận hành cụm Zeebe vượt ngân sách Free Tier. Vì Service Task đã là external, đây là đường
nâng cấp còn mở chứ không phải cánh cửa đã đóng.
