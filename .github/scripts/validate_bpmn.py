#!/usr/bin/env python3
"""Kiểm tra file .bpmn trước khi cho merge.

Nhiệm vụ chính (docs/09 E1-15, docs/00 §"Những gì KHÔNG được làm"):
**chặn Java Delegate lọt vào file .bpmn.**

Vì sao đây là luật cứng chứ không phải khuyến nghị: Java Delegate khoá chặt quy trình
vào Camunda 7 và vào JVM. Một khi delegate xuất hiện, service .NET và Python không gọi
được nữa, và đường chuyển sang Camunda 8 sau này gần như đóng lại. Mẫu bắt buộc là
**external task** — Camunda phát việc ra topic, worker viết bằng ngôn ngữ nào cũng nhận
được.

Script không chỉ tìm chuỗi "JavaDelegate". Nó kiểm tra theo cấu trúc XML nên không bị
qua mặt bằng cách đổi tên lớp hay bọc qua biến.

Dùng:
    python3 .github/scripts/validate_bpmn.py bpmn/
    python3 .github/scripts/validate_bpmn.py bpmn/p1-tiep-nhan-van-ban.bpmn --format github
"""

from __future__ import annotations

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

# Console Windows mặc định cp1252 — không ép UTF-8 thì thông báo tiếng Việt vỡ khi
# developer chạy script trên máy mình trước khi mở PR.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

BPMN_NS = "http://www.omg.org/spec/BPMN/20100524/MODEL"
CAMUNDA_NS = "http://camunda.org/schema/1.0/bpmn"

# Thuộc tính Camunda dùng để gắn mã Java vào phần tử BPMN. Bất kỳ cái nào xuất hiện
# cũng là vi phạm, ở bất kỳ phần tử nào.
JAVA_BINDING_ATTRS = (
    f"{{{CAMUNDA_NS}}}class",
    f"{{{CAMUNDA_NS}}}delegateExpression",
)

# Phần tử chứa logic thực thi bên trong mô hình, thay vì trong service.
FORBIDDEN_TAGS = {
    f"{{{BPMN_NS}}}scriptTask": (
        "scriptTask nhúng mã vào mô hình quy trình. Logic phải nằm trong service và "
        "được gọi qua external task."
    ),
}

LISTENER_TAGS = (
    f"{{{CAMUNDA_NS}}}executionListener",
    f"{{{CAMUNDA_NS}}}taskListener",
)

# Quy ước đặt tên id tiến trình, khớp với docs/10: P1, P2, P3, P4.
PROCESS_ID_PATTERN = re.compile(r"^P[1-9]\d*-[a-z0-9]+(?:-[a-z0-9]+)*$")

# Phần tử là node của luồng, dùng để kiểm tra toàn vẹn tham chiếu.
FLOW_NODE_TAGS = {
    "task", "userTask", "serviceTask", "scriptTask", "sendTask", "receiveTask",
    "manualTask", "businessRuleTask", "callActivity", "subProcess", "transaction",
    "startEvent", "endEvent", "intermediateCatchEvent", "intermediateThrowEvent",
    "boundaryEvent", "exclusiveGateway", "parallelGateway", "inclusiveGateway",
    "eventBasedGateway", "complexGateway",
}


@dataclass
class Finding:
    file: Path
    line: int
    level: str  # "error" | "warning"
    rule: str
    message: str


class LineIndex:
    """Tra số dòng của một phần tử dựa trên id hoặc tên thẻ.

    ElementTree không giữ số dòng. Đọc lại phần văn bản thô và tìm lần xuất hiện đầu
    tiên là đủ chính xác để annotation của GitHub trỏ đúng chỗ cho người sửa.
    """

    def __init__(self, text: str) -> None:
        self._lines = text.splitlines()

    def find(self, *needles: str) -> int:
        for needle in needles:
            if not needle:
                continue
            for number, line in enumerate(self._lines, start=1):
                if needle in line:
                    return number
        return 1


