"""Test cấu trúc: mọi route ngoài allowlist công khai phải đi qua ``tenant_hien_tai``.

`retrieval/main.py` tuyên bố rằng `router_can_xac_thuc` bảo vệ **mọi** route gắn vào nó —
"kể cả route mà người viết quên nghĩ tới chuyện xác thực". Một tuyên bố như thế mà không có
test thì vẫn là thói quen, chỉ là thói quen viết bằng chữ to hơn. Các test dưới đây biến nó
thành một điều kiện máy kiểm: ai gắn route mới vào nhầm router thì đỏ ngay tại đây, không đợi
review nhớ ra.

Không cần database: ``create_app`` chỉ đăng ký route, engine mở trong ``lifespan`` — và
lifespan không chạy khi không có client nào gửi request. Vì vậy test dựng ``Settings`` giả
thay cho fixture ``settings`` (fixture đó kéo theo container PostgreSQL).
"""

from __future__ import annotations

from typing import Final

from fastapi import APIRouter, FastAPI
from fastapi.routing import APIRoute, RouteContext, iter_route_contexts

from retrieval.config import Settings
from retrieval.main import PATH_PREFIX, create_app, router_can_xac_thuc, tenant_hien_tai

# Ghi cứng, KHÔNG suy từ `router_cong_khai.routes`: lấy allowlist từ chính code đang bị kiểm
# thì một route gắn nhầm vào router công khai sẽ tự động được "cho phép" — test đồng ý với
# đúng cái lỗi nó sinh ra để bắt. Thêm route công khai thì phải sửa dòng này, có ý thức.
ROUTE_CONG_KHAI: Final = frozenset(
    {
        f"{PATH_PREFIX}/health/live",
        f"{PATH_PREFIX}/health/ready",
    }
)


def _app_khong_can_db() -> FastAPI:
    return create_app(
        Settings(
            # Cổng 1 không có gì lắng nghe — và cũng không ai kết nối tới, vì lifespan không chạy.
            database_url="postgresql+asyncpg://khong:co@127.0.0.1:1/khong-co",
            jwt_issuer="https://identity.test.local",
            jwt_audience="xnk-api-test",
            jwt_signing_key="khoa-gia-chi-de-dung-app-dai-32-ky-tu",
        )
    )


def _api_route(app: FastAPI) -> list[RouteContext]:
    """Mọi API route **sau khi đã gắn vào app**, kèm tiền tố và dependency hiệu lực.

    ⚠️ Không lặp thẳng ``app.routes``. Từ FastAPI 0.14x, ``include_router`` không còn chép
    từng ``APIRoute`` vào app mà thêm **một** phần tử ``_IncludedRouter`` bọc cả router; lặp
    ``app.routes`` rồi lọc ``isinstance(r, APIRoute)`` sẽ ra danh sách RỖNG — và test chính
    xanh vì không có gì để soi. ``iter_route_contexts`` là đường công khai mà chính bộ sinh
    OpenAPI của FastAPI dùng để duyệt route đã phẳng hoá.

    Lọc theo ``original_route`` là ``APIRoute``: ``/docs`` và ``/openapi.json`` là ``Route``
    thường của Starlette, không mang dependency nào.
    """
    return [c for c in iter_route_contexts(app.routes) if isinstance(c.original_route, APIRoute)]


def route_thieu_xac_thuc(app: FastAPI) -> list[str]:
    """Route ngoài allowlist mà không đi qua ``tenant_hien_tai``, dạng ``"GET /retrieval/x"``.

    Đọc ``dependant`` của **ngữ cảnh hiệu lực**, không của ``APIRoute`` gốc: ngữ cảnh đó gộp
    cả dependency khai ở ``include_router(..., dependencies=...)``, nên test không báo sai khi
    ai đó chuyển lớp bảo vệ lên cấp app.

    Soi **một tầng** ``dependant.dependencies`` là đủ và là đúng: FastAPI chèn dependency của
    router vào đầu danh sách đó cho từng route. Soi đệ quy sẽ cho qua một route chỉ lấy tenant
    gián tiếp qua sub-dependency — trong khi ``tenant_hien_tai`` phải là **cách duy nhất** một
    handler biết tenant.
    """
    vi_pham: list[str] = []
    for route in _api_route(app):
        if route.path in ROUTE_CONG_KHAI:
            continue
        if tenant_hien_tai not in {d.call for d in route.dependant.dependencies}:
            vi_pham.extend(f"{m} {route.path}" for m in sorted(route.methods or ()))
    return vi_pham


def test_moi_route_ngoai_allowlist_deu_qua_tenant_hien_tai() -> None:
    vi_pham = route_thieu_xac_thuc(_app_khong_can_db())

    assert vi_pham == [], (
        "Route không qua tenant_hien_tai — gắn vào `router_can_xac_thuc`, hoặc nếu thật sự "
        f"công khai thì thêm vào ROUTE_CONG_KHAI có ý thức: {vi_pham}"
    )


def test_quet_thay_it_nhat_hai_route_can_xac_thuc() -> None:
    """Chặn trường hợp quét ra rỗng: danh sách vi phạm rỗng vì không có gì để soi cũng xanh."""
    can_xac_thuc = [r for r in _api_route(_app_khong_can_db()) if r.path not in ROUTE_CONG_KHAI]

    assert len(can_xac_thuc) >= 2, [r.path for r in can_xac_thuc]


def test_allowlist_khop_dung_route_dang_co() -> None:
    """Path trong allowlist phải tồn tại thật — đổi tên route thì allowlist không được mục."""
    dang_co = {r.path for r in _api_route(_app_khong_can_db()) if r.path is not None}

    assert dang_co >= ROUTE_CONG_KHAI, ROUTE_CONG_KHAI - dang_co


def test_bat_duoc_route_gan_nham_router() -> None:
    """Test âm: chứng minh bộ kiểm ĐỎ được, không phải xanh vì lý do sai.

    Dựng app riêng thay vì thêm route vào router thật — router là biến cấp module, sửa nó ở
    đây là rò trạng thái sang mọi test khác trong cùng lượt chạy.
    """
    router_quen = APIRouter(prefix=PATH_PREFIX)

    @router_quen.get("/bi-mat")
    def bi_mat() -> dict[str, str]:
        return {"lo": "ra"}

    app = FastAPI()
    app.include_router(router_quen)
    app.include_router(router_can_xac_thuc)

    assert route_thieu_xac_thuc(app) == [f"GET {PATH_PREFIX}/bi-mat"]
