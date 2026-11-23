using SwiftCollab.Bench.Harness;
using SwiftCollab.Core.Part3;
using Given = SwiftCollab.Baseline.Part3;

namespace SwiftCollab.Bench.Cases;

/// <summary>
/// Part 3 workload: sort n random integers. The original bubble sort stops at 100,000 items: the
/// preflight timing measured about 11 s there and about 48 s at 200,000, so larger sizes would only hit
/// the cap. Array.Sort is a reference row only.
/// </summary>
public static class Part3Cases
{
    private const string Workload = "random integers";
    private const int BubbleMaxSize = 100_000;

    public static IReadOnlyList<BenchCase> All =>
    [
        new(3, Workload, "BubbleSort (given)", CaseRole.Original, n => Sorting(n, Given.Sorting.BubbleSort), BubbleMaxSize),
        new(3, Workload, "IntroSorter.Sort", CaseRole.Refined, n => Sorting(n, IntroSorter.Sort)),
        new(3, Workload, "IntroSorter.ParallelSort", CaseRole.Refined, n => Sorting(n, a => IntroSorter.ParallelSort(a))),
        new(3, Workload, "Array.Sort (framework)", CaseRole.Reference, n => Sorting(n, Array.Sort)),
    ];

    private static Action Sorting(int n, Action<int[]> sort)
    {
        var rng = new Random(Ladder.Seed);
        int[] data = new int[n];
        for (int i = 0; i < n; i++)
        {
            data[i] = rng.Next();
        }

        return () => sort(data);
    }
}
