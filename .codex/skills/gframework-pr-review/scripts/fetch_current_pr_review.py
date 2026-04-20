#!/usr/bin/env python3
"""
Fetch the GitHub PR page for the current GFramework branch and extract the
signals needed for local follow-up work without relying on gh CLI.
"""

from __future__ import annotations

import argparse
import html
import json
import os
import re
import shutil
import subprocess
import sys
import urllib.parse
import urllib.request
from typing import Any

OWNER = "GeWuYou"
REPO = "GFramework"
DEFAULT_WINDOWS_GIT = "/mnt/d/Tool/Development Tools/Git/cmd/git.exe"
GIT_ENVIRONMENT_KEY = "GFRAMEWORK_WINDOWS_GIT"
USER_AGENT = "codex-gframework-pr-review"
CODERABBIT_LOGIN = "coderabbitai[bot]"
GITHUB_ACTIONS_LOGIN = "github-actions[bot]"
REVIEW_COMMENT_ADDRESSED_MARKER = "<!-- <review_comment_addressed> -->"
VISIBLE_ADDRESSED_IN_COMMIT_PATTERN = re.compile(r"✅\s*Addressed in commit\s+[0-9a-f]{7,40}", re.I)
DEFAULT_REQUEST_TIMEOUT_SECONDS = 60
REQUEST_TIMEOUT_ENVIRONMENT_KEY = "GFRAMEWORK_PR_REVIEW_TIMEOUT_SECONDS"


def resolve_git_command() -> str:
    candidates = [
        os.environ.get(GIT_ENVIRONMENT_KEY),
        DEFAULT_WINDOWS_GIT,
        "git.exe",
        "git",
    ]

    for candidate in candidates:
        if not candidate:
            continue

        if os.path.isabs(candidate):
            if os.path.exists(candidate):
                return candidate
            continue

        resolved_candidate = shutil.which(candidate)
        if resolved_candidate:
            return resolved_candidate

    raise RuntimeError(f"No usable git executable found. Set {GIT_ENVIRONMENT_KEY} to override it.")


def resolve_request_timeout_seconds() -> int:
    configured_timeout = os.environ.get(REQUEST_TIMEOUT_ENVIRONMENT_KEY)
    if not configured_timeout:
        return DEFAULT_REQUEST_TIMEOUT_SECONDS

    try:
        parsed_timeout = int(configured_timeout)
    except ValueError as error:
        raise RuntimeError(
            f"{REQUEST_TIMEOUT_ENVIRONMENT_KEY} must be an integer number of seconds."
        ) from error

    if parsed_timeout <= 0:
        raise RuntimeError(f"{REQUEST_TIMEOUT_ENVIRONMENT_KEY} must be greater than zero.")

    return parsed_timeout


def run_command(args: list[str]) -> str:
    process = subprocess.run(args, capture_output=True, text=True, check=False)
    if process.returncode != 0:
        stderr = process.stderr.strip()
        raise RuntimeError(f"Command failed: {' '.join(args)}\n{stderr}")
    return process.stdout.strip()


def get_current_branch() -> str:
    return run_command([resolve_git_command(), "rev-parse", "--abbrev-ref", "HEAD"])


def open_url(url: str, accept: str) -> tuple[str, Any]:
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    request = urllib.request.Request(url, headers={"Accept": accept, "User-Agent": USER_AGENT})
    with opener.open(request, timeout=resolve_request_timeout_seconds()) as response:
        return response.read().decode("utf-8", "replace"), response.headers


def fetch_json(url: str) -> tuple[Any, Any]:
    text, headers = open_url(url, accept="application/vnd.github+json")
    return json.loads(text), headers


def extract_next_link(headers: Any) -> str | None:
    link_header = headers.get("Link")
    if not link_header:
        return None

    match = re.search(r'<([^>]+)>;\s*rel="next"', link_header)
    return match.group(1) if match else None


