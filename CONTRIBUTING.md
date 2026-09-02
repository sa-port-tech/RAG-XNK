# Đóng góp vào RAG XNK

Tài liệu đầy đủ về quy trình: [`docs/17`](docs/17-runbook-github.md).
Cách làm việc theo Scrum: [`docs/01`](docs/01-ke-hoach-prototype-scrum.md).

---

## Trước khi commit lần đầu

Repo bắt buộc **ký commit**. Không cấu hình trước thì push sẽ bị từ chối với thông báo
khó hiểu:

```bash
git config --global gpg.format ssh
git config --global user.signingkey ~/.ssh/id_ed25519.pub
git config --global commit.gpgsign true
```

Rồi thêm chính khoá đó vào https://github.com/settings/keys với loại **Signing Key**
(khoá Authentication đã thêm không dùng để ký được).

---

## Vòng đời một thay đổi

```
Issue Form  →  "Create a branch" trên issue  →  code  →  PR có "Closes #n"
            →  CI + review  →  squash merge  →  deploy dev tự động
```

**Luôn tạo nhánh bằng nút "Create a branch" ngay trên issue.** Nhánh đặt tên tay không
bắt đầu bằng số issue sẽ không tự chuyển cột trên board.

---

## Luật PR

| Luật | Ai thực thi |
|---|---|
| Tiêu đề theo Conventional Commits — `feat(retrieval): mô tả` | `pr-governance` |
| Có dòng `Closes #<số>` | `pr-governance` |
| ≥1 approval, **≥2 nếu chạm `infra/` hoặc `db/migrations/`** | ruleset + `pr-governance` |
| Code owner của đường dẫn bị đụng phải duyệt | ruleset |
| Giải quyết hết comment | ruleset |
| Toàn bộ CI xanh | ruleset |

---

## Chạy toàn bộ hệ thống ở local

```bash
cp .env.example .env
docker compose --profile app up -d
BASE_URL=http://localhost:8080 bash .github/scripts/smoke_test.sh
```

Lệnh thứ hai dựng 7 service, PostgreSQL, Camunda, Ollama và nginx làm cổng vào. Chi tiết
yêu cầu máy và bảng cổng: [`README.md`](README.md).

**Bốn điều dễ vấp, ghi ra để đỡ mất buổi sáng:**

| Triệu chứng | Nguyên nhân |
|---|---|
| `required variable ... is missing` | Chưa `cp .env.example .env` |
| `bind: address already in use` cổng 8080 | Đặt `XNK_HTTP_PORT` khác trong `.env` |
| Gọi API ra 404 | Thiếu tiền tố tên service: `/corpus/documents`, không phải `/documents`. ALB không cắt tiền tố (ADR-013) và nginx ở local tái hiện đúng vậy |
| Sửa `db/` hay `tools/local/` mà không thấy đổi | Hai thư mục đó được **đóng gói vào image**, không bind mount. Chạy lại với `--build` |

**Sau khi đổi lược đồ**, xuất SQL và commit cùng PR (ADR-010):

```bash
bash tools/local/xuat-migration.sh Xnk.IdentityTenant IdentityDbContext 0003_ten_migration
```

**Chạy test:**

```bash
dotnet test src/dotnet/Xnk.sln                    # .NET
cd src/python && uv run pytest retrieval          # Python — MỘT service mỗi lần
bash tools/local/chay-test.sh                     # .NET, trong container
```

Ba service Python có cùng tên gói `tests`, nên gộp cả ba vào một lần `pytest` sẽ dừng với
"import file mismatch". CI cũng chạy từng service một qua matrix.

---

## Chạy cổng chất lượng trên máy trước khi mở PR

```bash
# Mô hình BPMN — cấm Java Delegate
python .github/scripts/validate_bpmn.py bpmn/

# Cổng eval (cần bộ đo của E7-03/E7-04)
python .github/scripts/eval_gate.py check --results eval/reports/latest.json

# Smoke test một môi trường
BASE_URL=<url> bash .github/scripts/smoke_test.sh
```

---

## Chạm nghiệp vụ XNK

Nếu thay đổi liên quan tới hiệu lực văn bản, thuế suất, mã HS, Incoterms, loại hình tờ
khai hay nội dung câu trả lời: **chuyên gia XNK phải xác nhận trước khi đóng story**
([`docs/01`](docs/01-ke-hoach-prototype-scrum.md) §5.2).

Mở issue bằng form 🎓 **Expert Review** với **một câu hỏi đóng** và 2–3 phương án đã
cân nhắc sẵn. Chuyên gia chỉ có 20% × 2 người — đây là nguồn lực khan hiếm nhất của dự
án, đừng để họ phải tự nghĩ ra lựa chọn.

---

## Ba việc không được làm

| | Vì sao |
|---|---|
| **Java Delegate trong file `.bpmn`** | Khoá quy trình vào Camunda 7 và JVM. `ci-bpmn` chặn |
| **Nới ngưỡng trong `eval/gates.yml` để PR xanh** | Đó là gian lận với chính mình ([`docs/14`](docs/14-phuong-phap-golden-set.md)) |
| **Thêm access key AWS vào Secrets** | Dùng OIDC. Rò rỉ một lần là mất cả tài khoản |
