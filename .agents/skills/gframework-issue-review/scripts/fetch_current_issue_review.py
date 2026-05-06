#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""
Fetch the current GFramework GitHub issue and extract the signals needed for
local follow-up work without relying on gh CLI.
"""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import urllib.error
import urllib.request
from typing import Any

OWNER = "GeWuYou"
REPO = "GFramework"
WORKTREE_ROOT_DIRECTORY_NAME = "GFramework-WorkTree"
GIT_ENVIRONMENT_KEY = "GFRAMEWORK_WINDOWS_GIT"
GIT_DIR_ENVIRONMENT_KEY = "GFRAMEWORK_GIT_DIR"
WORK_TREE_ENVIRONMENT_KEY = "GFRAMEWORK_WORK_TREE"
REQUEST_TIMEOUT_ENVIRONMENT_KEY = "GFRAMEWORK_ISSUE_REVIEW_TIMEOUT_SECONDS"
GITHUB_TOKEN_ENVIRONMENT_KEYS = ("GFRAMEWORK_GITHUB_TOKEN", "GITHUB_TOKEN", "GH_TOKEN")
PROXY_ENVIRONMENT_KEYS = ("http_proxy", "https_proxy", "HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "all_proxy")
DEFAULT_REQUEST_TIMEOUT_SECONDS = 60
USER_AGENT = "codex-gframework-issue-review"
DISPLAY_SECTION_CHOICES = (
    "issue",
    "summary",
    "comments",
    "events",
    "references",
    "warnings",
)
ISSUE_TYPE_CANDIDATES = ("bug", "feature", "docs", "question", "maintenance")
ACTIVE_TOPIC_KEYWORDS: dict[str, tuple[str, ...]] = {
    "ai-first-config-system": ("config", "configuration", "gameconfig", "settings"),
    "coroutine-optimization": ("coroutine", "yield", "await", "scheduler"),
    "cqrs-rewrite": ("cqrs", "command", "query", "eventbus", "event bus"),
    "data-repository-persistence": ("repository", "serialization", "persistence", "data", "settings"),
    "runtime-generator-boundary": ("source generator", "generator", "attribute", "packaging"),
    "semantic-release-versioning": ("release", "version", "semantic-release", "tag", "publish"),
    "documentation-full-coverage-governance": ("docs", "documentation", "readme", "vitepress", "api reference"),
}
ACTUAL_BEHAVIOR_PATTERNS = (
    "actual",
    "currently",
    "instead",
    "but",
    "error",
    "exception",
    "fails",
    "failed",
    "wrong",
)
EXPECTED_BEHAVIOR_PATTERNS = (
    "expected",
    "should",
    "want",
    "would like",
    "needs to",
)
REPRODUCTION_PATTERNS = (
    "steps to reproduce",
    "reproduce",
    "reproduction",
    "how to reproduce",
    "minimal example",
    "sample",
    "demo",
)
ENVIRONMENT_PATTERNS = (
    "windows",
    "linux",
    "macos",
    "wsl",
    "godot",
    ".net",
    "sdk",
    "version",
    "environment",
)
ACCEPTANCE_PATTERNS = (
    "acceptance",
    "done when",
    "definition of done",
    "verified by",
    "test plan",
)
FILE_PATH_PATTERN = re.compile(r"\b(?:[A-Za-z0-9_.-]+/)+[A-Za-z0-9_.-]+\b")
ISSUE_REFERENCE_PATTERN = re.compile(r"(?:^|\s)#(\d+)\b")
COMMIT_REFERENCE_PATTERN = re.compile(r"\b[0-9a-f]{7,40}\b")
LINE_BREAK_NORMALIZER = re.compile(r"\n{3,}")


def resolve_git_command() -> str:
    """Resolve the git executable to use for this repository."""
    candidates = [
        os.environ.get(GIT_ENVIRONMENT_KEY),
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


def find_repository_root(start_path: Path) -> Path | None:
    """Locate the repository root by walking parent directories for repo markers."""
    for candidate in (start_path, *start_path.parents):
        if (candidate / "AGENTS.md").exists() and (candidate / ".ai/environment/tools.ai.yaml").exists():
            return candidate

    return None


def resolve_worktree_git_dir(repository_root: Path) -> Path | None:
    """Resolve the main-repository worktree gitdir for this WSL worktree layout."""
    if repository_root.parent.name != WORKTREE_ROOT_DIRECTORY_NAME:
        return None

    primary_repository_root = repository_root.parent.parent / REPO
    candidate_git_dir = primary_repository_root / ".git" / "worktrees" / repository_root.name
    return candidate_git_dir if candidate_git_dir.exists() else None


def resolve_git_invocation() -> list[str]:
    """Resolve the git command arguments, preferring explicit WSL worktree binding."""
    configured_git_dir = os.environ.get(GIT_DIR_ENVIRONMENT_KEY)
    configured_work_tree = os.environ.get(WORK_TREE_ENVIRONMENT_KEY)
    linux_git = shutil.which("git")

    if configured_git_dir and configured_work_tree and linux_git:
        return [linux_git, f"--git-dir={configured_git_dir}", f"--work-tree={configured_work_tree}"]

    repository_root = find_repository_root(Path.cwd())
    if repository_root is not None and linux_git:
        worktree_git_dir = resolve_worktree_git_dir(repository_root)
        if worktree_git_dir is not None:
            return [linux_git, f"--git-dir={worktree_git_dir}", f"--work-tree={repository_root}"]

        root_git_dir = repository_root / ".git"
        if root_git_dir.exists():
            return [linux_git, f"--git-dir={root_git_dir}", f"--work-tree={repository_root}"]

    return [resolve_git_command()]


def resolve_request_timeout_seconds() -> int:
    """Return the GitHub request timeout in seconds."""
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
    """Run a command and return stdout, raising on failure."""
    process = subprocess.run(args, capture_output=True, text=True, check=False)
    if process.returncode != 0:
        stderr = process.stderr.strip()
        raise RuntimeError(f"Command failed: {' '.join(args)}\n{stderr}")
    return process.stdout.strip()


def get_current_branch() -> str:
    """Return the current git branch name."""
    return run_command([*resolve_git_invocation(), "rev-parse", "--abbrev-ref", "HEAD"])


def resolve_github_token() -> str | None:
    """Return the first configured GitHub token for authenticated API requests."""
    for environment_key in GITHUB_TOKEN_ENVIRONMENT_KEYS:
        token = os.environ.get(environment_key)
        if token:
            return token

    return None


def build_request_headers(accept: str) -> dict[str, str]:
    """Build GitHub request headers and include auth when a token is available."""
    headers = {"Accept": accept, "User-Agent": USER_AGENT}
    token = resolve_github_token()
    if token:
        headers["Authorization"] = f"Bearer {token}"

    return headers


def has_proxy_environment() -> bool:
    """Return whether the current process is configured to use an outbound proxy."""
    return any(os.environ.get(environment_key) for environment_key in PROXY_ENVIRONMENT_KEYS)


def perform_request(url: str, headers: dict[str, str], *, disable_proxy: bool) -> tuple[str, Any]:
    """Execute a single HTTP request and return decoded text plus response headers."""
    opener = (
        urllib.request.build_opener(urllib.request.ProxyHandler({}))
        if disable_proxy
        else urllib.request.build_opener()
    )
    request = urllib.request.Request(url, headers=headers)
    with opener.open(request, timeout=resolve_request_timeout_seconds()) as response:
        return response.read().decode("utf-8", "replace"), response.headers


def open_url(url: str, accept: str) -> tuple[str, Any]:
    """Open a URL, retrying without proxies only when the configured proxy path fails."""
    headers = build_request_headers(accept)

    try:
        return perform_request(url, headers, disable_proxy=False)
    except urllib.error.HTTPError:
        raise
    except (urllib.error.URLError, TimeoutError, OSError):
        if not has_proxy_environment():
            raise

    return perform_request(url, headers, disable_proxy=True)


def fetch_json(url: str, accept: str = "application/vnd.github+json") -> tuple[Any, Any]:
    """Fetch a JSON payload and its response headers from GitHub."""
    text, headers = open_url(url, accept=accept)
    return json.loads(text), headers


def extract_next_link(headers: Any) -> str | None:
    """Extract the next-page link from GitHub pagination headers."""
    link_header = headers.get("Link")
    if not link_header:
        return None

    match = re.search(r'<([^>]+)>;\s*rel="next"', link_header)
    return match.group(1) if match else None


def fetch_paged_json(url: str, accept: str = "application/vnd.github+json") -> list[dict[str, Any]]:
    """Fetch every page from a paginated GitHub API endpoint."""
    items: list[dict[str, Any]] = []
    next_url: str | None = url
    while next_url:
        payload, headers = fetch_json(next_url, accept=accept)
        if not isinstance(payload, list):
            raise RuntimeError(f"Expected list payload from GitHub API, got {type(payload).__name__}.")

        items.extend(payload)
        next_url = extract_next_link(headers)

    return items


def collapse_whitespace(text: str) -> str:
    """Collapse repeated whitespace into single spaces while preserving paragraph intent."""
    normalized = text.replace("\r\n", "\n").replace("\r", "\n")
    normalized = LINE_BREAK_NORMALIZER.sub("\n\n", normalized)
    normalized = re.sub(r"[ \t]+", " ", normalized)
    normalized = re.sub(r" *\n *", "\n", normalized)
    return normalized.strip()


def truncate_text(text: str, max_length: int) -> str:
    """Collapse whitespace and truncate long text for CLI display."""
    collapsed = collapse_whitespace(text)
    if max_length <= 0 or len(collapsed) <= max_length:
        return collapsed

    return collapsed[: max_length - 3].rstrip() + "..."


def filter_open_issue_candidates(items: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """Filter GitHub issue list responses down to non-PR issue items."""
    return [item for item in items if not item.get("pull_request")]


def select_single_open_issue_number(items: list[dict[str, Any]]) -> int:
    """Resolve the target issue number when the repository has exactly one open issue."""
    issues = filter_open_issue_candidates(items)
    if not issues:
        raise RuntimeError("No open GitHub issues found for this repository. Pass --issue <number> to inspect one.")

    if len(issues) > 1:
        numbers = ", ".join(str(item.get("number")) for item in issues[:5])
        suffix = "" if len(issues) <= 5 else ", ..."
        raise RuntimeError(
            "Multiple open GitHub issues found for this repository "
            f"({len(issues)} total: {numbers}{suffix}). Pass --issue <number> to inspect one."
        )

    return int(issues[0]["number"])


def resolve_issue_number(issue_number: int | None) -> tuple[int, str]:
    """Resolve the issue number, auto-selecting only when exactly one open issue exists."""
    if issue_number is not None:
        return issue_number, "explicit"

    open_items = fetch_paged_json(f"https://api.github.com/repos/{OWNER}/{REPO}/issues?state=open&per_page=100")
    return select_single_open_issue_number(open_items), "auto-single-open-issue"


def fetch_issue_metadata(issue_number: int) -> dict[str, Any]:
    """Fetch normalized metadata for a GitHub issue."""
    payload, _ = fetch_json(f"https://api.github.com/repos/{OWNER}/{REPO}/issues/{issue_number}")
    if not isinstance(payload, dict):
        raise RuntimeError("Failed to fetch GitHub issue metadata.")

    if payload.get("pull_request"):
        raise RuntimeError(f"Item #{issue_number} is a pull request, not a plain issue.")

    labels = []
    for label in payload.get("labels", []):
        if isinstance(label, dict) and label.get("name"):
            labels.append(str(label["name"]))

    assignees = []
    for assignee in payload.get("assignees", []):
        login = assignee.get("login")
        if login:
            assignees.append(str(login))

    milestone_title = None
    milestone = payload.get("milestone")
    if isinstance(milestone, dict) and milestone.get("title"):
        milestone_title = str(milestone["title"])

    return {
        "number": int(payload["number"]),
        "title": str(payload["title"]),
        "state": str(payload["state"]).upper(),
        "url": str(payload["html_url"]),
        "author": str(payload.get("user", {}).get("login") or ""),
        "created_at": str(payload.get("created_at") or ""),
        "updated_at": str(payload.get("updated_at") or ""),
        "labels": labels,
        "assignees": assignees,
        "milestone": milestone_title,
        "body": str(payload.get("body") or ""),
    }


def fetch_issue_comments(issue_number: int) -> list[dict[str, Any]]:
    """Fetch issue comments for the selected issue."""
    return fetch_paged_json(f"https://api.github.com/repos/{OWNER}/{REPO}/issues/{issue_number}/comments?per_page=100")


def fetch_issue_timeline(issue_number: int) -> list[dict[str, Any]]:
    """Fetch issue timeline events when GitHub exposes them to the current client."""
    return fetch_paged_json(
        f"https://api.github.com/repos/{OWNER}/{REPO}/issues/{issue_number}/timeline?per_page=100",
        accept="application/vnd.github+json",
    )


def normalize_comment(comment: dict[str, Any]) -> dict[str, Any]:
    """Normalize an issue comment for structured output."""
    return {
        "id": int(comment.get("id") or 0),
        "author": str(comment.get("user", {}).get("login") or ""),
        "created_at": str(comment.get("created_at") or ""),
        "updated_at": str(comment.get("updated_at") or ""),
        "body": str(comment.get("body") or ""),
    }


def normalize_timeline_event(event: dict[str, Any]) -> dict[str, Any]:
    """Normalize the GitHub timeline event fields used by triage output."""
    actor = str(event.get("actor", {}).get("login") or "")
    created_at = str(event.get("created_at") or event.get("submitted_at") or "")
    event_type = str(event.get("event") or event.get("__typename") or "unknown")
    label_name = ""
    assignee = ""
    source_issue_number: int | None = None
    source_issue_url = ""
    commit_id = ""

    label = event.get("label")
    if isinstance(label, dict) and label.get("name"):
        label_name = str(label["name"])

    assignee_payload = event.get("assignee")
    if isinstance(assignee_payload, dict) and assignee_payload.get("login"):
        assignee = str(assignee_payload["login"])

    source = event.get("source")
    if isinstance(source, dict):
        issue_payload = source.get("issue")
        if isinstance(issue_payload, dict):
            if issue_payload.get("number"):
                source_issue_number = int(issue_payload["number"])
            if issue_payload.get("html_url"):
                source_issue_url = str(issue_payload["html_url"])

    commit_id_value = event.get("commit_id")
    if isinstance(commit_id_value, str):
        commit_id = commit_id_value

    return {
        "event": event_type,
        "actor": actor,
        "created_at": created_at,
        "label": label_name,
        "assignee": assignee,
        "commit_id": commit_id,
        "source_issue_number": source_issue_number,
        "source_issue_url": source_issue_url,
    }


def gather_text_blocks(issue: dict[str, Any], comments: list[dict[str, Any]]) -> list[str]:
    """Return the issue body plus discussion comment bodies for heuristic parsing."""
    blocks = [issue.get("body", "")]
    blocks.extend(comment.get("body", "") for comment in comments)
    return [block for block in blocks if block]


def has_any_pattern(text_blocks: list[str], patterns: tuple[str, ...]) -> bool:
    """Return whether any normalized text block contains any requested pattern."""
    lowered_blocks = [collapse_whitespace(block).lower() for block in text_blocks]
    return any(pattern in block for block in lowered_blocks for pattern in patterns)


def choose_issue_type_candidates(issue: dict[str, Any], text_blocks: list[str]) -> list[str]:
    """Infer lightweight issue-type candidates from labels and discussion text."""
    labels = [label.lower() for label in issue.get("labels", [])]
    text = "\n".join(text_blocks).lower()
    candidates: list[str] = []

    if any(label in {"bug", "regression"} for label in labels) or "bug" in text or "error" in text or "fails" in text:
        candidates.append("bug")
    if any(label in {"feature", "enhancement"} for label in labels) or "feature" in text or "support" in text:
        candidates.append("feature")
    if any(label in {"documentation", "docs"} for label in labels) or "documentation" in text or "readme" in text:
        candidates.append("docs")
    if any(label in {"question", "help wanted"} for label in labels) or "?" in issue.get("title", ""):
        candidates.append("question")
    if any(label in {"chore", "maintenance", "refactor"} for label in labels) or "cleanup" in text or "refactor" in text:
        candidates.append("maintenance")

    if not candidates:
        candidates.append("question" if issue.get("body", "").strip().endswith("?") else "bug")

    ordered_candidates: list[str] = []
    for candidate in ISSUE_TYPE_CANDIDATES:
        if candidate in candidates:
            ordered_candidates.append(candidate)

    return ordered_candidates


def extract_references_from_text(text: str) -> dict[str, list[str]]:
    """Extract issue, commit, and file-path references from one text block."""
    issue_numbers = sorted({match.group(1) for match in ISSUE_REFERENCE_PATTERN.finditer(text)}, key=int)
    commit_shas = sorted({match.group(0) for match in COMMIT_REFERENCE_PATTERN.finditer(text)})
    file_paths = sorted({match.group(0) for match in FILE_PATH_PATTERN.finditer(text)})

    return {
        "issues": [f"#{number}" for number in issue_numbers],
        "commit_shas": commit_shas,
        "file_paths": file_paths,
    }


def merge_reference_values(values: list[dict[str, list[str]]]) -> dict[str, list[str]]:
    """Merge extracted reference lists while preserving sorted unique output."""
    merged: dict[str, set[str]] = {"issues": set(), "commit_shas": set(), "file_paths": set()}
    for value in values:
        for key in merged:
            merged[key].update(value.get(key, []))

    return {
        "issues": sorted(merged["issues"], key=lambda item: int(item[1:])),
        "commit_shas": sorted(merged["commit_shas"]),
        "file_paths": sorted(merged["file_paths"]),
    }


def build_references(issue: dict[str, Any], comments: list[dict[str, Any]], events: list[dict[str, Any]]) -> dict[str, Any]:
    """Build structured references from issue text and timeline context."""
    extracted = [extract_references_from_text(issue.get("body", ""))]
    extracted.extend(extract_references_from_text(comment.get("body", "")) for comment in comments)
    merged = merge_reference_values(extracted)
    referenced_by_timeline = sorted(
        {
            f"#{event['source_issue_number']}"
            for event in events
            if event.get("source_issue_number") is not None
        },
        key=lambda item: int(item[1:]),
    )

    pull_request_references = sorted(
        {
            issue_reference
            for issue_reference in merged["issues"]
            if issue_reference != f"#{issue['number']}"
        },
        key=lambda item: int(item[1:]),
    )

    return {
        "issues": merged["issues"],
        "pull_requests_or_issues": pull_request_references,
        "commit_shas": merged["commit_shas"],
        "file_paths": merged["file_paths"],
        "timeline_cross_references": referenced_by_timeline,
    }


def build_information_flags(
    issue: dict[str, Any],
    comments: list[dict[str, Any]],
    issue_type_candidates: list[str],
) -> dict[str, bool]:
    """Derive missing-information and readiness flags with issue-type-aware heuristics."""
    text_blocks = gather_text_blocks(issue, comments)
    has_reproduction_steps = has_any_pattern(text_blocks, REPRODUCTION_PATTERNS)
    has_expected_behavior = has_any_pattern(text_blocks, EXPECTED_BEHAVIOR_PATTERNS)
    has_actual_behavior = has_any_pattern(text_blocks, ACTUAL_BEHAVIOR_PATTERNS)
    has_environment_details = has_any_pattern(text_blocks, ENVIRONMENT_PATTERNS)
    has_acceptance_signals = has_any_pattern(text_blocks, ACCEPTANCE_PATTERNS)
    primary_issue_type = issue_type_candidates[0] if issue_type_candidates else "bug"

    if primary_issue_type == "bug":
        needs_clarification = not (
            (has_actual_behavior and (has_reproduction_steps or has_environment_details))
            or has_acceptance_signals
        )
    elif primary_issue_type in {"feature", "docs"}:
        needs_clarification = not (has_expected_behavior or has_acceptance_signals)
    elif primary_issue_type == "maintenance":
        needs_clarification = not (has_expected_behavior or has_actual_behavior or has_acceptance_signals)
    else:
        needs_clarification = not (has_expected_behavior or has_actual_behavior or has_acceptance_signals)

    return {
        "has_reproduction_steps": has_reproduction_steps,
        "has_expected_behavior": has_expected_behavior,
        "has_actual_behavior": has_actual_behavior,
        "has_environment_details": has_environment_details,
        "has_acceptance_signals": has_acceptance_signals,
        "needs_clarification": needs_clarification,
    }


def choose_affected_topics(issue: dict[str, Any], comments: list[dict[str, Any]]) -> list[str]:
    """Map the issue discussion to likely active topics when obvious keyword matches exist."""
    text = "\n".join(gather_text_blocks(issue, comments)).lower()
    matches: list[str] = []
    for topic, keywords in ACTIVE_TOPIC_KEYWORDS.items():
        if any(keyword in text for keyword in keywords):
            matches.append(topic)

    return matches


def choose_next_action(
    information_flags: dict[str, bool],
    issue_type_candidates: list[str],
    affected_topics: list[str],
) -> str:
    """Choose the next handling mode for boot handoff."""
    if information_flags["needs_clarification"]:
        return "clarify-issue-before-code"
    if affected_topics:
        return "resume-existing-topic-with-boot"
    if "docs" in issue_type_candidates and issue_type_candidates[0] == "docs":
        return "start-new-docs-topic-with-boot"
    return "start-new-topic-with-boot"


def build_triage_hints(issue: dict[str, Any], comments: list[dict[str, Any]]) -> dict[str, Any]:
    """Build lightweight, reviewable triage hints for boot follow-up."""
    text_blocks = gather_text_blocks(issue, comments)
    issue_type_candidates = choose_issue_type_candidates(issue, text_blocks)
    information_flags = build_information_flags(issue, comments, issue_type_candidates)
    affected_topics = choose_affected_topics(issue, comments)
    next_action = choose_next_action(information_flags, issue_type_candidates, affected_topics)

    return {
        "issue_type_candidates": issue_type_candidates,
        "information_flags": information_flags,
        "affected_active_topics": affected_topics,
        "next_action": next_action,
        "boot_handoff": {
            "recommended_skill": "gframework-boot",
            "mode": "resume" if affected_topics else "new",
            "notes": (
                "Use gframework-boot to verify the issue against local code and active ai-plan topics."
                if not information_flags["needs_clarification"]
                else "Use gframework-boot to record a clarification-first task before changing code."
            ),
        },
    }


def build_result(issue_number: int, branch: str, resolution_mode: str) -> dict[str, Any]:
    """Build the full issue review payload for the selected issue."""
    parse_warnings: list[str] = []
    issue = fetch_issue_metadata(issue_number)
    raw_comments = fetch_issue_comments(issue_number)
    comments = [normalize_comment(comment) for comment in raw_comments]

    events: list[dict[str, Any]] = []
    try:
        raw_events = fetch_issue_timeline(issue_number)
        events = [normalize_timeline_event(event) for event in raw_events]
    except Exception as error:  # noqa: BLE001
        parse_warnings.append(f"Issue timeline could not be fetched or parsed: {error}")

    references = build_references(issue, comments, events)
    triage_hints = build_triage_hints(issue, comments)

    return {
        "issue": {
            **issue,
            "resolved_from_branch": branch,
            "resolution_mode": resolution_mode,
        },
        "discussion": {
            "comment_count": len(comments),
            "comments": comments,
        },
        "events": {
            "count": len(events),
            "items": events,
        },
        "references": references,
        "triage_hints": triage_hints,
        "parse_warnings": parse_warnings,
    }


def write_json_output(result: dict[str, Any], output_path: str) -> str:
    """Write the full JSON result to disk and return the destination path."""
    destination_path = Path(output_path).expanduser()
    destination_path.parent.mkdir(parents=True, exist_ok=True)
    destination_path.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    return str(destination_path)


def summarize_events(events: list[dict[str, Any]]) -> list[str]:
    """Convert normalized events into concise text lines."""
    lines: list[str] = []
    for event in events:
        summary = f"- {event['event']}"
        details: list[str] = []
        if event.get("actor"):
            details.append(f"actor={event['actor']}")
        if event.get("label"):
            details.append(f"label={event['label']}")
        if event.get("assignee"):
            details.append(f"assignee={event['assignee']}")
        if event.get("source_issue_number") is not None:
            details.append(f"source_issue=#{event['source_issue_number']}")
        if event.get("commit_id"):
            details.append(f"commit={event['commit_id'][:12]}")
        if event.get("created_at"):
            details.append(f"at={event['created_at']}")
        if details:
            summary += " (" + ", ".join(details) + ")"
        lines.append(summary)
    return lines


def format_text(
    result: dict[str, Any],
    *,
    sections: list[str] | None = None,
    max_description_length: int = 400,
    json_output_path: str | None = None,
) -> str:
    """Format the result payload into concise text output."""
    lines: list[str] = []
    selected_sections = set(sections or DISPLAY_SECTION_CHOICES)
    issue = result["issue"]
    triage_hints = result["triage_hints"]
    discussion = result["discussion"]
    events = result["events"]
    references = result["references"]

    if "issue" in selected_sections:
        lines.append(f"Issue #{issue['number']}: {issue['title']}")
        lines.append(f"State: {issue['state']}")
        lines.append(f"Author: {issue['author']}")
        lines.append(f"Labels: {', '.join(issue['labels']) if issue['labels'] else '(none)'}")
        lines.append(f"Assignees: {', '.join(issue['assignees']) if issue['assignees'] else '(none)'}")
        lines.append(f"Milestone: {issue['milestone'] or '(none)'}")
        lines.append(f"Created: {issue['created_at']}")
        lines.append(f"Updated: {issue['updated_at']}")
        lines.append(f"Resolved from branch: {issue['resolved_from_branch'] or '(not branch-based)'}")
        lines.append(f"Resolution mode: {issue['resolution_mode']}")
        lines.append(f"URL: {issue['url']}")
        if issue["body"]:
            lines.append("Body:")
            lines.append(truncate_text(issue["body"], max_description_length))

    if "summary" in selected_sections:
        lines.append("")
        lines.append("Triage summary:")
        lines.append("- Issue type candidates: " + ", ".join(triage_hints["issue_type_candidates"]))
        information_flags = triage_hints["information_flags"]
        lines.append(
            "- Information flags: "
            + ", ".join(
                [
                    f"repro={'yes' if information_flags['has_reproduction_steps'] else 'no'}",
                    f"expected={'yes' if information_flags['has_expected_behavior'] else 'no'}",
                    f"actual={'yes' if information_flags['has_actual_behavior'] else 'no'}",
                    f"environment={'yes' if information_flags['has_environment_details'] else 'no'}",
                    f"acceptance={'yes' if information_flags['has_acceptance_signals'] else 'no'}",
                    f"needs_clarification={'yes' if information_flags['needs_clarification'] else 'no'}",
                ]
            )
        )
        lines.append(
            "- Affected active topics: "
            + (", ".join(triage_hints["affected_active_topics"]) if triage_hints["affected_active_topics"] else "(none)")
        )
        lines.append(f"- Next action: {triage_hints['next_action']}")
        lines.append(f"- Boot handoff: {triage_hints['boot_handoff']['notes']}")

    if "comments" in selected_sections:
        lines.append("")
        lines.append(f"Discussion comments: {discussion['comment_count']}")
        for comment in discussion["comments"]:
            lines.append(f"- {comment['author']} at {comment['created_at']}")
            lines.append(f"  {truncate_text(comment['body'], max_description_length)}")

    if "events" in selected_sections:
        lines.append("")
        lines.append(f"Timeline events: {events['count']}")
        lines.extend(summarize_events(events["items"]))

    if "references" in selected_sections:
        lines.append("")
        lines.append("References:")
        lines.append("- Mentioned issues: " + (", ".join(references["issues"]) if references["issues"] else "(none)"))
        lines.append(
            "- Cross references: "
            + (
                ", ".join(references["timeline_cross_references"])
                if references["timeline_cross_references"]
                else "(none)"
            )
        )
        lines.append(
            "- Related issue/PR mentions: "
            + (
                ", ".join(references["pull_requests_or_issues"])
                if references["pull_requests_or_issues"]
                else "(none)"
            )
        )
        lines.append("- Commit SHAs: " + (", ".join(references["commit_shas"]) if references["commit_shas"] else "(none)"))
        lines.append("- File paths: " + (", ".join(references["file_paths"]) if references["file_paths"] else "(none)"))

    if result["parse_warnings"] and "warnings" in selected_sections:
        lines.append("")
        lines.append("Warnings:")
        for warning in result["parse_warnings"]:
            lines.append(f"- {truncate_text(warning, max_description_length)}")

    if json_output_path:
        lines.append("")
        lines.append(f"Full JSON written to: {json_output_path}")

    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    """Parse CLI arguments."""
    parser = argparse.ArgumentParser()
    parser.add_argument("--branch", help="Override the current branch name.")
    parser.add_argument("--issue", type=int, help="Fetch a specific issue number instead of auto-selecting one.")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    parser.add_argument(
        "--json-output",
        help="Write the full JSON result to a file. When used with --format text, stdout stays concise and points to the file.",
    )
    parser.add_argument(
        "--section",
        action="append",
        choices=DISPLAY_SECTION_CHOICES,
        help="Limit text output to specific sections. Can be passed multiple times.",
    )
    parser.add_argument(
        "--max-description-length",
        type=int,
        default=400,
        help="Truncate long text bodies in text output to this many characters.",
    )
    return parser.parse_args()


def main() -> None:
    """Run the CLI entry point."""
    args = parse_args()
    branch = args.branch or get_current_branch()
    issue_number, resolution_mode = resolve_issue_number(args.issue)
    result = build_result(issue_number, branch, resolution_mode)

    json_output_path: str | None = None
    if args.json_output:
        json_output_path = write_json_output(result, args.json_output)

    if args.format == "json":
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return

    print(
        format_text(
            result,
            sections=args.section,
            max_description_length=args.max_description_length,
            json_output_path=json_output_path,
        )
    )


if __name__ == "__main__":
    try:
        main()
    except Exception as error:  # noqa: BLE001
        print(str(error), file=sys.stderr)
        sys.exit(1)
