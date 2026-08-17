# V2 feature inventory

What Alfred V2 should be able to do, drawn from the best personal task
managers and Alfred's own money-first identity. Priority: **core** ships in
V2, **later** is acknowledged and deferred. This list feeds
[phases.md](phases.md); anything not here enters through an ADR.

## Capture and input

| Feature | Priority | Notes |
| --- | --- | --- |
| Quick capture overlay (Ctrl+N) with kind selector | core | Rebuilt per ADR 0005 |
| Live parse chips: amount, date, time, brand, kind | core | ADR 0004 |
| Amount shorthand (42k, 1.5k, 2m, ₺500, 500tl) | core | ADR 0006 |
| Natural dates with resolved values shown | core | Extends existing DateHints |
| Inline add bar on every list page | core | Same component as overlay |
| `#folder` targeting in capture | core | ADR 0008 |
| Paste a multi-line list → multiple items | later | |

## Todos and structure

| Feature | Priority | Notes |
| --- | --- | --- |
| Folders with colour, one level | core | ADR 0008 |
| macOS-style colour dot picker | core | Kit component |
| Checklists inside a todo | core | Small steps under one item |
| Due dates vs. scheduled dates kept distinct | core | Deadline ≠ do-date |
| Repeating todos and reminders | core | Reuse ledger Schedule |
| Attached note on any item | core | ADR 0009 editor |
| Tags and tag filtering | later | Folders first; tags if folders prove insufficient |
| Drag to reorder and re-file | core | Keyboard equivalent required |

## Notes

| Feature | Priority | Notes |
| --- | --- | --- |
| Notes page, two-pane, autosave | core | ADR 0009 |
| Markdown-lite rendering | core | Headings, emphasis, bullets, checkboxes |
| Note folders sharing todo folder idiom | core | |

## Money

| Feature | Priority | Notes |
| --- | --- | --- |
| Subcategories: Subscriptions, Bills, Rent & Home, Debts, One-off, Income | core | ADR 0010 |
| Inferred subcategory with one-click correction | core | |
| Subscriptions lens: monthly total, next renewals | core | |
| Per-scope summaries (due this month, net debt) | core | |
| Brand icons on entries | core | Exists; extend coverage |
| Multi-currency | later | TRY-first stays |

## Time

| Feature | Priority | Notes |
| --- | --- | --- |
| Month grid calendar with day panel | core | ADR 0011 |
| Week strip (shared with Meals) | core | |
| Everything dated renders, domain-dotted | core | |
| Today page: events panel + stat cards + agenda | core | Redesign of existing |
| This-evening style section on Today | later | Evaluate after Today redesign |
| Upcoming: week-by-week with drag reschedule | core | |

## Findability

| Feature | Priority | Notes |
| --- | --- | --- |
| Quick Find: type-anywhere global search | core | Titles first; note bodies included |
| Search shows kind + resolved dates in results | core | ADR 0004 everywhere |

## Platform

| Feature | Priority | Notes |
| --- | --- | --- |
| Localization EN/TR, live switch | core | ADR 0012 |
| Rebuilt updater with honest progress | core | ADR 0013 |
| WCAG 2.2 AA across the app | core | ADR 0003 |
| Reduced-motion support | core | ADR 0007 |
| User-authored themes | core | ADR 0014 |
| Widgets reflect subcategories | core | Snapshot gains subscription total |
| Global hotkey capture from outside the app | later | |
| Import/export (JSON) | later | Vault is already a file |
