namespace SwiftCollab.Core.Part1;

/// <summary>
/// Refinement of the course's <c>BinaryTree</c> (Part 1): a self-balancing AVL tree that stores tasks by
/// priority, with an iterative search. Lower numbers are higher priority, as in the course scenario.
/// </summary>
/// <remarks>
/// Original: an unbalanced binary search tree of plain integers, recursive insert, no search, and an
/// in-order print that writes to the console. Inserting priorities in sorted order turns it into a chain,
/// so each insert walks every node already in the tree.
/// </remarks>
public sealed class AvlTaskTree
{
    // AI-suggested (modified): one node per priority holding all of its tasks, instead of one node per
    // insert. Why: repeated priorities no longer lengthen the tree, and a search returns every task with
    // that priority at once. Modified: the suggestion kept the original's rule of sending an equal priority
    // to the right; after rotations, equal priorities can sit on both sides of a node and a search would
    // find only one of them, so they share a node instead.
    private sealed class Node(int priority, string task)
    {
        public readonly int Priority = priority;
        public readonly List<string> Tasks = [task];
        public Node? Left;
        public Node? Right;

        // AI-suggested (kept): each node stores the height of its subtree. Why: the balance check reads two
        // stored numbers instead of measuring both subtrees again after every insert.
        public int Height = 1;
    }

    private Node? _root;

    /// <summary>Number of tasks stored, duplicates included.</summary>
    public int Count { get; private set; }

    /// <summary>Number of distinct priorities (tree nodes).</summary>
    public int PriorityCount { get; private set; }

    /// <summary>Height of the tree; 0 when empty.</summary>
    public int Height => HeightOf(_root);

    public void Insert(int priority, string task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _root = Insert(_root, priority, task);
        Count++;
    }

    // AI-suggested (modified): the recursive insert returns the new root of each subtree, which replaces
    // the original's separate null checks on Left and Right. Modified: the suggestion made insert fully
    // iterative with an explicit stack; recursion is kept because a balanced tree is at most about
    // 1.44·log2(n) levels deep (about 25 calls at 100,000 priorities), so the call depth is no longer a risk
    // and the rebalancing reads more clearly.
    private Node Insert(Node? node, int priority, string task)
    {
        if (node is null)
        {
            PriorityCount++;
            return new Node(priority, task);
        }

        if (priority < node.Priority)
        {
            node.Left = Insert(node.Left, priority, task);
        }
        else if (priority > node.Priority)
        {
            node.Right = Insert(node.Right, priority, task);
        }
        else
        {
            node.Tasks.Add(task);
            return node; // Same priority: no new node, so no height changes and nothing to rebalance.
        }

        return Rebalance(node);
    }

    // AI-suggested (kept): AVL balancing on the way back up from each insert. Why: no subtree is ever more
    // than one level taller than its sibling, so the tree stays about log2(n) deep even when priorities
    // arrive in sorted order; search and insert stay O(log n) instead of degrading to O(n).
    private static Node Rebalance(Node node)
    {
        UpdateHeight(node);
        int balance = BalanceOf(node);

        if (balance > 1)
        {
            if (BalanceOf(node.Left!) < 0)
            {
                node.Left = RotateLeft(node.Left!); // Left-right case.
            }

            return RotateRight(node);
        }

        if (balance < -1)
        {
            if (BalanceOf(node.Right!) > 0)
            {
                node.Right = RotateRight(node.Right!); // Right-left case.
            }

            return RotateLeft(node);
        }

        return node;
    }

    private static Node RotateRight(Node node)
    {
        Node pivot = node.Left!;
        node.Left = pivot.Right;
        pivot.Right = node;
        UpdateHeight(node);
        UpdateHeight(pivot);
        return pivot;
    }

    private static Node RotateLeft(Node node)
    {
        Node pivot = node.Right!;
        node.Right = pivot.Left;
        pivot.Left = node;
        UpdateHeight(node);
        UpdateHeight(pivot);
        return pivot;
    }

    private static int HeightOf(Node? node) => node?.Height ?? 0;

    private static int BalanceOf(Node node) => HeightOf(node.Left) - HeightOf(node.Right);

    private static void UpdateHeight(Node node) =>
        node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));

    // AI-suggested (kept): an iterative search by priority, which the original lacked. Why: one loop walks
    // a single path from the root, O(log n) on a balanced tree, with no call stack and no allocation.
    public bool TryFind(int priority, out IReadOnlyList<string> tasks)
    {
        Node? current = _root;
        while (current is not null)
        {
            if (priority == current.Priority)
            {
                tasks = current.Tasks;
                return true;
            }

            current = priority < current.Priority ? current.Left : current.Right;
        }

        tasks = [];
        return false;
    }

    public bool Contains(int priority) => TryFind(priority, out _);

    /// <summary>The highest-priority entry (lowest number): the leftmost node, found with a loop.</summary>
    public bool TryGetHighestPriority(out int priority, out IReadOnlyList<string> tasks)
    {
        Node? current = _root;
        if (current is null)
        {
            priority = 0;
            tasks = [];
            return false;
        }

        while (current.Left is not null)
        {
            current = current.Left;
        }

        priority = current.Priority;
        tasks = current.Tasks;
        return true;
    }

    // AI-suggested (kept): in-order traversal returns the entries instead of printing them, walking with an
    // explicit stack instead of recursion. Why: the tree no longer writes to the console, so callers decide
    // what to do with the tasks and tests can check the order; and a long walk never deepens the call stack.
    public IEnumerable<(int Priority, string Task)> InOrder()
    {
        var stack = new Stack<Node>();
        Node? current = _root;
        while (current is not null || stack.Count > 0)
        {
            while (current is not null)
            {
                stack.Push(current);
                current = current.Left;
            }

            current = stack.Pop();
            foreach (string task in current.Tasks)
            {
                yield return (current.Priority, task);
            }

            current = current.Right;
        }
    }

    /// <summary>Test check: every node is ordered, its stored height is right, and it is AVL-balanced.</summary>
    internal bool IsValidAvl() => Validate(_root, long.MinValue, long.MaxValue) >= 0;

    private static int Validate(Node? node, long min, long max)
    {
        if (node is null)
        {
            return 0;
        }

        if (node.Priority <= min || node.Priority >= max)
        {
            return -1;
        }

        int left = Validate(node.Left, min, node.Priority);
        int right = Validate(node.Right, node.Priority, max);
        if (left < 0 || right < 0 || Math.Abs(left - right) > 1)
        {
            return -1;
        }

        int height = 1 + Math.Max(left, right);
        return height == node.Height ? height : -1;
    }
}
