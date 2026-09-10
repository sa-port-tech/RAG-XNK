# Dockerfile DÙNG CHUNG cho ba service Python.
#
# Trước đây mỗi service một file, và ba file ấy khác nhau đúng ở tên gói. Khối giải thích
# `--factory` dài 5 dòng ở bản retrieval chỉ còn 3 dòng ở hai bản kia — cùng kiểu rữa như
# bên .NET: không sai, chỉ là lời giải thích mất dần ở các bản sao.
#
# ⚠️ Build context là GỐC REPO. cd-deploy.yml dùng `context: .` với
# `file: src/python/Dockerfile`.
#
#   docker build -f src/python/Dockerfile --build-arg GOI=retrieval -t xnk/retrieval .

FROM ghcr.io/astral-sh/uv:python3.12-bookworm-slim AS build

# Tên gói, ví dụ `retrieval`. Không có mặc định — xem lý do ở src/dotnet/Dockerfile.
ARG GOI
RUN test -n "$GOI" || (echo "Thiếu --build-arg GOI=<tên gói>" >&2; exit 1)

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
# thấy pyproject của mọi member để giải đúng bản lock. `--package` giới hạn phần thực sự
# được cài.
COPY src/python/ ./

# --frozen: dựng đúng theo uv.lock, thất bại nếu lock đã cũ so với pyproject. Đây là điều
# mong muốn — image khác với môi trường CI là loại sai lệch không được phép im lặng.
RUN uv sync --frozen --no-dev --package "$GOI"

FROM python:3.12-slim-bookworm AS runtime
WORKDIR /app

# Chạy bằng người dùng không đặc quyền. Ảnh python:slim không có sẵn nên tạo ở đây.
RUN groupadd --system --gid 1001 xnk \
    && useradd --system --uid 1001 --gid xnk --no-create-home xnk

COPY --from=build --chown=xnk:xnk /app /app

# ARG không sống qua ranh giới stage: phải khai lại.
ARG GOI
ENV PATH="/app/.venv/bin:$PATH" \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    XNK_GOI="${GOI}"

USER xnk
EXPOSE 8000

# Cổng 8000 khớp `container_port` trong .github/services.json. Không dùng --reload.
#
# `--factory`: mỗi service có cấu hình bắt buộc, nên ứng dụng được dựng bằng create_app()
# lúc khởi động thay vì bằng một biến toàn cục lúc import. Nhờ vậy chỉ import module (ví
# dụ trong unit test) không đòi đủ biến môi trường. Xem chú thích cuối <gói>/main.py.
#
# Dạng shell để `${XNK_GOI}` được khai triển; `exec` để uvicorn nhận PID 1 và thấy SIGTERM.
CMD ["/bin/sh", "-c", "exec uvicorn \"$XNK_GOI.main:create_app\" --factory --host 0.0.0.0 --port 8000"]
