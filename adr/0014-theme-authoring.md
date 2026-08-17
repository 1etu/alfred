# 0014 — User-authored themes from Settings

## Status

Accepted.

## Context

The theme framework already treats a theme as data: a name, a light/dark
base, colour overrides, icon-colour overrides, JSON persistence. What does
not exist is the surface where a person makes one.

## Decision

Settings gains a Themes section, built after the core pages are redesigned
(it depends on the kit's final vocabulary):

- **Gallery first.** Built-ins and user themes as preview cards — each card
  a miniature of the real shell (sidebar, rows, accent) rendered from the
  theme's actual palette, not a screenshot. One click applies.
- **Derive, don't start blank.** "New theme" always begins from an existing
  one; the editor lists semantic roles (background, text, accent, money in /
  out, folder colours, icon hues) with the same macOS-style colour dots used
  by folders, plus a free picker per role.
- **Contrast is enforced at author time.** The editor runs the same WCAG
  checks as the palette tests (ADR
  [0003](0003-accessibility-standard.md)) and marks failing pairs on the
  spot; a failing theme can be saved but is labelled, honestly, as failing.
- **Themes are files.** User themes persist as JSON in the app data folder;
  sharing a theme is sharing a file. No store, no accounts.

## Consequences

The theme framework's key set becomes a public contract — renaming a
semantic key breaks user theme files, so key renames now require a migration
step. The gallery card renderer doubles as a regression harness: every
built-in change is visible at a glance.
