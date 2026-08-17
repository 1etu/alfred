# 0002 — Calm, warm, native: the design language

## Status

Accepted.

## Context

Alfred's feel is Apple-grade restraint; its mechanism is native Windows —
DWM, WindowChrome, Segoe UI Variable, GPU-composited WPF. Studying the best
personal task managers yields a consistent set of habits worth adopting and a
few worth rejecting.

## Decision

**Surfaces.** Hierarchy comes from value, not borders. Panels are soft
rounded rectangles a step away from the page background; the app has exactly
one hairline (sidebar/content divide). No gradients in chrome, no drop
shadows, no translucency by default.

**Type.** One family, few sizes, SemiBold as the loudest interface weight.
Page titles are large and confident; everything else defers.

**Metadata is quiet.** A row is a title plus, at most, a small grey second
line and a tiny glyph. Chips are reserved for state the user set on purpose
(a parsed amount, a chosen date, a category). Never icon-plus-label where the
label alone is clear.

**One accent per surface.** Colour belongs to icons and data. Red appears
only for the single most urgent truth on screen (overdue, destructive).

**Resolved values accompany relative phrases.** Anywhere the UI says
"tomorrow" or "in 3 weeks", the concrete date rides along ("in 3 weeks ·
Thu, Jun 8"). The user's words and the system's interpretation are always
shown together.

**Sections beat pages.** Grouping inside a page (headings, "this evening"
style splits, whitespace groups) is preferred over new sidebar entries.

**Empty states are designed.** Every empty surface gets the mascot, one
honest sentence, and the name of the thing — never a blank void, never a
tutorial.

## Consequences

These rules are testable in review: count the accents, count the borders,
check every relative phrase for its resolved value. Deviations need a written
reason. The kit (`Alfred.UIKit`) is the single implementation of this
language; pages that want to deviate change the kit or don't deviate.
