/**
 * Kiểm tra chính sách pull request mà ruleset của GitHub không diễn đạt được.
 *
 * Ba luật:
 *
 *   1. Tiêu đề PR theo Conventional Commits.
 *      Vì squash merge lấy tiêu đề PR làm thông điệp commit, và GitHub Release
 *      `--generate-notes` dựng changelog từ đó (docs/00 §8.3 ⑦). Tiêu đề rác hôm nay
 *      là changelog rác vĩnh viễn.
 *
 *   2. PR phải liên kết ít nhất một issue.
 *      Không có liên kết thì issue không tự chuyển cột và board mất chính xác
 *      (docs/00 §8.3 ④⑥).
 *
 *   3. Chạm `infra/` hoặc `db/migrations/` thì cần ≥2 approval.
 *      docs/01 §5.2 yêu cầu điều này. Ruleset của GitHub chỉ đặt được MỘT con số
 *      approval cho cả nhánh, không phân biệt theo đường dẫn — nên luật này phải
 *      sống ở đây. Đặt số 2 cho toàn bộ repo là cách sai: nó bắt mọi PR sửa lỗi
 *      chính tả cũng phải chờ hai người, và đội sẽ học cách duyệt cho xong.
 */

"use strict";

const CONVENTIONAL = /^(feat|fix|docs|refactor|perf|test|build|ci|chore|revert)(\([a-z0-9][a-z0-9._/-]*\))?(!)?: .+/;

async function changedFiles(github, owner, repo, number) {
  const files = await github.paginate(github.rest.pulls.listFiles, {
    owner,
    repo,
    pull_number: number,
    per_page: 100,
  });
  return files.map((f) => f.filename);
}

async function linkedIssues(github, owner, repo, number) {
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

/**
 * Đếm approval còn hiệu lực.
 *
 * Một người có thể review nhiều lần; chỉ lần cuối cùng có ý nghĩa. Review dạng
 * COMMENTED không thay đổi lập trường nên bị bỏ qua khi tính trạng thái sau cùng —
 * nếu tính, một người đã APPROVED rồi comment thêm sẽ bị mất phiếu.
 */
async function countApprovals(github, owner, repo, number, prAuthor) {
  const reviews = await github.paginate(github.rest.pulls.listReviews, {
    owner,
    repo,
    pull_number: number,
    per_page: 100,
  });

  const latest = new Map();
  for (const review of reviews) {
    if (!review.user) continue;
    if (review.user.login === prAuthor) continue; // tự duyệt PR của mình không tính
    if (review.state === "COMMENTED" || review.state === "PENDING") continue;
    latest.set(review.user.login, review.state);
  }

  const approvers = [...latest.entries()]
    .filter(([, state]) => state === "APPROVED")
    .map(([login]) => login);

  return approvers;
}

module.exports = async ({ github, context, core, config }) => {
  const owner = context.repo.owner;
  const repo = context.repo.repo;
  const pr = context.payload.pull_request;

  if (!pr) {
    core.setFailed("Workflow này chỉ chạy trên sự kiện pull_request.");
    return;
  }

  const problems = [];
  const notes = [];

  // ── Luật 1 — tiêu đề ──────────────────────────────────────────────────────
  if (config.requireConventionalTitle) {
    if (!CONVENTIONAL.test(pr.title)) {
      problems.push(
        `Tiêu đề PR không theo Conventional Commits.\n` +
          `   Hiện tại: "${pr.title}"\n` +
          `   Định dạng: <type>(<scope>): <mô tả>\n` +
          `   type hợp lệ: feat, fix, docs, refactor, perf, test, build, ci, chore, revert\n` +
          `   Ví dụ: feat(retrieval): lọc chunk theo ngày hiệu lực`
      );
    } else {
      notes.push(`Tiêu đề đúng định dạng: \`${pr.title}\``);
    }
  }

  // ── Luật 2 — liên kết issue ───────────────────────────────────────────────
  if (config.requireLinkedIssue) {
    const issues = await linkedIssues(github, owner, repo, pr.number);
    if (issues.length === 0) {
      problems.push(
        `PR không liên kết issue nào.\n` +
          `   Thêm một dòng "Closes #<số>" vào phần mô tả PR.\n` +
          `   Nhắc tới issue mà không có từ khoá đóng thì GitHub không coi là liên kết.`
      );
    } else {
      notes.push(`Liên kết issue: ${issues.map((n) => `#${n}`).join(", ")}`);
    }
  }

  // ── Luật 3 — số approval theo đường dẫn ───────────────────────────────────
  const files = await changedFiles(github, owner, repo, pr.number);
  const touched = config.elevatedPaths.filter((prefix) =>
    files.some((file) => file.startsWith(prefix))
  );

  // Chỉ kiểm tra approval khi PR chạm đường dẫn cần mức CAO HƠN mức của ruleset.
  //
  // Mức nền (≥1 approval) đã do ruleset thực thi ở phía GitHub. Kiểm tra lại ở đây
  // không thêm gì, nhưng gây ra một hậu quả thật: PR nào cũng có một lượt chạy đỏ ở
  // thời điểm vừa mở, lúc chưa ai kịp review. Approval sau đó tạo ra một check run
  // THỨ HAI cùng tên trên cùng commit, và lượt đỏ cũ nằm lại vĩnh viễn trong rollup.
  //
  // Nói cách khác: mỗi PR đều mang sẵn một dấu đỏ không bao giờ xoá được, chỉ vì một
  // phép kiểm tra thừa. Nên bỏ hẳn phần trùng lặp và giữ đúng phần ruleset không
  // diễn đạt được — ngưỡng 2 approval theo đường dẫn.
  if (touched.length === 0) {
    notes.push(
      `Không chạm ${config.elevatedPaths.map((p) => `\`${p}\``).join(" hay ")} → ` +
        `áp mức approval mặc định của ruleset (${config.defaultApprovals}).`
    );
  } else {
    const required = config.elevatedApprovals;
    const approvers = await countApprovals(github, owner, repo, pr.number, pr.user.login);

    notes.push(`PR chạm ${touched.map((p) => `\`${p}\``).join(", ")} → cần ${required} approval.`);

    if (approvers.length < required) {
      problems.push(
        `Mới có ${approvers.length}/${required} approval` +
          (approvers.length ? ` (${approvers.join(", ")})` : "") +
          `.\n   PR chạm ${touched.join(", ")} — đây là thay đổi hạ tầng hoặc lược đồ dữ liệu, ` +
          `hỏng thì không revert bằng một commit được (docs/01 §5.2).`
      );
    } else {
      notes.push(`Đủ approval: ${approvers.length}/${required} — ${approvers.join(", ")}`);
    }
  }

  // ── Kết luận ──────────────────────────────────────────────────────────────
  const summary = core.summary.addHeading("Cổng chính sách pull request", 3);

  if (notes.length) {
    summary.addList(notes.map((n) => n.replace(/\n\s+/g, " ")));
  }

  if (problems.length) {
    summary.addHeading("Chưa đạt", 4);
    summary.addList(problems.map((p) => p.split("\n")[0]));
    await summary.write();

    for (const problem of problems) {
      core.error(problem);
    }
    core.setFailed(`${problems.length} luật chưa đạt — xem chi tiết ở trên.`);
    return;
  }

  summary.addRaw("\n✅ Toàn bộ luật chính sách PR đã đạt.\n");
  await summary.write();
  core.info("pr-governance đạt.");
};

module.exports.CONVENTIONAL = CONVENTIONAL;
module.exports.countApprovals = countApprovals;
