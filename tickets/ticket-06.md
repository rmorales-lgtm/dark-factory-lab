# Ticket 6 (stretch): Smart suggestions that remember corrections

Depends on: Ticket 2 (editing). Start only when Tickets 1 to 4 pass.

## User story

As an employee, I want Chronos to remember the category I correct on a meeting,
so that I don't have to fix the same meeting again next week.

## Rules

- The week proposal of Ticket 1 is already the suggestion (a meeting becomes `MGMT`, a Calendar meeting becomes `ADMIN`).
  This ticket adds the memory.
- A remembered correction is one rule: meeting title → category. The title is compared trimmed and ignoring case.
  There is one rule per title: the latest correction replaces the previous one. Rules are global (not per week) and stored in SQLite.
- A rule is created when the employee saves an edit that changes the category of an entry that comes from a meeting
  without a `trainingCode`. Nothing else creates a rule: not a time change, not an edit of a `PROJ` or added entry,
  not a change of a training meeting (rule R2: it stays `5843`), not an invalid edit.
- Rules apply only when a proposal is built: a week without saved changes, or after `Reset to proposal`.
  Saved entries are never rewritten, so a `Submitted` or `Approved` week never changes.

## Acceptance criteria

1. On week 1, the employee changes the category of `M1` (`Daily stand-up`) from `MGMT` to `ADMIN` and saves. A panel
   `Remembered corrections` shows the line `Daily stand-up: ADMIN`, on every week page and after an app restart.
2. Week 2 has no saved changes, so its proposal now shows `M12`, `M14`, `M16` and `M19` (the stand-ups) as `ADMIN`, each with the
   label `Suggested from your correction`. `M13`, `M17` and `M18` stay `MGMT`. Week 2 KPIs: `ADMIN` `1:30`, `MGMT` `2:45`,
   meeting time still `8:15`, violations still `3`.
3. Week 1 has saved changes, so its other stand-ups (`M3`, `M6`, `M7`, `M9`) stay `MGMT`. After `Reset to proposal`
   on week 1, `M1`, `M3`, `M6`, `M7` and `M9` are all `ADMIN` with the label `Suggested from your correction`.
4. The latest correction wins: changing a stand-up back to `MGMT` replaces the rule, which then reads `Daily stand-up: MGMT`.
   There is never more than one line for the same title.
5. Each remembered correction has a `Forget` button (POST, with the antiforgery token). After `Forget`, the line is gone
   and the week 2 proposal is back to the Ticket 1 reference (stand-ups `MGMT`, no label).
6. Nothing is remembered for: changing `M8` (training) to `MGMT` (the violation `Training must use code 5843: M8 on 2026-11-19`
   still appears), changing only a start or end time, editing a `PROJ` entry, adding an entry, or an invalid edit.
7. The rules are applied by a pure domain class with unit tests (no web): title matching (case and spaces), one rule per title,
   what creates a rule and what does not. One page test runs the week 1 to week 2 scenario above.

## Out of scope

- Remembering times, work items or deletions. Learning from pull requests or commits.
- A rule that depends on the day or on the meeting's attendees.
