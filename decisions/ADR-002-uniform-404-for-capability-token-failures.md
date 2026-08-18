# ADR-002 — A resource protected by a capability token returns 404 for every authorization failure

**Status:** Proposed
**Date:** 2026-08-17
**Issue:** [#21](https://github.com/vijaykgubbala/URLShortner/issues/21)
**Required by:** [`governance/architecture-docs-edit-gate.md`](../governance/architecture-docs-edit-gate.md) §3 — *"A change that adds, removes, or alters a rule additionally requires an `ADR-nnn` in `decisions/` recording what changed and why."*
**Amends:** [`architecture/api.md`](../architecture/api.md) §2.6

---

## Context

Issue #21 adds the system's first authorization surface: creating a short link returns a
secret management token, and mutating that link requires presenting it. There are no user
accounts and no sessions — the token *is* the authorization.

`DELETE /v1/short-links/{code}` therefore has three failure shapes:

1. The code does not exist.
2. The code exists, a token was presented, and it is wrong.
3. The code exists and no token was presented.

`architecture/api.md` §2.6 as written assigns these three different answers:

> *"`401` for a missing or invalid credential, `403` for an authenticated caller without
> permission, `404` for an absent resource."*

A capability token is a credential on the plain reading of that rule, so case 3 is `401`,
case 2 is `401` or `403`, and case 1 is `404`.

**Answering them differently discloses which codes exist.** A prober walks the code space
and reads the status: `404` means "keep looking", anything else means "this code is real".
That turns the authorization check into an enumeration oracle against the same space
`architecture/data.md` §1.4 spends a CSPRNG to protect:

> *"A short code is generated from a cryptographically secure random source, not from a
> sequence, a counter, an incrementing key, or a hash of the destination. Sequential codes
> make the whole link set enumerable."*

Generating codes unguessably and then confirming guesses through the status code gives the
property back.

`architecture-advisor`, asked about this work, flagged the conflict and was explicit about
the remedy:

> *"An anti-enumeration uniform-404 is not permitted anywhere in `architecture/`; if it is
> the intended design it needs that ADR, not a local exception."*

---

## Decision

**A resource whose authorization is a capability token returns an identical `404`, with an
identical body, for an unknown identifier, a wrong token, and a missing token.**

`architecture/api.md` §2.6 is amended to carry this exception. It is written as a narrow
exception rather than a general licence: it applies only where the credential *is* the
resource's authorization and there is no authenticated principal to whom a `403` could be
meaningfully addressed.

### Why this is not a deviation from HTTP

This is the case the HTTP specification anticipates. **RFC 9110 §15.5.4**:

> *"An origin server that wishes to 'hide' the current existence of a forbidden target
> resource MAY instead respond with a status code of 404 (Not Found)."*

And §15.5.5 defines `404` broadly enough that this is not a lie about the resource:

> *"The 404 (Not Found) status code indicates that the origin server did not find a current
> representation for the target resource **or is not willing to disclose that one exists**."*

So `api.md` §2.6 was not right-and-being-overridden. It was **narrower than the
specification it reflects**, listing the common mapping and omitting the hiding case the
RFC explicitly permits. The amendment closes that gap rather than carving out a local
exception, which is why it is written into the document for every future endpoint rather
than recorded as a one-issue waiver.

Precedent: GitHub's REST API returns `404` rather than `403` for private resources.

---

## Consequences

### Accepted costs

**A legitimate caller with a mistyped or lost token sees "not found".** They may conclude
the link was deleted. Mitigated by the `traceId` already required on every error body by
`api.md` §4.6 — the real reason exists in the log under that identifier. Accepted because
the alternative discloses the existence of every code an attacker cares to probe.

**No `WWW-Authenticate` challenge is ever emitted.** RFC 6750 §3 expects a resource server
rejecting a bearer token to answer `401` with a challenge. This design never emits `401` on
this path, so the challenge never appears. RFC 9110 §15.5.2's *"A server generating a 401
response MUST send at least one WWW-Authenticate header field"* is not violated — no `401`
is generated, so the requirement never fires — but the deviation from RFC 6750's response
model is real and is recorded here rather than discovered later.

### Consequences that create obligations

Two properties of `404` mean the decision is not self-executing. **Both are created by this
decision and neither is optional.**

**1. `404` is heuristically cacheable; `403` is not.** RFC 9110 §15.5.5:

> *"A 404 response is heuristically cacheable; i.e., unless otherwise indicated by the
> method definition or explicit cache controls."*

An intermediary may cache an authorization failure and serve it back to the legitimate
token holder. **Every `404` on a token-protected route must carry `Cache-Control:
no-store`.** Switching `403`→`404` introduces a caching problem that did not exist before.

**2. Identical responses do not imply identical timing.** If the handler returns `404`
immediately on an unknown code but hashes and compares on a found one, the two paths differ
measurably in duration and the oracle reopens underneath the status code.

**The verification path must do the same work whether or not the code exists** — always
hash the presented token, always run a fixed-time comparison, against a dummy value on the
not-found path.

### Scope of the uniformity

The disclosure is only closed if *every* method on the route behaves the same way. A `HEAD`,
an `OPTIONS`, a `405`, or a rate-limit response keyed on resource existence each reopens it.
This binds the whole route, not one handler.

---

## Alternatives considered

**Follow §2.6 as written — `401` missing, `403` wrong, `404` unknown.** Most conformant to
the document as it stood, clearest to a well-behaved client, and needs no ADR or gated edit.
Rejected because it accepts the enumeration oracle: a `403` confirms a code exists, which is
precisely what the code space is designed to hide.

**Record the reasoning in an ADR but leave `api.md` unchanged.** Cheaper, and touches no
gated path. Rejected because `architecture/README.md` §1.3 names this as the wrong shape:

> *"Where a rule here proves wrong once code exists, the correction is an `ADR-nnn` in
> `decisions/` plus an edit to the relevant file under the gate […] **not a local
> deviation.**"*

An ADR that contradicts an unamended rule is a local deviation with a decision record
stapled to it. Every future review of every future authorized endpoint would re-raise the
same conflict.

**Return `403` for both token failures and `404` only for an unknown code** — the criterion
as originally drafted on #21. Rejected for the same reason as the first alternative: the
`403`/`404` split is itself the oracle.

---

## Status of the amendment

The `api.md` §2.6 edit accompanying this ADR is a **gated change**.
[`governance/architecture-docs-edit-gate.md`](../governance/architecture-docs-edit-gate.md)
§3 requires *"review and approval by a named human other than the author, recorded on the
pull request"*, and states that **self-approval does not satisfy this gate at any risk
level**. This ADR is `Proposed` until that review happens; it becomes `Accepted` when the
pull request carrying it is approved by a second named human.
