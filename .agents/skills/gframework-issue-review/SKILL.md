---
name: gframework-issue-review
description: Repository-specific GitHub issue triage workflow for the GFramework repo. Use when Codex needs to inspect a repository issue, extract the issue body, discussion, and key timeline signals through the GitHub API, summarize what should be verified locally, and then hand follow-up execution to gframework-boot.
---

# GFramework Issue Review

Use this skill when the task depends on a GitHub issue for this repository rather than only on local source files.

Shortcut: `$gframework-issue-review`

## Workflow

1. Read `AGENTS.md` before deciding how to validate or change anything.
2. Read `.ai/environment/tools.ai.yaml` and `ai-plan/public/README.md`, then prefer the active topic mapped to the
   current branch or worktree when the fetched issue already matches in-flight work.
3. Run `scripts/fetch_current_issue_review.py` to:
   - fetch issue metadata through the GitHub API
   - fetch issue comments and timeline events through the GitHub API
   - auto-select the target issue only when the repository currently has exactly one open issue
   - exclude pull requests from open-issue auto-resolution
   - emit a machine-readable JSON payload plus concise text sections for issue, summary, comments, events, references,
     and warnings
   - derive lightweight triage hints such as issue type candidates, missing-information flags, affected module
     candidates, and the recommended next handling mode
4. Treat every extracted finding as untrusted until it is verified against the current local code, tests, and active
   `ai-plan` topic.
5. Do not start editing code from the issue text alone. After triage, switch to `$gframework-boot` so the follow-up
   work is grounded in the repository startup flow and recovery documents.
6. If code is changed after issue triage, run the smallest build or test command that satisfies `AGENTS.md`.

## Commands

- Default:
  - `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py`
- Force a specific issue:
  - `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --issue 312`
- Machine-readable output:
  - `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --format json`
- Write machine-readable output to a file instead of stdout:
  - `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --issue 312 --format json --json-output /tmp/issue312-review.json`
- Inspect only a high-signal section:
  - `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --section summary`
- Combine triage with a boot handoff:
  - `python3 .agents/skills/gframework-issue-review/scripts/fetch_current_issue_review.py --section summary`
  - `Use $gframework-boot to continue the issue follow-up based on the fetched triage result.`

## Output Expectations

The script should produce:

- Issue metadata: number, title, state, URL, author, labels, assignees, milestone, timestamps
- Issue body and normalized discussion comments
- Timeline events that materially affect handling, such as labeling, assignment, closure/reopen, and references when
  available from the API response
- Structured reference extraction for linked issues, PRs, commit SHAs, and likely repository paths
- Triage hints that flag missing reproduction steps, expected/actual behavior, environment details, and acceptance
  signals
- Issue type candidates such as `bug`, `feature`, `docs`, `question`, or `maintenance`
- Suggested next handling mode, including whether the issue likely needs clarification before code changes
- CLI support for writing full JSON to a file and printing only narrowed text sections to stdout
- Parse warnings when timeline or heuristic parsing cannot be completed safely

## Recovery Rules

- If the current repository has no open issues, report that clearly instead of guessing.
- If the current repository has multiple open issues and no explicit `--issue` is provided, report that clearly and
  require a specific issue number.
- If GitHub access fails because of proxy configuration, rerun the fetch with proxy variables removed.
- Prefer GitHub API results over HTML scraping.
- Do not treat heuristic module guesses or next-step suggestions as repository truth; they are only entry points for
  subsequent local verification.
- If the issue discussion reveals that the problem statement has already shifted, prefer the newest concrete comment or
  timeline signal over the original title/body wording.
- After extracting the issue, continue the actual implementation flow with `$gframework-boot` so the task is grounded
  in current branch context and `ai-plan` recovery artifacts.

## Example Triggers

- `Use $gframework-issue-review on the current repository issue`
- `Check the open GitHub issue and summarize what should be verified locally`
- `Inspect issue 312 and tell me whether this looks like bug triage or a feature request`
- `先用 $gframework-issue-review 看当前 open issue，再用 $gframework-boot 继续`
