# Mô hình quy trình BPMN

> CODEOWNERS: `@sa-port-tech/xnk-expert-team` + `@sa-port-tech/backend-lead`
> Đặc tả: [`docs/10`](../docs/10-dac-ta-quy-trinh-bpmn.md)

File `.bpmn` nằm trong git là quyết định có chủ đích ([`docs/00`](../docs/00-ke-hoach-tong-the.md) §8.1):
thay đổi quy trình là thay đổi hành vi hệ thống, nên phải qua PR và qua CI, giống hệt
thay đổi mã.

## Luật do `ci-bpmn` thực thi

Mọi file `.bpmn` trong thư mục này bị
[`validate_bpmn.py`](../.github/scripts/validate_bpmn.py) kiểm tra ở mỗi PR:

| Luật | Vì sao |
|---|---|
| Cấm `camunda:class` và `camunda:delegateExpression` | **Java Delegate khoá quy trình vào Camunda 7 và JVM**, loại service .NET/Python khỏi cuộc chơi, và đóng đường chuyển sang Camunda 8 |
| Cấm listener gắn class Java | Cùng lý do |
| Cấm `scriptTask` | Logic nằm trong service, không nằm trong mô hình |
| `serviceTask` phải có `camunda:type="external"` và `camunda:topic` | Mẫu external task là cách duy nhất để worker viết bằng ngôn ngữ bất kỳ nhận được việc |
| `process` phải có `id`, `name`, `isExecutable="true"` | Thiếu `isExecutable` thì Camunda deploy được nhưng không khởi tạo instance |
| Mọi `sequenceFlow` phải trỏ tới node có thật, không có node mồ côi | Nhánh chết trong quy trình là lỗi im lặng |

Quy ước đặt tên: `P<số>-<mo-ta-co-gach-noi>.bpmn`, id tiến trình trùng tên file.

Chạy trước khi mở PR:

```bash
python .github/scripts/validate_bpmn.py bpmn/
```
