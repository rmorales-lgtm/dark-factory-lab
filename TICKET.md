# Tickets: Mission Chronos

The tickets are one file each, in the `tickets/` folder. Read and build them in this order, each one on top of the previous:

| Order | File | Ticket | Level |
|---|---|---|---|
| 1 | `tickets/ticket-01.md` | Week proposal and KPIs | Minimum |
| 2 | `tickets/ticket-02.md` | Fix entries and validate against Progi's rules | Minimum |
| 3 | `tickets/ticket-03.md` | Roles and approval workflow | Minimum |
| 4 | `tickets/ticket-04.md` | Quality and security baseline | Minimum |
| 5 | `tickets/ticket-05.md` | 7pace CSV export of an approved week | Stretch |
| 6 | `tickets/ticket-06.md` | Smart suggestions that remember corrections | Stretch |
| 7 | `tickets/ticket-07.md` | Upload a week and see it as a calendar | Stretch |

Tickets 1 to 4 are the minimum. Start Ticket 5 only when the evaluator passes Tickets 1 to 4, then 6, then 7.
The shared rules (R1 to R6) and the reference proposal below apply to every ticket.

Chronos rebuilds an employee's week from what they actually did (meetings, commits, absences),
checks it against Progi's time entry rules, and gets it approved before it goes to 7pace.
All data is simulated: `data/week1.json` and `data/week2.json` (fictitious employee, fictitious work items).

## How to run the app

- Solution: `Chronos.sln` at the repo root.
- Web app: `src/Chronos.Web` (ASP.NET Core Razor Pages, .NET 10). Tests: `tests/Chronos.Tests` (xUnit).
- Run: `dotnet run --project src/Chronos.Web --urls http://localhost:5080`
- UI: http://localhost:5080 (the home page shows week 1).
- Tests: `dotnet test` from the repo root.
- Input data: `data/week1.json` and `data/week2.json`, read-only. Their folder comes from the config key `Chronos:DataDirectory`.
- Local storage: SQLite file `chronos.db` in the web app's content root, created at startup and git-ignored.

## Shared rules (all tickets)

Times are shown as `h:mm` (e.g. `8:30`, `36:40`). Durations are computed in minutes.

- **R1. Workday.** 08:00–16:20, with the break at 12:00–12:20. The break is never an entry.
- **R2. Meetings.** Each meeting becomes one entry with its own start and end, even if it overlaps another one
  or goes past 16:20. A meeting with `"cancelled": true` creates no entry.
  Its category is the `trainingCode` if present (`5843`). Otherwise it is `ADMIN` if `source` is `Calendar`, and `MGMT` in all other cases.
- **R3. Absences.** Category `ABSENCE`, labelled with their type (`Sick`, `Vacation`). The period `morning` is 08:00–12:00,
  `afternoon` is 12:20–16:20, and `full day` is 08:00–12:00 plus 12:20–16:20.
- **R4. Project time.** Every free slot of the workday becomes a `PROJ` entry. A free slot is a slot not covered by a meeting,
  an absence or the break. Each slot goes to the work item of the **first commit of that day** that has a work item.
  If no commit of that day has a work item, the entry has **no category**.
- **R5. Ignored activity.** A commit with `workItemId: null` or dated outside the week creates no time.
  Pull requests never create time.
- **R6. Target.** The week's target is `expectedHours` minus the absence hours: 36:00 for week 1, 32:00 for week 2.
  "Worked" is the sum of every entry that is not `ABSENCE`.

### Reference proposal (the tests must assert these exact values)

Week 1 (2026-11-16 to 2026-11-20). Worked `36:40`, target `36:00`.

| Day | Entries (start–end source category) | Worked |
|---|---|---|
| 2026-11-16 | 08:00–09:00 1201 PROJ · 09:00–09:15 M1 MGMT · 09:15–12:00 1201 PROJ · 12:20–13:30 1201 PROJ · 13:30–15:00 M2 MGMT · 15:00–16:20 1201 PROJ | 8:00 |
| 2026-11-17 | 08:00–09:00 1202 PROJ · 09:00–09:15 M3 MGMT · 09:15–12:00 1202 PROJ · 12:20–14:00 1202 PROJ · 14:00–15:00 M4 MGMT · 14:30–15:30 M5 MGMT · 15:30–16:20 1202 PROJ | 8:30 |
| 2026-11-18 | 08:00–09:00 1203 PROJ · 09:00–09:15 M6 MGMT · 09:15–12:00 1203 PROJ · 12:20–16:20 Sick ABSENCE | 4:00 |
| 2026-11-19 | 08:00–09:00 1203 PROJ · 09:00–09:15 M7 MGMT · 09:15–10:00 1203 PROJ · 10:00–12:00 M8 5843 · 12:20–16:20 1203 PROJ | 8:00 |
| 2026-11-20 | 08:00–09:00 1203 PROJ · 09:00–09:15 M9 MGMT · 09:15–12:00 1203 PROJ · 12:20–15:00 1203 PROJ · 15:00–16:00 M10 MGMT · 16:00–16:30 M11 ADMIN | 8:10 |

Week 1 by category: PROJ `28:25`, MGMT `5:45`, 5843 `2:00`, ADMIN `0:30`, ABSENCE `4:00`.

Week 2 (2026-11-23 to 2026-11-27). Worked `33:15`, target `32:00`.

| Day | Entries (start–end source category) | Worked |
|---|---|---|
| 2026-11-23 | 08:00–09:00 1203 PROJ · 09:00–09:15 M12 MGMT · 09:15–11:30 1203 PROJ · 11:30–13:00 M13 MGMT · 13:00–16:20 1203 PROJ | 8:20 |
| 2026-11-24 | 08:00–09:00 1204 PROJ · 09:00–09:15 M14 MGMT · 09:15–12:00 1204 PROJ · 12:20–13:00 1204 PROJ · 13:00–17:00 M15 5843 | 8:40 |
| 2026-11-25 | 08:00–09:00 1205 PROJ · 09:00–09:15 M16 MGMT · 09:15–10:00 1205 PROJ · 10:00–10:30 M17 MGMT · 10:15–11:00 M18 MGMT · 11:00–12:00 1205 PROJ · 12:20–16:20 1205 PROJ | 8:15 |
| 2026-11-26 | 08:00–09:00 1205 PROJ · 09:00–09:15 M19 MGMT · 09:15–12:00 1205 PROJ · 12:20–15:30 1205 PROJ · 15:30–16:00 M20 ADMIN · 16:00–16:20 1205 PROJ | 8:00 |
| 2026-11-27 | 08:00–12:00 Vacation ABSENCE · 12:20–16:20 Vacation ABSENCE (M21 is cancelled) | 0:00 |

Week 2 by category: PROJ `25:00`, MGMT `3:45`, 5843 `4:00`, ADMIN `0:30`, ABSENCE `8:00`.