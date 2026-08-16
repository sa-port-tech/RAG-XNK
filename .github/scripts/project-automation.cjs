/**
 * Tự động hoá GitHub Projects v2 — docs/00 §8.2 và §8.3.
 *
 * Board có sáu cột: Backlog → Todo → In Progress → In Review → Testing → Done.
 * Năm chuyển trạng thái dưới đây phải tự xảy ra, vì cột nào cần người nhớ kéo tay thì
 * cột đó sẽ sai sau tuần thứ hai và board mất giá trị:
 *
 *   ① issue mở              → thêm vào Project, cột Backlog, điền custom field
 *   ③ branch tạo từ issue   → In Progress
 *   ④ PR mở, có Closes #n   → In Review
 *   ⑥ PR merge              → Testing
 *   ⑦ deploy dev thành công → Done
 *
 * (② Sprint Planning là bước con người kéo Backlog → Todo, cố ý không tự động.)
 *
 * Điểm khiến việc này không tầm thường: REST API không đụng được Projects v2, tất cả
 * phải đi qua GraphQL, và GITHUB_TOKEN mặc định KHÔNG có quyền ghi Project của org.
 * Vì vậy workflow truyền vào một token riêng (secrets.PROJECT_TOKEN).
 *
 * Custom field được lấy từ chính câu trả lời trong Issue Form. Đây là lý do docs/00
 * §8.2 bắt buộc dùng Issue Form YAML thay vì markdown tự do: markdown tự do thì không
 * có gì để đọc ra field cả.
 */

"use strict";

const STATUS_FIELD = "Status";

/** Ánh xạ tiêu đề trong Issue Form → tên custom field trên Project. */
const FIELD_MAP = [
  { headings: ["Priority"], field: "Priority", kind: "select", normalize: firstToken },
  { headings: ["Mức ảnh hưởng"], field: "Priority", kind: "select", normalize: firstToken },
  { headings: ["Component"], field: "Component", kind: "select" },
  { headings: ["Phase"], field: "Phase", kind: "select" },
  { headings: ["Story Points"], field: "Story Points", kind: "number" },
  { headings: ["Expert Review Required"], field: "Expert Review Required", kind: "select" },
];

/** "P0 — Hệ thống đang trích dẫn…" → "P0" */
function firstToken(value) {
  return value.split(/[\s—-]/)[0].trim();
}

/**
 * Tách phần thân issue do Issue Form sinh ra thành map { tiêu đề: giá trị }.
 *
 * Issue Form luôn kết xuất theo dạng "### Nhãn\n\ngiá trị", nên cấu trúc này ổn định.
 * Trường người dùng bỏ trống được GitHub điền "_No response_" — coi như không có.
 */
