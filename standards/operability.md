# Standards — Operability

Pack prefix: `STD-OPS`. Version is pinned in [`VERSION`](VERSION).

These rules are the source review agents cite and `/github-issues` validates stories against. A rule that is not in this file cannot be enforced by any part of the system.

---

### STD-OPS-01 — Every story carries a performance budget with a number

**Statement:** A story touching a request path must state a latency or throughput budget as a numeric value at a named percentile.

**Rationale:** "Fast" is unfalsifiable, so it is never violated and never tested. A number is the only form of the requirement that can fail.

**Detection hint:** A story whose acceptance criteria describe a request, query, page load, or batch operation, containing no numeric threshold with a unit (ms, s, rps) and a percentile or explicit qualifier.

**Applies to:** story

**Severity if violated:** high

---

### STD-OPS-02 — Logs, a metric, and an alert are acceptance criteria

**Statement:** A story introducing a new behaviour or failure path must include acceptance criteria for its structured log events, its metric, and the alert condition — not a follow-up issue for them.

**Rationale:** Observability deferred to a follow-up is observability that arrives after the first incident it was needed for. It is also the first thing cut when the sprint tightens, which is how a system reaches production unmonitored while every individual decision looked reasonable.

**Detection hint:** A story adding an endpoint, background job, integration, or failure path whose acceptance criteria mention no log event, no metric name, and no alert threshold. Also: any issue body deferring these to "follow-up", "later", or "phase 2".

**Applies to:** story

**Severity if violated:** high

---

### STD-OPS-03 — Every change states its rollback path

**Statement:** Every change must state how it is undone in production, and the statement must name the mechanism — revert, flag flip, or documented reverse migration.

**Rationale:** The rollback path is decided under time pressure during an incident unless it was decided calmly beforehand. Writing it down is what converts an outage into a two-minute reversal.

**Detection hint:** A handover or issue with no Rollback section, or one whose content is "revert the commit" for a change including a data migration, a flag default change, or a contract change — cases where reverting code alone does not restore the prior state.

**Applies to:** both

**Severity if violated:** high

---

### STD-OPS-04 — A non-critical failure must not degrade the critical path

**Statement:** A failure or timeout in an analytics, notification, logging, or other non-critical dependency must not fail, block, or measurably slow the primary user operation.

**Rationale:** The most common cause of a critical outage is a non-critical dependency invoked synchronously. The dependency was correctly judged unimportant; the call site was not.

**Detection hint:** An awaited call to a non-critical service inside a request handler with no timeout, no fallback, and no catch that continues; or a critical-path method whose failure mode is a rethrow from such a call.

**Applies to:** code

**Severity if violated:** critical

---

### STD-OPS-05 — Behaviour changes to an existing endpoint go behind a feature flag

**Statement:** A change to the observable behaviour of an endpoint that already has consumers must ship behind a feature flag with a stated default and a removal plan.

**Rationale:** A flag converts a deploy-time risk into a runtime switch, which is the difference between a rollback that takes a pipeline run and one that takes a second. The removal plan is what stops the flag becoming permanent configuration nobody understands.

**Detection hint:** A diff altering the response shape, status code, ordering, or side effects of an existing route, with no flag check on the changed path. In story text: "change the ... endpoint so that", "now returns", "no longer".

**Applies to:** both

**Severity if violated:** high

---

### STD-OPS-06 — Outbound calls declare a timeout and a bounded retry

**Statement:** Every call leaving the process must specify an explicit timeout and, where retried, a bounded retry count with backoff.

**Rationale:** A default timeout is usually minutes or infinite, so one slow dependency consumes the whole connection pool. An unbounded retry converts a dependency's bad minute into a self-inflicted denial of service against it.

**Detection hint:** An HTTP client, database command, or message publish constructed without a timeout value, or a retry policy with no maximum attempt count or no backoff between attempts.

**Applies to:** code

**Severity if violated:** high
