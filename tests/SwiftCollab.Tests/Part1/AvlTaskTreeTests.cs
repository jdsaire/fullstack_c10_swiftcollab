using SwiftCollab.Core.Part1;
using Given = SwiftCollab.Baseline.Part1;

namespace SwiftCollab.Tests.Part1;

public class AvlTaskTreeTests
{
    // AVL trees are at most about 1.44·log2(n + 2) levels deep.
    private static double MaxAvlHeight(int n) => 1.4405 * Math.Log2(n + 2);

    [Test]
    public void SortedInserts_KeepTheTreeBalanced()
    {
        var tree = new AvlTaskTree();
        for (int p = 0; p < 10_000; p++)
        {
            tree.Insert(p, $"task-{p}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(tree.IsValidAvl(), Is.True);
            Assert.That(tree.Height, Is.LessThanOrEqualTo(MaxAvlHeight(10_000)));
            Assert.That(tree.PriorityCount, Is.EqualTo(10_000));
        });
    }

    [Test]
    public void ReverseSortedInserts_KeepTheTreeBalanced()
    {
        var tree = new AvlTaskTree();
        for (int p = 10_000; p > 0; p--)
        {
            tree.Insert(p, $"task-{p}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(tree.IsValidAvl(), Is.True);
            Assert.That(tree.Height, Is.LessThanOrEqualTo(MaxAvlHeight(10_000)));
        });
    }

    [Test]
    public void RandomInserts_StayValidAfterEveryInsert()
    {
        var rng = new Random(7);
        var tree = new AvlTaskTree();
        for (int i = 0; i < 2_000; i++)
        {
            tree.Insert(rng.Next(500), $"task-{i}");
            Assert.That(tree.IsValidAvl(), Is.True, $"invalid after insert {i}");
        }
    }

    [Test]
    public void SortedInserts_OriginalTreeDegradesToAChain()
    {
        // Documents the defect the refinement fixes: the given tree's depth equals the number of inserts.
        var tree = new Given.BinaryTree();
        for (int p = 0; p < 1_000; p++)
        {
            tree.Insert(p);
        }

        int depth = 0;
        for (Given.Node? node = tree.Root; node is not null; node = node.Right)
        {
            depth++;
        }

        Assert.That(depth, Is.EqualTo(1_000));
    }

    [Test]
    public void TryFind_ReturnsTheTasksForAPriority()
    {
        var tree = new AvlTaskTree();
        tree.Insert(5, "deploy");
        tree.Insert(1, "auth");
        tree.Insert(9, "report");

        Assert.Multiple(() =>
        {
            Assert.That(tree.TryFind(1, out IReadOnlyList<string> tasks), Is.True);
            Assert.That(tasks, Is.EqualTo(new[] { "auth" }));
        });
    }

    [Test]
    public void TryFind_MissesAnAbsentPriority()
    {
        var tree = new AvlTaskTree();
        tree.Insert(5, "deploy");

        Assert.Multiple(() =>
        {
            Assert.That(tree.TryFind(4, out IReadOnlyList<string> tasks), Is.False);
            Assert.That(tasks, Is.Empty);
            Assert.That(new AvlTaskTree().Contains(1), Is.False);
        });
    }

    [Test]
    public void DuplicatePriorities_AreKeptInInsertOrder()
    {
        var tree = new AvlTaskTree();
        for (int p = 0; p < 100; p++)
        {
            tree.Insert(p, $"task-{p}"); // rotations happen around priority 42 while it fills
        }

        tree.Insert(42, "second");
        tree.Insert(42, "third");

        Assert.Multiple(() =>
        {
            Assert.That(tree.TryFind(42, out IReadOnlyList<string> tasks), Is.True);
            Assert.That(tasks, Is.EqualTo(new[] { "task-42", "second", "third" }));
            Assert.That(tree.Count, Is.EqualTo(102));
            Assert.That(tree.PriorityCount, Is.EqualTo(100));
            Assert.That(tree.IsValidAvl(), Is.True);
        });
    }

    [Test]
    public void InOrder_ReturnsEveryTaskSortedByPriority()
    {
        var rng = new Random(3);
        var tree = new AvlTaskTree();
        var expected = new List<int>();
        for (int i = 0; i < 1_000; i++)
        {
            int p = rng.Next(200);
            expected.Add(p);
            tree.Insert(p, $"task-{i}");
        }

        expected.Sort();
        List<int> walked = tree.InOrder().Select(e => e.Priority).ToList();

        Assert.That(walked, Is.EqualTo(expected));
    }

    [Test]
    public void TryGetHighestPriority_ReturnsTheLowestNumber()
    {
        var tree = new AvlTaskTree();
        Assert.That(tree.TryGetHighestPriority(out _, out _), Is.False);

        tree.Insert(3, "data");
        tree.Insert(1, "auth");
        tree.Insert(2, "healthcheck");

        Assert.Multiple(() =>
        {
            Assert.That(tree.TryGetHighestPriority(out int priority, out IReadOnlyList<string> tasks), Is.True);
            Assert.That(priority, Is.EqualTo(1));
            Assert.That(tasks, Is.EqualTo(new[] { "auth" }));
        });
    }

    [Test]
    public void Insert_RejectsANullTask()
    {
        Assert.Throws<ArgumentNullException>(() => new AvlTaskTree().Insert(1, null!));
    }
}
