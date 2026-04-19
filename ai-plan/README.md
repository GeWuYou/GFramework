# AI Plan

`ai-plan/` stores AI task recovery artifacts for this repository, but each subdirectory serves a different sharing and
bootstrapping purpose.

## Directory Semantics

- `public/README.md`
  - Shared startup index for `boot`.
  - Maps worktrees or branches to active topics and points at the primary tracking/trace entry paths.
  - Must list only active topics.
- `public/<topic>/todos/`
  - Repository-safe recovery documents for one active topic.
  - Use these for durable task state that another contributor or worktree may need to resume safely.
- `public/<topic>/traces/`
  - Repository-safe execution traces for one active topic.
  - Record decisions, validation milestones, and the immediate next step.
- `public/<topic>/archive/`
  - Stage-level archive for completed artifacts that still belong to an active topic.
  - Use this when a topic remains active, but some prior phase no longer belongs in the default boot path.
- `public/archive/<topic>/`
  - Completed-topic archive.
  - Move the entire topic directory here when that work direction is fully complete.
- `private/`
  - Worktree-private recovery space.
  - Use this for temporary notes, local scratch recovery points, or state that only matters in the current worktree.
  - Keep this directory untracked.

## Workflow Rules

- `boot` must read `public/README.md` first, then the mapped active topic directories, and only then fall back to
  scanning active topics directly.
- If no mapping exists for the current worktree or branch, scan `public/<topic>/` and ignore `public/archive/` unless
  the user explicitly asks for historical context.
- When a worktree changes its active topic set, update `public/README.md` in the same change.
- When a stage is complete, move the finished artifacts into `public/<topic>/archive/`.
- When a topic is complete, move the whole topic directory into `public/archive/<topic>/` and remove it from the
  shared startup index.

## Content Rules

- Never write secrets, tokens, credentials, private keys, hostnames, IP addresses, proprietary URLs, or other
  sensitive data.
- Never write absolute file-system paths, home-directory paths, or machine usernames.
- Use repository-relative paths, branch names, PR numbers, recovery-point IDs, and stable document identifiers
  instead.
- Keep committed `public/**` content concise, handoff-safe, and understandable without machine-local context.

## Naming

- Topic directories should be named by capability or work direction, for example:
  - `public/ai-plan-governance/`
  - `public/cqrs-rewrite/`
- Worktree-private files should live under a folder named for the current branch or worktree, for example:
  - `private/feat-cqrs-optimization/`
  - `private/gframework-cqrs/`
