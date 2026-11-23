using System.Diagnostics.CodeAnalysis;

namespace SwiftCollab.Core.Part2;

// AI-suggested (kept): an immutable request. Why: the original's settable Priority could change after a
// request was queued, silently breaking the queue's order; a record fixes both values at creation.
/// <summary>An API request with its priority; lower numbers are served first.</summary>
public sealed record ApiRequest(string Endpoint, int Priority);

/// <summary>
/// Refinement of the course's <c>ApiRequestQueue</c> (Part 2): a binary min-heap, safe to share across
/// threads, with batch enqueue. Requests with equal priority leave in the order they arrived.
/// </summary>
/// <remarks>
/// Original: a list re-sorted on every enqueue (O(n log n) per insert) and emptied from the front with
/// RemoveAt(0) (O(n) per dequeue), not thread-safe, and with no order guarantee among equal priorities,
/// because List.Sort is not stable.
/// </remarks>
public sealed class ApiRequestHeap
{
    // AI-suggested (kept): the heap stores each request with its priority and an arrival number.
    // Why: comparing two integers avoids following a reference per comparison, and the arrival number
    // breaks ties so equal priorities come out first in, first out.
    private struct Entry(ApiRequest request, long sequence)
    {
        public readonly ApiRequest Request = request;
        public readonly int Priority = request.Priority;
        public readonly long Sequence = sequence;
    }

    // AI-suggested (kept): one lock around every operation. Why: enqueue and dequeue both move entries
    // inside one array, so concurrent callers must not interleave; an uncontended lock costs little and
    // is simpler than a lock-free design at this scale.
    private readonly Lock _gate = new();

    private Entry[] _entries;
    private int _count;
    private long _nextSequence;

    public ApiRequestHeap(int initialCapacity = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialCapacity);
        _entries = new Entry[initialCapacity];
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _count;
            }
        }
    }

    // AI-suggested (kept): enqueue appends at the end of the array and sifts the entry up. Why: at most
    // log2(n) swaps, so insertion is O(log n) instead of re-sorting the whole list.
    public void Enqueue(ApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_gate)
        {
            EnsureCapacity(_count + 1);
            _entries[_count] = new Entry(request, _nextSequence++);
            SiftUp(_count);
            _count++;
        }
    }

    // AI-suggested (modified): batch enqueue under a single lock and a single array resize. Why: one lock
    // and one resize for the whole batch instead of one per request. Modified: the suggestion always
    // rebuilt the heap from scratch (O(n + k)); that is only kept when the batch is at least as large as
    // the queue, otherwise each new request is sifted up (O(k log n)), which is cheaper for small batches.
    public void EnqueueRange(IEnumerable<ApiRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ApiRequest[] batch = requests.ToArray();
        foreach (ApiRequest request in batch)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(requests));
        }

        lock (_gate)
        {
            EnsureCapacity(_count + batch.Length);
            bool rebuild = batch.Length >= _count;
            foreach (ApiRequest request in batch)
            {
                _entries[_count] = new Entry(request, _nextSequence++);
                if (!rebuild)
                {
                    SiftUp(_count);
                }

                _count++;
            }

            if (rebuild)
            {
                for (int i = _count / 2 - 1; i >= 0; i--)
                {
                    SiftDown(i);
                }
            }
        }
    }

    // AI-suggested (kept): dequeue takes the root, moves the last entry to the top and sifts it down.
    // Why: O(log n) instead of the original's RemoveAt(0), which shifts every remaining request.
    // Also kept: TryDequeue reports an empty queue with false instead of returning null.
    public bool TryDequeue([MaybeNullWhen(false)] out ApiRequest request)
    {
        lock (_gate)
        {
            if (_count == 0)
            {
                request = null;
                return false;
            }

            request = _entries[0].Request;
            _count--;
            _entries[0] = _entries[_count];
            _entries[_count] = default;
            if (_count > 0)
            {
                SiftDown(0);
            }

            return true;
        }
    }

    /// <summary>The original's contract, kept for callers that expect it: null when the queue is empty.</summary>
    public ApiRequest? Dequeue() => TryDequeue(out ApiRequest? request) ? request : null;

    public bool TryPeek([MaybeNullWhen(false)] out ApiRequest request)
    {
        lock (_gate)
        {
            request = _count > 0 ? _entries[0].Request : null;
            return _count > 0;
        }
    }

    private static bool Precedes(in Entry a, in Entry b) =>
        a.Priority < b.Priority || (a.Priority == b.Priority && a.Sequence < b.Sequence);

    private void SiftUp(int index)
    {
        Entry item = _entries[index];
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (!Precedes(item, _entries[parent]))
            {
                break;
            }

            _entries[index] = _entries[parent];
            index = parent;
        }

        _entries[index] = item;
    }

    private void SiftDown(int index)
    {
        Entry item = _entries[index];
        int half = _count / 2;
        while (index < half)
        {
            int child = 2 * index + 1;
            int right = child + 1;
            if (right < _count && Precedes(_entries[right], _entries[child]))
            {
                child = right;
            }

            if (!Precedes(_entries[child], item))
            {
                break;
            }

            _entries[index] = _entries[child];
            index = child;
        }

        _entries[index] = item;
    }

    private void EnsureCapacity(int needed)
    {
        if (needed > _entries.Length)
        {
            Array.Resize(ref _entries, Math.Max(needed, _entries.Length * 2));
        }
    }
}
