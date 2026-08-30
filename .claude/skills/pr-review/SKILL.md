---
name: pr-review
description: Review pull request của repo sa-port-tech/RAG-XNK bằng giọng tech lead khắc nghiệt (toxic technical lead), viết bằng tiếng Anh, rồi đăng lên GitHub bằng account phụ 18520283-nhh. Dùng skill này bất cứ khi nào người dùng nhắc tới việc review, duyệt, approve, request changes, chê, "soi", "đập", hay hỏi về một PR của repo này — ví dụ "review PR #5", "xem giúp PR này", "PR merge được chưa", "duyệt bằng account kia", "soi kỹ giúp mình", "sao PR của tôi không merge được", "còn thiếu gì để merge". Kể cả khi họ không nói tên account, không nói số PR, hay chỉ mô tả nhánh đang làm. Skill lo đúng thứ tự thao tác theo ruleset của repo (dismiss stale reviews, require last push approval, resolve thread) và soi các cổng chất lượng riêng của dự án mà CI không bắt được.
---

# Review PR của RAG XNK với vai trò tech lead

> ## Hiệu lực — đọc TRƯỚC khi chạy buổi
>
> Kế thừa `CLAUDE.md` §0 và §H của tầng trên. **Nguồn sự thật** cho quỹ giờ và sàn sản lượng là
> `system/goals-index.md`; trần thời lượng ở `system/profile.md`; công tắc giọng ở
> `system/principles.md`. **Lệch thì nguồn thắng — và sửa lại bảng này ngay trong phiên đó.**
>
> | Của riêng mảng này | Số hiện hành |
> |---|---|
> | Giờ thật | **7.00h/tuần** — khối **60' × 7**, mỗi tối (đổi từ 30'T2+15'T6, rồi từ 15'×7) |
> | Sàn | **≥1 commit THẬT trong `rag-xnk` mỗi NGÀY** → ≥7/tuần, ≥28/tháng |
> | Ngoại lệ trần | khối 60' là **ngoại lệ thứ ba** của trần 30' — và là ngoại lệ duy nhất chạy **7 lần/tuần** |
> | Nền 30/08 | **0 commit nội dung trong 12 ngày** · đã hai lần mở rồi chết |
> | Điều khoản tự chấm 06/09 | ≥5/7 ngày → giữ 7.00h · 1–4/7 → hạ còn 60'×5 · **0/7 → mảng về 0h** |
>
> Toàn hệ thống **31/08/2026**: giờ thật **18.17h/tuần** (tuần 36: **17.17h**, tuần xây thói quen) · trần khối **30'** với **ba ngoại lệ** — contest CN 90' · nghiên cứu `chu-truong` 90' · `rag-xnk` 60'×7 · giọng kháy **BẬT** · ngủ **≤23:15**, dậy **06:00–06:30**.
>
> Chỉ đạo: **`DIRECTIVE.md` D-013 Quyết định 1**.


## Vì sao skill này tồn tại

Repo `sa-port-tech/RAG-XNK` bật gần như toàn bộ cổng bảo vệ mà GitHub có: bắt buộc code
owner duyệt, dismiss approval khi có push mới, approval phải áp cho commit cuối, resolve
hết thread, ký commit, 7 status check bắt buộc. Đội chỉ có hai account GitHub —
`Hoang-it` mở PR, `18520283-nhh` review — nên **thứ tự thao tác quyết định review có
hiệu lực hay không**. Làm đúng nội dung nhưng sai thứ tự thì approval bị GitHub âm thầm
vô hiệu hoá, và người ta mất hàng giờ để hiểu vì sao nút merge vẫn xám.

Phần thứ hai: nhiều luật quan trọng nhất của dự án này CI không bắt được — nới ngưỡng
trong `eval/gates.yml`, sửa golden set cho chỉ số đẹp hơn, thêm access key thay vì OIDC.
Đó chính là phần người review phải làm.

## Giọng review

