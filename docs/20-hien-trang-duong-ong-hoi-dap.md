# Hiện trạng đường ống hỏi–đáp

> **Tài liệu mô tả cái ĐANG CÓ, không phải cái sẽ có.** Thiết kế đích nằm ở
> [`docs/00`](00-ke-hoach-tong-the.md) §4 và backlog [`docs/09`](09-product-backlog.md).
> Viết riêng file này vì khoảng cách giữa hai thứ đó lớn, và người mở `POST /chat/ask` ra
> thấy nó trả lời trôi chảy rất dễ tưởng hệ thống đã làm được nhiều hơn thực tế.

**Cập nhật:** 02/09/2026, sau khi dựng xong skeleton local ([`docs/19`](19-ke-hoach-skeleton-local.md)).

---

## 1. Đường đi một câu hỏi

```
POST /chat/ask ──▶ chat (.NET) ──▶ retrieval (Python) ──▶ PostgreSQL
                       │
                       └────────▶ generation (Python) ──▶ Ollama (local)
                                                          Bedrock (production, chưa có)
```

Đo trên máy dev, mô hình `qwen2.5:0.5b-instruct` chạy CPU:

| Chặng | Thời gian |
|---|---|
| `chat` → `retrieval` | ~45 ms |
| `chat` → `generation` → mô hình | 13–21 s |

Toàn bộ độ trễ nằm ở bước sinh chữ. Đó là lý do `chat` **không** thử lại lời gọi
`generation` (POST, tốn kém) trong khi **có** thử lại `retrieval` (GET, rẻ).

## 2. Bản đồ file

| Chặng | File | Làm gì |
|---|---|---|
| Nhận câu hỏi | [`Xnk.Chat/Endpoints/ChatEndpoints.cs`](../src/dotnet/Xnk.Chat/Endpoints/ChatEndpoints.cs) | `POST /chat/ask`, điều phối, trả 502 khi service sau hỏng |
| Gọi hai service | [`Xnk.Chat/Clients/DownstreamClients.cs`](../src/dotnet/Xnk.Chat/Clients/DownstreamClients.cs) | `RetrievalClient`, `GenerationClient`, tham số `MaxContextDocuments` |
| Giữ danh tính | [`Xnk.Chat/Http/ForwardAuthorizationHandler.cs`](../src/dotnet/Xnk.Chat/Http/ForwardAuthorizationHandler.cs) | Chuyển tiếp token người dùng xuống cả hai chặng |
| Thử lại có kiểm soát | [`Xnk.Chat/Http/TransientRetryHandler.cs`](../src/dotnet/Xnk.Chat/Http/TransientRetryHandler.cs) | Một lần, chỉ GET, chỉ lỗi tạm thời |
| Lấy căn cứ | [`retrieval/db.py`](../src/python/retrieval/retrieval/db.py) | Câu SQL kèm điều kiện tenant |
| **Nói chuyện với mô hình** | [`generation/llm.py`](../src/python/generation/generation/llm.py) | `SYSTEM_PROMPT`, dựng prompt, gọi `/chat/completions` |
| Endpoint sinh | [`generation/main.py`](../src/python/generation/generation/main.py) | `POST /generation/answer`, readiness kiểm mô hình |
| Cấu hình mô hình | [`generation/config.py`](../src/python/generation/generation/config.py) | `LLM_BASE_URL`, `LLM_MODEL`, timeout |

## 3. ⚠️ Đây chưa phải RAG

Chữ **R** hiện gần như không tồn tại.

`RetrievalClient` gọi `GET /retrieval/documents?page_size=10` — tức là **lấy 10 văn bản đầu
tiên theo thứ tự số hiệu**, hoàn toàn không liên quan tới câu hỏi. Không embedding, không
tìm kiếm ngữ nghĩa, không BM25, không rerank, không lọc hiệu lực.

Và ngữ cảnh đưa cho mô hình chỉ gồm **số hiệu + trích yếu**, vì corpus mới có metadata:
`corpus/raw/` rỗng và 16/17 bản ghi trong registry còn ở trạng thái `de_xuat`
([`corpus/README.md`](../corpus/README.md) §4).

