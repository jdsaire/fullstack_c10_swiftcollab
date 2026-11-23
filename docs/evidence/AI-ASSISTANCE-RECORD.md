# AI Assistance Record · SwiftCollab

**System**: SwiftCollab task and workflow platform (course scenario)
**Author**: Juan Diego S. (`jdsaire`)
**Scope**: Course 10 capstone, Parts 1–4
**Feeds**: submission entries 2–7 (criteria 1–6)

---

## How this record works

Each course part lists prompts under "Request optimizations for". Every prompt was run as written with the AI assistant, one entry per prompt. Each entry records what the assistant suggested, what was kept, changed or rejected, and why. Annotations in the code (`// AI-suggested (kept)` or `(modified)`) point back to these entries.

## Setup notes

### S-01 · Baselines kept as given
- **Date**: 2026-11-22
- **What**: the four course code blocks live in `src/SwiftCollab.Baseline/`, copied from the course pages. A script compares each file with the page's lines: 0 lines differ.
- **Two changes, both outside the course code**: each file gets one namespace line (Parts 2 and 4 both declare a `Program` class, and the baseline type names would clash with the refined ones), and the baseline project turns off nullable warnings (the course code predates them).

### S-02 · Course page notes
- Part 1 shows its "review the following code" sentence twice; Part 2 has no Step 3 heading; Part 4's page title ("Optimizing Data Structures With the LLM") differs from its subject (debugging task execution). The run follows each page's evident intent.
- Indentation in every code block is reconstructed from the page editor; it does not change behaviour.
- Part 2's optimization prompts are typed on the page as lines starting with "- "; they are quoted here without that marker.
- Part 1's block has no `Main` and no `using System;`. It compiles as library code with the project's implicit usings.

### S-03 · Bench helper for Part 1
- The original tree has no search method. The bench gives it the plain iterative descent its public fields allow (`BaselineSearch` in the bench, not in the course code), so original and refined run the same search and the measured difference is the tree's shape.

---

## Part 1 · Binary tree (`src/SwiftCollab.Core/Part1/AvlTaskTree.cs`)

### AI-01 · Analyze the code
- **Prompt**: "Use an LLM to analyze the provided code and recommend optimizations. The following areas should be addressed:"
- **Suggested**: (1) the tree never rebalances, so priorities inserted in order build a chain and every insert walks all earlier nodes, O(n) per insert; (2) there is no search; (3) insert and print are recursive, and the print writes straight to the console; (4) nodes hold a bare integer, so there is nowhere to store the task; (5) an equal priority goes right, so repeats lengthen the tree.
- **Evaluation**: kept as the work list for AI-02 to AI-05. Finding (1) was checked with a test (1,000 sorted inserts give a chain 1,000 deep).

### AI-02 · Tree balancing
- **Prompt**: "Tree Balancing: Ensure that insertions do not lead to an unbalanced tree, which can degrade search performance."
- **Suggested**: an AVL tree with a stored height per node and the four rotation cases, sending an equal priority right as the original does. Named alternatives: a red-black tree, or the framework's `SortedDictionary`.
- **Decision**: AVL **kept**. The equal-priority rule **changed**: after rotations, equal priorities can end up on both sides of a node and a search would find only one, so each priority gets one node holding all its tasks. Red-black tree **rejected**: more cases to get right for the same O(log n) bound, and AVL's stricter balance suits a lookup-heavy queue. `SortedDictionary` **rejected as unnecessary**: it would replace the structure the activity asks to refine.

### AI-03 · Search functionality
- **Prompt**: "Search Functionality: Implement an efficient search method to retrieve tasks based on priority."
- **Suggested**: an iterative `TryFind(priority)` that walks one path from the root; a method returning the highest-priority entry; a cache of the last node found.
- **Decision**: `TryFind` and `TryGetHighestPriority` **kept** (loops, no recursion, no allocation). The cache **rejected as unnecessary**: the scenario shows no repeated lookups of one priority, and the cache would be extra state to keep correct on every insert.

