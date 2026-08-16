#!/usr/bin/env bash
# Hàm dùng chung cho các script trong .github/setup/.
#
# Nguyên tắc chung của bộ script này:
#   · Idempotent — chạy lại lần thứ hai không được hỏng và không được nhân bản gì.
#   · Không hỏi tương tác — chạy được trong CI cũng như trên máy cá nhân.
#   · Nói rõ đang làm gì với tài nguyên nào, vì chúng thay đổi cấu hình thật.

set -euo pipefail

SETUP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SETUP_DIR/../.." && pwd)"

# shellcheck source=config.env
source "$SETUP_DIR/config.env"

SLUG="${ORG}/${REPO}"

if [ -t 1 ]; then
  C_RESET=$'\033[0m'; C_DIM=$'\033[2m'; C_RED=$'\033[31m'
  C_GREEN=$'\033[32m'; C_YELLOW=$'\033[33m'; C_BLUE=$'\033[34m'; C_BOLD=$'\033[1m'
else
  C_RESET=""; C_DIM=""; C_RED=""; C_GREEN=""; C_YELLOW=""; C_BLUE=""; C_BOLD=""
fi

step() { printf '\n%s▸ %s%s\n' "$C_BOLD$C_BLUE" "$*" "$C_RESET"; }
ok()   { printf '  %s✓%s %s\n' "$C_GREEN" "$C_RESET" "$*"; }
info() { printf '  %s·%s %s\n' "$C_DIM" "$C_RESET" "$*"; }
warn() { printf '  %s⚠%s %s\n' "$C_YELLOW" "$C_RESET" "$*" >&2; }
fail() { printf '  %s✗%s %s\n' "$C_RED" "$C_RESET" "$*" >&2; }
die()  { fail "$*"; exit 1; }

# Số lỗi tích luỹ, dùng cho 99-verify.sh.
PROBLEMS=0
problem() { fail "$*"; PROBLEMS=$((PROBLEMS + 1)); }

require_gh() {
  command -v gh > /dev/null 2>&1 \
    || die "Không tìm thấy GitHub CLI. Cài bằng: winget install --id GitHub.cli"
  gh auth status > /dev/null 2>&1 \
    || die "Chưa đăng nhập GitHub CLI. Chạy: gh auth login"
}

require_jq() {
  command -v jq > /dev/null 2>&1 \
    || die "Không tìm thấy jq. Cài bằng: winget install --id jqlang.jq"
}

# Tìm trình thông dịch Python dùng được.
#
# Trên Windows, `python3` thường là App Execution Alias của Microsoft Store: nó tồn
# tại trong PATH, `command -v` thấy nó, nhưng chạy thì mở cửa hàng ứng dụng thay vì
# chạy Python. Kiểm tra bằng cách gọi thử là cách duy nhất phân biệt được.
detect_python() {
  local candidate
  for candidate in "python3" "python" "py -3"; do
    if $candidate -c 'import sys; sys.exit(0 if sys.version_info >= (3, 8) else 1)' > /dev/null 2>&1; then
      PY="$candidate"
      return 0
    fi
  done
  die "Không tìm thấy Python 3.8 trở lên. Cài từ python.org hoặc: winget install --id Python.Python.3.12"
}

require_python() {
  detect_python
  $PY -c 'import yaml' > /dev/null 2>&1 \
    || die "Thiếu PyYAML. Cài bằng: $PY -m pip install pyyaml"
}

# Gọi GitHub API, trả về thân phản hồi; im lặng khi lỗi để nơi gọi tự xử lý.
api() { gh api "$@" 2>/dev/null; }

# true nếu tài nguyên tồn tại (HTTP 2xx).
exists() { gh api "$1" > /dev/null 2>&1; }

confirm_repo() {
  exists "repos/$SLUG" \
    || die "Không truy cập được repos/$SLUG. Repo đã chuyển sang org $ORG chưa? Xem docs/17 §2."
}
