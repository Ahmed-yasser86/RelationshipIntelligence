# Overview

Relationship Intelligence tells you **who needs you this week, and why** —
with the evidence attached so you can judge for yourself.

## The problem it solves

Professional relationships decay silently. Nobody notices a contact going
cold until the moment they need them — the introduction, the referral, the
hire — and by then the cost is already paid. Contact lists, CRMs, and
LinkedIn all *store* people. None of them watch how relationships move
over time and tap you on the shoulder before drift becomes loss.

People run their networks on guilt and memory: "I should probably reach
out to…" with no evidence behind it, or generic check-ins that help
nobody. The methodological problem is prior to any feature: **before
asking what to do about a relationship, establish what the record
actually shows — what was logged, what the model derived, and what you
yourself asked for — and keep those three strictly separated.**

## How it works

- You **log** real contact (calls, emails, meetings, messages) or import
  history. Dated events are the only thing the system treats as observed
  fact.
- The system **scores** every relationship deterministically: tie strength
  from an exponential-decay model, silence against each contact's own
  measured rhythm, urgency as the deficit to your strongest tie, bands as
  display thresholds. Same inputs, same outputs — no hidden tuning.
- It **surfaces** who needs attention with the reason attached ("quiet
  120d vs ~30d rhythm"), enriched with upcoming events and open
  commitments — never as a bare number.
- You **express intent** in human terms ("stay in touch every 10 days",
  "remind me", "keep in touch intentionally"). Intent constrains surfacing;
  it never rewrites scores and never fabricates contact.
- The **Copilot** answers questions by calling read-only tools over your
  data and citing what it used. Anything that would change durable state
  is proposed first and executed only after your confirmation.
- **Approval is the only path** from AI suggestion to trusted state. Every
  applied change keeps provenance back to its source.

## What it does not do

- It does not send messages, book meetings, or act externally. Approval
  marks things reviewed — you do the contacting.
- It does not predict relationships, score people as humans, or claim
  causation. Urgency is a ranking aid over your own history, not a verdict.
- It does not read message content or sentiment, track the other side, or
  import anything you didn't give it.
- It does not merge wishing, measuring, and happening: reminders never
  fake contact, suggestions never become truth without approval, scores
  never narrate themselves as facts about a person.

## Current status

Working system on real data: deterministic scoring over logged history,
weekly digest with signed action links, outreach batches with grounded
drafts, meeting transcription-to-evidence pipeline, AI-assisted ingestion
with a review queue, per-person preferences and reminders, and an
agentic Copilot over ~40 tools. Backend suite (272 tests) plus 24
controller tests green; frontend type-checked and built. See
[testing](testing.md).

## Where to go next

- Product owners: [product-vision](product-vision.md) — why it matters,
  Copilot business value, horizons.
- HR and recruiters: [for-hr-recruiters](for-hr-recruiters.md) — pipelines,
  filtering, outreach at scale.
- Engineers: [architecture](architecture.md) · [api](api.md) ·
  [development](development.md).
- Researchers: [for-researchers](for-researchers.md) — constructs,
  evidence grades, what can and cannot be cited.
- Method: [methodology](methodology.md) · [pipelines](pipelines.md).
