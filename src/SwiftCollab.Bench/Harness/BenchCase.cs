namespace SwiftCollab.Bench.Harness;

/// <summary>
/// One timed method on one workload. <see cref="Prepare"/> builds fresh input for size n outside the
/// timed region and returns the action the Stopwatch times. Original and refined cases built from the
/// same seed receive identical input. <see cref="MaxSize"/> keeps a slow method off sizes known to pass the
/// cap, since the cap can only be checked after a run ends.
/// </summary>
public sealed record BenchCase(
    int Part,
    string Workload,
    string Method,
    CaseRole Role,
    Func<int, Action> Prepare,
    int MaxSize = int.MaxValue);

/// <summary>Whether a case is the course original, the refinement, or a framework reference row.</summary>
public enum CaseRole
{
    Original,
    Refined,
    Reference,
}
