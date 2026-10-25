using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Xero.DataComparer.Core;

/// <summary>
/// Compares two lists of <typeparamref name="T"/> matched on key properties. An instance holds only its configuration,
/// so it can run several comparisons at the same time.
/// </summary>
public class ListComparer<T> where T : class
{
    private const string ComparedItemsColumn = "ComparedItems";

    private readonly PropertyAccessor<T>[] _keys;
    private readonly PropertyAccessor<T>[] _compared;
    // Column names built once, not per diff row
    private readonly string[] _keyColumns;
    private readonly string[] _referenceColumns;
    private readonly string[] _targetColumns;
    private readonly (string Name, Type Type)[] _columns;
    // Max entries a diff dict can hold: (Reference_ + Target_) per prop + Key_ per key + "ComparedItems"
    private readonly int _diffCapacity;

    private readonly EqualityComparerEx<T> _keyComparer;
    private readonly ILogger<ListComparer<T>>? _logger;

    public ListComparer(
        List<string> keyProperties,
        List<string> ignoreProperties,
        ILogger<ListComparer<T>>? logger = null)
    {
        _logger = logger;

        var distinctKeys = keyProperties.Distinct().ToList();
        _keys = distinctKeys.Select(PropertyAccessor<T>.Create).ToArray();
        _keyComparer = new EqualityComparerEx<T>(_keys);

        // Use HashSet<string> for O(1) property name lookups during filtering
        var ignoreSet = new HashSet<string>(ignoreProperties);
        var keySet = new HashSet<string>(distinctKeys);

        _compared = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                             .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                             .Where(p => !ignoreSet.Contains(p.Name) && !keySet.Contains(p.Name))
                             .Select(PropertyAccessor<T>.Create)
                             .ToArray();

        _keyColumns = _keys.Select(k => $"Key_{k.Name}").ToArray();
        _referenceColumns = _compared.Select(p => $"Reference_{p.Name}").ToArray();
        _targetColumns = _compared.Select(p => $"Target_{p.Name}").ToArray();

        var columns = new List<(string Name, Type Type)>();
        columns.AddRange(_keys.Select((k, i) => (_keyColumns[i], k.Type)));
        for (int i = 0; i < _compared.Length; i++)
        {
            columns.Add((_referenceColumns[i], _compared[i].Type));
            columns.Add((_targetColumns[i], _compared[i].Type));
        }
        columns.Add((ComparedItemsColumn, typeof(CoupleItem<T>)));
        _columns = columns.ToArray();
        _diffCapacity = _columns.Length;
    }

    public async Task<CompareResult<T>> CompareListAsync(List<T> referenceItems, List<T> targetItems)
    {
        if (referenceItems is null) throw new ArgumentNullException(nameof(referenceItems));
        if (targetItems is null) throw new ArgumentNullException(nameof(targetItems));

        // Per call, so concurrent comparisons do not share their timings.
        var clock = Stopwatch.StartNew();
        var timings = new List<TaskTiming>();

        _logger?.LogInformation(
            "Starting comparison — Reference: {RefCount:N0} rows, Target: {TgtCount:N0} rows",
            referenceItems.Count, targetItems.Count);

        var comparerSet = new HashSetComparer<T>(referenceItems.Count, targetItems.Count, _keyComparer);

        var duplicates = await Task.WhenAll(
            Task.Run(() => MeasureTime(() => AddAll(referenceItems, comparerSet.AddLeftItem), "Build Ref HashSet", clock, timings)),
            Task.Run(() => MeasureTime(() => AddAll(targetItems, comparerSet.AddRightItem), "Build Target HashSet", clock, timings))
        ).ConfigureAwait(false); // the rest runs on the thread pool, never on the caller's UI thread

        List<T> onlyInTarget = [];
        List<T> onlyInReference = [];
        List<PooledDictionary<string, object?>> inBothButDiff = [];

        Parallel.Invoke(
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            () => onlyInTarget    = MeasureTime(() => comparerSet.OnlyInRight().ToList(),  "Get OnlyInTarget",    clock, timings),
            () => onlyInReference = MeasureTime(() => comparerSet.OnlyInLeft().ToList(),   "Get OnlyInReference", clock, timings),
            () => inBothButDiff   = MeasureTime(() => CompareInBoth(comparerSet.InBoth()), "Compare InBoth",      clock, timings)
        );

        var result = new CompareResult<T>(inBothButDiff, _columns)
        {
            OnlyInReference       = onlyInReference,
            OnlyInTarget          = onlyInTarget,
            DuplicatesInReference = duplicates[0],
            DuplicatesInTarget    = duplicates[1],
            Timings               = timings.OrderBy(t => t.Start).ToArray(),
        };

        _logger?.LogInformation(
            "Comparison finished in {Elapsed:F2}s — InBothButDiff: {Diff:N0}, OnlyInRef: {OnlyRef:N0}, OnlyInTgt: {OnlyTgt:N0}",
            clock.Elapsed.TotalSeconds,
            result.Count,
            result.OnlyInReference.Count,
            result.OnlyInTarget.Count);

        if (result.DuplicatesInReference.Count + result.DuplicatesInTarget.Count > 0)
            _logger?.LogWarning(
                "Duplicate keys left out of the comparison — Reference: {DupRef:N0}, Target: {DupTgt:N0}",
                result.DuplicatesInReference.Count,
                result.DuplicatesInTarget.Count);

        LogGantt(result.Timings);

        return result;
    }

