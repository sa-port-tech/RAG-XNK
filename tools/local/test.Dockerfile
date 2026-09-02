# Chạy toàn bộ bộ test .NET trong container.
#
#   bash tools/local/chay-test.sh                    # cả solution
#   bash tools/local/chay-test.sh "Category=TenantIsolation"
#
# ── Vì sao cần đường này bên cạnh `dotnet test` thẳng trên máy ────────────────────────
# Trên máy Windows bật Application Control (WDAC / Smart App Control), assembly test vừa
# biên dịch có thể bị chặn nạp:
#
#   Could not load file or assembly '...Tests.dll'. An Application Control policy has
#   blocked this file. (0x800711C7)
#
# Nó xảy ra không đều — cùng một project chạy được lúc trước, rồi bị chặn sau khi thêm
# file. Đổi chính sách bảo mật của máy để chạy được bộ test là cái giá sai; chạy trong
# container Linux thì không có chính sách đó.
#
# Lợi ích kèm theo: đây đúng là cách CI chạy — `PostgresFixture` ưu tiên biến
# XNK_TEST_CONNECTION và chỉ tự khởi Testcontainers khi không có nó. Trong container, biến
# đó trỏ tới PostgreSQL của docker-compose, nên không cần docker-in-docker.

FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /src

# Chép trước phần khai báo phụ thuộc rồi restore — giữ lớp cache khi chỉ mã nguồn đổi.
COPY global.json ./
COPY src/dotnet/Directory.Build.props src/dotnet/Directory.Packages.props src/dotnet/Xnk.sln ./src/dotnet/
COPY src/dotnet/Xnk.Shared/Xnk.Shared.csproj ./src/dotnet/Xnk.Shared/
COPY src/dotnet/Xnk.Corpus/Xnk.Corpus.csproj ./src/dotnet/Xnk.Corpus/
COPY src/dotnet/Xnk.Corpus.Tests/Xnk.Corpus.Tests.csproj ./src/dotnet/Xnk.Corpus.Tests/
COPY src/dotnet/Xnk.IdentityTenant/Xnk.IdentityTenant.csproj ./src/dotnet/Xnk.IdentityTenant/
COPY src/dotnet/Xnk.IdentityTenant.Tests/Xnk.IdentityTenant.Tests.csproj ./src/dotnet/Xnk.IdentityTenant.Tests/
COPY src/dotnet/Xnk.Chat/Xnk.Chat.csproj ./src/dotnet/Xnk.Chat/
COPY src/dotnet/Xnk.Chat.Tests/Xnk.Chat.Tests.csproj ./src/dotnet/Xnk.Chat.Tests/
COPY src/dotnet/Xnk.WorkflowWorker/Xnk.WorkflowWorker.csproj ./src/dotnet/Xnk.WorkflowWorker/
COPY src/dotnet/Xnk.WorkflowWorker.Tests/Xnk.WorkflowWorker.Tests.csproj ./src/dotnet/Xnk.WorkflowWorker.Tests/
COPY src/dotnet/Xnk.Web/Xnk.Web.csproj ./src/dotnet/Xnk.Web/
RUN dotnet restore src/dotnet/Xnk.sln

COPY src/dotnet/ ./src/dotnet/

# Bộ lọc test truyền qua biến môi trường; rỗng nghĩa là chạy tất cả.
ENV TEST_FILTER=""

ENTRYPOINT ["/bin/sh", "-c", \
  "if [ -n \"$TEST_FILTER\" ]; then dotnet test src/dotnet/Xnk.sln --no-restore --filter \"$TEST_FILTER\"; else dotnet test src/dotnet/Xnk.sln --no-restore; fi"]
