using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SwiftCollab.Bench.Harness;

/// <summary>The environment line printed above every result table.</summary>
public static class Machine
{
    public static string Describe(RunnerSettings settings)
    {
        string chip = Sysctl("machdep.cpu.brand_string") ?? RuntimeInformation.ProcessArchitecture.ToString();
        string memory = long.TryParse(Sysctl("hw.memsize"), out long bytes)
            ? $"{bytes / (1024L * 1024 * 1024)} GB"
            : "memory unknown";
        string os = RuntimeInformation.OSDescription;
        string macVersion = SwVers() is { } v ? $"macOS {v}" : os;

        return $"{chip} · {Environment.ProcessorCount} cores · {memory} · {macVersion} · " +
               $".NET {Environment.Version} · Release · median of {settings.Runs} runs " +
               $"({settings.ReducedRuns} when a run passes {settings.SlowRun.TotalSeconds:0} s) · " +
               $"cap {settings.Cap.TotalSeconds:0} s per run · {DateTime.Now:yyyy-MM-dd}";
    }

    /// <summary>True when every measured assembly was compiled with the JIT optimizer enabled.</summary>
    public static bool IsOptimizedBuild(params Assembly[] assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            var debuggable = assembly.GetCustomAttribute<DebuggableAttribute>();
            if (debuggable is { IsJITOptimizerDisabled: true })
            {
                return false;
            }
        }

        return true;
    }

    private static string? Sysctl(string key) => Run("sysctl", $"-n {key}");

    private static string? SwVers() => Run("sw_vers", "-productVersion");

    private static string? Run(string file, string args)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return null;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo(file, args)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });
            string? output = process?.StandardOutput.ReadToEnd().Trim();
            process?.WaitForExit();
            return string.IsNullOrEmpty(output) ? null : output;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
