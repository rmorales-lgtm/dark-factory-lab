# Mission Chronos: team plan (harness phase, 60 min)

Goal: build a factory that delivers the app on its own in 15 minutes after the bell. We don't write the app. We write what guides and controls the agent: `CLAUDE.md`, the agents, the hooks, the permissions and the tickets.

Launch message at the bell: `Build TICKET.md end to end following CLAUDE.md.`

## Starting point

- Already in place: a Stop hook that runs `dotnet test` (3 retries max), permissions for `dotnet`, `curl` and `git`, and three agents (planner, evaluator, reviewer).
- Done: the CLAUDE.md framework is set to ASP.NET Core with Razor Pages. `check-tests.sh` now exits 0 when no `.sln` exists, so the hook no longer blocks before the app is created.
- Missing: `TICKET.md` is an empty template, CLAUDE.md has no port, project layout or order of work, and the three agents still have `TODO` comments.

## Who does what (7 people)

| # | Who | Role | Owns | Files |
|---|---|---|---|---|
| 1 | **Alex Rodrigues** | Supervisor | Kickoff decisions, client questions, scope and priorities, ticket sign-off, go/no-go before the bell | No files of their own. Reviews `TICKET.md` |
| 2 | **Richard Morales** | Senior, Harness Master | `CLAUDE.md`, clean Claude config check (`/memory`, `/hooks`, `/agents`, `/plugin`), the dry run, the final commit | `CLAUDE.md` |
| 3 | **Maurice Collenne** | QA | Testability of every acceptance criterion, the data-trap criteria, the evaluator agent (PASS/FAIL table and evidence rules) | `.claude/agents/evaluator.md` |
| 4 | **Esteban Florido** | Developer | Tickets 1 and 2 (import, timeline, KPIs, validation rules) and the "How to run the app" section | `TICKET.md`, sections 1 and 2 and the run section |
| 5 | **Juan David Sansmartin** | Developer | Tickets 3 and 4 (approval workflow, suggestions, CSV export) and the planner agent | `TICKET.md`, sections 3 and 4, `.claude/agents/planner.md` |
| 6 | **Oscar Ortiz** | Developer | Hooks and permissions, so there is no prompt at launch | `.claude/hooks/`, `settings.json` |
| 7 | **Andrés Felipe Parra Rojas** | Developer | Reviewer agent (blocking and non-blocking rules), then helps Oscar test the permissions | `.claude/agents/reviewer.md` |

There are no analysts on this team, so the developers write the tickets. Alex gets the answers from the clients, and Maurice makes sure every criterion can be proven.

Rules:
- One file per owner, so nothing conflicts at merge time. Esteban and Juan David edit different sections of `TICKET.md`, so they should not edit it at the same time. Esteban writes first, then Juan David adds his sections.
- Oscar owns `settings.json`. Richard reviews it but doesn't edit it at the same time.
- Nobody writes application code by hand, not even during the harness phase.

## Timeline

| Min | Who | What |
|---|---|---|
| 0–10 | All | Kickoff led by Alex: fix the stack, port and layout. Alex sends the first questions to the clients. |
| 10–35 | Each owner | Work on your own files only. Alex shares client answers in the chat as they arrive. |
| 35–45 | All | Cross-review (below). |
| 45–55 | Richard, with all watching | Run the launch message in a clean session, time it, and count permission prompts. The target is zero. |
| 55–60 | All | Fix only what the dry run broke. Alex makes the go/no-go call. Richard resets `.claude/.attempts` to 0, cleans the generated code and commits. |

Cross-review at minute 35–45:
- Maurice checks every criterion in `TICKET.md`: can it be proven by a test or a `curl`? He tells Esteban and Juan David what to fix.
- Richard reviews Oscar's hooks and permissions, and Oscar reviews Richard's CLAUDE.md.
- Andrés checks that the reviewer's rules match the CLAUDE.md conventions.
- Esteban and Juan David check that the evaluator and planner match the tickets they wrote.
- Alex reads `TICKET.md` against the PDF's minimum expectations.

## Decisions to fix at kickoff

1. **Stack:** ASP.NET Core Razor Pages with SQLite. It is simpler than Blazor for curl-based checks.
2. **Fixed port:** for example `http://localhost:5080`, so the evaluator and the curl checks can rely on it.
3. **Tickets:** one `TICKET.md` with one section per ticket.
4. **Priority:** the minimum expectations come first. The stretch items only come after the evaluator passes the minimum.

## Tickets, in build order

1. **Import and timeline.**
   - Read `week1.json` and `week2.json` without modifying them.
   - Show the week as entries, each with a category (PROJ, MGMT, ADMIN or 5843 Training).
   - Compute the KPIs: hours per category, hours entered against expected, and the number of rule violations.
