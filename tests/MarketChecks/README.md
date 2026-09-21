# Market calculation checks

`-- --health-live` uses the selected development database for read-only health query
checks: nonexistent users see only featured assets, unknown asset retries are rejected,
and a seeded in-memory cooldown prevents any provider call. No database writes or
upstream calls are made in this mode. Set `MINIMOLA_CHECK_CONNECTION` as below.

Run deterministic checks (no network/database calls):

```powershell
dotnet run --project tests/MarketChecks/MarketChecks.csproj
```

Optional integration check: set `MINIMOLA_CHECK_CONNECTION` to a development database,
then add `-- --live`. This calls the real catalog, price, portfolio and estimate services
and **writes** their normal catalog/quote/estimate updates. Never point it at production.

The KAP holdings model resolves eligible domestic equity fund identities from KAP's
YF catalog and discovers monthly reports using the public fund query and attachment
detail endpoints. Successful checks are cached for
six hours, failures for 30 minutes. Fund identity, month, attachment name/id and
conservative holdings thresholds are checked before storing a new report. Failed
checks preserve the last valid report. Same-month corrections require manual review;
unknown PDF formats in eligible equity funds return unavailable, not a fabricated
holdings estimate. Funds outside this scope retain the existing proxy model.

`-- --live-portfolios` checks THF, AK3, TI2, HVS and NNF end to end and verifies
that the currently unsupported MAC format returns unavailable. This writes real
validated portfolio reports into the selected development database.

Optional PDF regression: download the August 2026 PDFs into ignored `tmp/pdfs/`
as `THF.pdf`, `AK3.pdf`, `TI2.pdf`, `HVS.pdf`, `NNF.pdf`, then run:

```powershell
dotnet run --project tests/MarketChecks/MarketChecks.csproj --no-restore -- --pdf-check tmp/pdfs
```

KAP notification IDs: THF 1657113, AK3 1657452, TI2 1657938, HVS 1657358,
NNF 1657437, YEF 1658136 (save its attachment as `YEF.pdf`). YEF uses a separate
Yapı Kredi layout reader: market-value sums must reconcile within 0.05 TL and
weights are calculated against net asset value, not the displayed portfolio-value
percentages. Expected YEF output is 32 nonzero rounded holdings, 92.00% NAV weight.
The check asserts holdings counts and FTD totals against these specific
reports, and rejects incorrect fund identities/months. PDF files are not bundled.
The holdings calculation requires
today's TEFAS price date, same-session quotes (45-minute tolerance during trading),
at least 60% weighted price coverage and a report no older than 45 days. After 18:30
Turkey time, same-day quotes from 18:00 onward are accepted as closing observations.
Weekends wait for a new session. Holidays are not predicted by a full exchange calendar;
without today's valid prices, no estimate is produced.

The next weekday is used as the expected publication date; evaluation only accepts
that exact date for `kap-holdings-v2`. Missing/holiday dates remain unevaluated rather
than being compared to a later multi-day movement. The date convention and estimates
must be validated against actual TEFAS publications before treating accuracy as proven.
Old proxy-model results are kept separate. Coverage is not a probability of accuracy.

Intraday and closing snapshots are now separate rows. Legacy rows are retained but
excluded from new metrics. Closing snapshots are immutable; only evaluated closing
snapshots contribute to the last-30 statistics. The holdings capture job runs on weekdays
in the 18:45-19:30 Turkey-time window, accepting only same-day quotes from 18:08 onward
with at least 60% coverage. The app must be running. No late backfill or half-day
calendar inference is performed. An official price already stored before a prediction
is not accepted as an out-of-sample evaluation result.
