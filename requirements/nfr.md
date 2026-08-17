# Non-functional requirements

All six categories are present. **A category that does not apply is marked not-applicable with a reason, never omitted** — the omitted ones are the ones nobody volunteers, and a missing section reads identically as "no concern here" and "nobody looked".

**Source note:** the assignment states almost no non-functional requirements directly. §6 names quality attributes ("modular, testable, reliable, secure, scalable") and §7 says "production-grade", but supplies no number, no target, and no threat model. Most entries below therefore rest on an assumption, and each names it.

---

## 1. Performance

**Applies.** §2 requires "reliability features"; §6 requires scalable code; §4.4 requires a performance quality gate.

| ID | Requirement | Rests on |
|---|---|---|
| NFR-001 | The redirect path shall resolve a short code in under 50 ms at p99. | [ASM-010] |
| NFR-002 | Link creation shall complete in under 200 ms at p99. | [ASM-010] |
| NFR-003 | The service shall sustain 100 requests per second on the resolve path. | [ASM-011] |

**No number appears in the source.** Every figure above is a proposed default from the assumption register, not a stated requirement. `STD-OPS-01` requires a numeric budget at a named percentile for any story touching a request path, so numbers must exist — these are placeholders until confirmed.

---

## 2. Availability

**Applies, at prototype level.**

| ID | Requirement | Rests on |
|---|---|---|
| NFR-004 | The service shall expose a health endpoint reporting its readiness. | [ASM-003] |
| NFR-005 | A failure in the analytics path shall not fail, block, or measurably slow a redirect. | [ASM-002], [ASM-003] |

NFR-005 is not merely a preference: `STD-OPS-04` states that a failure in a non-critical dependency must not degrade the critical path, at severity `critical`. Analytics on the redirect path is the textbook instance.

**No uptime target is stated and none is proposed.** The source specifies no SLA and the deployment target is local only ([ASM-012]), so an availability percentage would be an invented number with nothing to measure it against.

---

## 3. Security

**Applies, and this is the sharpest non-functional surface in the system.**

| ID | Requirement | Rests on |
|---|---|---|
| NFR-006 | A user-supplied destination URL shall be validated against a permitted scheme and host policy before any redirect is issued. | `STD-SEC-05` |
| NFR-007 | Every value arriving from outside the process shall be validated for type, range, length and format at the trust boundary. | `STD-SEC-02` |
| NFR-008 | No property holding a credential, token or key shall be serializable or exposed on a public surface. | `STD-SEC-03` |
| NFR-009 | Each identified threat shall have an automated test asserting the attack fails. | `STD-SEC-06`, REQ-052 |

**NFR-006 is the defining security requirement of a URL shortener.** An unvalidated destination makes this service a credible-looking hop for someone else's phishing, and a server-side fetch of one makes it a proxy into whatever network it runs in. Both are the system working exactly as written.

**Authorization is unresolved and escalated.** The source states no authentication or authorization model; `STD-SEC-01` requires one at story level, at severity `critical`. See the ESCALATE candidate in [`assumptions.md`](assumptions.md#escalate-candidate) under ASM-007. **No NFR is minted here, because doing so would resolve a conflict this skill is required to surface and stop on.**

---

## 4. Privacy

**Applies conditionally — and which way it resolves is the single highest-leverage unanswered question in this baseline.**

| ID | Requirement | Rests on |
|---|---|---|
| NFR-010 | Analytics shall not store any value attributable to an individual visitor — no IP address, user agent, referrer, or session identifier. | [ASM-017] |
| NFR-011 | Every persisted data class shall state its retention period and the mechanism that enforces it. | `STD-DATA-02`, [ASM-013] |

**If [ASM-017] is answered "no visitor data", privacy is a two-line obligation.** If it is answered "yes", this system begins processing personal data and pulls in `STD-SEC-04` (no user identifiers in log arguments), `STD-DATA-03` (derived identifiers inherit the retention cap of what they derive from), and a materially larger surface.

The source says nothing either way. Answering it by building is how the second outcome happens without anyone choosing it.

---

## 5. Operability

**Applies.** §4.4 requires quality gates; §4.6 requires validation guardrails; §4.8 requires stated limitations.

| ID | Requirement | Rests on |
|---|---|---|
| NFR-012 | Every new behaviour and failure path shall have a structured log event, a metric, and an alert condition defined as part of its acceptance criteria. | `STD-OPS-02` |
| NFR-013 | Every change shall state its rollback path, naming the mechanism. | `STD-OPS-03` |
| NFR-014 | Every outbound call shall declare an explicit timeout and, where retried, a bounded retry count with backoff. | `STD-OPS-06` |
| NFR-015 | Setup instructions shall be executable from a clean state without undocumented steps. | REQ-079, REQ-073 |

NFR-015 is testable by execution rather than review, which is the only way a setup document is ever proven — an author who already has the environment cannot see the missing step.

---

## 6. Cost

**Not applicable — with a reason.**

The deployment target is local only ([ASM-012]): no hosted infrastructure, no metered service, no per-request billing, and no storage charged by volume. There is no cost dimension to constrain, so a cost NFR would be a heading with nothing under it.

**This is recorded rather than omitted so that the reasoning is visible and can be overturned.** If [ASM-012] is answered differently — if anything is deployed to a hosted environment — this category reopens immediately, and a per-request cost target and a storage-growth bound both become real. The dependency is stated here so that reopening is a lookup rather than a rediscovery.

---

## Category summary

| Category | Status | NFRs |
|---|---|---|
| Performance | applies | NFR-001…NFR-003 |
| Availability | applies | NFR-004…NFR-005 |
| Security | applies | NFR-006…NFR-009 (+ 1 escalated, not minted) |
| Privacy | applies conditionally | NFR-010…NFR-011 |
| Operability | applies | NFR-012…NFR-015 |
| Cost | **not applicable — local deployment only, [ASM-012]** | — |

**15 NFRs. Nine of them rest on an assumption rather than a stated requirement**, and three of those assumptions carry `low` confidence. That is the honest state of a source document which specifies process in detail and product properties barely at all.