### AI-04 · Recursive optimization
- **Prompt**: "Recursive Optimization: Reduce redundant recursive calls to improve efficiency."
- **Suggested**: make insert iterative with an explicit stack of parents; make the in-order walk iterative; have insert return each subtree's new root, removing the original's repeated null checks.
- **Decision**: iterative walk and subtree-returning insert **kept**. Fully iterative insert **changed**: insert stays recursive because a balanced tree is at most about 1.44·log2(n) deep (about 25 calls at 100,000 priorities), so call depth is no longer a risk and the rebalancing reads more clearly.

### AI-05 · Memory and performance
- **Prompt**: "Memory and Performance Enhancements: Identify and apply improvements to optimize memory usage and execution time."
- **Suggested**: (1) one node per priority with a task list; (2) stop rebalancing when an insert only adds a task to an existing priority; (3) return entries from the walk instead of printing; (4) store the height in a byte to shrink each node.
- **Decision**: (1) and (3) **kept**. (2) **changed**: the node with the repeated priority returns at once, but the levels above it still recompute their heights (a constant step per level). (4) **rejected as inaccurate**: on 64-bit .NET an object's size rounds up to 8 bytes, and the priority and height (4 bytes each) already share one 8-byte slot, so a byte height leaves the node the same size.
- **Note for the report**: the refined node also stores the task text and a list, so it allocates more memory per insert than the original's bare integer node. The bench reports allocated bytes beside time.

---

## Part 2 · Priority queue (`src/SwiftCollab.Core/Part2/ApiRequestHeap.cs`)

### AI-06 · Analyze the queue
- **Prompt**: "Prompt an LLM (e.g., Microsoft Copilot) to analyze the provided priority queue implementation and suggest improvements."
- **Suggested**: (1) every enqueue re-sorts the whole list, O(n log n) per insert; (2) every dequeue calls `RemoveAt(0)`, which shifts all remaining requests, O(n); (3) `List.Sort` is not stable, so requests with equal priority can leave out of arrival order; (4) the list is not safe to share across threads; (5) `Priority` has a public setter, so a queued request can change priority and break the order; (6) dequeue returns null on an empty queue, which callers can miss.
- **Evaluation**: all six **kept** as the work list. (5) became an immutable `ApiRequest` record; (3) became an arrival number that breaks ties; (6) became `TryDequeue`, with `Dequeue()` kept for callers that expect null.

### AI-07 · Replace List.Sort with a min-heap
- **Prompt**: "Replacing List.Sort() with a Min-Heap (Binary Heap Implementation) to improve efficiency."
- **Suggested**: an array-backed binary min-heap with sift-up and sift-down; alternatively, use the framework's `PriorityQueue<TElement, TPriority>`.
- **Decision**: own heap **kept** (the activity asks for the heap itself). Framework queue **rejected for the implementation** and kept as a reference row in the bench: it has no lock and no first-in, first-out order among equal priorities, so it is not a like-for-like replacement.

### AI-08 · Insertion from O(n log n) to O(log n)
- **Prompt**: "Reducing insertion complexity from O(n log n) to O(log n) by using a binary heap."
- **Suggested**: append at the end of the array and sift up; alternatively, keep the list sorted and insert at the slot found by binary search.
- **Decision**: sift-up **kept** (at most log2(n) moves). Binary-search insertion **rejected as inaccurate for the goal**: finding the slot is O(log n), but inserting into a list shifts every later element, so the insert stays O(n).

### AI-09 · Dequeue from O(n) to O(log n)
- **Prompt**: "Refactoring the Dequeue operation to run in O(log n) instead of O(n)."
- **Suggested**: take the root, move the last entry to the top, sift it down; alternatively, keep the list sorted in reverse and remove from the end.
- **Decision**: sift-down **kept**. Reverse-sorted list **rejected**: dequeue becomes O(1), but every enqueue still sorts, so the original bottleneck stays.

