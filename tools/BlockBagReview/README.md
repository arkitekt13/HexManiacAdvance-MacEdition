# Mixing-bag pointer and Core review

Build/run instructions and findings: [BLOCK_BAG_REVIEW.md](../../tests/BLOCK_BAG_REVIEW.md).

The executable uses fabricated map bytes only. It verifies 16 positive controls and records a
separate known failed preserve-mode fill characterization. Exit success means the review
completed; it does not declare the printed FLOOD_FILL_PRESERVES_MOVEMENT:False result correct.
This is investigation tooling outside the normal Mac regression runner.
