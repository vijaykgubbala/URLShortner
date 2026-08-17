# Architecture — Data

Reference. Every rule is numbered and written to be quoted alone.

---

## 1. Persistence model

### 1.1 Storage engine

Persistent state is held in a relational database. Relational storage is the default; any other storage technology requires an `ADR-nnn` in `decisions/` before it is introduced.

### 1.2 The short link record

A short link is stored as one row carrying: its code, its destination URL, its creation timestamp, its expiry timestamp where one applies, and its owner identifier where one applies. The code is the row's public identity.

### 1.3 Surrogate keys are internal

A surrogate primary key, where one exists, is never exposed in an API response, a URL, or a log. The public identifier is the short code; see `api.md` §2.4.

### 1.4 Short codes are unguessable

A short code is generated from a cryptographically secure random source, not from a sequence, a counter, an incrementing key, or a hash of the destination. Sequential or derivable codes make the entire link set enumerable.

### 1.5 Uniqueness is enforced by the database

Short-code uniqueness is enforced by a unique constraint in the database, not only by a check-then-insert in application code. A check-then-insert does not survive two concurrent requests.

### 1.6 Timestamps

Every stored timestamp is UTC and is stored in a type that carries no ambiguity about offset. Local times are converted at the boundary, never stored.

---

## 2. Ownership

### 2.1 This system owns its own data

This system owns the short-link records and nothing else. It is the only writer to those tables.

### 2.2 Store the identifier, not a copy

Data belonging to another system — a user's name, email, or profile — is referenced by identifier and fetched when needed. It must not be copied into this system's tables except as an explicitly modelled cache with an expiry.

### 2.3 No shared database access

No other system reads or writes this system's tables directly. Access is through this system's API. A second writer to a table makes every invariant in this repository unenforceable.

### 2.4 Read models

A denormalised read model is permitted where a query needs it, provided it is rebuildable from the owning tables and is never the system of record.

---

## 3. Access

### 3.1 All access is through repositories

Database access happens only in repository implementations in Infrastructure. Query construction in Application, Domain, or Entrypoints is prohibited; see `layers.md` §4.3.

### 3.2 Reads are bounded

Every query returning a collection applies a row limit in the query itself. Fetching a full set and discarding rows afterwards is prohibited.

### 3.3 Indexes for query paths

Every query used on a request path is served by an index. Adding a query pattern without the index that serves it is an incomplete change.

### 3.4 Transactions

A single use case that writes more than one row does so in one transaction, opened and committed in Infrastructure. Transaction handles are not passed into Application or Domain.

---

## 4. Migrations

### 4.1 Schema changes are migrations

Every schema change ships as a versioned migration in source control. Manual schema modification against any deployed environment is prohibited.

### 4.2 Migrations are additive

A migration must be safe to apply while the previous version of the application is still running. Adding a nullable column, a table, or an index is safe. Dropping or renaming an in-use column, narrowing a type, or adding a non-nullable column without a default is not.

### 4.3 Destructive change is a separate release

Removing a column or table happens in a later release than the one that stops using it, after the previous application version is no longer running anywhere.

### 4.4 Every migration has a documented reverse

Every migration ships with a reverse step or, where a true reverse is impossible, a documented recovery procedure stating what is lost. "Restore from backup" is a recovery procedure only when the backup interval is stated.

### 4.5 Data migrations state their backfill

A migration that populates or reinterprets existing rows documents how the backfill runs, how long it is expected to take, and whether it is re-runnable. A backfill that cannot be safely re-run must say so.

### 4.6 Migrations run before the new version serves traffic

Migrations are applied as a deployment step that completes before the new application version begins serving requests. Applying migrations from application startup code is prohibited, because it couples schema state to instance scaling.

---

## 5. Retention

### 5.1 Every stored data class has a stated retention

Every table and every persisted event class states how long its rows are kept and what removes them. Data with no stated retention is kept forever by default, which is a decision nobody made.

### 5.2 Expired links are removed, not left

Short links past their expiry are removed or tombstoned by a scheduled process. An expiry that is enforced only at read time leaves the data in place indefinitely.

### 5.3 Derived identifiers inherit retention

A hash, fingerprint, or surrogate derived from personal data carries the same retention cap as the data it derives from; see `standards/data.md` `STD-DATA-03`.
