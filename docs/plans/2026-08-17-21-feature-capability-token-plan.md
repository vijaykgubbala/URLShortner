---
issue: 21
title: Protect mutations with a capability token
risk: risk:high
mode: TDD
brainstorm: docs/brainstorms/2026-08-17-21-capability-token-brainstorm.md
intake: docs/intake/2026-08-17-url-shortener-assignment-intake.md
adr: decisions/ADR-002-uniform-404-for-capability-token-failures.md (Proposed)
date: 2026-08-17
---

# Plan — #21 Protect mutations with a capability token

## Context

Issue [#21](https://github.com/vijaykgubbala/URLShortner/issues/21). Serves **REQ-003**
(the service shall expose its operations as APIs), **NFR-008** (no property holding a
credential shall be serializable or exposed on a public surface) and **NFR-009** (each
identified threat shall have an automated test asserting the attack fails).

This is the resolution of the `STD-SEC-01` escalation recorded in **ASM-007**. The source
document states no authentication model; the standard requires one at `critical` severity.
ASM-007's outcome is explicit: *"`STD-SEC-01` is satisfied, not waived."*

**Chosen approach** (from the brainstorm): a SHA-256 hash of the token stored as a nullable
column on `ShortLink`; token generation behind a Domain-declared interface implemented in
Infrastructure; verification as a pure Domain predicate; `DELETE /v1/short-links/{code}` as
the first consumer.

**#25 (Delete a link) closes as delivered here.** `DELETE` was pulled into this issue
because #24 and #25 are both `Blocked by #21`, so without it no mutation endpoint would
exist when this ships and AC-2 and AC-3 could not be proven at the HTTP boundary.

---

## Execution Mode

**`TDD`**, carried from the intake record (`work_type: greenfield`).

Every test below is written and observed to fail before the code that satisfies it exists.
For the structural tests (T-14, T-18) "observed to fail" means failing against the current
code, not merely compiling.

---

## Acceptance Criteria

Copied verbatim from the issue. **AC-3 is amended** — the original text is preserved and
the amendment stated beneath it, so the change is auditable rather than silent.

- [ ] Given a successful create, when the response is returned, then it contains a management token generated from a cryptographically secure source.
- [ ] Given a mutation request with the correct token for that link, when it is processed, then it succeeds.
- [ ] ~~Given a mutation request with a missing or incorrect token, when it is processed, then `403` is returned and no change is made.~~
  - **AMENDED:** *Given a mutation request with a missing or incorrect token, or a request for a code that does not exist, when it is processed, then an identical `404` is returned, with an identical body, and no change is made.*
  - **Rationale:** answering the three cases differently discloses which codes exist, turning the authorization check into an enumeration oracle against the space `data.md` §1.4 spends a CSPRNG to protect. Recorded in [`ADR-002`](../../decisions/ADR-002-uniform-404-for-capability-token-failures.md) with the amendment to `api.md` §2.6 that permits it.
- [ ] Given any response other than the original create, when it is returned, then it does not contain the token.
- [ ] Given the application's logs, when a mutation is processed, then the token does not appear in any log argument.
- [ ] Given a forged token attempt, when the test suite runs, then a negative test asserts it is rejected.

---

## Architecture Constraints

From `/architecture-guide`, quoted with their sources. **Binding — no step below violates
one.**

| Rule | Source | Quote |
|---|---|---|
| Time and randomness | `layers.md` §3.6 | *"Current time and random or generated values must be obtained through an interface declared in Domain or Application and implemented in Infrastructure. Direct calls to a system clock or random source outside Infrastructure are prohibited, because they make domain behaviour untestable."* |
| Business rules and invariants | `layers.md` §3.2 | *"A rule that constrains what is valid about domain state — expiry, uniqueness, code format, permitted destination — is implemented in Domain, on the entity or in a domain service. It must not be implemented only in a handler, controller, or view model."* |
| Use-case handlers | `layers.md` §3.3 | *"A type that coordinates a single user-facing operation — create a short link, resolve a code, expire a link — belongs in Application, one type per use case."* |
| Use-case input and output | `layers.md` §5.2 | *"An Application use case takes and returns its own input and output types, declared in Application. It must not take or return an Entrypoints request or response DTO, because that couples the use case to one transport."* |
| Where the interface lives | `layers.md` §4.1 | *"A repository interface is declared in the layer that consumes it — Domain when a domain service needs it, Application when a use case needs it. It is never declared in Infrastructure."* |
| Method shape | `layers.md` §4.4 | *"Every repository method that performs input or output is asynchronous and accepts a cancellation token as its final parameter."* |
| All access is through repositories | `data.md` §3.1 | *"Database access happens only in repository implementations in Infrastructure. Query construction in Application, Domain, or Entrypoints is prohibited."* |
| Transport types | `layers.md` §3.5 | *"Request and response models, route definitions, filters, and middleware belong in Entrypoints. They must not appear in Domain or Application."* |
| Resource identity | `api.md` §2.4 | *"A single resource is addressed as `/v1/<resource>/{id}`. The identifier in the path is the resource's public identifier — for a short link, its code — never a database surrogate key."* |
| Status codes | `api.md` §2.6 | *"`204` for a successful delete"* — plus the capability-token exception added by ADR-002. |
| Migrations are additive | `data.md` §4.2 | *"Adding a nullable column, a table, or an index is safe. Dropping or renaming an in-use column, narrowing a type, or **adding a non-nullable column without a default is not**."* |
| One error body shape | `api.md` §4.1 | *"Every error response body uses the same shape across all endpoints and versions."* |

### Two violations the advisor flagged, and how this plan complies

**`data.md` §4.2 — the token-hash column MUST be nullable.** Existing rows have no token.
A non-nullable column without a default is named explicitly as unsafe. Step 9 specifies
nullable; any not-null tightening is a later release and out of scope here.

**`api.md` §1.2 — the browser must not call the domain API directly.** A capability token
held by a human makes a direct browser `DELETE` the obvious wiring, which §1.2 forbids.
This plan adds no browser code; the endpoint is server-side only. Recorded so a future BFF
does not wire it wrongly.

### Rules the advisor reported as absent

Relayed rather than filled:

> *"No rule in these documents covers handling, storing, or logging a secret or credential
> value […] and no rule covers constant-time comparison when verifying a presented token
> against a stored hash. […] I am not supplying rules for these; if they are wanted they do
> not exist here today."*

This plan follows external best practice for both (see Decisions Made). Promoting either to
a standards rule is a `/workflow-compound` graduation question, not a step here.

---

## Relevant Learnings

**`enumerate-the-category-not-the-examples`** (#17) — applies at **T-14**. AC-4 says *"any
response other than the original create"*, which is a **category**. Today that is three
response types; #22, #24 and #25 add more. A test enumerating today's responses will agree
with the implementation forever and prove nothing — exactly how "reserved address range"
shipped with multicast permitted. The prescription *"express a range as a range"* means
T-14 is a reflection test over every type the Api assembly can serialize, not one assertion
per endpoint.

**`a-named-gap-is-not-a-work-item`** (#17) — applies at **Step 20**. Three of this issue's
ACs are absence or negative claims, and absence is what gets written into a handover's
"what's not tested" and then ships. Every gap found during execution gets a test, a
deferred review finding, or a follow-up issue before handover closes. A gap with no
destination has no owner.

**Review finding COR-007** (#19) — applies at **Step 9**. The stored hash is `byte[]`, not
a string, and is never compared in a SQL predicate. SQL Server's default collation is
case-insensitive, so a string hash comparison silently halves the alphabet. That is the
same unpinned-collation root cause COR-007 found on the code column, arriving on a second
surface.

**Review finding SEC-006** (#19) — applies at **T-16**. The fix pattern for keeping a
payload out of the log is already established: structured arguments carrying the identifier,
never the secret.

---

## Testing Strategy

Every AC has at least one test. Type is unit (u), integration (i) or structural (s).

| # | Test | Type | Proves | Edge cases |
|---|---|---|---|---|
| **T-01** | `Next()` returns a 43-character unpadded base64url string | u | AC-1 | no `=`, no `+`, no `/` |
| **T-02** | 1,000 draws are all distinct and not ordinally sequential | u | AC-1 | catches `Random.Shared` or a counter substituted for the CSPRNG |
| **T-03** | Hashing the same token twice yields equal bytes; two tokens yield different bytes | u | AC-1, AC-2 | — |
| **T-04** | Verification succeeds for the token that produced the stored hash | u | AC-2 | — |
| **T-05** | Verification fails for a wrong token, a truncated token, an empty string, and a null | u | AC-3, AC-6 | length mismatch must not throw |
| **T-06** | `POST /v1/short-links` returns a token in the response body | i | AC-1 | token is 43 chars |
| **T-07** | `DELETE` with the correct token returns `204` and the row is gone | i | AC-2 | verified by a follow-up `GET` returning 404 |
| **T-08** | `DELETE` with a wrong token returns `404` and the row remains | i | AC-3, AC-6 | row asserted still present |
| **T-09** | `DELETE` with **no** `Authorization` header returns `404` and the row remains | i | AC-3 | guards the `[FromHeader] string?` binding — see Decisions |
| **T-10** | `DELETE` on an unknown code returns `404` | i | AC-3 | — |
| **T-11** | The responses from T-08, T-09 and T-10 are identical in `status`, `type`, `title`, `detail` and headers, and each carries a `traceId` | i | AC-3 | the oracle test; fails if any path diverges. **`traceId` differs per request and is expected to** — see the amendment below |
| **T-12** | Every `404` from the delete route carries `Cache-Control: no-store` | i | ADR-002 | `404` is heuristically cacheable, `403` is not |
| **T-13** | The create response carries `Cache-Control: no-store` | i | ADR-002 | it is the one response containing a plaintext secret |
| **T-14** | **No public type in the `UrlShortener.Api` assembly exposes the token hash, and none has a property of type `ShortLink`** — checked recursively through property types | s | AC-4 | the category test; must fail if a future response type gains either |
| **T-15** | A redirect response contains no token material | i | AC-4 | — |
| **T-16** | Neither the create path nor a failed delete writes the token or its hash into any log message | u | AC-5 | `RecordingLogger`, asserting `DoesNotContain` over `Messages` |
| **T-17** | A forged token — right shape, wrong bytes — is rejected | u+i | AC-6 | `STD-SEC-06` negative test |
| **T-18** | **The verification path is reached even when the code does not exist** | u | AC-3 | a counting `ILinkTokenVerifier` fake asserts it; without this, timing reopens the oracle the `404` closes. Written in the use-case cycle, where there is something to count |

**T-11, T-14 and T-18 are the three that carry this issue.** T-11 proves the responses are
indistinguishable, T-18 proves the *timing* is too, and T-14 proves the absence claim
structurally rather than by enumeration. The other fifteen are ordinary coverage.

### Amendment — T-11 cannot assert byte-identical bodies

Raised by the `/workflow-execute` architecture pre-flight, against `api.md` §4.2:

> *"§4.2 requires `detail` to be 'specific to this occurrence'; three distinct causes
> producing a byte-identical body is in tension with that. Not a violation if `detail`
> remains occurrence-specific in a way that does not distinguish the three causes […] while
> `type` and `title` stay identical — and the response must still carry `traceId` per §4.6."*

T-11's original wording — "byte-identical in status, body and headers" — was a defect in
this plan. `api.md` §4.6 requires a `traceId` on every error body, and a `traceId` is
per-request, so **byte-identical bodies are impossible for any two error responses**. A test
written that way could only pass by dropping `traceId`, which violates §4.6 and destroys the
one mitigation `ADR-002` records for the accepted cost of this design: a legitimate caller
who sees a `404` has a trace identifier, and the real reason exists in the log under it.

**The property that matters is indistinguishability of the cause, not identity of the
bytes.** T-11 asserts the three responses match in every field that could reveal which case
occurred, and that each carries a `traceId` — without asserting the identifiers match.
`detail` names only the requested code, which is occurrence-specific per §4.2 while
revealing nothing about which of the three failures happened.

---

## Implementation Steps

Tests precede the code they verify. Every step names the test it satisfies.

### Domain — token generation and verification

- [x] **1.** Write T-01, T-02 against a not-yet-existing `ILinkTokenGenerator`. Observe compile failure, then red.
- [x] **2.** Declare `ILinkTokenGenerator { string Next(); }` in Domain beside `IShortCodeGenerator`, per `layers.md` §3.6. *(T-01, T-02)*
- [x] **3.** Write T-03, T-04, T-05 against a not-yet-existing `LinkToken` Domain type. Observe red.
- [x] **4.** Add `LinkToken` to Domain: `Hash(string token) -> byte[]` using `SHA256.HashData`, and `Verify(string? presented, byte[]? storedHash) -> bool` using `CryptographicOperations.FixedTimeEquals`. Decode the presented token to bytes **before** hashing. Per `layers.md` §3.2 this is a domain rule and lives here. *(T-03, T-04, T-05)*
- [x] **5.** Declare `ILinkTokenVerifier` in Domain with `LinkTokenVerifier` behind it, and register it. **Amended:** the plan specified a counting *hasher* fake, but `LinkToken` is static, and an early return is behaviourally identical — same outcome, same body — so no outcome-based test catches it. A seam makes the property provable; the counting test itself moves to the use-case cycle, where there is something to count. *(T-18)*
- [x] **6.** Make `Verify` perform the hash and the fixed-time comparison unconditionally, comparing against a fixed dummy when `storedHash` is null. *(T-18)*

### Infrastructure — the generator

- [x] **7.** Implement `CryptoLinkTokenGenerator` in Infrastructure: `RandomNumberGenerator.Fill` into a 32-byte span, `Base64Url.EncodeToString`. Register as a singleton in `Program.cs` beside `IShortCodeGenerator`. *(T-01, T-02)*

### Persistence — the column

- [ ] **8.** Write T-06 and T-07 at the endpoint level. Observe red.
- [ ] **9.** Add `TokenHash` to `ShortLink` as `byte[]?`, mapped **nullable** in `OnModelCreating` per `data.md` §4.2, stored as `byte[]` and never compared in a query predicate (COR-007). Add the constructor parameter as **required** — an optional default silently creates links with no credential. *(T-06, T-07)*
- [ ] **10.** Update all **nine** `new ShortLink(...)` call sites. One is target-typed (`new(code, destination, ...)` in `ShortLinkStoreTests.cs:58`) and a grep for `new ShortLink(` misses it. *(compile)*
- [ ] **11.** Update the literal DDL in `ShortLinkStoreTests.cs:117` to include the new column. Without this the NOT NULL test still passes but for the wrong reason — "no such column" instead of the constraint it claims to assert. *(existing test integrity)*
- [ ] **12.** Add `TryDeleteAsync(string code, CancellationToken)` to `IShortLinkRepository`, returning whether a row was removed. Implement with `ExecuteDeleteAsync` — one round trip, and the database decides, matching `TryAddAsync`. Update `FakeRepository` in the Application tests, which will not compile otherwise. *(T-07)*

### Application — the use case

- [ ] **13.** Write T-08, T-09, T-10, T-16, T-17. Observe red.
- [ ] **14.** Add `DeleteShortLink` to Application with `DeleteOutcome` and `DeleteResult`, following the `ResolveShortLink` shape: primary-constructor DI, optional logger and counter last. It finds the link with `AsNoTracking`, calls `LinkToken.Verify`, and deletes only on success. Returns its own types per `layers.md` §5.2. *(T-07, T-08, T-09, T-10)*
- [ ] **15.** Thread the plaintext token through `CreateResult` so the endpoint can return it once. It is never persisted and never logged. *(T-06, T-16)*

### Entrypoints — the endpoint

- [ ] **16.** Write T-11, T-12, T-13, T-15. Observe red.
- [ ] **17.** Add `ManagementToken` to `CreateShortLinkResponse` — an additive response field, permitted within the version by `api.md` §3.1. Set `Cache-Control: no-store` on the create response. *(T-06, T-13)*
- [ ] **18.** Add `MapDelete($"/v1/short-links/{{code:length({ShortLink.CodeLength})}}", ...)` with `[FromHeader(Name = "Authorization")] string? authorization`. **Nullable is required** — see Decisions. Guard with `ShortLink.IsWellFormedCode` for the alphabet half. Return `204` on success and an identical `404` with `no-store` for every failure. Register `DeleteShortLink` as scoped. *(T-07 … T-12)*

### Structural proof

- [ ] **19.** Write T-14 as a reflection test over the Api assembly, recursing through property types. Observe it fail against a deliberately added `TokenHash` property, then remove that property and observe green. *(T-14)*

### Close-out

- [ ] **20.** Run the full suite. For every gap discovered during execution, create a test, a deferred finding, or a follow-up issue — not a note. *(`a-named-gap-is-not-a-work-item`)*
- [ ] **21.** Verify every AC has a passing test asserting it. Report any without one as a blocker.

---

## Decisions Made

| Decision | Rationale |
|---|---|
| **SHA-256, no salt, no slow KDF** | NIST SP 800-63B and OWASP ASVS 5.0 §6.5.2 make the salted-KDF requirement *conditional on being below 112 bits of entropy*. At 256 bits a standard hash is explicitly sufficient. A slow KDF would also hand an unauthenticated caller a per-request CPU amplifier on a public mutation endpoint. |
| **`CryptographicOperations.FixedTimeEquals`** | The argument that hashing removes the timing channel is sound but is not the recommendation, and no standards body endorses skipping it. It costs nanoseconds on 32 bytes and removes the need for a reviewer to re-derive the reasoning. Compares raw bytes, never strings. |
| **Hash stored as `byte[]`, never compared in SQL** | SQL Server's default collation is case-insensitive and ignores trailing spaces, so a string comparison in a predicate silently weakens the hash. Same root cause as review finding COR-007. |
| **32 bytes, base64url, unpadded (43 chars)** | `.NET 9`'s `System.Buffers.Text.Base64Url` omits padding natively — no manual string surgery. Padding is header-legal per RFC 6750's `b64token` grammar, but mixed padded/unpadded clients cause intermittent auth failures. |
| **`Authorization: Bearer`, not a custom header** | RFC 6750 §1 states it defines *"a general HTTP authorization method that can be used with bearer tokens from any source"* — it is not OAuth-scoped, so the scheme is accurate. RFC 6648 deprecates `X-` prefixes outright. `Authorization` also gets correct redaction and proxy handling that a custom header does not. |
| **`[FromHeader] string?` — nullable, not `string`** | A non-nullable parameter with an explicit binding source is *required*: ASP.NET fails the binding **before the handler runs** and returns `400` in an ASP.NET-shaped body. That would answer "known code, no token" with `400` while "unknown code" answers `404` — reopening the exact oracle ADR-002 closes, and violating `api.md` §4.1. |
| **Uniform `404`, with `no-store` and constant work** | ADR-002. RFC 9110 §15.5.4 explicitly permits hiding a forbidden resource behind a `404`. The two obligations are created by the decision, not incidental to it. |
| **Token never persisted, hash never returned** | Reconciles NFR-008 with AC-1: the persisted property is not the credential, and the credential is never at rest. |

---

## Known Constraints

Accepted limitations, stated as accepted.

**`ADR-002` is `Proposed`, not `Accepted`.** It becomes accepted when a named human other
than the author approves [PR #53](https://github.com/vijaykgubbala/URLShortner/pull/53), per
the edit gate. **If a reviewer rejects the scoping, the status-code design in this plan
changes** — T-08 through T-12 and Step 18 would revert to `401`/`403`/`404`. Nothing else
in the plan depends on it.

**A lost token is unrecoverable.** A consequence of storing only the hash, not an
oversight. If the create response is lost to a network failure after commit, the link is
created and permanently unmanageable. An idempotency key returning the same token on retry
is the standard remedy and must be designed in at creation — it cannot be retrofitted. Out
of scope here; recorded so it is a decision rather than a discovery.

**No token rotation or revocation.** Out of scope per the issue. If a token leaks, the only
remedy is deleting the link.

**No token prefix.** A short type prefix (as GitHub's `ghp_` and Stripe's `sk_live_` use)
would make leaked tokens greppable by secret scanners and allow rejecting malformed input
before hashing. Considered and not adopted, because it was not among the decisions taken
and it changes the token format contract. Recorded here rather than dropped.

**No rate limiting on the delete endpoint.** #30 covers creation only. 256 bits makes
guessing infeasible, but the endpoint remains an unauthenticated surface where each wrong
token costs a hash and a database read.

**The schema change has no migration — an accepted deviation, decided before the code
exists.** `data.md` §4.1 requires every schema change to ship as a versioned migration, and
§4.4 requires a reverse step. This change ships neither. `EnsureCreated` does nothing against
an existing `shortlinks.db`, so the `TokenHash` column will not appear on a database created
before it.

Raised by the `/workflow-execute` architecture pre-flight and put to the user, who chose to
proceed and record rather than introduce migrations inside a security story or write a
waiver. The deviation is recorded here, in the handover, and on #49, which owns the
migration mechanism — deliberately in more than one place, because a deviation recorded only
in a plan is one nobody reads again. This is a knowing violation of `data.md` §4.1, not a
gap: `/workflow-review` and `/gate-check` should see it as decided, not as missed.

**`requirements/nfr.md:49` is stale** — it still reads *"Authorization is unresolved and
escalated"*, which ASM-007 resolved. Correcting it is a one-line documentation fix, not part
of this plan.
