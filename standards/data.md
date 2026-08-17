# Standards — Data

Pack prefix: `STD-DATA`. Version is pinned in [`VERSION`](VERSION).

These rules are the source review agents cite and `/github-issues` validates stories against. A rule that is not in this file cannot be enforced by any part of the system.

---

### STD-DATA-01 — Schema changes are additive and reversible

**Statement:** A schema change must be additive — new nullable column, new table, new index — and must not drop or rename an in-use column in the same release that stops writing to it.

**Rationale:** During a rolling deploy both versions of the application run against one schema. A destructive change breaks the version that has not been replaced yet, which is the version currently serving traffic.

**Detection hint:** A migration containing `DROP COLUMN`, `DROP TABLE`, `RENAME`, a type narrowing, or a `NOT NULL` added without a default, in the same change that alters the code writing to it.

**Applies to:** code

**Severity if violated:** critical

---

### STD-DATA-02 — Retention is stated for every new data class

**Statement:** A story collecting or persisting a class of data not already stored must state how long it is kept and what removes it.

**Rationale:** Data with no stated retention is kept forever by default, because deletion is the only operation nobody is ever assigned. The obligation is created at collection time and can only be answered then.

**Detection hint:** A story introducing a new table, column, event stream, log class, or cached record, whose acceptance criteria state no retention period and name no expiry or purge mechanism.

**Applies to:** story

**Severity if violated:** high

---

### STD-DATA-03 — Derived identifiers linked to a person carry a retention cap

**Statement:** Any hash, token, fingerprint, or surrogate key that can be linked back to a person must carry the same retention cap as the personal data it derives from.

**Rationale:** A derived identifier feels anonymous and is treated as exempt, but it is only pseudonymous — the link survives, and so does the obligation. Retaining it after deleting the source recreates the record the deletion was meant to remove.

**Detection hint:** A persisted column holding a hash, device fingerprint, session identifier, or surrogate key derived from an email, account, IP address, or user ID, with no expiry column and no purge job covering it.

**Applies to:** both

**Severity if violated:** high

---

### STD-DATA-04 — Migrations ship with a documented backfill and rollback

**Statement:** A migration that changes the meaning or population of existing rows must ship with a documented backfill procedure and a documented reverse step.

**Rationale:** The forward migration is written while the change is understood; the reverse is needed when it is not, by someone else, at speed. A migration with no reverse step is a one-way door disguised as a routine change.

**Detection hint:** A migration file with data manipulation — `UPDATE`, `INSERT ... SELECT`, a computed column populated from existing rows — and no corresponding down step or documented reverse procedure in the handover.

**Applies to:** both

**Severity if violated:** critical

---

### STD-DATA-05 — Store the identifier, not a copy of another owner's data

**Statement:** Data owned by another bounded context or service must be referenced by identifier and fetched, not copied into this system's tables, except as an explicitly time-bounded cache.

**Rationale:** A copy is correct at the instant it is made and silently wrong afterwards, and it duplicates the original owner's retention and correction obligations without duplicating their ability to meet them.

**Detection hint:** A table or entity carrying descriptive fields — name, email, address, status — for a concept this system does not own, alongside that concept's identifier. Permitted only where the column set is explicitly modelled as a cache with an expiry.

**Applies to:** both

**Severity if violated:** high

---

### STD-DATA-06 — Collection reads are bounded

**Statement:** Every query returning a collection must impose a maximum row count at the query, not by discarding rows after they are fetched.

**Rationale:** An unbounded read is correct in development against a small dataset and takes the process down in production against a large one. The failure arrives with growth, which is exactly when it is least affordable.

**Detection hint:** A repository or query method returning a collection type with no limit, take, page-size, or top clause; or a filter, slice, or take applied to the materialized result after the query has executed.

**Applies to:** code

**Severity if violated:** high
