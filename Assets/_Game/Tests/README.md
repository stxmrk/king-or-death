# Prototype integration verification

`RuntimeVerification` runs only when the standalone player receives `-verify <absolute-output-directory>`.

It executes the same components used in play: pathing across the bridge, flag capture, territory and inventory checks, worker harvesting, caravan delivery, melee/death, scent-driven wolves, physical burial, ownership loss, cargo salvage and blocked-route recovery.

The runner writes individual PASS/FAIL lines, an initial rendered screenshot, and a nonzero exit code on failure. It has a 180-second wall-clock watchdog. This is a custom player integration harness, not an NUnit EditMode test suite. It supplements, rather than replaces, manual UI checks.
