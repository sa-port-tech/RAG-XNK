# ADR-008 — Nội dung corpus không nằm trong git, metadata thì có

**Trạng thái:** chấp nhận · quyết định gốc trong [`docs/00`](../00-ke-hoach-tong-the.md) §8.1
và §18 · ghi thành ADR 2026-09-09

> Đã thi hành: `.gitignore` loại `corpus/raw/` và `corpus/derived/`, còn `corpus/registry/`
> được commit. ADR viết muộn — xem ghi chú ở ADR-001.

## Bối cảnh

Hệ thống trả lời câu hỏi nghiệp vụ bằng cách trích dẫn văn bản pháp luật. Muốn vậy phải có
một kho văn bản. Câu hỏi là kho đó sống ở đâu.

Ba sức ép kéo về ba hướng khác nhau:

- **Bản quyền.** [`docs/05`](../05-ra-soat-ban-quyen.md) rà soát quyền sử dụng từng nguồn.
  Một repo git được nhân bản đầy đủ về máy của mọi người đã clone nó, kể cả sau khi xoá file.
- **Kích thước và lịch sử.** Văn bản PDF lớn, và git giữ **mọi phiên bản** đã từng commit.
  Một corpus vài GB làm mọi lần clone trở nên đắt, vĩnh viễn.
- **Truy vết.** `docs/04` đòi biết văn bản nào đã lấy, từ nguồn nào, hash bao nhiêu, ai xác
  minh — và những dữ kiện đó **phải** đi qua review chứ không được đổi lặng lẽ.

## Quyết định

**Tách theo bản chất dữ liệu, không theo thư mục:**

| Thư mục | Trong git? | Nội dung |
|---|---|---|
| `corpus/registry/` | ✅ **có** | Chỉ metadata: số hiệu, ngày, nguồn, `sha256`, trạng thái xác minh |
| `corpus/raw/` | ❌ không | File gốc bất biến đã tải về |
| `corpus/derived/` | ❌ không | Text đã trích xuất, kết quả phân rã |

Nội dung văn bản đi qua quy trình **P1** và sống trong RDS/S3. Git chỉ chứa **code, prompt,
BPMN và golden set**.

## Hệ quả

- Metadata trong git là **thước đo và bằng chứng truy vết**: đổi một dòng trong
  `registry/van-ban.yaml` là một PR có người duyệt, không phải một lần sửa file lặng lẽ.
- `sha256` trong registry là dây nối giữa bản ghi trong git và file nằm ngoài git. Mất dây đó
  thì registry chỉ còn là một danh sách tên.
- **Clone repo xong chưa chạy được luồng nghiệp vụ thật** — phải chạy `tools/corpus/thu_thap.py`
  để tải nội dung. Đây là cái giá có chủ đích, và là lý do seed local chỉ mang **metadata**
  chứ không mang nội dung văn bản.
- S3 giữ file gốc với **Versioning + Object Lock** (`docs/00` §11): khi có tranh chấp về nội
  dung một văn bản, bằng chứng gốc phải là bất biến — thứ mà git, nơi lịch sử có thể bị viết
  lại, không bảo đảm được.

## Phương án đã cân nhắc và loại bỏ

**Commit cả nội dung corpus.** Đơn giản nhất: clone xong là có đủ. Loại bỏ vì bản quyền
(`docs/05`), vì kích thước lịch sử git không bao giờ nhỏ lại, và vì git không cho phép khoá
bất biến một bản ghi.

**Git LFS.** Giải quyết được kích thước. Loại bỏ vì không giải quyết bản quyền hay tính bất
biến, mà lại thêm một hạ tầng phải trả tiền và phải vận hành, cho đúng một loại dữ liệu vốn đã
có chỗ ở đúng hơn là S3.

**Không giữ registry trong git, để tất cả trong database.** Nhất quán hơn về mặt lưu trữ. Loại
bỏ vì khi ấy "văn bản nào đã được xác minh" trở thành một hàng trong bảng mà bất kỳ ai có
quyền ghi cũng đổi được, không để lại người duyệt — đúng thứ mà `docs/04` cần chống.
