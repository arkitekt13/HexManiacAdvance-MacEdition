# Mixing-bag pointer and Core review

Build/run instructions and findings: [BLOCK_BAG_REVIEW.md](../../tests/BLOCK_BAG_REVIEW.md).

The executable uses fabricated map bytes only. It verifies 17 controls including the repaired
preserve-mode fill. A failed preservation check now fails the executable; the historical
failure is retained in the linked report. This real-control review remains outside the normal
Mac regression runner; tests/BlockBagFill supplies the automated fill/history matrix.
