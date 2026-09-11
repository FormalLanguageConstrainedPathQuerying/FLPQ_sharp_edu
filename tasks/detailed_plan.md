# Task 267: Fix code-review skill ordering — review before the hard gate

## Task description (verbatim)

Fix code-review skill ordering: review runs before the hard gate, not after. Update the skill's intro line and Prerequisites section, which currently require quality gates to pass before code review (contradicting the AGENTS.md working loop: step 6 review, then step 7 gate).

## Context and analysis

The `code-review` skill states in two places that quality gates must pass
BEFORE code review:

1. Intro line: "Performed at task completion — after all subtasks are
   committed **and quality gates pass**, before marking `[done]`."
2. Prerequisites item 2: "Quality gates pass (format check, lint, build,
   tests, coverage) — see `quality-gates` skill"

The AGENTS.md working loop runs review (step 6) BEFORE the hard gate
(step 7), and all recent tasks (263/264/265/266) followed that order.
User decision (2026-09-10): **review before the gate** is canonical —
fix the skill, not AGENTS.md. Rationale: review fixes are then included
in the final gate run, so the gate validates the code as merged.

## Subtasks

### S1: Update code-review skill intro line and Prerequisites — [done]

**Code:** none (docs-only).
**Tests:** skip (docs-only).
**Docs:** `.opencode/skills/code-review/SKILL.md`.

**Spec:**
- Intro line: replace "after all subtasks are committed and quality gates pass, before marking `[done]`" with wording that places review after subtask commits, BEFORE the hard gate, referencing the AGENTS.md working loop (step 6 → step 7).
- Prerequisites: remove item 2 (quality gates pass); keep "all subtasks committed" and "no uncommitted changes"; add a note that the hard gate runs AFTER code review per the AGENTS.md working loop, so review fixes are included in the final gate run.
