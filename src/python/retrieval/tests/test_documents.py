"""API văn bản, kiểm qua HTTP với token của hai tenant khác nhau.

Đây là bản đối ứng phía Python của ``DocumentsApiTests`` bên .NET. Cùng một điều cần chứng
minh: bộ lọc tenant **nằm trong câu SQL** và còn nguyên tác dụng sau khi đi qua cả chồng
HTTP — token → kiểm chữ ký → claim ``tenant_id`` → mệnh đề WHERE.
"""

from __future__ import annotations

import datetime as dt
import uuid
from typing import Any

import jwt
import pytest
from fastapi.testclient import TestClient

from retrieval import db
from retrieval.config import Settings
from retrieval.main import create_app, router_can_xac_thuc
from tests.conftest import AUDIENCE_TEST, ISSUER_TEST, KHOA_KY_TEST


def _token(tenant_id: uuid.UUID | None, **ghi_de: Any) -> str:
    """Ký một token hợp lệ cho test.

    Ký tại chỗ chứ không gọi sang identity-tenant: bộ test của retrieval phải trả lời
    "retrieval xử lý đúng chưa khi nhận một token hợp lệ", không kéo theo một service khác.
    Điều này làm được là nhờ khoá đối xứng — cái giá và hạn chuyển ghi ở ADR-014.
    """
    bay_gio = dt.datetime.now(dt.UTC)
    claims: dict[str, Any] = {
        "iss": ISSUER_TEST,
        "aud": AUDIENCE_TEST,
        "sub": str(uuid.uuid4()),
        "iat": bay_gio,
        "exp": bay_gio + dt.timedelta(minutes=10),
        "tenant_type": "b2b_khach_hang",
        "role": "user",
    }
    if tenant_id is not None:
        claims["tenant_id"] = str(tenant_id)
    claims.update(ghi_de)

    return jwt.encode(claims, KHOA_KY_TEST, algorithm="HS256")


def _so_hieu(client: TestClient, token: str) -> list[str]:
    phan_hoi = client.get(
        "/retrieval/documents",
        params={"page_size": 100},
        headers={"Authorization": f"Bearer {token}"},
    )
    assert phan_hoi.status_code == 200, phan_hoi.text
    return [m["document_number"] for m in phan_hoi.json()["items"]]


def test_khong_co_token_thi_401(settings: Settings) -> None:
    with TestClient(create_app(settings)) as client:
        assert client.get("/retrieval/documents").status_code == 401


def test_token_ky_bang_khoa_khac_thi_401(settings: Settings) -> None:
    gia = jwt.encode(
        {
            "iss": ISSUER_TEST,
            "aud": AUDIENCE_TEST,
            "exp": dt.datetime.now(dt.UTC) + dt.timedelta(minutes=10),
            "tenant_id": str(uuid.uuid4()),
        },
        "mot-khoa-hoan-toan-khac-cung-dai-32-ky-tu",
        algorithm="HS256",
    )

    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get("/retrieval/documents", headers={"Authorization": f"Bearer {gia}"})

    assert phan_hoi.status_code == 401


def test_token_het_han_thi_401(settings: Settings) -> None:
    het_han = _token(
        uuid.uuid4(),
        exp=dt.datetime.now(dt.UTC) - dt.timedelta(hours=1),
    )

    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get(
            "/retrieval/documents", headers={"Authorization": f"Bearer {het_han}"}
        )

    assert phan_hoi.status_code == 401


@pytest.mark.usefixtures("engine")
def test_moi_tenant_chi_thay_van_ban_dung_chung_va_cua_chinh_minh(
    settings: Settings,
    tenant_co_du_lieu: tuple[uuid.UUID, uuid.UUID, str, str, str],
) -> None:
    tenant_a, tenant_b, so_chung, so_a, so_b = tenant_co_du_lieu

    with TestClient(create_app(settings)) as client:
        cua_a = _so_hieu(client, _token(tenant_a))
        cua_b = _so_hieu(client, _token(tenant_b))

    assert so_chung in cua_a
    assert so_a in cua_a
    assert so_b not in cua_a

    assert so_chung in cua_b
    assert so_b in cua_b
    assert so_a not in cua_b


def test_token_khong_co_claim_tenant_thi_401(settings: Settings) -> None:
    """Thiếu claim ``tenant_id`` thì bị TỪ CHỐI, không phải rơi về "chỉ phần dùng chung".

    ⚠️ Test này thay cho một test cũ khẳng định điều ngược lại
    (``..._thi_chi_thay_phan_dung_chung``). Hành vi cũ fail-closed nên không rò rỉ, và lý do
    ghi kèm nó vẫn đúng: mất quyền xem tốt hơn rò rỉ. Nhưng nó **im lặng** — người gọi nhận
    200 với danh sách ngắn hơn họ tưởng, và không ai biết token đã hỏng. Một tài khoản mất
    quyền xem trong im lặng được báo sau nhiều ngày, mô tả là "hệ thống thiếu dữ liệu", và
    không ai đi tìm ở tầng xác thực.

    `identity-tenant` LUÔN phát `tenant_id` (TokenIssuer.Phat). Token hợp lệ về chữ ký mà
    thiếu claim đó không phải thứ hệ thống này sinh ra.
    """
    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get(
            "/retrieval/documents", headers={"Authorization": f"Bearer {_token(None)}"}
        )

    assert phan_hoi.status_code == 401


