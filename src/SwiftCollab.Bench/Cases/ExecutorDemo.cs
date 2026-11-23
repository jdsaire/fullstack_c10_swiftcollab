using Microsoft.Extensions.Logging;
using SwiftCollab.Core.Part4;
using Given = SwiftCollab.Baseline.Part4;

namespace SwiftCollab.Bench.Cases;

/// <summary>
/// Runs the course's four tasks (Task 1, null, "Fail Task", Task 2) through the original and the refined
/// executor and prints what each one did.
/// </summary>
public static class ExecutorDemo
{
    private static readonly string?[] CourseTasks = ["Task 1", null, "Fail Task", "Task 2"];

    public static void Run()
    {
        Console.WriteLine("=== Original TaskExecutor (given) ===");
        var original = new Given.TaskExecutor();
        foreach (string? task in CourseTasks)
        {
            original.AddTask(task);
        }

        try
        {
            original.ProcessTasks();
        }
        catch (Exception ex)
        {
            // Caught here only so the demo can continue; in the course program this exception ends the run.
            Console.WriteLine($"Unhandled {ex.GetType().Name}: {ex.Message} (the course program stops here)");
        }

        Console.WriteLine();
        Console.WriteLine("=== Refined ResilientTaskExecutor (2 retries) ===");
        ExecutionSummary summary;
        using (ILoggerFactory factory = LoggerFactory.Create(builder => builder
                   .SetMinimumLevel(LogLevel.Information)
                   .AddSimpleConsole(options =>
                   {
                       options.SingleLine = true;
                       options.IncludeScopes = false;
                   })))
        {
            var refined = new ResilientTaskExecutor(factory.CreateLogger<ResilientTaskExecutor>());
            foreach (string? task in CourseTasks)
            {
                refined.AddTask(task);
            }

            summary = refined.ProcessTasks();
        }

        Console.WriteLine();
        Console.WriteLine($"Summary: {summary.Completed} completed · {summary.Failed} failed · {summary.Rejected} rejected · {summary.Attempts} attempts");
    }
}
