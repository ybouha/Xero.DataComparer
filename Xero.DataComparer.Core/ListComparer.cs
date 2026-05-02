using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Xero.DataComparer.Core;

public class ListComparer<T> where T : class, new()
{
    // Store name and getter for iterating and creating the diff dictionary
    private readonly List<(string Name, Func<T, object> Getter)> _keyPropertiesData;
    private readonly List<(string Name, Func<T, object> Getter)> _propertiesToCompareData;
    // Max entries a diff dict can hold: (Reference_ + Target_) per prop + Key_ per key + "ComparedItems"
    private int _diffCapacity;

    private readonly EqualityComparerEx<T> keyEqComparer;
    private readonly ILogger<ListComparer<T>>? _logger;

    // Gantt Chart Data
    public class TaskInfo
    {
        public string Name = string.Empty;
        public long Start;   // in ms
        public long End;
        public long Duration => End - Start;
    }

    private List<TaskInfo> _taskTimings = new();
    private Stopwatch _globalStopwatch = new();

    public ListComparer(
        List<string> keyProperties,
        List<string> ignoreProperties,
        ILogger<ListComparer<T>>? logger = null)
    {
        _logger = logger;

        var distinctKeys = keyProperties.Distinct().ToList();
        _keyPropertiesData = new List<(string Name, Func<T, object> Getter)>(distinctKeys.Count);
        var keyGetters = new List<Func<T, object>>(distinctKeys.Count);

        foreach (var propName in distinctKeys)
        {
             var getter = CreatePropertyGetter(propName);
             _keyPropertiesData.Add((propName, getter));
             keyGetters.Add(getter);
        }

        // Use HashSet<string> for O(1) property name lookups during filtering
        var ignoreSet = new HashSet<string>(ignoreProperties);
        var keySet = new HashSet<string>(distinctKeys);

        var allProps = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        _propertiesToCompareData = new List<(string Name, Func<T, object> Getter)>();

        foreach (var prop in allProps)
        {
            if (ignoreSet.Contains(prop.Name)) continue;
            if (keySet.Contains(prop.Name)) continue;

            _propertiesToCompareData.Add((prop.Name, CreatePropertyGetter(prop)));
        }

        keyEqComparer = new EqualityComparerEx<T>(keyGetters);
        _diffCapacity = _propertiesToCompareData.Count * 2 + _keyPropertiesData.Count + 1;
    }

    private Func<T, object> CreatePropertyGetter(string propertyName)
    {
        var propInfo = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (propInfo == null) throw new ArgumentException($"Property {propertyName} not found on {typeof(T).Name}");
        return CreatePropertyGetter(propInfo);
    }

    private Func<T, object> CreatePropertyGetter(PropertyInfo propInfo)
    {
        var parameter = Expression.Parameter(typeof(T), "i");
        var propertyAccess = Expression.Property(parameter, propInfo);
        var conversion = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<T, object>>(conversion, parameter).Compile();
    }

    private async Task MeasureTimeAsync(Func<Task> taskFunc, string action)
    {
        long start = _globalStopwatch.ElapsedMilliseconds;

        var sw = new Stopwatch();
        sw.Start();
        await taskFunc();
        sw.Stop();

        long end = _globalStopwatch.ElapsedMilliseconds;

        lock (_taskTimings)
        {
            _taskTimings.Add(new TaskInfo { Name = action, Start = start, End = end });
        }

        _logger?.LogInformation("  {Action} completed in {Elapsed:F2}s", action, sw.Elapsed.TotalSeconds);
    }

    private TResult MeasureTime<TResult>(Func<TResult> taskFunc, string action)
    {
        long start = _globalStopwatch.ElapsedMilliseconds;

        var sw = new Stopwatch();
        sw.Start();
        TResult result = taskFunc();
        sw.Stop();

        long end = _globalStopwatch.ElapsedMilliseconds;

        lock (_taskTimings)
        {
            _taskTimings.Add(new TaskInfo { Name = action, Start = start, End = end });
        }

        _logger?.LogInformation("  {Action} completed in {Elapsed:F2}s", action, sw.Elapsed.TotalSeconds);
        return result;
    }