def test_token_thieu_sub_thi_401(settings: Settings) -> None:
    """``sub`` cũng bắt buộc — nó là thứ duy nhất nói token này của AI."""
    thieu_sub = _token(uuid.uuid4())
    claims = jwt.decode(
        thieu_sub, KHOA_KY_TEST, algorithms=["HS256"], audience=AUDIENCE_TEST, issuer=ISSUER_TEST
    )
    del claims["sub"]
    khong_sub = jwt.encode(claims, KHOA_KY_TEST, algorithm="HS256")

    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get(
            "/retrieval/documents", headers={"Authorization": f"Bearer {khong_sub}"}
        )

    assert phan_hoi.status_code == 401


def test_tenant_id_khong_phai_guid_thi_401(settings: Settings) -> None:
    """Claim có mặt nhưng méo cũng là 401, không phải im lặng bỏ qua."""
    # Không dùng `_token(..., tenant_id=...)`: `tenant_id` là tham số vị trí của hàm đó,
    # truyền lại qua **ghi_de là đụng tên. Ký thẳng cho rõ ý.
    hop_le = _token(uuid.uuid4())
    claims = jwt.decode(
        hop_le, KHOA_KY_TEST, algorithms=["HS256"], audience=AUDIENCE_TEST, issuer=ISSUER_TEST
    )
    claims["tenant_id"] = "khong-phai-guid"
    meo = jwt.encode(claims, KHOA_KY_TEST, algorithm="HS256")

    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get("/retrieval/documents", headers={"Authorization": f"Bearer {meo}"})

    assert phan_hoi.status_code == 401


def test_route_moi_tren_router_can_xac_thuc_mac_dinh_da_duoc_bao_ve(
    settings: Settings,
) -> None:
    """Route mới gắn vào ``router_can_xac_thuc`` được bảo vệ mà KHÔNG cần viết thêm gì.

    Đây là test quan trọng nhất của tầng xác thực, và nó không kiểm một endpoint cụ thể nào
    — nó kiểm **cấu trúc**. Trước đây mỗi handler tự gọi ``tenant_tu_request(...)`` trong
    thân hàm; cách đó đúng khi người viết nhớ, và một biện pháp bảo vệ phải nhớ mới có tác
    dụng thì nó là thói quen chứ không phải biện pháp.

    Đo được: 09/09/2026, hai mươi tư giờ sau khi review nêu đúng điều này, một route mới
    (``GET /documents/{document_id}``) được thêm theo đúng khuôn cũ. Lần đó người viết nhớ.

    Route dựng tại chỗ rồi gỡ đi trong ``finally``: ``router_can_xac_thuc`` là biến cấp
    module, để lại route thừa là làm bẩn mọi test chạy sau.
    """

    @router_can_xac_thuc.get("/_route-thu-nghiem")
    def _route_thu_nghiem() -> dict[str, str]:
        # Cố tình KHÔNG khai tenant_id, không gọi hàm xác thực nào — đúng kiểu một route
        # viết vội. Nó vẫn phải bị chặn.
        return {"trang_thai": "khong-bao-gio-toi-day-neu-thieu-token"}

    try:
        with TestClient(create_app(settings)) as client:
            khong_token = client.get("/retrieval/_route-thu-nghiem")
            co_token = client.get(
                "/retrieval/_route-thu-nghiem",
                headers={"Authorization": f"Bearer {_token(uuid.uuid4())}"},
            )

        assert khong_token.status_code == 401
        assert co_token.status_code == 200
    finally:
        router_can_xac_thuc.routes[:] = [
            r for r in router_can_xac_thuc.routes if getattr(r, "name", "") != "_route_thu_nghiem"
        ]


def test_health_van_an_danh(settings: Settings) -> None:
    """Vế còn lại của cùng một quyết định: healthcheck KHÔNG được đòi token.

    ALB và ``smoke_test.sh`` gọi hai endpoint này mà không có token. Gắn nhầm chúng vào
    ``router_can_xac_thuc`` sẽ làm mọi lần deploy báo service chết trong khi nó vẫn khoẻ.
    """
    with TestClient(create_app(settings)) as client:
        assert client.get("/retrieval/health/live").status_code == 200


