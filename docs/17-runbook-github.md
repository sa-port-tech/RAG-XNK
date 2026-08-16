# Runbook GitHub — cài đặt và vận hành

> **Trạng thái: ✅ Dùng được.**
> Sở hữu: DevOps (N9) · Đồng sở hữu: Tech Lead (N4) · Cập nhật khi đổi cấu hình
>
> Tài liệu này là **phần thực thi** của [`00`](00-ke-hoach-tong-the.md) §8. Chỗ nào
> §8 nói "phải như thế này", chỗ này nói "nó nằm ở file nào và kiểm chứng bằng lệnh gì".

---

## 1. Hai quyết định nền và cái giá của chúng

### 1.1 Repo đặt trong Organization, không phải tài khoản cá nhân

`CODEOWNERS` trong [`00`](00-ke-hoach-tong-the.md) §8.6 dùng handle dạng
`@xnk-expert-team`, `@ai-lead` — đó là **team**, và tài khoản cá nhân không có team.
Trên repo cá nhân, năm dòng CODEOWNERS ấy sẽ bị GitHub bỏ qua trong im lặng: không
báo lỗi, không gán reviewer, và không ai biết cho tới khi có sự cố.

Org: `sa-port-tech` · Repo: `sa-port-tech/RAG-XNK`

### 1.2 Repo để công khai

| Yêu cầu | Personal + private | Org + private (free) | **Org + public** |
|---|---|---|---|
| Ruleset trên `main` (§8.4) | ❌ | ❌ | ✅ |
| CodeQL (§8.3 ④) | ❌ | ❌ | ✅ |
| Environment protection (§8.5) | ❌ | ❌ | ✅ |
| Team cho CODEOWNERS (§8.6) | ❌ | ✅ | ✅ |
| Chi phí | $0 | $0 | **$0** |

Ba trong bốn cổng chỉ tồn tại khi repo công khai hoặc khi trả tiền gói Team. Ngân sách
[`01`](01-ke-hoach-prototype-scrum.md) §7.1 dự trù $0–63 cho GitHub, nhưng gói Team vẫn
không mở được CodeQL — thứ đó cần GitHub Advanced Security, một khoản khác hẳn.

**Cái giá phải nhìn thẳng:** mã nguồn công khai. Rủi ro dữ liệu bằng 0 vì
[`00`](00-ke-hoach-tong-the.md) §8.1 đã chốt corpus không nằm trong git, và §6 chốt
license key Telerik nằm trong Secrets Manager. Cái công khai là **cách hệ thống được
xây**, không phải nội dung nó phục vụ.

Muốn đảo ngược: đổi `VISIBILITY` trong [`config.env`](../.github/setup/config.env),
chạy lại `01-repo-settings.sh`, và chấp nhận mất ba cổng ở trên cho tới khi nâng gói.

---

## 2. Dựng lần đầu

### 2.1 Ba việc phải làm bằng tay

GitHub không mở API cho ba thứ này:

| Việc | Làm ở đâu |
|---|---|
| Tạo Organization `sa-port-tech` | https://github.com/organizations/plan → **Free** |
| Field `Iteration` trên Project | Project → Settings → Fields → New field → Iteration, chu kỳ **2 tuần** |
| Thêm người vào team | `gh api -X PUT orgs/sa-port-tech/teams/<team>/memberships/<user> -f role=member` |

### 2.2 Trình tự

```bash
# ① Chuyển repo vào org
gh api -X POST repos/<user>/RAG-XNK/transfer -f new_owner=sa-port-tech

# ② Cấp scope cho CLI
gh auth refresh -h github.com -s admin:org,project,workflow,repo

# ③ Đẩy toàn bộ .github/ lên main TRƯỚC khi khoá nhánh
git push origin main

# ④ Áp toàn bộ cấu hình
bash .github/setup/apply-all.sh
```

**Thứ tự ③ trước ④ không đổi được.** Đặt ruleset khi `main` chưa có workflow là tự
khoá mình ra ngoài: không push thẳng được nữa, mà cũng chưa có check nào để báo cáo,
nên PR đầu tiên sẽ treo vĩnh viễn ở trạng thái chờ.

### 2.3 Cờ tạm thời phải tắt

[`config.env`](../.github/setup/config.env) có `BOOTSTRAP_ADMIN_BYPASS=true`.

Lý do tồn tại: ruleset bật "require review from Code Owners", mà khi org mới có một
người thì người đó vừa là tác giả PR vừa là code owner duy nhất — GitHub không cho tự
duyệt PR của mình, nên **không PR nào merge được**.

