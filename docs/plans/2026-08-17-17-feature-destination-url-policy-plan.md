# Implementation plan — #17 Destination URL validation policy

**Issue:** [#17](https://github.com/vijaykgubbala/URLShortner/issues/17) · `risk:high` · type `feature`
**Traces to:** REQ-001, REQ-002; NFR-006, NFR-009
**Brainstorm:** [`docs/brainstorms/2026-08-17-17-destination-url-policy-brainstorm.md`](../brainstorms/2026-08-17-17-destination-url-policy-brainstorm.md)
**Date:** 2026-08-17

---

## Context

Delivers the policy that decides whether a destination URL may be stored and redirected to. #18 calls it at creation, #19 at resolution, #24 on update — it blocks all three.

`STD-SEC-05` sets this at `critical` severity: an unvalidated destination makes the service a credible-looking hop for someone else's phishing.

**Chosen approach**, from the brainstorm after its stress test reversed the first recommendation: a **pure synchronous Domain predicate**, with DNS resolution moved out to an Application orchestration calling an Infrastructure port. The decisive factor is #19's redirect path, which needs a synchronous scheme-only answer — forcing that through an async service with a stubbed resolver is what makes someone write the check inline in a handler instead, which is the `layers.md` §3.2 violation the rule exists to prevent.

**Risk note.** The intake record classifies the assignment `risk:medium`; this issue was raised to `risk:high` at `/github-issues` because it is the open-redirect control. `/gate-check` reads the issue label, so this needs a named reviewer **and** written sign-off.

---

## Execution Mode

**`TDD`** — carried from the intake record (`greenfield`). New behaviour, so red-green-refactor applies directly: every test below is written and observed to fail before the code that satisfies it exists.

---

## Acceptance Criteria

Copied verbatim from #17.

- [ ] Given a destination URL with scheme `http` or `https`, when it is validated, then it is accepted.
- [ ] Given a destination URL with any other scheme, when it is validated, then it is rejected with `422` and the response names the failing rule.
- [ ] Given a destination URL resolving to a loopback, link-local, private or reserved address range, when it is validated, then it is rejected with `422`.
- [ ] Given a value that cannot be parsed as an absolute URL, when it is validated, then it is rejected with `400` naming the offending field.
- [ ] Given a rejected destination, when the rejection occurs, then a structured log event `link.destination.rejected` is emitted and the `link_destination_rejections` counter increments.
- [ ] Given the validator, when the test suite runs, then a negative test asserts each rejection case fails closed, including a DNS resolution failure.

---

## Architecture Constraints

Returned by `/architecture-guide` and quoted with their sources. **Every step below complies; none of these was rewritten around.**

| Source | Rule |
|---|---|
| `layers.md` §2.1 | *"A project may reference only projects closer to the domain than itself. Domain references no other project in this system. Application references Domain. Infrastructure references Application and Domain. Entrypoints reference Application and Domain."* |
| `layers.md` §2.3 | *"Exactly one file — the application's startup or program entry — may reference every layer, for the sole purpose of registering implementations against interfaces. No logic other than registration belongs in it."* |
| `layers.md` §3.3 | *"A type that coordinates a single user-facing operation — create a short link, resolve a code, expire a link — belongs in Application, one type per use case."* |
| `layers.md` §3.5 | *"Request and response models, route definitions, filters, and middleware belong in Entrypoints. They must not appear in Domain or Application."* |
| `layers.md` §5.3 | *"Mapping between transport DTOs and use-case types happens in Entrypoints. Mapping between use-case types and domain types happens in Application. Domain types are never mapped in Infrastructure except to storage representations."* |
| `layers.md` §1.3 | *"The Application layer holds use-case handlers that orchestrate domain objects, plus the interfaces describing what the use case needs from the outside world. It contains sequencing and coordination, not business rules."* |
| `layers.md` §1.4 | *"The Infrastructure layer holds implementations of interfaces declared by Domain or Application: persistence, clock, identifier generation, outbound HTTP, messaging. It contains no business rule and no use-case sequencing."* |
| `layers.md` §2.2 | *"No project may hold a compile-time reference to Infrastructure except the composition root. Infrastructure implementations are bound to their interfaces at startup by dependency injection."* |
| `layers.md` §3.2 | *"A rule that constrains what is valid about domain state — expiry, uniqueness, code format, permitted destination — is implemented in Domain, on the entity or in a domain service. It must not be implemented only in a handler, controller, or view model."* |
| `layers.md` §5.2 | *"An Application use case takes and returns its own input and output types, declared in Application. It must not take or return an Entrypoints request or response DTO, because that couples the use case to one transport."* |
| `layers.md` §6.3 | *"A layer must not let an exception type from a lower-level concern escape unchanged. Infrastructure translates storage exceptions into the failure types its interface declares; Entrypoints translate domain and application failures into the error contract in `api.md`."* |
| `api.md` §4.2 | *"`type` is a stable, kebab-case, machine-readable code that must not change once published. `title` is a short human-readable summary safe to show a user. `status` matches the HTTP status code. `detail` is specific to this occurrence. `traceId` correlates the response with logs."* |
| `api.md` §4.3 | *"An error body must not contain a stack trace, an exception type name, a SQL fragment, a file path, a connection string, or an internal host name. `detail` describes the failure in domain terms only."* |
| `api.md` §4.4 | *"A request failing input validation returns `400` with one entry per invalid field, each naming the field and the reason. Validation failures are reported together, not one at a time."* |
| `api.md` §4.5 | *"A well-formed request that violates a domain rule returns `422`, not `400`. The distinction is what tells a caller whether to fix the request or to stop retrying it."* |
| `api.md` §4.6 | *"Every error response includes `traceId`, and that identifier appears in the corresponding log entry. An error response with no trace identifier is not diagnosable in production."* |

### The five flags, and how each is avoided

| Flag | Condition that avoids it |
|---|---|
| §1.3 / §3.2 — orchestration deciding | The port returns a **resolution outcome**; the Application layer passes it through untouched and decides nothing. The Domain owns what `NotFound` and `Failed` mean. Steps 11–12, tests T-15, T-16. |
| §1.4 — adapter filtering | The adapter returns every resolved address **unfiltered and unranked**. Step 14, test T-17. |
| §4.3 — address in the error body | The resolved address reaches the **log only**, correlated by `traceId`, never the response. Steps 17–18, tests T-21, T-25. |
| §4.4 — errors one at a time | Destination parse failure is collected with the request's other field validations **before** any 422 path. Step 16, test T-23. |
| §4.2 — reason projected onto `type` | Entrypoints maps each domain reason to a **deliberately chosen kebab-case string**; the domain enum is never serialized. Step 16, test T-19. |

### Two conflicts found at the execute-time pre-flight, and how the plan was amended

The pre-flight at `/workflow-execute` Step 3.5 described the **project layout** rather than the design shape, and returned five rules the plan-time run had not — §2.1, §2.3, §3.3, §3.5, §5.3. Two of them caught defects in this plan. **Both were resolved before any code was written.**

| Conflict | Rule | Resolution |
|---|---|---|
| The Domain predicate takes the resolution outcome type as a parameter, but the plan declared that type behind an Application interface — which would force a Domain→Application reference | §2.1 *"Domain references no other project in this system"* | **The outcome type is declared in Domain.** The resolver interface stays in Application per §1.3 and returns the Domain-declared type. Application references Domain, which §2.1 permits. |
| Entrypoints mapped the Domain verdict and reason types directly onto the HTTP contract | §5.2, §5.3 | **The Application handler translates the Domain verdict into an Application-declared result type**; Entrypoints maps that. Keeps the published `type` strings a deliberate Entrypoints choice rather than a serialized Domain enum, which §4.2 requires. |

### An absence the advisor reported

`architecture/` carries no rule governing the shape of a **general outbound port** — where its interface is declared, or whether it must be async and take a cancellation token. The advisor reported the gap rather than supplying a rule. **ADR-001 fills it** (Step 1), which is also what `STD-ARCH-06` requires for a first outbound integration.

---

## Relevant Learnings

**`docs/solutions/` is empty.** This is the first issue through the loop, so there are no prior lessons to inline. Recorded rather than omitted — a missing section reads identically as "no lessons applied" and "nobody looked".

---

## Testing Strategy

25 cases. Every one is written and observed to fail before its implementation exists.

### Domain — parsing and scheme

| ID | Test | Type | Proves | Edge cases |
|---|---|---|---|---|
| T-01 | `http` scheme is accepted | unit | AC-1 | uppercase `HTTP`, trailing dot in host |
| T-02 | `https` scheme is accepted | unit | AC-1 | port specified, userinfo present |
| T-03 | Any other scheme is rejected with reason `scheme-not-permitted` | unit | AC-2 | `javascript:`, `data:`, `file:`, `ftp:`, scheme-relative `//host` |
| T-13 | A value that cannot be parsed as an absolute URL is rejected with a parse reason | unit | AC-4 | empty string, whitespace only, `http://`, spaces in host |
| T-14 | A relative URL is rejected as unparseable rather than accepted | unit | AC-4 | `/path`, `example.com` with no scheme |

### Domain — address rules

| ID | Test | Type | Proves | Edge cases |
|---|---|---|---|---|
| T-04 | IPv4 loopback is rejected | unit | AC-3 | `127.0.0.1`, anything in `127.0.0.0/8` |
| T-05 | IPv4 private ranges are rejected | unit | AC-3 | `10.0.0.0/8`, `172.16.0.0/12` boundaries, `192.168.0.0/16` |
| T-06 | IPv4 link-local and reserved are rejected | unit | AC-3 | `169.254.0.0/16`, `0.0.0.0`, `255.255.255.255`, CGNAT `100.64.0.0/10` |
| T-07 | IPv6 loopback, link-local and unique-local are rejected | unit | AC-3 | `::1`, `fe80::/10`, `fc00::/7` |
| T-08 | IPv4-mapped IPv6 addresses are rejected on their mapped value | unit | AC-3 | `::ffff:127.0.0.1`, `::ffff:10.0.0.1` |
| T-09 | A host resolving to several addresses is rejected if **any** is disallowed | unit | AC-3 | one private among four public; all public; all private |

### Domain — outcome handling and verdict shape

| ID | Test | Type | Proves | Edge cases |
|---|---|---|---|---|
| T-10 | Resolution outcome `NotFound` is rejected | unit | AC-6 | no A record; NXDOMAIN |
| T-11 | Resolution outcome `Failed` is rejected — fails closed | unit | AC-6 | timeout; SERVFAIL; resolver unreachable |
| T-12 | A scheme-only verdict is a distinct value from a fully-checked verdict | unit | AC-1, AC-6 | a caller cannot pass a scheme-only verdict where a full one is required |

### Application — orchestration

| ID | Test | Type | Proves | Edge cases |
|---|---|---|---|---|
| T-15 | The orchestration passes every resolved address to the predicate, unfiltered | unit, fake resolver | AC-3 | five addresses in, five evaluated |
| T-16 | The orchestration makes no accept or reject decision of its own | unit, fake resolver | AC-6 | on `NotFound` and `Failed` it still calls the predicate rather than short-circuiting |

### Infrastructure — DNS adapter

| ID | Test | Type | Proves | Edge cases |
|---|---|---|---|---|
| T-17 | The adapter returns addresses unfiltered and unranked | integration | AC-3 | a host with mixed IPv4 and IPv6 records |
| T-18 | The adapter applies an explicit timeout and returns `Failed` on expiry | integration | AC-6 | a resolver that never answers |

### Entrypoints — error contract

| ID | Test | Type | Proves | Edge cases |
|---|---|---|---|---|
| T-19 | A policy rejection returns `422` with a stable kebab-case `type` | integration | AC-2, AC-3 | each reason maps to its own published string |
| T-20 | An unparseable value returns `400` naming the offending field | integration | AC-4 | — |
| T-21 | No error body contains a resolved IP address or internal host name | integration | AC-3 | private-address rejection; resolution failure |
| T-22 | Every error response carries `traceId` | integration | AC-2, AC-4 | both the 400 and 422 paths |
| T-23 | On the `400` path, field failures are reported together | integration | AC-4 | two invalid fields in one request |

### Observability

| ID | Test | Type | Proves | Edge cases |
|---|---|---|---|---|
| T-24 | A rejection emits `link.destination.rejected` and increments `link_destination_rejections` | integration | AC-5 | one event per rejection, not per rule evaluated |
| T-25 | The resolved address appears in the log entry but not the response, correlated by `traceId` | integration | AC-3, AC-6 | — |

---

## Implementation Steps

**Test-writing steps precede the implementation steps they verify.** Every implementation step names its test cases.

- [x] **1.** Write **ADR-001** in `decisions/` — the DNS resolver port: where the interface is declared, whether it is asynchronous, whether it takes a cancellation token, and the timeout policy. Required by `STD-ARCH-06`, and it fills the gap the advisor reported. Must be accepted before Step 13.
- [x] **2.** Scaffold `src/UrlShortener.Domain`, `src/UrlShortener.Application`, `src/UrlShortener.Infrastructure`, `src/UrlShortener.Api` and the matching test projects under `tests/`. References per `layers.md` §2.1 — Domain references nothing; Application references Domain; Infrastructure references Application and Domain; Api references Application and Domain. Per §2.2, only the composition root in `UrlShortener.Api` references Infrastructure.
- [x] **3.** Write T-13, T-14 — parse failures. Run. Confirm they fail.
- [x] **4.** Implement URL parsing and the parse-failure reason. *(satisfies T-13, T-14)*
- [x] **5.** Write T-01, T-02, T-03 — scheme rules. Run. Confirm they fail.
- [x] **6.** Implement the scheme allowlist in the Domain predicate. *(satisfies T-01, T-02, T-03)*
- [ ] **7.** Write T-12 — the two-state verdict. Run. Confirm it fails.
- [ ] **8.** Implement the verdict type as two distinct states. *(satisfies T-12)*
- [ ] **9.** Write T-04 … T-09 — address ranges across both families, and the multi-address rule. Run. Confirm they fail.
- [ ] **10.** Implement the address-range rules in the Domain predicate. *(satisfies T-04 … T-09)*
- [ ] **11.** Write T-10, T-11, T-16 — resolution outcomes fail closed, and the orchestration decides nothing. Run. Confirm they fail.
- [ ] **12.** Implement the resolution outcome type **in Domain** (§2.1), outcome handling in the Domain predicate, and the Application orchestration that passes it through untouched. *(satisfies T-10, T-11, T-16)*
- [ ] **13.** Write T-15, T-17, T-18 — passthrough, unfiltered results, explicit timeout. Run. Confirm they fail.
- [ ] **14.** Implement the Infrastructure DNS adapter per ADR-001, returning results unfiltered and unranked. *(satisfies T-15, T-17, T-18)*
- [ ] **15.** Write T-19 … T-23 — the error contract. Run. Confirm they fail.
- [ ] **16.** Implement the translation in two hops per §5.2 and §5.3: the Application handler converts the Domain verdict into an **Application-declared result type**, then Entrypoints maps that result to a published kebab-case `type`, `422` for policy and `400` for parse, field failures collected together, `traceId` on every response. *(satisfies T-19 … T-23)*
- [ ] **17.** Write T-24, T-25 — log event, counter, and address-in-log-not-response. Run. Confirm they fail.
- [ ] **18.** Implement the structured log event and the counter. *(satisfies T-24, T-25)*
- [ ] **19.** Run the full suite, lint and format. Verify every AC has a passing test asserting it.

---

## Decisions Made

| Decision | Rationale |
|---|---|
| Pure Domain predicate; DNS resolution in Application via an Infrastructure port | The brainstorm's stress test reversed the original recommendation. #19 needs a synchronous scheme-only answer; an async domain service would force a stubbed resolver to ask a synchronous question, and that friction is what produces an inline check in a handler. |
| The port returns a **resolution outcome** — `Resolved` / `NotFound` / `Failed` — not a bare address list | Advisor flag 1. An empty list would force the Application layer to distinguish "no addresses" from "resolution failed", which is a rule about a permitted destination and belongs in Domain per `layers.md` §1.3. |
| Reject if **any** resolved address is disallowed | Fails closed. Our resolver and the visitor's may select different addresses from the same set, so requiring all to be disallowed fails open on exactly the case that matters. |
| Both address families, including IPv4-mapped IPv6 | An IPv4-only check lets `::ffff:127.0.0.1` through, which defeats the control on any dual-stack host. |
| The verdict is two-state, not boolean | Brainstorm stress test: a boolean lets a caller mistake a scheme-only verdict for a fully-checked one, and that failure mode fails open. |
| ADR-001 required before the adapter is built | `STD-ARCH-06` for the first outbound integration, and it fills the outbound-port-shape gap the advisor reported in `architecture/`. |
| `422` for policy rejections, `400` for parse failures | `api.md` §4.5 and §4.4. Recorded on #17 with both citations. |
| DNS resolved at creation only | Brainstorm Q&A-2 — keeps DNS off #19's 50 ms p99 redirect path. |

**No open questions.** Every risk identified at Step 4 was resolved before this document was written.

---

## Known Constraints

Accepted limitations, not worries.

**DNS rebinding is not detected.** A host resolving to a public address at creation may later resolve to a private one. Accepted because the service never fetches the destination server-side — it emits a `Location` header and the visitor's own browser makes the request from its own network. **If server-side fetching is ever added, this decision and the brainstorm's Q&A-2 must both be revisited.**

**No host allowlist.** `STD-SEC-05` says *"an allowlist of permitted schemes and hosts"*. A literal host allowlist would make a general URL shortener useless, so "and hosts" is read as requiring a host **check** — supported by the rule's own detection hint, which looks for *"no scheme and host check"*. Recorded in the brainstorm as an interpretation a reviewer may overturn.

**`architecture/` has no outbound-port rule.** The advisor reported the absence rather than inventing one. ADR-001 fills it for this port; a general rule remains unwritten and is a candidate for `/workflow-compound`.

**Forward conformance does not read `architecture/`.** The `400`-versus-`422` defect on #17 was found at brainstorm, not at `/github-issues` Step 4, because that step checks `standards/` only. Logged as a candidate rule for `/workflow-compound`.
