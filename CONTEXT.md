# MyNetatmo24

MyNetatmo24 is a personal dashboard application for monitoring data from Netatmo weather stations, currently built out through its account and identity layer.

## Language

**Account**:
The persisted aggregate representing an application user — their profile (name, avatar, nickname), Auth0 subject correlation, and their Netatmo Connection, if authorized. Owned by the AccountManagement module, keyed by AccountId.
_Avoid_: User, Profile, Auth0 user

**User**:
The transient, per-request view of the authenticated browser session, built directly from OIDC claims at the Gateway (BFF) layer and mirrored on the frontend. Not persisted — distinct from Account.
_Avoid_: Session, Account

**Netatmo Connection**:
The authorization an Account holds to call the Netatmo API on the user's behalf — an access token, refresh token, and expiry. Obtained through a not-yet-built OAuth handshake.
_Avoid_: NetatmoAuthInfo, Netatmo token, Netatmo auth

**Registration**:
The anonymous flow that creates a new Auth0 identity — email, password, and profile (nickname, given/family name, optional avatar URL) — via the Auth0 Management API, and triggers Auth0 email verification. Creates no Account: the Account is still provisioned on the first authenticated call.
_Avoid_: Signup, Sign-up, Account creation

**Module**:
An architectural bounded context in the modular monolith (e.g. AccountManagement) — its own schema, DbContext, and caching strategy behind an IModule contract.
_Avoid_: using "Module" for a physical Netatmo hardware sensor add-on — pick a different term (e.g. "Sensor") if/when that concept gets built.
