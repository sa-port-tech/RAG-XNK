#!/usr/bin/env python3
"""Sinh cấu hình nginx cho môi trường local từ `.github/services.json`.

Vì sao tồn tại
--------------
ADR-013 chốt: **ALB định tuyến theo tiền tố nhưng KHÔNG cắt tiền tố** trước khi chuyển
tiếp. Vì vậy `Xnk.Corpus` gọi `UsePathBase("/corpus")` và `retrieval` gắn thẳng
`prefix="/retrieval"` vào router.

Nếu ở máy dev mỗi service một cổng và gọi thẳng, hành vi tiền tố đó **không bao giờ được
kiểm chứng ở local** — sai lệch chỉ lộ ra sau khi deploy. Container nginx dựng từ file này
tái hiện đúng hành vi của ALB, nên `smoke_test.sh` chạy được ở local với cùng một
`BASE_URL` như trên môi trường thật.

Vì sao sinh lúc container khởi động, không commit file sinh ra
--------------------------------------------------------------
ADR-009: `.github/services.json` là nguồn sự thật duy nhất cho cấu trúc monorepo. Nếu
`nginx.conf` là file commit tay thì thêm một service mà quên sửa proxy là chuyện sẽ xảy ra,
và nó chỉ lộ ra khi có người thứ hai gõ đúng URL đó. Sinh lại ở mỗi lần `up` thì lệch là
điều bất khả thi — không cần thêm một cổng CI để canh một file lẽ ra không nên tồn tại.

Dùng
----
    python tools/local/render_nginx.py --services .github/services.json --output nginx.conf

Trong `docker-compose.yml`, service `nginx-conf` chạy đúng lệnh này rồi mới tới lượt nginx.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

# Console Windows mặc định cp1252. Ép UTF-8 để thông báo tiếng Việt không làm script chết
# giữa chừng — cùng lý do đã ghi ở tools/corpus/kiem_tra.py.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

# Cổng nginx nghe ở local. Trùng với ánh xạ cổng trong docker-compose.yml và với BASE_URL
# mà README hướng dẫn truyền cho smoke_test.sh.
CONG_NGINX = 8080

# DNS nội bộ của Docker. Cần khai tường minh vì upstream được đặt qua biến (xem chú thích
# ở _khoi_location) — nginx chỉ tra tên lúc chạy khi có resolver.
DNS_DOCKER = "127.0.0.11"

# Thư mục nginx phục vụ nội dung tĩnh của Blazor WASM. Volume tương ứng rỗng cho tới khi
# story E1-11 dựng xong Xnk.Web; khi đó `/` trả 404 — đúng sự thật, không giả vờ có trang.
GOC_WEB = "/usr/share/nginx/html"


def _doc_manifest(duong_dan: Path) -> dict[str, Any]:
    """Đọc danh mục service, báo lỗi rõ ràng thay vì ném traceback."""
    if not duong_dan.is_file():
        sys.exit(f"✗ Không tìm thấy danh mục service: {duong_dan}")

    try:
        noi_dung: dict[str, Any] = json.loads(duong_dan.read_text(encoding="utf-8"))
    except json.JSONDecodeError as loi:
        sys.exit(f"✗ {duong_dan} không phải JSON hợp lệ: {loi}")

    if not noi_dung.get("services"):
        sys.exit(f"✗ {duong_dan} không có mục 'services' nào.")

    return noi_dung


def _khoi_location(ten: str, cong: int) -> str:
    """Sinh hai khối location cho một service.

    Hai khối chứ không phải một: ``location /corpus/`` không khớp ``/corpus`` (không có
    dấu gạch cuối), và dùng ``location /corpus`` một mình thì nó khớp luôn cả
    ``/corpusgi-do``. Cặp ``= /corpus`` + ``/corpus/`` khớp đúng những gì cần khớp.

    Hai điểm cốt tử trong thân khối:

    * ``$request_uri`` ở cuối ``proxy_pass`` — **giữ nguyên tiền tố**, đúng hành vi ALB
      mà ADR-013 mô tả. Bỏ nó đi là biến local thành một môi trường khác với thật.
    * Upstream đặt qua biến ``$upstream_*``. Nếu viết thẳng tên host, nginx tra DNS **lúc
      nạp cấu hình** và **không khởi động được** khi service đó chưa dựng — mà ở giai đoạn
      này bốn service vẫn còn là thư mục trống. Qua biến thì nginx tra lúc có request, và
      service thiếu trả **502** — một thất bại nói đúng sự thật, thay vì cả cổng vào chết.
    """
    bien = f"$upstream_{ten.replace('-', '_')}"
    than = (
        f"        set {bien} {ten};\n"
        f"        proxy_pass http://{bien}:{cong}$request_uri;\n"
        "        proxy_http_version 1.1;\n"
        "        proxy_set_header Host $host;\n"
        "        proxy_set_header X-Real-IP $remote_addr;\n"
        "        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n"
        "        proxy_set_header X-Forwarded-Proto $scheme;\n"
        # Thời gian chờ dài hơn mặc định: buổi hỏi–đáp đi qua LLM chạy trên máy dev có thể
        # mất hàng chục giây. 60s đủ rộng mà vẫn cắt được kết nối treo thật.
        "        proxy_read_timeout 60s;\n"
    )
    return (
        f"    # {ten} → http://{ten}:{cong} (giữ nguyên tiền tố /{ten})\n"
        f"    location = /{ten} {{\n{than}    }}\n\n"
        f"    location /{ten}/ {{\n{than}    }}\n"
    )


def sinh_cau_hinh(manifest: dict[str, Any]) -> str:
    """Dựng toàn bộ nội dung file cấu hình từ danh mục service."""
    khoi: list[str] = []

    for service in manifest["services"]:
        ten = service["name"]
        cong = service.get("container_port")
        if not cong:
            sys.exit(f"✗ Service '{ten}' thiếu 'container_port' trong danh mục.")
        khoi.append(_khoi_location(ten, int(cong)))

    phan_service = "\n".join(khoi)

    # Blazor WASM là ứng dụng một trang: mọi đường dẫn không phải file tĩnh đều trả về
    # index.html để bộ định tuyến phía client xử lý. Khối này đứng CUỐI vì `location /`
    # là tiền tố khớp yếu nhất, không cướp request của các khối service phía trên.
    phan_web = (
        "    # web (Blazor WASM). Thư mục rỗng cho tới khi Xnk.Web được dựng — khi đó\n"
        "    # nginx trả 404, không phải trang giả.\n"
        "    location / {\n"
        f"        root {GOC_WEB};\n"
        "        try_files $uri $uri/ /index.html;\n"
        "    }\n"
    )

    return (
        "# ⚠️ FILE SINH TỰ ĐỘNG — ĐỪNG SỬA TAY, ĐỪNG COMMIT.\n"
        "#\n"
        "# Nguồn: .github/services.json (ADR-009).\n"
        "# Sinh bởi: tools/local/render_nginx.py, chạy trong service `nginx-conf` mỗi lần\n"
        "# `docker compose up`. Muốn đổi định tuyến thì sửa danh mục service, không sửa đây.\n"
        "\n"
        f"resolver {DNS_DOCKER} valid=10s ipv6=off;\n"
        "\n"
        "server {\n"
        f"    listen {CONG_NGINX};\n"
        "    server_name _;\n"
        "\n"
        "    # Corpus văn bản có thể lên tới hàng chục MB mỗi tệp khi ingestion tải lên.\n"
        "    client_max_body_size 64m;\n"
        "\n"
        f"{phan_service}\n"
        f"{phan_web}"
        "}\n"
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument(
        "--services",
        type=Path,
        default=Path(".github/services.json"),
        help="Đường dẫn tới danh mục service (mặc định: .github/services.json)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        required=True,
        help="Nơi ghi file cấu hình nginx",
    )
    tham_so = parser.parse_args()

    manifest = _doc_manifest(tham_so.services)
    cau_hinh = sinh_cau_hinh(manifest)

    tham_so.output.parent.mkdir(parents=True, exist_ok=True)
    # newline="\n" tường minh: script này chạy được cả trên Windows lúc gỡ lỗi, mà nginx
    # đọc file có CRLF thì báo lỗi cú pháp ở một dòng trông hoàn toàn bình thường.
    tham_so.output.write_text(cau_hinh, encoding="utf-8", newline="\n")

    ten_service = ", ".join(s["name"] for s in manifest["services"])
    print(f"✓ Đã sinh {tham_so.output} cho {len(manifest['services'])} service: {ten_service}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
