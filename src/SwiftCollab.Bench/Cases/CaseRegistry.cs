using SwiftCollab.Bench.Harness;

namespace SwiftCollab.Bench.Cases;

/// <summary>Every bench case, by part. Each part adds its cases when its refinement lands.</summary>
public static class CaseRegistry
{
    public static IReadOnlyList<BenchCase> ForPart(int part) => part switch
    {
        1 => Part1Cases.All,
        2 => Part2Cases.All,
        _ => [],
    };
}
