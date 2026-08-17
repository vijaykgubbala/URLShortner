# Architecture — Layers

Reference. Every rule is numbered and written to be quoted alone.

---

## 1. The layer model

### 1.1 The four layers

This system has four layers, listed from the centre outward: **Domain**, **Application**, **Infrastructure**, **Entrypoints**. Entrypoints comprises the domain API and the backend-for-frontend; both are outermost and neither is closer to the domain than the other.

### 1.2 Domain layer

The Domain layer holds entities, value objects, domain events, domain services, and the interfaces those need. It expresses what is true about short links regardless of how they are stored or requested.

### 1.3 Application layer

The Application layer holds use-case handlers that orchestrate domain objects, plus the interfaces describing what the use case needs from the outside world. It contains sequencing and coordination, not business rules.

### 1.4 Infrastructure layer

The Infrastructure layer holds implementations of interfaces declared by Domain or Application: persistence, clock, identifier generation, outbound HTTP, messaging. It contains no business rule and no use-case sequencing.

### 1.5 Entrypoints layer

The Entrypoints layer holds the domain API and the backend-for-frontend. It translates transport concerns into use-case invocations and back. It contains no business rule.

---

## 2. Permitted references

### 2.1 Dependency direction

A project may reference only projects closer to the domain than itself. Domain references no other project in this system. Application references Domain. Infrastructure references Application and Domain. Entrypoints reference Application and Domain.

### 2.2 Infrastructure is not referenced at compile time

No project may hold a compile-time reference to Infrastructure except the composition root. Infrastructure implementations are bound to their interfaces at startup by dependency injection.

### 2.3 The composition root is the single exception

Exactly one file — the application's startup or program entry — may reference every layer, for the sole purpose of registering implementations against interfaces. No logic other than registration belongs in it.

### 2.4 Entrypoints do not reference each other

The domain API and the backend-for-frontend must not reference each other's projects. Shared contracts are duplicated deliberately or extracted into a contracts project referenced by both.

### 2.5 Test projects

A test project may reference any layer it tests. A test project must never be referenced by a production project.

---

## 3. Type placement

### 3.1 Entities and value objects

Entities and value objects belong in Domain. A short link, its code, its destination, and its expiry are Domain types.

### 3.2 Business rules and invariants

A rule that constrains what is valid about domain state — expiry, uniqueness, code format, permitted destination — is implemented in Domain, on the entity or in a domain service. It must not be implemented only in a handler, controller, or view model.

### 3.3 Use-case handlers

A type that coordinates a single user-facing operation — create a short link, resolve a code, expire a link — belongs in Application, one type per use case.

### 3.4 Persistence types

Types that exist because of the storage technology — configuration, mappings, migrations, provider-specific query code — belong in Infrastructure. They must not appear in Domain or Application.

### 3.5 Transport types

Request and response models, route definitions, filters, and middleware belong in Entrypoints. They must not appear in Domain or Application.

### 3.6 Time and randomness

Current time and random or generated values must be obtained through an interface declared in Domain or Application and implemented in Infrastructure. Direct calls to a system clock or random source outside Infrastructure are prohibited, because they make domain behaviour untestable.

---

## 4. Repository interfaces

### 4.1 Where the interface lives

A repository interface is declared in the layer that consumes it — Domain when a domain service needs it, Application when a use case needs it. It is never declared in Infrastructure.

### 4.2 Where the implementation lives

A repository implementation lives in Infrastructure and is the only place storage-technology types appear.

### 4.3 What a repository interface may expose

A repository interface is expressed in domain terms and returns domain types. It must not expose queryable abstractions, storage-technology types, or transaction handles to its callers, because doing so moves query construction into a layer that cannot be tested without the storage engine.

### 4.4 Method shape

Every repository method that performs input or output is asynchronous and accepts a cancellation token as its final parameter.

---

## 5. DTOs

### 5.1 Request and response DTOs

DTOs describing an HTTP request or response belong in the Entrypoints project that serves them. They must not be referenced by Domain or Application.

### 5.2 Use-case input and output

An Application use case takes and returns its own input and output types, declared in Application. It must not take or return an Entrypoints request or response DTO, because that couples the use case to one transport.

### 5.3 Mapping direction

Mapping between transport DTOs and use-case types happens in Entrypoints. Mapping between use-case types and domain types happens in Application. Domain types are never mapped in Infrastructure except to storage representations.

### 5.4 Domain types do not leave the domain

A domain entity must not be serialized directly to an HTTP response. Responses are built from response DTOs, so that a change to internal domain shape is not automatically a change to a public contract.

---

## 6. Cross-cutting

### 6.1 Configuration

Configuration values are read in the composition root and passed to the types that need them as typed options. Reading configuration directly from Domain, Application, or Infrastructure code is prohibited.

### 6.2 Logging

Logging is permitted in every layer through an abstraction. Log statements must not receive domain personal data or user identifiers as arguments; see `standards/security.md` `STD-SEC-04`.

### 6.3 Exceptions crossing layers

A layer must not let an exception type from a lower-level concern escape unchanged. Infrastructure translates storage exceptions into the failure types its interface declares; Entrypoints translate domain and application failures into the error contract in `api.md`.
