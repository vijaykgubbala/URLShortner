---
name: architecture-guide
description: Get the layer rules that apply to a specific piece of work before writing any code. Use whenever you are about to add, modify, or move code in src/ and want to know which rules bind this change — even if the user does not ask for architecture guidance.
argument-hint: "<one sentence describing what you are about to build>"
allowed-tools: Task, Read
---

# architecture-guide

Returns the architecture rules that bind a specific piece of work, before the code is written.

This skill does exactly one thing: it dispatches `architecture-advisor` with your description and relays what comes back.

## Procedure

**Step 1 — Take the description.** Use `$ARGUMENTS` as the one-sentence description of the work. If it is empty, ask for one sentence describing what is about to be built. Do not guess from the surrounding conversation — a vague description produces a vague rule set, which is worse than none because it looks like coverage.

**Step 2 — Dispatch the advisor.** Invoke the `architecture-advisor` subagent with that description, using its real `subagent_type` so its tool restrictions and turn budget apply.

**Step 3 — Relay the output unchanged.** Present the advisor's returned rules exactly as received: the same rule headings, the same source citations, the same verbatim quotes, the same likely-violation flags.

## The rule that makes this trustworthy

**Do not transform, summarize, or reword the advisor's output.**

Not to make it shorter. Not to make it read better. Not to merge two rules that seem to say the same thing. Not to drop a rule that seems obviously satisfied already.

Rewording breaks the verbatim-quoting contract that makes the guidance trustworthy. The value of a returned rule is that it is provably the text in the document — a reader can open `architecture/layers.md` §4.3 and see the same words. A summarised rule is an assertion by this skill about a document, which is exactly the thing `architecture-advisor` carries no inline rules in order to avoid.

If the advisor returns nothing, relay that it returned nothing. Do not fill the gap with general architectural advice.

## Constraints

- Do not modify the advisor's output in any way, including formatting that changes quoted text.
- Do not add rules of your own, from general knowledge, or from memory of the architecture docs.
- Do not read the `architecture/` documents into this session to "check" or supplement the advisor. That reintroduces the context cost the advisor exists to avoid.
- Do not write or modify any code. This skill answers a question and stops.
- Do not suppress a returned rule on the grounds that it appears already satisfied.
