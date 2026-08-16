# Golden set

> CODEOWNERS: `@sa-port-tech/xnk-expert-team` — **chỉ chuyên gia XNK được duyệt thay đổi**
> Phương pháp: [`docs/14`](../../docs/14-phuong-phap-golden-set.md)

Đây là **thước đo** của cả dự án. Mọi kết luận về chất lượng hệ thống đều quy về các
con số đo trên tập câu hỏi ở đây.

## Cấu trúc

```
cau-hoi/            câu hỏi + đáp án chuẩn + căn cứ pháp lý
gay-tranh-cai/      câu bẫy — tối thiểu 10 câu, đủ 5 dạng
baseline.json       chỉ số baseline trên main — DO CI SINH, KHÔNG SỬA TAY
```

## Ba luật

1. **Không sửa golden set để chỉ số đẹp hơn.** [`docs/14`](../../docs/14-phuong-phap-golden-set.md)
   gọi thẳng tên việc này: gian lận với chính mình. CODEOWNERS + lịch sử git tồn tại để
   phòng đúng điều đó.

2. **Thêm câu mới thì baseline cũ hết hiệu lực.** So sánh chỉ số qua hai phiên bản
   thước đo khác nhau là một sai lầm im lặng. `eval_gate.py` phát hiện bằng vân tay
   nội dung thư mục này và tự bỏ so sánh, chỉ giữ lại các ngưỡng tuyệt đối.

3. **`baseline.json` do CI ghi.** Muốn baseline tốt hơn thì sửa hệ thống rồi merge —
   đó là cách duy nhất hợp lệ.

## Ngưỡng

Xem [`eval/gates.yml`](../gates.yml). Chặn merge tuyệt đối: `Stale Citation Rate = 0`.
