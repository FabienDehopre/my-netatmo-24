# Registration creates the Auth0 identity only — the Account is still provisioned on first login

Registration (`POST /account/register`, anonymous) creates the user in Auth0 via the Management API — email, password, and profile attributes (`nickname`, `given_name`, `family_name`, `picture`) — and creates **no local Account row**. The Account keeps its single creation path: the existing `POST /account/ensure` JIT provisioning on the first authenticated call, which reads exactly those attributes back from Auth0 `/userinfo`.

## Considered Options

- **Registration also inserts the Account row eagerly.** Rejected: two writers of `Account.Create` invite drift between the paths, and nothing needs an Account to exist before first login in a personal dashboard.
- **No custom endpoint — Auth0 Universal Login signup + JIT provisioning.** Rejected: custom signup UX must capture nickname, name, and avatar at registration time.

## Consequences

- A registered-but-never-logged-in user exists only in Auth0; any future "registered accounts" listing cannot rely on the local database.
- The registration endpoint depends on an Auth0 M2M application (`create:users` scope); the profile attributes it sets are the contract with `EnsureAccount`'s `/userinfo` read.
- Email verification is triggered at registration (`email_verified: false`, `verify_email: true`) but not enforced at login by this codebase; enforcement, if wanted, lives tenant-side (Auth0 Action).
