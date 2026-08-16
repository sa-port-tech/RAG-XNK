# Dựng lại hạ tầng GitHub từ đầu

> **Trạng thái: ✅ Dùng được.** Sở hữu: DevOps (N9)
>
> [`17`](17-runbook-github.md) trả lời *"cái gì nằm ở đâu và vì sao"*.
> Tài liệu này trả lời *"gõ gì, theo thứ tự nào, và khi hỏng thì làm gì"*.
>
> Mọi sự cố liệt kê ở §6 đều **đã thật sự xảy ra** trong lần dựng đầu tiên
> (16/08/2026). Không có mục nào là giả định.

---

## 1. Dùng tài liệu này khi nào

- Dựng lại dự án trên một org khác
- Đưa người mới vào và họ cần chạy được `apply-all.sh`
- Gặp một thông báo lỗi lạ → tra thẳng §6 bằng chính chuỗi lỗi

**Nguyên tắc xuyên suốt: mỗi bước đều có "kết quả mong đợi".** Không thấy đúng kết
quả đó thì dừng lại, đừng đi tiếp — mọi sự cố tốn thời gian nhất trong lần dựng đầu
đều là do một bước im lặng làm sai rồi ba bước sau mới lộ ra.

---

## 2. Chuẩn bị máy — Windows

### 2.1 Cài công cụ

