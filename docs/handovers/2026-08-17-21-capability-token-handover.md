---
issue: 21
branch: feat/21-capability-token
base: main
plan: docs/plans/2026-08-17-21-feature-capability-token-plan.md
impact_analysis: none — greenfield path, /solution-plan ran instead
mode: TDD
adr: decisions/ADR-002-uniform-404-for-capability-token-failures.md (Proposed, not Accepted)
tests: 217 passing (200 before)
---

# Handover — #21 Protect mutations with a capability token

## Release Notes

**Creating a short link now returns a management token, and that token is the only way to
delete the link.**

For anyone creating links: the `POST /v1/short-links` response gains a `managementToken`
field. **It is shown exactly once and cannot be recovered.** If it is lost, the link can no
longer be deleted by anyone — that is by design, not a defect, because only a hash of it is
kept. Tell anyone using the API to store it at creation time.

**Links can now be deleted:** `DELETE /v1/short-links/{code}` with the token in an
`Authorization: Bearer <token>` header. A successful delete returns `204` and the short link
stops resolving immediately.

**What support will see, and it will look wrong:** a delete with a wrong token, a missing
token, or an unknown code all return **`404 Not Found`** — the same response, with the same
body. A caller who mistypes their token is told the link was not found, not that their token
was wrong. This is deliberate. Answering those cases differently would let anyone discover
which short codes exist by trying them, which is the thing unguessable codes exist to
prevent. Every such response carries a `traceId`, and the real reason is in the log under it
— **that identifier is what support should ask for.**

**Links created before this change have no token and therefore cannot be deleted at all.**
See Rollback and What's Not Tested.

Nothing about creating or following a short link changes for a visitor.

---

## Risk Analysis

**Derived here, not carried forward.** There is no impact analysis for this issue — the
greenfield path runs `/solution-plan` instead — so the surface below was worked out from the
brainstorm, the plan and the change itself. A reviewer should treat it as one person's
reading rather than as a prior analysis confirmed.

| Area | Blast Radius | Reviewer Focus | Mitigation |
|---|---|---|---|
| `DeleteShortLink.ExecuteAsync` | **large: the system's first and only authorization decision.** Every future mutation endpoint (#24 update) will copy this shape | **That `verifier.Verify(...)` is called before any branch on `link is null`, and that nothing re-orders it.** This is the whole timing property; it is one line and reads like a redundant call | T-18 with a counting seam. Mutation-proven: the obvious early return fails exactly one test |
| `LinkToken.Verify` | **large: every authorization outcome in the system passes through it** | The `matches && … && Decode(presented) is not null` return. It is deliberately convoluted so no path short-circuits — check it actually fails closed rather than merely looking careful | 11 Domain tests including null, empty, whitespace, truncated, non-base64url, and a wrong-length stored hash |
| `ShortLink.TokenHash` column | **medium: every row, and every existing database** | **Nullable is load-bearing.** A reviewer may reasonably want it `IsRequired()`; that would break `data.md` §4.2 and lock out every pre-existing row differently than intended | `data.md` §4.2 quoted at the property; `Verify` fails closed on null |
| The uniform `404` on delete | **medium: one endpoint now, every authorized endpoint later** | Whether the exception in `api.md` §2.6 is scoped too loosely — it licenses hiding behind `404` wherever "possession of a secret is the authorization" | ADR-002; T-11 asserts indistinguishability |
| `CreateShortLinkResponse` | **medium: a published contract, and the only carrier of a secret** | That `no-store` is set on the 201 and that nothing else ever gains a token field | 4 structural reflection tests, mutation-proven |
| `EfShortLinkRepository.TryDeleteAsync` | small: one method, called from one place | `ExecuteDeleteAsync` bypasses the change tracker. Safe here only because the prior read is `AsNoTracking` — check that assumption still holds if the read changes | Exercised through the endpoint tests; **no direct test** — see gaps |
| `CreateShortLink` constructor | small: one new dependency, but it changed an existing type's shape | Nothing; mechanical | Compiler |

---

## Test Coverage

### Planned vs Actual

18 planned test cases. **All written. Four changed, and the changes matter more than the
count.**

