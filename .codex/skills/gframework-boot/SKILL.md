---
name: gframework-boot
description: Repository-specific boot workflow for the GFramework repo. Use when Codex needs to start or resume work in this repository from short prompts such as "boot", "continue", "read AGENTS", or "start the next step"; when the user expects Codex to first read AGENTS.md, .ai/environment/tools.ai.yaml, and local-plan tracking files; or when Codex should assess task complexity, decide whether explorer or worker subagents are warranted, and then proceed under the repository's workflow rules.
---

# GFramework Boot

## Overview

Use this skill to bootstrap work in the GFramework repository with minimal user prompting.
Treat `AGENTS.md` as the source of truth. Use this skill to enforce a startup sequence, not to replace repository rules.

## Startup Workflow

1. Read `AGENTS.md` before choosing tools, planning edits, or delegating work.
2. Read `.ai/environment/tools.ai.yaml` to confirm the preferred local toolchain.
3. Inspect `local-plan/todos/` and `local-plan/traces/` before asking the user for missing context.
4. Classify the task state:
   - `new`: no matching recovery document exists, or the user is clearly starting fresh work
   - `resume`: a matching todo or trace exists and the user is continuing that thread
   - `recovery`: prior work looks partial, interrupted, or ambiguous and the next safe recovery point must be reconstructed
5. Choose the best matching `local-plan` artifacts:
   - Prefer path names or headings that match the user's task wording
   - Break ties by most recently updated trace or todo
   - If ambiguity would materially change implementation, summarize the candidates and ask one concise question
6. Classify the task complexity before deciding on subagents:
   - `simple`: one concern, one file or module, no parallel discovery required
   - `medium`: a small number of modules, some read-only exploration helpful, critical path still easy to keep local
   - `complex`: cross-module design, migration, large refactor, or work likely to exceed one context window
7. Apply the delegation policy from `AGENTS.md`:
   - Keep the critical path local
   - Use `explorer` with `gpt-5.1-codex-mini` for narrow read-only questions, tracing, inventory, and comparisons
   - Use `worker` with `gpt-5.4` only for bounded implementation tasks with explicit ownership
   - Do not delegate purely for ceremony; delegate only when it materially shortens the task or controls context growth
8. Before editing files, tell the user what you read, how you classified the task, whether subagents will be used, and the first implementation step.
9. Proceed with execution, validation, and documentation updates required by `AGENTS.md`.

## Task Tracking

For multi-step, cross-module, or interruption-prone work, maintain the repository recovery artifacts instead of keeping state only in chat.

- Update the active document under `local-plan/todos/` with completed work, validation results, risks, and the next recovery point.
- Update the matching document under `local-plan/traces/` with key decisions, delegated scope, and the immediate next step.
- If the task is clearly complex and no recovery artifact exists yet, create one before substantive edits.

## Recovery Heuristics

- If the user says `next step`, `continue`, `继续`, or similar resume language, assume recovery mode and search `local-plan/` first.
- If the current branch and the newest recovery documents describe the same feature area, prefer resuming that thread.
- If the repository state suggests in-flight work but no recovery document matches, reconstruct the safest next step from code, tests, and Git state before asking the user for clarification.

## Example Triggers

- `boot`
- `Use $gframework-boot and continue the current task`
- `Read AGENTS and local-plan, then start the next step`
- `继续当前任务，先看 AGENTS.md 和 local-plan`

## References

Read `references/startup-artifacts.md` when you need a quick reminder of the repository entrypoints, task-state heuristics, or delegation defaults without re-reading the entire skill.
