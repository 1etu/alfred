# 0009 — Notes as a first-class domain

## Status

Accepted.

## Context

Alfred's promise includes notes, but V1 shipped none. Thoughts currently get
mangled into todos or lost. The gap also weakens other domains — a payment or
plan cannot carry supporting text.

## Decision

**Scope, deliberately small.** Notes are plain text with a title, created
instantly, saved continuously, searched fully. Formatting is a light
markdown subset rendered in place: headings, bold/italic, bullet and
checkbox lines, nothing else. No embeds, no attachments, no sync — the vault
owns them like everything else.

**Model** (`Alfred.Core`): `Note` with title, body, created/modified UTC,
optional folder (reusing `TodoFolder`'s colour idiom via a shared folder
concept from ADR [0008](0008-todo-folders.md)), soft-deleted through the
existing Recycler.

**Page**: a two-pane surface — note list left (title + first line + relative
date), editor right, the app's only two-pane page. The editor is honest
paper: title as a large field, body under it, zero chrome until hover.
Autosave is debounced through the vault's existing write path; there is no
Save button anywhere.

**Item notes**: the same body editor attaches to a todo, payment or plan as
an expandable detail — one implementation in `Alfred.UIKit`, used by every
domain. A row with a note shows the quiet page glyph after its title.

**Capture**: a Note kind joins quick capture; the title parses, the body
opens focused.

## Consequences

A new vault table and a new sidebar entry (the sidebar's "work" group grows
to four). The markdown subset is a hard line — every rejected feature
request points at this ADR. Full-text search lands with the Quick Find phase
and indexes note bodies from day one.