    public async Task<CompareResult<T>> CompareList(List<T> referenceItems, List<T> targetItems)
    {
        _globalStopwatch = Stopwatch.StartNew();
        _taskTimings.Clear();

        _logger?.LogInformation(
            "Starting comparison — Reference: {RefCount:N0} rows, Target: {TgtCount:N0} rows",
            referenceItems.Count, targetItems.Count);

        List<T>? onlyInTarget    = null;
        List<T>? onlyInReference = null;
        List<PooledDictionary<string, object>>? inBothButDiff = new();

        var comparerSet = new HashSetComparer<T>(referenceItems.Count, targetItems.Count, keyEqComparer);

        var sw = Stopwatch.StartNew();

        await Task.WhenAll(
            MeasureTimeAsync(() => Task.Run(() => referenceItems.ForEach(x => comparerSet.AddLeftItem(x))),  "Build Ref HashSet"),
            MeasureTimeAsync(() => Task.Run(() => targetItems.ForEach(x => comparerSet.AddRightItem(x))), "Build Target HashSet")
        );

        Parallel.Invoke(
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            () => onlyInTarget    = MeasureTime(() => comparerSet.OnlyInRight().ToList(),         "Get OnlyInTarget"),
            () => onlyInReference = MeasureTime(() => comparerSet.OnlyInLeft().ToList(),          "Get OnlyInReference"),
            () => inBothButDiff   = MeasureTime(() => CompareInBoth(comparerSet.InBoth()),        "Compare InBoth")
        );

        var result = new CompareResult<T>(inBothButDiff)
        {
            OnlyInReference = onlyInReference,
            OnlyInTarget    = onlyInTarget
        };

        sw.Stop();
        _logger?.LogInformation(
            "Comparison finished in {Elapsed:F2}s — InBothButDiff: {Diff:N0}, OnlyInRef: {OnlyRef:N0}, OnlyInTgt: {OnlyTgt:N0}",
            sw.Elapsed.TotalSeconds,
            result.Count,
            result.OnlyInReference?.Count ?? 0,
            result.OnlyInTarget?.Count ?? 0);

        LogGantt(_taskTimings);

        return result;
    }

    private void LogGantt(List<TaskInfo> tasks)
    {
        if (tasks.Count == 0) return;

        long minTime       = tasks.Min(t => t.Start);
        long maxTime       = tasks.Max(t => t.End);
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
        Array.Fill(axis, ' ');
        WriteAt(axis, 0, startTs);
        WriteAt(axis, BarWidth / 2 - midTs.Length / 2, midTs);
        WriteAt(axis, BarWidth - endTs.Length, endTs);
        sb.Append(pad).Append('│').Append(new string(axis)).AppendLine("│");

        sb.Append(pad).Append('├').Append(new string('─', BarWidth)).AppendLine("┤");

        // ── One row per task — horizontal histogram of (idle | running | idle) ─
        foreach (var task in tasks.OrderBy(t => t.Start))
        {
            int startPos    = (int)((task.Start - minTime) * scale);
            int durationLen = Math.Max(1, (int)(task.Duration * scale));
            if (startPos + durationLen > BarWidth) durationLen = BarWidth - startPos;
            int trailing    = BarWidth - startPos - durationLen;

            sb.Append(task.Name.PadRight(LabelWidth));
            sb.Append('│');
            sb.Append(new string('░', startPos));
            sb.Append(new string('█', durationLen));
            sb.Append(new string('░', trailing));
            sb.Append('│');
            sb.AppendLine($" {task.Duration,6:N0} ms  ({100.0 * task.Duration / totalDuration,4:F1}%)");
        }

        sb.Append(pad).Append('└').Append(new string('─', BarWidth)).AppendLine("┘");
        sb.Append($"Total wall-clock: {totalDuration:N0} ms  ·  Tasks: {tasks.Count}");

        _logger?.LogInformation("{GanttChart}", sb.ToString());

        // Local helper: paste `text` into `buffer` at `offset`, clamped.
        static void WriteAt(char[] buffer, int offset, string text)
        {
            if (offset < 0) offset = 0;
            for (int i = 0; i < text.Length && offset + i < buffer.Length; i++)
                buffer[offset + i] = text[i];
        }
    }

    private List<PooledDictionary<string, object>> CompareInBoth(IEnumerable<(T, T)> inBothEnumerator)
    {
        return inBothEnumerator
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select(couple => CompareProperties(couple.Item1, couple.Item2))
            .Where(static diff => diff is not null)
            .Select(static diff => diff!)
            .ToList();
    }

    private PooledDictionary<string, object>? CompareProperties(T referenceItem, T targetItem)
    {
        PooledDictionary<string, object>? diff = null;

        for (int i = 0; i < _propertiesToCompareData.Count; i++)
        {
            var propName = _propertiesToCompareData[i].Name;
            var getter   = _propertiesToCompareData[i].Getter;

            var referenceValue = getter(referenceItem);
            var targetValue    = getter(targetItem);

            if (Equals(referenceValue, targetValue)) continue;

            if (diff == null) diff = new PooledDictionary<string, object>(_diffCapacity);

            diff.Add($"Reference_{propName}", referenceValue);
            diff.Add($"Target_{propName}",    targetValue);
        }

        if (diff == null) return null;

        for (int i = 0; i < _keyPropertiesData.Count; i++)
        {
            var propName = _keyPropertiesData[i].Name;
            var getter   = _keyPropertiesData[i].Getter;
            diff.Add($"Key_{propName}", getter(referenceItem));
        }

        diff.Add("ComparedItems", new CoupleItem<T>(referenceItem, targetItem));
        return diff;
    }
}
