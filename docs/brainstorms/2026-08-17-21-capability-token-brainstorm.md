---
issue: 21
title: Protect mutations with a capability token
risk: risk:high
intake: docs/intake/2026-08-17-url-shortener-assignment-intake.md
ambiguity: ambiguous (brainstorm mandatory)
date: 2026-08-17
---

# Brainstorm — #21 Protect mutations with a capability token

## Problem Statement

Creation returns a secret management token; update and delete require it. This is the
resolution of the `STD-SEC-01` escalation recorded in **ASM-007** — the source document
states no authentication model at all, and the standard requires one at `critical`
severity. ASM-007's outcome is explicit: **`STD-SEC-01` is satisfied, not waived.**

**In scope:** token generation, the stored representation, presentation and verification on
a mutation request, and `DELETE` as the first consumer (see Q2).

**Out of scope:** user accounts, sessions, any persistent owner identity — stated on the
issue. Token rotation and recovery are also out of scope, and are a consequence of Q1
rather than an omission: a hash-only store makes a lost token unrecoverable by design.

---

## Codebase Context

### There is no authorization of any kind today

Zero occurrences of `401`, `403`, `Forbidden` or `Unauthorized` across `src/` and `tests/`.
`Program.cs` is 46 lines with **no `app.Use*` middleware at all** — no authentication, no
authorization, no exception handler, no CORS. Nothing anywhere reads an HTTP header; the
only `HttpContext` access is `TraceIdentifier`, `Request.Scheme` and `Request.Host`.

This is the first authorization surface in the system, not an extension of one.

### The established shape for a generated secret

`IShortCodeGenerator` is declared in Domain and implemented in Infrastructure, and the
XML doc records why: *"randomness must come through an interface (§3.6) so the domain stays
testable without a random source."* `architecture/layers.md` §3.6 makes this binding —
**a direct `RandomNumberGenerator` call outside Infrastructure is prohibited**, so the
token generator needs its own interface rather than a static helper.

`RandomNumberGenerator` appears exactly once in the repo, in `CryptoShortCodeGenerator`,
and uses `GetInt32` per character — there is no byte-buffer or base64url helper to reuse.

### The create response is the only serialized success type

```csharp
public sealed record CreateShortLinkResponse(string Code, string Destination, string ShortUrl);
```

Adding a field to it is permitted within the version by `architecture/api.md` §3.1 —
*"Adding an optional request field or a new response field is permitted within an existing
version."*

### Every response type is a `record`, and that matters

C# synthesizes a `ToString` on a record that prints every property. `STD-SEC-03`'s
detection hint names precisely that: *"A property or field whose name contains `secret`,
`token`, `key`... that is public, has a public getter on a serializable type, **or appears
in a `ToString` override**."*

**There is no existing pattern in this repo for a value that must not be serialized** — no
`[JsonIgnore]`, no wrapper type, no suppressed `ToString`. This is the first one.

### The error contract, and the precedent for not naming the check

`DestinationProblem` carries `(Type, Title, Status, Detail, TraceId, Errors)`. Its `Policy`
factory is private, so a new status needs a new public factory — the way `Unavailable` was
added during the #19 remediation.

Two of its arms deliberately refuse to say what failed:

```csharp
// Deliberately says nothing about which address, or that an address was
// involved at all — api.md §4.3. The address is in the log, under this traceId.
```
```csharp
// Says nothing about userinfo specifically — api.md §4.3. Naming the check
// tells a probe which shape to try next.
```

That reasoning is the direct precedent for Q3.

### There are no migrations

Schema is created by `Database.EnsureCreated()` at startup, which does nothing against an
existing database. **A token-hash column is the first schema change to hit that gap** on an
existing `shortlinks.db`. Tracked as #49; this issue depends on it in practice even though
the board does not say so.

### Past learnings that apply

