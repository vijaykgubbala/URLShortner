# Execution paths

How work moves through this system, from arrival to closure.

`/work-intake` is the only front door. It classifies incoming work on two independent axes — **work type** and **ambiguity** — and selects one of six paths. Applying maximum rigor to every piece of work is not rigor, it is ceremony: a one-line bug fix routed through full requirements engineering produces artifacts nobody reads and buries the judgment that actually mattered.

Every path converges on the same per-issue loop, and every path passes the same conformance and gate checks. Depth changes how much is **asked**; it never changes how much is **checked**.

---

## 1. Master routing

```mermaid
flowchart TD
    SRC(["Requirement doc · feature request<br/>bug report · refactor note"])
    INTAKE["<b>/work-intake</b><br/><i>classify + route</i>"]
    AXIS{"Work<br/>type?"}

    RREF["<b>/requirements-refine</b><br/>REQ · ASM · NFR"]
    SPLAN["<b>/solution-plan</b><br/>work streams · sequencing"]
    RREFB["<b>/requirements-refine</b><br/><i>lean mode</i>"]
    IMPACT["<b>/impact-analysis</b><br/>blast radius · rollback<br/>constraining tests"]

    ISSUES["<b>/github-issues</b><br/>forward conformance<br/>vs standards packs"]
    CONF{"Conformance<br/>verdict"}
    ESC(["Human decision<br/>ADR or waiver"])

    SRC --> INTAKE
    INTAKE --> AXIS

    AXIS -->|greenfield| RREF
    RREF --> SPLAN
    SPLAN --> ISSUES

    AXIS -->|enhancement| RREFB
    RREFB --> IMPACT
    AXIS -->|bugfix| IMPACT
    AXIS -->|refactor| IMPACT
    IMPACT --> ISSUES

    AXIS -->|test-improve| ISSUES
    AXIS -->|docs-improve| ISSUES

    ISSUES --> CONF
    CONF -->|BLOCK| ISSUES
    CONF -->|ESCALATE| ESC
    ESC --> ISSUES
    CONF -->|PASS| BS

    subgraph LOOP ["Per-issue loop — every path"]
        direction TB
        BS["<b>/workflow-brainstorm</b><br/><i>skipped when well-defined</i>"]
        PL["<b>/workflow-plan</b><br/>test steps before impl steps"]
        EX["<b>/workflow-execute</b><br/>mode A B C or D"]
        HO["<b>/workflow-handover</b><br/>planned vs actual coverage"]
        RV["<b>/workflow-review</b><br/>4 agents · dispositions"]
        GT{"<b>/gate-check</b>"}
        CP["<b>/workflow-compound</b>"]

        BS --> PL
        PL --> EX
        EX --> HO
        HO --> RV
        RV --> GT
        GT -->|blocked| EX
        GT -->|pass| CP
    end

    STD[("standards/<br/>architecture · security<br/>operability · data")]
    ARCH[("architecture/<br/>via /architecture-guide")]

    STD -.->|rules| ISSUES
    STD -.->|rules| RV
    ARCH -.->|pre-flight| PL
    ARCH -.->|pre-flight| EX
    CP -.->|graduated rule| STD

    classDef green fill:#E8F5E9,stroke:#2E7D32,stroke-width:2px,color:#1B5E20
    classDef brown fill:#EFEBE9,stroke:#6D4C41,stroke-width:2px,color:#3E2723
    classDef shared fill:#E3F2FD,stroke:#1565C0,stroke-width:2px,color:#0D47A1
    classDef human fill:#FFF3E0,stroke:#EF6C00,stroke-width:2px,color:#E65100
    classDef ref fill:#F3E5F5,stroke:#6A1B9A,stroke-width:1px,color:#4A148C

    class RREF,SPLAN green
    class RREFB,IMPACT brown
    class ISSUES,BS,PL,EX,HO,RV,CP shared
    class ESC,AXIS,CONF,GT human
    class STD,ARCH ref
```

Three things this encodes deliberately:

- **Both paths converge at `/github-issues`, not before.** Greenfield goes through solution planning because it has work streams to sequence; brownfield skips it because a single-issue change does not. Conformance is checked once, on the same gate, for both — a bug fix cannot take the short route past the standards packs.
- **`BLOCK` and `ESCALATE` loop backward.** An issue that fails conformance is never created. That is the difference between validation and a warning label.
- **`/gate-check` failure returns to `/workflow-execute`, not to review.** A blocked gate means work remains, not that the review needs redoing.

