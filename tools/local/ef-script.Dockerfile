# Xuất script SQL idempotent từ EF Core migrations, chạy trong container.
#
# ⚠️ Build context là GỐC REPO:
#
#   bash tools/local/xuat-migration.sh Xnk.IdentityTenant IdentityDbContext 0002_initial_identity
#
# ── Vì sao phải qua container ────────────────────────────────────────────────────────
# ADR-010 bắt buộc: sinh migration xong **phải** xuất `.sql` idempotent và commit cùng PR,
# vì file đó là hợp đồng liên ngôn ngữ mà ba service Python, docker-compose và task
# migration trên ECS đều dùng.
#
# Trên máy Windows có bật Application Control (WDAC / Smart App Control), `dotnet ef`
# **không nạp được** assembly vừa biên dịch và dừng với:
#
#   Could not load file or assembly '...dll'. An Application Control policy has blocked
#   this file. (0x800711C7)
#
# Thông báo đó nói về chính sách của máy, không phải về mã nguồn — và đổi chính sách bảo
# mật của máy để chạy được một lệnh sinh mã là cái giá sai. Trong container Linux thì
# không có chính sách đó, và kết quả xuất ra giống hệt.
#
# Lợi ích kèm theo: bước này cho ra cùng một file trên mọi máy và trên CI, không phụ thuộc
# việc ai đã cài `dotnet-ef` phiên bản nào.

FROM mcr.microsoft.com/dotnet/sdk:9.0

# Ghim đúng phiên bản EF Core mà repo dùng (Directory.Packages.props). Lệch phiên bản giữa
# công cụ và runtime là cách sinh ra migration mà chính ứng dụng không áp được.
ARG EF_VERSION=9.0.19
RUN dotnet tool install --global dotnet-ef --version ${EF_VERSION}
ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /src
COPY global.json ./
COPY src/dotnet/ ./src/dotnet/

# Project chứa DbContext, tên context, và tên file đầu ra (không có đuôi .sql).
ARG PROJECT
ARG CONTEXT
ARG OUTPUT

RUN test -n "${PROJECT}" && test -n "${CONTEXT}" && test -n "${OUTPUT}" \
    || (echo "Thiếu build-arg PROJECT / CONTEXT / OUTPUT" && exit 1)

RUN cd "src/dotnet/${PROJECT}" \
    && dotnet ef migrations script --idempotent \
        --context "${CONTEXT}" \
        --output "/out/${OUTPUT}.sql"
