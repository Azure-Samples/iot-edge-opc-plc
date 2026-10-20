namespace OpcPlc.Helpers;

using Opc.Ua.Export;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Parses NodeSet2 XML files once and shares the result, so the expensive XML deserialization can be
/// started in the background before the node managers need it and is not repeated by multiple consumers.
/// </summary>
internal static class NodeSetXmlCache
{
    private static readonly ConcurrentDictionary<string, Lazy<UANodeSet>> s_nodeSets = new(StringComparer.Ordinal);

    /// <summary>
    /// Starts parsing the node sets sequentially, in the given order, on a background thread.
    /// A consumer that needs a node set before the prefetch reached it parses it inline.
    /// </summary>
    public static void Prefetch(params string[] relativePaths)
    {
        Lazy<UANodeSet>[] nodeSets = Array.ConvertAll(relativePaths, GetOrAdd);
        _ = Task.Run(() =>
        {
            foreach (Lazy<UANodeSet> nodeSet in nodeSets)
            {
                try
                {
                    _ = nodeSet.Value;
                }
                catch
                {
                    // Errors are rethrown to the consumer reading the node set.
                }
            }
        }, CancellationToken.None);
    }

    /// <summary>
    /// Returns the parsed node set, waiting for or performing the parse if necessary.
    /// </summary>
    public static UANodeSet Read(string relativePath)
    {
        Lazy<UANodeSet> nodeSet = GetOrAdd(relativePath);
        try
        {
            return nodeSet.Value;
        }
        catch
        {
            // Do not cache failures, a later read retries.
            s_nodeSets.TryRemove(new(ResolvePath(relativePath), nodeSet));
            throw;
        }
    }

    /// <summary>
    /// Releases the cached node sets once the address space is built.
    /// </summary>
    public static void Clear()
    {
        s_nodeSets.Clear();
    }

    /// <summary>
    /// Resolves a path relative to the application (or snap) directory.
    /// </summary>
    public static string ResolvePath(string relativePath)
    {
        string snapLocation = Environment.GetEnvironmentVariable("SNAP");
        return string.IsNullOrWhiteSpace(snapLocation)
            ? relativePath
            : Path.Join(snapLocation, relativePath);
    }

    private static Lazy<UANodeSet> GetOrAdd(string relativePath)
    {
        string path = ResolvePath(relativePath);
        return s_nodeSets.GetOrAdd(path, static p => new Lazy<UANodeSet>(
            () =>
            {
                using var stream = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read);
                return UANodeSet.Read(stream);
            },
            LazyThreadSafetyMode.ExecutionAndPublication));
    }
}