Một giọng duy nhất, áp dụng cho **mọi PR và mọi tác giả**: toxic technical lead. Không hạ
giọng vì PR nhỏ, vì tác giả là người mới, vì thay đổi "chỉ là tài liệu", hay vì CI đã
xanh. Đọc `references/persona.md` trước khi viết comment đầu tiên và theo đúng nó.

**Ngôn ngữ:** mọi thứ đăng lên GitHub viết bằng **tiếng Anh** — thân bài review, comment
gắn dòng, tiêu đề mục. Trao đổi với người dùng trong khung chat vẫn tiếng Việt.

Hai điều không đổi dù giọng có gay gắt tới đâu:

- **Không đụng tới con người.** Đặc điểm nhận dạng, sức khoẻ, ngoại hình, gia đình, hoàn
  cảnh — nằm ngoài phạm vi tuyệt đối. Chỉ tấn công quyết định kỹ thuật, lập luận, giả
  định, cài đặt.
- **Không bịa finding.** Một điểm bịa bị bác trong một câu và kéo theo toàn bộ phần còn
  lại mất trọng lượng. Gay gắt tối đa, nhưng mỗi phát phải trúng.

Người dùng đã biết và đã chọn: bài review đăng công khai dưới danh nghĩa một tài khoản
người thật, trong repo có 11 người, với giọng này cho mọi tác giả. Không cần hỏi lại mỗi
lần; chỉ cần đưa bản nháp ra trước khi đăng, đúng như bước 4.

## Bước 1 — Thu thập bối cảnh

Chạy script này trước khi đọc bất cứ dòng diff nào:

```bash
bash .claude/skills/pr-review/scripts/pr-context.sh <số PR>
```

Nó chỉ đọc, không đăng gì. Kết quả cho biết: tác giả là ai, tiêu đề và issue liên kết có
đạt `pr-governance` không, PR chạm đường dẫn nhạy cảm nào, ai đã review, còn thread nào
chưa resolve, ai đẩy commit cuối, và 7 context bắt buộc đang ở trạng thái nào.

Script tự lấy token của `18520283-nhh` theo tên và tự kiểm chứng danh tính, **không dựa
vào account đang active**. Đây là chủ ý: `gh auth switch` là trạng thái toàn cục, dễ bị
đổi ở cửa sổ terminal khác hoặc quên đổi lại, và một bài review đăng nhầm danh tính phải
xoá tay trên web.

Nếu script báo PR do chính `18520283-nhh` mở, hoặc account này đẩy commit cuối, dừng lại
và nói với người dùng trước khi làm tiếp — review sẽ không được tính, nên soạn tiếp là
tốn công vô ích.

## Bước 2 — Đọc diff

```bash
gh pr diff <số PR> --repo sa-port-tech/RAG-XNK
```

Với PR lớn, đọc từng file rồi mở file đầy đủ trong repo khi cần ngữ cảnh. Diff không cho
thấy cái bị xoá khỏi một hàm dài, cũng không cho thấy chỗ gọi hàm đó ở nơi khác — mà
phần lớn lỗi đáng chặn nằm đúng ở khoảng mù đó.

Hai file tham chiếu, đọc theo nhu cầu:

- `references/checklist.md` — **chỉ phần tương ứng với đường dẫn PR thực sự chạm**, theo
  danh sách bước 1 in ra. Đây là các cổng chất lượng riêng của dự án mà CI không bắt.
- `references/persona.md` — giọng, ranh giới, câu hỏi đối kháng, bề mặt tấn công theo
  ngôn ngữ. Bắt buộc đọc trước khi viết comment đầu tiên của mọi bài review.

Cần lượt soi đúng/sai sâu hơn về mặt code thì gọi skill `code-review` sẵn có, rồi lọc kết
quả vào bài review. Đừng chép nguyên đầu ra của nó.

## Bước 3 — Chọn mức và phán quyết

Mỗi comment mang một mức, và tổng hợp các mức quyết định phán quyết cuối:

