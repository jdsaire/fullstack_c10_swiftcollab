using SwiftCollab.Bench.Harness;
using SwiftCollab.Core.Part1;
using Given = SwiftCollab.Baseline.Part1;

namespace SwiftCollab.Bench.Cases;

/// <summary>
/// Part 1 workload: insert n priorities, then n/10 searches (half hits, half misses).
/// "random" draws priorities from 0..n-1 (so some repeat); "sorted" inserts 0..n-1 in order, the input
/// that turns the original tree into a chain.
/// </summary>
public static class Part1Cases
{
    public static IReadOnlyList<BenchCase> All =>
    [
        Original("random"), Refined("random"),
        Original("sorted"), Refined("sorted"),
    ];

    private static BenchCase Original(string workload) => new(
        1, workload, "BinaryTree (given) + BST descent", CaseRole.Original, n =>
        {
            (int[] priorities, int[] queries) = Input(workload, n);
            return () =>
            {
                var tree = new Given.BinaryTree();
                foreach (int p in priorities)
                {
                    tree.Insert(p);
                }

                int found = 0;
                foreach (int q in queries)
                {
                    if (BaselineSearch.Contains(tree, q))
                    {
                        found++;
                    }
                }

                GC.KeepAlive(found);
            };
        });

    private static BenchCase Refined(string workload) => new(
        1, workload, "AvlTaskTree", CaseRole.Refined, n =>
        {
            (int[] priorities, int[] queries) = Input(workload, n);
            string[] tasks = Array.ConvertAll(priorities, p => "task-" + p);
            return () =>
            {
                var tree = new AvlTaskTree();
                for (int i = 0; i < priorities.Length; i++)
                {
                    tree.Insert(priorities[i], tasks[i]);
                }

                int found = 0;
                foreach (int q in queries)
                {
                    if (tree.Contains(q))
                    {
                        found++;
                    }
                }

                GC.KeepAlive(found);
            };
        });

    private static (int[] Priorities, int[] Queries) Input(string workload, int n)
    {
        var rng = new Random(Ladder.Seed);
        int[] priorities = workload == "sorted"
            ? Enumerable.Range(0, n).ToArray()
            : Enumerable.Range(0, n).Select(_ => rng.Next(n)).ToArray();

        int[] queries = new int[Math.Max(1, n / 10)];
        for (int i = 0; i < queries.Length; i++)
        {
            queries[i] = i % 2 == 0 ? priorities[rng.Next(n)] : n + rng.Next(n); // hit : miss
        }

        return (priorities, queries);
    }
}

/// <summary>
/// Harness helper, not course code: the original tree has no search method, so the bench gives it the
/// plain iterative descent its public Left/Right fields allow. Original and refined then run the same
/// search algorithm, and the difference measured is the shape of the tree.
/// </summary>
internal static class BaselineSearch
{
    public static bool Contains(Given.BinaryTree tree, int value)
    {
        Given.Node? current = tree.Root;
        while (current is not null)
        {
            if (value == current.Value)
            {
                return true;
            }

            current = value < current.Value ? current.Left : current.Right;
        }

        return false;
    }
}
