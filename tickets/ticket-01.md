# Ticket 1: Week proposal and KPIs

## User story

As an employee, I want Chronos to rebuild my week from my meetings, commits and absences,
so that I don't have to enter my time from memory.

## Acceptance criteria

1. `GET /` returns 200 and shows week 1. A week selector lists `2026-11-16` and `2026-11-23`, and
   `GET /?week=2026-11-23` shows week 2. `GET /?week=2020-01-06` shows `Week not found`: never a 500.
2. The page shows the employee name (`Alex Gagnon`), the team, and the week's dates. It has one section per day,
   with that day's entries in time order. Each entry shows its start, end, duration, source and category.
   The source is the meeting id and title, the work item id and title, or the absence type.
3. Both weeks match the reference proposal in the "Shared rules" section of `TICKET.md` exactly: entries, categories, worked time per day,
   time per category, and worked time for the week (rules R1 to R6). This is covered by unit tests on the domain
   classes (no web) for both weeks, and by one page test per week.
4. Week 2 has no entry for M21, and no time from commit `f3a4b5c` (no work item) or commit `b9c0d1e` (2026-11-28, outside the week).
   An "Ignored activity" section lists both commits with their reason: `no work item` or `outside the week`.
5. The KPI panel shows:
   - time per category;
   - worked vs target: `36:40 / 36:00` for week 1, `33:15 / 32:00` for week 2;
   - the share of Project time in worked time, rounded: `78%` for week 1, `75%` for week 2;
   - the meeting time (MGMT + ADMIN + 5843): `8:15` for both weeks.
6. A "Work items" section lists each work item that has time this week, with its id, title, type and PROJ time.
   It also lists that work item's pull requests with their state (`merged` or `open`). PR 5503 appears once per week:
   `open` in week 1 and `merged` in week 2.

## Out of scope

- Any connection to 7pace, Azure DevOps, Teams or another real system.
- Authentication, more than one employee, weeks other than the two data files.