Nói gọn: **đường ống đã thông, nhưng chưa có gì chảy qua nó ngoài tiêu đề văn bản.**

### Hệ quả phải nhớ

- Câu trả lời hiện tại là một mô hình 0,5B đọc mấy dòng trích yếu rồi diễn giải. **Đừng đọc
  nó rồi kết luận gì về chất lượng hệ thống.**
- `SYSTEM_PROMPT` trong `llm.py` **không phải prompt sản phẩm**. Prompt thật thuộc
  [`prompts/`](../prompts/), do `@ai-lead` và chuyên gia XNK sở hữu qua CODEOWNERS.
- Cổng chất lượng `eval/gates.yml` đang `enabled: false` vì chưa có bộ đo. Không có số nào
  đang canh chất lượng câu trả lời.

## 4. Cái còn thiếu, theo backlog

| Thiếu | Thuộc | Quy tắc nghiệp vụ liên quan |
|---|---|---|
| Parser cấu trúc, chunking theo Điều | `E2-05` | — |
| Đồ thị hiệu lực, quan hệ sửa đổi | `E2-02` | BR-09 |
| Embedding + schema `vector` + hybrid search | `E3-04` | — |
| **Lọc hiệu lực — không trả điều khoản hết hiệu lực** | `E3-05` | **BR-02** ⚙️ required |
| Truy vấn theo mốc thời gian | `E3-08` | BR-03 |
| Citations API | `E4-02` | BR-01 |
| Guardrail mã HS | `E4-03` | BR-04 |
| **Từ chối khi không đủ căn cứ** | `E4-05` | **BR-07** |
| Disclaimer cuối câu trả lời | `E4-08` | BR-10 |
| Golden set + bộ đo | `E7-03`, `E7-04` | mọi BR |

Ma trận truy vết đầy đủ: [`docs/16`](16-ma-tran-truy-vet.md). Tính tới nay **1/11** quy tắc
có cài đặt và test — là BR-11 (cách ly tenant), làm sớm vì mọi endpoint đọc dữ liệu đều phải
đi qua nó.

## 5. Muốn đổi gì thì vào đâu

| Muốn | Sửa | Rồi chạy |
|---|---|---|
| Cách ra lệnh cho mô hình | `SYSTEM_PROMPT`, `_dung_prompt()` trong `llm.py` | `docker compose --profile app up -d --build generation` |
| Đổi mô hình | `LLM_MODEL` trong `.env` | `up -d --build generation ollama-pull` |
| Trỏ sang endpoint khác (LM Studio, vLLM, một máy khác) | `LLM_BASE_URL` trong `.env` | `up -d generation` |
| Số văn bản đưa vào prompt | `MaxContextDocuments` (mặc định 10) | `up -d --build chat` |
| Nhiệt độ, tham số sinh | `OpenAiCompatibleClient.tra_loi()` | `up -d --build generation` |

Thêm nhà cung cấp mô hình mới thì cài đặt `LlmClient` — Protocol đã tách sẵn trong
`llm.py`, nên `main.py` không phải sửa. Lý do chọn giao thức tương thích OpenAI thay vì SDK
riêng: [ADR-015](adr/015-giao-thuc-tuong-thich-openai.md).

## 6. Thử ngay

```bash
TOKEN=$(curl -s localhost:8080/identity-tenant/token -H 'Content-Type: application/json' -d '{"email":"an.nguyen@noibo.vn","password":"matkhau-local-2026"}' | jq -r .access_token)
```

```bash
curl -s localhost:8080/chat/ask -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d '{"question":"Van ban nao quy dinh thu tuc hai quan?"}' | jq
```

Phản hồi luôn kèm `citations` — **kể cả khi rỗng**. Một câu trả lời không có căn cứ nào mà
trông giống hệt câu có căn cứ là đúng thứ hệ thống này tồn tại để tránh
([`docs/00`](00-ke-hoach-tong-the.md) §10.3).
