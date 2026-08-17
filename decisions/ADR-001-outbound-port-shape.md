# ADR-001 — Shape of an outbound port, and the DNS resolver as its first instance

**Status:** Accepted
**Date:** 2026-08-17
**Issue:** [#17](https://github.com/vijaykgubbala/URLShortner/issues/17)
**Required by:** `STD-ARCH-06` — *"Introducing a transport, persistence technology, messaging pattern, or third-party integration style not already used in this repo requires an accepted `ADR-nnn` in `decisions/` before the code merges."*

---

## Context

The destination URL policy (#17) needs to know what addresses a hostname resolves to. That is the repository's **first outbound integration** — nothing in `src/` currently calls anything outside the process.

`architecture-advisor` reported a genuine silence when asked about this work:

> *"There is no rule in `architecture/` governing the shape of a general outbound port such as a DNS resolver — where its interface is declared, or whether it must be asynchronous and take a cancellation token. I am not supplying one."*
>
> *"`layers.md` §4.4 exists but is scoped to repository methods, so I have not returned it as binding. If you want it to bind, that is an ADR plus an edit, not a local reading."*

So two things need deciding: the general shape of an outbound port, and the specific DNS resolver.

`architecture/layers.md` does bind the parts it covers:

- **§2.1** — *"Domain references no other project in this system."*
- **§1.3** — *"The Application layer holds use-case handlers that orchestrate domain objects, plus the interfaces describing what the use case needs from the outside world."*
- **§1.4** — *"The Infrastructure layer holds implementations of interfaces declared by Domain or Application… It contains no business rule and no use-case sequencing."*
- **§6.3** — *"Infrastructure translates storage exceptions into the failure types its interface declares."*

---

## Decision

### 1. An outbound port's interface is declared in Application

Per §1.3, the interface describing what a use case needs from the outside world belongs in Application. This ADR extends that to outbound ports generally, not only repositories.

### 2. The types the port returns are declared in Domain when the Domain reasons over them

This is the constraint §2.1 forces. If a Domain predicate takes the port's result as a parameter, that result type cannot live in Application — Domain may reference nothing.

So: **the port interface is in Application; the type it returns is in Domain.** Application references Domain, which §2.1 permits.

For the DNS resolver specifically, `HostResolution` — carrying `Resolved` with addresses, `NotFound`, or `Failed` — is a **Domain** type. It expresses what the domain needs to know in order to judge a destination, which is a domain concern regardless of how it is obtained.

### 3. An outbound port is asynchronous and takes a `CancellationToken` as its final parameter

`layers.md` §4.4 states this for repository methods. This ADR adopts the same shape for outbound ports, for the same reason: a call leaving the process can block, and a caller that cannot cancel it cannot shed load.

### 4. An outbound port never throws for an expected failure

Per §6.3, Infrastructure translates lower-level exceptions into the failure types its interface declares. A DNS timeout, `SERVFAIL`, or `NXDOMAIN` are **expected** outcomes of resolving an arbitrary user-supplied hostname, not exceptional ones. They are returned as `HostResolution.NotFound` or `HostResolution.Failed`.

This matters beyond tidiness: if the adapter threw, the Application handler would need a `catch` that decides whether a caught exception means reject — and deciding that is a business rule, which §1.3 forbids in Application.

### 5. Every outbound call carries an explicit timeout

`STD-OPS-06` — *"Every call leaving the process must specify an explicit timeout."* The DNS resolver's timeout is configuration-supplied with a default of **2 seconds**, chosen so that a resolver outage cannot approach #18's 200 ms p99 create budget for long before the request is abandoned.

**No retry.** A retry on the create path multiplies the latency cost of an unreachable resolver, and the operation is user-initiated and safely repeatable by the caller.

### 6. The adapter returns facts, never judgments

Per §1.4 the adapter contains no business rule. It returns **every** resolved address, unfiltered and unranked. It does not drop loopback addresses, does not prefer IPv4, and does not truncate the list.

---

## Consequences

**The Domain predicate stays pure and synchronous.** It receives a `HostResolution` and returns a verdict. No I/O, no async, testable with no fake resolver.

**`NotFound` and `Failed` are decided by Domain.** The Application handler passes the resolution through without inspecting it. This is what advisor flag 1 required — the accept/reject meaning of a failed resolution is a rule about a permitted destination, so it belongs in Domain.

**Fail closed is a Domain rule, not an adapter behaviour.** `HostResolution.Failed` rejects, and it rejects because the Domain predicate says so, where it can be unit-tested without a network.

**This ADR does not amend `architecture/`.** It records a decision for a silence. If the outbound-port shape should bind future work as a rule rather than a precedent, that is an edit to `architecture/layers.md` — a gated path requiring explicit approval — and is left as a candidate for `/workflow-compound`'s graduation question.

---

## Alternatives considered

**Declare the port interface in Domain.** Clean for §2.1 and it is what §4.1 does for repositories consumed by a domain service. Rejected because §1.3 assigns outbound interfaces to Application explicitly, and the Domain predicate here consumes a *result*, not the port.

**Return an address list, empty on failure.** Simpler signature. Rejected: the Application layer would then have to distinguish "no addresses" from "resolution failed", and that distinction is a rule about a permitted destination — advisor flag 1, §1.3.

**Throw on resolution failure.** Idiomatic .NET. Rejected under §6.3 and for the reason in decision 4 — the `catch` block becomes the business rule.

**Retry on timeout.** Rejected: multiplies the latency cost of an unreachable resolver against a 200 ms p99 budget, for an operation the caller can safely repeat.
