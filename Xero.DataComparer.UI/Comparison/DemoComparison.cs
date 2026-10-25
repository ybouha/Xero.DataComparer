using System.Diagnostics;
using Xero.DataComparer.Core;

namespace Xero.DataComparer.UI;

/// <summary>Generates the reference and target lists, compares them with <see cref="ListComparer{T}"/> and keeps the result as grid rows.</summary>
sealed class DemoComparison
{
    public static readonly string[] KeyNames = { nameof(RiskIndicator.TradeId), nameof(RiskIndicator.Book), nameof(RiskIndicator.AsOfDate) };
    public static readonly string[] DefaultIgnored = { nameof(RiskIndicator.LastUpdated), nameof(RiskIndicator.RunId) };

    public required Dataset Dataset { get; init; }
    public required int ReferenceCount { get; init; }
    public required int TargetCount { get; init; }
    public required Field[] Keys { get; init; }
    public required Field[] Properties { get; init; }   // compared: neither keys nor ignored
    public required Field[] Ignored { get; init; }
    public required List<DiffRow> Rows { get; init; }
    public required IReadOnlyList<TaskTiming> Tasks { get; init; }
    public required TimeSpan Generation { get; init; }
    public required TimeSpan Comparison { get; init; }
    public required TimeSpan RowsBuild { get; init; }

    public int OnlyInReference { get; private init; }
    public int OnlyInTarget { get; private init; }
    public int Different { get; private init; }
    public int DuplicatesInReference { get; private init; }   // left out of the comparison by ListComparer
    public int Identical => ReferenceCount - DuplicatesInReference - OnlyInReference - Different;

    /// <summary>Different pairs whose gaps are all within the relevance threshold.</summary>
    public int Masked { get; private set; }

    public static async Task<DemoComparison> RunAsync(Dataset dataset, IReadOnlyCollection<string> ignored, IProgress<string> progress)
    {
        progress.Report($"Generating {dataset.Rows:N0} reference rows and {dataset.Rows:N0} target rows…");
        var clock = Stopwatch.StartNew();
        var (reference, target) = DataBuilder.Generate(dataset.Rows, dataset.Gaps, dataset.Gaps, dataset.Gaps);
        var generation = clock.Elapsed;
        // Collect the generator's garbage now, so the comparison is not timed with it.
        GC.Collect();

        progress.Report($"Comparing {reference.Count:N0} with {target.Count:N0} rows…");
        var comparer = new ListComparer<RiskIndicator>(KeyNames.ToList(), ignored.ToList());
        clock.Restart();
        var result = await comparer.CompareListAsync(reference, target);
        var comparison = clock.Elapsed;

        clock.Restart();
        var properties = Field.All.Where(f => !KeyNames.Contains(f.Name) && !ignored.Contains(f.Name)).ToArray();
        var onlyInReference = result.OnlyInReference;
        var onlyInTarget = result.OnlyInTarget;
        var rows = new List<DiffRow>(onlyInReference.Count + onlyInTarget.Count + result.Count);
        rows.AddRange(onlyInReference.Select(r => new DiffRow { Kind = DiffKind.OnlyInReference, Reference = r }));
        rows.AddRange(onlyInTarget.Select(t => new DiffRow { Kind = DiffKind.OnlyInTarget, Target = t }));
        rows.AddRange(Pairs(result, properties));

        return new DemoComparison
        {
            Dataset = dataset,
            ReferenceCount = reference.Count,
            TargetCount = target.Count,
            Keys = KeyNames.Select(Field.Named).ToArray(),
            Properties = properties,
            Ignored = Field.All.Where(f => ignored.Contains(f.Name)).ToArray(),
            Rows = rows,
            Tasks = result.Timings,
            Generation = generation,
            Comparison = comparison,
            RowsBuild = clock.Elapsed,
            OnlyInReference = onlyInReference.Count,
            OnlyInTarget = onlyInTarget.Count,
            Different = result.Count,
            DuplicatesInReference = result.DuplicatesInReference.Count,
        };
    }

    /// <summary>
    /// The diff rows of the result hold Reference_X / Target_X only for the properties that differ, plus the compared pair:
    /// one <see cref="DiffRow"/> each, with a bit per differing property.
    /// </summary>
    static IEnumerable<DiffRow> Pairs(CompareResult<RiskIndicator> result, Field[] properties)
    {
        var referenceKeys = properties.Select(p => "Reference_" + p.Name).ToArray();
        foreach (var diff in result)
        {
            var couple = (CoupleItem<RiskIndicator>)diff["ComparedItems"]!;
            ulong differing = 0;
            for (int i = 0; i < referenceKeys.Length; i++)
                if (diff.TryGetValue(referenceKeys[i], out _)) differing |= 1UL << i;
            yield return new DiffRow { Kind = DiffKind.Different, Reference = couple.ReferenceItem, Target = couple.TargetItem, Differing = differing };
        }
    }

    /// <summary>Marks which gaps are relevant. Runs on the comparison result only, so changing the threshold never compares again.</summary>
    public void ApplyThreshold(RelevanceThreshold threshold)
    {
        int masked = 0;
        foreach (var row in Rows)
        {
            if (row.Kind != DiffKind.Different) continue;
            ulong relevant = 0;
            for (int i = 0; i < Properties.Length; i++)
                if (row.Differs(i) && threshold.Exceeds(Properties[i], row.Reference!, row.Target!)) relevant |= 1UL << i;
            row.Relevant = relevant;
            if (relevant == 0) masked++;
        }
        Masked = masked;
    }
}
