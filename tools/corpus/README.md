# tools/corpus — thu thập & kiểm tra văn bản lõi

Hai script phục vụ story `E2-03` (tải 15 văn bản, lưu S3 kèm `sha256`, ghi metadata) và
checklist gỡ chặn Sprint 1 của [`docs/04`](../../docs/04-danh-muc-van-ban-loi.md) §7.

Đặt ở `tools/` chứ không ở `src/python/` là có chủ đích: đây là công cụ vận hành thủ công
của Sprint 0–1, không phải mã chạy trong service. `src/python/` chịu cổng ruff + mypy +
coverage của `ci-python.yml` theo `.github/services.json`; nhét công cụ một lần vào đó chỉ
làm nhiễu ma trận build mà không đổi lấy gì.

---

## `kiem_tra.py` — checklist docs/04 §7 chạy được

```bash
python tools/corpus/kiem_tra.py
python tools/corpus/kiem_tra.py --chi-tiet
```

Đối chiếu ba file registry với tám hạng mục của [`docs/04`](../../docs/04-danh-muc-van-ban-loi.md) §7,
cộng một lượt kiểm tra toàn vẹn cấu trúc. Mã thoát `0` = đủ điều kiện, `1` = còn thiếu.

Câu nó trả lời: **Sprint 1 đã có căn cứ để bắt đầu chưa?** — biến một dòng trong tài liệu
thành dữ kiện kiểm chứng được, thay vì một câu trả lời cảm tính trong standup.

Kiểm tra toàn vẹn gồm: `ma_van_ban` khớp quy ước sinh từ `so_hieu` · không trùng
`ma_van_ban` · `trang_thai` chỉ nhận giá trị trong enum [`docs/00`](../../docs/00-ke-hoach-tong-the.md) §9.4.

> Hạng mục ⑧ (chuyên gia ký xác nhận) **luôn báo chưa đạt**. Chữ ký là hành vi của con
> người, không tự động hoá được — script giữ nó hiện diện để không ai quên rằng checklist
> chưa đóng.

---

## `thu_thap.py` — tải file gốc, tính sha256

```bash
python tools/corpus/thu_thap.py                        # dry-run, xem sẽ tải gì
python tools/corpus/thu_thap.py --thuc-thi             # tải thật
python tools/corpus/thu_thap.py --ma 38-2015-TT-BTC --thuc-thi
```

**Mặc định là dry-run.** Tải thật phải truyền `--thuc-thi`.

### Bốn nghĩa vụ được cài vào code, không để người chạy tự nhớ

| Nghĩa vụ | Nguồn | Cài đặt |
|---|---|---|
| Tôn trọng `robots.txt` | [`docs/05`](../../docs/05-ra-soat-ban-quyen.md) §4 | `urllib.robotparser`, cache theo domain. **Đọc không được thì coi như CẤM** |
| ≤ 1 request / 2 giây / domain | [`docs/00`](../../docs/00-ke-hoach-tong-the.md) §9.1 | Hàng đợi theo domain, chặn ở tầng gọi |
| User-Agent định danh + email | [`docs/00`](../../docs/00-ke-hoach-tong-the.md) §9.1 | Đọc từ `XNK_CRAWL_EMAIL`; **script từ chối chạy nếu chưa đặt** |
| File gốc bất biến + `sha256` | [`docs/00`](../../docs/00-ke-hoach-tong-the.md) §9.1 | Ghi `tai-ve.lock.yaml`, tải lại thì so hash |

```bash
export XNK_CRAWL_EMAIL='lien-he@to-chuc.vn'
```

### Hai hành vi đáng chú ý

**① Từ chối tải bản ghi chưa được chuyên gia xác minh.** Chỉ `trang_thai_thu_thap` từ
`da_xac_minh` trở lên mới được tải. Bản ghi `de_xuat` bị bỏ qua kèm lý do. Đây không phải
sự bất tiện — nó là hàng rào chống đúng rủi ro mà [`docs/04`](../../docs/04-danh-muc-van-ban-loi.md) §5
gọi tên: ingest một số hiệu chưa ai kiểm.

**② Phát hiện file nguồn đổi nội dung.** Nếu file đã tải và hash khác với hash đã ghi trong
lock, script **không ghi đè** — nó dừng lại và báo cả hai hash, thoát với mã `2`.

Với văn bản pháp luật, nội dung đổi ở cùng một URL là sự kiện cần điều tra chứ không phải
cần đồng bộ: hoặc nguồn đã đính chính, hoặc ta đang tải nhầm bản. Cả hai đều phải có người
xem trước khi corpus thay đổi.

---

## Vì sao có `tai-ve.lock.yaml` riêng thay vì ghi ngược vào registry

`van-ban.yaml` là tài liệu **do người viết**: nó dày comment giải thích vì sao mỗi trường
để trống, ai được điền, và căn cứ ở đâu. Cho script `yaml.safe_dump()` ghi đè lên file đó
sẽ xoá sạch phần comment ngay lần chạy đầu tiên — mất đúng phần có giá trị nhất.

Nên tách vai: registry do người sở hữu, lock file do máy sinh. `kiem_tra.py` đối chiếu chéo
hai bên. Cả hai đều nằm trong git vì cả hai đều là **bằng chứng truy vết**, không phải nội
dung corpus.

---

## Yêu cầu

Python ≥ 3.10 và `PyYAML`. Không phụ thuộc gì thêm — chủ ý, để chạy được trên máy chuyên
gia và máy Data Engineer mà không cần dựng môi trường của dự án.
