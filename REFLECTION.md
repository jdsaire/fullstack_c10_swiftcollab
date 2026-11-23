# LLM Contribution Reflection

**How the AI assistant helped.** I ran each course prompt as written, 21 in all. The assistant's first pass on each part listed the concrete defects (the chain that sorted priorities build, the full re-sort on every request, the program ending on one null task) and offered a fix with alternatives for each prompt. That gave every part a short list of decisions to make, and the assistant also wrote the tests that prove each fix.

**What was wrong or unnecessary.** 16 suggestions were rejected and 8 changed. Two were wrong: a smaller height field would save no memory, because .NET rounds object sizes up to 8 bytes; and inserting into a sorted list at a binary-searched slot still shifts every later item, so it stays O(n). One was overstated: plain quicksort is O(n log n) only on average, so the sort gained a heapsort fallback. Others were sound but unnecessary here: merge sort needs a second array, a retry backoff would hold up every task behind it, and a lock-free queue adds risk without a need.

**Most impactful improvements.** The heap and the introsort: 4,707 ms down to 2.5 ms for 20,000 requests, and 11,370 ms down to 4.6 ms for 100,000 integers. The balanced tree removed the worst case (5,802 ms to 16 ms on sorted input), though it is slower on random input. The executor matters most for users: the queue now finishes, and every failure is logged.

Full record of prompts and decisions: https://github.com/jdsaire/fullstack_c10_swiftcollab/blob/main/docs/evidence/AI-ASSISTANCE-RECORD.md
