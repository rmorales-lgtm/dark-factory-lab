# Ticket 5 (stretch): 7pace CSV export of an approved week

Depends on: Ticket 3 (approval). Start only when Tickets 1 to 4 pass.

## User story

As an employee, I want to export my approved week as a CSV file ready to import into 7pace,
so that I don't have to type my time again.

## CSV format

No format was provided by the clients: this is the format to implement and test (assumptions are marked).

- Encoding UTF-8, line separator `\n`, one header line, fields separated by `,`.
- Header, exactly: `Date,Start,End,Hours,Category,WorkItemId,Description`
- One row per saved entry of the week (the entries as shown on the week page, edits included), except `ABSENCE` entries
  (assumption: absences are not entered as time in 7pace). Rows are sorted by date, then start, then end.
- `Date` is `yyyy-MM-dd`, `Start` and `End` are `HH:mm`. `Hours` is the duration as a decimal with 2 digits and a dot
  (`0.25`, `1.50`, `1.17`).
- `Category` is the entry's category (`PROJ`, `MGMT`, `ADMIN`, `5843`). `WorkItemId` is the work item id of a `PROJ` entry,
  empty otherwise.
- `Description` is the meeting title, the work item title, or `Manual entry` for an entry added by the employee.
- A field containing `,`, `"` or a line break is wrapped in double quotes, with inner quotes doubled.
  A field starting with `=`, `+`, `-` or `@` is prefixed with `'` so spreadsheets don't execute it.

## Acceptance criteria

1. The week page shows an `Export CSV` button (a link to `GET /Export?week=<start>`) only when the status is `Approved`.
2. `GET /Export?week=<start>` on a week that is not `Approved` answers 409 with the text `Week is not approved`.
   An unknown week answers 404 with `Week not found`. Neither is a 500.
3. For an approved week 1 (the Ticket 2 criterion 5 scenario, then submit and approve), `GET /Export?week=2026-11-16`
   answers 200 with `Content-Type: text/csv` and a `Content-Disposition` header with the file name `chronos-2026-11-16.csv`.
4. That file has exactly the header above and 27 data rows (no `ABSENCE` row), sorted. The rows include:
   - `2026-11-16,08:00,09:00,1.00,PROJ,1201,Add parts search filter`
   - `2026-11-16,12:20,13:30,1.17,PROJ,1201,Add parts search filter`
   - `2026-11-16,13:30,15:00,1.50,MGMT,,Sprint planning`
   - `2026-11-17,15:00,15:30,0.50,MGMT,,Architecture review` (the edited M5)
   - `2026-11-19,10:00,12:00,2.00,5843,,Secure coding training`
   - `2026-11-20,12:30,15:00,2.50,PROJ,1203,Refactor notification service` (the edited entry)
   - `2026-11-20,16:00,16:30,0.50,ADMIN,,Expense reports`
5. The file reflects the saved entries: an entry added by the employee appears with `Manual entry`, a deleted entry does not appear.
6. Escaping: a title `Review, "final"` is exported as `"Review, ""final"""` and a title `=1+1` as `'=1+1`.
7. The CSV generation is a pure domain class with unit tests (no web) covering the format, the decimal hours, the excluded
   absences, the sort order and the escaping. Page tests cover the HTTP behaviour (button, 409, 404, headers, rows).
8. The export is built in memory: it writes no file on disk, and `data/` stays unchanged.

## Out of scope

- Other CSV formats, column selection, configurable separators, time zones.
- Sending the file to 7pace or any real system.
- Rows for commits, pull requests or absences.
