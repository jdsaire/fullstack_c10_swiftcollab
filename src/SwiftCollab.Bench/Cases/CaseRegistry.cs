using SwiftCollab.Bench.Harness;

namespace SwiftCollab.Bench.Cases;

/// <summary>Every bench case, by part. Each part adds its cases when its refinement lands.</summary>
public static class CaseRegistry
{
    public static IReadOnlyList<BenchCase> ForPart(int part) => part switch
    {
        _ => [],
    };
}
