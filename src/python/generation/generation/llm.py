"""Nói chuyện với mô hình ngôn ngữ qua giao thức tương thích OpenAI.

Vì sao chọn giao thức này thay vì SDK của một nhà cung cấp
----------------------------------------------------------
Prototype chạy **hoàn toàn ở local, không cần internet**: mô hình đứng sau `LLM_BASE_URL`
là Ollama chạy trong docker-compose. Production dùng Bedrock (docs/00 §4.3).

Nếu viết thẳng theo SDK của Bedrock thì ở local phải có một nhánh giả — đúng thứ mà yêu cầu
"không mock" cấm. Chọn một giao thức mà **cả hai phía đều nói được** thì local và cloud khác
nhau ở **cấu hình**, không khác nhau ở **nhánh mã**. Xem ADR-015.

Endpoint được dùng là ``POST {base}/chat/completions`` — phần giao thức mà Ollama, vLLM,
llama.cpp, LM Studio và OpenAI đều cài đặt giống nhau. Không dùng tính năng riêng của nhà
nào; ngày đổi sang Bedrock chỉ là viết một lớp cài đặt khác cho cùng interface này.
"""

from __future__ import annotations

from typing import Any, Final, Protocol

import httpx2

# System prompt tối thiểu cho lát cắt dọc.
#
# ⚠️ Đây KHÔNG phải prompt sản phẩm. Prompt thật thuộc `prompts/`, do @ai-lead và chuyên gia
# XNK sở hữu qua CODEOWNERS, và đi kèm bộ guardrail của epic E4 (từ chối khi không đủ căn
# cứ, trích dẫn bắt buộc, chặn mã HS bịa). Chuỗi dưới đây chỉ đủ để chứng minh đường ống
# chạy được — thay nó bằng một prompt nghiêm túc là việc của E4, không phải của skeleton.
SYSTEM_PROMPT: Final = (
    "Bạn là trợ lý nghiệp vụ xuất nhập khẩu. Chỉ trả lời dựa trên danh sách văn bản được "
    "cung cấp. Nếu chúng không đủ căn cứ, hãy nói rõ là chưa đủ căn cứ thay vì suy đoán."
)


class LlmClient(Protocol):
    """Hợp đồng mà mọi cài đặt LLM phải theo.

    Có Protocol ngay từ đầu, khi mới đúng một cài đặt, là có chủ đích: nó ghi ra ranh giới
    để lớp Bedrock sau này không phải sửa `main.py`, và để test thay được cài đặt mà không
    cần một cờ ``if is_test`` nào trong mã sản phẩm.
    """

    async def tra_loi(self, cau_hoi: str, ngu_canh: list[str]) -> str:
        """Sinh câu trả lời từ câu hỏi và các trích đoạn văn bản."""
        ...

    async def san_sang(self) -> None:
        """Ném ngoại lệ nếu mô hình chưa gọi được."""
        ...


class OpenAiCompatibleClient:
    """Cài đặt cho mọi endpoint nói giao thức OpenAI — local là Ollama."""

    def __init__(self, client: httpx2.AsyncClient, model: str) -> None:
        self._client = client
        self._model = model

    async def tra_loi(self, cau_hoi: str, ngu_canh: list[str]) -> str:
        noi_dung = _dung_prompt(cau_hoi, ngu_canh)

        phan_hoi = await self._client.post(
            "/chat/completions",
            json={
                "model": self._model,
                "messages": [
                    {"role": "system", "content": SYSTEM_PROMPT},
                    {"role": "user", "content": noi_dung},
                ],
                # Nhiệt độ thấp cho nghiệp vụ pháp lý: câu trả lời cần lặp lại được, không
                # cần sáng tạo. Đây là lựa chọn nghiệp vụ, không phải tinh chỉnh kỹ thuật.
                "temperature": 0.1,
                "stream": False,
            },
        )
        phan_hoi.raise_for_status()

        du_lieu: dict[str, Any] = phan_hoi.json()
        lua_chon = du_lieu.get("choices") or []
        if not lua_chon:
            raise ValueError(f"Phản hồi LLM không có 'choices': {str(du_lieu)[:200]}")

        return str(lua_chon[0]["message"]["content"])

    async def san_sang(self) -> None:
        """Hỏi danh sách mô hình — phần nhẹ nhất của giao thức mà vẫn chứng minh được kết nối.

        Không gọi thử ``/chat/completions``: một lần sinh chữ trên CPU mất hàng chục giây,
        và readiness bị gọi liên tục bởi ALB.
        """
        phan_hoi = await self._client.get("/models")
        phan_hoi.raise_for_status()


def _dung_prompt(cau_hoi: str, ngu_canh: list[str]) -> str:
    """Ghép câu hỏi với các trích đoạn thành một tin nhắn."""
    if not ngu_canh:
        return f"Không có văn bản nào được cung cấp.\n\nCâu hỏi: {cau_hoi}"

    danh_sach = "\n".join(f"[{i}] {v}" for i, v in enumerate(ngu_canh, start=1))
    return f"Các văn bản liên quan:\n{danh_sach}\n\nCâu hỏi: {cau_hoi}"