**Đặt lại thành `false` và chạy lại `06-ruleset.sh` ngay khi org có người thứ hai.**
`99-verify.sh` cảnh báo chừng nào cờ này còn bật.

---

## 3. Ký commit — việc mỗi người phải làm một lần

Ruleset bật `required_signatures` theo [`00`](00-ke-hoach-tong-the.md) §8.4. Commit
không ký sẽ bị từ chối với thông báo không nói rõ nguyên nhân, nên đây là bước
onboarding bắt buộc, không phải tuỳ chọn.

Ký bằng SSH đơn giản hơn GPG và dùng lại đúng khoá đang có:

```bash
git config --global gpg.format ssh
git config --global user.signingkey ~/.ssh/id_ed25519.pub
git config --global commit.gpgsign true
```

Rồi thêm chính khoá đó vào GitHub **một lần nữa** với loại **Signing Key** (khoá
Authentication đã thêm không dùng để ký được):
https://github.com/settings/keys

Kiểm chứng: `git log --show-signature -1` phải hiện `Good "git" signature`.

---

## 4. Vận hành thường ngày

Bảy bước của [`00`](00-ke-hoach-tong-the.md) §8.3, kèm cái gì tự động và cái gì không:

| | Bước | Người làm | Tự động |
|---|---|---|---|
| ① | Mở issue bằng Issue Form | bất kỳ ai | vào Project cột `Backlog`, điền Priority/Component/Phase/Story Points từ chính câu trả lời trong form |
| ② | Sprint Planning | PO + đội | — *(cố ý để con người quyết định)* |
| ③ | Bấm **"Create a branch"** trên issue | dev | nhánh `123-mo-ta`, liên kết hai chiều, issue → `In Progress` |
| ④ | Mở PR có `Closes #123` | dev | CODEOWNERS gán reviewer, CI chạy theo path filter, issue → `In Review` |
| ⑤ | Cổng chất lượng | CI | 7 required check |
| ⑥ | Squash merge | reviewer | issue tự đóng, → `Testing`, nhánh tự xoá |
| ⑦ | Deploy | tự động (dev) | smoke test, issue → `Done` |

Bước ③ **phải** dùng nút "Create a branch" trên issue. Nhánh đặt tên tay không bắt đầu
bằng số issue sẽ không kích hoạt được chuyển cột — `project-automation` đọc số issue từ
đầu tên nhánh.

---

## 5. Ma trận truy vết — yêu cầu §8 → cài đặt → kiểm chứng

Đây là phần trả lời "có sót yêu cầu nào không".

### 5.1 §8.1 — Cấu trúc monorepo

| Yêu cầu | Cài đặt | Kiểm chứng |
|---|---|---|
| `ISSUE_TEMPLATE/{bug,feature,corpus-issue,expert-review}.yml` | 4 file + `config.yml` | `99-verify.sh` §8.1 |
| `PULL_REQUEST_TEMPLATE.md` · `CODEOWNERS` | có | `99-verify.sh` §8.1 |
| 7 workflow | 7 + `codeql` + `pr-governance` (xem §6.1) | `99-verify.sh` §8.1 |
| Prompt và BPMN versioned trong git | `prompts/`, `bpmn/` có CODEOWNERS riêng, đụng vào thì chạy `eval-regression` / `ci-bpmn` | path filter trong workflow |
| Corpus KHÔNG trong git | `.gitignore` + repo không có thư mục corpus | — |

### 5.2 §8.2 — Quản lý ticket

| Yêu cầu | Cài đặt | Kiểm chứng |
|---|---|---|
| Issue Form YAML có trường cấu trúc | 4 form, dropdown + checkbox, `blank_issues_enabled: false` | mở thử một issue |
| Board 6 cột | `04-project.sh` ghi đè lựa chọn của field `Status` | `99-verify.sh` §8.2 |
| Custom field Priority · Component · Phase · Story Points · Expert Review Required | `04-project.sh` | `99-verify.sh` §8.2 |
| Custom field Iteration | **bấm tay** — API không tạo được | `99-verify.sh` nhắc |
| Dữ liệu form vào Project | `project-automation.cjs` đọc phần thân issue do form sinh ra và điền vào field | mở issue rồi xem board |

> `Expert Review Required` được mô tả là boolean trong §8.2. Projects v2 không có kiểu
> boolean, nên dùng single-select `Có`/`Không` — lọc trên board vẫn hoạt động y hệt.

