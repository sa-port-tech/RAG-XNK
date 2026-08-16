# System prompt

> CODEOWNERS: `@sa-port-tech/xnk-expert-team` + `@sa-port-tech/ai-lead`

Prompt nằm trong git là quyết định có chủ đích
([`docs/00`](../docs/00-ke-hoach-tong-the.md) §8.1): **thay đổi system prompt là thay
đổi hành vi hệ thống**, nên phải qua PR và phải chạy regression eval, giống hệt thay
đổi mã.

Mọi PR chạm thư mục này kích hoạt
[`eval-regression`](../.github/workflows/eval-regression.yml) và phải qua các ngưỡng
trong [`eval/gates.yml`](../eval/gates.yml).

## Một điều cần nhớ khi sửa prompt

Năm guardrail của [`docs/16`](../docs/16-ma-tran-truy-vet.md) §4 **không được chỉ tồn
tại trong prompt**. Guardrail nằm trong system prompt là guardrail model có thể bỏ qua
khi gặp câu hỏi lạ. Mỗi guardrail phải có một lớp kiểm tra bằng mã ở tầng
post-processing; prompt chỉ là lớp đầu tiên.
