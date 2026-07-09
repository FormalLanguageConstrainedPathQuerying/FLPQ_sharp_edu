---
name: documentation
description: Use when writing or updating documentation: module docs in docs/, recording book errors in fixes_for_book.md, or documenting design decisions. Covers content requirements, format, and protocols.
---

# Documentation

## Module Documentation

Each implemented module must have a dedicated documentation file in `docs/` (e.g., `docs/matrix.md`). The file must describe:

- **Type definitions with design rationale** — why each type exists and what problem it solves
- **All function signatures** with behavior, preconditions, and postconditions
- **Key design decisions** and their justification
- **Relationship to the book** (section/figure references) where applicable

Root documentation entry point is [`docs/main.md`](/docs/main.md). Use it for navigation.

## Decision Documentation

Each decision point and decision must be documented before implementation:

- Documentation must be detailed enough to reproduce an identical project from scratch without intermediate steps — anyone must be able to reimplement the project in another language using the documentation only
- Documentation must make it clear **why** a particular decision was made

## Commit Messages

Commit messages must be detailed enough to understand the reasons for changes. Anyone must be able to explain why particular changes were required using only the commit message.

## Book Errors

If a book error is found, record it in `tasks/fixes_for_book.md` with:

- Clear description of the error
- Suggested correction
- Notification to the user

If additional information not presented in the book was required, record it similarly.
