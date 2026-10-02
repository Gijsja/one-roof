# Elevator arrival presentation — 2026-10-02

Reported symptom: Residents appeared to walk through multiple floors.

Reproduced in a production-presenter PlayMode test: queued on floor 0, hidden while riding, then walking on floor 4. Before the fix, the view's Y was -3.422892 rather than the destination hallway's 3.22. Hidden riders retained the departure Transform and walking used three-dimensional MoveTowards.

Walking now interpolates only X. Y and depth always match the projected hallway; a floor change also resets X to the destination position so stale or pooled views cannot traverse the old hallway. Simulation routing and elevator rules are unchanged.

Connected Unity 6000.3.24f1 compiled successfully, with no Console errors. Focused graphical PlayMode validation: **3 passed, 0 failed** (arrival regression, actual elevator cycle/floor expansion, hallway frontage/fade). The arrival regression failed before the fix and passed afterward, and checks that same-floor horizontal interpolation remains smooth. No full-suite or performance certification is claimed. The optional dotnet check could not complete because generated restore assets were missing; Unity compilation and runtime tests provide the validation above.