def fetch_paged_json(url: str) -> list[dict[str, Any]]:
    items: list[dict[str, Any]] = []
    next_url: str | None = url
    while next_url:
        payload, headers = fetch_json(next_url)
        if not isinstance(payload, list):
            raise RuntimeError(f"Expected list payload from GitHub API, got {type(payload).__name__}.")

        items.extend(payload)
        next_url = extract_next_link(headers)

    return items


def fetch_pull_request_metadata(pr_number: int) -> dict[str, Any]:
    payload, _ = fetch_json(f"https://api.github.com/repos/{OWNER}/{REPO}/pulls/{pr_number}")
    if not isinstance(payload, dict):
        raise RuntimeError("Failed to fetch GitHub PR metadata.")

    return {
        "number": int(payload["number"]),
        "title": payload["title"],
        "state": str(payload["state"]).upper(),
        "head_branch": payload["head"]["ref"],
        "base_branch": payload["base"]["ref"],
        "url": payload["html_url"],
    }


def resolve_pr_number(branch: str) -> int:
    head_query = urllib.parse.quote(f"{OWNER}:{branch}")
    payload, _ = fetch_json(f"https://api.github.com/repos/{OWNER}/{REPO}/pulls?state=all&head={head_query}")
    if not isinstance(payload, list):
        raise RuntimeError("Failed to resolve pull request from branch.")

    matching_pull_requests = [item for item in payload if item.get("head", {}).get("ref") == branch]
    if not matching_pull_requests:
        raise RuntimeError(f"No public PR matched branch '{branch}'.")

    latest_pull_request = max(matching_pull_requests, key=lambda item: item.get("updated_at", ""))
    return int(latest_pull_request["number"])


