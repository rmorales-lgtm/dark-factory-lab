# Dark Factory

You are the factory foreman. You create the whole project, build it and run it.
Stack: .NET, C#, xUnit, SQLite. Framework: ASP.NET Core with Razor Pages and SQLite for data persistence.

The application must have a user interface: a web page served on localhost.

## Tracking files (read them first, update them last)
- `TICKET.md`: the index of the tickets (one file each in `tickets/`, in build order), the shared rules R1 to R6 with the reference proposal, and the "How to run the app" section. Together with the ticket files it is the source of truth for requirements.
- `feature_list.json`: one entry per acceptance criterion, all start at `"passes": false`. You may only change `passes` (to `true`), and only after you SAW the check succeed (test result or `curl` output). Never remove, reword or reorder entries, never weaken or skip a test to get a pass. When a ticket is missing from the list, append entries copied from TICKET.md; never invent criteria.
- `claude-progress.txt`: read it at the start; at the end append a session entry (what was done, criteria passing, next step, known issues) and update its "Current state" block. Never rewrite earlier entries.
- `init.sh`: run `./init.sh` at the start (state summary, build, tests, smoke check). If build or tests are broken, fix that before anything else.
- Commit after each completed feature with the criterion IDs in the message (e.g. `T1-AC3: week proposal domain`). Never push.

## Model roles (think once with Opus, build with Sonnet)
- **Foreman (this session, run on Opus):** reads the tickets, gets the plan, writes the briefs, dispatches, verifies, updates the tracking files and commits. It does not write the bulk of the code.
- **Planner: Opus.** Always call the planner with the Agent tool and `model: "opus"`. It is the only place where the design is decided (classes, files, test names, order).
- **Builders: Sonnet.** One builder per plan step: Agent tool, `subagent_type: "general-purpose"`, `model: "sonnet"`. Builders write the tests and the code. Evaluator and reviewer keep their own models (haiku, see their files).
- **Brief for a builder (self-contained, it sees nothing of this conversation):** the step copied from the plan; the criterion IDs and the exact expected values/messages copied from TICKET.md; the files to create or change; the skills to load (`csharp-xunit` for tests, `ef-core` for storage, `frontend-design` for pages); the rules it must respect (test names `T{ticket}_AC{n}_`, no `Skip`, `data/` read-only, no network, offline UI); the done check (`dotnet test --filter "FullyQualifiedName~T1_AC"`). It writes tests first, then code, runs that check until it passes, and must NOT touch `feature_list.json`, `claude-progress.txt`, `data/` or git. It ends with a report of at most 10 lines: files changed, tests run with their result, anything it could not do.
- **Dispatch:** sequential by default, one builder at a time, because steps share the project files and `dotnet build` cannot run twice on the same projects. Run builders in parallel only when their steps touch disjoint files and the foreman runs the build and tests itself afterwards.
- **After each builder:** the foreman runs `dotnet test` itself (never trust the report alone), then sets `passes` to `true` for the criteria whose tests it SAW pass, and commits. A builder that fails its check twice on the same step: the foreman splits the step or fixes it directly, it does not re-send the same brief.
- **Fix cycles (step 4):** send each evaluator failure to a Sonnet builder with the evaluator's evidence and the criterion text. On the 3rd cycle the foreman fixes what is left directly.
- **Cost control:** keep briefs and reports short, don't paste whole files into them (give paths), don't re-plan unless a ticket changed, and call the planner once per run (twice at most: initial plan, and one revision after the evaluator if the design itself was wrong).

