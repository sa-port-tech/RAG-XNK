# web — Blazor WebAssembly, đóng gói nội dung tĩnh rồi đổ vào volume cho nginx.
#
# ⚠️ Build context là GỐC REPO:
#
#   docker build -f src/dotnet/Xnk.Web/Dockerfile -t xnk/web .
#
# ── Vì sao là container chạy một lần rồi thoát, không phải một web server ─────────────
# Blazor WASM là **nội dung tĩnh**, không có tiến trình nào phải sống. Trên AWS nó nằm ở
# S3 sau CloudFront (.github/services.json mục "sites"). Ở local, nginx đã là cổng vào duy
# nhất rồi — dựng thêm một web server thứ hai chỉ để phục vụ mấy file tĩnh là thêm một
# tiến trình phải vận hành mà không đổi được gì.
#
# Container này chép nội dung vào volume dùng chung rồi thoát, giống hệt cách `nginx-conf`
# đổ file cấu hình sang cho nginx.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/dotnet/Directory.Build.props src/dotnet/Directory.Packages.props ./src/dotnet/
COPY src/dotnet/Xnk.Web/Xnk.Web.csproj ./src/dotnet/Xnk.Web/
RUN dotnet restore src/dotnet/Xnk.Web/Xnk.Web.csproj

COPY src/dotnet/Xnk.Web/ ./src/dotnet/Xnk.Web/
RUN dotnet publish src/dotnet/Xnk.Web/Xnk.Web.csproj \
        --configuration Release \
        --no-restore \
        --output /app

FROM alpine:3.21
WORKDIR /noi-dung

COPY --from=build /app/wwwroot/ ./

# Xoá trước khi chép: bỏ bước đó thì file của bản build cũ ở lại trong volume, và trình
# duyệt có thể nạp lẫn lộn hai phiên bản của cùng một ứng dụng.
ENTRYPOINT ["sh", "-c", "rm -rf /dich/* && cp -r /noi-dung/. /dich/ && echo 'Đã đổ giao diện vào volume.'"]
