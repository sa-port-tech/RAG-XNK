# migrate — áp lược đồ, vai trò và dữ liệu seed cho database local, rồi thoát.
#
# ⚠️ Build context là GỐC REPO:
#
#   docker build -f db/Dockerfile -t xnk/migrate .
#
# ── Vì sao COPY thay vì bind mount ───────────────────────────────────────────────────
# Cùng lý do đã ghi ở tools/local/Dockerfile: ổ ảo Google Drive không chia sẻ được vào
# WSL2, và Docker mount thành thư mục RỖNG mà không báo lỗi gì.
#
# Ở đây lý do thứ hai còn nặng hơn: trên môi trường thật, migration chạy bằng **một ECS
# one-off task** dựng từ image, trước bước deploy (ADR-010, cd-deploy.yml). Đóng gói
# migration vào image làm cho cơ chế ở local trùng với cơ chế trên cloud — thay vì hai
# đường khác nhau, và loại sai lệch chỉ lộ ra sau khi deploy.
#
# ── Vì sao alpine + postgresql-client, không phải postgres:16 ───────────────────────
# Container này cần đúng hai thứ: `psql` và `bash`. Ảnh `postgres:16` có cả hai, nhưng nó
# mang theo cả một MÁY CHỦ PostgreSQL — initdb, binary server, contrib — khoảng 430 MB cho
# một tiến trình chạy vài giây rồi thoát và không bao giờ phục vụ kết nối nào.
#
# Trên máy dev đó là vài phút tải thêm. Trên ECS nó là thứ kéo mỗi lần task migration khởi
# động, tức là nằm ngay trên đường tới hạn của mỗi lần deploy.
#
# Phiên bản client giữ đúng 16 để khớp máy chủ: `psql` mới hơn máy chủ thì cảnh báo, cũ hơn
# thì có cú pháp không hiểu.
#
# Không dùng ảnh .NET SDK để gọi `dotnet ef database update`: ba service Python và mọi công
# cụ vận hành đều cần đúng lược đồ này mà không nên phải cài SDK .NET để có nó (ADR-010).

FROM alpine:3.20

RUN apk add --no-cache bash postgresql16-client

# Toàn bộ thư mục db/: apply-migrations.sh, roles.sql, migrations/, seed/.
# Sửa bất cứ file nào trong đó thì phải build lại — `docker compose --profile app up -d --build`.
COPY db/ /db/

ENTRYPOINT ["bash", "/db/bootstrap-local.sh"]