@pytest.mark.usefixtures("engine")
def test_tong_so_dem_sau_khi_loc(
    settings: Settings,
    tenant_co_du_lieu: tuple[uuid.UUID, uuid.UUID, str, str, str],
) -> None:
    tenant_a, _, _, _, so_b = tenant_co_du_lieu

    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get(
            "/retrieval/documents",
            params={"page_size": 100},
            headers={"Authorization": f"Bearer {_token(tenant_a)}"},
        )

    than = phan_hoi.json()
    assert than["total_count"] == len(than["items"])
    assert so_b not in [m["document_number"] for m in than["items"]]


@pytest.mark.parametrize(
    ("tham_so", "gia_tri"), [("page", 0), ("page_size", 0), ("page_size", 101)]
)
def test_tham_so_phan_trang_sai_thi_422(settings: Settings, tham_so: str, gia_tri: int) -> None:
    # FastAPI trả 422 cho ràng buộc Query(ge=…/le=…) — khác 400 của phía .NET, và đó là quy
    # ước của từng framework chứ không phải chỗ cần thống nhất bằng mọi giá.
    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get(
            "/retrieval/documents",
            params={tham_so: gia_tri},
            headers={"Authorization": f"Bearer {_token(uuid.uuid4())}"},
        )

    assert phan_hoi.status_code == 422


@pytest.mark.usefixtures("engine")
def test_lay_mot_van_ban_theo_id(
    settings: Settings,
    tenant_co_du_lieu: tuple[uuid.UUID, uuid.UUID, str, str, str],
) -> None:
    tenant_a, _, _, so_a, _ = tenant_co_du_lieu

    with TestClient(create_app(settings)) as client:
        danh_sach = client.get(
            "/retrieval/documents",
            params={"page_size": 100},
            headers={"Authorization": f"Bearer {_token(tenant_a)}"},
        ).json()["items"]
        van_ban_id = next(m["id"] for m in danh_sach if m["document_number"] == so_a)

        phan_hoi = client.get(
            f"/retrieval/documents/{van_ban_id}",
            headers={"Authorization": f"Bearer {_token(tenant_a)}"},
        )

    assert phan_hoi.status_code == 200, phan_hoi.text
    assert phan_hoi.json()["document_number"] == so_a


def test_lay_mot_van_ban_id_khong_ton_tai_thi_404(settings: Settings) -> None:
    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get(
            f"/retrieval/documents/{uuid.uuid4()}",
            headers={"Authorization": f"Bearer {_token(uuid.uuid4())}"},
        )

    assert phan_hoi.status_code == 404


@pytest.mark.usefixtures("engine")
def test_lay_mot_van_ban_cua_tenant_khac_thi_404_khong_phai_403(
    settings: Settings,
    tenant_co_du_lieu: tuple[uuid.UUID, uuid.UUID, str, str, str],
) -> None:
    """Id thuộc tenant B, tenant A gọi tới — phải 404 y hệt id không tồn tại.

    Không phải 403: 403 xác nhận id đó CÓ tồn tại, chỉ là không có quyền. FastAPI dễ mắc
    bẫy này nếu tách thành "lấy theo id trước, kiểm tenant sau" — endpoint này cố tình không
    làm vậy, xem docstring của ``db.lay_van_ban``.
    """
    tenant_a, tenant_b, _, _, so_b = tenant_co_du_lieu

    with TestClient(create_app(settings)) as client:
        cua_b = client.get(
            "/retrieval/documents",
            params={"page_size": 100},
            headers={"Authorization": f"Bearer {_token(tenant_b)}"},
        ).json()["items"]
        id_cua_b = next(m["id"] for m in cua_b if m["document_number"] == so_b)

        phan_hoi = client.get(
            f"/retrieval/documents/{id_cua_b}",
            headers={"Authorization": f"Bearer {_token(tenant_a)}"},
        )

    assert phan_hoi.status_code == 404


def test_dieu_kien_tenant_nam_trong_cau_sql_chu_khong_o_tang_ung_dung() -> None:
    """Khoá lại chính cơ chế, không chỉ khoá kết quả.

    Năm test phía trên vẫn xanh nếu ai đó chuyển việc lọc lên Python — lấy hết bản ghi rồi
    lọc trong vòng lặp. Test này thì không: nó đọc chuỗi SQL và đòi điều kiện tenant có mặt
    ở đó. Đây là bản đối ứng của
    ``Bo_loc_tenant_nam_trong_cau_SQL_chu_khong_o_tang_ung_dung`` bên .NET.
    """
    assert '"TenantId" IS NULL OR "TenantId" = :tenant_id' in db.DIEU_KIEN_TENANT
    assert db.DIEU_KIEN_TENANT in db.CAU_LIET_KE
    assert db.DIEU_KIEN_TENANT in db.CAU_DEM
    assert db.DIEU_KIEN_TENANT in db.CAU_LAY_MOT
