"""Kiểm tra JWT và lấy tenant từ claim.

Đây là bản đối ứng phía Python của `Xnk.Shared/Authentication/JwtAuthenticationExtensions.cs`
và `HttpTenantContext`. Hai bản cài đặt, một hợp đồng — nên bộ tham số kiểm tra phải giống
hệt nhau. Bỏ sót một tham số ở đây không gây lỗi biên dịch, không làm đỏ test nào, chỉ làm
service này chấp nhận thứ mà sáu service .NET từ chối.

⚠️ Khi service Python thứ hai cần xác thực, **đừng chép file này sang đó.** Lúc ấy phải
tách thành một package dùng chung trong workspace — và vì việc đó đụng ma trận của
`ci-python` cùng ghi chú ở `src/python/pyproject.toml`, nó cần một ADR riêng chứ không phải
một lần copy-paste.
"""

from __future__ import annotations

import logging
import uuid
from typing import Any, Final

import jwt
from fastapi import HTTPException, Request, status

# Tên claim phải khớp TenantClaims bên .NET (src/dotnet/Xnk.Shared/Tenancy/TenantClaims.cs).
# Lệch tên không phải lỗi biên dịch mà là một service coi mọi request như không có tenant —
# hậu quả là người dùng không thấy dữ liệu riêng của mình, trông y hệt một lỗi nghiệp vụ.
CLAIM_TENANT_ID: Final = "tenant_id"
CLAIM_TENANT_TYPE: Final = "tenant_type"
CLAIM_ROLE: Final = "role"

_log = logging.getLogger(__name__)

# HS256 và chỉ HS256. Không để thư viện tự suy thuật toán từ header của chính token —
# đó là đường dẫn tới lỗi "alg: none" và lỗi nhầm khoá công khai thành khoá HMAC.
THUAT_TOAN: Final = ["HS256"]

# Bằng ClockSkew 30 giây của phía .NET. Mặc định của thư viện rộng hơn nhiều, và với token
# ngắn hạn thì mấy phút ân hạn là một quãng đáng kể sau khi token đã hết hạn.
DO_LECH_DONG_HO_GIAY: Final = 30

# Claim BẮT BUỘC phải có mặt thì token mới được chấp nhận.
#
# `tenant_id` nằm trong danh sách này là một quyết định về ngữ nghĩa, không phải một phép
# kiểm chặt tay thêm cho vui. Trước đây token thiếu `tenant_id` vẫn qua, rồi rơi âm thầm về
# "chỉ thấy tài liệu dùng chung" — fail-closed nên không rò rỉ, nhưng **không có tín hiệu
# nào**: người gọi nhận 200 với một danh sách ngắn hơn họ tưởng, và không ai biết token đã
# hỏng. Một tài khoản mất quyền xem trong im lặng là sự cố người ta báo sau nhiều ngày, mô
# tả là "hệ thống thiếu dữ liệu", và không ai đi tìm ở tầng xác thực.
#
# `identity-tenant` LUÔN phát cả `sub` lẫn `tenant_id` (xem TokenIssuer.Phat). Một token
# hợp lệ về chữ ký mà thiếu hai claim đó không phải thứ hệ thống này sinh ra — nó đến từ
# một bộ phát khác hoặc một bản cũ, và chấp nhận nó là chấp nhận một hợp đồng mình không
# biết. Phía .NET chặn cùng điều này trong `AddXnkJwtAuthentication` (sự kiện
# OnTokenValidated); hai danh sách phải giống nhau, lệch là một service nhận thứ service
# bên cạnh từ chối.
CLAIM_BAT_BUOC: Final = ["exp", "iss", "aud", "sub", CLAIM_TENANT_ID]


def _giai_ma(token: str, issuer: str, audience: str, signing_key: str) -> dict[str, Any]:
    try:
        return jwt.decode(
            token,
            signing_key,
            algorithms=THUAT_TOAN,
            audience=audience,
            issuer=issuer,
            leeway=DO_LECH_DONG_HO_GIAY,
            options={"require": CLAIM_BAT_BUOC},
        )
    except jwt.InvalidTokenError as loi:
        # Một thông báo duy nhất cho mọi lý do: hết hạn, sai chữ ký, sai audience. Phân
        # biệt ra là kể cho người gọi biết token của họ hỏng ở đúng chỗ nào.
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Token không hợp lệ.",
            headers={"WWW-Authenticate": "Bearer"},
        ) from loi


def tenant_tu_request(request: Request, issuer: str, audience: str, signing_key: str) -> uuid.UUID:
    """Lấy tenant từ header ``Authorization``.

    Thiếu header, chữ ký sai, hết hạn, thiếu claim bắt buộc, hoặc ``tenant_id`` không phải
    GUID — tất cả đều là **401**. Một mã lỗi cho mọi lý do: phân biệt ra là kể cho người gọi
    biết token của họ hỏng ở đúng chỗ nào.

    Trước đây hàm này trả ``None`` khi claim thiếu hoặc méo, nghĩa là "chỉ thấy phần dùng
    chung". Hướng đó fail-closed nên không rò rỉ, nhưng nó **im lặng** — và một biện pháp an
    toàn không phát ra tín hiệu nào thì không ai biết nó vừa hoạt động. Xem ``CLAIM_BAT_BUOC``.
    """
    header = request.headers.get("Authorization", "")
    lich, _, token = header.partition(" ")

    if lich.lower() != "bearer" or not token:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Thiếu header Authorization: Bearer <token>.",
            headers={"WWW-Authenticate": "Bearer"},
        )

    claims = _giai_ma(token, issuer, audience, signing_key)

    # `CLAIM_BAT_BUOC` đã bảo đảm claim CÓ MẶT; ở đây chỉ còn kiểm nó có đúng dạng không.
    tho = claims.get(CLAIM_TENANT_ID)
    if isinstance(tho, str):
        try:
            return uuid.UUID(tho)
        except ValueError:
            pass

    _log.warning(
        "Token hợp lệ về chữ ký nhưng claim %s không phải GUID — từ chối.", CLAIM_TENANT_ID
    )
    raise HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Token không hợp lệ.",
        headers={"WWW-Authenticate": "Bearer"},
    )
