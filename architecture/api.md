# Architecture — API

Reference. Every rule is numbered and written to be quoted alone.

Covers the domain API surface. The backend-for-frontend is covered in [`frontend.md`](frontend.md).

---

## 1. The two API surfaces

### 1.1 The domain API

The domain API is the service-to-service surface. It exposes use cases over HTTP and is consumed by the backend-for-frontend and by any other trusted server-side caller.

### 1.2 The browser never calls the domain API directly

No browser-originated request may reach the domain API. The browser calls only the backend-for-frontend, which calls the domain API server-side. Any code path that would issue a domain API request from client-side JavaScript is a violation, regardless of how the URL is constructed.

### 1.3 Why the rule exists

The domain API's authorization model assumes a trusted server-side caller. Exposing it to the browser puts a surface designed for one trust level at another, and does so silently — the endpoint works, so nothing appears wrong until it is enumerated.

### 1.4 The redirect endpoint is the exception, and is not the domain API

Public short-code resolution — the request a visitor's browser makes when following a short link — is served by a dedicated public redirect endpoint, not by the domain API. That endpoint performs resolution and redirect only, and exposes no other operation.

---

## 2. Endpoint shape

### 2.1 Version prefix

Every route begins with a version segment, `/v1/`, `/v2/`, and so on. A route with no version prefix must not be added.

### 2.2 Path casing and pluralisation

Resource path segments are kebab-case plural nouns: `/v1/short-links`. Singular resource segments and camelCase or snake_case segments must not be used.

### 2.3 No verbs in paths

The HTTP method is the verb. Path segments such as `create`, `get`, `update`, `delete`, `fetch`, `process`, or `do` must not appear in a route.

### 2.4 Resource identity

A single resource is addressed as `/v1/<resource>/{id}`. The identifier in the path is the resource's public identifier — for a short link, its code — never a database surrogate key.

### 2.5 Method semantics

`GET` is safe and has no side effects. `POST` creates. `PUT` replaces a whole resource. `PATCH` applies a partial change. `DELETE` removes. An operation that does not fit these is modelled as a resource, not as a verb path.

### 2.6 Status codes

`200` for a successful read or replace, `201` with a `Location` header for a successful create, `204` for a successful delete, `400` for a malformed request, `401` for a missing or invalid credential, `403` for an authenticated caller without permission, `404` for an absent resource, `409` for a conflict with current state, `422` for a well-formed request that violates a domain rule.

**Exception — a resource whose authorization is a capability token.** Where possession of a secret is itself the authorization and there is no authenticated principal, an unknown identifier, a wrong credential and a missing credential all return an identical `404` with an identical body. Distinguishing them tells a caller which identifiers exist, which returns the guessability that an unguessable identifier was generated to remove. RFC 9110 §15.5.4 permits this — *"An origin server that wishes to 'hide' the current existence of a forbidden target resource MAY instead respond with a status code of 404 (Not Found)"* — and §15.5.5 defines `404` as covering a server that "is not willing to disclose" the resource exists.

This exception carries two obligations, because a `404` is not a drop-in replacement for a `403`. Every such `404` must set `Cache-Control: no-store`, since `404` is heuristically cacheable and `403` is not, and an intermediary may otherwise serve a cached authorization failure to the legitimate holder. And the verification path must perform the same work whether or not the resource exists — a response that returns early when it does not exist leaks through timing what the status code conceals. See [`ADR-002`](../decisions/ADR-002-uniform-404-for-capability-token-failures.md).

### 2.7 Collection responses

Every endpoint returning a collection is paginated with an enforced maximum page size and returns the page size and a continuation mechanism in the response body. An unbounded collection endpoint must not be added.

---

## 3. Versioning

### 3.1 Additive change within a version

Adding an optional request field or a new response field is permitted within an existing version. Existing consumers must continue to work unchanged.

### 3.2 Breaking change requires a new version

Removing or renaming a field, narrowing a type, making an optional field required, or changing the meaning of an existing field requires a new version prefix. Such a change must not be made in place.

### 3.3 Concurrent versions

When two versions are live, both are served from the same deployment and share the Application layer. Version-specific behaviour lives in Entrypoints mapping, not in Application or Domain.

### 3.4 Retirement

A retired version returns `410 Gone` for a stated period before its routes are removed, rather than being deleted outright.

---

## 4. Error contract

### 4.1 One error body shape

Every error response body uses the same shape across all endpoints and versions:

```json
{
  "type": "short-link-expired",
  "title": "The short link has expired.",
  "status": 422,
  "detail": "Short link 'a1b2c3' expired on 2026-07-01.",
  "traceId": "0af7651916cd43dd8448eb211c80319c"
}
```

### 4.2 Field meanings

`type` is a stable, kebab-case, machine-readable code that must not change once published. `title` is a short human-readable summary safe to show a user. `status` matches the HTTP status code. `detail` is specific to this occurrence. `traceId` correlates the response with logs.

### 4.3 Errors do not leak internals

An error body must not contain a stack trace, an exception type name, a SQL fragment, a file path, a connection string, or an internal host name. `detail` describes the failure in domain terms only.

### 4.4 Validation failures

A request failing input validation returns `400` with one entry per invalid field, each naming the field and the reason. Validation failures are reported together, not one at a time.

### 4.5 Domain rule failures

A well-formed request that violates a domain rule returns `422`, not `400`. The distinction is what tells a caller whether to fix the request or to stop retrying it.

### 4.6 Every error carries a trace identifier

Every error response includes `traceId`, and that identifier appears in the corresponding log entry. An error response with no trace identifier is not diagnosable in production.

---

## 5. Contract documentation

### 5.1 OpenAPI is generated

The OpenAPI document is generated from route metadata and DTO types at build time. It must not be hand-edited or committed as a maintained artefact.

### 5.2 The generated document is the contract

Where the generated OpenAPI document and any written description disagree, the generated document is correct and the description is a defect.
