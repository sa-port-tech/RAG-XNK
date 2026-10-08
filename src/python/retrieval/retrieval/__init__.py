"""Service retrieval — tra cứu văn bản theo tenant, chưa phải tìm kiếm ngữ nghĩa.

⚠️ Docstring này từng ghi "hybrid search với bộ lọc hiệu lực ở tầng SQL", và nửa sai thứ
hai nguy hiểm hơn nửa sai thứ nhất.

* **Chưa có hybrid search.** Truy vấn duy nhất là ``SELECT … ORDER BY "DocumentNumber",
  "Id" LIMIT … OFFSET …`` (xem ``db.py``). Không embedding, không vector, không BM25,
  không rerank. Đó là phạm vi epic E3.
* **Chưa có bộ lọc hiệu lực.** ``EffectiveFrom``/``EffectiveTo`` chỉ nằm trong danh sách
  SELECT, **không có trong mệnh đề WHERE** — và ``docs/00`` §10.3 gọi lọc hiệu lực là
  phòng tuyến quan trọng nhất của cả sản phẩm. Viết rằng nó "ở tầng SQL" trong khi nó chưa
  tồn tại là cách chắc chắn nhất để không ai đi xây nó.

Thứ ĐANG ở tầng SQL là bộ lọc **tenant** (``DIEU_KIEN_TENANT``), chốt ở ADR-012 — một
phòng tuyến khác, và đừng nhầm hai cái với nhau.
"""