def collapse_whitespace(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


def strip_tags(text: str) -> str:
    return collapse_whitespace(re.sub(r"<[^>]+>", " ", text))


def strip_markdown_links(text: str) -> str:
    return re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", text)


def extract_section(text: str, start_marker: str, end_markers: list[str]) -> str | None:
    start = text.find(start_marker)
    if start < 0:
        return None

    end = len(text)
    for marker in end_markers:
        marker_index = text.find(marker, start + len(start_marker))
        if marker_index >= 0:
            end = min(end, marker_index)

    return text[start:end].strip()


def parse_failed_checks(summary_block: str) -> list[dict[str, str]]:
    failed_section = extract_section(
        summary_block,
        "### ❌ Failed checks",
        ["<details>\n<summary>✅ Passed checks", "<sub>", "<!-- pre_merge_checks_walkthrough_end -->"],
    )
    if failed_section is None:
        return []

    rows: list[dict[str, str]] = []
    for line in failed_section.splitlines():
        stripped = line.strip()
        if not stripped.startswith("|") or "Check name" in stripped or stripped.startswith("| :"):
            continue

        parts = [part.strip() for part in stripped.strip("|").split("|")]
        if len(parts) != 4:
            continue

        rows.append(
            {
                "name": parts[0],
                "status": parts[1],
                "explanation": parts[2],
                "resolution": parts[3],
            }
        )

    return rows


def parse_actionable_comments(actionable_block: str) -> dict[str, Any]:
    comment_count_match = re.search(r"Actionable comments posted:\s*(\d+)", actionable_block)
    count = int(comment_count_match.group(1)) if comment_count_match else 0

    primary_block = actionable_block.split(
        "<details>\n<summary>🤖 Prompt for all review comments with AI agents</summary>",
        1,
    )[0]
    comments = parse_comment_cards(primary_block)

    prompt_match = re.search(
        r"<summary>🤖 Prompt for all review comments with AI agents</summary>\s*```(.*?)```",
        actionable_block,
        re.S,
    )

    return {
        "count": count or len(comments),
        "comments": comments,
        "all_comments_prompt": prompt_match.group(1).strip() if prompt_match else "",
        "raw": actionable_block.strip(),
    }


def parse_comment_cards(comment_block: str) -> list[dict[str, str]]:
    comments: list[dict[str, str]] = []
    pattern = re.compile(
        r"<summary>"
        # CodeRabbit can fold cards for source, docs, scripts, and repo config files.
        # Keep the matcher path-like, but do not hardcode a tiny extension allow-list
        # or we will silently drop valid findings such as .py skill files.
        r"((?:[^<\n]+/)*[^<\n/]+(?:\.[A-Za-z0-9._-]+)+|AGENTS\.md|CLAUDE\.md|README\.md|\.gitignore)"
        r" \((\d+)\)</summary><blockquote>\s*(.*?)\s*(?:(?:</blockquote></details>)|(?:</blockquote>))",
        re.S,
    )

    for path, _, body in pattern.findall(comment_block):
        finding_match = re.search(r"`([^`]+)`: \*\*(.*?)\*\*", body, re.S)
        prompt_match = re.search(r"<summary>🤖 Prompt for AI Agents</summary>\s*```(.*?)```", body, re.S)
        suggestion_match = re.search(r"<summary>✏️ 建议文案调整</summary>\s*```diff(.*?)```", body, re.S)

        body_without_details = body.split("<details>", 1)[0]
        description = strip_tags(body_without_details)
        if finding_match is not None:
            description = description.replace(f"{finding_match.group(1)}: {finding_match.group(2)}", "").strip()

        comments.append(
            {
                "path": path.strip(),
                "range": finding_match.group(1).strip() if finding_match else "",
                "title": collapse_whitespace(finding_match.group(2)) if finding_match else "",
                "description": description,
                "suggested_diff": suggestion_match.group(1).strip() if suggestion_match else "",
                "ai_prompt": prompt_match.group(1).strip() if prompt_match else "",
            }
        )

    return comments


def normalize_review_body_for_parsing(review_body: str) -> str:
    # CodeRabbit sometimes wraps structured HTML sections in markdown blockquotes,
    # such as the CAUTION block used for outside-diff comments. Remove the quote
    # prefixes for parsing while leaving the original raw body unchanged for output.
    return re.sub(r"(?m)^>\s?", "", review_body)


def find_section_block_end(review_body: str, block_start: int) -> int:
    depth = 1
    for tag_match in re.finditer(r"<details>|</details>", review_body[block_start:]):
        tag = tag_match.group(0)
        if tag == "<details>":
            depth += 1
        else:
            depth -= 1
            if depth == 0:
                return block_start + tag_match.start()

    return len(review_body)


def parse_review_comment_group(review_body: str, section_name: str) -> dict[str, Any]:
    section_match = re.search(
        rf"<summary>[^<]*{re.escape(section_name)} \((?P<count>\d+)\)</summary><blockquote>\s*",
        review_body,
        re.S,
    )
    if section_match is None:
        return {"count": 0, "comments": [], "raw": ""}

    block_end = find_section_block_end(review_body, section_match.end())
    comment_block = review_body[section_match.end() : block_end].strip()
    comment_block = re.sub(r"\s*</blockquote>\s*$", "", comment_block, flags=re.S)
    return {
        "count": int(section_match.group("count")),
        "comments": parse_comment_cards(comment_block),
        "raw": comment_block,
    }


def parse_latest_review_body(review_body: str) -> dict[str, Any]:
    normalized_review_body = normalize_review_body_for_parsing(review_body)
    actionable_count_match = re.search(r"\*\*Actionable comments posted:\s*(\d+)\*\*", normalized_review_body)
    prompt_match = re.search(
        r"<summary>🤖 Prompt for all review comments with AI agents</summary>\s*```(.*?)```",
        normalized_review_body,
        re.S,
    )
    outside_diff_group = parse_review_comment_group(normalized_review_body, "Outside diff range comments")
    nitpick_group = parse_review_comment_group(normalized_review_body, "Nitpick comments")
    return {
        "actionable_count": int(actionable_count_match.group(1)) if actionable_count_match else 0,
        "outside_diff_count": outside_diff_group["count"],
        "outside_diff_comments": outside_diff_group["comments"],
        "nitpick_count": nitpick_group["count"],
        "nitpick_comments": nitpick_group["comments"],
        "all_comments_prompt": prompt_match.group(1).strip() if prompt_match else "",
        "raw": review_body.strip(),
    }


def parse_megalinter_comment(comment_body: str) -> dict[str, Any]:
    normalized_body = html.unescape(comment_body).strip()
    summary_match = re.search(
        r"##\s*(?P<badges>.*?)\[MegaLinter\]\([^)]+\)\s+analysis:\s+\[(?P<status>[^\]]+)\]\((?P<run_url>[^)]+)\)",
        normalized_body,
    )

    report: dict[str, Any] = {
        "status": summary_match.group("status").strip() if summary_match else "",
        "run_url": summary_match.group("run_url").strip() if summary_match else "",
        "badges": collapse_whitespace(summary_match.group("badges")) if summary_match else "",
        "descriptor_rows": [],
        "detailed_issues": [],
        "raw": normalized_body,
    }

    table_match = re.search(
        r"\| Descriptor .*?\|\n\|[-| :]+\|\n(?P<rows>(?:\|.*\|\n?)+)",
        normalized_body,
        re.S,
    )
    if table_match is not None:
        for raw_line in table_match.group("rows").splitlines():
            line = raw_line.strip()
            if not line.startswith("|"):
                continue

            parts = [collapse_whitespace(strip_markdown_links(part)) for part in line.strip("|").split("|")]
            if len(parts) != 7:
                continue

            report["descriptor_rows"].append(
                {
                    "descriptor": parts[0],
                    "linter": parts[1],
                    "files": parts[2],
                    "fixed": parts[3],
                    "errors": parts[4],
                    "warnings": parts[5],
                    "elapsed_time": parts[6],
                }
            )

    for summary, details in re.findall(r"<summary>(.*?)</summary>\s*```(.*?)```", normalized_body, re.S):
        report["detailed_issues"].append(
            {
                "summary": collapse_whitespace(strip_tags(summary)),
                "details": details.strip(),
            }
        )

    return report


def parse_test_report(block: str) -> dict[str, Any]:
    report: dict[str, Any] = {
        "raw": block.strip(),
        "stats": {},
        "failed_tests": [],
        "has_failed_tests": False,
    }

    summary_row_match = re.search(
        r"\|\s*\*?\*?(\d+)\*?\*?\s*\|\s*\*?\*?(\d+)\*?\*?\s*\|\s*\*?\*?(\d+)\*?\*?\s*\|"
        r"\s*\*?\*?(\d+)\*?\*?\s*\|\s*\*?\*?(\d+)\*?\*?\s*\|\s*\*?\*?(\d+)\*?\*?\s*\|\s*\*?\*?([^\|]+?)\*?\*?\s*\|",
        block,
    )
    if summary_row_match is not None:
        report["stats"] = {
            "tests": int(summary_row_match.group(1)),
            "passed": int(summary_row_match.group(2)),
            "failed": int(summary_row_match.group(3)),
            "skipped": int(summary_row_match.group(4)),
            "other": int(summary_row_match.group(5)),
            "flaky": int(summary_row_match.group(6)),
            "duration": summary_row_match.group(7).strip(),
        }

    failed_tests_section = extract_section(
        block,
        "### Failed Tests",
        ["### Slowest Tests", "### Insights", "<sub>", "[Github Test Reporter]"],
    )
    if failed_tests_section:
        lines = [line.strip("- ").strip() for line in failed_tests_section.splitlines()[1:] if line.strip()]
        report["failed_tests"] = lines
        report["has_failed_tests"] = True
    elif "No failed tests in this run." in block or "All tests passed!" in block:
        report["failed_tests"] = []
        report["has_failed_tests"] = False

    return report


def fetch_issue_comments(pr_number: int) -> list[dict[str, Any]]:
    return fetch_paged_json(f"https://api.github.com/repos/{OWNER}/{REPO}/issues/{pr_number}/comments?per_page=100")


def select_latest_comment_body(
    comments: list[dict[str, Any]],
    predicate: Any,
    required_user: str | None = None,
) -> str:
    matching_comments = []
    for comment in comments:
        body = html.unescape(str(comment.get("body", "")))
        if required_user is not None and comment.get("user", {}).get("login") != required_user:
            continue
        if predicate(body):
            comment_copy = dict(comment)
            comment_copy["body"] = body
            matching_comments.append(comment_copy)

    if not matching_comments:
        return ""

    latest_comment = max(matching_comments, key=lambda item: (item.get("updated_at", ""), item.get("created_at", "")))
    return str(latest_comment.get("body", "")).strip()


def select_comment_bodies(
    comments: list[dict[str, Any]],
    predicate: Any,
    required_user: str | None = None,
) -> list[str]:
    matching_comments = []
    for comment in comments:
        body = html.unescape(str(comment.get("body", "")))
        if required_user is not None and comment.get("user", {}).get("login") != required_user:
            continue
        if predicate(body):
            comment_copy = dict(comment)
            comment_copy["body"] = body
            matching_comments.append(comment_copy)

    matching_comments.sort(key=lambda item: (item.get("created_at", ""), item.get("updated_at", "")))
    return [str(comment.get("body", "")).strip() for comment in matching_comments]


def summarize_review_comment(comment: dict[str, Any]) -> dict[str, Any]:
    return {
        "id": comment.get("id"),
        "path": comment.get("path") or "",
        "line": comment.get("line"),
        "side": comment.get("side") or "",
        "created_at": comment.get("created_at") or "",
        "updated_at": comment.get("updated_at") or "",
        "user": comment.get("user", {}).get("login") or "",
        "commit_id": comment.get("commit_id") or "",
        "in_reply_to_id": comment.get("in_reply_to_id"),
        "body": comment.get("body") or "",
    }


def classify_review_thread_status(latest_comment: dict[str, Any]) -> str:
    body = latest_comment.get("body") or ""
    author = latest_comment.get("user") or ""
    if author == CODERABBIT_LOGIN and REVIEW_COMMENT_ADDRESSED_MARKER in body:
        return "addressed"
    return "open"


def contains_visible_addressed_commit_text(body: str) -> bool:
    return bool(VISIBLE_ADDRESSED_IN_COMMIT_PATTERN.search(body))


def build_latest_commit_review_threads(comments: list[dict[str, Any]]) -> list[dict[str, Any]]:
    comment_threads: dict[int, dict[str, Any]] = {}

    # GitHub review replies point to the root comment id. Grouping them first lets
    # the skill surface the latest thread state instead of every historical reply.
    for comment in sorted(comments, key=lambda item: (item.get("created_at") or "", item.get("id") or 0)):
        comment_id = comment.get("id")
        if comment_id is None:
            continue

        summary = summarize_review_comment(comment)
        root_id = summary["in_reply_to_id"] or comment_id
        thread = comment_threads.setdefault(
            root_id,
            {
                "thread_id": root_id,
                "path": summary["path"],
                "line": summary["line"],
                "root_comment": None,
                "replies": [],
            },
        )

        if summary["in_reply_to_id"] is None:
            thread["root_comment"] = summary
            thread["path"] = summary["path"]
            thread["line"] = summary["line"]
        else:
            thread["replies"].append(summary)

    threads: list[dict[str, Any]] = []
    for thread in comment_threads.values():
        root_comment = thread.get("root_comment")
        if root_comment is None:
            continue

        ordered_comments = [root_comment, *thread["replies"]]
        latest_comment = max(ordered_comments, key=lambda item: (item.get("updated_at") or "", item.get("created_at") or ""))
        thread["latest_comment"] = latest_comment
        thread["status"] = classify_review_thread_status(latest_comment)
        threads.append(thread)

    return sorted(threads, key=lambda item: (item["path"], item["line"] or 0, item["thread_id"]))


def fetch_latest_commit_review(pr_number: int) -> dict[str, Any]:
    api_base = f"https://api.github.com/repos/{OWNER}/{REPO}/pulls/{pr_number}"
    commits = fetch_paged_json(f"{api_base}/commits?per_page=100")
    reviews = fetch_paged_json(f"{api_base}/reviews?per_page=100")
    comments = fetch_paged_json(f"{api_base}/comments?per_page=100")

    if not commits:
        return {
            "latest_commit": {},
            "latest_review": {},
            "threads": [],
            "open_threads": [],
        }

    latest_commit = commits[-1]
    latest_commit_sha = latest_commit.get("sha", "")
    latest_commit_reviews = [
        review for review in reviews if review.get("commit_id") == latest_commit_sha and review.get("submitted_at")
    ]
    candidate_reviews = latest_commit_reviews or [review for review in reviews if review.get("submitted_at")]
    latest_review = (
        max(candidate_reviews, key=lambda review: review.get("submitted_at", ""))
        if candidate_reviews
        else None
    )

    latest_commit_comments = [comment for comment in comments if comment.get("commit_id") == latest_commit_sha]
    threads = build_latest_commit_review_threads(latest_commit_comments)
    open_threads = [thread for thread in threads if thread["status"] == "open"]

    return {
        "latest_commit": {
            "sha": latest_commit_sha,
            "message": latest_commit.get("commit", {}).get("message", ""),
        },
        "latest_review": {
            "id": latest_review.get("id") if latest_review else None,
            "state": latest_review.get("state") if latest_review else "",
            "submitted_at": latest_review.get("submitted_at") if latest_review else "",
            "commit_id": latest_review.get("commit_id") if latest_review else "",
            "user": latest_review.get("user", {}).get("login") if latest_review else "",
            "body": latest_review.get("body") if latest_review else "",
        },
        "threads": threads,
        "open_threads": open_threads,
    }


def build_result(pr_number: int, branch: str) -> dict[str, Any]:
    warnings: list[str] = []
    pull_request_metadata = fetch_pull_request_metadata(pr_number)
    issue_comments = fetch_issue_comments(pr_number)
    summary_block = select_latest_comment_body(
        issue_comments,
        lambda body: "auto-generated comment: summarize by coderabbit.ai" in body,
        required_user=CODERABBIT_LOGIN,
    )
    actionable_block = select_latest_comment_body(
        issue_comments,
        lambda body: "Actionable comments posted:" in body and "Prompt for all review comments with AI agents" in body,
        required_user=CODERABBIT_LOGIN,
    )
    test_blocks = select_comment_bodies(
        issue_comments,
        lambda body: "CTRF PR COMMENT TAG:" in body or "### Test Results" in body,
    )
    megalinter_block = select_latest_comment_body(
        issue_comments,
        lambda body: "MegaLinter" in body and "Detailed Issues" in body,
        required_user=GITHUB_ACTIONS_LOGIN,
    )

    if not summary_block:
        warnings.append("CodeRabbit summary block was not found in issue comments.")
    if not test_blocks:
        warnings.append("PR test-report block was not found in issue comments.")
    if not megalinter_block:
        warnings.append("MegaLinter report block was not found in issue comments.")

    latest_commit_review: dict[str, Any] = {}
    coderabbit_review: dict[str, Any] = {}
    try:
        latest_commit_review = fetch_latest_commit_review(pr_number)
        latest_review = latest_commit_review.get("latest_review", {})
        latest_review_body = str(latest_review.get("body") or "")
        if latest_review.get("user") == CODERABBIT_LOGIN and latest_review_body:
            coderabbit_review = parse_latest_review_body(latest_review_body)
            outside_diff_count = int(coderabbit_review.get("outside_diff_count") or 0)
            parsed_outside_diff_count = len(coderabbit_review.get("outside_diff_comments", []))
            nitpick_count = int(coderabbit_review.get("nitpick_count") or 0)
            parsed_nitpick_count = len(coderabbit_review.get("nitpick_comments", []))
            if "Outside diff range comments" in latest_review_body and not parsed_outside_diff_count:
                warnings.append("CodeRabbit outside-diff comments block could not be parsed from the latest review body.")
            elif outside_diff_count and parsed_outside_diff_count != outside_diff_count:
                warnings.append(
                    "CodeRabbit outside-diff comments were only partially parsed from the latest review body: "
                    f"declared={outside_diff_count}, parsed={parsed_outside_diff_count}."
                )
            if "Nitpick comments" in latest_review_body and not parsed_nitpick_count:
                warnings.append("CodeRabbit nitpick comments block could not be parsed from the latest review body.")
            elif nitpick_count and parsed_nitpick_count != nitpick_count:
                warnings.append(
                    "CodeRabbit nitpick comments were only partially parsed from the latest review body: "
                    f"declared={nitpick_count}, parsed={parsed_nitpick_count}."
                )
    except Exception as error:  # noqa: BLE001
        warnings.append(f"Latest commit review comments could not be fetched: {error}")

    if (
        not actionable_block
        and not latest_commit_review.get("threads")
        and not coderabbit_review.get("nitpick_comments")
    ):
        warnings.append("CodeRabbit actionable comments block was not found in issue comments.")

    return {
        "pull_request": {
            "number": pull_request_metadata["number"],
            "title": pull_request_metadata["title"],
            "state": pull_request_metadata["state"],
            "head_branch": pull_request_metadata["head_branch"],
            "base_branch": pull_request_metadata["base_branch"],
            "url": pull_request_metadata["url"],
            "resolved_from_branch": branch,
        },
        "coderabbit_summary": {
            "failed_checks": parse_failed_checks(summary_block) if summary_block else [],
            "raw": summary_block,
        },
        "coderabbit_comments": parse_actionable_comments(actionable_block) if actionable_block else {},
        "coderabbit_review": coderabbit_review,
        "latest_commit_review": latest_commit_review,
        "megalinter_report": parse_megalinter_comment(megalinter_block) if megalinter_block else {},
        "test_reports": [parse_test_report(block) for block in test_blocks],
        "parse_warnings": warnings,
    }


def format_text(result: dict[str, Any]) -> str:
    lines: list[str] = []
    pr = result["pull_request"]
    lines.append(f"PR #{pr['number']}: {pr['title']}")
    lines.append(f"State: {pr['state']}")
    lines.append(f"Branch: {pr['head_branch']} -> {pr['base_branch']}")
    lines.append(f"URL: {pr['url']}")

    failed_checks = result["coderabbit_summary"].get("failed_checks", [])
    lines.append("")
    lines.append(f"Failed checks: {len(failed_checks)}")
    for check in failed_checks:
        lines.append(f"- {check['name']}: {check['status']}")
        lines.append(f"  Explanation: {check['explanation']}")
        lines.append(f"  Resolution: {check['resolution']}")

    coderabbit_comments = result.get("coderabbit_comments", {})
    review_feedback = result.get("coderabbit_review", {})
    comments = coderabbit_comments.get("comments", [])
    actionable_count = review_feedback.get("actionable_count") or coderabbit_comments.get("count") or len(comments)
    lines.append("")
    lines.append(f"CodeRabbit actionable comments: {actionable_count}")
    for comment in comments:
        lines.append(f"- {comment['path']} {comment['range']}".rstrip())
        if comment["title"]:
            lines.append(f"  Title: {comment['title']}")
        if comment["description"]:
            lines.append(f"  Description: {comment['description']}")
    if actionable_count and not comments:
        lines.append("  Details: see latest-commit review threads below.")

    outside_diff_comments = review_feedback.get("outside_diff_comments", [])
    outside_diff_count = review_feedback.get("outside_diff_count") or len(outside_diff_comments)
    lines.append("")
    lines.append(f"CodeRabbit outside-diff comments: {outside_diff_count} declared, {len(outside_diff_comments)} parsed")
    for comment in outside_diff_comments:
        lines.append(f"- {comment['path']} {comment['range']}".rstrip())
        if comment["title"]:
            lines.append(f"  Title: {comment['title']}")
        if comment["description"]:
            lines.append(f"  Description: {comment['description']}")

    nitpick_comments = review_feedback.get("nitpick_comments", [])
    nitpick_count = review_feedback.get("nitpick_count") or len(nitpick_comments)
    lines.append("")
    lines.append(f"CodeRabbit nitpick comments: {nitpick_count} declared, {len(nitpick_comments)} parsed")
    for comment in nitpick_comments:
        lines.append(f"- {comment['path']} {comment['range']}".rstrip())
        if comment["title"]:
            lines.append(f"  Title: {comment['title']}")
        if comment["description"]:
            lines.append(f"  Description: {comment['description']}")

    latest_commit_review = result.get("latest_commit_review", {})
    latest_commit = latest_commit_review.get("latest_commit", {})
    latest_review = latest_commit_review.get("latest_review", {})
    open_threads = latest_commit_review.get("open_threads", [])
    if latest_commit:
        lines.append("")
        lines.append(f"Latest reviewed commit: {latest_commit.get('sha', '')}")
        if latest_review:
            lines.append(
                "Latest review: "
                f"{latest_review.get('state', '')} by {latest_review.get('user', '')} "
                f"at {latest_review.get('submitted_at', '')}"
            )

        lines.append(
            "Latest commit review threads: "
            f"{len(latest_commit_review.get('threads', []))} total, {len(open_threads)} open"
        )
        for thread in open_threads:
            root_comment = thread["root_comment"]
            latest_comment = thread["latest_comment"]
            lines.append(f"- {thread['path']}:{thread['line']}")
            lines.append(f"  Root by {root_comment['user']}: {collapse_whitespace(root_comment['body'])}")
            if latest_comment["id"] != root_comment["id"]:
                lines.append(f"  Latest by {latest_comment['user']}: {collapse_whitespace(latest_comment['body'])}")
            if contains_visible_addressed_commit_text(root_comment["body"]) or contains_visible_addressed_commit_text(
                latest_comment["body"]
            ):
                lines.append(
                    "  Note: thread is still open; treat the visible 'Addressed in commit ...' text as unverified until local code matches."
                )

    megalinter_report = result.get("megalinter_report", {})
    if megalinter_report:
        lines.append("")
        lines.append(
            "MegaLinter: "
            f"{megalinter_report.get('status', 'unknown')}"
            + (
                f" ({megalinter_report.get('run_url', '')})"
                if megalinter_report.get("run_url")
                else ""
            )
        )

        descriptor_rows = megalinter_report.get("descriptor_rows", [])
        for descriptor_row in descriptor_rows:
            lines.append(
                "- "
                f"{descriptor_row['descriptor']} / {descriptor_row['linter']}: "
                f"errors={descriptor_row['errors']} warnings={descriptor_row['warnings']} files={descriptor_row['files']}"
            )

        for issue in megalinter_report.get("detailed_issues", []):
            lines.append(f"- Detailed issue: {issue['summary']}")
            lines.append(f"  {collapse_whitespace(issue['details'])}")

    lines.append("")
    lines.append(f"Test reports: {len(result['test_reports'])}")
    for index, report in enumerate(result["test_reports"], start=1):
        stats = report.get("stats", {})
        if stats:
            lines.append(
                f"- Report {index}: tests={stats.get('tests')} passed={stats.get('passed')} "
                f"failed={stats.get('failed')} skipped={stats.get('skipped')} flaky={stats.get('flaky')} "
                f"duration={stats.get('duration')}"
            )
        else:
            lines.append(f"- Report {index}: no structured test stats parsed")

        if report["has_failed_tests"]:
            for failed_test in report["failed_tests"]:
                lines.append(f"  Failed test: {failed_test}")
        else:
            lines.append("  Failed tests: none reported")

    if result["parse_warnings"]:
        lines.append("")
        lines.append("Warnings:")
        for warning in result["parse_warnings"]:
            lines.append(f"- {warning}")

    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--branch", help="Override the current branch name.")
    parser.add_argument("--pr", type=int, help="Fetch a specific PR number instead of resolving from branch.")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    if args.pr is not None:
        pr_number = args.pr
        branch = args.branch or ""
    else:
        branch = args.branch or get_current_branch()
        pr_number = resolve_pr_number(branch)

    result = build_result(pr_number, branch)

    if args.format == "json":
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return

    print(format_text(result))


if __name__ == "__main__":
    try:
        main()
    except Exception as error:  # noqa: BLE001
        print(str(error), file=sys.stderr)
        sys.exit(1)
