# Golden first playable cutoff regression

Root cause: the acceptance fixture queued 50 residents into the default eight-seat car but retained a >20-minute congestion assertion from the previous minute-scale movement. With current physical timing, baseline wait is 8.92 minutes, intervention 4.24 (52.47% improvement). This is improved service, not a cause-chain failure.

Changed only GoldenFirstPlayableTests.cs: explicitly give both synthetic peak scenarios two seats per car. Physical default timing remains in use; adding a second car remains the sole service variable changed between scenarios. All delivery, >20-minute baseline congestion, >=40% improvement, overlay/inspector, prediction, and persistence assertions remain intact. The deliberately undersized fixture yields 24.52-minute baseline, 12.04-minute intervention, 50.90% improvement. Probe also confirmed capacities 4 and 1 behave monotonically (18.28 and 37.26 minutes baseline).

Portable validation compiles actual Domain and Application sources plus every Application EditMode test against Unity NUnit: RED 74/75 passing with only the named Golden test failing at >20 (actual 8.92); GREEN 75/75 passing, zero skipped. Retained checkpoint-golden-red.xml and checkpoint-golden-green.xml. git diff --check passes. No production changes, no Unity runner started, no commit. Root controller handles full native verification.
