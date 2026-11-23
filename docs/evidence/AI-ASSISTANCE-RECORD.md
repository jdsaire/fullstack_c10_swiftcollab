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