---

## 2. Greenfield — new systems and features

```mermaid
flowchart TD
    S(["New system or feature<br/>requirement document"])
    S --> IN["<b>/work-intake</b>"]
    IN --> CL[/"type: greenfield<br/>risk: assigned<br/>path: full"/]
    CL --> R1["<b>/requirements-refine</b>"]
    R1 --> A1[/"requirements/baseline.md · REQ-nnn<br/>assumptions.md · ASM-nnn + confidence<br/>nfr.md · six categories<br/>review.md · R1–R5 checks"/]
    A1 --> R2["<b>/solution-plan</b>"]
    R2 --> A2[/"docs/plans/solution-plan.md<br/>work streams · sequencing<br/>risks · coverage map"/]
    A2 --> R3["<b>/github-issues</b>"]
    R3 --> V{"Forward conformance<br/>vs 4 standards packs"}
    V -->|BLOCK · amend| R3
    V -->|ESCALATE| H(["Human decision<br/>ADR or waiver"])
    H --> R3
    V -->|PASS| A3[/"GitHub issues<br/>traceability/rtm.md"/]
    A3 --> L["<b>Per-issue loop</b><br/>brainstorm → plan → execute <b>Mode A · TDD</b><br/>→ handover → review → gate-check → compound"]

    classDef skill fill:#E8F5E9,stroke:#2E7D32,stroke-width:2px,color:#1B5E20
    classDef art fill:#F5F5F5,stroke:#9E9E9E,color:#424242
    classDef human fill:#FFF3E0,stroke:#EF6C00,stroke-width:2px,color:#E65100
    class IN,R1,R2,R3,L skill
    class CL,A1,A2,A3 art
    class V,H human
```

---

## 3. Brownfield — enhancements, refactors, bug fixes

```mermaid
flowchart TD
    S(["Change request · bug report<br/>refactor proposal"])
    S --> IN["<b>/work-intake</b>"]
    IN --> T{"Which kind?"}

    T -->|enhancement| E1["<b>/requirements-refine</b><br/><i>lean or full by ambiguity</i>"]
    E1 --> IA
    T -->|bugfix| BG{"Is correct behaviour<br/>stated anywhere?"}
    BG -->|no| REROUTE(["Not a bug —<br/>route back as enhancement"])
    BG -->|yes, cite the REQ| IA
    T -->|refactor| IA

    IA["<b>/impact-analysis</b>"]
    IA --> A1[/"docs/impact/…-impact.md<br/>change surface · named consumers<br/>blast radius + reasoning<br/>hot paths · migration · rollback<br/><b>constraining tests</b>"/]
    A1 --> ISS["<b>/github-issues</b><br/>single issue"]
    ISS --> V{"Forward conformance"}
    V -->|BLOCK or ESCALATE| ISS
    V -->|PASS| M{"Execution mode"}

    M -->|enhancement · bugfix| MA["<b>Mode A · TDD</b><br/>bugfix: reproduction test must fail<br/><i>for the reason the bug exists</i>"]
    M -->|refactor| MB["<b>Mode B · Characterization-first</b><br/>pin current behaviour →<br/>commit pinning tests <b>alone</b> →<br/>refactor in steps, never edit an assertion"]

    MA --> L["handover → review → gate-check → compound"]
    MB --> L

    classDef skill fill:#EFEBE9,stroke:#6D4C41,stroke-width:2px,color:#3E2723
    classDef art fill:#F5F5F5,stroke:#9E9E9E,color:#424242
    classDef human fill:#FFF3E0,stroke:#EF6C00,stroke-width:2px,color:#E65100
    classDef stop fill:#FFEBEE,stroke:#C62828,color:#B71C1C
    class IN,E1,IA,ISS,MA,MB,L skill
    class A1 art
    class T,V,M,BG human
    class REROUTE stop
```

The bugfix branch has a stop in it. If nothing states the correct behaviour, this is not a bug — it is an enhancement wearing a bug's clothing, and it routes back. That distinction is the one most often blurred, and blurring it is how requirements rot: the second kind never gets written back into the baseline.

---

