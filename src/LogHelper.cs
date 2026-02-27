using BepInEx.Logging;
using System.IO;
using System.Runtime.CompilerServices;

internal static class LogHelper
{
    internal static ManualLogSource log { get; set; }

    public static void LogInfo(object data, [CallerFilePath] string filePath = "") 
        => log?.LogInfo($"[{GetClassName(filePath)}] {data}");

    public static void LogWarning(object data, [CallerFilePath] string filePath = "") 
        => log?.LogWarning($"[{GetClassName(filePath)}] {data}");

    public static void LogError(object data, [CallerFilePath] string filePath = "") 
        => log?.LogError($"[{GetClassName(filePath)}] {data}");

    public static void LogDebug(object data, [CallerFilePath] string filePath = "") 
        => log?.LogDebug($"[{GetClassName(filePath)}] {data}");

    private static string GetClassName(string path) => Path.GetFileNameWithoutExtension(path);
}