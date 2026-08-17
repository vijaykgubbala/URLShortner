# Standards — Architecture

Pack prefix: `STD-ARCH`. Version is pinned in [`VERSION`](VERSION).

These rules are the source `review-architecture` cites and `/github-issues` validates stories against. A rule that is not in this file cannot be enforced by any part of the system.

---

### STD-ARCH-01 — Dependency direction points inward

**Statement:** A project may reference only projects in the same layer or in a layer closer to the domain; the domain layer references no other layer in this system.

**Rationale:** The dependency graph is the only thing preventing the domain from being reshaped by the concerns of whatever calls it. Once an inward layer references an outward one, the two are one layer and the boundary is decorative.

**Detection hint:** A project reference, import, or `using` in a domain or application file that names an API, web, infrastructure, or persistence-implementation module. In story text: a criterion describing domain behaviour in terms of HTTP status codes, request objects, or database tables.

**Applies to:** both

**Severity if violated:** critical

---

### STD-ARCH-02 — Domain rules live in the domain layer

**Statement:** Business rules, invariants, and validation of domain meaning must be implemented in the domain layer and never in a controller, endpoint handler, or view model.

**Rationale:** A rule implemented in the API layer is enforced only for callers that arrive through that endpoint, so the second entry point — a background job, a second endpoint, a migration — silently skips it.

**Detection hint:** A conditional in a controller or endpoint handler that tests domain state rather than request shape — expiry checks, quota checks, state-machine transitions, uniqueness rules. Distinguish from permitted request-shape validation (null, length, format, required field).

**Applies to:** code

**Severity if violated:** critical

---

### STD-ARCH-03 — REST paths are versioned, kebab-case, plural nouns

**Statement:** Every HTTP route must begin with a version prefix and use kebab-case plural nouns, with no verbs in the path.

**Rationale:** The verb is the HTTP method; repeating it in the path produces two ways to express the same operation and no way to tell which is canonical. Without a version prefix there is no way to change a contract except by breaking it.

**Detection hint:** A route template that lacks a leading `/v<n>/` segment, contains an uppercase letter or underscore, contains a singular resource noun, or contains a verb such as `get`, `create`, `update`, `delete`, `fetch`, `do`, or `process`.

**Applies to:** code

**Severity if violated:** medium

---

### STD-ARCH-04 — OpenAPI is generated from code

**Statement:** The OpenAPI document must be produced from the running application's types and route metadata, and must not be edited by hand.

**Rationale:** A hand-maintained specification is a second source of truth that drifts silently — it is wrong only in the moments that matter, when someone integrates against it.

**Detection hint:** An OpenAPI or Swagger file committed under source control with no generation step in the build, or a diff that edits such a file without a corresponding change to a route or DTO.

**Applies to:** code

**Severity if violated:** medium

---

### STD-ARCH-05 — The BFF shapes views and never enforces domain invariants

**Statement:** A backend-for-frontend may aggregate, project, and reshape data for a specific client, but must not be the only place a domain invariant is enforced.

**Rationale:** A BFF exists to serve one consumer. An invariant enforced there protects that one consumer and no other, which is indistinguishable from no invariant at all the moment a second client appears.

**Detection hint:** In a BFF or view-model file: a rejection, throw, or validation failure whose condition tests domain state rather than the shape of the client request. Also: a rule present in the BFF with no equivalent assertion in the domain test suite.

**Applies to:** code

**Severity if violated:** high

---

### STD-ARCH-06 — A new integration pattern requires an ADR

**Statement:** Introducing a transport, persistence technology, messaging pattern, or third-party integration style not already used in this repo requires an accepted `ADR-nnn` in `decisions/` before the code merges.

**Rationale:** Integration patterns are the decisions that are cheapest to make and most expensive to reverse. Recording them at the point of choice is the only moment the alternatives are still known.

**Detection hint:** A new package dependency providing transport, queuing, caching, or storage; a first occurrence of a client type for an external service; a new protocol scheme in configuration. In story text: "integrate with", "publish to", "consume from", "call the ... API".

**Applies to:** both

**Severity if violated:** high