2. **Validation rules.** Each rule gets an explicit, testable message.
   - 40 hours per week, minus absences (sick, vacation or other).
   - The 20-minute daily break is not entered.
   - Training goes under code 5843.
   - No overlaps.
   - Every entry has a category.
3. **Approval workflow.**
   - A role switch in the UI (Employee or Manager), with no authentication.
   - The employee submits the week. The manager approves, or rejects with a comment.
   - An approved week is locked, so edits are refused and the buttons are disabled.
   - A week with violations cannot be submitted.
4. **Smart suggestions and CSV export (stretch).**
   - Meeting becomes MGMT, PR becomes PROJ, "Expense reports" becomes ADMIN.
   - A correction on week 1 applies to week 2, stored in SQLite.
   - A 7pace-style CSV, available only when the week is approved.

Every acceptance criterion must be provable by a test or a `curl`, with a concrete input and an expected output.

## Traps in the data to turn into criteria

| Data | Expected behavior |
|---|---|
| W1 `M4` (14:00–15:00) and `M5` (14:30–15:30) overlap. W2 `M17` and `M18` overlap too. | The overlap is detected and a clear message is shown. |
| W1 has 4 h of sick leave on 18/11. | The week's target is 36 h, not 40 h. |
| W2 has a vacation day on 27/11. | The week's target is 32 h. |
| W2 `M21` is cancelled. | It is excluded from the entries. |
| W2 has a commit dated 28/11, a weekend day. | It is ignored or flagged. |
| W2 has a commit with `workItemId: null`. | It is handled without crashing. |
| PR 5503 appears in both weeks. | It is not counted twice. |
| `M8` and `M15` carry `trainingCode` 5843. | They go under 5843. A training entry under any other code is rejected. |
| The data has 4 categories, but the PDF mentions 5 at Progi. | Ask the clients. |

## Questions for the clients (Alex)

- Does a commit or PR become a time entry? What duration does it get?
- Is the 20-minute break deducted automatically, or does the employee enter and then remove it?
- What columns does the 7pace CSV need?
- Do the 5 Progi categories (general administration, team management, product management, client management, project) map onto PROJ, MGMT and ADMIN?
- Is the 40 hours net or gross of breaks?

## What goes in each harness file

**CLAUDE.md** (Richard)
- Keep the framework line (ASP.NET Core Razor Pages) and add SQLite and the fixed port.
- Add a project layout: `src/Chronos.Web`, `tests/Chronos.Tests` and the solution at the root.
- Put the validation rules in a pure domain class, so they can be unit tested without the UI.
- Add an order of work: domain, tests, storage, pages, KPIs, workflow, then the stretch items.
- State that a small working product beats a large broken one.
- Change step 6 of the chain to start the app in the background with `dotnet run --urls http://localhost:5080`, wait for it to respond, then kill it.

**Evaluator** (Maurice)
- A PASS or FAIL table with one row per criterion.
- Evidence must be a passing test name, or a `curl` with the status and an HTML snippet. "Looks fine" is not evidence.
- It greps the HTML for the expected messages, labels and KPIs.
- It checks that `data/*.json` is unmodified using `git diff --stat data/`.
- It runs on Haiku, so keep its instructions mechanical.

**Planner** (Juan David)
- An ordered list of steps, each tied to a ticket and criterion number, with the test to write first and the trap from the table above.

**Hooks and permissions** (Oscar)
- Check that the allow list covers starting the app in the background, `curl` and cleanup (`taskkill`), so there is no permission prompt at launch.
- Reset `.claude/.attempts` to 0 before the dry run.

**Reviewer** (Andrés)
- Blocking: secrets, SQL built by string concatenation, unescaped HTML output, no antiforgery token on POST, a locked week that can still be modified, a skipped test, and any real network call.
- Suggestions only: naming and duplication.

**Tickets** (Esteban and Juan David, checked by Maurice)
- One section per ticket with a user story, numbered testable criteria and an out-of-scope list.
- "How to run the app" section: command, port, URL and where the data lives.

## Hand-offs

- Alex to Esteban and Juan David: client answers, as soon as they arrive.
- Esteban and Juan David to Maurice: criteria must be provable by a test or a `curl`. Maurice says when one isn't.
- Richard to Maurice: the port and project paths in CLAUDE.md must match what the evaluator checks.
- Oscar and Andrés: the reviewer's security checks should match the permissions. For example, the reviewer flags any real network call.
- Everyone to Richard: report as soon as your piece is ready, so the dry run can start early.

## Risks

- Too much scope for 15 minutes. Tickets 1 to 3 must work before anything else. If time is short, drop ticket 4 first.
- A weak evaluator. If its checks aren't mechanical, it will pass broken work.
- A permission prompt at launch. It costs a penalty, so test for it during the dry run.
- The dry run takes more than 15 minutes. Cut scope in the tickets, not in the harness.
- No analysts on the team. If the tickets are late, Alex steps in to write the "How to run the app" section and the out-of-scope lists.
