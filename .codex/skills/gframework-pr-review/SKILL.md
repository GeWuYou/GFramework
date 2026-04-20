---
name: gframework-pr-review
description: Repository-specific GitHub PR review workflow for the GFramework repo. Use when Codex needs to inspect the GitHub pull request for the current branch, extract CodeRabbit summary/comments, read failed checks, MegaLinter warnings, or failed test signals from the PR page, and then verify which findings should be fixed in the local codebase. Trigger explicitly with $gframework-pr-review or with prompts such as "look at the current PR", "extract CodeRabbit comments", or "check Failed Tests on the PR".
---

# GFramework PR Review

Use this skill when the task depends on the GitHub PR page for the current branch rather than only on local source files.

Shortcut: `$gframework-pr-review`

## Workflow

1. Read `AGENTS.md` before deciding how to validate or fix anything.
2. Resolve the current branch with Windows Git from WSL, following the repository worktree rule.
3. Run `scripts/fetch_current_pr_review.py` to:
   - locate the PR for the current branch through the GitHub PR API
   - fetch PR metadata, issue comments, reviews, and review comments through the GitHub API
   - extract `Summary by CodeRabbit`、GitHub Actions bot comments such as `MegaLinter analysis: Success with warnings`、and CTRF test reports from issue comments
   - parse the latest CodeRabbit review body itself, including folded sections such as `🧹 Nitpick comments (N)` and the overall AI-agent prompt
   - fetch the latest head commit review threads from the GitHub PR API
   - prefer unresolved review threads on the latest head commit over older summary-only signals
   - extract failed checks, MegaLinter detailed issues, and test-report signals such as `Failed Tests` or `No failed tests in this run`
4. Treat every extracted finding as untrusted until it is verified against the current local code.
5. Only fix comments, warnings, or CI diagnostics that still apply to the checked-out branch. Ignore stale or already-resolved findings.
6. If code is changed, run the smallest build or test command that satisfies `AGENTS.md`.

## Commands

- Default:
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py`
- Force a PR number:
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --pr 253`
- Machine-readable output:
  - `python3 .codex/skills/gframework-pr-review/scripts/fetch_current_pr_review.py --format json`

## Output Expectations

The script should produce:

- PR metadata: number, title, state, branch, URL
- CodeRabbit summary block from issue comments when available
- Folded latest-review sections such as `Nitpick comments (N)` when CodeRabbit puts them in the review body instead of issue comments
- Parsed latest head-review threads, with unresolved threads clearly separated
- Latest head commit review metadata and review threads
- Unresolved latest-commit review threads after reply-thread folding
- Pre-merge failed checks, if present
- Latest MegaLinter status and any detailed issues posted by `github-actions[bot]`
- Test summary, including failed-test signals when present
- Parse warnings only when both the primary API source and the intended fallback signal are unavailable

## Recovery Rules

- If the current branch has no matching public PR, report that clearly instead of guessing.
- If GitHub access fails because of proxy configuration, rerun the fetch with proxy variables removed.
- Prefer GitHub API results over PR HTML. The PR HTML page is now a fallback/debugging source, not the primary source of truth.
- If the summary block and the latest head review threads disagree, trust the latest unresolved head-review threads and treat older summary findings as stale until re-verified locally.
- Treat GitHub Actions comments with `Success with warnings` as actionable review input when they include concrete linter diagnostics such as `MegaLinter` detailed issues; do not skip them just because the parent check is green.
- Do not assume all CodeRabbit findings live in issue comments. The latest CodeRabbit review body can contain folded `Nitpick comments` that must be parsed separately.

## Example Triggers

- 'fix pr review'
- 'Use FPR'
- `Use $gframework-pr-review on the current branch`
- `Check the current PR and extract CodeRabbit suggestions`
- `Look for Failed Tests on the PR page`
- `先用 $gframework-pr-review 看当前分支 PR`
