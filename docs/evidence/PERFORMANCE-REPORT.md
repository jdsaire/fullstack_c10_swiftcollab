# Performance Report · SwiftCollab

Each course original timed against its refinement on the same input.

**Machine and method**: Apple M2 (8 cores) · 16 GB · macOS 26.6.2 · .NET 10.0.5 · Release build, full JIT optimization from the first call · Stopwatch, one warm-up run per method, then the median of 5 runs (3 when a run passes 2 s) · 15 s cap per run · seeded inputs (seed 42), identical for original and refined · measured 22 Nov 2026.
**Badges**: **[M]** measured on this machine · **[SIM]** extrapolated, with its formula · Big-O lines are **analysis**, not measurements.
**Raw cells**: [`bench-results.csv`](bench-results.csv) · reproduce with `dotnet run -c Release --project src/SwiftCollab.Bench`.
**Allocated** = memory allocated during the timed run (all threads), median.

---

## Part 1 · Binary tree → AVL tree

| | Insert | Search |
|---|---|---|
| Original (analysis) | O(log n) on random input, **O(n) on sorted input** (the tree becomes a chain) | none in the given code |
| Refined (analysis) | O(log n) in every case | O(log n), iterative |

Workload: insert n priorities, then n/10 searches (half hits). The original is searched with a plain descent written in the bench (the given code has none).

| Input | Size | Original (ms) | Refined (ms) | Original (KB) | Refined (KB) |
|---|---:|---:|---:|---:|---:|
| sorted | 1,000 | 0.557 [M] | 0.104 [M] | 40 | 110 |
| sorted | 10,000 | 61.80 [M] | 1.12 [M] | 391 | 1,094 |
| sorted | 30,000 | 519 [M] | 3.65 [M] | 1,173 | 3,282 |
| sorted | 60,000 | 2,071 [M] | 7.64 [M] | 2,344 | 6,563 |
| sorted | 100,000 | 5,802 [M] | 16.03 [M] | 3,907 | 10,938 |
| random | 1,000 | 0.047 [M] | 0.119 [M] | 40 | 86 |
| random | 10,000 | 0.586 [M] | 1.48 [M] | 391 | 843 |
| random | 30,000 | 2.28 [M] | 5.13 [M] | 1,173 | 2,530 |
| random | 60,000 | 5.44 [M] | 11.15 [M] | 2,344 | 5,057 |
| random | 100,000 | 10.38 [M] | 24.22 [M] | 3,907 | 8,422 |

**Reading**: on sorted input the original degrades and the AVL tree does not. On random input the original happens to stay shallow and is about 2.3 times quicker, since the AVL tree pays for rebalancing and stores task text; it also allocates about 2 to 3 times the memory. Balancing buys a worst-case guarantee, not a win on every input.

## Part 2 · Sort-on-insert list → binary min-heap

| | Enqueue | Dequeue |
|---|---|---|
| Original (analysis) | O(n log n): sorts the whole list | O(n): `RemoveAt(0)` shifts every item |
| Refined (analysis) | O(log n); batch O(n + k) or O(k log n) | O(log n) |

Workload: enqueue n requests (priorities 1–100), then dequeue all.

| Size | Original (ms) | Heap (ms) | Heap, batch (ms) | Framework `PriorityQueue`, reference (ms) |
|---:|---:|---:|---:|---:|
| 1,000 | 9.10 [M] | 0.099 [M] | 0.088 [M] | 0.064 [M] |
| 2,000 | 37.69 [M] | 0.203 [M] | 0.180 [M] | 0.125 [M] |
| 5,000 | 260 [M] | 0.543 [M] | 0.477 [M] | 0.336 [M] |
| 10,000 | 1,108 [M] | 1.16 [M] | 1.03 [M] | 0.731 [M] |
| 20,000 | 4,707 [M] | 2.48 [M] | 2.23 [M] | 1.54 [M] |

**Reading**: the heap removes the original's growth curve. The framework queue is quicker still: it takes no lock and does not keep equal priorities in arrival order, which the refined heap does; it is a reference row, not the submitted refinement. Memory at 10,000: original 257 KB, heap 769 KB, heap with batch enqueue 314 KB [M].

## Part 3 · Bubble sort → introsort

| | Time | Extra memory |
|---|---|---|
| Original (analysis) | O(n²) in every case | O(1), in place |
| Refined (analysis) | O(n log n), worst case included (heapsort fallback) | O(log n) call stack, in place |

Workload: n random integers.

| Size | Original (ms) | Refined (ms) | Refined, parallel (ms) | Framework `Array.Sort`, reference (ms) |
|---:|---:|---:|---:|---:|
| 1,000 | 0.448 [M] | 0.036 [M] | 0.031 [M] | 0.032 [M] |
| 10,000 | 43.02 [M] | 0.370 [M] | 0.355 [M] | 0.368 [M] |
| 50,000 | 2,641 [M] | 2.10 [M] | 2.06 [M] | 2.12 [M] |
| 100,000 | 11,370 [M] | 4.55 [M] | 2.49 [M] | 4.45 [M] |
| 500,000 | not run | 25.43 [M] | 7.52 [M] | 25.07 [M] |
| 1,000,000 | not run | 52.34 [M] | 18.82 [M] | 53.60 [M] |

**Extrapolated, not measured** (the original is not run above 100,000; formula t(n) ≈ t(100,000) × (n / 100,000)², from O(n²)):

| Size | Original (ms) |
|---:|---:|
| 500,000 | ≈ 284,000 [SIM] |
| 1,000,000 | ≈ 1,137,000 [SIM] (about 19 minutes) |

**Reading**: the parallel option starts at 65,536 items, so below that it runs the sequential sort (same figures). From 100,000 it uses several cores. All sorts allocate under 8 KB: they work inside the array.

## Part 4 · Task executor → resilient executor

| | Original | Refined |
|---|---|---|
| Course queue (Task 1, null, "Fail Task", Task 2) [M] | completes 1 task, then stops on an unhandled exception | completes 2, rejects 1 (null), fails 1 after 3 attempts, logs each |
| Complexity (analysis) | O(n) | O(n · (retries + 1)) in the worst case |

Workload for timing: n valid tasks (the original cannot run a queue holding a null or failing task). Output goes to a null writer for both.

| Size | Original (ms) | Refined (ms) | Original (KB) | Refined (KB) |
|---:|---:|---:|---:|---:|
| 1,000 | 0.067 [M] | 0.314 [M] | 181 | 330 |
| 10,000 | 0.597 [M] | 3.02 [M] | 2,039 | 3,453 |
| 100,000 | 6.56 [M] | 31.24 [M] | 20,003 | 34,776 |

**Reading**: the refinement is about 5 times slower on valid tasks: structured log messages cost more than a console line. Its gain is that the queue finishes. This part is measured as a stability change, not a speed change.

---

## Method notes

- A first sweep used .NET's default tiered JIT. It timed two identical code paths (the parallel sort below its threshold runs the sequential sort) about 3 times apart, because the method timed first ran partly on unoptimized code. The bench now runs with full JIT optimization from the first call, and this report uses the second sweep. The first sweep is kept in the run records.
- Medians come from one sweep; run-to-run spread on this machine was not measured separately.
