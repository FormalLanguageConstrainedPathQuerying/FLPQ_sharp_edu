---
name: user-guidance-transfer
description: Use when the Blocked Work Protocol fires and user provides guidance, or when a task is found ambiguous/underspecified during planning. Persists user guidance back into the task description so future executions do not hit the same ambiguity.
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

Append guidance to the task in `tasks/tasks.md`. The annotation goes after the task's **full formulation** (including all sub-items), before the next task number:

```
XXX. Task description line 1
     1.   Sub-item 1
     2.   Sub-item 2
     **[USER GUIDANCE]**: Concise clarification text. 1-2 lines.
```

Rules:

- **Additive only** — never modify, delete, or rewrite the original task text
- **Concise** — capture the clarification in 1-2 lines, not full design rationale
- **Chronological stack** — if multiple guidance annotations are added over time, stack them in chronological order (oldest first, newest last), each on its own indented line
- **Persist permanently** — guidance annotations remain even after `[done]`, as a record of decisions made
- **Blank line separator** — keep the existing inter-task blank line after the guidance annotation(s), before the next task

### 5. Record in Detailed Plan

Also record the guidance in `tasks/detailed_plan.md` under `## Design Notes` (existing Blocked Work Protocol). The detailed plan captures full algorithmic rationale; the task annotation captures the concise clarification for fast re-reading.

### 6. Resume Execution

Proceed with the task using the clarified understanding. The guidance annotation ensures that if the task is interrupted and resumed, the clarification is immediately visible.

## When NOT to Use

- **Simple fact questions** ("what does flag -a do?") — ask, get answer, proceed. No annotation needed.
- **Design discussions** that don't change task scope — record only in `detailed_plan.md` Design Notes.
- **Implementation details** that are obvious from the codebase — no user guidance needed.

## Integration Points

| Skill | Integration |
|-------|------------|
| `subtask-loop` | Blocked Work Protocol references this skill for step "transfer user guidance to task" |
| `planning` | Ambiguities found during detailed plan creation → ask user → annotate task before decomposition |
