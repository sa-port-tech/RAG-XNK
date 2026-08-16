# OIDC federation GitHub → AWS

> Nửa AWS của yêu cầu docs/00 §8.5 và story [E1-07](../../docs/09-product-backlog.md).
> Nửa GitHub nằm ở [`.github/setup/05-environments.sh`](../../.github/setup/05-environments.sh).
> Sở hữu: DevOps (N9) · Cần review 2 người vì nằm trong `infra/`.

---

## 1. Vì sao không dùng access key

docs/00 xếp "AWS access key dài hạn trong GitHub Actions" vào danh sách những việc
**không được làm**, với lý do một dòng: rò rỉ một lần là mất cả tài khoản.

Điểm cốt lõi của OIDC không phải là tiện hơn, mà là **không còn bí mật dài hạn nào để
rò rỉ**. GitHub ký một token JWT sống vài phút cho đúng workflow đang chạy; AWS đổi
token đó lấy credential tạm thời. Không có gì nằm trong GitHub Secrets, nên cũng không
có gì để lộ khi ai đó bị chiếm tài khoản, hay khi một action bên thứ ba bị chèn mã.

---

## 2. Kiến trúc

```
GitHub Actions (repo sa-port-tech/RAG-XNK)
        │  yêu cầu OIDC token  (cần permissions: id-token: write)
        ▼
token.actions.githubusercontent.com
        │  JWT có claim `sub` mô tả CHÍNH XÁC ngữ cảnh chạy
        ▼
AWS IAM OIDC Identity Provider
        │  sts:AssumeRoleWithWebIdentity, trust policy khớp `sub`
        ▼
IAM Role riêng cho từng môi trường  →  credential tạm thời 1 giờ
```

Ánh xạ chốt trong docs/00 §8.5:

| Claim `sub` của GitHub | IAM Role | AWS account |
|---|---|---|
| `repo:sa-port-tech/RAG-XNK:ref:refs/heads/main` | `XnkGitHubDeployDev` | dev |
| `repo:sa-port-tech/RAG-XNK:environment:staging` | `XnkGitHubDeployStaging` | staging |
| `repo:sa-port-tech/RAG-XNK:environment:production` | `XnkGitHubDeployProduction` | production |

**Vì sao production và staging khoá theo `environment:` chứ không theo `ref:`** — đây
là chi tiết quyết định toàn bộ giá trị của thiết kế. GitHub chỉ phát ra claim
`environment:production` khi job khai báo `environment: production`, và khi đó
environment ấy đã áp luật required reviewers ở phía GitHub. Nghĩa là **không tồn tại
đường lấy được credential production mà không đi qua người duyệt** — kể cả khi ai đó
sửa được file workflow, vì sửa workflow không tạo ra được claim mà họ không có quyền.

Nếu khoá theo `ref:refs/heads/main`, bất kỳ job nào chạy trên `main` đều assume được
role production, và cổng duyệt trở thành trang trí.

---

## 3. Dựng bằng tay lần đầu

Ba file trong thư mục này là **trust policy** — phần trả lời câu hỏi "ai được assume
role". Quyền role được làm gì (`ecr:*`, `ecs:*`…) do CDK sinh trong story E1-04 → E1-06,
không đặt ở đây.

```bash
# ① Đăng ký GitHub làm nhà cung cấp danh tính. Làm một lần cho mỗi AWS account.
#    Không cần thumbprint: từ 2023 AWS tự tin cậy CA gốc của GitHub.
aws iam create-open-id-connect-provider \
  --url https://token.actions.githubusercontent.com \
  --client-id-list sts.amazonaws.com
```

```bash
# ② Tạo role cho môi trường dev, gắn trust policy.
#    Thay <AWS_ACCOUNT_ID> trong file trước khi chạy.
aws iam create-role \
  --role-name XnkGitHubDeployDev \
  --assume-role-policy-document file://infra/oidc/trust-policy-dev.json \
  --max-session-duration 3600 \
  --description "GitHub Actions deploy vào môi trường dev — OIDC, không có access key"
```

```bash
# ③ Ghi ARN vào biến của GitHub Environment tương ứng.
gh variable set AWS_ROLE_ARN \
  --repo sa-port-tech/RAG-XNK --env dev \
  --body "arn:aws:iam::<AWS_ACCOUNT_ID>:role/XnkGitHubDeployDev"
```

Lặp lại ② và ③ cho `staging` và `production` với file trust policy tương ứng.

---

## 4. Nghiệm thu E1-07

Bốn AC của story, kèm cách kiểm chứng:

| AC | Kiểm chứng |
|---|---|
| Không có `AWS_ACCESS_KEY_ID` nào trong GitHub Secrets | `gh secret list --repo sa-port-tech/RAG-XNK` — cũng được `99-verify.sh` kiểm tra tự động |
| Trust policy giới hạn theo `repo:…:ref:refs/heads/main` | Đọc `trust-policy-dev.json`, đối chiếu `sts.amazonaws.com` và `sub` |
| Workflow deploy chạy thành công mà không có credential tĩnh | Chạy `cd-deploy` và xem bước "Lấy quyền AWS qua OIDC" |
| Thử assume role từ nhánh khác `main` → bị từ chối | Xem §5 |

---

## 5. Bài kiểm tra bắt buộc: assume role từ nhánh khác phải thất bại

Một trust policy viết sai vẫn **hoạt động bình thường** ở đường đi thuận. Nó chỉ lộ ra
khi có người thử đường đi nghịch — và lúc đó thường là đã muộn. Vì vậy AC cuối của
E1-07 không phải thủ tục giấy tờ.

Cách kiểm chứng, chạy một lần khi nghiệm thu:

1. Tạo nhánh `test/oidc-negative` từ `main`.
2. Thêm workflow tạm với `on: push` trên đúng nhánh đó, gọi
   `aws-actions/configure-aws-credentials` với `role-to-assume` là ARN của
   `XnkGitHubDeployDev`.
3. Push và xem log.

**Kết quả đúng là job ĐỎ**, với thông báo dạng:

```
Error: Could not assume role with OIDC: Not authorized to perform sts:AssumeRoleWithWebIdentity
```

Nếu job xanh, trust policy đang dùng ký tự đại diện quá rộng — kiểm tra lại xem có
đang để `repo:sa-port-tech/RAG-XNK:*` không. Xoá nhánh test sau khi xong.

---

## 6. Những chỗ hay sai

| Triệu chứng | Nguyên nhân |
|---|---|
| `Credentials could not be loaded` | Job thiếu `permissions: id-token: write`. Đây là lỗi phổ biến nhất, và khai báo `permissions` ở cấp workflow không tự lan xuống job đã tự khai báo `permissions` riêng. |
| `No OpenIDConnect provider found` | Chưa chạy bước ① trong đúng AWS account đó. |
| `Not authorized to perform sts:AssumeRoleWithWebIdentity` | `sub` trong trust policy không khớp ngữ cảnh chạy thật. In claim ra để đối chiếu thay vì đoán. |
| Role production assume được từ job không khai báo environment | Trust policy đang khoá theo `ref` thay vì `environment` — xem §2. |
| Chạy được lúc đầu rồi hỏng khi đổi tên repo | `sub` chứa tên repo. Đổi tên repo là đổi danh tính. |
