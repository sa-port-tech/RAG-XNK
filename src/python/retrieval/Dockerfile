# retrieval.
#
# ⚠️ Build context là GỐC REPO, không phải thư mục này — cd-deploy.yml dùng
# `context: .` với `file: <service_path>/Dockerfile`. Build tay để kiểm tra:
#
#   docker build -f src/python/retrieval/Dockerfile -t xnk/retrieval .

FROM ghcr.io/astral-sh/uv:python3.12-bookworm-slim AS build
# ⚠️ WORKDIR phải TRÙNG với WORKDIR của stage runtime.
#
# uv ghi đường dẫn tuyệt đối vào shebang của mọi script trong .venv/bin. Build ở /src rồi
# chép sang /app thì shebang vẫn trỏ /src/.venv/bin/python, và container chết lúc khởi
# động với "exec /app/.venv/bin/uvicorn: no such file or directory" — một thông báo đổ lỗi
# cho uvicorn trong khi thứ thiếu là trình thông dịch.
WORKDIR /app

ENV UV_COMPILE_BYTECODE=1 \
    UV_LINK_MODE=copy

# Toàn bộ workspace được chép vào vì `uv.lock` là lock chung cho cả ba service — uv cần
# thấy pyproject của mọi member để giải đúng bản lock. `--package retrieval` giới hạn
# phần thực sự được cài.
COPY src/python/ ./

# --frozen: dựng đúng theo uv.lock, thất bại nếu lock đã cũ so với pyproject. Đây là điều
# mong muốn — image khác với môi trường CI là loại sai lệch không được phép im lặng.
RUN uv sync --frozen --no-dev --package retrieval

FROM python:3.12-slim-bookworm AS runtime
WORKDIR /app

# Chạy bằng người dùng không đặc quyền. Ảnh python:slim không có sẵn nên tạo ở đây.
RUN groupadd --system --gid 1001 xnk \
    && useradd --system --uid 1001 --gid xnk --no-create-home xnk

COPY --from=build --chown=xnk:xnk /app /app

ENV PATH="/app/.venv/bin:$PATH" \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

USER xnk
EXPOSE 8000

# Cổng 8000 khớp `container_port` trong .github/services.json. Không dùng --reload.
CMD ["uvicorn", "retrieval.main:app", "--host", "0.0.0.0", "--port", "8000"]
