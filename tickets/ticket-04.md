# Ticket 4: Quality and security baseline

Depends on: Tickets 1 to 3 (it checks the pages and forms they created).

## User story

As Progi, I want the application to meet our basic quality and security standards,
so that it can be trusted with time entries.

## Acceptance criteria

1. Every form with `method="post"` on every page carries the antiforgery token, and a POST without a valid token is refused
   with HTTP 400. A test lists the pages (`/`, `/?week=2026-11-23` and every other page with a form) and checks each form.
2. Text typed by a user is HTML-encoded. A manager comment `<script>alert(1)</script>` is shown literally, and the page
   source contains no raw `<script>alert(1)</script>`.
3. Errors never show a stack trace. An unknown URL (`GET /nothing-here`) answers 404 with the text `Page not found`.
   A malformed parameter (`GET /?week=abc`, `GET /?week=` and a 5,000-character week) answers 200 with `Week not found`,
   never a 500. An unhandled exception answers 500 with the text `Something went wrong` and no exception detail.
4. Every response carries these headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` and
   `Content-Security-Policy: default-src 'self'; style-src 'self' 'unsafe-inline'`. The pages still work with them:
   no inline script, no CDN, no external font or image.
5. The application makes no outbound call and holds no secret. In `src/`, a text search finds no `HttpClient`, `WebClient`,
   `WebRequest`, `Socket`, password, token or API key. Views, CSS and scripts contain no `http://` or `https://` URL
   except the SVG namespace `http://www.w3.org/2000/svg`.
6. All SQL goes through Entity Framework Core with parameters. In `src/`, a text search finds no `FromSqlRaw`,
   `ExecuteSqlRaw`, `ExecuteSqlInterpolated` or SQL text built with string concatenation or interpolation.
7. The input data is never modified. After a full `dotnet test`, `git status --porcelain -- data` is empty.
8. Basic page quality: every page has `<html lang="en">`, a non-empty `<title>`, a `<meta name="viewport">` tag,
   and every form field has a label.
9. The tests are honest. `dotnet test` passes, and a text search finds no `Skip`, `[Ignore]` or empty test method in `tests/`.

## Out of scope

- Authentication and authorization (roles are switched in the UI, see Ticket 3).
- HTTPS, rate limiting, logging infrastructure, penetration testing.
