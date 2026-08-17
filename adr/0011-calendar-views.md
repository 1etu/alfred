# 0011 — Calendar rebuilt as a real month grid

## Status

Accepted.

## Context

V1's calendar is a list wearing a calendar's name. Windows deserves the
calendar idiom desktop users already know from the best native calendars: a
month as a true grid of days, weeks as rows, density without clutter.

## Decision

**Month view** is the core: a 7-column grid, weekday initials as a quiet
header row, day numbers top-right in each cell, today marked by a filled
accent circle around its number — the page's single accent. Out-of-month
days at reduced opacity. Each cell stacks up to three item pills (tiny
colour-dotted, single-line) plus a "+2 more" overflow line; clicking a day
opens a day panel listing everything, where items can be added inline with
the shared capture bar.

**Week strip**: above the grid, the selected week renders as a horizontal
day strip reused from Meals' day picker — one control in the kit, two
consumers.

**What renders**: everything dated — reminders, todos with due dates,
scheduled payments (with amount), meals — colour-dotted by domain using the
sidebar icon hues. Microsoft-synced events render read-only in their
calendar's tint. A legend is unnecessary because the dots match the sidebar
the user already knows.

**Navigation**: PgUp/PgDn months, Home returns to today, arrows move the
day focus — the entire grid is keyboard-walkable with a visible focus ring
(ADR [0003](0003-accessibility-standard.md)).

**Virtualization**: months render on demand; the grid is cheap enough to
never own a loading state.

## Consequences

The calendar becomes a read-write hub rather than a report. Pill truncation
rules and the day panel must be designed once, in the kit, because Upcoming
will reuse both. Sync remains one-way (Alfred → Microsoft) — rendering
remote events read-only avoids conflict resolution entirely, and that
boundary is restated here on purpose.
