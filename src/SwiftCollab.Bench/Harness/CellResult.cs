namespace SwiftCollab.Bench.Harness;

/// <summary>The measured outcome of one case at one input size.</summary>
public sealed record CellResult(
    BenchCase Case,
    int Size,
    double MedianMs,
    int Runs,
    long MedianAllocatedBytes,
    bool Capped,
    double CappedRunMs)
{
    public static CellResult CappedAt(BenchCase c, int size, int runs, double runMs) =>
        new(c, size, double.NaN, runs, 0, true, runMs);
}