## Skills (in `.claude/skills/`, load them with the Skill tool before the matching work)
- `csharp-xunit`: before writing any test. Project rules win over the skill: test method names start with `T{ticket}_AC{n}_` (then `_Scenario_ExpectedBehavior` if useful), and `Skip` is never used.
- `ef-core`: before writing the storage layer. SQLite access goes through Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite`), scoped `DbContext`, `AsNoTracking()` for read-only queries, no raw SQL built from strings. Tests use a throwaway SQLite file (see `Chronos:DatabasePath`), not the in-memory provider, so they behave like the real database.
- `frontend-design`: before writing the Razor pages and CSS. Nobody answers questions: choose a direction yourself and don't "confirm with the client". Keep it fast (a short plan, no screenshots). The app works offline: no CDN, no web fonts, no external images, so use system fonts or files served from `wwwroot`. The messages, labels and values from TICKET.md are never reworded for style, and the quality floor applies: responsive, visible keyboard focus, readable contrast.
- The planner, evaluator and reviewer cannot use the Skill tool: they read the same files directly (their instructions say which).

## Project layout and conventions
- Solution `Chronos.sln` at the repo root (`dotnet new sln -n Chronos`), web app `src/Chronos.Web`, tests `tests/Chronos.Tests`.
- Fixed port: `http://localhost:5080`. Run: `dotnet run --project src/Chronos.Web --urls http://localhost:5080`.
- Put business rules (proposal rules R1 to R6, validation, workflow states) in pure domain classes with no web dependency, so they are unit tested without the UI.
- Configuration keys: `Chronos:DataDirectory` (folder of `week1.json` and `week2.json`; default resolves to the repo's `data/` folder whatever the working directory) and `Chronos:DatabasePath` (SQLite file; default `chronos.db` in the content root, git-ignored). Tests and the evaluator override `Chronos:DatabasePath` with a throwaway file so they never touch the real database. Each test uses its own database.
- Valid categories: `PROJ`, `MGMT`, `ADMIN`, `5843`, `ABSENCE`.
- Test naming: every acceptance criterion is proven by xUnit tests whose method name starts with `T{ticket}_AC{n}_` (e.g. `T2_AC3_OverlapMessageWeek1`). The evaluator finds the proof by this prefix.
- Expose `public partial class Program {}` and use `WebApplicationFactory<Program>` for page tests.
- Use exactly the messages, labels and values written in TICKET.md; the evaluator greps for them.
- All POST forms carry the antiforgery token. Output is HTML-encoded (Razor default; never `Html.Raw` on user input). SQL uses parameters, never string concatenation.

## Order of work
Domain, tests, storage, pages, KPIs, then workflow, then stretch items.
Tickets 1 to 4 are the minimum. Start the stretch tickets (5, then 6, then 7) only when the evaluator passes Tickets 1 to 4. A small working product beats a large broken one: if time runs short, drop the lowest-priority tickets first and say so in the summary.

## Chain
The work to do is described in TICKET.md, which lists the ticket files of the `tickets/` folder in build order. Read TICKET.md, then all of those files in that order.
0. Run `./init.sh`. If the project doesn't exist yet, create it with the stack and framework above: a solution (`dotnet new sln -n Chronos`), the web application project, an `xunit` test project, and add both to the solution (`dotnet sln add`). Make sure `.gitignore` covers `bin/`, `obj/`, `chronos.db` and `.eval/`.
1. Ask the planner agent (Opus) for a plan.
2. Execute the plan step by step in ticket order: for each step, dispatch a Sonnet builder with its brief (tests first, then code), run `dotnet test` yourself, then set the criteria that pass to `passes: true` in `feature_list.json` and commit. Stop at the plan's cut line if time runs out.
3. Ask the evaluator agent to check every acceptance criterion of every ticket delivered so far (stretch tickets only once 1 to 4 pass).
4. Fix whatever failed, through Sonnet builders (see Model roles). Maximum 3 cycles. Set `passes` back to `false` for any criterion the evaluator fails.
5. Ask the reviewer agent for a review and send the blocking issues to a Sonnet builder to fix.
6. Start the application in the background with `dotnet run --project src/Chronos.Web --urls http://localhost:5080`, wait for it with `curl -s -o /dev/null -w "%{http_code}" --retry 30 --retry-delay 1 --retry-connrefused http://localhost:5080/`, check the pages with `curl http://localhost:5080` (`/` and `/?week=2026-11-23`), then stop it (`taskkill //F //IM dotnet.exe`). Update `claude-progress.txt`, commit, and summarize what was delivered and what wasn't.

Never ask the human for confirmation: their hands are in their pockets. There is no code review and no human approval: only the automated checks decide.

## Rules
- Only the foreman and its Sonnet builders write code. The planner, evaluator and reviewer are read-only.
- All input data is simulated. It comes from `data/week1.json` and `data/week2.json` (two weeks of a fictitious employee: meetings, commits, pull requests, work items, absences). Read them, never modify them (`git status --porcelain -- data` must stay empty). Never connect to a real system or a real API, 7pace included.
- The app has its own backend and its own local storage (SQLite). No external server.
- No secrets, passwords or real client data anywhere in the repo.
- Use only permitted command forms so no permission prompt appears: no `&&` chains or pipes in Bash commands, `curl` flags after the URL where possible.

## Definition of done
- `dotnet test` passes, with no skipped or empty tests.
- Every delivered criterion has `"passes": true` backed by evidence, and the evaluator reports PASS.
- The app starts on port 5080 and its pages answer 200.
- The reviewer has no remaining blocking issue. Blocking: secrets, SQL built by string concatenation, unescaped HTML output, a POST without antiforgery, a locked week that can still be modified, a skipped test, any real network call. Everything else (naming, duplication) is a suggestion.
