---
name: workflow-review
description: Orchestrate a code review across correctness, security, testing and architecture lenses, then walk the engineer through each finding for disposition. Use when a branch is ready for review or a PR is about to be opened.
disable-model-invocation: true
argument-hint: "[issue number | branch name | 'latest']"
---

# workflow-review

Orchestrates a multi-lens code review, then walks the engineer through every finding for disposition.

This skill produces findings and dispositions. It does **not** decide whether the work ships — that is `/gate-check`'s job, and keeping the two separate is what stops a reviewer's judgement from silently becoming an approval.

Read [`_shared/references/conventions.md`](../_shared/references/conventions.md) before running.

---

## Phase 0 — Gather the change and its context

Determine the base branch. Gather the diff and the changed-file list **into a temp directory** — not into context.

Read **the handover doc** in `docs/handovers/`. It tells you where the blast radius is, which is where the review should spend its attention.

**Read the execution mode. It changes what matters.**

| Mode | What the review asks first |
|---|---|
| `TDD` | Is the new behaviour correct, and is it actually proven? |
| `characterization` | **Did behaviour change?** Before anything else. |
| `mutation-verified` | **Do these tests actually assert something?** |
| `documentation` | Do the instructions work as written, from a clean state? |

A refactor review that opens with naming suggestions has already missed the only question that mattered.

---

## Phase 1 — Discover and select agents

Glob `.claude/agents/review-*.md`. Match each agent's frontmatter description against the change surface.

**Announce which agents you selected AND which you skipped, and why.**

> A silent skip is indistinguishable from a clean result.

A report listing three lenses with no findings reads as three lenses that found nothing — unless it says the fourth was never run.

---

## Phase 2 — Dispatch in parallel

Dispatch the selected agents **in parallel** via the Task tool, using the **real `subagent_type`** for each — `review-correctness`, `review-security`, `review-testing`, `review-architecture` — so their frontmatter model and tool restrictions actually apply. Dispatching them as generic subagents silently discards the tool restrictions that stop them writing files.

Each agent returns **exactly one JSON object** as its final message and writes no files.

### Verification and retry

- Verify each response **parses** and carries `status: "complete"`.
- **Retry a failed agent exactly once — a single retry, not a loop.**
- **Record any agent still failing rather than dropping it silently, and say so in the report.** An agent that crashed and an agent that found nothing produce the same empty section otherwise.

### Normalisation

| Agent severity | Normalised |
|---|---|
| `critical` | critical |
| `warning` | high |
| `suggestion` | medium |

**Deduplicate findings naming the same file and line.** Two lenses reaching the same place is corroboration, not two problems — keep the more severe, and note that both raised it.

---

## Phase 3 — Disposition

Walk the engineer through each finding, **most severe first**. For each, offer:

- **addressed** — record the **commit SHA**.
- **deferred** — **requires a written reason.**
- **ignored** — **requires a written reason.**

**That reason is the audit trail. A finding dismissed without one is indistinguishable from a finding nobody read.** Six months later the question is never "was this seen?" — it is "what was the thinking?", and only the reason answers it.

---

## Phase 4 — Write the review record

Write `docs/reviews/YYYY-MM-DD-<issue>.json`:

```json
{
  "metadata": {
    "timestamp": "<ISO 8601>",
    "branch": "<name>",
    "commit": "<sha>",
    "files_reviewed": ["<path>"],
    "execution_mode": "<mode>",
    "agents_run": ["review-correctness"],
    "agents_skipped": [{ "agent": "review-security", "reason": "<why>" }],
    "agents_failed": [{ "agent": "<name>", "reason": "<why>" }]
  },
  "findings": [
    {
      "id": "COR-001",
      "severity": "critical",
      "file": "src/...",
      "line": 42,
      "title": "",
      "description": "",
      "suggestion": "",
      "code_snippet": "",
      "disposition": "addressed",
      "reason": "",
      "commit": "<sha if addressed>"
    }
  ],
  "summary": {
    "by_severity": { "critical": 0, "high": 0, "medium": 0, "low": 0 },
    "by_disposition": { "addressed": 0, "deferred": 0, "ignored": 0 }
  }
}
```

---

## Phase 5 — Label and hand on

```bash
gh issue edit <n> --add-label "status:in-review" --remove-label "status:in-progress"
```

Add and remove in the same call — an issue carrying two status labels has no state.

Report the counts by severity and by disposition, and any agent that failed. Suggest `/gate-check <n>`.

**Do not declare the work shippable.** That is the gate's call, not the reviewer's. A review that ends "looks good to merge" has pre-empted the step that checks AC proof, requirement coverage, standards version, and approvals — none of which this skill looked at.

---

## Constraints

- Do not fix findings during review. Review and remediation are separate passes.
- Do not let an agent write files.
- Do not skip an agent silently.
- Do not declare the work ready to merge — that is `/gate-check`'s job.
- Do not retry a failed agent more than once.
- Do not pull the full diff into main context; work from the temp directory.
