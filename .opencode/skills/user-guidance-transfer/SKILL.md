---
name: user-guidance-transfer
description: Use when the Blocked Work Protocol fires and user provides guidance, or when a task is found ambiguous/underspecified during planning. Persists user guidance as a comment on the task issue so future executions do not hit the same ambiguity.
---

# User Guidance Transfer

When a task is ambiguous or underspecified, the agent must ask the user for clarification (Blocked Work Protocol). After the user responds, **persist the guidance into the task itself** so the task carries its own clarifications forward.

## Protocol

### 1. Detect Ambiguity

During planning or subtask execution, detect that a task is unclear:

- Missing constraints, scope boundaries, or expected outputs
- Conflicting requirements within the task
- Algorithmic gap requiring design decisions (existing Blocked Work Protocol)

### 2. Ask User

Ask concrete, specific questions. Do not ask "what should I do?" — ask about the unresolved choice:

- "Should X be computed eagerly or lazily?"
- "Does 'combined' mean intersection or union?"
- "The task specifies A but the codebase does B — should the task override the codebase?"

### 3. Receive Guidance

User responds. The guidance is authoritative — it resolves the ambiguity.

### 4. Transfer to Task

Post the guidance verbatim as a comment on the task issue:

```bash
gh issue comment <N> --body "**[USER GUIDANCE]**: Concise clarification text. 1-2 lines."
```

Rules:

- **Additive only** — never modify, delete, or rewrite the original task text (the issue body is user-authored and immutable)
- **Concise** — capture the clarification in 1-2 lines, not full design rationale
- **Chronological stack** — if multiple guidance comments are added over time, they stack chronologically in the issue's comment thread (oldest first, newest last); never edit an earlier comment
- **Persist permanently** — guidance comments remain even after the task is closed, as a record of decisions made

### 5. Record in Detailed Plan

Also record the guidance in `tasks/detailed_plan.md` under `## Design Notes` (existing Blocked Work Protocol). The detailed plan captures full algorithmic rationale; the issue comment captures the concise clarification for fast re-reading.

### 6. Resume Execution

Proceed with the task using the clarified understanding. The guidance comment ensures that if the task is interrupted and resumed, the clarification is immediately visible in the issue thread.

## When NOT to Use

- **Simple fact questions** ("what does flag -a do?") — ask, get answer, proceed. No annotation needed.
- **Design discussions** that don't change task scope — record only in `detailed_plan.md` Design Notes.
- **Implementation details** that are obvious from the codebase — no user guidance needed.

## Integration Points

| Skill | Integration |
| --- | --- |
| `subtask-loop` | Blocked Work Protocol references this skill for step "transfer user guidance to task" |
| `planning` | Ambiguities found during detailed plan creation → ask user → post the guidance as an issue comment before decomposition |
