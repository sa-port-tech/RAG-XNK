# corpus-service.
#
# ⚠️ Build context là GỐC REPO, không phải thư mục này — cd-deploy.yml dùng
# `context: .` với `file: <service_path>/Dockerfile`. Mọi đường dẫn COPY dưới đây vì vậy
# tính từ gốc repo. Build tay để kiểm tra:
#
#   docker build -f src/dotnet/Xnk.Corpus/Dockerfile -t xnk/corpus .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Chép trước phần khai báo phụ thuộc rồi restore, sau đó mới chép mã nguồn. Nhờ vậy đổi
# một dòng code không làm mất lớp cache của restore — bước tốn thời gian nhất.
COPY global.json ./
COPY src/dotnet/Directory.Build.props src/dotnet/Directory.Packages.props ./src/dotnet/
COPY src/dotnet/Xnk.Shared/Xnk.Shared.csproj ./src/dotnet/Xnk.Shared/
COPY src/dotnet/Xnk.Corpus/Xnk.Corpus.csproj ./src/dotnet/Xnk.Corpus/
RUN dotnet restore src/dotnet/Xnk.Corpus/Xnk.Corpus.csproj

COPY src/dotnet/Xnk.Shared/ ./src/dotnet/Xnk.Shared/
COPY src/dotnet/Xnk.Corpus/ ./src/dotnet/Xnk.Corpus/
RUN dotnet publish src/dotnet/Xnk.Corpus/Xnk.Corpus.csproj \
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

# Gọi thẳng dll: Directory.Build.props tắt UseAppHost nên không có file thực thi gốc.
ENTRYPOINT ["dotnet", "Xnk.Corpus.dll"]
