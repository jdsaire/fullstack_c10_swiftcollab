using System.Diagnostics;

namespace SwiftCollab.Bench.Harness;

/// <summary>Run counts and limits for one sweep.</summary>
public sealed class RunnerSettings
{
    /// <summary>Runs per cell when a single run is quick.</summary>
    public int Runs { get; init; } = 5;

    /// <summary>Runs per cell when the first run is slower than <see cref="SlowRun"/>.</summary>
    public int ReducedRuns { get; init; } = 3;

    public TimeSpan SlowRun { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>A run longer than this drops the size for that method; larger sizes are skipped.</summary>
    public TimeSpan Cap { get; init; } = TimeSpan.FromSeconds(15);
}

/// <summary>Stopwatch measurement: warm-up, fresh input per run, median of the runs.</summary>
public static class Runner
{
    /// <summary>One untimed run so the JIT has compiled the method before measuring.</summary>
    public static void WarmUp(BenchCase benchCase, int size) => benchCase.Prepare(size)();

    public static CellResult Measure(BenchCase benchCase, int size, RunnerSettings settings)
    {
        var times = new List<double>(settings.Runs);
        var allocations = new List<long>(settings.Runs);
        int planned = settings.Runs;

        for (int run = 0; run < planned; run++)
        {
            Action work = benchCase.Prepare(size);

            // Start each run from a collected heap so one run's garbage is not billed to the next.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            long start = Stopwatch.GetTimestamp();
            work();
            TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
            long allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);

            if (elapsed > settings.Cap)
            {
                return CellResult.CappedAt(benchCase, size, run + 1, elapsed.TotalMilliseconds);
            }

            times.Add(elapsed.TotalMilliseconds);
            allocations.Add(allocatedAfter - allocatedBefore);

            if (run == 0 && elapsed > settings.SlowRun)
            {
                planned = settings.ReducedRuns;
            }
        }

        return new CellResult(
            benchCase,
            size,
            Median(times),
            times.Count,
            (long)Median(allocations.ConvertAll(a => (double)a)),
            Capped: false,
            CappedRunMs: double.NaN);
    }

    /// <summary>
    /// Every method in the list, every size in ascending order. A capped size stops that method's ladder,
    /// since larger inputs would only take longer.
    /// </summary>
    public static List<CellResult> Sweep(
        IEnumerable<BenchCase> cases,
        IReadOnlyList<int> sizes,
        RunnerSettings settings,
        Action<CellResult>? onCell = null)
    {
        var results = new List<CellResult>();
        foreach (BenchCase benchCase in cases)
        {
            // Sizes run in ascending order; a method never runs, warm-up included, above its size limit.
            int[] allowed = sizes.Where(size => size <= benchCase.MaxSize).ToArray();
            if (allowed.Length == 0)
            {
                continue;
            }

            WarmUp(benchCase, allowed[0]);
            foreach (int size in allowed)
            {

                CellResult cell = Measure(benchCase, size, settings);
                results.Add(cell);
                onCell?.Invoke(cell);
                if (cell.Capped)
                {
                    break;
                }
            }
        }

        return results;
    }

    private static double Median(List<double> values)
    {
        values.Sort();
        int mid = values.Count / 2;
        return values.Count % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2.0;
    }
}