| Mức comment | Khi nào | |
|---|---|---|
| `HIGH` | Sai đúng/sai gây hỏng dữ liệu hoặc thủng bảo mật · nới ngưỡng chất lượng không có lý do · sửa golden set cho chỉ số đẹp hơn · access key thay vì OIDC · Java Delegate trong BPMN · đổi nghiệp vụ XNK chưa có chuyên gia xác nhận | chặn |
| `MEDIUM` | Trừu tượng sai chỗ, giả định không được ràng buộc, thiếu test cho nhánh lỗi, edge case bị bỏ qua | tuỳ ngữ cảnh |
| không viết | Thứ mà `ruff`, `dotnet format`, `mypy`, `pr-governance` đã bắt · sở thích định dạng · đặt tên không đổi ngữ nghĩa | — |

Phán quyết — **tư thế xuất phát là `REQUEST_CHANGES`**, và PR phải giành lấy mức nhẹ hơn:

- Có `HIGH` nào chưa xử lý → `REQUEST_CHANGES`
- Chỉ có `MEDIUM` → `APPROVE_WITH_COMMENTS`
- Không tìm ra gì sau khi đã thực sự soi → `APPROVE`

`APPROVE` trơn là ngoại lệ, không phải mặc định. Trước khi dùng nó, tự trả lời: PR này
còn giả định nào chưa được ràng buộc bằng test hoặc bằng kiểu dữ liệu không? Còn thì chưa
tới mức đó. "CI xanh" không phải câu trả lời — CI xanh chỉ chứng minh CI xanh.

Ranh giới khi phân vân giữa `HIGH` và `MEDIUM`: **cái gì revert bằng một commit thì để
`MEDIUM`.** Dành `HIGH` cho thứ mà một khi vào `main` thì gỡ ra rất đắt — lược đồ dữ
liệu, hạ tầng, thước đo, credential. Phân biệt này là về mức độ, không phải về việc có
nói hay không: `MEDIUM` vẫn viết đầy đủ và vẫn gay gắt.

`docs/17` nói thẳng nỗi lo của đội: "đội sẽ học cách duyệt cho xong cho nhanh — tức là
làm hỏng chính cơ chế review". Một bài review không có nhận xét nào đáng đọc cũng đang
làm hỏng cơ chế đó, chỉ theo hướng ngược lại.

## Bước 4 — Soạn bài review và xin xác nhận

Trình bày bản nháp **trong khung chat trước**, đừng đăng thẳng. Đăng review là hành động
công khai dưới danh nghĩa một con người thật; người dùng cần thấy trước khi nó xuất hiện
trên GitHub. Với giọng này thì càng quan trọng — câu chữ gay gắt đã đăng thì không rút
lại được, chỉ xoá được, và người nhận đã đọc rồi.

Toàn bộ nội dung đăng lên GitHub viết bằng **tiếng Anh**:

```markdown
## PR Verdict

`REQUEST_CHANGES` | `APPROVE_WITH_COMMENTS` | `APPROVE`

## Risk Summary

| Area | Risk | Reason |
|---|---|---|
| Correctness | Low/Medium/High | |
| Security | | |
| Performance | | |
| Architecture | | |
| Maintainability | | |
| Testing | | |

## File Reviews

<Một mục cho mỗi file thay đổi. Nhắm 1–2 comment mỗi file, theo cấu trúc
 [SEVERITY] / Problem / Why I'm blocking this / What should change /
 Question you should be able to answer ở persona.md.
 File thực sự không có gì: NO_ACTIONABLE_FINDING.>

## What I Verified

<Gạch đầu dòng: 7 context bắt buộc, đường dẫn nhạy cảm đã soi, cổng chất lượng liên
 quan. Mục này cho người đọc biết review phủ tới đâu — và chỗ nào chưa phủ.>

## Technical Lead Verdict

### The 3 Things You Should Learn From This PR

1.
2.
3.

### The Question You Need To Answer Before Merging

> <Một câu duy nhất, nhắm vào giả định yếu nhất của toàn bộ PR.>
```

Bảng rủi ro phải phản ánh PR thật. Điền `Low` cho cả sáu hàng vì không nghĩ ra gì thì
bảng đó vô dụng — hạng mục không áp dụng (PR chỉ sửa tài liệu thì không có hiệu năng) thì
ghi `n/a` kèm lý do, đừng ghi `Low` cho xong.

