# Architecture reference — the map

Read this first. It says what each document covers and how they are meant to be used.

These documents are **reference, not narrative**. `architecture-advisor` quotes from them verbatim into sessions that cannot see the rest of the file, so every numbered rule is written to survive being read alone.

---

## 1. The documents

### 1.1 What each file covers

- **[`layers.md`](layers.md)** — the layer model, which type belongs in which layer, which project references are permitted, and where repository interfaces and DTOs must live.
- **[`api.md`](api.md)** — endpoint shape, versioning, the error contract, and the rule that the browser never calls the domain API directly.
- **[`data.md`](data.md)** — the persistence model, data ownership, and the migration approach.
- **[`frontend.md`](frontend.md)** — the backend-for-frontend pattern, the server/client boundary, and what must not leak into components.

### 1.2 What this system is

This repository implements a URL shortener: a caller submits a long destination URL and receives a short code; a visitor requesting that code is redirected to the destination. Short codes are treated as unguessable identifiers, not as sequential keys.

### 1.3 Status of these documents

No application code exists in `src/` yet. These documents are prescriptive — they describe the structure code must be built into, not a structure observed from existing code. Where a rule here proves wrong once code exists, the correction is an `ADR-nnn` in `decisions/` plus an edit to the relevant file under the gate in [`governance/architecture-docs-edit-gate.md`](../governance/architecture-docs-edit-gate.md), not a local deviation.

---

## 2. How these documents are used

### 2.1 Citation format

Every rule in these documents is numbered. Cite a rule as `<file> §<number>` — for example `layers.md §2.3`. A citation without a section number is not a citation, because it cannot be checked.

### 2.2 The advisor reads these; sessions do not

`architecture-advisor` reads these documents in its own context and returns only the rules that bind a specific task. Calling sessions get the applicable subset, not the documents. Do not read these files into a working session wholesale — that is the cost the advisor exists to avoid.

Ask for the applicable rules with `/architecture-guide "<one sentence describing what you are about to build>"`.

### 2.3 Relationship to the standards packs

These documents describe **this system's structure**. The packs in [`standards/`](../standards/) carry the enforceable engineering rules that review agents cite. Where both speak to the same concern, `standards/architecture.md` states the rule and its severity, and these documents state this system's specific shape. `review-architecture` cites both.

### 2.4 These documents are gated

`architecture/`, `.claude/agents/architecture-advisor.md`, and `.claude/skills/architecture-guide/SKILL.md` are one trust boundary and are gated together. See [`governance/architecture-docs-edit-gate.md`](../governance/architecture-docs-edit-gate.md). An AI session must not modify any of the three without explicit in-conversation approval.
