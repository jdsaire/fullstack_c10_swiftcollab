using SwiftCollab.Bench.Harness;
using SwiftCollab.Core.Part4;
using Given = SwiftCollab.Baseline.Part4;

namespace SwiftCollab.Bench.Cases;

/// <summary>
/// Part 4 workload: n valid tasks. The original cannot run a queue that holds a null or failing task (it
/// throws and stops), so timing uses valid tasks only; robustness is shown by <c>executor-demo</c>.
/// </summary>
public static class Part4Cases
{
    private const string Workload = "all-valid queue";

    public static IReadOnlyList<BenchCase> All =>
    [
        new(4, Workload, "TaskExecutor (given)", CaseRole.Original, n =>
        {
            string[] tasks = Tasks(n);
            return () =>
            {
                TextWriter console = Console.Out;
                Console.SetOut(TextWriter.Null);
                try
                {
                    var executor = new Given.TaskExecutor();
                    foreach (string t in tasks)
                    {
                        executor.AddTask(t);
                    }

                    executor.ProcessTasks();
                }
                finally
                {
                    Console.SetOut(console);
                }
            };
        }),
        new(4, Workload, "ResilientTaskExecutor", CaseRole.Refined, n =>
        {
            string[] tasks = Tasks(n);
            var logger = new NullWriterLogger<ResilientTaskExecutor>();
            return () =>
            {
                var executor = new ResilientTaskExecutor(logger);
                foreach (string t in tasks)
                {
                    executor.AddTask(t);
                }

                executor.ProcessTasks();
            };
        }),
    ];

    private static string[] Tasks(int n) => Enumerable.Range(1, n).Select(i => $"Task {i}").ToArray();
}
