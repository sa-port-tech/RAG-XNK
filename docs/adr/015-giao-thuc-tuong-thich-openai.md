# ADR-015 — Gọi mô hình ngôn ngữ qua giao thức tương thích OpenAI

**Trạng thái:** chấp nhận · 2026-09-02

## Bối cảnh

[`docs/00`](../00-ke-hoach-tong-the.md) §4.3 chốt production gọi Claude qua **Amazon
Bedrock**. Nhưng đợt dựng skeleton ([`docs/19`](../19-ke-hoach-skeleton-local.md)) có một
ràng buộc cứng: hệ thống phải chạy **trọn vẹn ở máy dev, không cần internet**, và luật
"không mock" cấm mọi nhánh mã kiểu `if (isLocal) return cauTraLoiGia;`.

Hai điều đó ghép lại thành một câu hỏi: `generation` gọi cái gì khi không có Bedrock?

## Quyết định

**Nói chuyện với mô hình qua giao thức `POST {base}/chat/completions` — phần giao thức mà
Ollama, vLLM, llama.cpp, LM Studio và OpenAI đều cài đặt giống nhau.** Địa chỉ đến từ biến
`LLM_BASE_URL`.

Ở local, biến đó trỏ vào container **Ollama** trong `docker-compose.yml`, profile `app`.
Ở cloud, nó sẽ trỏ vào một lớp cài đặt khác của cùng interface `LlmClient`
(`src/python/generation/generation/llm.py`).

Khác nhau giữa local và production nằm ở **cấu hình và lớp cài đặt**, không nằm ở nhánh
điều kiện trong mã nghiệp vụ.

## Hệ quả

- **Không có đường nào giả vờ chạy.** Ollama chưa lên thì `/generation/health/ready` trả
  **503** và `/chat/ask` trả **502** kèm lý do. Đó là hành vi đúng, không phải lỗi cần vá.
- `Protocol` được khai ngay từ khi mới có một cài đặt. Nhờ nó, test thay được lớp LLM mà mã
  sản phẩm không cần một dòng `if is_test` nào — xem `LlmTrongBoNho` trong bộ test.
- Ollama nằm trong profile **`app`**, không phải một profile riêng. Cân nhắc đã ghi ngay
  trong `docker-compose.yml`: để nó ra ngoài thì lệnh khởi động trong README cho ra một hệ
  thống có hai service báo lỗi — và như vậy thì không phải "hệ thống chạy được".
- Mô hình mặc định là **0.5B**. Nó đủ để chứng minh đường ống, và **không** đủ để đánh giá
  chất lượng câu trả lời. Đo chất lượng là việc của golden set ([`docs/14`](../14-phuong-phap-golden-set.md))
  trên mô hình thật — đừng đọc câu trả lời của mô hình 0.5B rồi kết luận gì về hệ thống.
- Chỉ dùng phần giao thức chung. Không dùng tính năng riêng của nhà nào (prompt caching,
  tool calling dạng riêng), vì đó chính là thứ khoá lại lựa chọn.
- **Cái mất:** prompt caching và citations API của Anthropic là ưu thế thật mà giao thức
  chung không có. Khi thêm lớp Bedrock ở epic E4, hai thứ đó nằm trong lớp ấy chứ không
  nằm trong interface — nghĩa là bản local sẽ không có chúng, và số đo giữa hai môi trường
  không so sánh trực tiếp được.

## Phương án đã cân nhắc và loại bỏ

**Viết thẳng theo SDK Bedrock, local dùng một lớp trả lời cứng.** Ngắn nhất. Loại bỏ vì đó
đúng là định nghĩa của mock trong mã sản phẩm: đường chạy ở máy dev khác đường chạy thật,
nên mọi lỗi ở tầng gọi mô hình chỉ lộ ra sau khi deploy.

**LiteLLM hoặc một lớp proxy đa nhà cung cấp.** Giải quyết đúng bài toán này và còn hơn thế.
Loại bỏ ở prototype vì thêm một tiến trình phải vận hành, phải phiên bản hoá, phải gỡ lỗi —
trong khi thứ cần chỉ là một lời gọi HTTP và một interface bốn dòng.

**Chỉ chạy được khi có Bedrock.** Trung thực nhất về mặt "giống production". Loại bỏ vì nó
phá yêu cầu chạy offline của `docs/19`, và biến mọi buổi làm việc của dev thành phụ thuộc
vào credential AWS — thứ mà `E1-02` còn chưa kiểm chứng xong.
