---
name: gframework-batch-boot
description: Repository-specific bulk-task workflow for the GFramework repo. Use when Codex should start from the normal GFramework boot context and then continue a repetitive or large-scope task in automatic batches without waiting for manual round-by-round prompts, especially for analyzer warning cleanup, repetitive test refactors, documentation waves, or similar multi-file work with an explicit stop condition such as changed-file count, warning count, or timebox.
---

# GFramework Batch Boot

## Overview

Use this skill when `gframework-boot` is necessary but not sufficient because the task should keep advancing in bounded
batches until a clear stop condition is met.

Treat `AGENTS.md` as the source of truth. This skill extends `gframework-boot`; it does not replace it.

## Startup Workflow

1. Execute the normal `gframework-boot` startup sequence first:
   - read `AGENTS.md`
   - read `.ai/environment/tools.ai.yaml`
   - read `ai-plan/public/README.md`
   - read the mapped active topic `todos/` and `traces/`
2. Classify the task as a batch candidate only if all of the following are true:
   - the work is repetitive, sliceable, or likely to require multiple similar iterations
   - each batch can be given an explicit ownership boundary
   - a stop condition can be measured locally
3. Before any delegation, define the batch objective in one sentence:
   - warning family reduction
   - repeated test refactor pattern
   - module-by-module documentation refresh
   - other repetitive multi-file cleanup

## Baseline Selection

When the stop condition depends on branch size or changed-file count, choose the baseline carefully.

1. Prefer the freshest remote-tracking reference that already exists locally:
   - `origin/main`
   - or the mapped upstream base branch for the current topic
2. Do not default to local `main` when `refs/heads/main` is behind `refs/remotes/origin/main`.
3. If both local and remote-tracking refs exist, report:
   - ref name
   - short SHA
   - committer date
4. If only a local branch exists, state that the baseline may be stale before using it.
5. When the task is tied to a PR or topic branch rather than `main`, prefer that explicit upstream comparison target over
   a generic `main`.

For changed-file limits, measure branch-wide scope against the chosen baseline, not just the current working tree:

- use `git diff --name-only <baseline>...HEAD`
- do not confuse branch diff size with `git status --short`

For changed-line limits, also measure branch-wide scope against the chosen baseline:

- prefer `git diff --numstat <baseline>...HEAD`
- treat "changed lines" as `added + deleted` summed across the branch diff
- do not use working-tree-only line counts as a substitute for branch-wide scope

For shorthand numeric thresholds, use a fixed default baseline:

- compare the current branch's cumulative diff against remote `origin/main`
- include all commits reachable from `HEAD` that are not already in `origin/main`
- do not reinterpret shorthand thresholds as "this batch only" or "current unstaged changes only"
- only use another baseline when the user explicitly names it in the prompt

## Stop Conditions

Choose one primary stop condition before the first batch and restate it to the user.

Common stop conditions:

- branch diff vs baseline approaches a file-count threshold
- warnings-only build reaches a target count
- a specific hotspot list is exhausted
- a timebox or validation budget is reached

If multiple stop conditions exist, rank them and treat one as primary.

## Shorthand Stop-Condition Syntax

`gframework-batch-boot` may be invoked with shorthand numeric thresholds when the user clearly wants a branch-size stop
condition instead of a long natural-language prompt.

Interpret shorthand as follows:

- `$gframework-batch-boot 75`
  - means: stop when the current branch's cumulative diff vs remote `origin/main` approaches `75` changed files
- `$gframework-batch-boot 75 2000`
  - means: stop when the current branch's cumulative diff vs remote `origin/main` approaches `75` changed files OR
    `2000` changed lines
  - default positional meaning is `<files> <lines>`
- `$gframework-batch-boot 75 | 2000`
  - may be interpreted as the same OR shorthand in plain-language chat
  - when restating, planning, or documenting the command, normalize it to `$gframework-batch-boot 75 2000`
  - prefer the no-pipe form because `|` is easy to confuse with a shell pipeline

When shorthand is used:

- report the resolved thresholds explicitly before the first batch
- report that the baseline is remote `origin/main`, unless the user explicitly overrides it
- if two numeric thresholds are present, treat file count as the default primary metric for status reporting unless the
  user says otherwise
- stop when either threshold is reached or exceeded, even if the other threshold still has headroom

## Batch Loop

1. Inspect the current state before the first batch:
   - current branch and active topic
   - selected baseline
   - current stop-condition metric
   - next candidate slices
2. Keep the critical path local.
3. Delegate only bounded slices with explicit ownership:
   - one file
   - one warning family within one project
   - one module documentation wave
4. For each worker batch, specify:
   - objective
   - owned files or subsystem
   - required validation commands
   - output format
   - reminder that other agents may be editing the repo
5. While workers run, use the main thread for non-overlapping tasks:
   - queue the next candidate slice
   - inspect the next hotspot
   - recompute branch size or warning distribution
6. After each completed batch:
   - integrate or verify the result
   - rerun the required validation
   - recompute the primary stop-condition metric
   - decide immediately whether to continue or stop
7. Do not require the user to manually trigger every round unless:
   - the next slice is ambiguous
   - a validation failure changes strategy
   - the batch objective conflicts with the active topic

## Task Tracking

For multi-batch work, keep recovery artifacts current.

- Update the active `ai-plan/public/<topic>/todos/` document when a meaningful batch lands.
- Update the matching `traces/` document with:
  - accepted delegated scope
  - validation milestones
  - current stop-condition metric
  - next recommended batch
- Keep the active recovery point concise; archive detailed history when it starts to sprawl.

## Delegation Defaults

- Prefer `worker` subagents for independent write slices.
- Prefer `explorer` subagents for read-only hotspot ranking or next-batch discovery.
- Keep each worker ownership boundary disjoint.
- Avoid launching a new batch when the expected write set would push the branch beyond the declared threshold without a
  deliberate decision.

## Completion

Stop the loop when any of the following becomes true:

- the primary stop condition has been reached or exceeded
- the remaining slices are no longer low-risk
- validation failures indicate the task is no longer repetitive
- the branch has grown large enough that reviewability would materially degrade

When stopping, report:

- which baseline was used
- the exact metric value at stop time
- completed batches
- remaining candidate batches
- whether further work should continue in a new turn or after rebasing/fetching

## Example Triggers

- `Use $gframework-batch-boot 75 to keep reducing analyzer warnings until the branch diff vs baseline approaches 75 files.`
- `Use $gframework-batch-boot 75 2000 to keep reducing warnings until the branch diff approaches 75 files or 2000 changed lines.`
- `Use $gframework-batch-boot and keep reducing analyzer warnings until the branch diff vs origin/main approaches 75 files.`
- `Use $gframework-batch-boot to continue this repetitive test refactor in bounded batches until the warning count drops below 10.`
- `Use $gframework-batch-boot and refresh module docs in waves without asking me to trigger every round.`
