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
# Ảnh nền postgres:16 có sẵn `psql` và `bash`. Không dùng ảnh .NET SDK để gọi
# `dotnet ef database update`: ba service Python và mọi công cụ vận hành đều cần đúng lược
# đồ này mà không nên phải cài SDK .NET để có nó (ADR-010).

FROM postgres:16

# Toàn bộ thư mục db/: apply-migrations.sh, roles.sql, migrations/, seed/.
# Sửa bất cứ file nào trong đó thì phải build lại — `docker compose --profile app up -d --build`.
COPY db/ /db/

ENTRYPOINT ["bash", "/db/bootstrap-local.sh"]
