# SwiftCollab refinements

Course 10 capstone (Data Structures and Algorithms) of the Microsoft Full-Stack Developer program. SwiftCollab is the course's scenario: a collaboration platform whose task tree, request queue, report sort and task runner slow down or break as it grows. This repository keeps the four code blocks the course provides, refines each one beside its original, and times both on the same input.

## What changed

- **Binary tree (Part 1):** a self-balancing AVL tree with a search by priority. Tasks that share a priority stay together.
- **Priority queue (Part 2):** a binary min-heap with batch enqueue, safe to share across threads.
- **Sort (Part 3):** an in-place introsort, with a parallel option for large arrays.
- **Task executor (Part 4):** null checks, error handling per task, logging and a simple retry, so one bad task no longer stops the queue.

Each change carries an `AI-suggested` comment in the code that says whether the suggestion was kept or modified, and why.

## Run it

Requires the .NET 10 SDK.

```
dotnet test
dotnet run -c Release --project src/SwiftCollab.Bench
```

The first command runs the test suite. The second times each original against its refinement and prints the tables (about a minute and a half). Add `-- executor-demo` to replay Part 4's course tasks through both executors.

## Results

Measured on an Apple M2 with .NET 10, Release build. Full tables, method and complexity analysis: [docs/evidence/PERFORMANCE-REPORT.md](docs/evidence/PERFORMANCE-REPORT.md).

- **Part 1:** 100,000 priorities inserted in order, 5,802 ms before and 16 ms after. With random priorities the original stays quicker (10 ms against 24 ms).
- **Part 2:** 20,000 requests queued and served, 4,707 ms before and 2.5 ms after.
- **Part 3:** 100,000 integers, 11,370 ms before and 4.6 ms after (2.5 ms with the parallel option).
- **Part 4:** the course's four tasks now end with 2 completed, 1 rejected and 1 failed after retries, where the original stopped after the first. On valid tasks it runs about 5 times slower, the cost of logging.

## AI Assistance Disclosure

Claude Code was the AI assistant for this project. Each course prompt was run as written, and every suggestion is logged with what was kept, changed or rejected and why in [docs/evidence/AI-ASSISTANCE-RECORD.md](docs/evidence/AI-ASSISTANCE-RECORD.md). Each change was reviewed before it was merged. The reflection is in [REFLECTION.md](REFLECTION.md).
