# Dockerfile DÙNG CHUNG cho bốn service API .NET.
#
# Trước đây mỗi service có một Dockerfile riêng, và bốn file ấy khác nhau đúng ở tên
# project cộng vài chữ trong comment. Chúng đã bắt đầu trôi: bản của Xnk.Corpus có một
# đoạn giải thích `container_port` mà ba bản kia rút gọn mất — đó là cách một bản sao rữa,
# không phải bằng lỗi mà bằng việc lời giải thích biến mất khỏi ba trong bốn chỗ.
#
# ⚠️ Xnk.Web KHÔNG dùng file này. Nó publish bundle WASM rồi chép vào một volume để nginx
# phục vụ — một công thức khác hẳn, không phải một biến thể.
#
# ⚠️ Build context là GỐC REPO. cd-deploy.yml dùng `context: .` với
# `file: src/dotnet/Dockerfile`, nên mọi đường dẫn COPY tính từ gốc repo.
#
#   docker build -f src/dotnet/Dockerfile --build-arg DU_AN=Xnk.Corpus -t xnk/corpus .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Tên project, ví dụ `Xnk.Corpus`. Không có giá trị mặc định: mặc định ở đây nghĩa là một
# lần quên `--build-arg` sẽ dựng nhầm service mà build vẫn xanh.
ARG DU_AN
RUN test -n "$DU_AN" || (echo "Thiếu --build-arg DU_AN=<tên project>" >&2; exit 1)

WORKDIR /src

# Chép trước phần khai báo phụ thuộc rồi restore, sau đó mới chép mã nguồn. Nhờ vậy đổi
# một dòng code không làm mất lớp cache của restore — bước tốn thời gian nhất.
COPY global.json ./
COPY src/dotnet/Directory.Build.props src/dotnet/Directory.Packages.props ./src/dotnet/
COPY src/dotnet/Xnk.Shared/Xnk.Shared.csproj ./src/dotnet/Xnk.Shared/
COPY src/dotnet/Xnk.Shared/packages.lock.json ./src/dotnet/Xnk.Shared/
COPY src/dotnet/${DU_AN}/${DU_AN}.csproj ./src/dotnet/${DU_AN}/
COPY src/dotnet/${DU_AN}/packages.lock.json ./src/dotnet/${DU_AN}/

# --locked-mode: dựng đúng theo packages.lock.json, đỏ nếu lock đã cũ so với
# Directory.Packages.props. Image khác với môi trường CI là loại sai lệch không được phép
# im lặng — cùng lý do `uv sync --frozen` bên Python.
RUN dotnet restore src/dotnet/${DU_AN}/${DU_AN}.csproj --locked-mode

COPY src/dotnet/Xnk.Shared/ ./src/dotnet/Xnk.Shared/
COPY src/dotnet/${DU_AN}/ ./src/dotnet/${DU_AN}/
RUN dotnet publish src/dotnet/${DU_AN}/${DU_AN}.csproj \
        --configuration Release \
        --no-restore \
        --output /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Ảnh aspnet:9.0 đã có sẵn người dùng không đặc quyền `app` (uid 1654) và đặt
# ASPNETCORE_HTTP_PORTS=8080. Dùng lại thay vì tự tạo user — trùng với `container_port`
# khai trong .github/services.json.
USER app
EXPOSE 8080

COPY --from=build /app .

# ARG không sống qua ranh giới stage: phải khai lại để dùng ở ENTRYPOINT.
ARG DU_AN
ENV XNK_DLL="${DU_AN}.dll"

# Gọi thẳng dll: Directory.Build.props tắt UseAppHost nên không có file thực thi gốc.
#
# Dạng shell để `${XNK_DLL}` được khai triển — dạng exec sẽ truyền nguyên chuỗi
# "${XNK_DLL}" làm tên file. `exec` để dotnet nhận PID 1 và thấy SIGTERM khi ECS thu hồi
# task; thiếu nó thì shell giữ PID 1 và container chỉ chết sau khi hết thời gian chờ.
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet \"$XNK_DLL\""]
