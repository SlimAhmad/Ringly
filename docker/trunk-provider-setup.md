# SIP Trunk Provider Deployment (Ringly-Reference.md §8.7)

Row #28. Unlike Asterisk/Postgres/coturn, a real SIP trunk provider account (e.g. Twilio
Elastic SIP Trunking) can't be stood up in this repo — it's a real external account with real
signaling IPs and, if used for outbound dialing, real cost. This is a checklist for whoever
configures that account, not a script.

## 1. Provider-side controls (§8.7 item 11) — do this first, before any app code runs

These are the **primary, non-negotiable** defense per §8.4 — the ones that still hold even if
Asterisk itself is fully compromised. `SipTrunkFoundationService`'s app-side checks (row #25)
are defense in depth on top of these, not a replacement for them.

In the trunk provider's own dashboard/API, before `ConfigureTrunkAsync` is ever called against
this trunk:

- [ ] Set a hard daily/monthly spend cap at the provider level.
- [ ] Set a destination whitelist (or leave international calling off entirely) at the
      provider level — don't rely solely on `SipTrunkConfig.AllowedDestinationCountryCodes`.
- [ ] Leave international dialing **disabled** by default at the provider level. Match
      `SipTrunkConfig.InternationalDialingEnabled = false` unless there's a specific,
      considered reason to enable it on both sides.
- [ ] Note the trunk's actual signaling IP address(es) — needed for
      `SipTrunkConfig.ProviderHost`, and it must be a real IP/CIDR, not a hostname (see
      `SipTrunkBroker.ConfigureTrunkAsync`'s comment on the `identify` object — Asterisk
      rejects a hostname there with "failed field value validation", confirmed against the
      real endpoint in row #24).
- [ ] Note the trunk's SIP credentials (username/password) if the provider uses credential
      auth rather than (or in addition to) IP-based auth — these become
      `SipTrunkConfig.Username`/`Password`.

## 2. PJSIP trunk objects (§8.7 item 12) — already handled dynamically, no static config needed

Row #19b moved this deployment to realtime PJSIP (Postgres-backed), not static `pjsip.conf`
stanzas — the same choice applies here. `SipTrunkBroker.ConfigureTrunkAsync` (row #24) creates
the trunk's `aor`/`auth`/`identify`/`endpoint` PJSIP objects dynamically via the same ARI
dynamic-config PUT path client SIP provisioning uses (row #7/#21), once given a `SipTrunkConfig`
built from the values gathered in step 1. There's no `docker/asterisk/config/pjsip.conf`
trunk stanza to add — confirm the values above, then call `ConfigureTrunkAsync`.

Note: `endpoint` object creation is currently blocked by a confirmed, still-open upstream
Asterisk bug ([asterisk/asterisk#1655](https://github.com/asterisk/asterisk/issues/1655)) —
`aor`/`auth`/`identify` work correctly against the realtime Postgres backend, `endpoint` does
not, regardless of trunk vs. client SIP objects. See row #21/#24's notes.

## 3. Spend-status alerting (§8.7 item 13)

Done — row #29. `TrunkSpendAlertBackgroundService` polls every configured trunk on an interval
(`TrunkSpendAlertOptions.PollInterval`, default 5 minutes) via `TrunkSpendAlertService.PollAsync`,
comparing each against its own `MaxConcurrentCallsPerTrunk`/`MaxDailySpendUsd` and calling
`ITrunkSpendAlertNotifier` on anomalies. Only a logging-based default notifier
(`LoggingTrunkSpendAlertNotifier`, Critical-level) ships — swap in a real paging/messaging
integration (Slack/email/PagerDuty/SMS, not doc-specified which) before relying on this for
production toll-fraud detection.
