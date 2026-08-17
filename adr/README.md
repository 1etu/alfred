# Architecture decision records

Numbered, immutable decisions. A new decision that changes an old one gets a
new number and marks the old one superseded — history is never rewritten.

| ADR                                            | Decision                                        |
| ---------------------------------------------- | ----------------------------------------------- |
| [0001](0001-v2-product-vision.md)              | V2 vision: a product, one page at a time        |
| [0002](0002-design-language.md)                | Calm, warm, native — the design language        |
| [0003](0003-accessibility-standard.md)         | WCAG 2.2 AA is a ship gate                      |
| [0004](0004-interaction-clarity.md)            | The user never guesses — interpretation is visible |
| [0005](0005-quick-capture-redesign.md)         | Quick capture v2                                |
| [0006](0006-input-normalization.md)            | Amount and date shorthand ("42k" → 42.000)      |
| [0007](0007-motion-language.md)                | Motion: confirm, never entertain                |
| [0008](0008-todo-folders.md)                   | Folders for todos, with macOS-style colors      |
| [0009](0009-notes.md)                          | Notes as a first-class domain                   |
| [0010](0010-payments-subcategories.md)         | Payments split into subcategories               |
| [0011](0011-calendar-views.md)                 | Calendar rebuilt as a real month grid           |
| [0012](0012-localization.md)                   | Alfred.Localization — no hardcoded messages     |
| [0013](0013-update-engine.md)                  | Alfred.Update — a lighter, precise updater      |
| [0014](0014-theme-authoring.md)                | User-authored themes from Settings              |

[features.md](features.md) is the V2 feature inventory the ADRs draw from.
[phases.md](phases.md) is the execution roadmap.

## Format

Each ADR: **Status** (accepted / superseded by NNNN), **Context** (the
problem, honestly), **Decision** (what we chose, concretely), and
**Consequences** (what it costs and what it buys).