### 5.3 §8.3 — Luồng từ ticket đến deploy

| Bước | Cài đặt |
|---|---|
| ① vào `Backlog` | `project-automation.yml` · sự kiện `issues.opened` |
| ③ branch → `In Progress` | `project-automation.yml` · sự kiện `create` |
| ④ PR → `In Review` | `project-automation.yml` · sự kiện `pull_request.opened` |
| ④ CODEOWNERS gán reviewer | ruleset `require_code_owner_review` |
| ④ CI theo path filter | mọi workflow lọc bằng `dorny/paths-filter` ở cấp job (xem §6.2) |
| ⑤ cổng chất lượng | ruleset `required_status_checks` |
| ⑥ squash merge | `01-repo-settings.sh` tắt merge commit và rebase |
| ⑦ build → ECR → dev → smoke test | `cd-deploy.yml` |
| ⑦ chờ duyệt thủ công | GitHub Environment `production` có required reviewers |
| ⑦ blue/green + auto rollback | `cd-deploy.yml` khi `vars.USE_CODEDEPLOY_BLUE_GREEN=true` |
| ⑦ issue → `Done` + tạo Release | `project-automation.yml` (`workflow_run`) + job `release` |

### 5.4 §8.4 — Branch protection

| Yêu cầu §8.4 | Rule trong ruleset |
|---|---|
| Cấm push trực tiếp | `pull_request` |
| Bắt buộc PR và status check | `pull_request` + `required_status_checks` |
| Linear history (squash merge) | `required_linear_history` + cấu hình repo chỉ cho squash |
| Giải quyết hết comment trước khi merge | `required_review_thread_resolution: true` |
| Signed commits | `required_signatures` |
| Cấm force push và xoá nhánh | `non_fast_forward` + `deletion` |

Thêm ngoài §8.4, vì thiếu chúng thì các luật trên vẫn lách được:
`strict_required_status_checks_policy` (bắt nhánh cập nhật với `main` trước khi merge —
nếu không, hai PR xanh độc lập vẫn hợp lại thành `main` đỏ),
`dismiss_stale_reviews_on_push` và `require_last_push_approval` (approval phải áp cho
đúng đoạn mã sẽ được merge, không phải cho phiên bản trước đó).

### 5.5 §8.5 — Xác thực GitHub Actions → AWS

Xem [`infra/oidc/README.md`](../infra/oidc/README.md). Điểm cốt lõi: role `production`
và `staging` khoá theo claim `environment:`, không theo `ref:` — nhờ đó không tồn tại
đường lấy credential production mà không đi qua người duyệt, kể cả khi sửa được
workflow file.

### 5.6 §8.6 — CODEOWNERS

Năm đường dẫn của §8.6 giữ nguyên. Bốn nhóm bổ sung, kèm lý do:

| Bổ sung | Vì sao |
|---|---|
| `*` → `@tech-lead` | §8.6 không nêu chủ sở hữu mặc định; thiếu nó thì phần lớn PR không có reviewer tự động và §8.3 ④ không thành hiện thực |
| `/.github/` → `@devops-lead @tech-lead` | §8.5 chốt "không có đường deploy thẳng lên production, kể cả khi ai đó sửa được workflow file". Muốn đúng vậy thì chính workflow file phải có chủ |
| `/eval/gates.yml`, `/.github/quality-gates.yml` | Ngưỡng chặn merge nằm ở đây. Sửa được hai file này là sửa được cổng |
| `/eval/golden-set/baseline.json` → `@ai-lead` | Do CI sinh ra, không phải thước đo. Bắt chuyên gia XNK duyệt từng lần cập nhật số liệu tự động là tiêu thời gian khan hiếm nhất của dự án vào việc không cần chuyên môn của họ |

---

## 6. Bốn chỗ GitHub không làm được như tài liệu mô tả

Ghi lại thành mục riêng vì đây là những chỗ người đọc §8 sẽ tưởng đã có mà thật ra phải
tự dựng.

### 6.1 Ruleset không đặt được số approval theo đường dẫn

[`01`](01-ke-hoach-prototype-scrum.md) §5.2 yêu cầu "≥1 approval, ≥2 nếu chạm `infra/`
hoặc `db/migrations/`". Ruleset chỉ nhận **một** con số cho cả nhánh.

