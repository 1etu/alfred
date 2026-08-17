# 0003 — WCAG 2.2 AA is a ship gate

## Status

Accepted.

## Context

V1 was checked by eye. Measured, parts of it fail: light-theme secondary text
`#86868B` on white is ~3.6:1 (AA requires 4.5:1 for body text), and the
sidebar count badge `#A0A5AC` on `#F1F1F3` is ~2.2:1 — below even the 3:1
large-text floor. A product for strangers includes strangers who see, hear,
and move differently.

## Decision

Every finished page passes WCAG 2.2 AA, interpreted for a Windows desktop
app:

1. **Contrast.** Text ≥ 4.5:1; large text (≥ 18.66px bold / 24px) and
   essential UI glyphs ≥ 3:1 — in both themes. The theme framework is the
   enforcement point: palettes are validated by automated contrast tests in
   `Alfred.Theme.Tests`, so a palette that fails cannot ship.
2. **Keyboard.** Every action reachable without a mouse; focus visible;
   no keyboard traps; shortcuts rebindable (already true) and conflict-free.
3. **Names.** Every interactive element carries `AutomationProperties.Name`;
   images that convey meaning are named, decorative ones are not.
4. **Motion.** All animation respects the OS "show animations" setting; no
   information is conveyed by motion alone. See ADR
   [0007](0007-motion-language.md).
5. **Targets.** Pointer targets ≥ 24×24px (WCAG 2.2 target-size minimum),
   with row-level targets much larger.
6. **Text.** No text in bitmaps; the UI survives 100–200% DPI and bold
   system text.

Immediate corrections: light-theme `TextSecondary` darkens to meet 4.5:1;
badge colours are corrected in the accessibility phase's full palette audit.

## Consequences

Some greys get slightly darker; the calm look is preserved by spacing and
weight, not by illegibly light ink. Contrast tests make the standard cheap to
keep: adding a colour means adding its pairing to the test.
