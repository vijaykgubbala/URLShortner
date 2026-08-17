# Brainstorm — #17 Validate destination URLs against an allowlist policy

**Issue:** [#17](https://github.com/vijaykgubbala/URLShortner/issues/17) · `risk:high` · `status:refined`
**Traces to:** REQ-001, REQ-002; NFR-006, NFR-009
**Intake:** `ambiguous` · `greenfield` — brainstorm mandatory, not optional
**Date:** 2026-08-17

---

## Problem Statement

Deliver the policy that decides whether a destination URL may be stored and redirected to. Every other link operation depends on it: #18 calls it at creation, #19 at resolution, #24 on update.

`STD-SEC-05` sets the bar at `critical` severity:

> *"A user-supplied or stored destination URL must be validated against an allowlist of permitted schemes and hosts immediately before issuing a redirect or server-side request."*

**Out of scope:** the create, redirect and update endpoints themselves. This issue delivers the policy they call.

**What makes it `risk:high`:** an unvalidated destination makes this service a credible-looking hop for someone else's phishing, and — were we ever to fetch the URL server-side — a proxy into whatever network it runs in. Both are the system working exactly as written.

---

## Codebase Context

**Research deviation, recorded.** Step 1 specifies two dispatched agents. `src/` and `docs/solutions/` are both empty — there is no existing pattern to follow or diverge from, and no prior pitfall recorded. Research was performed inline against `architecture/` and `standards/`, which are the documents that actually constrain this issue. Recorded rather than skipped silently.

### What binds this work

| Source | Text | Consequence |
|---|---|---|
| `architecture/layers.md` §3.2 | *"A rule that constrains what is valid about domain state — expiry, uniqueness, code format, **permitted destination** — is implemented in Domain, on the entity or in a domain service. It must not be implemented only in a handler, controller, or view model."* | Names **permitted destination** explicitly. The policy is a Domain rule, not request-shape validation. Rules out an entrypoint filter. |
| `architecture/api.md` §4.5 | *"A well-formed request that violates a domain rule returns `422`, not `400`."* | **Conflicts with #17's acceptance criteria**, which say `400`. See Q&A-1. |
| `architecture/api.md` §4.4 | *"A request failing input validation returns `400` with one entry per invalid field."* | A URL that will not parse is genuinely `400`. |
| `architecture/layers.md` §3.6 | *"Current time and random or generated values must be obtained through an interface declared in Domain or Application and implemented in Infrastructure."* | By analogy, DNS resolution is an Infrastructure concern reached through a port. |
| `STD-OPS-06` | *"Every call leaving the process must specify an explicit timeout."* | A DNS lookup is a call leaving the process. |
| #19's ACs | Re-validate stored destinations at redirect, **and** hold 50 ms p99 at 100 rps | These two pull against each other if validation requires DNS. |

### Two findings the research surfaced

**F-1 — the acceptance criteria specify the wrong status code.** #17 says `400` for a disallowed scheme and for a private-range host. Per §3.2 the destination policy is a domain rule, and per §4.5 a domain rule violation is `422`. A well-formed `javascript:` URL is not a malformed request.

**F-2 — forward conformance cannot catch F-1.** `/github-issues` Step 4 checks issues against `standards/`. The rule that #17 violates lives in `architecture/api.md`, which forward conformance never reads. **This is a gap in the pipeline, not just in this issue** — the error contract, the layer model and the API shape are all unenforced at story time. Carried forward as a candidate rule for `/workflow-compound`'s graduation question.

---

## Q&A Decisions

### Q&A-1 — Status code for a policy rejection

**Question.** §4.5 says a domain-rule violation returns `422`; §3.2 names permitted destination as a domain rule; #17's ACs say `400`.

**Decision: `422` for policy rejections, `400` for a URL that will not parse.** #17's ACs are amended.

**Why it matters:** the distinction tells a caller whether to fix the request or stop retrying it. A client receiving `400` for a well-formed URL to a blocked host will keep re-sending it in slightly different forms.

### Q&A-2 — When the private-address check runs

**Question.** #19 requires re-validating stored destinations at redirect and holds 50 ms p99 at 100 rps. The private-address check needs DNS, which is a call leaving the process.

**Decision: resolve DNS once at creation, with an explicit timeout. At redirect, re-check the scheme and the stored policy verdict only — no DNS on the hot path.**

**Accepted residual risk:** DNS rebinding. A hostname that resolved to a public address at creation may resolve to a private one later, and we will not detect it. This is acceptable **because we never fetch the destination server-side** — we emit a `Location` header and the visitor's own browser makes the request from its own network. The SSRF half of `STD-SEC-05`'s rationale does not apply to a pure redirect. **If server-side fetching is ever added, this decision must be revisited**, and that dependency is recorded here rather than left to be rediscovered.

### Q&A-3 — What "allowlist of permitted hosts" means

**Question.** `STD-SEC-05` requires *"an allowlist of permitted schemes and hosts"*. A literal host allowlist means only pre-approved destinations can be shortened, which is not a URL shortener.

**Decision: read it as a scheme allowlist plus a host check, satisfied by denying loopback, link-local, private and reserved ranges.**

The rule's own detection hint supports this reading — it looks for *"no scheme and host check on the path between them"*, a **check**, not an allowlist. Recorded as an interpretation a reviewer may overturn, not as a settled fact.

**The alternative was to escalate**, since REQ-001 requires accepting any destination and the standard's letter requires an allowlist. It was not escalated because the detection hint resolves the ambiguity within the standard itself.

### Q&A-4 — Scope if it has to be cut

Not asked separately; the answer follows from the above. The scheme allowlist is the irreducible core — it alone prevents `javascript:` and `data:` destinations. The address-range check is the part that could be deferred, and deferring it would be a documented limitation rather than a silent omission.

---

## Proposed Approaches

### Approach A — Domain policy service holding a resolver port

A `DestinationPolicy` domain service with an `IAddressResolver` interface declared in Domain and implemented in Infrastructure. The service resolves the host, then applies both rules.

**Pros.** The whole rule lives in one Domain type, satisfying §3.2 unambiguously. One call site for all three consumers.

**Cons.** Makes a Domain service asynchronous and network-aware. Every domain test needs a fake resolver. The scheme-only check at redirect must still go through an async service with a stubbed resolver.

### Approach B — Pure Domain predicate, Application orchestration, Infrastructure resolver

Three parts:

- **Domain** — `DestinationPolicy.Evaluate(scheme, host, resolvedAddresses)` returning a structured verdict. Pure, synchronous, no I/O.
- **Application** — the use case calls an `IAddressResolver` port, then passes the result to the Domain predicate.
- **Infrastructure** — the DNS implementation, with the timeout `STD-OPS-06` requires.

**Pros.** The rule — *what is permitted* — is in Domain per §3.2, and stays pure and synchronously testable. The redirect path calls the same predicate with no resolved addresses to get a scheme-only verdict, with no resolver involved. I/O sits where §3.6's analogy puts it.

**Cons.** The policy is expressed across two layers, so a reader must look in two places to see the whole flow. Slightly more ceremony than A.

### Approach C — Entrypoint filter

Validate in a request filter before the handler.

**Rejected.** §3.2 states the rule *"must not be implemented only in a handler, controller, or view model"*. A filter is a handler concern, and the redirect path would need its own copy — which is precisely the duplication §3.2 exists to prevent.

---

## Recommendation

**Approach B**, after the stress test changed the answer from A.

**Why.** It is the simplest approach that satisfies §3.2 without dragging network I/O into the Domain layer. The decisive factor is the redirect path: Q&A-2 requires a scheme-only check there, and B gives that for free by calling the same pure predicate with an empty address list. Under A, the redirect path would have to invoke an async service with a stubbed resolver to ask a synchronous question — friction that invites someone to write an inline `if (scheme != "https")` in the handler, which is the §3.2 violation the rule exists to prevent.

**YAGNI check.** B is not the more extensible option; it is the one with fewer moving parts at the point of use. No configuration surface, no policy registry, no pluggable rule chain — one predicate and one port.

---

## Stress-Test Findings

**This step changed the recommendation. It was A; it is now B.**

### The strongest objection to A

A Domain service that depends on an asynchronous I/O port makes the Domain layer network-aware, which is the thing layering exists to prevent. The genuine domain rule is a **pure predicate** — *"this scheme is permitted and this address is not private"* — over facts already gathered. Resolution is not a domain concern; it is how the caller obtains a fact the rule needs.

`architecture/layers.md` §3.6 supports the objection by analogy: time and randomness are obtained *through* an interface, so that domain behaviour stays testable. Putting the resolver *inside* the domain service inverts that.

**Verdict: the objection holds.** Approach A was revised into Approach B.

### Where B fails

B fails if the two halves drift — if a future consumer calls the Domain predicate without first resolving, gets a scheme-only verdict, and treats it as a full one. That is a real failure mode and it fails **open**, which is the dangerous direction.

**Mitigation, to be carried into `/workflow-plan`:** the verdict type must distinguish *"permitted, fully checked"* from *"permitted, scheme only"* so a caller cannot mistake one for the other. A boolean return would make this failure invisible; a two-state verdict makes it a compile-time distinction.

### What would have to be true for the runner-up to win

**Approach A wins if DNS resolution becomes unconditional** — if the redirect path is ever required to re-resolve, the scheme-only path disappears and B's main advantage with it.

**Is that likely?** Only if server-side fetching is added, which would also invalidate the residual-risk acceptance in Q&A-2. Both hinge on the same condition, and that condition is recorded in both places. Today it is not true.

**Approach C never wins.** It violates a stated rule, and the only argument for it is convenience.

---

## Carried forward to `/workflow-plan`

1. **Amend #17's ACs to `422`** for policy rejections before planning — `/workflow-plan` copies acceptance criteria verbatim, so a wrong AC becomes a wrong test.
2. **The verdict type is two-state**, not boolean — see the stress test.
3. **The DNS resolver needs an explicit timeout** per `STD-OPS-06`, and a failure to resolve must fail **closed**.
4. **Rejection reasons are structured**, one per failing rule, to satisfy §4.4's "one entry per invalid field" shape for the parse-failure case.
5. **DNS rebinding is an accepted residual risk**, contingent on never fetching the destination server-side.

---

## Related Docs

- Intake: [`docs/intake/2026-08-17-url-shortener-assignment-intake.md`](../intake/2026-08-17-url-shortener-assignment-intake.md)
- Plan: [`docs/plans/2026-08-17-solution-plan.md`](../plans/2026-08-17-solution-plan.md) — WS-2
- Conformance: [`validation/url-shortener-conformance.md`](../../validation/url-shortener-conformance.md) — F-06 recorded this issue as PASS against `STD-SEC-05`
- Standards: `standards/security.md` `STD-SEC-05`, `standards/operability.md` `STD-OPS-06`
- Architecture: `architecture/layers.md` §3.2, §3.6 · `architecture/api.md` §4.4, §4.5
- Past learnings: **none — `docs/solutions/` is empty.** This is the first issue through the loop.