| Planned Test | Status | Notes |
|---|---|---|
| T-01 43-char unpadded base64url | written | `A_token_is_43_unpadded_base64url_characters` |
| T-02 distinct + not sequential | written | Two tests: `A_thousand_draws_are_all_distinct`, `Draws_are_not_ordinally_sequential` |
| T-03 hash determinism | written | Plus `Padding_does_not_change_the_hash` |
| T-04 verify succeeds | written | — |
| T-05 fails on malformed | written | `[Theory]`, 6 inputs, plus wrong-length stored hash |
| T-06 create returns a token | written | — |
| T-07 correct token → 204 | written | Asserts the link stops resolving afterwards |
| T-08 wrong token → 404 | **changed** | **Strengthened.** As planned it passed with no `DELETE` route at all — unrouted paths 404 too. Now asserts the problem body, so the route is load-bearing. This is review finding TST-007 from #19 recurring on a new endpoint |
| T-09 missing header → 404 | **changed** | Same strengthening, same reason |
| T-10 unknown code → 404 | **changed** | Same strengthening, same reason |
| T-11 three responses identical | **changed** | Planned as **byte-identical**, which is impossible: `api.md` §4.6 mandates a per-request `traceId`. The planned test could only have passed by dropping `traceId` — violating §4.6 and destroying ADR-002's stated mitigation. Now asserts identity of `type`/`title`/`status`, byte-identical `detail` for one code under two reasons, and identity modulo the caller's own code across the third |
| T-12 refused delete sets no-store | written | — |
| T-13 create sets no-store | written | — |
| T-14 structural absence proof | written | 4 tests in `ResponseSurfaceTests` |
| T-15 redirect carries no token | written | — |
| T-16 token in no log message | written | Exercises all three delete paths |
| T-17 forged token rejected | written | — |
| T-18 verification runs on not-found | **changed** | Planned as a counting **hasher** fake; `LinkToken` is static with nothing to inject. Delivered as a counting `ILinkTokenVerifier` — a seam added for the sole purpose of making this provable |
| *(unplanned)* `Every_character_is_in_the_base64url_alphabet` | added | |
| *(unplanned)* `The_leading_character_is_not_fixed` | added | Catches a generator seeded once per process |
| *(unplanned)* `A_hash_is_32_bytes` | added | |
| *(unplanned)* `Hashing_a_malformed_token_throws` | added | Written after a real defect — see below |
| *(unplanned)* `A_token_built_from_32_bytes_is_43_characters` | added | Pins the fixture, after two tests failed on bad fixtures |
| *(unplanned)* `Verification_fails_when_no_hash_is_stored` | added | The pre-existing-row case |
| *(unplanned)* `Verification_fails_for_a_stored_hash_of_the_wrong_length` | added | |
| *(unplanned)* `Two_links_receive_different_tokens` | added | A shared token would authorize the wrong link |
| *(unplanned)* `The_plaintext_token_is_never_persisted` | added | AC-4 at the persistence boundary |
| *(unplanned)* `An_unknown_code_reports_the_same_outcome_as_a_wrong_token` | added | |
| *(unplanned)* `The_correct_token_deletes_the_link` (unit) | added | Unit twin of T-07 |
| *(unplanned)* `A_missing_token_is_refused_and_the_link_survives` (unit) | added | |
| *(unplanned)* `The_walker_reaches_the_known_response_surface` | added | Without it, a broken reflection walker makes the three T-14 assertions pass vacuously |

**One defect was found by a test failing for the right reason.** Two T-03 tests failed on
first run because the fixtures decoded to 35 bytes rather than 32 — and both produced the
SHA-256 of the empty input. The fixtures were wrong, but the failure exposed that
`LinkToken.Hash` was silently hashing empty for any malformed input, which would have given
**every malformed token one shared stored hash**. `Hash` now throws; fixtures are built from
byte arrays rather than hand-typed base64url.

### What's Not Tested

Named plainly. None of these is covered anywhere in the suite.

1. **The timing property is not measured.** T-18 proves verification is *reached* on the
   not-found path. It does not prove the two paths take comparable time. A future change
   that makes the not-found path cheaper in some other way would leak existence again, and
   nothing here would fail. **This is the residual risk in ADR-002's central claim.**
   No destination yet — it needs a decision about whether a timing assertion belongs in CI
   at all.

