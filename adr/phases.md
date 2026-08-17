# V2 phases

The execution order for [features.md](features.md). Each phase lands as a
stream of small commits to `main`, every commit buildable and green. Budgets
are estimates that keep the whole arc inside ~100–150 commits.

Rules of the road: a phase is done when its pages pass the finished-page bar
(both themes, DPI scales, keyboard paths, empty states, WCAG). Reusable
pieces discovered mid-phase are extracted to `Alfred.UIKit` in the same
phase. Later phases may reopen earlier pages only through a new ADR.

| # | Phase | Contents | ~commits |
| --- | --- | --- | --- |
| 0 | Foundation | Theme framework, UIKit, page reset — **done** | 6 |
| 1 | Platform libraries | `Alfred.Localization` (ADR 0012), `Alfred.Update` (ADR 0013), `AmountHints` (ADR 0006), light-theme contrast fix (ADR 0003) | 10 |
| 2 | Kit vocabulary | Parse chips, keycap hints, `ColorDotPicker`, `FolderCard`, shimmer + motion helpers (ADR 0007), day strip, segmented scope control, focus visuals | 14 |
| 3 | Capture v2 | Overlay rebuild on the new vocabulary, kind inference, commit line, shared inline add bar (ADR 0005) | 10 |
| 4 | Meals | First redesigned page: week strip, slots, add bar | 6 |
| 5 | Todos + folders | Folder model + migration, grouped page, checklists, repeat, drag/keyboard re-file (ADR 0008) | 14 |
| 6 | Notes | Model, two-pane page, markdown-lite, item notes everywhere (ADR 0009) | 12 |
| 7 | Money | Subcategories + inference + migration, Payments lenses and summaries, widget totals (ADR 0010) | 12 |
| 8 | Calendar + Upcoming | Month grid, day panel, week reuse, Upcoming rebuild (ADR 0011) | 12 |
| 9 | Today | Events panel, stat cards, agenda rows, greeting — the flagship page last among pages, when the kit is sharpest | 8 |
| 10 | Plans, Board, Wishes, Trash | Remaining pages on the mature kit | 10 |
| 11 | Quick Find | Global search over titles and note bodies, kind-aware results | 6 |
| 12 | Localization sweep | Every remaining literal into catalogs; Turkish complete; language row in Settings | 6 |
| 13 | Theme authoring | Gallery, derive-and-edit, contrast-checked editor (ADR 0014) | 8 |
| 14 | Accessibility + performance audit | Full WCAG pass with tests, palette corrections, startup/frame budgets re-measured, reduced-motion verification | 6 |
| 15 | Release | Docs, README, changelog, `v2.0.0` tag | 4 |

Total ≈ 144 commits.

## Sequencing logic

Platform libraries precede everything so no phase hardcodes a string or
reinvents parsing. The kit vocabulary precedes capture; capture precedes the
pages because every page embeds its add bar. Meals goes first among pages as
the warm-up; Today goes last among the core pages so the most-seen screen is
built with the most-practiced hand.
