# Support Team — Internal Notes

These are the notes I share with new folks joining the support team. It's not a
formal policy document, more how we actually handle things day to day. Use your
judgement, ask in #support-leads if something is weird, and when in doubt: don't
promise anything you can't back up.

## What we sell

Two plans. Basic is the cheap one for hobbyists and small side projects, Premium
is the one most real customers are on. Basic is around $10/month, Premium is
around $50/month — I don't remember the exact numbers off the top of my head,
check the pricing page if a customer asks for the cent.

Basic gets you up to a handful of projects, a small amount of storage, and email
support (that's us, with slower SLAs). Premium is "unlimited" projects, a lot
more storage, priority support, analytics, and API access. The big differentiator
in practice is API access — that's what most upgrades are driven by.

There used to be an Enterprise tier but we don't sell that anymore as of last
year. If someone asks, tell them we'll have them talk to sales.

## Refunds — how we actually handle them

We're generally pretty flexible on refunds in the first month. If someone signed
up, didn't use it, and asks within ~30 days, just do it. Don't make them justify
it.

After the first month it gets fuzzier. The situations where we DO refund:

- There was a real service outage during their billing period and they couldn't
  use the product. If the outage is in the status page or in #incidents, that's
  easy — refund for the affected period.
- Obvious billing error on our side (double charge, wrong amount, charged after
  cancellation). Refund and apologize, no drama.
- First-time customer who's clearly not using the product and writes in politely.
  We usually give them one. Don't make a habit of it.

Situations where we DO NOT refund (or at least, where I push back first):

- Customer is asking for a refund covering multiple months because they "forgot
  to cancel." We don't do retroactive refunds for forgetting. Best I'll usually
  do is refund the most recent month as a goodwill gesture if they're otherwise
  polite.
- Customer has already had a refund within the past year. Our informal rule is
  one goodwill refund per customer per year. If they had a real billing error
  that's different.
- Customer is abusive in the ticket. I'm not paying people to be yelled at. If
  the tone crosses a line, escalate instead of engaging.

When we do refund, it goes back to the original payment method, takes 5-7
business days usually (depends on the card issuer, we don't control that). Don't
promise a specific date.

## Cancellations

This one is simpler. If a customer wants to cancel, cancel them. Don't try to
talk them out of it unless they're asking a question rather than actually saying
"cancel me."

The mechanics: cancellation is effective immediately in the system but they keep
access until the end of the current billing period they already paid for. Their
data sticks around for about 90 days after cancellation in case they change their
mind or want an export. After that it's gone.

If someone wants to export their data before cancelling, there's a self-serve
export tool in settings. Send them there. If the export tool is broken (it's
been flaky), file a ticket with engineering and keep the account active until
it's resolved.

## Escalation — when to kick it up

Escalate when:

- Customer explicitly asks for a manager or supervisor. Don't argue, just do it.
- Billing dispute over $100. I want eyes on these before we commit either way.
- Customer has contacted us three or more times about the same issue in the past
  month and it's still not resolved. That's a pattern and it needs senior
  attention.
- Anything that smells legal — mentions of lawyers, GDPR, chargebacks, regulators.
  Don't try to handle those yourself. Kick it up immediately.
- You genuinely don't know what to do. Better to ask than to guess.

Escalations go to the senior support team via an internal ticket tagged
`ESCALATE`. SLA for them is ~4 business hours during the workday. On weekends
it's best-effort — Maya covers Saturdays when she can.

## Tone

- Short and human. We're not a corporate chatbot.
- Acknowledge what they said before jumping to the answer. Especially if they're
  upset.
- Never invent a feature or a rule. If you don't know whether we support
  something, say "let me check" and actually check. Customers remember being
  lied to.
- Don't paste generic templates. It's obvious and it's annoying.

## Things we don't do

- We don't offer partial-month credits for downgrades. If someone downgrades
  mid-month, the change takes effect next cycle.
- We don't grant plan features (like API access) as a one-off exception. Either
  they're on Premium or they're not.
- We don't share customer information between accounts, even if they ask nicely.
  "My colleague set this up, can you tell me the plan" — no. They have to get it
  from their colleague.

## Stuff that's come up a lot lately

- People confusing "cancel" with "pause." We don't have a pause feature. If
  they want to pause, the honest answer is they cancel and resubscribe later.
- API rate limit questions on Basic — the answer is no, API is Premium-only.
  This trips people up because the docs used to be ambiguous. Docs team is
  fixing it.
- "Why was I charged again?" usually means their renewal happened and they
  forgot it was a subscription. Just explain it calmly. If they still want to
  cancel and refund after that, see the refund rules above.
