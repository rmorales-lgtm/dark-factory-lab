# Ticket 2: Fix entries and validate the week against Progi's rules

## User story

As an employee, I want to fix my entries and see which of Progi's time entry rules my week breaks,
so that I only submit a valid week.

## Acceptance criteria

1. The employee can edit an entry (start, end, category), delete an entry and add an entry.
   Changes are saved in SQLite for that week only, and they are still there after a page reload.
   A `Reset to proposal` button deletes the saved changes of the week and shows the proposal again.
2. Invalid input is refused with a message next to the field, and nothing is saved. Invalid input means:
   an end at or before the start, a date outside the week, or an unknown category.
3. A "Violations" panel checks Progi's rules. It lists one line per violation, with these exact messages:
   - **Overlap.** No entry may overlap another, absences included. Message: `Overlap: <A> and <B> on <date>`.
     Week 1: `Overlap: M4 and M5 on 2026-11-17`. Week 2: `Overlap: M17 and M18 on 2026-11-25`.
   - **Break.** Every worked day keeps one gap of at least 20 minutes with no entry, between its first and its last entry.
     Message: `Break: no 20-minute break on <date>`. Week 2: `Break: no 20-minute break on 2026-11-23` (M13 covers 12:00–12:20).
   - **Target.** The week total must equal the target (40 hours minus absences).
     Message: `Total <worked> does not match target <target>`. Week 1: `Total 36:40 does not match target 36:00`.
     Week 2: `Total 33:15 does not match target 32:00`.
   - **Training code.** An entry created from a meeting that has a `trainingCode` must keep category `5843`.
     Message: `Training must use code 5843: <meeting id> on <date>`. Example: change M8 to `MGMT` →
     `Training must use code 5843: M8 on 2026-11-19`.
   - **Category.** Every entry has a category. Message: `Missing category: <start>-<end> on <date>`.
     The provided data has none; a unit test with test data covers it.
4. The KPI panel of Ticket 1 also shows the number of violations: `2` for the week 1 proposal and `3` for the week 2 proposal.
   All KPIs are recalculated from the saved entries.
5. A week with no violation shows `Week is valid`. Scenario for a test: in week 1,
   - change M5 to 15:00–15:30 (2026-11-17 becomes 8:00);
   - change the 2026-11-20 entry 12:20–15:00 to 12:30–15:00 (2026-11-20 becomes 8:00).

   The result is worked `36:00 / 36:00`, 0 violations and `Week is valid`.
6. Each violation type has its own unit test on the domain classes (no web), and one page test shows the messages of each week.

## Out of scope

- Fixing violations automatically.
- Ignored activity (Ticket 1, criterion 4) is information only, not a violation.
