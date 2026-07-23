# Do NOT flag these — already decided

Recurring false positives from earlier review passes. These are closed decisions, not oversights.
Do not re-raise them unless the code changes in a way that contradicts the stated reasoning below.

## `IOptions<AppSettings>` in `AuthModule.HandleGoogleCallback`
`HandleGoogleCallback` reads `AppBaseUrl` via `IOptions<AppSettings>` while neighboring methods in
the same module read config via `IConfiguration` directly. This looks like an inconsistent pattern
within one file — it is not. It's a deliberate choice, documented inline in the handler (see commit
`23516fc`, "docs(auth): explain why HandleGoogleCallback uses IOptions<AppSettings>").
Do not suggest unifying it to `IConfiguration` for consistency's sake.

## `AppBaseUrl` vs `FrontendUrl` naming
`AppBaseUrl` (in `AppSettings`) is the single source of truth for the frontend base URL used in
OAuth redirects. `FrontendUrl` was a duplicate that has already been removed (see commit `c3829a9`,
"fix(settings): consolidate AppBaseUrl and FrontendUrl into one source"). If you see `AppBaseUrl`
being read directly instead of through a service indirection, that's the intended, simplified state
— not a missing abstraction.

## MediatR version pinned to 11.1.0
Not an outdated dependency. See `conventions.md` — deliberate, to stay on the last Apache-2.0 license
before MediatR went commercial in v13. Don't suggest upgrading it.