**`enumerate-the-category-not-the-examples`** (#17). A criterion named a *category* —
"reserved address range" — and the implementation shipped four named examples, with the
tests derived from the same examples so they agreed perfectly and proved nothing.

AC-4 here is a category claim: *"any response other than the original create."* Today that
is three responses; #22, #24 and #25 add more. An enumerated test would agree with the
implementation forever. **The prescription — "express a range as a range" — means the
absence proof must be structural, not per-endpoint.** See the Stress Test.

**`a-named-gap-is-not-a-work-item`** (#17). A gap was measured, written down as gap 1 of
10, and shipped anyway, because nothing carries a handover gap into the review record.
#21 has three absence/negative ACs, and absence is exactly what gets deferred into a
"what's not tested" line. Each needs a test or a follow-up issue before handover closes.

---

## Q&A Decisions

### Q1 — NFR-008 forbids a serializable token property; AC-1 requires the token in the create response

`NFR-008`: *"No property holding a credential, token or key shall be serializable or
exposed on a public surface."* AC-1: *"Given a successful create, when the response is
returned, then it contains a management token."*

As written these contradict. They are reconciled by applying them to different things.

**Decision: store a hash, return the plaintext exactly once.** The persisted property is
not the credential, so `NFR-008` binds cleanly; the credential exists in memory for one
request and is never at rest. Verification hashes the presented token and compares.

**Consequence:** a lost token is unrecoverable. That is a design property, not a defect,
and it is why token recovery is out of scope rather than deferred.

**Consequence:** `STD-DATA-03` — *"Any hash, token, fingerprint, or surrogate key that can
be linked back to a person must carry the same retention cap as the personal data it
derives from"* — is satisfied without a new purge job, because the hash lives on the link
row and dies with it.

### Q2 — #24 and #25 are blocked by #21, so no mutation endpoint exists when #21 ships

AC-2 and AC-3 are written about mutation requests. With no mutation endpoint, neither can
be proven at the HTTP boundary — which is how an AC becomes unprovable at `/gate-check`
criterion 2, exactly as #19's two latency criteria did.

**Decision: pull `DELETE` into #21.** This issue delivers the token, the verification, and
`DELETE /v1/short-links/{code}` as its first consumer. Every AC becomes provable end to
end. **#25 closes as delivered here.**

Deliberately not both — pulling `UPDATE` in as well would make this three stories in one,
which is the sizing signal `/github-issues` exists to split on. `DELETE` is the smaller
consumer and exercises the whole mechanism.

### Q3 — AC-3 says 403; `api.md` §2.6 says 401 for a missing credential

Two authorities, quoted:

> **#21 AC-3:** *"Given a mutation request with a **missing or incorrect** token... then
> **`403`** is returned and no change is made."*
>
> **`architecture/api.md` §2.6:** *"**`401`** for a missing or invalid credential, **`403`**
> for an authenticated caller without permission."*

A capability token is a credential, not an identity, so §2.6 puts a *missing* token at 401.
Underneath that sits a worse problem: `DELETE /v1/short-links/{code}` has three failure
shapes — unknown code, known code with wrong token, known code with no token — and
answering them differently tells a prober **which codes exist**, turning the authorization
check into an enumeration oracle against the space `architecture/data.md` §1.4 exists to
protect.

**Decision: all three failures return an identical `404` with an identical body.** An
attacker learns nothing. This **overrides AC-3 as drafted**, and the amendment is recorded
here as a security decision rather than applied silently.

**Cost accepted:** a legitimate caller who mistypes their token gets a 404, which is
confusing. Judged acceptable because the alternative discloses the existence of every code
an attacker cares to probe.

**Consequence:** the `403` in AC-3 and the `401` in `api.md` §2.6 are both unused on this
path. §2.6 is not violated — no 401 or 403 is emitted at all — but the deviation from the
AC must be carried into `/workflow-plan` as an amended criterion.

### Defaults taken without asking

Stopping the interrogation here, per the question protocol — the rest have a defensible
default and asking would spend attention without changing the answer.

| Default | Reasoning |
|---|---|
| Token presented in an `Authorization` header | A query string lands in access logs, `Referer` and browser history. A body on `DELETE` is non-standard. |
| 32 bytes of CSPRNG output, base64url-encoded | 256 bits removes brute force from the threat model entirely. |
| SHA-256, not bcrypt or argon2 | Slow KDFs defend *low-entropy* secrets against dictionary attack. A 256-bit random token has no dictionary, so a fast hash is correct and keeps the mutation path cheap. |
| Verification as a pure Domain predicate | Mirrors `DestinationPolicy`: Domain judges, Application orchestrates — `layers.md` §3.2. |
| A fixed-time comparison for the hash | Standard for any secret comparison; costs nothing here. |

---

## Proposed Approaches

Q1 settled that a hash is stored. What remains open is **where it lives** and **where
verification happens**.

### Approach A — Hash as a column on `ShortLink`, verification as a Domain predicate

A `TokenHash` property on the entity, mapped in `OnModelCreating` beside the others. A
`LinkToken` Domain type owning generation-shape and comparison. `DeleteShortLink` in
Application orchestrates: find, verify, delete.

**Pros**
- One row per link; no join, no second table, no lifecycle to keep in sync.
- The hash dies with the link, so `STD-DATA-03` needs no purge job.
- Matches `architecture/data.md` §1.2, which already anticipates *"its owner identifier
  where one applies"* as a field on the link record.
- Verification in Domain mirrors `DestinationPolicy`, the pattern #17 established and #19
  confirmed was right.

**Cons**
- The credential hash and the resource share a row, so a careless read loads both.
- `FindAsync` selects the whole entity, so the redirect hot path would load a column it
  never uses. (Addressed in the Stress Test.)

### Approach B — A separate `LinkCredential` entity keyed by code

A second table holding `(Code, TokenHash, CreatedAt)`, with its own repository methods.

**Pros**
- The credential is never loaded by a query for a link, so the redirect path cannot touch
  it and no response shaped from `ShortLink` can leak it.
- Leaves room for rotation, multiple tokens per link, and revocation history.

**Cons**
- A second table, a second repository, and a lifecycle that must be kept consistent with
  the link — a delete that removes one and not the other leaves an orphan.
- Rotation and multiple-tokens are explicitly out of scope, so the flexibility is bought
  for a requirement nobody has.
- Two schema objects created by `EnsureCreated`, doubling the exposure to the missing
  migration path.

### Approach C — HMAC-derived token, nothing stored

Considered and rejected at Q1: rotating the server secret invalidates every token at once,
the secret itself becomes a new secret to manage with no existing pattern, and revoking a
single link's token is impossible.

---

## Recommendation

**Approach A**, with the two amendments the stress test produced.

YAGNI decides between A and B. B's advantages are rotation, revocation history and
multiple tokens per link — all three explicitly out of scope. Its one advantage that is
*not* speculative is isolation of the hash from link reads, and that is obtainable inside A
at lower cost (see amendment 1) than a second table and a second lifecycle.

---

## Stress-Test Findings

### The strongest objection

**Approach A puts the credential and the resource in one row.** `EfShortLinkRepository
.FindAsync` selects the whole entity, so every redirect — the hot path, held to 50 ms at
p99 — loads a token hash it has no business seeing. The `ResolveShortLink` use case would
hold a credential-derived value it never uses, which is precisely the shape that leaks
later.

This is a real cost, not a theoretical one, and Approach B does not have it.

### The scenario where it fails

**#22 "Query the link collection" is already on the board.** If it returns link records
shaped from the entity — the obvious implementation — the token hash goes out on the wire
on a read endpoint nobody thought of as a credential surface.

This is not hypothetical. It is the `enumerate-the-category-not-the-examples` failure
arriving on schedule: AC-4 says *"any response other than the original create"*, an
enumerated test covers the three responses that exist today, #22 adds a fourth, and the
test still passes.

### What would have to be true for the runner-up to win

Approach B wins if **either** the hash must be isolated from link reads by construction
rather than by discipline, **or** rotation lands in scope.

The second is settled — rotation is out of scope and the issue says so.

The first is the live one, and it does **not** hold, because the isolation A needs is
already mandated by an existing rule rather than by new discipline: `layers.md` §5.2/§5.3
forbid mapping a Domain type onto the published contract. #22 returning entity-shaped
records would violate that rule *today*, independent of this issue. A is therefore
protected by a rule that already exists and is already enforced by `review-architecture`.

### Did the recommendation change?

**No — but it gained two amendments, and both are now binding on `/workflow-plan`.**

1. **The resolve path must not load the hash.** Add a projection to the repository so the
   redirect reads only what it needs, rather than selecting the entity. This removes the
   objection at a fraction of B's cost and pays back into the 50 ms budget.

2. **The absence proof must be structural, not per-endpoint.** A test asserting "the token
   is not in the redirect response, the query response, the delete response" is an
   enumeration and will agree with the implementation forever. Assert instead over *every*
   type reachable from a response — by reflection across the response types in the
   assembly, or by asserting the hash property is not reachable from any type the API
   serializes. That is the "express a range as a range" prescription applied to this AC.

Without the stress test, amendment 2 would have been written as three assertions and #22
would have leaked past them.

---

## Consequences for the plan

- **AC-3 is amended** — 404 for all three failure shapes, not 403. Carry the amended text
  into `/workflow-plan` verbatim; do not paraphrase.
- **AC-2 and AC-3 become provable** because `DELETE` is in scope. #25 closes here.
- **AC-4's test is structural.** An enumerated test does not satisfy it.
- **AC-5's test uses the `RecordingLogger`** already established, asserting
  `DoesNotContain` over `logger.Messages` — the inversion of the existing
  `Assert.Contains(logger.Messages, m => m.Contains("10.0.0.5"))` idiom.
- **A schema change lands with no migration path.** #49 covers it; this issue should not
  invent a second mechanism, and the plan must say which it depends on.
- **`requirements/nfr.md:49` is stale** — it still reads *"Authorization is unresolved and
  escalated"*, which ASM-007 resolved. Worth correcting when this lands.

---

## Related Docs

| Doc | Bearing |
|---|---|
| [`docs/intake/2026-08-17-url-shortener-assignment-intake.md`](../intake/2026-08-17-url-shortener-assignment-intake.md) | `ambiguous` classification — brainstorm mandatory, not optional |
| [`requirements/assumptions.md`](../../requirements/assumptions.md) ASM-007 | The decision this issue implements, and its history as an ESCALATE |
| [`requirements/nfr.md`](../../requirements/nfr.md) NFR-008, NFR-009 | The clauses Q1 reconciles |
| [`docs/solutions/validation/enumerate-the-category-not-the-examples.md`](../solutions/validation/enumerate-the-category-not-the-examples.md) | Produced stress-test amendment 2 |
| [`docs/solutions/process/a-named-gap-is-not-a-work-item.md`](../solutions/process/a-named-gap-is-not-a-work-item.md) | Three absence ACs need destinations before handover closes |
| [`docs/reviews/2026-08-17-19.json`](../reviews/2026-08-17-19.json) | SEC-006 established the log-hygiene pattern AC-5 needs |
