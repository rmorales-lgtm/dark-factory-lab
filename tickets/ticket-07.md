# Ticket 7 (stretch): Upload a week and see it as a calendar

Depends on: Tickets 1 to 3. Start only when Tickets 1 to 4 pass. It is not in the brief's minimum or "going further" list:
it is a usability extra, to build last.

## User story

As an employee, I want to upload a JSON file in the format of `data/week1.json` and see my week as a calendar,
so that I can check at a glance that the data is what I expect, and work on it like the provided weeks.

## Expected JSON format

The format of `data/week1.json` and `data/week2.json`. Required top-level sections: `employee`, `week`
(`start`, `end`, `expectedHours`, `dailyBreakMinutes`), `meetings`, `commits`, `absences`.
Optional sections (treated as empty when absent): `categories`, `workItems`, `pullRequests`.
The uploaded file is only read: `data/*.json` is never written.

## Rules

- One uploaded week is kept at a time, in SQLite. A new upload replaces the previous upload.
- The uploaded week joins the week selector of Ticket 1 and goes through the same pipeline (proposal, KPIs, editing,
  violations, approval).
- If the uploaded week starts on the same date as an existing week, the uploaded data replaces that week's source data and
  that week's saved changes, status and history are deleted, unless the week is `Submitted` or `Approved` (criterion 4).

## Acceptance criteria

1. `GET /Upload` returns 200 and shows a form with a file input (`.json`), an `Upload` button, and the antiforgery token.
   The home page `GET /` has a link to `/Upload`.
2. Uploading `data/week1.json` (POST `/Upload`, multipart) redirects to `GET /Calendar?week=2026-11-16` and answers 200.
   The same works with `data/week2.json`. The uploaded week is still there after an app restart, and it is listed in the
   week selector of `GET /`.
3. Uploading a file for a start date that is not week 1 or week 2 (for example a copy of `week1.json` with `week.start`
   `2026-12-07`) lists that week in the selector, and `GET /?week=2026-12-07` shows its proposal, KPIs and violations.
4. Uploading a week that is `Submitted` or `Approved` is refused with `Week is locked: cannot replace its data`,
   and nothing changes. Uploading a `Draft` week with saved changes replaces its source data and deletes those changes.
5. `GET /Calendar?week=<start>` shows the employee name (`Alex Gagnon`), the team, and the week dates. It has one column
   per day of the week, in date order, each headed by its date. Without a `week` parameter it shows the first week of the
   selector. `GET /Calendar?week=2020-01-06` shows `Week not found` (200, not a 500).
6. Each meeting is shown in its day column with its id, title, start and end (e.g. `M2 Sprint planning 13:30–15:00`), in
   time order. Week 1 shows 11 meetings. A meeting with `"cancelled": true` shows the label `Cancelled` (week 2: `M21`).
7. Each absence is shown in its day column with its type and hours (week 1: `Sick 4h` on `2026-11-18`; week 2: `Vacation`
   on `2026-11-27`). Each commit is shown in its day column with its time, sha and message (week 1: 7 commits).
8. Overlapping meetings are visibly flagged with the label `Overlap`: `M4` and `M5` on `2026-11-17` (week 1), `M17` and
   `M18` on `2026-11-25` (week 2). Meetings that only touch (end time equals next start time) are not flagged.
9. Invalid uploads are refused with a clear message on the Upload page, with HTTP 200 (never a 500 or a stack trace),
   and the previously stored data is kept unchanged:
   - no file selected: `Please choose a JSON file`;
   - a file that is not valid JSON (e.g. `not json`): `Invalid JSON file`;
   - a valid JSON without a required section (e.g. `{}`): `Missing section: employee` (first missing section, in the order listed above);
   - `week.start` or `week.end` that is not a date `yyyy-MM-dd`: `Invalid week dates`;
   - a file larger than 1 MB: `File too large (max 1 MB)`.
10. Text from the uploaded file (names, titles, messages) is HTML-encoded: a meeting titled `<script>alert(1)</script>` is shown
    literally, and the page source contains no raw `<script>alert(1)</script>`.

## Out of scope

- Dragging or resizing on the calendar, a month view, more than one uploaded week at a time.
- Any connection to 7pace, Azure DevOps, Teams or another real system.