2. **`EfShortLinkRepository.TryDeleteAsync` has no direct test.** It is exercised only
   through the endpoint tests. Deleting a code that does not exist, and deleting twice, are
   both untested against real SQLite — so the `> 0` return contract rests on
   `ExecuteDeleteAsync` behaving as documented rather than as observed. Destination: **#50**.

3. **Randomness quality is not tested, only shape and ordering.** A generator returning a
   large fixed cycle of well-formed tokens passes every assertion. `RandomNumberGenerator`
   is trusted; nothing verifies distribution.

4. **Concurrent deletes are untested.** Two simultaneous deletes of one link, and a delete
   racing a redirect, have no coverage.

5. **The pre-existing-database case is untested end to end.** `Verify` returning false for a
   null hash is unit-tested, but no test starts from a database created before this column
   and asserts a delete is refused. It cannot easily be, because `EnsureCreated` builds the
   schema from the current model — which is itself the gap #49 exists for.

6. **No rate limiting on the delete endpoint.** #30 covers creation only. Each wrong token
   costs a hash and a database read on an unauthenticated surface.

7. **Token loss has no recovery path, and no test asserts the consequence.** If the create
   response is lost after commit, the link is permanently unmanageable. An idempotency key
   is the standard remedy and must be designed in at creation.

### Mode-specific evidence — TDD

No additional section is required for TDD. Recording it anyway, because three of these
tests assert properties that are trivial to write and worthless if they cannot fail:

| Mutation | Result |
|---|---|
| Early return on `link is null` before verification | Fails **exactly one** test — T-18 `Verification_runs_even_when_the_code_does_not_exist` — and nothing else. Restored, suite green |
| `TokenHash` and `ShortLink` properties added to `CreateShortLinkResponse` | Fails all three `ResponseSurfaceTests` assertions. Restored, suite green |
| *(carried from #19, re-run)* blanket `catch (DbUpdateException)`; removed `Detached` | Each fails exactly one repository test |

**RED was observed for every cycle**, as a compile failure for a new type or as a genuine
assertion failure. **One RED was weak and is recorded as such:** T-08, T-09 and T-10 passed
before the `DELETE` route existed, because an unrouted `DELETE` already returns `404`. They
were strengthened to assert the problem body before being accepted as green.

---

## Rollback

**Mechanism: `git revert` of the six commits on this branch.** No feature flag exists.

**A revert is not clean, and the reason is data.**

- **The `TokenHash` column stays in the database.** Reverting removes the property from the
  entity; it does not drop the column, and there is no migration to reverse (`data.md` §4.1
  deviation, recorded on #49). The column remains, unmapped and ignored. That is harmless.
- **Every token issued while this was live becomes meaningless.** Links created during the
  window keep resolving — no visitor sees a change — but the tokens their creators hold
  authorize nothing, because the endpoint that consumed them is gone.
- **Nothing is lost that cannot be recovered**, because delete is the only operation gated
  by the token and reverting removes the ability to delete rather than deleting anything.

**Recovery procedure if a revert happens after links were created:** none is needed for data
integrity. Notify anyone issued a token during the window that it no longer authorizes
anything, and that links created in that period cannot be deleted through the API until the
change is reapplied.

**Reapplying after a revert is safe.** The column persists, so previously issued hashes
still match their tokens — a token issued before the revert works again afterwards.

---

## Open items a reviewer must see

**`ADR-002` is `Proposed`, not `Accepted`.** [PR #53](https://github.com/vijaykgubbala/URLShortner/pull/53)
carries it and the `api.md` §2.6 amendment that permits the uniform `404`. It requires a
reviewer who is not the author, per
[`governance/architecture-docs-edit-gate.md`](../../governance/architecture-docs-edit-gate.md)
§3 — *"self-approval does not satisfy this gate at any risk level."*

**Until that merges, `api.md` §2.6 in this branch still says `401` for a missing credential
and contradicts what shipped.** A reviewer checking this code against the current
architecture document will find a conflict, and should. It is decided, not unresolved — but
it is not yet recorded in the document.

**The schema change ships with no migration**, a knowing violation of `data.md` §4.1 and
§4.4, decided before the code existed and recorded on
[#49](https://github.com/vijaykgubbala/URLShortner/issues/49).

**`DestinationProblem` now carries errors that are not about destinations** — the uniform
`404` and the 503. Flagged by `architecture-advisor`; destination
[#54](https://github.com/vijaykgubbala/URLShortner/issues/54).
