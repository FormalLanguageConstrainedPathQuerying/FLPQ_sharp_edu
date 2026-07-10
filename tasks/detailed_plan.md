# Detailed Plan: Task 156 — Fix skill-doc duplication

## S1: Create canonical documentation conventions guide

**Code:** N/A
**Tests:** N/A
**Docs:** New `docs/developer/guides/documentation-conventions.md`

**Spec:**
- Aggregate all "what" content currently duplicated across skills into one document:
  - Module documentation structure (from `documentation` skill §Module Documentation)
  - Decision documentation standard (from `documentation` skill §Decision Documentation)
  - Book error recording format (from `documentation` skill §Book Errors)
  - Documentation Mapping Table (from `planning` skill §Documentation Mapping Table)
  - Doc review criteria (from `code-review` skill §4 Documentation)
- Document must be the single source of truth for all documentation requirements

## S2: Trim `documentation` skill to procedural "how" only

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/documentation/SKILL.md`

**Spec:**
- Remove §Module Documentation, §Decision Documentation, §Book Errors (these move to S1)
- Replace with concise procedure: "When writing/updating docs, load `docs/developer/guides/documentation-conventions.md` for content requirements, then..."
- Keep commit message section (it's procedural)
- The skill should describe *how* to write docs, not *what* docs contain

## S3: Deduplicate `planning` skill — remove mapping table, add reference

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/planning/SKILL.md`

**Spec:**
- Remove the inline Documentation Mapping Table
- Replace with a reference: "See `docs/developer/guides/documentation-conventions.md` for the full mapping of source changes to doc actions"
- Keep the subtask format template (Code/Tests/Docs sections) — it's procedural
- Update the Docs section template to reference the conventions doc

## S4: Deduplicate `subtask-loop` skill — replace checklist with reference

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/subtask-loop/SKILL.md`

**Spec:**
- Replace the Step 3 hard gate checklist with: "Update documentation per the `documentation` skill. The `documentation` skill references `docs/developer/guides/documentation-conventions.md` for the complete mapping of source changes to required doc updates."
- Keep the step structure, just remove the duplicated checklist

## S5: Deduplicate `code-review` skill — replace doc criteria with reference

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `.opencode/skills/code-review/SKILL.md`

**Spec:**
- Replace §4 Documentation criteria with: "Verify doc completeness per `docs/developer/guides/documentation-conventions.md`"
- Keep the review loop structure, just remove the duplicated doc criteria

## S6: Update `docs/main.md` to link the new conventions doc

**Code:** N/A
**Tests:** N/A
**Docs:** Edit `docs/main.md`

**Spec:**
- Add link to `docs/developer/guides/documentation-conventions.md` in the Developer Documentation section
