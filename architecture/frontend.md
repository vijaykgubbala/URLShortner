# Architecture — Frontend

Reference. Every rule is numbered and written to be quoted alone.

---

## 1. The backend-for-frontend

### 1.1 What the BFF is

The backend-for-frontend is a server-side layer that exists to serve this application's user interface and nothing else. It aggregates and reshapes domain API responses into the exact shape one screen needs.

### 1.2 The BFF is the browser's only backend

The browser calls the BFF and nothing else. The domain API, the database, and any third-party service are reached only through the BFF; see `api.md` §1.2.

### 1.3 The BFF does not enforce domain invariants

The BFF may reject a malformed request and may check that a caller is authenticated, but it must not be the only place a business rule is enforced. Every rule the BFF applies exists and is tested in the Domain layer.

### 1.4 Why the rule exists

A BFF serves one client. A rule enforced only there protects only that client, which is indistinguishable from no rule the moment a second client — a mobile app, a script, a partner integration — calls the domain API.

### 1.5 The BFF holds no persistent state

The BFF owns no database and no long-lived store. Caching is permitted, is explicitly time-bounded, and is rebuildable from the domain API.

### 1.6 One BFF endpoint per screen concern

BFF endpoints are shaped for the view that consumes them and may change whenever that view changes. They are not a general-purpose API and carry no stability guarantee to other consumers.

---

## 2. Server and client boundaries

### 2.1 Default to server

Rendering and data fetching happen on the server by default. Client-side execution is used only for behaviour that genuinely requires the browser: user input handling, local interactivity, and browser APIs.

### 2.2 Secrets are server-only

API keys, tokens, service credentials, and internal host names exist only in server-side code and server-side configuration. A secret placed in client-reachable configuration is public regardless of how the page is served.

### 2.3 Data fetching is server-side

Calls to the domain API are made from BFF server code. Client components do not call the domain API, and do not receive its base URL.

### 2.4 The network boundary is a trust boundary

Every value arriving from the browser is untrusted, including values the client received from the server moments earlier. Validate at the BFF entry point; see `standards/security.md` `STD-SEC-02`.

### 2.5 Authorization is decided server-side

Whether a caller may perform an action is decided in server-side code. Hiding a control in the interface is a usability affordance, never an access control.

---

## 3. What must not leak into components

### 3.1 Domain entities

A domain entity must not be passed to a component. Components receive view models shaped for display, built in the BFF.

### 3.2 Domain API response shapes

A raw domain API response must not be forwarded to a component unchanged. Doing so couples the interface to a contract it does not own, so a domain API change becomes a UI change without anyone choosing that.

### 3.3 Persistence concerns

Database identifiers, row versions, storage-technology types, and pagination cursors expressed in storage terms must not reach a component.

### 3.4 Internal identifiers

Surrogate keys and internal identifiers must not be rendered, embedded in markup, or placed in a data attribute. Components receive public identifiers only.

### 3.5 Error internals

A component receives the `title` and, where useful, the `type` from the error contract in `api.md` §4.1. Stack traces, exception type names, and `detail` strings containing internal information must not reach the browser.

### 3.6 Business rules

A component must not implement a business rule. Displaying that a link has expired is presentation; deciding that it has expired is domain.

---

## 4. Composition

### 4.1 View models are explicit

Every server-to-client data shape is a named view model type. Passing an untyped or pass-through object to a component is prohibited, because it makes §3.1 to §3.5 unenforceable by inspection.

### 4.2 Components are presentational by default

A component receives what it displays. Fetching, aggregating, and deciding happen above it.

### 4.3 Loading and failure states are part of the component contract

Every component that displays fetched data defines its loading state and its failure state. A component that assumes success is incomplete.
