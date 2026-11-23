using System.Collections.Concurrent;
using SwiftCollab.Core.Part2;
using Given = SwiftCollab.Baseline.Part2;

namespace SwiftCollab.Tests.Part2;

public class ApiRequestHeapTests
{
    private static List<ApiRequest> Drain(ApiRequestHeap heap)
    {
        var drained = new List<ApiRequest>();
        while (heap.TryDequeue(out ApiRequest? request))
        {
            drained.Add(request);
        }

        return drained;
    }

    [Test]
    public void CourseExample_ServesAuthFirst()
    {
        var heap = new ApiRequestHeap();
        heap.Enqueue(new ApiRequest("/auth", 1));
        heap.Enqueue(new ApiRequest("/data", 3));
        heap.Enqueue(new ApiRequest("/healthcheck", 2));

        Assert.That(Drain(heap).Select(r => r.Endpoint), Is.EqualTo(new[] { "/auth", "/healthcheck", "/data" }));
    }

    [Test]
    public void CourseExample_OriginalAlsoServesAuthFirst()
    {
        var queue = new Given.ApiRequestQueue();
        queue.Enqueue(new Given.ApiRequest("/auth", 1));
        queue.Enqueue(new Given.ApiRequest("/data", 3));
        queue.Enqueue(new Given.ApiRequest("/healthcheck", 2));

        Assert.That(queue.Dequeue()?.Endpoint, Is.EqualTo("/auth"));
    }

    [Test]
    public void RandomPriorities_ComeOutInPriorityOrder()
    {
        var rng = new Random(11);
        var heap = new ApiRequestHeap();
        for (int i = 0; i < 5_000; i++)
        {
            heap.Enqueue(new ApiRequest($"/r{i}", rng.Next(1, 101)));
        }

        List<int> priorities = Drain(heap).Select(r => r.Priority).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(priorities, Has.Count.EqualTo(5_000));
            Assert.That(priorities, Is.Ordered);
        });
    }

    [Test]
    public void EqualPriorities_ComeOutFirstInFirstOut()
    {
        var heap = new ApiRequestHeap();
        for (int i = 0; i < 500; i++)
        {
            heap.Enqueue(new ApiRequest($"/r{i}", i % 3));
        }

        List<ApiRequest> drained = Drain(heap);

        foreach (IGrouping<int, ApiRequest> group in drained.GroupBy(r => r.Priority))
        {
            List<int> arrival = group.Select(r => int.Parse(r.Endpoint.AsSpan(2))).ToList();
            Assert.That(arrival, Is.Ordered, $"priority {group.Key}");
        }
    }

    [Test]
    public void BatchEnqueue_IntoAnEmptyHeap_KeepsOrder()
    {
        var rng = new Random(5);
        var heap = new ApiRequestHeap();
        heap.EnqueueRange(Enumerable.Range(0, 2_000).Select(i => new ApiRequest($"/r{i}", rng.Next(1, 11))));

        List<ApiRequest> drained = Drain(heap);

        Assert.Multiple(() =>
        {
            Assert.That(drained, Has.Count.EqualTo(2_000));
            Assert.That(drained.Select(r => r.Priority), Is.Ordered);
            foreach (IGrouping<int, ApiRequest> group in drained.GroupBy(r => r.Priority))
            {
                Assert.That(group.Select(r => int.Parse(r.Endpoint.AsSpan(2))), Is.Ordered);
            }
        });
    }

    [Test]
    public void SmallBatch_IntoALargerHeap_KeepsOrder()
    {
        var heap = new ApiRequestHeap();
        for (int i = 0; i < 100; i++)
        {
            heap.Enqueue(new ApiRequest($"/a{i}", 50 + (i % 10)));
        }

        heap.EnqueueRange([new ApiRequest("/urgent", 1), new ApiRequest("/late", 99)]);
        List<ApiRequest> drained = Drain(heap);

        Assert.Multiple(() =>
        {
            Assert.That(drained, Has.Count.EqualTo(102));
            Assert.That(drained[0].Endpoint, Is.EqualTo("/urgent"));
            Assert.That(drained[^1].Endpoint, Is.EqualTo("/late"));
            Assert.That(drained.Select(r => r.Priority), Is.Ordered);
        });
    }

    [Test]
    public void EmptyHeap_ReportsNothingToDequeue()
    {
        var heap = new ApiRequestHeap();

        Assert.Multiple(() =>
        {
            Assert.That(heap.TryDequeue(out _), Is.False);
            Assert.That(heap.Dequeue(), Is.Null);
            Assert.That(heap.TryPeek(out _), Is.False);
            Assert.That(heap.Count, Is.Zero);
        });
    }

    [Test]
    public void NullRequests_AreRejected()
    {
        var heap = new ApiRequestHeap();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => heap.Enqueue(null!));
            Assert.Throws<ArgumentNullException>(() => heap.EnqueueRange([new ApiRequest("/ok", 1), null!]));
            Assert.That(heap.Count, Is.Zero);
        });
    }

    [Test]
    public async Task ConcurrentEnqueueAndDequeue_LoseAndDuplicateNothing()
    {
        const int producers = 4;
        const int perProducer = 10_000;
        const int total = producers * perProducer;
        var heap = new ApiRequestHeap();
        var taken = new ConcurrentBag<string>();
        int remaining = total;

        Task[] producing = Enumerable.Range(0, producers).Select(p => Task.Run(() =>
        {
            var rng = new Random(p);
            for (int start = 0; start < perProducer; start += 100)
            {
                heap.EnqueueRange(Enumerable.Range(start, 100).Select(k => new ApiRequest($"/p{p}-{k}", rng.Next(1, 101))));
            }
        })).ToArray();

        Task[] consuming = Enumerable.Range(0, producers).Select(_ => Task.Run(() =>
        {
            while (Volatile.Read(ref remaining) > 0)
            {
                if (heap.TryDequeue(out ApiRequest? request))
                {
                    taken.Add(request.Endpoint);
                    Interlocked.Decrement(ref remaining);
                }
            }
        })).ToArray();

        await Task.WhenAll(producing);
        bool finished = Task.WaitAll(consuming, TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(finished, Is.True, "consumers did not drain the heap");
            Assert.That(taken, Has.Count.EqualTo(total));
            Assert.That(taken.Distinct().Count(), Is.EqualTo(total));
            Assert.That(heap.Count, Is.Zero);
        });
    }

    [Test]
    public async Task ConcurrentSingleEnqueues_AllArrive()
    {
        var heap = new ApiRequestHeap();
        await Task.WhenAll(Enumerable.Range(0, 8).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < 5_000; i++)
            {
                heap.Enqueue(new ApiRequest($"/t{t}-{i}", i % 50));
            }
        })));

        List<ApiRequest> drained = Drain(heap);

        Assert.Multiple(() =>
        {
            Assert.That(drained, Has.Count.EqualTo(40_000));
            Assert.That(drained.Select(r => r.Endpoint).Distinct().Count(), Is.EqualTo(40_000));
            Assert.That(drained.Select(r => r.Priority), Is.Ordered);
        });
    }
}
