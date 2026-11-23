namespace SwiftCollab.Core.Part3;

/// <summary>
/// Refinement of the course's <c>BubbleSort</c> (Part 3): an in-place introsort, with a parallel option
/// for large arrays. Sorts integers in ascending order, with the same result as <c>Array.Sort</c>.
/// </summary>
/// <remarks>
/// Original: bubble sort, two nested loops over the array whatever its order, O(n²) comparisons in every
/// case. It already sorts in place.
/// </remarks>
public static class IntroSorter
{
    // AI-suggested (kept): insertion sort finishes partitions of 16 or fewer items. Why: on tiny ranges it
    // beats further partitioning, which has more setup per item.
    private const int InsertionThreshold = 16;

    /// <summary>Arrays shorter than this are sorted on one thread even when the parallel option is used.</summary>
    public const int DefaultParallelThreshold = 1 << 16;

    public static void Sort(int[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        Sort(array.AsSpan());
    }

    public static void Sort(Span<int> items)
    {
        if (items.Length > 1)
        {
            IntroSort(items, DepthLimit(items.Length));
        }
    }

    // AI-suggested (modified): quicksort replaces bubble sort. Why: each partition step splits the range
    // around a pivot, so the work grows as n log n on average instead of n². Modified: the suggestion
    // stated plain quicksort as O(n log n), which holds only on average; this version switches to heapsort
    // when partitioning goes deeper than 2·log2(n), so the worst case also stays O(n log n) (introsort).
    private static void IntroSort(Span<int> items, int depthLimit)
    {
        // AI-suggested (kept): recurse into the smaller side and loop on the larger one. Why: the call
        // stack stays at most log2(n) deep, so the sort needs no extra memory beyond the array itself.
        while (items.Length > InsertionThreshold)
        {
            if (depthLimit == 0)
            {
                HeapSort(items);
                return;
            }

            depthLimit--;
            int pivot = Partition(items);
            Span<int> left = items[..pivot];
            Span<int> right = items[(pivot + 1)..];
            if (left.Length < right.Length)
            {
                IntroSort(left, depthLimit);
                items = right;
            }
            else
            {
                IntroSort(right, depthLimit);
                items = left;
            }
        }

        InsertionSort(items);
    }

    // AI-suggested (kept): median-of-three pivot and an in-place partition that stops on equal values.
    // Why: sorted, reversed and duplicate-heavy inputs still split near the middle, and swapping inside the
    // array needs no second buffer (merge sort would need one the size of the input).
    private static int Partition(Span<int> items)
    {
        int hi = items.Length - 1;
        int mid = hi / 2;
        SwapIfGreater(items, 0, mid);
        SwapIfGreater(items, 0, hi);
        SwapIfGreater(items, mid, hi);

        int pivot = items[mid];
        Swap(items, mid, hi - 1);
        int left = 0;
        int right = hi - 1;
        while (left < right)
        {
            while (items[++left] < pivot)
            {
            }

            while (pivot < items[--right])
            {
            }

            if (left >= right)
            {
                break;
            }

            Swap(items, left, right);
        }

        if (left != hi - 1)
        {
            Swap(items, left, hi - 1);
        }

        return left;
    }

    private static void InsertionSort(Span<int> items)
    {
        for (int i = 1; i < items.Length; i++)
        {
            int value = items[i];
            int j = i - 1;
            while (j >= 0 && items[j] > value)
            {
                items[j + 1] = items[j];
                j--;
            }

            items[j + 1] = value;
        }
    }

    internal static void HeapSort(Span<int> items)
    {
        int n = items.Length;
        for (int i = n / 2 - 1; i >= 0; i--)
        {
            SiftDown(items, i, n);
        }

        for (int end = n - 1; end > 0; end--)
        {
            Swap(items, 0, end);
            SiftDown(items, 0, end);
        }
    }

    private static void SiftDown(Span<int> items, int root, int size)
    {
        int value = items[root];
        while (true)
        {
            int child = 2 * root + 1;
            if (child >= size)
            {
                break;
            }

            if (child + 1 < size && items[child + 1] > items[child])
            {
                child++;
            }

            if (items[child] <= value)
            {
                break;
            }

            items[root] = items[child];
            root = child;
        }

        items[root] = value;
    }

    // AI-suggested (modified): the parallel option sorts the two sides of each partition at the same time.
    // Why: large arrays use more than one core. Modified: the suggestion forked at every level; this version
    // forks only for ranges of at least the threshold and only for the first log2(cores) + 1 levels, so the
    // number of parallel tasks stays near the core count and small ranges carry no task overhead.
    public static void ParallelSort(int[] array, int threshold = DefaultParallelThreshold)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfLessThan(threshold, InsertionThreshold + 1);
        if (array.Length < threshold)
        {
            Sort(array);
            return;
        }

        int parallelDepth = (int)Math.Log2(Environment.ProcessorCount) + 1;
        ParallelIntroSort(array, 0, array.Length, DepthLimit(array.Length), parallelDepth, threshold);
    }

    private static void ParallelIntroSort(int[] array, int start, int length, int depthLimit, int parallelDepth, int threshold)
    {
        if (length < threshold || parallelDepth == 0 || depthLimit == 0)
        {
            IntroSort(array.AsSpan(start, length), depthLimit);
            return;
        }

        int pivot = Partition(array.AsSpan(start, length));
        Parallel.Invoke(
            () => ParallelIntroSort(array, start, pivot, depthLimit - 1, parallelDepth - 1, threshold),
            () => ParallelIntroSort(array, start + pivot + 1, length - pivot - 1, depthLimit - 1, parallelDepth - 1, threshold));
    }

    internal static void IntroSortWithDepthLimit(Span<int> items, int depthLimit) => IntroSort(items, depthLimit);

    private static int DepthLimit(int length) => 2 * (int)Math.Log2(length);

    private static void SwapIfGreater(Span<int> items, int i, int j)
    {
        if (items[i] > items[j])
        {
            Swap(items, i, j);
        }
    }

    private static void Swap(Span<int> items, int i, int j) => (items[i], items[j]) = (items[j], items[i]);
}
