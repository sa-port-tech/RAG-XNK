# nginx-conf — sinh cấu hình nginx từ .github/services.json rồi thoát.
#
# ⚠️ Build context là GỐC REPO, giống mọi Dockerfile khác trong repo này:
#
#   docker build -f tools/local/Dockerfile -t xnk/nginx-conf .
#
# ── Vì sao COPY file vào image thay vì bind mount thư mục repo ────────────────────────
# Bind mount là cách hiển nhiên và ngắn hơn. Nó không dùng được, vì hai lý do độc lập:
#
# 1. Docker Desktop trên Windows chia sẻ file qua WSL2, và **ổ ảo của Google Drive
#    (File Stream) không chia sẻ được**. Tệ hơn: mount một đường dẫn không chia sẻ được
#    KHÔNG báo lỗi — container nhận một thư mục RỖNG, rồi chết với "No such file or
#    directory" trỏ vào một file đang nằm ngay đó trên máy. Repo này thật sự nằm ở
#    `G:\My Drive\...` trên máy dev đầu tiên, nên đây là ca đã xảy ra, không phải giả định.
#
# 2. Trên môi trường thật, bước tương đương chạy bằng một image trong ECR chứ không phải
#    bằng thư mục nào cả. Đóng gói vào image làm local giống production hơn một bậc.
#
# Cái giá: sửa render_nginx.py hay services.json thì phải `up -d --build`. Đổi lại, stack
# chạy được trên mọi máy mà không phụ thuộc cấu hình chia sẻ ổ đĩa của từng người.

FROM python:3.12-slim

WORKDIR /opt/xnk

# Script không cần phụ thuộc ngoài thư viện chuẩn — cố ý, để image này không phải cài gì.
COPY tools/local/render_nginx.py ./render_nginx.py
COPY .github/services.json ./services.json

# Ghi vào volume `nginx-conf` dùng chung với container nginx.
ENTRYPOINT ["python", "/opt/xnk/render_nginx.py", \
            "--services=/opt/xnk/services.json", \
            "--output=/etc/nginx/conf.d/default.conf"]
