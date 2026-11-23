using SwiftCollab.Core.Part3;
using Given = SwiftCollab.Baseline.Part3;

namespace SwiftCollab.Tests.Part3;

public class IntroSorterTests
{
    public static IEnumerable<TestCaseData> Shapes()
    {
        var rng = new Random(21);
        yield return new TestCaseData(Array.Empty<int>()).SetName("Empty");
        yield return new TestCaseData(new[] { 42 }).SetName("Single");
        yield return new TestCaseData(new[] { 50, 20, 40, 10, 30 }).SetName("CourseExample");
        yield return new TestCaseData(Enumerable.Range(0, 10_000).ToArray()).SetName("Sorted");
        yield return new TestCaseData(Enumerable.Range(0, 10_000).Reverse().ToArray()).SetName("Reversed");
        yield return new TestCaseData(Enumerable.Repeat(7, 5_000).ToArray()).SetName("AllEqual");
        yield return new TestCaseData(Enumerable.Range(0, 20_000).Select(_ => rng.Next(4)).ToArray()).SetName("DuplicateHeavy");
        yield return new TestCaseData(Enumerable.Range(0, 20_000).Select(_ => rng.Next(int.MinValue, int.MaxValue)).ToArray()).SetName("RandomWithNegatives");
        yield return new TestCaseData(Enumerable.Range(0, 10_000).Select(i => i % 2 == 0 ? i : 10_000 - i).ToArray()).SetName("Interleaved");
    }

    private static int[] Expected(int[] input)
    {
        int[] copy = (int[])input.Clone();
        Array.Sort(copy);
        return copy;
    }

    [TestCaseSource(nameof(Shapes))]
    public void Sort_MatchesArraySort(int[] input)
    {
        int[] actual = (int[])input.Clone();
        IntroSorter.Sort(actual);
        Assert.That(actual, Is.EqualTo(Expected(input)));
    }

    [TestCaseSource(nameof(Shapes))]
    public void ParallelSort_MatchesArraySort(int[] input)
    {
        int[] actual = (int[])input.Clone();
        IntroSorter.ParallelSort(actual, threshold: 1_000); // low threshold so the parallel path runs here
        Assert.That(actual, Is.EqualTo(Expected(input)));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void LargeRandomArrays_SequentialAndParallelAgree(int seed)
    {
        var rng = new Random(seed);
        int[] input = Enumerable.Range(0, 200_000).Select(_ => rng.Next()).ToArray();
        int[] sequential = (int[])input.Clone();
        int[] parallel = (int[])input.Clone();

        IntroSorter.Sort(sequential);
        IntroSorter.ParallelSort(parallel);

        Assert.Multiple(() =>
        {
            Assert.That(sequential, Is.EqualTo(Expected(input)));
            Assert.That(parallel, Is.EqualTo(sequential));
        });
    }

    [Test]
    public void HeapsortFallback_SortsCorrectly()
    {
        // A depth limit of 0 sends the whole range to the heapsort fallback that bounds the worst case.
        var rng = new Random(9);
        int[] input = Enumerable.Range(0, 5_000).Select(_ => rng.Next(100)).ToArray();
        int[] actual = (int[])input.Clone();

        IntroSorter.IntroSortWithDepthLimit(actual, 0);

        Assert.That(actual, Is.EqualTo(Expected(input)));
    }

    [Test]
    public void NullArrays_AreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => IntroSorter.Sort((int[])null!));
            Assert.Throws<ArgumentNullException>(() => IntroSorter.ParallelSort(null!));
        });
    }

    [Test]
    public void OriginalBubbleSort_SortsTheCourseExample()
    {
        int[] data = [50, 20, 40, 10, 30];
        Given.Sorting.BubbleSort(data);
        Assert.That(data, Is.EqualTo(new[] { 10, 20, 30, 40, 50 }));
    }
}
