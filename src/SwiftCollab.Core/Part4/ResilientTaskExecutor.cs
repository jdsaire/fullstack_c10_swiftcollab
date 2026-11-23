using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SwiftCollab.Core.Part4;

/// <summary>What one call to <see cref="ResilientTaskExecutor.ProcessTasks"/> did.</summary>
/// <param name="Completed">Tasks that ran successfully, on any attempt.</param>
/// <param name="Failed">Tasks that still failed after every retry.</param>
/// <param name="Rejected">Inputs refused when added (null or blank) since the last run.</param>
/// <param name="Attempts">Executions in total, retries included.</param>
public sealed record ExecutionSummary(int Completed, int Failed, int Rejected, int Attempts);

// AI-suggested (kept): a specific exception type for a task that fails. Why: the log and the tests can
// tell an expected task failure apart from a bug, where the original threw a bare Exception for both.
/// <summary>A task reported that it could not complete.</summary>
public sealed class TaskExecutionException(string message) : Exception(message);

/// <summary>
/// Refinement of the course's <c>TaskExecutor</c> (Part 4): invalid input is refused when added, each task
/// runs inside its own error handling with a simple retry, and every outcome is logged.
/// </summary>
/// <remarks>
/// Original: a null task or a failing task throws out of <c>ProcessTasks</c>, which ends the program and
/// leaves every later task unprocessed; messages go straight to the console.
/// </remarks>
public sealed class ResilientTaskExecutor
{
    private readonly Queue<string> _queue = new();
    private readonly ILogger _logger;
    private readonly int _maxRetries;
    private readonly Action<string> _work;
    private int _rejected;

    /// <param name="logger">Where outcomes are logged; nothing is logged when omitted.</param>
    /// <param name="maxRetries">Extra attempts after a first failure (0 = no retry).</param>
    /// <param name="work">The task itself; defaults to the course's behaviour (<see cref="RunCourseTask"/>).</param>
    public ResilientTaskExecutor(ILogger<ResilientTaskExecutor>? logger = null, int maxRetries = 2, Action<string>? work = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        _logger = logger ?? NullLogger<ResilientTaskExecutor>.Instance;
        _maxRetries = maxRetries;
        _work = work ?? RunCourseTask;
    }

    public int Pending => _queue.Count;

    // AI-suggested (kept): the null check happens when a task is added. Why: invalid input is refused once,
    // at the door, with a warning in the log, so it never reaches the queue and cannot stop it later.
    // Blank names are refused too, since they carry no task.
    public bool AddTask(string? task)
    {
        if (string.IsNullOrWhiteSpace(task))
        {
            _rejected++;
            _logger.LogWarning("Rejected a task with no name (null or blank); it was not queued.");
            return false;
        }

        _queue.Enqueue(task);
        return true;
    }

    public ExecutionSummary ProcessTasks()
    {
        int completed = 0;
        int failed = 0;
        int attempts = 0;

        while (_queue.TryDequeue(out string? task))
        {
            if (Execute(task, ref attempts))
            {
                completed++;
            }
            else
            {
                failed++;
            }
        }

        var summary = new ExecutionSummary(completed, failed, _rejected, attempts);
        _rejected = 0;
        _logger.LogInformation(
            "Queue finished: {Completed} completed, {Failed} failed, {Rejected} rejected, {Attempts} attempts.",
            summary.Completed, summary.Failed, summary.Rejected, summary.Attempts);
        return summary;
    }

    // AI-suggested (modified): each task runs inside its own try/catch and is retried up to the configured
    // count, one attempt after another, with no waiting and no threads. Why: one failing task no longer ends
    // the run; the queue moves on to the next task. Modified: the suggestion waited longer before each retry
    // (exponential backoff); on a single queue every wait also delays the tasks behind it, so retries run
    // at once and the count stays small.
    private bool Execute(string task, ref int attempts)
    {
        for (int attempt = 1; attempt <= _maxRetries + 1; attempt++)
        {
            attempts++;
            _logger.LogDebug("Processing task {Task} (attempt {Attempt}).", task, attempt);
            try
            {
                _work(task);
                _logger.LogInformation("Task {Task} completed (attempt {Attempt}).", task, attempt);
                return true;
            }

            // AI-suggested (modified): catch any exception the task throws, not only TaskExecutionException.
            // Why: whatever goes wrong inside one task, the rest of the queue still runs. Modified: the
            // suggestion caught the custom exception alone, which would let any other error stop the queue
            // just as the original did.
            catch (Exception ex)
            {
                if (attempt <= _maxRetries)
                {
                    _logger.LogWarning("Task {Task} failed on attempt {Attempt}: {Reason} Retrying.", task, attempt, ex.Message);
                }
                else
                {
                    // AI-suggested (kept): the log records the failure with its exception. Why: errors are
                    // tracked with their level, task name and stack trace, where the original printed a line.
                    _logger.LogError(ex, "Task {Task} failed after {Attempts} attempts; moving on.", task, attempt);
                }
            }
        }

        return false;
    }

    /// <summary>The course's task behaviour: any task whose name contains "Fail" fails.</summary>
    public static void RunCourseTask(string task)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.Contains("Fail", StringComparison.Ordinal))
        {
            throw new TaskExecutionException($"Task '{task}' failed.");
        }
    }
}
