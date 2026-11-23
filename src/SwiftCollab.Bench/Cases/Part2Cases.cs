using SwiftCollab.Bench.Harness;
using SwiftCollab.Core.Part2;
using Given = SwiftCollab.Baseline.Part2;

namespace SwiftCollab.Bench.Cases;

/// <summary>
/// Part 2 workload: enqueue n requests with random priorities 1–100, then dequeue until empty.
/// The framework's PriorityQueue is a reference row only: it has no lock and no first-in, first-out
/// order among equal priorities, so it does less work than the refinement.
/// </summary>
public static class Part2Cases
{
    private const string Workload = "random priorities 1-100";

    public static IReadOnlyList<BenchCase> All =>
    [
        new(2, Workload, "ApiRequestQueue (given)", CaseRole.Original, n =>
        {
            Given.ApiRequest[] requests = Array.ConvertAll(Priorities(n), p => new Given.ApiRequest("/r" + p, p));
            return () =>
            {
                var queue = new Given.ApiRequestQueue();
                foreach (Given.ApiRequest r in requests)
                {
                    queue.Enqueue(r);
                }

                while (queue.Dequeue() is not null)
                {
                }
            };
        }),
        new(2, Workload, "ApiRequestHeap", CaseRole.Refined, n =>
        {
            ApiRequest[] requests = Refined(n);
            return () =>
            {
                var heap = new ApiRequestHeap();
                foreach (ApiRequest r in requests)
                {
                    heap.Enqueue(r);
                }

                while (heap.TryDequeue(out _))
                {
                }
            };
        }),
        new(2, Workload, "ApiRequestHeap, batch enqueue", CaseRole.Refined, n =>
        {
            ApiRequest[] requests = Refined(n);
            return () =>
            {
                var heap = new ApiRequestHeap();
                heap.EnqueueRange(requests);
                while (heap.TryDequeue(out _))
                {
                }
            };
        }),
        new(2, Workload, "PriorityQueue (framework)", CaseRole.Reference, n =>
        {
            ApiRequest[] requests = Refined(n);
            return () =>
            {
                var queue = new PriorityQueue<ApiRequest, int>();
                foreach (ApiRequest r in requests)
                {
                    queue.Enqueue(r, r.Priority);
                }

                while (queue.TryDequeue(out _, out _))
                {
                }
            };
        }),
    ];

    private static ApiRequest[] Refined(int n) =>
        Array.ConvertAll(Priorities(n), p => new ApiRequest("/r" + p, p));

    private static int[] Priorities(int n)
    {
        var rng = new Random(Ladder.Seed);
        return Enumerable.Range(0, n).Select(_ => rng.Next(1, 101)).ToArray();
    }
}
