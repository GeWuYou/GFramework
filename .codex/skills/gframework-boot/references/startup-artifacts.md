# Startup Artifacts

## Required Reads

- `AGENTS.md`
- `.ai/environment/tools.ai.yaml`
- `local-plan/todos/`
- `local-plan/traces/`

## Local-Plan Selection Heuristics

- Match the user's wording against todo and trace file names first.
- Prefer the newest matching trace when several candidates describe the same feature area.
- If one file records a clearer recovery point than a newer but vague file, prefer the clearer recovery point.

## Complexity Defaults

- `simple`: keep everything local, no subagent
- `medium`: keep design local, optionally use one `explorer` for parallel read-only discovery
- `complex`: keep architecture and integration local, delegate only bounded non-blocking subtasks

## Model Defaults

- `explorer`: `gpt-5.1-codex-mini`
- `worker`: `gpt-5.4`

## Startup Summary Template

Use a short update before execution:

`Read AGENTS.md, the environment inventory, and the relevant local-plan artifacts. This looks like a <task-state> <complexity> task. I will <delegate-or-not> and start with <first-step>.`