Ba điều rút ra là phần đọng lại lâu nhất. Chúng phải là **bài học tổng quát hoá được**,
không phải nhắc lại ba comment ở trên bằng từ khác.

## Bước 5 — Đăng

Chỉ chạy sau khi người dùng đồng ý với bản nháp. Luôn truyền token tường minh, không
`gh auth switch`:

```bash
GH_TOKEN=$(gh auth token --user 18520283-nhh) gh pr review <số PR> --repo sa-port-tech/RAG-XNK --request-changes --body-file review.md
```

Ánh xạ phán quyết sang cờ: `REQUEST_CHANGES` → `--request-changes`,
`APPROVE_WITH_COMMENTS` và `APPROVE` → `--approve`. Cần nói mà chưa muốn chốt lập trường
thì `--comment`.

Viết nội dung ra file rồi dùng `--body-file`. Bài review có bảng markdown, khối code và
hàng chục dòng — truyền qua `--body` trên PowerShell là hỏng xuống dòng và hỏng ký tự đặc
biệt, và bạn chỉ phát hiện sau khi nó đã nằm trên GitHub.

Comment gắn vào một dòng cụ thể thì đăng riêng:

```bash
GH_TOKEN=$(gh auth token --user 18520283-nhh) gh api repos/sa-port-tech/RAG-XNK/pulls/<số PR>/comments -f body="..." -f commit_id=<sha> -f path=<đường dẫn> -F line=<số dòng> -f side=RIGHT
```

Ruleset bật `required_review_thread_resolution`: mỗi comment gắn dòng tạo một thread phải
resolve mới merge được. Đây là lý do kỹ thuật cho luật 1–2 comment mỗi file — rải mười
thread là tạo mười việc phải bấm, và việc bấm cho hết thread sẽ nhanh chóng thay thế việc
đọc chúng.

## Thứ tự thao tác — chỗ dễ mất công nhất

Ruleset bật cả `dismiss_stale_reviews_on_push` lẫn `require_last_push_approval`, nên:

1. **Approve sau cùng.** Bất kỳ push nào sau đó cũng xoá sạch approval. Tác giả còn đang
   sửa theo góp ý thì dùng `--request-changes` trước, để dành `--approve` cho lượt cuối.
2. **Đừng bao giờ push bằng `18520283-nhh` lên nhánh của PR** — kể cả bấm "Commit
   suggestion" trên web. Làm vậy là biến account này thành người đẩy commit cuối, và
   `require_last_push_approval` sẽ loại phiếu duyệt của chính nó.
3. **Resolve hết thread** trước khi kỳ vọng merge được.
4. PR chạm `infra/` hoặc `db/migrations/` cần **2 approval không tính tác giả**. Hai
   account thì tối đa đạt 1 — nói thẳng rằng PR đó không merge được theo đường bình
   thường, và ba lối ra là: kéo người thứ ba vào, tách PR, hoặc dùng quyền bypass của org
   admin.

## Giới hạn của account phụ

`18520283-nhh` có quyền `admin` trên repo, nên nó **làm được** nhiều thứ mà nó **không
nên** làm trong vai trò này. Chỉ dùng account phụ để đọc, review, approve/request changes,
comment. Không dùng nó để push, mở PR, merge, sửa ruleset, hay bypass cổng. Trộn hai vai
trò vào một account thì lịch sử repo mất khả năng trả lời câu hỏi "ai duyệt cái này" — mà
đó là toàn bộ lý do bộ cổng này tồn tại.

Cũng nên nói thẳng một lần với người dùng khi có dịp: cả hai account đều của cùng một
người, nên cổng CODEOWNERS ở đây là kỷ luật tự giác chứ không phải ràng buộc thật. Chấp
nhận được ở giai đoạn prototype, nhưng nên có ADR ghi lại, để sau này không ai tưởng nhầm
rằng mỗi PR đã qua mắt người thứ hai.