### AI-10 · Batch enqueue
- **Prompt**: "Adding bulk processing support (batch enqueue)."
- **Suggested**: `EnqueueRange` that takes the lock and resizes the array once, then rebuilds the heap bottom-up in O(n + k).
- **Decision**: **changed**: one lock and one resize kept; the bottom-up rebuild is used only when the batch is at least as large as the queue. For a small batch into a large queue, each new request is sifted up (O(k log n)), which is cheaper than rebuilding everything.

### AI-11 · Thread safety
- **Prompt**: "Implementing thread-safe request handling for concurrent processing."
- **Suggested**: (1) one lock around every operation, using .NET's `Lock` type; (2) a reader-writer lock so `Count` and `TryPeek` can run in parallel; (3) a lock-free design with one concurrent queue per priority level.
- **Decision**: (1) **kept**. (2) **rejected as unnecessary**: those reads take nanoseconds, and a plain lock is simpler and cheaper when uncontended. (3) **rejected as unnecessary**: harder to get right, and it limits priorities to a fixed set of levels; beyond a learner-level refinement.
- **Verified by**: two concurrency tests (4 producers enqueuing batches while 4 consumers dequeue, 40,000 requests; 8 threads enqueuing 40,000 single requests): no request lost or duplicated, order intact.

---

## Part 3 · Sort (`src/SwiftCollab.Core/Part3/IntroSorter.cs`)

### AI-12 · Analyze the sort
- **Prompt**: "Prompt an LLM (e.g., Microsoft Copilot) to analyze the provided sorting algorithm and identify inefficiencies."
- **Suggested**: (1) two nested loops run in full whatever the input, O(n²) comparisons even on a sorted array; (2) a quick fix: stop when a pass makes no swap; (3) the sort already works in place, O(1) extra memory, which any replacement should keep.
- **Evaluation**: (1) and (3) **kept**. (2) **rejected as unnecessary**: it only helps input that is already nearly sorted and leaves the O(n²) worst case, and the algorithm is being replaced anyway.

### AI-13 · Replace bubble sort
- **Prompt**: "Replacing Bubble Sort with a more efficient sorting algorithm such as Quick Sort or Merge Sort."
- **Suggested**: merge sort (stable, O(n log n) in every case) or quicksort; alternatively, call the framework's `Array.Sort`.
- **Decision**: quicksort **kept**. Merge sort **rejected**: it needs a second buffer as large as the input, which conflicts with the in-place requirement of AI-15, and stability adds nothing when sorting plain integers. `Array.Sort` **rejected for the implementation** (the activity asks for the refactor itself) and kept as a reference row in the bench.

### AI-14 · O(n²) to O(n log n)
- **Prompt**: "Reducing time complexity from O(n²) to O(n log n)."
- **Suggested**: quicksort, described as O(n log n); insertion sort for small partitions.
- **Decision**: **changed**: quicksort is O(n log n) on average but O(n²) in its worst case, so the description was inaccurate as stated. The refined sort switches to heapsort when partitioning goes deeper than 2·log2(n) (introsort), which keeps the worst case at O(n log n). Insertion sort for partitions of 16 or fewer items **kept**.

### AI-15 · In-place sorting
- **Prompt**: "Optimizing space complexity by using an in-place sorting method if applicable."
- **Suggested**: partition inside the array around a median-of-three pivot; recurse into the smaller side and loop on the larger one.
- **Decision**: both **kept**. The call stack stays at most log2(n) deep, and the partition stops on equal values, so duplicate-heavy arrays still split near the middle.

### AI-16 · Parallel sorting
- **Prompt**: "Supporting parallel sorting to improve performance for large datasets."
- **Suggested**: sort the two sides of each partition at the same time with `Parallel.Invoke`; alternatively, PLINQ's `OrderBy`.
- **Decision**: `Parallel.Invoke` **changed**: the suggestion forked at every level; the refined sort forks only for ranges of at least 65,536 items and only for the first log2(cores) + 1 levels, so task overhead stays small. PLINQ **rejected**: it builds a new sorted sequence instead of sorting in place, which undoes AI-15.
