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

# HS256 và chỉ HS256. Không để thư viện tự suy thuật toán từ header của chính token —
# đó là đường dẫn tới lỗi "alg: none" và lỗi nhầm khoá công khai thành khoá HMAC.
THUAT_TOAN: Final = ["HS256"]

# Bằng ClockSkew 30 giây của phía .NET. Mặc định của thư viện rộng hơn nhiều, và với token
# ngắn hạn thì mấy phút ân hạn là một quãng đáng kể sau khi token đã hết hạn.
DO_LECH_DONG_HO_GIAY: Final = 30


def _giai_ma(token: str, issuer: str, audience: str, signing_key: str) -> dict[str, Any]:
    try:
        return jwt.decode(
            token,
            signing_key,
            algorithms=THUAT_TOAN,
            audience=audience,
            issuer=issuer,
            leeway=DO_LECH_DONG_HO_GIAY,
            options={"require": ["exp", "iss", "aud"]},
        )
    except jwt.InvalidTokenError as loi:
        # Một thông báo duy nhất cho mọi lý do: hết hạn, sai chữ ký, sai audience. Phân
        # biệt ra là kể cho người gọi biết token của họ hỏng ở đúng chỗ nào.
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Token không hợp lệ.",
            headers={"WWW-Authenticate": "Bearer"},
        ) from loi


def tenant_tu_request(
    request: Request, issuer: str, audience: str, signing_key: str
) -> uuid.UUID | None:
    """Lấy tenant từ header ``Authorization``.

    Trả ``None`` khi claim thiếu hoặc không phải GUID — nghĩa là "chỉ thấy phần dùng chung",
    đúng ngữ nghĩa của `HttpTenantContext` bên .NET. Không ném ngoại lệ ở đó: một token méo
    phải bị chặn ở bước kiểm chữ ký phía trên, và nếu nó lọt qua được thì **mất quyền xem
    vẫn tốt hơn là rò rỉ**.

    Thiếu hẳn header thì là 401 — khác với claim thiếu, đây là "chưa xác thực", không phải
    "đã xác thực nhưng không thuộc tenant nào".
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

    tho = claims.get(CLAIM_TENANT_ID)
    if not isinstance(tho, str):
        return None

    try:
        return uuid.UUID(tho)
    except ValueError:
        return None
