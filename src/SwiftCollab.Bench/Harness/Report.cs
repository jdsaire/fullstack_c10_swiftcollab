using System.Globalization;
using System.Text;

namespace SwiftCollab.Bench.Harness;

/// <summary>Markdown tables and a CSV of the raw cells. Every measured cell is badged [M].</summary>
public static class Report
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Table(int part, string title, IEnumerable<CellResult> cells)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"### Part {part} · {title}");
        sb.AppendLine();
        sb.AppendLine("| Workload | Size | Method | Role | Median (ms) | Runs | Allocated (KB) |");
        sb.AppendLine("|---|---:|---|---|---:|---:|---:|");
        foreach (CellResult c in cells)
        {
            string median = c.Capped
                ? $"over cap ({c.CappedRunMs.ToString("0", Inv)} ms on run {c.Runs}), not measured"
                : $"{Format(c.MedianMs)} [M]";
            string kb = c.Capped ? "—" : (c.MedianAllocatedBytes / 1024.0).ToString("0.0", Inv);
            sb.AppendLine(
                $"| {c.Case.Workload} | {c.Size.ToString("N0", Inv)} | {c.Case.Method} | {c.Case.Role} | " +
                $"{median} | {c.Runs} | {kb} |");
        }

        return sb.ToString();
    }

    public static string Csv(IEnumerable<CellResult> cells)
    {
        var sb = new StringBuilder("part,workload,method,role,size,median_ms,runs,allocated_bytes,capped\n");
        foreach (CellResult c in cells)
        {
            sb.Append(Inv, $"{c.Case.Part},{Quote(c.Case.Workload)},{Quote(c.Case.Method)},{c.Case.Role},{c.Size},");
            sb.Append(c.Capped ? "" : c.MedianMs.ToString("0.0000", Inv));
            sb.Append(Inv, $",{c.Runs},{(c.Capped ? "" : c.MedianAllocatedBytes.ToString(Inv))},{c.Capped}\n");
        }

        return sb.ToString();
    }

    private static string Quote(string field) =>
        field.Contains(',') || field.Contains('"') ? $"\"{field.Replace("\"", "\"\"")}\"" : field;

    private static string Format(double ms) =>
        ms < 1 ? ms.ToString("0.000", Inv) : ms < 100 ? ms.ToString("0.00", Inv) : ms.ToString("N0", Inv);
}
