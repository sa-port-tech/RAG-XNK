"""Ràng buộc trên cấu hình đọc từ biến môi trường.

Các test khác trong thư mục này dựng ``Settings`` trực tiếp để độc lập với môi trường của
máy đang chạy — đúng, nhưng hệ quả là đường ``Settings.tu_moi_truong()`` không có ai kiểm,
mà đó lại là đường duy nhất service thật đi khi khởi động. File này kiểm riêng đường đó.
"""

from __future__ import annotations

import pytest

from retrieval.config import (
    DO_DAI_KHOA_TOI_THIEU,
    KHOA_DA_CONG_KHAI,
    Settings,
    chuan_hoa_dsn,
)

_DSN = "postgresql://xnk_retrieval:mk@localhost:5432/xnk"
_ISSUER = "https://identity.test.local"
_AUDIENCE = "xnk-api-test"
_KHOA_TOT = "khoa-ky-danh-rieng-cho-test-dai-32-ky-tu"


def _dat_moi_truong(monkeypatch: pytest.MonkeyPatch, khoa: str) -> None:
    monkeypatch.setenv("DATABASE_URL", _DSN)
    monkeypatch.setenv("XNK_JWT_ISSUER", _ISSUER)
    monkeypatch.setenv("XNK_JWT_AUDIENCE", _AUDIENCE)
    monkeypatch.setenv("XNK_JWT_SIGNING_KEY", khoa)


def test_du_bien_thi_doc_duoc(monkeypatch: pytest.MonkeyPatch) -> None:
    _dat_moi_truong(monkeypatch, _KHOA_TOT)

    settings = Settings.tu_moi_truong()

    assert settings.jwt_signing_key == _KHOA_TOT
    assert settings.database_url.startswith("postgresql+asyncpg://")


def test_thieu_bien_thi_bao_mot_lan_cho_tat_ca(monkeypatch: pytest.MonkeyPatch) -> None:
    # Báo từng biến một thì người sửa phải chạy lại service sau mỗi lần sửa một dòng.
    for ten in ("DATABASE_URL", "XNK_JWT_ISSUER", "XNK_JWT_AUDIENCE", "XNK_JWT_SIGNING_KEY"):
        monkeypatch.delenv(ten, raising=False)

    with pytest.raises(RuntimeError) as loi:
        Settings.tu_moi_truong()

    thong_diep = str(loi.value)
    assert "DATABASE_URL" in thong_diep
    assert "XNK_JWT_ISSUER" in thong_diep
    assert "XNK_JWT_AUDIENCE" in thong_diep
    assert "XNK_JWT_SIGNING_KEY" in thong_diep


@pytest.mark.parametrize(
    "khoa", ["qua-ngan", "CHAY-sinh-khoa.sh", "31-ky-tu-van-con-thieu-mot-byt"]
)
def test_khoa_ngan_hon_32_byte_thi_chan(monkeypatch: pytest.MonkeyPatch, khoa: str) -> None:
    _dat_moi_truong(monkeypatch, khoa)

    with pytest.raises(RuntimeError, match="byte"):
        Settings.tu_moi_truong()


def test_gia_tri_mau_trong_env_example_khong_khoi_dong_duoc(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    # Đây là điều khiến bước `bash tools/local/sinh-khoa.sh` là BẮT BUỘC chứ không phải
    # một lời khuyên trong README. Ai kéo dài giá trị mẫu cho "tiện" sẽ làm test này đỏ.
    gia_tri_mau = "CHAY-sinh-khoa.sh"
    assert len(gia_tri_mau.encode("utf-8")) < DO_DAI_KHOA_TOI_THIEU

    _dat_moi_truong(monkeypatch, gia_tri_mau)
    with pytest.raises(RuntimeError):
        Settings.tu_moi_truong()


@pytest.mark.parametrize("khoa", sorted(KHOA_DA_CONG_KHAI))
def test_khoa_da_tung_nam_trong_git_thi_chan_du_rat_dai(
    monkeypatch: pytest.MonkeyPatch, khoa: str
) -> None:
    # ⚠️ HỒI QUY, song sinh với JwtOptionsTests bên .NET.
    #
    # Chuỗi này qua mọi phép đo độ dài — nó bị chặn vì đã được commit, nên ai đọc lịch sử
    # repo cũng ký được token hợp lệ cho cả bảy service (ADR-014, một khoá đối xứng chung).
    # Đo 08/09/2026: .env trên máy dev mang y nguyên giá trị này dù .env.example có dặn
    # hãy thay. Lời dặn không đổi được hành vi khi bỏ qua nó không gây hậu quả nào.
    assert len(khoa.encode("utf-8")) >= DO_DAI_KHOA_TOI_THIEU

    _dat_moi_truong(monkeypatch, khoa)
    with pytest.raises(RuntimeError, match="lịch sử git"):
        Settings.tu_moi_truong()


def test_khoa_it_ky_tu_nhung_du_byte_thi_hop_le(monkeypatch: pytest.MonkeyPatch) -> None:
    # Vì sao đo BYTE chứ không đo ký tự: HMAC-SHA256 cần 32 byte, và chữ tiếng Việt có dấu
    # chiếm 2-3 byte mỗi ký tự trong UTF-8. Phép đo cũ đếm ký tự nên từ chối một khoá đủ
    # mạnh theo đúng thứ thuật toán cần — và lệch đơn vị so với phía .NET.
    khoa_co_dau = "khoá-ký-đủ-mạnh-nhưng-ít-ký-tự"
    assert len(khoa_co_dau) < DO_DAI_KHOA_TOI_THIEU
    assert len(khoa_co_dau.encode("utf-8")) >= DO_DAI_KHOA_TOI_THIEU

    _dat_moi_truong(monkeypatch, khoa_co_dau)

    assert Settings.tu_moi_truong().jwt_signing_key == khoa_co_dau


@pytest.mark.parametrize(
    ("vao", "ra"),
    [
        ("postgresql://a@b/c", "postgresql+asyncpg://a@b/c"),
        ("postgres://a@b/c", "postgresql+asyncpg://a@b/c"),
        ("postgresql+asyncpg://a@b/c", "postgresql+asyncpg://a@b/c"),
    ],
)
def test_chuan_hoa_dsn(vao: str, ra: str) -> None:
    assert chuan_hoa_dsn(vao) == ra


def test_dsn_khong_hop_le_thi_bao_loi() -> None:
    with pytest.raises(RuntimeError, match="DSN PostgreSQL"):
        chuan_hoa_dsn("mysql://a@b/c")