Mở **PowerShell bằng Run as Administrator** (bắt buộc, xem [S-01](#s-01)):

```powershell
winget install --id GitHub.cli --accept-package-agreements --accept-source-agreements
winget install --id jqlang.jq --scope user --accept-package-agreements --accept-source-agreements
winget install --id Python.Python.3.12 --accept-package-agreements --accept-source-agreements
```

Mỗi lệnh **một gói** — `winget install --id A --id B` là cú pháp sai và im lặng bỏ qua gói thứ hai.

### 2.2 Kiểm chứng

Mở **Git Bash** rồi chạy:

```bash
gh --version && jq --version && python --version && git --version
```

| Công cụ | Nếu "command not found" |
|---|---|
| `gh` | thêm `/c/Program Files/GitHub CLI` vào `PATH` |
| `jq` | thêm `/c/Users/<user>/AppData/Local/Microsoft/WinGet/Packages/jqlang.jq_*` vào `PATH` |
| `python` | cài lại, **tick "Add python.exe to PATH"** |

> ⚠️ **Đừng tin `python3`.** Trên Windows nó thường là App Execution Alias của
> Microsoft Store: có trong `PATH`, `command -v` thấy nó, nhưng chạy thì mở cửa hàng
> ứng dụng. `lib.sh` đã tự dò ([S-04](#s-04)).

```bash
python -m pip install --quiet pyyaml
```

### 2.3 Đăng nhập GitHub CLI

```bash
gh auth login
```

```bash
gh auth refresh -h github.com -s admin:org,project,workflow,repo,admin:ssh_signing_key
```

**Kết quả mong đợi** — `gh auth status` in ra đủ 5 scope. Thiếu scope nào thì bước
tương ứng sẽ hỏng với HTTP 404 hoặc 403, không phải thông báo "thiếu quyền" ([S-22](#s-22)).

### 2.4 Bật ký commit

Ruleset bắt buộc `required_signatures`, và **squash merge KHÔNG vượt qua được luật
này** ([S-34](#s-34)). Không làm bước này thì không merge được PR nào.

```bash
git config --global gpg.format ssh
```

```bash
git config --global commit.gpgsign true
```

```bash
git config --global user.signingkey "C:/Users/<user>/.ssh/id_ed25519.pub"
```

Chưa có khoá thì tạo: `ssh-keygen -t ed25519`.

Đăng ký khoá lên GitHub — **phải chọn đúng loại Signing Key**:

```bash
gh ssh-key add "C:/Users/<user>/.ssh/id_ed25519.pub" --type signing --title "May lam viec"
```

Hoặc qua web: https://github.com/settings/ssh/new → **Key type = Signing Key**.

> Khoá *authentication* và khoá *signing* là **hai loại tách biệt**. Cùng một file
> khoá phải đăng ký **hai lần**. `admin:public_key` không bao gồm signing key ([S-22](#s-22)).

Để `git log --show-signature` hiển thị đúng ở máy (không ảnh hưởng GitHub, [S-37](#s-37)):

```bash
mkdir -p ~/.config/git && printf '%s namespaces="git" %s\n' "$(git config --get user.email)" "$(cut -d' ' -f1,2 ~/.ssh/id_ed25519.pub)" > ~/.config/git/allowed_signers && git config --global gpg.ssh.allowedSignersFile ~/.config/git/allowed_signers
```

**Kết quả mong đợi** — tạo một commit thử, `git log --format='%G?' -1` in ra `G`.

---

## 3. Bốn việc GitHub không cho tự động

Làm trước, vì các script phía sau giả định chúng đã xong.

| # | Việc | Làm ở đâu |
|---|---|---|
| 1 | Tạo Organization | https://github.com/organizations/plan → **Free**. Không có API nào tạo org |
| 2 | Chuyển repo vào org | `gh api -X POST repos/<user>/<repo>/transfer -f new_owner=<org>` |
| 3 | Field `Iteration` trên Project | Sau bước §4.5. Enum `ProjectV2CustomFieldType` chỉ có TEXT, NUMBER, DATE, SINGLE_SELECT |
| 4 | Thêm người vào team | `gh api -X PUT orgs/<org>/teams/<team>/memberships/<user> -f role=member` |

Repo phải để **public** nếu muốn Rulesets + CodeQL + Environment protection ở mức $0.
Xem [`17`](17-runbook-github.md) §1.2 để cân nhắc đánh đổi.

---

## 4. Trình tự dựng

### 4.1 Sửa `config.env`

```
.github/setup/config.env  →  ORG, REPO, PROJECT_TITLE, AWS_REGION
```

### 4.2 Chuẩn hoá xuống dòng — làm TRƯỚC khi commit script

`.gitattributes` phải có `* text=auto eol=lf`. Thiếu dòng này, script `.sh` được
checkout dạng CRLF trên Windows và hỏng ngay dòng đầu với `$'\r': command not found`
([S-09](#s-09)).

### 4.3 Đẩy toàn bộ `.github/` lên `main` — TRƯỚC khi khoá nhánh

```bash
git push origin main
```

> ⚠️ **Thứ tự này không đổi được.** Áp ruleset khi `main` chưa có workflow là tự khoá
> mình ra ngoài: không push thẳng được nữa, mà cũng chưa có check nào để báo cáo, nên
> PR đầu tiên treo vĩnh viễn ở *"Expected — Waiting for status to be reported"*.

### 4.4 Chạy preflight

```bash
bash .github/setup/00-preflight.sh
```

**Kết quả mong đợi** — `Preflight đạt.` Dừng lại nếu có bất kỳ dòng đỏ nào.

### 4.5 Áp toàn bộ cấu hình

```bash
bash .github/setup/apply-all.sh
```

Chạy tuần tự 01 → 07. Thứ tự có ràng buộc:

| | Script | Phụ thuộc |
|---|---|---|
| 01 | repo settings | visibility phải là public **trước**, vì ruleset và CodeQL miễn phí dựa vào đó |
| 02 | teams | phải xong **trước** ruleset — "require code owner review" cần team tồn tại thật |
| 03 | labels | phải xong **trước** khi ai đó mở issue đầu tiên |
| 04 | project | sinh ra `PROJECT_NUMBER` mà workflow cần |
| 05 | environments | phải xong **trước** ruleset |
| 06 | ruleset | từ lúc này `main` bị khoá |
| 07 | actions security | siết quyền sau khi mọi thứ đã chạy được |

Chạy lại một bước riêng: `bash .github/setup/apply-all.sh 06-ruleset.sh`

### 4.6 Tạo field `Iteration`

https://github.com/orgs/`<org>`/projects/`<số>`/settings/fields → New field → **Iteration** → chu kỳ **2 tuần**.

### 4.7 Tạo `PROJECT_TOKEN`

Fine-grained PAT tại https://github.com/settings/personal-access-tokens/new

| | |
|---|---|
| Resource owner | **phải chọn org**, không phải cá nhân |
| Organization permissions | Projects: **Read and write** |
| Repository permissions | Issues: Read · Pull requests: Read · Metadata: Read |

```bash
gh secret set PROJECT_TOKEN --repo <org>/<repo>
```

> ⚠️ Dán token **rồi mới** Enter. Bấm Enter sớm sẽ tạo secret có tên nhưng **giá trị
> rỗng**, và mọi phép kiểm tra dựa trên tên đều báo xanh trong khi board không chạy
> ([S-36](#s-36)).

Kiểm chứng bằng lần chạy thật, không bằng sự tồn tại của cái tên:

```bash
gh workflow run project-automation --repo <org>/<repo> --ref main
```

### 4.8 Thêm thành viên vào team

Tối thiểu cần **hai người** trong org, nếu không không PR nào merge được ([S-33](#s-33)).

### 4.9 Mở một PR nháp cho 7 check chạy lần đầu

GitHub chỉ nhận diện một status check **sau khi nó đã chạy ít nhất một lần**. Chưa
chạy thì required check hiển thị *"Expected — Waiting for status"* mãi mãi.

### 4.10 Đối chiếu

```bash
bash .github/setup/99-verify.sh
```

**Kết quả mong đợi** — `✓ 76/76 mục kiểm tra đạt.`

### 4.11 Tắt cửa hậu

Khi org đã có người thứ hai: `BOOTSTRAP_ADMIN_BYPASS=false` trong `config.env`, rồi:

```bash
bash .github/setup/apply-all.sh 06-ruleset.sh
```

---

## 5. Bốn quyết định thiết kế phải hiểu

Không hiểu bốn điều này thì sẽ dựng ra một hệ thống trông đúng mà không chạy.

### 5.1 Path filter đặt ở cấp job, không đặt ở `on:`

Viết `on: pull_request: paths: [...]` là cách trực giác và **sai**: khi PR không đụng
đường dẫn đó, workflow không chạy, check không báo cáo, PR treo vĩnh viễn.

→ Workflow chạy trên **mọi** PR, lọc bằng `dorny/paths-filter` ở cấp job, và kết thúc
bằng một job tổng hợp trùng tên workflow với `if: always()`. Job đó luôn báo cáo.
**Bảy tên trong `REQUIRED_CHECKS` là tên JOB, không phải tên workflow.**

### 5.2 Một check name chỉ nên do một sự kiện sinh ra

Workflow nghe cả `pull_request` lẫn `pull_request_review` sẽ tạo **hai check run cùng
tên trên cùng commit**. Lượt đỏ đầu tiên nằm lại vĩnh viễn ([S-25](#s-25)).

→ Đừng để check phụ thuộc vào trạng thái review nếu ruleset đã kiểm tra điều đó. Và
đặt `cancel-in-progress: false` cho workflow nghe nhiều sự kiện ([S-26](#s-26)).

### 5.3 CODEOWNERS đọc từ nhánh BASE

Sửa CODEOWNERS trong một PR **không** áp dụng cho chính PR đó — chỉ có hiệu lực sau
khi merge ([S-21](#s-21)).

GitHub đòi approval từ chủ sở hữu của **từng** quy tắc khớp phải. PR chạm 3 thư mục
thuộc 3 nhóm owner khác nhau cần 3 chữ ký. Muốn "một approval của tech-lead là đủ"
thì cho `@tech-lead` đồng sở hữu **mọi** dòng.

### 5.4 Kiểm tra phải dựa trên hành vi, không dựa trên sự tồn tại

Hai lần bộ đối chiếu báo xanh trong khi hệ thống hỏng:

- `PROJECT_TOKEN` **có tên** nhưng giá trị rỗng → board không chạy ([S-36](#s-36))
- `grep AWS_ACCESS_KEY_ID` khớp đúng **câu chú thích** khẳng định không có key ([S-29](#s-29))

→ Kiểm tra bằng kết quả lần chạy thật gần nhất, và loại dòng chú thích trước khi grep.

---

## 6. Bảng tra sự cố

Tra bằng chính chuỗi lỗi bạn nhìn thấy.

### Môi trường Windows

<a id="s-01"></a>**S-01 · `winget` treo không bao giờ xong**
Cần nâng quyền, nhưng hộp thoại UAC không hiện được trong phiên non-interactive.
→ Chạy PowerShell bằng **Run as Administrator**. Gói `--scope user` (như `jqlang.jq`) thì không cần.

<a id="s-02"></a>**S-02 · `winget install --id A --id B` chỉ cài một gói**
→ Mỗi lệnh một gói.

<a id="s-04"></a>**S-04 · `Python was not found; run without arguments to install from the Microsoft Store`**
`python3` là App Execution Alias, không phải Python.
→ Dùng `python`. `lib.sh` có `detect_python()` gọi thử từng ứng viên vì `command -v` không phân biệt được.

<a id="s-05"></a>**S-05 · `base64: invalid input`**
jq bản Windows mở stdout ở chế độ text nên xuất **CRLF**; chuỗi base64 dính `\r`.
→ `lib.sh` bọc `jq()` lọc `\r` **và giữ nguyên mã thoát** — bắt buộc, vì `jq -e` được dùng làm điều kiện.

<a id="s-06"></a>**S-06 · Nhãn/chuỗi so sánh luôn sai dù nhìn giống hệt**
`print()` của Python trên Windows cũng xuất CRLF → `"type: bug\r"`.
→ Dùng `python_run()` trong `lib.sh`. **Không** đặt tên hàm là `py`: nếu `PY="py -3"` thì hàm tự gọi lại chính nó.

<a id="s-07"></a>**S-07 · `UnicodeEncodeError: 'charmap' codec can't encode character`**
Console Windows mặc định cp1252.
→ Mọi script Python trong repo đã có `sys.stdout.reconfigure(encoding="utf-8")`.

<a id="s-08"></a>**S-08 · `open ~/.ssh/id_ed25519.pub: The system cannot find the path specified`**
`~` là cú pháp bash; `gh.exe` nhận chuỗi thô. Tương tự, `$env:USERPROFILE` là cú pháp PowerShell và bash không hiểu.
→ Dùng đường dẫn tuyệt đối: `C:/Users/<user>/.ssh/id_ed25519.pub` — đúng ở mọi shell.

<a id="s-09"></a>**S-09 · `$'\r': command not found` khi chạy script `.sh`**
Git checkout script dạng CRLF trên Windows.
→ `.gitattributes`: `* text=auto eol=lf` cộng `*.sh text eol=lf`. Sửa **trước** khi commit script.

### GitHub API

<a id="s-11"></a>**S-11 · `Invalid security_and_analysis payload` (HTTP 422)**
Endpoint cũ còn tồn tại nhưng không nhận nữa; GitHub đã chuyển sang code security configuration cấp org.
→ `POST /orgs/<org>/code-security/configurations/<id>/attach` với `scope=selected`.

<a id="s-12"></a>**S-12 · CODEOWNERS trỏ tới team có thật nhưng GitHub bỏ qua**
API sinh slug từ trường `name`, **không** nhận `slug` mình đặt: `"DevOps / Platform"` → `devops-platform`.
→ Đặt `name` sao cho slugify đúng bằng slug cần, rồi **đọc lại `.slug` trả về** và báo lỗi nếu lệch.
Sửa team đã lỡ tạo: `gh api -X PATCH orgs/<org>/teams/<slug-cu> -f name="Tên Mới"` — giữ nguyên thành viên.

<a id="s-13"></a>**S-13 · `"..." is not of type 'array'` / `'object'` (HTTP 422)**
`-f` và `--raw-field` của `gh` gửi mọi giá trị dưới dạng **chuỗi**.
→ Dựng body bằng `jq -n '{...}'` rồi `| gh api -X PUT <path> --input -`.
Áp dụng cho: `reviewers`, `deployment_branch_policy`, `patterns_allowed`, và **biến GraphQL kiểu mảng** (gửi nguyên `{query, variables}`).

<a id="s-15"></a>**S-15 · Quyền team đọc ra rỗng dù team có quyền**
`GET /orgs/<org>/teams/<t>/repos/<owner>/<repo>` trả **HTTP 204 không kèm thân phản hồi**.
→ Liệt kê `/teams/<t>/repos` rồi lọc theo `full_name`.

<a id="s-16"></a>**S-16 · So sánh quyền luôn sai: đặt `push`, đọc ra `write`**
Tên cũ khi ghi (`push`/`pull`), tên vai trò khi đọc (`write`/`read`).
→ Chuẩn hoá trước khi so sánh.

<a id="s-17"></a>**S-17 · Không tạo được field Iteration qua API**
Enum `ProjectV2CustomFieldType` chỉ có TEXT, NUMBER, DATE, SINGLE_SELECT.
→ Bấm tay. Projects v2 cũng không có kiểu boolean — dùng single-select `Có`/`Không`.

<a id="s-18"></a>**S-18 · `project-automation` không ghi được vào board**
`GITHUB_TOKEN` không có quyền ghi Projects v2 cấp tổ chức, và **không khoá `permissions` nào cấp được**.
→ Dùng fine-grained PAT `PROJECT_TOKEN` (§4.7).

<a id="s-20"></a>**S-20 · Gỡ reviewer khỏi PR nhưng GitHub tự thêm lại**
Khi `require_code_owner_review = true`, danh sách reviewer được **suy ra từ CODEOWNERS**, không phải danh sách sửa được. `DELETE` trả 2xx rồi GitHub dựng lại ngay.
→ Sửa CODEOWNERS (có hiệu lực từ PR sau), hoặc merge bằng bypass.

<a id="s-21"></a>**S-21 · Sửa CODEOWNERS trong PR mà PR đó không đổi**
CODEOWNERS được đọc từ nhánh **base**.
→ Merge xong mới có hiệu lực. PR đầu tiên phải dùng bypass.

<a id="s-22"></a>**S-22 · `HTTP 404` ở `user/ssh_signing_keys`, hoặc thêm khoá ký không được**
Thiếu scope `admin:ssh_signing_key`. `admin:public_key` **không** bao gồm khoá ký.
→ `gh auth refresh -h github.com -s admin:ssh_signing_key`, hoặc thêm qua web (không cần scope).

### CI và cổng chất lượng

<a id="s-24"></a>**S-24 · PR treo ở `Expected — Waiting for status to be reported`**
Hai nguyên nhân: (a) check chưa từng chạy lần nào trên repo → mở một PR nháp; (b) dùng `on.pull_request.paths` → xem §5.1.

<a id="s-25"></a>**S-25 · Check đỏ cũ không bao giờ biến mất**
Hai check run cùng tên trên cùng commit do hai sự kiện khác nhau sinh ra. Lượt đỏ đầu nằm lại trong rollup.
→ Xem §5.2.

<a id="s-26"></a>**S-26 · Check ở trạng thái `CANCELLED`, tính là không xanh**
`cancel-in-progress: true` khiến lượt sau huỷ lượt trước.
→ Đặt `false` cho workflow nghe nhiều sự kiện.

<a id="s-28"></a>**S-28 · `MSBUILD : error MSB1009: Project file does not exist`**
Bộ lọc gồm `.github/quality-gates.yml`, nên sửa ngưỡng cũng kích hoạt build trong khi `src/dotnet/` còn trống.
→ Phân biệt **ba** trạng thái, không gộp hai: *không đụng* / *đụng nhưng chưa có mã* (bỏ qua kèm `::notice`) / *có mã nhưng thiếu solution* (lỗi thật).

<a id="s-29"></a>**S-29 · Bộ đối chiếu báo có access key trong workflow, nhưng không có**
`grep` khớp đúng câu **chú thích** khẳng định rằng không có access key nào.
→ Loại dòng chú thích trước khi tìm.

<a id="s-31"></a>**S-31 · Ruleset không đặt được số approval theo đường dẫn**
Ruleset chỉ nhận **một** con số cho cả nhánh. Đặt cứng 2 cho mọi PR là sai — đội sẽ học cách duyệt cho xong.
→ Ruleset đặt 1; mức 2 cho `infra/` và `db/migrations/` do workflow `pr-governance` thực thi.

<a id="s-33"></a>**S-33 · Không merge được PR của chính mình**
Ruleset bật "require code owner review", org chỉ có một người → người đó vừa là tác giả vừa là owner duy nhất, mà GitHub cấm tự duyệt.
→ Thêm người thứ hai, hoặc dùng `BOOTSTRAP_ADMIN_BYPASS=true` (bypass **chỉ khi merge PR**, vẫn không push thẳng được).

<a id="s-34"></a>**S-34 · `Commits must have verified signatures` chặn merge**
**Squash merge KHÔNG vượt qua được luật này.** Ruleset xét chính các commit trong PR.
→ Ký commit thật (§2.4). Đây là điểm tài liệu này sửa lại một khẳng định sai lúc đầu.

<a id="s-35"></a>**S-35 · Approval biến mất sau mỗi lần push**
`dismiss_stale_reviews_on_push = true`.
→ Xin approval **sau khi** đã push xong commit cuối.

<a id="s-36"></a>**S-36 · Secret có tên nhưng giá trị rỗng**
`gh secret set` nhận bất cứ thứ gì được đưa, kể cả chuỗi rỗng. API không cho đọc lại giá trị, nên mọi phép kiểm tra dựa trên tên đều báo xanh.
→ Kiểm chứng bằng kết quả lần chạy thật gần nhất của workflow dùng secret đó.

<a id="s-37"></a>**S-37 · `No signature` ở local dù commit đã ký**
Kèm theo `error: gpg.ssh.allowedSignersFile needs to be configured`. Đây là lỗi **hiển thị ở máy**, GitHub không dùng file này.
Kiểm chứng cứng: `git cat-file commit HEAD | grep gpgsig`.
→ Tạo `allowed_signers` (§2.4).

<a id="s-38"></a>**S-38 · GitHub báo `UNKNOWN_KEY valid=false`**
Commit **có** chữ ký nhưng khoá chưa đăng ký trên tài khoản ở dạng Signing Key.
→ Đăng ký khoá. **Không cần push lại** — GitHub tính trạng thái verify tại thời điểm hiển thị, nên commit cũ tự chuyển sang Verified và approval hiện có không bị huỷ.

---

## 7. Checklist nghiệm thu

- [ ] `bash .github/setup/99-verify.sh` → `76/76 đạt`
- [ ] Field `Iteration` có trên Project
- [ ] `gh workflow run project-automation` → lượt chạy **xanh**
- [ ] Org có ≥ 2 người; `xnk-expert-team` đủ 2 chuyên gia
- [ ] Mỗi người `git log --format='%G?' -1` ra `G`
- [ ] Một PR thật đi hết luồng: issue → nhánh từ issue → 7 check xanh → approve → squash merge
- [ ] `BOOTSTRAP_ADMIN_BYPASS=false`
- [ ] OIDC: 3 IAM role + `AWS_ROLE_ARN` cho từng environment ([`infra/oidc/`](../infra/oidc/README.md))
- [ ] Bài kiểm tra nghịch E1-07: assume role từ nhánh khác `main` **phải thất bại**
- [ ] 🔴 `eval/gates.yml` → `enabled: true` sau khi E7-03 và E7-04 xong

---

## 8. Thời gian thực tế

| Việc | Lần đầu | Lần sau |
|---|---|---|
| Chuẩn bị máy | 30' | 10' |
| Bốn việc thủ công (§3) | 20' | 15' |
| `apply-all.sh` | 15' | 3' |
| Gỡ sự cố | **~4 giờ** | tra §6 |

Phần lớn thời gian lần đầu nằm ở §6. Đó là lý do tài liệu này tồn tại.
