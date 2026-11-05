# Architecture

HDD_Checker separates evidence acquisition from interpretation and longitudinal
analysis. This allows raw SMART data to be reinterpreted as normalization rules
improve without altering the original measurement.

## Current layers

1. **Discovery** locates smartctl device paths and bridge types.
2. **Acquisition** executes read-only smartctl commands and preserves process
   metadata, output and exit-bit semantics.
3. **Normalization** extracts conservative ATA and NVMe concepts while keeping
   unavailable values distinct from zero.
4. **Local evidence packages** store immutable `smartctl.json` beside versioned
   normalized metadata in the Git-ignored `captures/` directory.
5. **Catalog** reopens every package, derives stable device identity and groups
   measurements into per-device histories.
6. **Longitudinal comparison** orders two measurements and calculates raw
   deltas without assigning premature risk scores.

## Stable identity

Identity is selected in this order:

1. WWN;
2. serial number;
3. normalized model plus capacity as a documented fallback.

Transient paths such as `/dev/sda` and `/dev/sdd` are never persistent device
identifiers.

## Future service boundary

The acquisition engine is designed to be reused by a lightweight Windows
service. Directly attached system drives are the primary production target.
USB bridges remain a laboratory and secondary compatibility surface.

The service will own privileged physical-drive access. User interfaces and
analysis components should remain unprivileged and communicate through a
restricted local interface. The acquisition boundary must accept only known
read-only operations, not arbitrary smartctl arguments.

## Persistence direction

JSON evidence packages remain the source record. SQLite will later provide an
index and queryable longitudinal store; it must not replace or mutate the raw
evidence.
