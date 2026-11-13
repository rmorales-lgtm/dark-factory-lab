# Ticket 3: Roles and approval workflow

Depends on: Ticket 1 (week page) and Ticket 2 (editing and violations).

## User story

As an employee, I want to submit my week, and as a manager I want to approve it or reject it with a comment,
so that only a validated week is locked and ready for 7pace.

## Workflow rules

- Statuses: `Draft` (initial), `Submitted`, `Rejected`, `Approved`. The status is stored in SQLite, per week.
- Roles: `Employee` (default) and `Manager`. The role is switched in the UI and kept in a cookie. There is no authentication.
- Transitions: Employee submits `Draft` or `Rejected` → `Submitted` (only when the week has 0 violations).
  Manager approves or rejects `Submitted` → `Approved` or `Rejected`. `Approved` is final.
- Entries (Ticket 2: edit, delete, add, `Reset to proposal`) can only change while the status is `Draft` or `Rejected`.
- A history list on the week page keeps every transition, in order.

## Acceptance criteria

1. Every page shows `Role: Employee` by default. A form (`POST /Role`, with the antiforgery token) has the buttons `Employee`
   and `Manager`. After choosing `Manager`, every page shows `Role: Manager`, also after a reload. Any other role value is
   refused and the role does not change.
2. The week page shows `Status: Draft` for both weeks at the start. The status of one week does not affect the other week
   and is still there after an app restart.
3. An Employee sees the `Submit week` button when the status is `Draft` or `Rejected`, and never sees `Approve` or `Reject`.
   A Manager never sees `Submit week`.
4. Submitting a week with violations is refused, the status stays `Draft`, and the page shows
   `Cannot submit: <n> violation(s) to fix`. Week 1 proposal: `Cannot submit: 2 violation(s) to fix`.
   Week 2 proposal: `Cannot submit: 3 violation(s) to fix`.
5. Valid-week scenario (the Ticket 2 criterion 5 edits on week 1): the Employee submits, the page shows `Status: Submitted`
   and the history shows `Submitted by Employee`.
6. A Manager sees `Approve`, `Reject` and a comment field on a `Submitted` week. `Approve` gives `Status: Approved` and the
   history shows `Approved by Manager`.
7. `Reject` without a comment is refused with `A comment is required to reject` and the status stays `Submitted`.
   `Reject` with the comment `Fix the stand-up category` gives `Status: Rejected`, shows `Manager comment: Fix the stand-up category`
   and the history shows `Rejected by Manager: Fix the stand-up category`. The Employee can then edit the week and submit it
   again (only if it has 0 violations).
8. An `Approved` week is locked: the page shows no edit, delete, add or `Reset to proposal` control, and the corresponding
   POST requests are refused with HTTP 409 and the text `Week is locked`. A `Submitted` week refuses them the same way with
   `Week is awaiting approval`. Nothing is changed in storage in both cases.
9. The server enforces the roles. A Manager POSTing a submit is refused with HTTP 403 and `Action not allowed for role Manager`.
   An Employee POSTing an approve or a reject is refused with HTTP 403 and `Action not allowed for role Employee`.
   An invalid transition (approving a `Draft` week) is refused with HTTP 409 and `Invalid transition: Draft to Approved`.
   The status never changes on a refusal.
10. The state machine (every valid and invalid transition) is a pure domain class with unit tests (no web).
    One page test runs the whole scenario on week 1: edits, submit, reject with a comment, submit again, approve, locked.

## Out of scope

- Authentication, several employees or managers, notifications, timestamps in the history.
- Unlocking an approved week.
