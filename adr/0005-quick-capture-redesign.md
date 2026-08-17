# 0005 — Quick capture v2

## Status

Accepted.

## Context

The capture overlay (Ctrl+N) is Alfred's front door and currently its worst
surface. Problems, honestly: the kind chips sit *below* the input so the mode
is chosen after typing; there is no feedback on what parsed; amounts must be
typed in full ("42.000"); nothing says what Enter will do; the scrim-and-card
presentation is heavier than the interaction it hosts.

## Decision

Rebuild capture around ADR [0004](0004-interaction-clarity.md):

1. **Kind first, visibly.** The kind selector sits above the input as a
   segmented row (Todo · Reminder · Expense · Payment · Income · Wish), with
   the active kind driving the placeholder ("What do you owe?") and the
   parser set. Ctrl+1…6 switches kind; typing certain verbs ("pay", "remind")
   suggests a kind switch as a dismissible chip rather than switching
   silently.
2. **Live parse chips** trail the input: amount, date, time, brand — each
   showing the resolved value, each removable. Amount shorthand from ADR
   [0006](0006-input-normalization.md) applies ("42k" chips as `₺42.000`).
3. **A commit line** under the input states the outcome in one sentence
   ("Enter adds this payment, due Thu 21 Aug") and doubles as the error line
   when something essential is missing.
4. **Keycap hints** (Enter · Esc · Tab) render as quiet keycaps, not prose.
5. **The surface calms down.** Lighter scrim, tighter card, suggestions
   expand inside the card exactly as elsewhere — the input stays one physical
   object.
6. Capture becomes a kit composition (`Alfred.UIKit`) so the same parsing,
   chips and hints serve every page's inline add bar, not only the overlay.

## Consequences

Capture stops being a separate dialect: page add-bars and the overlay share
one implementation and one behaviour. The kind-inference suggestion must
never override an explicit choice — a wrong silent guess would burn trust
faster than no guess.