function parseIssueForm(body) {
  const result = {};
  if (!body) return result;

  const sections = body.split(/^###\s+/m).slice(1);
  for (const section of sections) {
    const newline = section.indexOf("\n");
    if (newline === -1) continue;
    const heading = section.slice(0, newline).trim();
    const value = section.slice(newline + 1).trim();
    if (!value || value === "_No response_") continue;
    result[heading] = value;
  }
  return result;
}

async function loadProject(github, org, projectNumber) {
  const data = await github.graphql(
    `query($org: String!, $number: Int!) {
       organization(login: $org) {
         projectV2(number: $number) {
           id
           title
           fields(first: 50) {
             nodes {
               __typename
               ... on ProjectV2Field { id name dataType }
               ... on ProjectV2SingleSelectField { id name options { id name } }
               ... on ProjectV2IterationField { id name }
             }
           }
         }
       }
     }`,
    { org, number: projectNumber }
  );

  const project = data.organization && data.organization.projectV2;
  if (!project) {
    throw new Error(
      `Không tìm thấy Project số ${projectNumber} của org ${org}. ` +
        `Kiểm tra biến PROJECT_NUMBER và quyền của PROJECT_TOKEN.`
    );
  }

  const fields = new Map();
  for (const node of project.fields.nodes) {
    if (node && node.name) fields.set(node.name, node);
  }
  return { id: project.id, title: project.title, fields };
}

async function addToProject(github, projectId, contentId) {
  // Mutation này idempotent: gọi lại với item đã có sẽ trả về đúng item cũ.
  const data = await github.graphql(
    `mutation($project: ID!, $content: ID!) {
       addProjectV2ItemById(input: { projectId: $project, contentId: $content }) {
         item { id }
       }
     }`,
    { project: projectId, content: contentId }
  );
  return data.addProjectV2ItemById.item.id;
}

async function setField(github, core, project, itemId, fieldName, rawValue, kind) {
  const field = project.fields.get(fieldName);
  if (!field) {
    core.warning(`Project không có field "${fieldName}" — bỏ qua giá trị "${rawValue}".`);
    return;
  }

  let value;
  if (kind === "select") {
    const options = field.options || [];
    // So khớp không phân biệt hoa thường và bỏ qua khoảng trắng thừa, vì nhãn trên
    // Issue Form và tên option trên Project do hai người khác nhau gõ.
    const wanted = String(rawValue).trim().toLowerCase();
    const option =
      options.find((o) => o.name.trim().toLowerCase() === wanted) ||
      options.find((o) => o.name.trim().toLowerCase().startsWith(wanted));
    if (!option) {
      core.warning(
        `Field "${fieldName}" không có lựa chọn khớp "${rawValue}". ` +
          `Các lựa chọn hiện có: ${options.map((o) => o.name).join(", ")}`
      );
      return;
    }
    value = { singleSelectOptionId: option.id };
  } else if (kind === "number") {
    const parsed = Number(String(rawValue).replace(",", "."));
    if (Number.isNaN(parsed)) {
      core.warning(`Field "${fieldName}" cần số, nhận được "${rawValue}".`);
      return;
    }
    value = { number: parsed };
  } else {
    value = { text: String(rawValue) };
  }

  await github.graphql(
    `mutation($project: ID!, $item: ID!, $field: ID!, $value: ProjectV2FieldValue!) {
       updateProjectV2ItemFieldValue(
         input: { projectId: $project, itemId: $item, fieldId: $field, value: $value }
       ) { projectV2Item { id } }
     }`,
    { project: project.id, item: itemId, field: field.id, value }
  );

  core.info(`  ${fieldName} = ${rawValue}`);
}

async function findIssue(github, owner, repo, number) {
  const data = await github.graphql(
    `query($owner: String!, $repo: String!, $number: Int!) {
       repository(owner: $owner, name: $repo) {
         issue(number: $number) {
           id
           title
           projectItems(first: 20) { nodes { id project { id number } } }
         }
       }
     }`,
    { owner, repo, number }
  );
  return data.repository.issue;
}

async function closingIssuesOfPullRequest(github, owner, repo, number) {
  const data = await github.graphql(
    `query($owner: String!, $repo: String!, $number: Int!) {
       repository(owner: $owner, name: $repo) {
         pullRequest(number: $number) {
           closingIssuesReferences(first: 20) { nodes { number } }
         }
       }
     }`,
    { owner, repo, number }
  );
  const pr = data.repository.pullRequest;
  return pr ? pr.closingIssuesReferences.nodes.map((n) => n.number) : [];
}

async function closingIssuesOfCommit(github, owner, repo, oid) {
  const data = await github.graphql(
    `query($owner: String!, $repo: String!, $oid: GitObjectID!) {
       repository(owner: $owner, name: $repo) {
         object(oid: $oid) {
           ... on Commit {
             associatedPullRequests(first: 10) {
               nodes { closingIssuesReferences(first: 20) { nodes { number } } }
             }
           }
         }
       }
     }`,
    { owner, repo, oid }
  );

  const object = data.repository.object;
  if (!object || !object.associatedPullRequests) return [];

  const numbers = new Set();
  for (const pr of object.associatedPullRequests.nodes) {
    for (const issue of pr.closingIssuesReferences.nodes) numbers.add(issue.number);
  }
  return [...numbers];
}

/** Đưa issue vào Project (nếu chưa có) rồi đặt cột Status. */
async function moveIssue(github, core, project, owner, repo, issueNumber, status) {
  const issue = await findIssue(github, owner, repo, issueNumber);
  if (!issue) {
    core.warning(`Không tìm thấy issue #${issueNumber}.`);
    return null;
  }

  let item = issue.projectItems.nodes.find((n) => n.project.id === project.id);
  let itemId = item ? item.id : await addToProject(github, project.id, issue.id);

  await setField(github, core, project, itemId, STATUS_FIELD, status, "select");
  core.info(`#${issueNumber} "${issue.title}" → ${status}`);
  return itemId;
}

/** Số issue nằm ở đầu tên nhánh do GitHub sinh từ nút "Create a branch". */
function issueNumberFromBranch(ref) {
  const match = /^(\d+)-/.exec(ref || "");
  return match ? Number(match[1]) : null;
}

module.exports = async ({ github, context, core, org, projectNumber, deployStatus }) => {
  const project = await loadProject(github, org, Number(projectNumber));
  core.info(`Project: ${project.title} (#${projectNumber})`);

  const owner = context.repo.owner;
  const repo = context.repo.repo;
  const event = context.eventName;
  const payload = context.payload;

  // ── ① Issue mở → Backlog + điền custom field từ Issue Form ─────────────────
  if (event === "issues" && (payload.action === "opened" || payload.action === "reopened")) {
    const issue = payload.issue;
    const itemId = await addToProject(github, project.id, issue.node_id);
    await setField(github, core, project, itemId, STATUS_FIELD, "Backlog", "select");

    const answers = parseIssueForm(issue.body);
    for (const rule of FIELD_MAP) {
      for (const heading of rule.headings) {
        if (answers[heading] === undefined) continue;
        const raw = rule.normalize ? rule.normalize(answers[heading]) : answers[heading];
        await setField(github, core, project, itemId, rule.field, raw, rule.kind);
        break;
      }
    }
    return;
  }

  // ── ③ Tạo branch từ issue → In Progress ────────────────────────────────────
  if (event === "create" && payload.ref_type === "branch") {
    const issueNumber = issueNumberFromBranch(payload.ref);
    if (!issueNumber) {
      core.info(
        `Nhánh "${payload.ref}" không bắt đầu bằng số issue — bỏ qua. ` +
          `Dùng nút "Create a branch" ngay trên issue để có liên kết hai chiều.`
      );
      return;
    }
    await moveIssue(github, core, project, owner, repo, issueNumber, "In Progress");
    return;
  }

  // ── ④⑥ PR mở → In Review · PR merge → Testing ──────────────────────────────
  if (event === "pull_request") {
    const pr = payload.pull_request;
    const merged = payload.action === "closed" && pr.merged;
    const opened = ["opened", "reopened", "ready_for_review"].includes(payload.action);

    if (!merged && !opened) return;

    const issues = await closingIssuesOfPullRequest(github, owner, repo, pr.number);
    if (issues.length === 0) {
      core.warning(
        `PR #${pr.number} không liên kết issue nào. Thêm "Closes #<số>" vào mô tả PR, ` +
          `nếu không issue sẽ không tự chuyển cột (docs/00 §8.3 ④).`
      );
      return;
    }

    const status = merged ? "Testing" : "In Review";
    for (const number of issues) {
      await moveIssue(github, core, project, owner, repo, number, status);
    }
    return;
  }

  // ── ⑦ Deploy thành công → Done ─────────────────────────────────────────────
  if (event === "workflow_run") {
    const run = payload.workflow_run;
    if (run.conclusion !== "success") {
      core.info(`cd-deploy kết thúc với "${run.conclusion}" — không chuyển issue sang Done.`);
      return;
    }

    const issues = await closingIssuesOfCommit(github, owner, repo, run.head_sha);
    if (issues.length === 0) {
      core.info(`Không có issue nào gắn với commit ${run.head_sha}.`);
      return;
    }

    for (const number of issues) {
      await moveIssue(github, core, project, owner, repo, number, deployStatus || "Done");
    }
    return;
  }

  core.info(`Sự kiện "${event}" không có quy tắc tương ứng — không làm gì.`);
};

// Xuất thêm để kiểm thử được từng phần mà không cần gọi API.
module.exports.parseIssueForm = parseIssueForm;
module.exports.issueNumberFromBranch = issueNumberFromBranch;
module.exports.firstToken = firstToken;
