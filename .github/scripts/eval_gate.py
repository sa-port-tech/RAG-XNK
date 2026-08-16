#!/usr/bin/env python3
"""Cổng chất lượng eval — đối chiếu kết quả một lần chạy với ngưỡng và với baseline.

Thực thi docs/09 E7-05 và E7-06, theo bảng ngưỡng ở docs/14:

    Stale Citation Rate  = 0        → chặn merge tuyệt đối
    Recall@10            ≥ 0,90     → chặn merge
    Citation Precision   ≥ 0,90     → cảnh báo
    Correct Refusal Rate ≥ 0,90     → cảnh báo
    Bất kỳ chỉ số nào tụt > 2% so với baseline → chặn merge

Ba chế độ:

    check           đối chiếu, in bảng so sánh, thoát khác 0 nếu bị chặn
    write-baseline  ghi baseline mới từ kết quả (chỉ chạy trên main sau khi merge)
    fingerprint     in vân tay của golden set hiện tại

Một điểm thiết kế cần nói rõ: khi golden set thay đổi, baseline cũ bị coi là **không so
sánh được** chứ không phải "so sánh rồi bỏ qua". Đo hai phiên bản thước đo khác nhau rồi
kết luận chất lượng tăng hay giảm là một sai lầm im lặng — docs/14 chốt sẵn điều này.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

# Runner của GitHub dùng UTF-8, nhưng console Windows mặc định cp1252 và sẽ ném
# UnicodeEncodeError ngay ký tự tiếng Việt hoặc emoji đầu tiên. Developer phải chạy
# được script này trên máy mình thì cổng mới có ý nghĩa.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

GOLDEN_SET_DIR = Path("eval/golden-set")
# baseline.json nằm trong golden-set nhưng không phải là một phần của thước đo,
# nên không được tính vào vân tay — nếu tính, mọi lần cập nhật baseline sẽ tự làm
# vô hiệu chính nó.
FINGERPRINT_EXCLUDE = {"baseline.json"}


# ─── Tiện ích hiển thị ──────────────────────────────────────────────────────────


def vi(value: float, digits: int = 4) -> str:
    """Số theo cách viết tiếng Việt: dấu phẩy thập phân."""
    return f"{value:.{digits}f}".replace(".", ",")


def fmt(value: Any, kind: str) -> str:
    if value is None:
        return "—"
    if kind == "percent":
        return f"{value * 100:.2f}".replace(".", ",") + "%"
    return vi(float(value))


def emit_output(name: str, value: str) -> None:
    target = os.environ.get("GITHUB_OUTPUT")
    if target:
        with open(target, "a", encoding="utf-8") as handle:
            if "\n" in value:
                delimiter = f"ghadelim_{os.urandom(8).hex()}"
                handle.write(f"{name}<<{delimiter}\n{value}\n{delimiter}\n")
            else:
                handle.write(f"{name}={value}\n")


def write_summary(markdown: str) -> None:
    target = os.environ.get("GITHUB_STEP_SUMMARY")
    if target:
        with open(target, "a", encoding="utf-8") as handle:
            handle.write(markdown + "\n")


def load_yaml(path: Path) -> Any:
    try:
        import yaml
    except ModuleNotFoundError:
        sys.exit("eval_gate.py: thiếu PyYAML — thêm `pip install pyyaml` vào workflow.")
    if not path.is_file():
        sys.exit(f"eval_gate.py: không tìm thấy {path}")
    return yaml.safe_load(path.read_text(encoding="utf-8"))


def load_json(path: Path) -> Any:
    if not path.is_file():
        sys.exit(f"eval_gate.py: không tìm thấy {path}")
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        sys.exit(f"eval_gate.py: {path} không phải JSON hợp lệ — {exc}")


# ─── Vân tay golden set ─────────────────────────────────────────────────────────


def fingerprint(directory: Path = GOLDEN_SET_DIR) -> str:
    """Băm nội dung golden set để phát hiện thước đo đã đổi."""
    if not directory.is_dir():
        return "khong-co-golden-set"

    digest = hashlib.sha256()
    files = sorted(
        p for p in directory.rglob("*")
        if p.is_file() and p.name not in FINGERPRINT_EXCLUDE
    )
    for file in files:
        digest.update(file.relative_to(directory).as_posix().encode("utf-8"))
        digest.update(file.read_bytes())
    return digest.hexdigest()[:16]


def count_questions(directory: Path = GOLDEN_SET_DIR) -> int:
    """Đếm file câu hỏi, dùng để hiển thị trong báo cáo."""
    if not directory.is_dir():
        return 0
    return sum(
        1
        for p in directory.rglob("*")
        if p.is_file() and p.suffix in {".yaml", ".yml"} and p.name not in FINGERPRINT_EXCLUDE
    )


# ─── Đánh giá ───────────────────────────────────────────────────────────────────


@dataclass
class Row:
    key: str
    label: str
    direction: str
    fmt_kind: str
    blocking: bool
    current: Any = None
    previous: Any = None
    delta: float | None = None
    verdicts: list[str] = field(default_factory=list)
    blocked: bool = False
    warned: bool = False

    @property
    def status_icon(self) -> str:
        if self.blocked:
            return "❌"
        if self.warned:
            return "⚠️"
        return "✅"


def evaluate(gates: dict, results: dict, baseline: dict, comparable: bool) -> list[Row]:
    metrics = results.get("metrics")
    if not isinstance(metrics, dict):
        sys.exit(
            "eval_gate.py: file kết quả thiếu khoá 'metrics'. Runner phải xuất "
            "{\"metrics\": {\"recall_at_10\": 0.91, ...}}."
        )

    # Không có khoá nào trong `metrics` nghĩa là bộ đo chưa tồn tại, chứ không phải
    # hệ thống đo ra kết quả xấu.
    no_data = not metrics

    base_metrics = baseline.get("metrics") or {}
    tolerance = float(gates["regression"]["max_relative_drop_percent"])
    regression_blocking = bool(gates["regression"].get("blocking", True))

    rows: list[Row] = []
    for spec in gates["metrics"]:
        row = Row(
            key=spec["key"],
            label=spec["label"],
            direction=spec["direction"],
            fmt_kind=spec.get("format", "ratio"),
            blocking=bool(spec.get("blocking", False)),
        )
        row.current = metrics.get(spec["key"])
        row.previous = base_metrics.get(spec["key"]) if comparable else None

        if row.current is None:
            row.blocked = True
            # Phân biệt hai nguyên nhân khác hẳn nhau. Bộ đo chưa tồn tại là trạng
            # thái bình thường của Sprint 0; runner có chạy nhưng thiếu một khoá là
            # lỗi thật trong bộ đo, và gộp chung hai thứ sẽ che mất cái thứ hai.
            if no_data:
                row.verdicts.append("chưa có số liệu")
            else:
                row.verdicts.append(
                    f"runner không xuất chỉ số `{spec['key']}` — cổng không thể kết luận"
                )
            rows.append(row)
            continue

        current = float(row.current)

        # ── Ngưỡng tuyệt đối ────────────────────────────────────────────────
        def fail(text: str) -> None:
            row.verdicts.append(text)
            if row.blocking:
                row.blocked = True
            else:
                row.warned = True

        absolute = spec.get("absolute") or {}
        if "max" in absolute and current > float(absolute["max"]) + 1e-12:
            fail(f"vượt ngưỡng tối đa {vi(float(absolute['max']))}")
        if "min" in absolute and current + 1e-12 < float(absolute["min"]):
            fail(f"dưới ngưỡng tối thiểu {vi(float(absolute['min']))}")

        # ── Trượt so với baseline ───────────────────────────────────────────
        if row.previous is not None:
            previous = float(row.previous)
            row.delta = current - previous

            if previous == 0:
                # Không chia được cho 0. Với chỉ số càng thấp càng tốt mà baseline = 0
                # (đúng trường hợp Stale Citation Rate), mọi mức tăng đều là xấu và đã
                # bị ngưỡng tuyệt đối bắt ở trên.
                worsened_relative = 0.0
            elif row.direction == "higher_is_better":
                worsened_relative = (previous - current) / abs(previous) * 100.0
            else:
                worsened_relative = (current - previous) / abs(previous) * 100.0

            if worsened_relative > tolerance:
                row.verdicts.append(
                    f"tụt {vi(worsened_relative, 2)}% so với baseline "
                    f"(biên cho phép {vi(tolerance, 2)}%)"
                )
                if regression_blocking:
                    row.blocked = True
                else:
                    row.warned = True

        rows.append(row)

    return rows


# ─── Báo cáo ────────────────────────────────────────────────────────────────────


def render_report(
    rows: list[Row],
    *,
    enabled: bool,
    comparable: bool,
    baseline: dict,
    results: dict,
    tolerance: float,
    reason_not_comparable: str | None,
) -> str:
    lines: list[str] = []
    lines.append("## 📊 Eval regression — golden set")
    lines.append("")

    if not enabled:
        lines.append(
            "> 🔴 **CỔNG EVAL ĐANG TẮT** (`eval/gates.yml` → `enabled: false`). "
            "Bảng dưới chỉ để tham khảo — **không có chỉ số nào chặn merge lúc này.** "
            "Bật lên trong PR hoàn thành E7-04."
        )
        lines.append("")

    if not (results.get("metrics") or {}):
        lines.append(
            "> ⏳ **Chưa có số liệu** — bộ đo eval chưa tồn tại (story E7-03, E7-04). "
            "Các dòng dưới trống vì chưa có gì để đo, **không phải** vì hệ thống đo ra "
            "kết quả xấu."
        )
        lines.append("")

    if not comparable:
        lines.append(f"> ℹ️ **Không so sánh được với baseline** — {reason_not_comparable}")
        lines.append("> Các ngưỡng tuyệt đối vẫn được áp dụng bình thường.")
        lines.append("")

    lines.append("| | Chỉ số | Baseline (`main`) | PR này | Thay đổi | Kết luận |")
    lines.append("|---|---|---|---|---|---|")

    for row in rows:
        if row.delta is None:
            delta_text = "—"
        else:
            arrow = "▲" if row.delta > 0 else ("▼" if row.delta < 0 else "●")
            delta_text = f"{arrow} {vi(abs(row.delta))}"

        verdict = "; ".join(row.verdicts) if row.verdicts else "đạt"
        label = f"**{row.label}**" if row.blocking else row.label
        lines.append(
            f"| {row.status_icon} | {label} | {fmt(row.previous, row.fmt_kind)} | "
            f"{fmt(row.current, row.fmt_kind)} | {delta_text} | {verdict} |"
        )

    lines.append("")

    blocked = [r for r in rows if r.blocked]
    warned = [r for r in rows if r.warned and not r.blocked]

    if blocked and not enabled:
        # Cổng đang tắt thì check báo SUCCESS. Viết "Chặn merge" ở đây là để báo cáo
        # tự mâu thuẫn với chính kết luận của nó — người đọc sẽ tin cái nào? Sau vài
        # lần như vậy họ thôi đọc cả hai.
        names = ", ".join(f"`{r.label}`" for r in blocked)
        lines.append(f"### ⏸ Sẽ chặn merge khi cổng được bật — {names}")
        lines.append("")
        lines.append(
            "PR này **không bị chặn** vì `eval/gates.yml` đang để `enabled: false`. "
            "Danh sách trên là thứ phải xử lý xong trước khi bật cổng."
        )
    elif blocked:
        names = ", ".join(f"`{r.label}`" for r in blocked)
        lines.append(f"### ❌ Chặn merge — {names}")
        lines.append("")
        lines.append(
            "Sửa hệ thống cho chỉ số đạt lại. **Không nới ngưỡng trong `eval/gates.yml` "
            "để PR xanh** — docs/14 gọi đó là gian lận với chính mình."
        )
    elif warned:
        names = ", ".join(f"`{r.label}`" for r in warned)
        lines.append(f"### ⚠️ Cảnh báo — {names}")
        lines.append("")
        lines.append("Không chặn merge, nhưng phải nêu trong phần mô tả PR vì sao chấp nhận được.")
    else:
        lines.append("### ✅ Toàn bộ cổng eval đạt")

    lines.append("")
    lines.append("<details><summary>Chi tiết lần chạy</summary>")
    lines.append("")
    lines.append("| | |")
    lines.append("|---|---|")
    lines.append(f"| Số câu golden set | {results.get('question_count', count_questions())} |")
    lines.append(f"| Vân tay golden set | `{fingerprint()}` |")
    lines.append(f"| Baseline sinh lúc | {baseline.get('generated_at') or '—'} |")
    lines.append(f"| Baseline tại commit | `{(baseline.get('commit') or '—')[:12]}` |")
    lines.append(f"| Biên trượt cho phép | {vi(tolerance, 2)}% |")
    lines.append("")
    lines.append("</details>")

    return "\n".join(lines)


# ─── Lệnh ───────────────────────────────────────────────────────────────────────


def cmd_check(args: argparse.Namespace) -> int:
    gates = load_yaml(Path(args.gates))
    results = load_json(Path(args.results))
    baseline_path = Path(gates["baseline"]["path"])
    baseline = load_json(baseline_path) if baseline_path.is_file() else {"status": "not-established"}

    enabled = bool(gates.get("enabled", False))

    reason: str | None = None
    comparable = True
    if baseline.get("status") != "established":
        comparable = False
        reason = "chưa có lần chạy nào trên `main` để lấy làm mốc."
    elif gates["baseline"].get("invalidate_when_golden_set_changes", True):
        current_fp = fingerprint()
        if baseline.get("golden_set_fingerprint") != current_fp:
            comparable = False
            reason = (
                "golden set đã thay đổi kể từ lần lập baseline "
                f"(`{baseline.get('golden_set_fingerprint')}` → `{current_fp}`). "
                "Baseline sẽ được lập lại khi PR này merge vào `main`."
            )

    rows = evaluate(gates, results, baseline, comparable)
    report = render_report(
        rows,
        enabled=enabled,
        comparable=comparable,
        baseline=baseline,
        results=results,
        tolerance=float(gates["regression"]["max_relative_drop_percent"]),
        reason_not_comparable=reason,
    )

    print(report)
    write_summary(report)
    Path(args.comment_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.comment_out).write_text(report, encoding="utf-8")

    blocked = [r for r in rows if r.blocked]
    emit_output("blocked", "true" if blocked else "false")
    emit_output("enabled", "true" if enabled else "false")

    for row in rows:
        if row.blocked:
            print(f"::error title=eval::{row.label} — {'; '.join(row.verdicts)}")
        elif row.warned:
            print(f"::warning title=eval::{row.label} — {'; '.join(row.verdicts)}")

    if not enabled:
        print("::warning title=eval::Cổng eval đang tắt (eval/gates.yml enabled: false).")
        return 0

    return 1 if blocked else 0


def cmd_write_baseline(args: argparse.Namespace) -> int:
    gates = load_yaml(Path(args.gates))
    results = load_json(Path(args.results))
    metrics = results.get("metrics") or {}

    baseline = {
        "_comment": [
            "Sinh tự động bởi .github/scripts/eval_gate.py trên nhánh main. Không sửa tay.",
        ],
        "status": "established",
        "generated_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "commit": os.environ.get("GITHUB_SHA"),
        "run_url": (
            f"{os.environ.get('GITHUB_SERVER_URL', 'https://github.com')}/"
            f"{os.environ.get('GITHUB_REPOSITORY', '')}/actions/runs/"
            f"{os.environ.get('GITHUB_RUN_ID', '')}"
        ),
        "golden_set_fingerprint": fingerprint(),
        "golden_set_question_count": results.get("question_count", count_questions()),
        "metrics": {spec["key"]: metrics.get(spec["key"]) for spec in gates["metrics"]},
    }

    path = Path(gates["baseline"]["path"])
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(baseline, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Đã ghi baseline mới vào {path}")
    return 0


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--gates", default="eval/gates.yml")
    sub = parser.add_subparsers(dest="command", required=True)

    check = sub.add_parser("check", help="Đối chiếu kết quả với ngưỡng và baseline")
    check.add_argument("--results", default="eval/reports/latest.json")
    check.add_argument("--comment-out", default="eval/reports/pr-comment.md")
    check.set_defaults(func=cmd_check)

    write = sub.add_parser("write-baseline", help="Ghi baseline mới (chỉ chạy trên main)")
    write.add_argument("--results", default="eval/reports/latest.json")
    write.set_defaults(func=cmd_write_baseline)

    fp = sub.add_parser("fingerprint", help="In vân tay golden set hiện tại")
    fp.set_defaults(func=lambda a: (print(fingerprint()), 0)[1])

    args = parser.parse_args()
    sys.exit(args.func(args))


if __name__ == "__main__":
    main()
