# Product vision

`HDD_Checker` is the current working name. A more distinctive public name will
be chosen when the product identity, interface and distribution model are more
mature. Renaming must not obscure repository history or stable data schemas.

## Differentiator

The product is not intended to be another exhaustive one-time SMART viewer. Its
primary value is lightweight, explainable observation of each device over time.

The current state describes a device. The trajectory describes deterioration.

The monitor should detect and explain:

- new or increasing error counters;
- persistent changes rather than isolated noise;
- rates per power-on hour and calendar time when intervals support them;
- acceleration only when multiple comparable rates exist;
- combinations of indicators that become relevant together;
- missing, inaccessible or non-responding devices without blocking the rest of
  the monitoring cycle.

## Honest limits

Some failures are sudden: power loss, accidental disconnection, controller
failure, impact or other abrupt damage may occur without a useful SMART trend.
The tool cannot guarantee advance notice or predict an exact failure date.

Its promise is narrower and useful: when ordinary degradation leaves observable
evidence, preserve that evidence, compare it carefully and notify the user with
an explicit reason and proportionate severity.

## Product character

- lightweight and quiet;
- preventive rather than diagnostic-heavy;
- deterministic and auditable before predictive;
- conservative about missing data and short intervals;
- resilient when one device or controller misbehaves;
- clear that monitoring complements, but never replaces, verified backups.
