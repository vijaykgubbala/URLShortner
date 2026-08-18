---
issue: 21
risk: risk:high
standards_version: standards@2026.08.1
review_record: docs/reviews/2026-08-18-21.json
branch: feat/21-capability-token
outcome: BLOCK
run: 1
date: 2026-08-18
---

# GATE-21 — run 1

**Outcome: BLOCK.** Criteria 1, 2 and 6 failed.

## Criteria

| # | Criterion | Result | Evidence |
|---|---|---|---|
| 1 | Unaddressed critical finding | **fail** | `docs/reviews/2026-08-18-21.json` carries 5 critical findings. Four are `addressed`. **SEC-002 is `open`.** Its recorded reason: *"Serialisation half remains OPEN, awaiting a named approver and expiry for a WVR against STD-SEC-03 scoped to this single property on the 201."* `standards/waivers.md` contains no waiver for `STD-SEC-03` — only WVR-001 (`STD-SEC-05`, expiry 2026-11-17) and WVR-002 (gate criterion 3, expiry 2026-10-17). There is nothing to accept. |
| 2 | Unproven acceptance criterion | **fail** | Two of six. See below. |
| 3 | Uncovered requirement | pass | `Traces to` names REQ-003, NFR-008, NFR-009. REQ-003 (*"The service shall expose its operations as APIs"*) is covered by 56 tests in `UrlShortener.Api.Tests`. NFR-008 (*"No property holding a credential, token or key shall be serializable or exposed on a public surface"*) is covered by the four `ResponseSurfaceTests` assertions and `The_create_response_does_not_print_its_token`. NFR-009 (*"Each identified threat shall have an automated test asserting the attack fails"*) is covered by `A_forged_token_is_refused_and_the_link_survives`, `One_links_token_cannot_delete_another_link` and `Every_verification_path_does_the_same_work`. This criterion asks for coverage *at all*, not complete coverage — the incompleteness is criterion 2's business, and it is failing there. |
| 4 | Untraceable issue | pass | Issue #21 `Traces to` line reads verbatim: `REQ-003; NFR-008, NFR-009`. |
| 5 | Stale conformance record | pass | `standards/VERSION` contains `standards@2026.08.1`. `validation/url-shortener-conformance.md:3` reads **"Standards version in force: `standards@2026.08.1`"**. They match. |
| 6 | Missing approvals | **fail** | `risk:high` requires engineer, named reviewer, **and** explicit written sign-off — all three recorded with name and timestamp. `docs/gates/` contains only `GATE-17.md`. **No approval of any kind exists for #21.** |
| 7 | Mode-specific (`TDD`) | pass | Handover frontmatter records `mode: TDD`. TDD adds no criterion beyond the above. |

## Criterion 2 — the detail

**AC-1 — the second clause has no test.**

> *"Given a successful create, when the response is returned, then it contains a management token **generated from a cryptographically secure source**."*

The first clause is proven by `Creating_a_link_returns_a_management_token`. The second is not proven by anything. Review finding TST-004 established this **by mutation, verified during the review**: replacing `RandomNumberGenerator.Fill` with `Random.Shared.NextBytes` leaves all 20 generator tests passing, as does a seeded `Random`. Only a monotonic counter fails. `CryptoLinkTokenGeneratorTests` now states this openly — *"These tests prove shape, distinctness and non-ordering. They do NOT prove the source is cryptographically secure"* — which corrects a false claim but does not close the gap. The distribution test is on #50.

**AC-3 — the criterion on the issue asserts a status code the system does not emit.**

The issue's AC-3 reads verbatim:

> *"Given a mutation request with a missing or incorrect token, when it is processed, then **`403`** is returned and no change is made."*

`grep -rn "403" src/ tests/` returns three matches, all of them comments explaining why `404` was chosen *instead* of `403`. **No code emits a 403 and no test asserts one.** The issue body contains zero occurrences of "404".

The amendment to this criterion was recorded in `docs/brainstorms/2026-08-17-21-capability-token-brainstorm.md`, in `docs/plans/2026-08-17-21-feature-capability-token-plan.md`, in `docs/reviews/2026-08-18-21.json` and in the handover — **in every artifact except the issue, which is where the acceptance criteria authoritatively live.** `/workflow-plan` step 0 states the reason this matters: *"Read the acceptance criteria. They are the definition of done for everything downstream."*

So the shipped behaviour is correct against a decision that was made, and unproven against the criterion as written. Amending the issue is not a formality — until it happens, this criterion has no passing test and cannot have one.

**The other four ACs pass:**

| AC | Proven by |
|---|---|
| 2 — correct token succeeds | `The_correct_token_deletes_the_link_and_returns_204`, `The_correct_token_deletes_the_link` |
| 4 — no other response carries it | `ResponseSurfaceTests` ×4, `A_redirect_response_contains_no_token`, `The_plaintext_token_is_never_persisted` |
| 5 — token in no log argument | `The_token_never_reaches_a_log_message`, now with a `NotEmpty` guard against a vacuous pass |
| 6 — forged token rejected | `A_forged_token_is_refused_and_the_link_survives`, `One_links_token_cannot_delete_another_link` |

## Waivers applied

| Waiver | Rule/finding | Approver | Expiry |
|---|---|---|---|
| — | none | — | — |

WVR-001 and WVR-002 both exist and are unexpired, but neither is applicable to this issue: WVR-001 waives `STD-SEC-05` on the redirect path, and WVR-002 waives gate criterion 3 for component issues — criterion 3 passed here on its own.

**No waiver exists for `STD-SEC-03`**, which is what SEC-002 requires. A waiver naming no approver or no expiry would be invalid — treated as absent, not as weak — so none has been written.

## Approvals

| Role | Name | Timestamp |
|---|---|---|
| Engineer | — | — |
| Named reviewer | — | — |
| Written sign-off | — | — |

**None recorded.** `risk:high` requires all three, each with a name and a timestamp. No name has been supplied for this issue by any means.

## Observation outside the criteria

Recorded because it is material and because no criterion covers it, not as a finding.

Six shipped source files cite `ADR-002` as binding authority, and that document **does not exist on this branch** — it is on `docs/adr-002-uniform-404` (PR #53). Verified: `decisions/` here contains only `ADR-001-outbound-port-shape.md`. Review findings ARCH-001 and ARCH-002 record this, and both the security and architecture lenses reported that they were consequently unable to judge whether the code matches the reasoning it defers to.

`architecture/README.md` §1.3 states the correction for a rule that proves wrong is *"an `ADR-nnn` in `decisions/` plus an edit to the relevant file under the gate […] not a local deviation"* — and on this branch neither half is present. PR #53 is therefore a merge-order prerequisite, and per `governance/architecture-docs-edit-gate.md` §3 it requires *"review and approval by a named human other than the author"*, which **self-approval does not satisfy at any risk level**.

Two review findings also record independent defects in the reasoning that PR asks a reviewer to write into `api.md` permanently: SEC-005 (the enumeration rationale overstates its benefit, because `GET /{code}` already discloses existence) and ARCH-003 (the exception is scoped on the wrong axis and omits its three obligations).
