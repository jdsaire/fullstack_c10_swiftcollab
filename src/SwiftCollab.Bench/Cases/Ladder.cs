namespace SwiftCollab.Bench.Cases;

/// <summary>
/// Input sizes per part, set from the preflight timing of the original sort (22 Nov 2026): the original
/// bubble sort took about 11 s at 100,000 items, so sizes above that would only hit the cap.
/// </summary>
public static class Ladder
{
    public const int Seed = 42;

    public static readonly IReadOnlyDictionary<int, int[]> Sizes = new Dictionary<int, int[]>
    {
        [1] = [1_000, 10_000, 30_000, 60_000, 100_000],
        [2] = [1_000, 2_000, 5_000, 10_000, 20_000],
        [3] = [1_000, 10_000, 50_000, 100_000, 500_000, 1_000_000],
        [4] = [1_000, 10_000, 100_000],
    };

    public static readonly IReadOnlyDictionary<int, string> Titles = new Dictionary<int, string>
    {
        [1] = "Binary tree: insert, then search",
        [2] = "Priority queue: enqueue all, then dequeue all",
        [3] = "Sort: random integers",
        [4] = "Task executor: all-valid queue",
    };
}