## 4. Test and documentation improvements

```mermaid
flowchart TD
    S(["Tests are weak or missing<br/>Docs are stale or wrong"])
    S --> IN["<b>/work-intake</b>"]
    IN --> T{"Which?"}

    T -->|test-improve| TI["<b>/github-issues</b> · single issue<br/><i>AC names the uncovered behaviour,<br/>not a coverage percentage</i>"]
    T -->|docs-improve| DI["<b>/github-issues</b> · single issue"]

    TI --> SKIP1(["<b>skip</b> /workflow-brainstorm<br/>approach is not open"])
    DI --> SKIP2(["<b>skip</b> /workflow-brainstorm"])

    SKIP1 --> P1["<b>/workflow-plan</b><br/>every test case names the<br/>mutation that will verify it"]
    SKIP2 --> P2["<b>/workflow-plan</b><br/>testing strategy replaced by an<br/>execution checklist"]

    P1 --> C["<b>Mode C · Mutation-verified</b>"]
    C --> C1["1 · baseline coverage"]
    C1 --> C2["2 · write test, confirm it passes"]
    C2 --> C3["3 · <b>mutate production code</b><br/>test MUST now fail"]
    C3 --> CQ{"Still passes?"}
    CQ -->|yes| CBAD(["Test asserts nothing.<br/>Worse than no test —<br/>it makes coverage lie."])
    CQ -->|no| C4["4 · restore code exactly<br/>git diff must be clean"]
    C4 --> C5["5 · commit test alone,<br/>mutation recorded in message"]

    P2 --> D["<b>Mode D · Documentation</b>"]
    D --> D1["1 · read the code — it is the truth"]
    D1 --> D2["2 · write or correct the doc"]
    D2 --> D3["3 · <b>execute every instruction</b><br/>literally, from a clean state"]
    D3 --> D4["4 · record what had to be fixed<br/>because it did not work as written"]

    C5 --> G["handover → review → gate-check → compound"]
    D4 --> G

    classDef skill fill:#E3F2FD,stroke:#1565C0,stroke-width:2px,color:#0D47A1
    classDef step fill:#F5F5F5,stroke:#9E9E9E,color:#424242
    classDef human fill:#FFF3E0,stroke:#EF6C00,stroke-width:2px,color:#E65100
    classDef stop fill:#FFEBEE,stroke:#C62828,color:#B71C1C
    class IN,TI,DI,P1,P2,C,D,G skill
    class C1,C2,C3,C4,C5,D1,D2,D3,D4 step
    class T,CQ,SKIP1,SKIP2 human
    class CBAD stop
```

Neither of these can use red-green-refactor. You cannot write a failing test *for a test*, and a documentation change has no red step at all. Mode C inverts the verification — prove the test would catch the bug it claims to catch. Mode D replaces it — a doc that has not been executed is a doc that is wrong.

---

## 5. Well-defined vs ambiguous — the depth dial

Ambiguity is a second, independent axis. It does not choose a path; it changes how much interrogation the chosen path gets.

```mermaid
flowchart TD
    S(["Any incoming work"]) --> IN["<b>/work-intake</b>"]
    IN --> TEST{"Can you write the acceptance<br/>criterion right now, from the text<br/>alone, inventing nothing?"}

    TEST -->|yes| WD["<b>well-defined</b>"]
    TEST -->|no| AM["<b>ambiguous</b>"]

    WD --> WD1["<b>/requirements-refine</b> · <i>lean mode</i><br/>decompose, record only genuine<br/>ambiguities, elicit NFRs, review"]
    WD1 --> WD2(["<b>skip</b> /workflow-brainstorm<br/>unless the <i>approach</i> is open"])
    WD2 --> WD3["Expected outcome:<br/>PASS or PASS_WITH_AMENDMENT"]

    AM --> AM1["<b>/requirements-refine</b> · <i>full mode</i><br/>every ambiguity gets an <b>ASM-nnn</b><br/>with a proposed default and<br/>a confidence level"]
    AM1 --> AM2["Interrogate the medium- and<br/>low-confidence assumptions<br/>one question at a time"]
    AM2 --> AM3["<b>/workflow-brainstorm</b> · <b>mandatory</b><br/>2–3 approaches, then stress-test<br/>the recommendation"]
    AM3 --> AM4{"Requirement conflicts<br/>with a standard?"}
    AM4 -->|no| AM5["PASS_WITH_AMENDMENT"]
    AM4 -->|yes| ESC["<b>ESCALATE</b><br/>both citations · the trade-off ·<br/>2+ options · the role who decides<br/><b>then STOP</b>"]
    ESC --> HUM(["Human resolves →<br/>ADR or waiver with expiry"])

    WD3 --> RV["Review pass runs <b>identically</b><br/>in both modes — depth changes<br/>how much is <i>asked</i>, never<br/>how much is <i>checked</i>"]
    AM5 --> RV
    HUM --> RV

    classDef wd fill:#E8F5E9,stroke:#2E7D32,stroke-width:2px,color:#1B5E20
    classDef am fill:#FFF8E1,stroke:#F9A825,stroke-width:2px,color:#F57F17
    classDef human fill:#FFF3E0,stroke:#EF6C00,stroke-width:2px,color:#E65100
    classDef key fill:#E3F2FD,stroke:#1565C0,stroke-width:2px,color:#0D47A1
    class WD,WD1,WD2,WD3 wd
    class AM,AM1,AM2,AM3,AM5 am
    class TEST,AM4,ESC,HUM human
    class IN,RV key
```