def local(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def check_file(path: Path) -> list[Finding]:
    findings: list[Finding] = []
    text = path.read_text(encoding="utf-8", errors="replace")
    index = LineIndex(text)

    def add(level: str, rule: str, message: str, *needles: str) -> None:
        findings.append(Finding(path, index.find(*needles), level, rule, message))

    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        line = exc.position[0] if exc.position else 1
        findings.append(
            Finding(path, line, "error", "xml-malformed", f"XML không hợp lệ: {exc}")
        )
        return findings

    if local(root.tag) != "definitions":
        add("error", "not-bpmn", "Phần tử gốc phải là <bpmn:definitions>.", "<")
        return findings

    # ── Luật 1 — cấm mọi ràng buộc vào mã Java ────────────────────────────────
    for element in root.iter():
        element_id = element.get("id", "")
        for attr in JAVA_BINDING_ATTRS:
            if attr in element.attrib:
                short = attr.rsplit("}", 1)[-1]
                add(
                    "error",
                    "java-delegate",
                    f"<{local(element.tag)} id=\"{element_id}\"> dùng camunda:{short}"
                    f"=\"{element.attrib[attr]}\". Java Delegate bị cấm — dùng external task "
                    f"(camunda:type=\"external\" + camunda:topic).",
                    f'camunda:{short}="{element.attrib[attr]}"',
                    f'id="{element_id}"',
                )

    # ── Luật 2 — cấm listener gắn mã Java ─────────────────────────────────────
    for tag in LISTENER_TAGS:
        for listener in root.iter(tag):
            if "class" in listener.attrib or "delegateExpression" in listener.attrib:
                add(
                    "error",
                    "java-listener",
                    f"<camunda:{local(tag)}> gắn class/delegateExpression. Listener chạy mã "
                    f"Java trong tiến trình Camunda — cùng lý do cấm như Java Delegate.",
                    f"camunda:{local(tag)}",
                )

    # ── Luật 3 — cấm phần tử nhúng logic vào mô hình ──────────────────────────
    for tag, reason in FORBIDDEN_TAGS.items():
        for element in root.iter(tag):
            add(
                "error",
                "logic-in-model",
                f"<{local(tag)} id=\"{element.get('id', '')}\"> bị cấm. {reason}",
                f'id="{element.get("id", "")}"',
                local(tag),
            )

    # ── Luật 4 — service task bắt buộc là external task ───────────────────────
    for task in root.iter(f"{{{BPMN_NS}}}serviceTask"):
        task_id = task.get("id", "")
        task_type = task.get(f"{{{CAMUNDA_NS}}}type")
        topic = task.get(f"{{{CAMUNDA_NS}}}topic")

        if task_type != "external":
            add(
                "error",
                "service-task-not-external",
                f"<serviceTask id=\"{task_id}\"> thiếu camunda:type=\"external\" "
                f"(hiện tại: {task_type or 'không khai báo'}).",
                f'id="{task_id}"',
            )
        if not topic:
            add(
                "error",
                "service-task-no-topic",
                f"<serviceTask id=\"{task_id}\"> thiếu camunda:topic. Worker không có "
                f"topic thì không nhận được việc.",
                f'id="{task_id}"',
            )

    # ── Luật 5 — tiến trình phải khai báo đầy đủ ──────────────────────────────
    processes = list(root.iter(f"{{{BPMN_NS}}}process"))
    if not processes:
        add("error", "no-process", "File không chứa <bpmn:process> nào.", "<bpmn:definitions")

    for process in processes:
        process_id = process.get("id", "")
        needle = f'id="{process_id}"'

        if not process_id:
            add("error", "process-no-id", "<bpmn:process> thiếu thuộc tính id.", "<bpmn:process")
            continue

        if not process.get("name"):
            add(
                "error",
                "process-no-name",
                f"<process id=\"{process_id}\"> thiếu thuộc tính name. Cockpit hiển thị "
                f"name — thiếu thì người vận hành nhìn thấy id kỹ thuật.",
                needle,
            )

        if process.get("isExecutable") != "true":
            add(
                "error",
                "process-not-executable",
                f"<process id=\"{process_id}\"> không có isExecutable=\"true\". Camunda "
                f"sẽ deploy nhưng không khởi tạo instance được.",
                needle,
            )

        if not PROCESS_ID_PATTERN.match(process_id):
            add(
                "warning",
                "process-id-convention",
                f"id \"{process_id}\" không theo quy ước P<số>-<ten-co-gach-noi>, "
                f"ví dụ P1-tiep-nhan-van-ban (docs/10).",
                needle,
            )

        # ── Luật 6 — toàn vẹn tham chiếu và tính đóng của luồng ───────────────
        node_ids: set[str] = set()
        for child in process.iter():
            if local(child.tag) in FLOW_NODE_TAGS and child.get("id"):
                node_ids.add(child.get("id", ""))

        starts = list(process.iter(f"{{{BPMN_NS}}}startEvent"))
        ends = list(process.iter(f"{{{BPMN_NS}}}endEvent"))
        if not starts:
            add("error", "no-start-event", f"<process id=\"{process_id}\"> không có startEvent.", needle)
        if not ends:
            add("error", "no-end-event", f"<process id=\"{process_id}\"> không có endEvent.", needle)

        referenced: set[str] = set()
        for flow in process.iter(f"{{{BPMN_NS}}}sequenceFlow"):
            flow_id = flow.get("id", "")
            for side in ("sourceRef", "targetRef"):
                ref = flow.get(side)
                if not ref:
                    add(
                        "error",
                        "flow-missing-ref",
                        f"<sequenceFlow id=\"{flow_id}\"> thiếu {side}.",
                        f'id="{flow_id}"',
                    )
                elif ref not in node_ids:
                    add(
                        "error",
                        "flow-dangling-ref",
                        f"<sequenceFlow id=\"{flow_id}\"> có {side}=\"{ref}\" trỏ tới node "
                        f"không tồn tại trong tiến trình.",
                        f'id="{flow_id}"',
                    )
                else:
                    referenced.add(ref)

        orphans = sorted(node_ids - referenced)
        if len(node_ids) > 1:
            for orphan in orphans:
                add(
                    "error",
                    "orphan-node",
                    f"Node \"{orphan}\" không nối với sequenceFlow nào — nhánh chết trong "
                    f"tiến trình {process_id}.",
                    f'id="{orphan}"',
                )

    return findings


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("targets", nargs="+", help="File .bpmn hoặc thư mục chứa chúng")
    parser.add_argument(
        "--format",
        choices=["text", "github"],
        default="text",
        help="github = xuất annotation ::error file=…,line=…",
    )
    parser.add_argument(
        "--warnings-as-errors",
        action="store_true",
        help="Coi cảnh báo là lỗi (dùng khi đội đã thống nhất quy ước đặt tên).",
    )
    args = parser.parse_args()

    files: list[Path] = []
    for target in args.targets:
        path = Path(target)
        if path.is_dir():
            files.extend(sorted(path.rglob("*.bpmn")))
        elif path.is_file():
            files.append(path)
        else:
            sys.exit(f"validate_bpmn.py: không tìm thấy '{target}'")

    if not files:
        print("Không có file .bpmn nào để kiểm tra.")
        return

    findings: list[Finding] = []
    for file in files:
        findings.extend(check_file(file))

    errors = [f for f in findings if f.level == "error"]
    warnings = [f for f in findings if f.level == "warning"]
    if args.warnings_as_errors:
        errors, warnings = errors + warnings, []

    for finding in findings:
        level = "error" if finding in errors else "warning"
        if args.format == "github":
            print(
                f"::{level} file={finding.file.as_posix()},line={finding.line},"
                f"title={finding.rule}::{finding.message}"
            )
        else:
            print(f"{finding.file}:{finding.line} [{level}] {finding.rule}: {finding.message}")

    print()
    print(f"Đã kiểm tra {len(files)} file .bpmn — {len(errors)} lỗi, {len(warnings)} cảnh báo.")

    if errors:
        sys.exit(1)


if __name__ == "__main__":
    main()
