# ADR-014 — Khoá JWT đối xứng dùng chung cho prototype, và hạn chuyển sang bất đối xứng

**Trạng thái:** chấp nhận · 2026-09-02

## Bối cảnh

[`docs/00`](../00-ke-hoach-tong-the.md) §12 chốt: prototype dùng **JWT đối xứng** cho cả ba
loại tenant, production chuyển sang SSO SAML/OIDC nhưng giữ nguyên mô hình `tenant_id`.

Phần chưa được chốt ở đâu cả: khi `identity-tenant` ra đời (story `E1-11`), nó **ký** token
bằng cùng khoá mà sáu service kia dùng để **kiểm tra**. Hệ quả của điều đó chưa được viết
xuống, và nó là loại hệ quả dễ trôi qua nhiều tháng mà không ai nhắc lại.

## Quyết định

**Giữ khoá đối xứng HS256 cho prototype.** `Jwt:SigningKey` giống nhau ở cả bảy service;
`identity-tenant` ký, sáu service còn lại kiểm tra bằng `AddXnkJwtAuthentication` trong
`Xnk.Shared`.

**Ghi rõ cái giá:** mọi service giữ khoá ký đều **tự phát được token hợp lệ** cho bất kỳ
tenant nào. Không có gì trong hệ thống phân biệt được token do `identity-tenant` phát với
token do `generation` tự ký, vì chúng là cùng một phép toán trên cùng một khoá.

**Hạn chuyển:** trước khi có môi trường thật đầu tiên phục vụ dữ liệu của tenant có thật —
tức trước bước triển khai `staging` của `E1-06`, không phải "trước production".

## Hệ quả

- Ở local, một dev tự ký được token cho bất kỳ tenant nào. Chấp nhận: dev vốn đã có quyền
  đọc thẳng database.
- **Bộ test được lợi từ chính điều này** và không nên coi đó là mẹo: `Xnk.Corpus.Tests`
  ký token tại chỗ thay vì gọi sang `identity-tenant`. Nhờ vậy một lỗi ở identity không làm
  đỏ bộ test của corpus. Khi chuyển sang khoá bất đối xứng, test chỉ cần đổi sang ký bằng
  khoá riêng của bộ test — cấu trúc không đổi.
- Khoá ký nằm trong `appsettings.Development.json` và `.env.example`, **có chủ đích**, để
  `dotnet run` chạy được ngay sau khi clone. Trên dev/staging/production, giá trị đến từ
  AWS Secrets Manager và hai file đó không được deploy.
- Thu hồi một token cụ thể chưa làm được. Claim `jti` đã có trong token phát ra để về sau
  dựng danh sách thu hồi mà không phải đổi khoá — nhưng danh sách đó chưa tồn tại.

## Đường chuyển sang bất đối xứng

Khi tới hạn: `identity-tenant` giữ khoá riêng RSA/ECDSA và công bố khoá công khai qua
JWKS; sáu service kia đổi `IssuerSigningKey` thành `IssuerSigningKeyResolver` trỏ vào JWKS
đó. Thay đổi nằm gọn trong `Xnk.Shared/Authentication/JwtAuthenticationExtensions.cs` và
`TokenIssuer` — đó chính là lý do hai phần này được tập trung ngay từ đầu thay vì để mỗi
service tự cấu hình.

## Phương án đã cân nhắc và loại bỏ

**Bất đối xứng ngay từ prototype.** Đúng về mặt bảo mật, nhưng kéo theo quản lý vòng đời
khoá, endpoint JWKS, và bộ nhớ đệm khoá ở sáu service — trong khi prototype còn chưa có
môi trường nào ngoài máy dev. Đổi lại là món nợ đã có ngày trả ghi ở trên.

**Amazon Cognito ngay từ đầu.** Bỏ được toàn bộ bảng mật khẩu và lớp `PasswordHasher`.
Loại bỏ vì prototype phải chạy trọn vẹn **ở local, không cần internet** — đó là yêu cầu của
đợt dựng skeleton này ([`docs/19`](../19-ke-hoach-skeleton-local.md)). Một phụ thuộc bắt
buộc vào dịch vụ đám mây để đăng nhập được ở máy mình là đúng thứ cần tránh.

**Mỗi service một khoá riêng, identity ký bằng khoá của mình.** Không giải quyết được gì:
với thuật toán đối xứng, kiểm tra chữ ký đòi đúng khoá đã ký, nên sáu service vẫn phải giữ
khoá của identity. Đây là lý do đường ra thật sự là bất đối xứng, không phải "quản khoá
chặt hơn".