    /// <summary>Adds the items to one side of the set; returns those whose key is already there.</summary>
    private static List<T> AddAll(List<T> items, Func<T, bool> add)
    {
        var duplicates = new List<T>();
        foreach (var item in items)
            if (!add(item)) duplicates.Add(item);
        return duplicates;
    }

    private TResult MeasureTime<TResult>(Func<TResult> taskFunc, string action, Stopwatch clock, List<TaskTiming> timings)
    {
        var start = clock.Elapsed;
        TResult result = taskFunc();
        var end = clock.Elapsed;

        lock (timings)
        {
            timings.Add(new TaskTiming(action, start, end));
        }

        _logger?.LogInformation("  {Action} completed in {Elapsed:F2}s", action, (end - start).TotalSeconds);
        return result;
    }

    private void LogGantt(IReadOnlyList<TaskTiming> tasks)
    {
        if (tasks.Count == 0 || _logger is null || !_logger.IsEnabled(LogLevel.Information)) return;

        long Ms(TimeSpan t) => (long)t.TotalMilliseconds;
        long minTime       = tasks.Min(t => Ms(t.Start));
        long maxTime       = tasks.Max(t => Ms(t.End));
        long totalDuration = Math.Max(1, maxTime - minTime);

        const int BarWidth   = 60;
        const int LabelWidth = 22;
        double scale = (double)BarWidth / totalDuration;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine();
        sb.AppendLine("Parallel Task Visualization (Gantt Chart)");

        // ── Time axis header ────────────────────────────────────────────────
        var pad     = new string(' ', LabelWidth);
        var startTs = "0ms";
        var midTs   = $"{totalDuration / 2}ms";
        var endTs   = $"{totalDuration}ms";

        sb.Append(pad).Append('┌').Append(new string('─', BarWidth)).AppendLine("┐");

        // Tick row: 0 ........ midpoint ........ end
        var axis = new char[BarWidth];
        for (int i = 0; i < axis.Length; i++) axis[i] = ' ';
        WriteAt(axis, 0, startTs);
        WriteAt(axis, BarWidth / 2 - midTs.Length / 2, midTs);
        WriteAt(axis, BarWidth - endTs.Length, endTs);
        sb.Append(pad).Append('│').Append(new string(axis)).AppendLine("│");

        sb.Append(pad).Append('├').Append(new string('─', BarWidth)).AppendLine("┤");

        // ── One row per task — horizontal histogram of (idle | running | idle) ─
        foreach (var task in tasks)
        {
            long duration   = Ms(task.Duration);
            int startPos    = (int)((Ms(task.Start) - minTime) * scale);
            int durationLen = Math.Max(1, (int)(duration * scale));
            if (startPos + durationLen > BarWidth) durationLen = BarWidth - startPos;
            int trailing    = BarWidth - startPos - durationLen;

            sb.Append(task.Name.PadRight(LabelWidth));
            sb.Append('│');
            sb.Append(new string('░', startPos));
            sb.Append(new string('█', durationLen));
            sb.Append(new string('░', trailing));
            sb.Append('│');
            sb.AppendLine($" {duration,6:N0} ms  ({100.0 * duration / totalDuration,4:F1}%)");
        }

        sb.Append(pad).Append('└').Append(new string('─', BarWidth)).AppendLine("┘");
        sb.Append($"Total wall-clock: {totalDuration:N0} ms  ·  Tasks: {tasks.Count}");

        _logger.LogInformation("{GanttChart}", sb.ToString());

        // Local helper: paste `text` into `buffer` at `offset`, clamped.
        static void WriteAt(char[] buffer, int offset, string text)
        {
            if (offset < 0) offset = 0;
            for (int i = 0; i < text.Length && offset + i < buffer.Length; i++)
                buffer[offset + i] = text[i];
        }
    }

    private List<PooledDictionary<string, object?>> CompareInBoth(IEnumerable<(T, T)> inBothEnumerator)
    {
        return inBothEnumerator
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select(couple => CompareProperties(couple.Item1, couple.Item2))
            .Where(static diff => diff is not null)
            .Select(static diff => diff!)
            .ToList();
    }

    private PooledDictionary<string, object?>? CompareProperties(T referenceItem, T targetItem)
    {
        PooledDictionary<string, object?>? diff = null;

        for (int i = 0; i < _compared.Length; i++)
        {
            // Typed comparison: the values are boxed only when they differ
            var property = _compared[i];
            if (property.AreEqual(referenceItem, targetItem)) continue;

            diff ??= new PooledDictionary<string, object?>(_diffCapacity);

            diff.Add(_referenceColumns[i], property.Get(referenceItem));
            diff.Add(_targetColumns[i],    property.Get(targetItem));
        }

        if (diff == null) return null;

        for (int i = 0; i < _keys.Length; i++)
        {
            diff.Add(_keyColumns[i], _keys[i].Get(referenceItem));
        }

        diff.Add(ComparedItemsColumn, new CoupleItem<T>(referenceItem, targetItem));
        return diff;
    }
}
