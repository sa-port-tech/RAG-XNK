"""Service generation — sinh câu trả lời qua endpoint tương thích OpenAI.

⚠️ Docstring này từng ghi "Claude qua Bedrock, guardrail nghiệp vụ XNK". Cả hai vế đều
chưa đúng, và cái sai đó mô tả **kiến trúc đích** như thể nó đã có:

* Mô hình đứng sau ``LLM_BASE_URL`` hiện là Ollama chạy trong docker-compose. Bedrock là
  một lần đổi cấu hình, không phải một nhánh mã — chính vì thế mà ADR-015 chọn giao thức
  tương thích OpenAI. Nhưng "đổi được" khác "đã đổi".
* **Chưa có guardrail nào.** ``SYSTEM_PROMPT`` trong ``llm.py`` tự khai nó không phải
  prompt sản phẩm; bộ guardrail nghiệp vụ XNK thuộc epic E4.

Một docstring mô tả đích thay vì hiện trạng là chỗ rẻ nhất để một người đọc mã kết luận
sai về việc hệ thống đã kiểm soát được cái gì.
"""
