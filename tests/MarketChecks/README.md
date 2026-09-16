# Market calculation checks

Run deterministic checks (no network/database calls):

```powershell
dotnet run --project tests/MarketChecks/MarketChecks.csproj
```

Optional integration check: set `MINIMOLA_CHECK_CONNECTION` to a development database,
then add `-- --live`. This calls the real catalog, price, portfolio and estimate services
and **writes** their normal catalog/quote/estimate updates. Never point it at production.

The KAP holdings model currently uses the configured THF pilot report. It requires
the manually configured August 2026 report; automatic discovery of new reports is
not implemented. Other funds retain the existing allocation/proxy estimate model.
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