The last node is the point. The five review checks in `/requirements-refine` run identically in both modes. Otherwise "well-defined" becomes a way to skip rigor rather than a way to skip ceremony.

---

## 6. Execution modes

```mermaid
flowchart LR
    EX["/workflow-execute"] --> M{"Execution mode<br/>from intake record"}

    M -->|"A · greenfield<br/>enhancement · bugfix"| A["<b>TDD</b><br/>red → green → refactor"]
    M -->|"B · refactor"| B["<b>Characterization-first</b><br/>pin → commit → refactor"]
    M -->|"C · test-improve"| C["<b>Mutation-verified</b><br/>write → mutate → restore"]
    M -->|"D · docs-improve"| D["<b>Documentation</b><br/>write → execute literally"]

    A --> AG(["Test must be <i>observed</i><br/>failing before any code"])
    B --> BG(["Pinning tests committed <i>alone</i>,<br/>before anything is touched"])
    C --> CG(["Test must fail against<br/>a deliberate mutation"])
    D --> DG(["Every instruction run<br/>from a clean state"])

    AG --> DONE["Task complete only when<br/>every AC has a passing test"]
    BG --> DONE
    CG --> DONE
    DG --> DONE

    classDef mode fill:#E3F2FD,stroke:#1565C0,stroke-width:2px,color:#0D47A1
    classDef gate fill:#FFF3E0,stroke:#EF6C00,stroke-width:2px,color:#E65100
    class A,B,C,D mode
    class AG,BG,CG,DG gate
```

---

## 7. The traceability spine

Every artifact links back to a requirement clause. `traceability/rtm.md` is regenerated, never hand-maintained, and exists to answer the three questions at the bottom.

```mermaid
flowchart LR
    SRC["Source doc"] --> REQ["REQ-014"]
    REQ --> ASM["ASM-003<br/><i>confidence: low</i>"]
    REQ --> NFR["NFR-002"]
    REQ --> ISS["Issue #81"]
    ISS --> AC["AC-3"]
    AC --> TST["TEST_ConcurrentSubmit"]
    TST --> CMT["commit a91f3c"]
    ISS --> GATE["GATE-81<br/>approver + timestamp"]

    RTM[("traceability/rtm.md")]
    REQ -.-> RTM
    ISS -.-> RTM
    RTM --> Q1{{"Which REQs<br/>have no issue?"}}
    RTM --> Q2{{"Which issues<br/>trace to nothing?"}}
    RTM --> Q3{{"Which ACs<br/>have no test?"}}

    classDef q fill:#FFEBEE,stroke:#C62828,color:#B71C1C
    class Q1,Q2,Q3 q
```

A non-zero answer to any of the three is a finding, not a footnote.

---

## Related

- [`architecture/README.md`](../architecture/README.md) — the system's structure, as opposed to this document's process
- [`standards/`](../standards/) — the rule packs the conformance check and review agents cite
- [`governance/architecture-docs-edit-gate.md`](../governance/architecture-docs-edit-gate.md) — why `architecture/` is gated and this document is not
