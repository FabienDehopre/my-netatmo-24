# Coding Standards

<!-- The reviewer agent loads this file during code review via @.sandcastle/CODING_STANDARDS.md
     so these standards are enforced during review without costing tokens during implementation. -->

## Style

### C# (.NET 10)

- Warnings are errors: `TreatWarningsAsErrors` and `CodeAnalysisTreatWarningsAsErrors` are enabled with `AnalysisMode=All` in `Directory.Build.props`. Code must build clean — do not suppress diagnostics with pragmas or attributes unless the `.editorconfig` already disables that rule globally.
- Nullable reference types and implicit usings are enabled everywhere; `LangVersion` is `preview`.
- File-scoped namespaces; usings outside the namespace, `System` directives first.
- Allman braces (new line before `{`, `else`, `catch`, `finally`); always use braces; 4-space indentation.
- Naming: private/internal fields `_camelCase`, private static fields `s_camelCase`, constants `PascalCase`. No `this.` qualification.
- Prefer `var` everywhere, switch expressions, pattern matching, primary constructors, expression-bodied members, collection expressions (only when types exactly match), and throw expressions.
- Endpoints are static classes with a `Configure(IEndpointRouteBuilder)` method mapping minimal API routes plus a `HandleAsync` handler returning `Results<...>` typed results. Every route declares `WithName`, `WithSummary`, `WithDescription`, and `ProducesWithDescription` for each status code.
- Use FluentResults (`Result<T>`) for domain/application error flow; errors are defined centrally per module (see `Errors.cs`) and map to HTTP status codes via `EndpointError` from the SharedKernel.
- DTOs are `sealed record` types with XML doc comments on parameters.
- IDs are Vogen strongly-typed value objects (`MyNetatmo24.SharedKernel/StronglyTypedIds`); never pass raw `Guid`/`string` for identities.

### TypeScript / Angular (v22)

- Standalone components only, signals for state, signal forms; no NgModules.
- Use `inject()` instead of constructor injection; `computed()` for derived state; NgRx Signals (`@ngrx/signals`) for stores.
- `import type` for type-only imports; imports grouped (Angular/vendor, then app aliases like `@app/...`).
- Component selectors use the `app-` prefix; separate `.html` template files; spartan/ui (`@spartan-ng/brain` + `@spartan-ng/helm`) with TailwindCSS v4 for UI.
- Runtime validation of external data with Zod (`zod/mini`) via the shared `parse`/`parseCollection` helpers.
- ESLint config extends `@fabdeh/eslint-config` plus Sheriff rules; `pnpm run -r lint` must pass (it auto-fixes with `--fix`).
- 2-space indentation for TS/JSON/YAML (per `.editorconfig`).

## Testing

### .NET — TUnit only (never xUnit/NUnit/MSTest)

- Test methods are `[Test]` attributed, `async Task`, and use TUnit's fluent assertions: `await Assert.That(x).IsEqualTo(y)` / `.IsTrue()` / `.IsFalse()`.
- Test names describe behavior: `MethodOrScenario_ExpectedOutcome` (e.g. `UserNotAuthenticated_HasUnauthorizedStatusCode`).
- Unit tests live in `MyNetatmo24.Modules.*.Tests` mirroring the module's folder structure (`Application/`, `Data/`, `Domain/`, ...); shared fakes go in `TestSupport/` (e.g. `PassThroughHybridCache`, `TestAccountDbContext`).
- Integration tests live in `MyNetatmo24.Modules.*.IntegrationTests` and use Testcontainers for real PostgreSQL/Redis.
- Run with `dotnet test`.

### Frontend — Vitest only (never Jest/Karma/Jasmine)

- Specs are `*.spec.ts` colocated with the code under test; use `describe`/`test` with names that state expected behavior.
- Use Angular Testing Library for component tests; `vi.spyOn`/`vi.fn` for mocks, always restored after use.
- Tests run in a real browser (Playwright + Chromium, headless) via Vitest browser mode.
- Run with `pnpm --filter=frontend-app test --watch=false --reporter=dot`.
- E2E tests are Playwright in `MyNetatmo24.EndToEndTests`: `pnpm --filter=e2e-tests test --reporter=dot`.

## Architecture

### Backend — modular monolith with DDD bounded contexts

- Each module implements `IModule`, owns its DbContext, its own PostgreSQL schema (e.g. `accountmanagement`), and its own caching strategy. Modules never reference each other's internals; cross-module contracts go through `MyNetatmo24.SharedKernel` (messages, value objects, result types).
- Module layout: `Domain/` (entities, value objects), `Application/` (one file per use case, endpoint + handler together), `Data/` (EF Core config), `HttpClients/`, `Migrations/` (generated code — do not hand-edit).
- EF Core conventions: owned types for complex properties (e.g. `FullName`, `NetatmoAuthInfo`), shadow-property foreign keys, global query filters for soft deletes (`DeletedAt`).
- Caching via FusionCache/HybridCache (L1 memory + L2 Redis); handlers take a keyed `HybridCache` service.
- BFF pattern: only the Gateway (YARP) handles Auth0 tokens and cookies; the API validates Bearer JWTs. Never expose tokens to the frontend.
- Secrets: SOPS-encrypted `config/appsettings.encrypted.json` must be updated whenever `MyNetatmo24.AppHost/appsettings.json` changes.

### Frontend — Sheriff-enforced domain slices

- Code lives in `src/app/domains/<domain>/` split into tagged folders: `feature-*`, `ui-*`, `data`, `util-*`, `util-shared`.
- Dependency rules (enforced by `sheriff.config.ts`): feature → data/ui/util; ui → data/util; data → util; util → shared; shared depends on nothing. Domains may only depend on themselves and `domain:shared`. Barrel files are disallowed (`enableBarrelLess`).
- App shell (`src/app/shell/`) and root config wire routing and providers; cross-cutting state lives in `util-state` stores.
