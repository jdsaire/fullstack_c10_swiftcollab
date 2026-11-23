using Microsoft.Extensions.Logging;
using SwiftCollab.Core.Part4;
using Given = SwiftCollab.Baseline.Part4;

namespace SwiftCollab.Tests.Part4;

public class ResilientTaskExecutorTests
{
    private sealed class ListLogger : ILogger<ResilientTaskExecutor>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }

    private static ResilientTaskExecutor CourseQueue(ListLogger logger, int maxRetries = 2)
    {
        var executor = new ResilientTaskExecutor(logger, maxRetries);
        executor.AddTask("Task 1");
        executor.AddTask(null);
        executor.AddTask("Fail Task");
        executor.AddTask("Task 2");
        return executor;
    }

    [Test]
    public void CourseTasks_QueueSurvivesNullAndFailingTask()
    {
        var logger = new ListLogger();
        ResilientTaskExecutor executor = CourseQueue(logger);

        ExecutionSummary summary = executor.ProcessTasks();

        Assert.Multiple(() =>
        {
            Assert.That(summary, Is.EqualTo(new ExecutionSummary(Completed: 2, Failed: 1, Rejected: 1, Attempts: 5)));
            Assert.That(executor.Pending, Is.Zero);
            Assert.That(logger.Entries.Count(e => e.Message.Contains("Task 2 completed")), Is.EqualTo(1));
        });
    }

    [Test]
    public void CourseTasks_OriginalStopsAtTheNullTask()
    {
        // Documents the defect: the given executor throws on the null task, so "Fail Task" and "Task 2" never run.
        var executor = new Given.TaskExecutor();
        executor.AddTask("Task 1");
        executor.AddTask(null);
        executor.AddTask("Fail Task");
        executor.AddTask("Task 2");

        TextWriter console = Console.Out;
        Console.SetOut(TextWriter.Null);
        try
        {
            Exception? thrown = Assert.Throws<Exception>(executor.ProcessTasks);
            Assert.That(thrown!.Message, Is.EqualTo("Task is null"));
        }
        finally
        {
            Console.SetOut(console);
        }
    }

    [Test]
    public void FailingTask_IsAttemptedRetriesPlusOneTimes()
    {
        int calls = 0;
        var executor = new ResilientTaskExecutor(new ListLogger(), maxRetries: 3, work: _ =>
        {
            calls++;
            throw new TaskExecutionException("always fails");
        });
        executor.AddTask("Fail Task");

        ExecutionSummary summary = executor.ProcessTasks();

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(4));
            Assert.That(summary.Failed, Is.EqualTo(1));
            Assert.That(summary.Attempts, Is.EqualTo(4));
        });
    }

    [Test]
    public void FlakyTask_CompletesOnTheSecondAttempt()
    {
        int calls = 0;
        var logger = new ListLogger();
        var executor = new ResilientTaskExecutor(logger, maxRetries: 2, work: _ =>
        {
            if (++calls == 1)
            {
                throw new TimeoutException("first call times out");
            }
        });
        executor.AddTask("Sync job");

        ExecutionSummary summary = executor.ProcessTasks();

        Assert.Multiple(() =>
        {
            Assert.That(summary, Is.EqualTo(new ExecutionSummary(Completed: 1, Failed: 0, Rejected: 0, Attempts: 2)));
            Assert.That(logger.Entries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("Retrying")), Is.True);
            Assert.That(logger.Entries.Any(e => e.Level == LogLevel.Error), Is.False);
        });
    }

    [Test]
    public void ZeroRetries_FailsAfterOneAttempt()
    {
        var executor = new ResilientTaskExecutor(new ListLogger(), maxRetries: 0);
        executor.AddTask("Fail Task");

        Assert.That(executor.ProcessTasks(), Is.EqualTo(new ExecutionSummary(0, 1, 0, 1)));
    }

    [Test]
    public void EveryFailure_IsLoggedWithItsException()
    {
        var logger = new ListLogger();
        CourseQueue(logger).ProcessTasks();

        Assert.Multiple(() =>
        {
            Assert.That(logger.Entries.Count(e => e.Level == LogLevel.Warning && e.Message.Contains("Rejected")), Is.EqualTo(1));
            Assert.That(logger.Entries.Count(e => e.Level == LogLevel.Warning && e.Message.Contains("Retrying")), Is.EqualTo(2));
            (LogLevel _, string message, Exception? exception) = logger.Entries.Single(e => e.Level == LogLevel.Error);
            Assert.That(message, Does.Contain("Fail Task failed after 3 attempts"));
            Assert.That(exception, Is.InstanceOf<TaskExecutionException>());
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void InvalidTasks_AreRejectedAndNotQueued(string? task)
    {
        var executor = new ResilientTaskExecutor(new ListLogger());

        Assert.Multiple(() =>
        {
            Assert.That(executor.AddTask(task), Is.False);
            Assert.That(executor.Pending, Is.Zero);
            Assert.That(executor.ProcessTasks().Rejected, Is.EqualTo(1));
        });
    }

    [Test]
    public void UnexpectedExceptions_DoNotStopTheQueue()
    {
        var executor = new ResilientTaskExecutor(new ListLogger(), maxRetries: 0, work: task =>
        {
            if (task == "broken")
            {
                throw new InvalidOperationException("not a task failure, a bug");
            }
        });
        executor.AddTask("broken");
        executor.AddTask("after");

        Assert.That(executor.ProcessTasks(), Is.EqualTo(new ExecutionSummary(1, 1, 0, 2)));
    }

    [Test]
    public void NegativeRetries_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResilientTaskExecutor(maxRetries: -1));
    }
}
