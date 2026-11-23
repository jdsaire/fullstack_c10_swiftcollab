using SwiftCollab.Bench.Cases;
using SwiftCollab.Bench.Harness;

namespace SwiftCollab.Bench;

/// <summary>
/// Stopwatch bench: times each course original against its refinement across input sizes.
/// <code>
/// dotnet run -c Release --project src/SwiftCollab.Bench                      full sweep, all parts
/// dotnet run -c Release --project src/SwiftCollab.Bench -- --part 1 --size 30000   one quick row
/// dotnet run -c Release --project src/SwiftCollab.Bench -- --smoke           smallest size per part
/// dotnet run -c Release --project src/SwiftCollab.Bench -- --write docs/evidence   sweep + files
/// </code>
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (!Machine.IsOptimizedBuild(
                typeof(Program).Assembly,
                typeof(SwiftCollab.Baseline.Part3.Sorting).Assembly))
        {
            Console.Error.WriteLine("Timings are only reported from a Release build: add -c Release.");
            return 2;
        }

        int? part = IntArg(args, "--part");
        int? size = IntArg(args, "--size");
        bool smoke = args.Contains("--smoke");
        string? writeDir = StringArg(args, "--write");

        var settings = new RunnerSettings();
        Console.WriteLine(Machine.Describe(settings));
        Console.WriteLine();

        int[] parts = part is { } p ? [p] : [1, 2, 3, 4];
        var all = new List<CellResult>();
        foreach (int current in parts)
        {
            IReadOnlyList<BenchCase> cases = CaseRegistry.ForPart(current);
            if (cases.Count == 0)
            {
                Console.WriteLine($"Part {current}: no cases yet.");
                continue;
            }

            int[] ladder = size is { } s ? [s] : smoke ? [Ladder.Sizes[current][0]] : Ladder.Sizes[current];
            List<CellResult> cells = Runner.Sweep(cases, ladder, settings);
            all.AddRange(cells);
            Console.WriteLine(Report.Table(current, Ladder.Titles[current], cells));
        }

        if (writeDir is not null)
        {
            Directory.CreateDirectory(writeDir);
            File.WriteAllText(Path.Combine(writeDir, "bench-results.csv"), Report.Csv(all));
            Console.WriteLine($"Raw cells written to {Path.Combine(writeDir, "bench-results.csv")}");
        }

        return 0;
    }

    private static int? IntArg(string[] args, string name) =>
        StringArg(args, name) is { } value && int.TryParse(value, out int n) ? n : null;

    private static string? StringArg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
