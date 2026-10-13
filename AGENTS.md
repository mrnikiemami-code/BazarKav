# BazarKav repository instructions

Read docs/architecture/ARCHITECTURE.md, docs/architecture/GUARDS.md and docs/planning/CURRENT-STATE.md before changing this repository.

- User instructions take precedence. Work only on the requested scope.
- Preserve the agreed folder layout: separate Application/Commands, Application/Queries and Application/Validation. Never colocate validators with commands or queries.
- Clean Architecture inside each module; cross-module references only through Contracts.
- HTTP input DTOs belong only in module Presentation/Requests, one public DTO per matching file. The *Request suffix is reserved for these HTTP DTOs. Never put HTTP wrappers in Contracts or Application; Application uses separate *Command and *Query types. Contracts is for shared module contracts, not an HTTP DTO collection. State the consumer/purpose when adding a shared contract.
- Domain and Contracts have no package/project references. Domain must not know MediatR, EF, HTTP, logging or source adapters.
- Host is composition only. Module work must not introduce business logic, handlers, endpoints or feature folders into Host.
- BazarKav is analytics-only. Accounting adapters may read sources; they must never create/update/delete accounting records.
- Commands may update internal configuration/mappings or schedule internal analysis. Queries are read-only.
- Enforce trusted business scope. Never infer business authorization from a client-provided correlation ID or arbitrary header.
- Preserve source identity and provenance. Do not double count repeated imports or overlapping sources. Missing data is not zero.
- Preserve distinct source codes and internal identities for products, customers and warehouses. No identity merge based only on names/codes/phone numbers.
- Support both historical-pattern analysis and manager rules. Their precedence is unresolved; do not silently invent it.
- Structured logging, correlation/trace and centralized safe ProblemDetails are mandatory when runtime is introduced. No secrets or raw accounting payloads in logs/responses.
- Propagate cancellation. Do not swallow failures or return success with empty data.
- Keep folder depth shallow. Do not create placeholder projects/classes/folders for unapproved modules.
- Pin packages after documenting selected versions/licenses. Do not make unsolicited package/runtime upgrades.
- Run python3 tools/architecture/check_architecture.py and relevant build/tests when code exists; do not claim runtime checks for documentation-only work.
- Update CURRENT-STATE.md with actual outcome, checks and unresolved decisions.
- Do not rewrite unrelated changes or force-push. Verify remote head before publishing.
- Stop at USER_REVIEW after the requested work. automaticNextImplementationTask=NONE.
