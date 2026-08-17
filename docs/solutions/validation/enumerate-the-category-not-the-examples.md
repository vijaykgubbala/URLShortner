# Implementing the examples in a rule instead of the category it names

**From:** #17 · `GATE-17.md` run 1, review findings COR-002 / SEC-002 / TST-002

## Problem

An acceptance criterion named a **category**: *"loopback, link-local, private or **reserved** address range"*. The implementation had to decide what "reserved" meant.

## What Happened

The IPv4 check enumerated eight first-octet cases. Three of the four categories were complete. "Reserved" was implemented as four named examples — and multicast `224/4`, future-use `240/4`, the benchmark range and all three TEST-NET blocks were left permitted.

The same shape recurred on the IPv6 side: five named refusals, with multicast, 6to4, NAT64 and IPv4-compatible encodings all permitted.

Every test passed. 119 of them. **The tests were written from the same examples as the code**, so they agreed with it perfectly and proved nothing about the category.

## Root Cause

Reading a rule and implementing the instances that came to mind, rather than the set the rule names. A category is a claim about *everything* in it; an enumeration is a claim about the members someone thought of.

The tests inherited the defect because they were derived from the implementation's mental model rather than from the criterion's words.

## Prevention

- **Quote the criterion's category word** in the code, next to the check that implements it. `// AC-3: "reserved"` forces the next reader to compare the list to the claim.
- **Express a range as a range.** `>= 224 => false` cannot omit a member; `224 => false, 239 => false` can and did.
- **Write one test case from the category that is not in the implementation's list.** If the code enumerates, the test must not enumerate the same way.
- **Test the boundary in both directions.** Assert that `223.255.255.255` and `198.20.0.1` stay *permitted*. A mask one bit too wide passes every refusal test while silently blocking legitimate space, and only the permitted-side rows catch it.

## Key Insight

A test derived from the implementation confirms the implementation, not the requirement — so an enumeration and its tests will always agree, and will always agree about the members nobody thought of.