Đặt cứng 2 cho mọi PR là cách sai: nó bắt cả PR sửa lỗi chính tả phải chờ hai người, và
đội sẽ học cách duyệt cho xong cho nhanh — tức là làm hỏng chính cơ chế review.

→ Ruleset đặt 1; mức 2 do [`pr-governance.yml`](../.github/workflows/pr-governance.yml)
thực thi bằng cách đọc danh sách file thay đổi và đếm approval còn hiệu lực. Ngưỡng
khai báo trong [`quality-gates.yml`](../.github/quality-gates.yml) → `pull_request`.

`pr-governance` là workflow **thứ 8**, ngoài danh sách 7 của §8.1. Nó cũng gánh luôn
hai luật khác không diễn đạt được bằng ruleset: tiêu đề PR theo Conventional Commits
(vì §8.3 ⑦ dựng changelog từ tiêu đề PR đã squash) và PR phải liên kết issue (vì §8.3
④⑥ dựa vào liên kết đó để chuyển cột).

### 6.2 Path filter ở cấp `on:` làm treo required check

Cách viết trực giác là `on: pull_request: paths: ['src/dotnet/**']`. Nó hỏng: khi PR
không đụng .NET, workflow **không chạy**, check `ci-dotnet` không bao giờ báo cáo, và
PR treo vĩnh viễn ở `Expected — Waiting for status to be reported`.

→ Mọi workflow chạy trên mọi PR, lọc đường dẫn ở **cấp job** bằng `dorny/paths-filter`,
và kết thúc bằng một job tổng hợp trùng tên workflow với `if: always()`. Job đó luôn
báo cáo, nên required check luôn có kết quả. Job rỗng chạy hết vài giây.

Đây là lý do bảy tên trong `REQUIRED_CHECKS` là **tên job**, không phải tên workflow.

### 6.3 GITHUB_TOKEN không ghi được Projects v2

Không có khoá `permissions` nào cấp được quyền này — giới hạn nằm ở phía GitHub.

→ `project-automation` dùng `secrets.PROJECT_TOKEN`, một fine-grained PAT:

| | |
|---|---|
| Tạo tại | https://github.com/settings/personal-access-tokens/new |
| Resource owner | `sa-port-tech` |
| Organization permissions | Projects: **Read and write** |
| Repository permissions | Issues: Read · Pull requests: Read · Metadata: Read |
| Lưu bằng | `gh secret set PROJECT_TOKEN --repo sa-port-tech/RAG-XNK` |
| Hạn | 90 ngày — **đặt lịch xoay vòng, hết hạn thì board lặng lẽ ngừng cập nhật** |

Token gắn với một cá nhân. Khi người đó rời dự án, board ngừng chạy — chuyển sang
GitHub App khi đội ổn định là việc của Phase 1.

### 6.4 Ruleset cấm push trực tiếp, kể cả của CI

[`09`](09-product-backlog.md) E7-05 yêu cầu baseline cập nhật khi merge vào `main`.
Cách thường thấy là cho CI push thẳng, nhưng làm vậy phải mở bypass cho GitHub Actions —
và điều đó biến "cấm push trực tiếp" thành "cấm push trực tiếp, trừ khi viết được một
workflow", tức là không cấm gì cả.

→ `eval-regression` **mở PR** cập nhật baseline thay vì push. PR này không tự kích hoạt
lại chính nó vì `baseline.json` bị loại khỏi bộ lọc đường dẫn, và commit được ký qua
API (`sign-commits: true`) để không vướng `required_signatures`.

---

## 7. Trạng thái hiện tại — cái gì đã sống, cái gì chưa

| Hạng mục | Trạng thái |
|---|---|
| Issue Form, PR template, CODEOWNERS, nhãn | ✅ dùng được ngay |
| Board + tự động chuyển cột | ✅ sau khi đặt `PROJECT_TOKEN` và tạo field `Iteration` |
| Ruleset, environment, chính sách Actions | ✅ |
| `ci-bpmn` | ✅ chạy thật — validator có bài tự kiểm chứng trong workflow |
| `pr-governance` | ✅ chạy thật |
| `codeql` | ✅ chạy thật |
| `ci-dotnet`, `ci-python`, `ci-blazor` | ⬜ đúng logic, nhưng chưa có mã để build — kích hoạt khi E1-11 dựng skeleton |
| `cd-deploy` | ⬜ chờ hạ tầng E1-04 → E1-06 và biến `AWS_ROLE_ARN` |
| `eval-regression` | 🔶 **cổng đang TẮT** — xem dưới |

### 7.1 Cổng eval đang tắt

