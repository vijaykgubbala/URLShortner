# Standards — Security

Pack prefix: `STD-SEC`. Version is pinned in [`VERSION`](VERSION).

These rules are the source `review-security` cites and `/github-issues` validates stories against. A rule that is not in this file cannot be enforced by any part of the system.

---

### STD-SEC-01 — Authorization is decided at story level

**Statement:** Every story that reads or changes data must state in its acceptance criteria who is permitted to perform the action and what happens when someone else attempts it.

**Rationale:** Authorization deferred to implementation is authorization decided by whoever is writing the endpoint at the time, under deadline, without the context to decide it. It is a product decision wearing a technical costume.

**Detection hint:** A story whose acceptance criteria describe a read or state change with no criterion naming an actor role and no negative criterion for an unpermitted actor. Trigger words in story text: "user can", "admin can", "view", "edit", "delete", "approve".

**Applies to:** story

**Severity if violated:** critical

---

### STD-SEC-02 — Input is validated at the trust boundary

**Statement:** Every value that originates outside the process must be validated for type, range, length, and format at the point it enters the system, before it reaches any domain or persistence code.

**Rationale:** Validation performed deeper in the stack protects only the paths that happen to route through it. The boundary is the one place every untrusted value provably passes through.

**Detection hint:** A request DTO property, query-string parameter, header, route value, or deserialized message body consumed without a validator, attribute, or explicit guard clause in the same handler. Especially: values passed straight into a repository call, file path, or outbound URL.

**Applies to:** code

**Severity if violated:** critical

---

### STD-SEC-03 — Secrets are not serializable and not debugger-visible

**Statement:** No property holding a credential, token, key, or connection string may be serializable, logged by a default formatter, or exposed on a type's public surface.

**Rationale:** Secrets escape through the paths nobody designed — a debug dump, an error response, a serialized audit record. Making them structurally unable to leave is the only control that survives the code being modified by someone who does not know the rule.

**Detection hint:** A property or field whose name contains `secret`, `token`, `key`, `password`, `credential`, `connectionstring`, or `apikey`, that is public, has a public getter on a serializable type, or appears in a `ToString` override.

**Applies to:** code

**Severity if violated:** critical

---

### STD-SEC-04 — No personal data or user identifiers in log arguments

**Statement:** Log statements must not receive personal data, user identifiers, or full request payloads as arguments; correlate with an opaque request or trace identifier instead.

**Rationale:** Logs are copied, shipped, and retained under a different regime from the database, by systems that were never assessed for holding personal data. Once an identifier is in a log pipeline it is in every downstream copy of it.

**Detection hint:** A logging call whose interpolated or structured arguments include an email, name, address, phone number, raw user ID, full request body, or a variable named `user`, `customer`, `email`, `principal`, or `claims`.

**Applies to:** code

**Severity if violated:** high

---

### STD-SEC-05 — Destination URLs are allowlisted before any redirect

**Statement:** A user-supplied or stored destination URL must be validated against an allowlist of permitted schemes and hosts immediately before issuing a redirect or server-side request.

**Rationale:** An unvalidated destination turns this system into a credible-looking hop for someone else's phishing, and a server-side fetch of one turns it into a proxy into the internal network. Both are the system working exactly as written.

**Detection hint:** A redirect result, `Location` header assignment, or outbound HTTP request constructed from a value that came from a request parameter or a persisted record, with no scheme and host check on the path between them.

**Applies to:** both

**Severity if violated:** critical

---

### STD-SEC-06 — Every identified threat has a negative test

**Statement:** Each threat named in a story, ADR, or review finding must be paired with an automated test asserting that the attack fails.

**Rationale:** A mitigation with no negative test is a claim. The test is what detects the day a refactor removes the check, which is the day the mitigation was actually needed.

**Detection hint:** A story, ADR, or review finding naming a threat — injection, traversal, forgery, replay, enumeration, escalation — with no test whose name or assertion expresses the rejection case. Look for tests asserting a 401, 403, 400, or thrown rejection, not only the happy path.

**Applies to:** both

**Severity if violated:** high
