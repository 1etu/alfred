# 0008 — Folders for todos, with macOS-style colors

## Status

Accepted.

## Context

TODOs are one flat list. Real lives have shapes — work, home, a renovation, a
trip — and a flat list forces either prefix hacks or abandonment. The kanban
board and Plans exist for heavier structure; todos need exactly one light
level: folders.

## Decision

**Model** (`Alfred.Core`): a `TodoFolder` with id, name, colour key, and sort
order; a todo optionally references one folder. No nesting — one level, by
design. Deleting a folder moves its todos to the flat list (via Trash for the
folder itself); it never deletes contents silently.

**Colour** comes from a fixed palette of eight named folder colours defined
per-theme in `Alfred.Theme` (so a custom theme can retint the whole set). The
picker is a row of small filled circles — the familiar macOS tag idiom: a
ring appears around the selected dot, a subtle check inside on commit. Dots
are 20px with 24px hit targets (ADR
[0003](0003-accessibility-standard.md)), each carrying its colour name for
narrators.

**Presentation**: the TODOs page groups by folder with quiet section headers
(dot + name + count). A folder card — used on overview surfaces — renders a
paper-and-pocket folder shape that opens slightly on hover, letting the top
items peek out; a spring-out ease (the one sanctioned playful curve, still
under 500ms and reduced-motion aware). The card and the dot picker live in
`Alfred.UIKit` as `FolderCard` and `ColorDotPicker`, reusable by any future
domain that wants grouping (Notes will).

**Capture**: "#folder" in quick capture targets a folder, chipped live per
ADR [0004](0004-interaction-clarity.md).

## Consequences

One migration (folder table + nullable folder id on todos). The sidebar
stays flat — folders are page-level structure, not navigation, keeping the
sidebar's five calm groups intact.