[`eval/gates.yml`](../eval/gates.yml) có `enabled: false`, vì bộ đo chưa tồn tại
(E7-03, E7-04). Workflow vẫn chạy và vẫn đăng bảng so sánh vào PR, nhưng **không chặn
merge**.

> 🔴 Chừng nào dòng đó còn `false`, tuyên bố của E7-05 — *"không ai merge được thay đổi
> làm hệ thống trích dẫn văn bản hết hiệu lực, kể cả vô tình"* — **chưa thành hiện thực**.

Bật lên trong chính PR hoàn thành E7-04. Đó là một dòng diff, và CODEOWNERS
(`@ai-lead` + `@tech-lead`) phải duyệt nó.

### 7.2 Hợp đồng mà mã nguồn phải tuân theo

Workflow đã viết xong giả định những thứ sau sẽ tồn tại. Đây không phải chỗ trống bỏ
quên — đây là hợp đồng để story sau cắm vào:

| Ai cần | Cái gì |
|---|---|
| `ci-dotnet` | solution tại `src/dotnet/Xnk.sln`; project test tham chiếu `coverlet.collector`; ít nhất một test gắn `[Trait("Category", "TenantIsolation")]` |
| `ci-python` | `src/python/pyproject.toml` quản lý bằng `uv`, có ruff + mypy + pytest-cov |
| `ci-blazor` | `src/dotnet/Xnk.Web/Xnk.Web.csproj` |
| `cd-deploy` | mỗi service có `Dockerfile` tại thư mục của nó; ECR repo và ECS service đúng tên trong [`services.json`](../.github/services.json) |
| `eval-regression` | `eval/harness/__main__.py` xuất JSON `{"question_count": n, "metrics": {...}}` với 6 khoá chỉ số |

`ci-dotnet` cố ý coi **"không có test nào khớp bộ lọc TenantIsolation" là ĐỎ**. Cổng
cách ly tenant tự tắt trong im lặng đúng vào lúc nó cần thiết nhất là kịch bản tệ hơn
hẳn so với một lần CI đỏ.

---

## 8. Checklist Sprint 0

- [ ] Tạo org `sa-port-tech`, chuyển repo vào
- [ ] `bash .github/setup/apply-all.sh` xanh
- [ ] Tạo field `Iteration` trên Project, chu kỳ 2 tuần
- [ ] Đặt `secrets.PROJECT_TOKEN`
- [ ] Thêm 11 người vào 9 team; `@xnk-expert-team` đủ 2 chuyên gia
- [ ] Mỗi người cấu hình ký commit (§3) và commit thử một lần
- [ ] Mở một PR nháp để 7 required check chạy lần đầu, rồi chạy lại `99-verify.sh`
- [ ] `BOOTSTRAP_ADMIN_BYPASS=false` sau khi org có người thứ hai
- [ ] Dựng OIDC provider + 3 IAM role, đặt `AWS_ROLE_ARN` cho từng environment
- [ ] Chạy bài kiểm tra nghịch của E1-07: assume role từ nhánh khác `main` **phải thất bại**
- [ ] Nhập backlog [`09`](09-product-backlog.md) vào Project bằng Issue Form

---

## 9. Sự cố thường gặp

| Triệu chứng | Nguyên nhân | Xử lý |
|---|---|---|
| PR treo ở `Expected — Waiting for status` | Check chưa từng chạy trên repo | Mở một PR nháp cho workflow chạy một lượt |
| Không merge được PR của chính mình | Code owner duy nhất là tác giả | Thêm người vào team, hoặc tạm bật `BOOTSTRAP_ADMIN_BYPASS` |
| Issue không vào board | Thiếu `PROJECT_TOKEN` hoặc `PROJECT_NUMBER` | Xem log job `sync`; chạy `04-project.sh` |
| Issue không chuyển sang `In Progress` | Nhánh đặt tên tay | Dùng nút "Create a branch" trên issue |
| CODEOWNERS không gán ai | Team chưa có quyền write | `02-teams.sh`; kiểm tra `repos/.../codeowners/errors` |
| Commit bị từ chối | Chưa cấu hình ký | §3 |
| `Credentials could not be loaded` trong cd-deploy | Job thiếu `permissions: id-token: write` | [`infra/oidc/README.md`](../infra/oidc/README.md) §6 |
| Board ngừng cập nhật sau vài tháng | `PROJECT_TOKEN` hết hạn | Tạo lại token, đặt lịch nhắc |
