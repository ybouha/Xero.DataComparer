# Xero.DataComparer

**High-performance generic list comparator for .NET built for non-regression testing and large-scale data reconciliation.**

[![.NET](https://img.shields.io/badge/.NET-net48%20%E2%80%93%20net10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-Youssef%20Bouha-0A66C2?logo=linkedin)](https://www.linkedin.com/in/youssef-bouha-3311946/)

---

## Overview

`Xero.DataComparer` compares two arbitrary lists of objects and returns, in a single pass:

- the items present **only in the reference** set,
- the items present **only in the target** set,
- the items present in **both** but with property-level differences, expressed as a flat row that is grid-bindable out of the box.

It is designed for the kind of workload you hit when validating a release of a pricing engine, reconciling trade-booking environments, or comparing the output of two ETL pipelines on millions of records: the user supplies one or more **key properties** to identify equivalent rows, an optional list of **ignored properties**, and the comparator does the rest in parallel.

The codebase is small, dependency-free apart from `Microsoft.Extensions.Logging.Abstractions`, and tuned to avoid the usual hot-path costs (reflection, hash recomputation, dictionary resizing).

---

## ⚡ What you get back: structured diff data, not a pass/fail verdict

This is the point that separates `Xero.DataComparer` from a hash-based "are these two lists equal?" check.

The result is **not a boolean**. It is a fully populated [`CompareResult<T>`](Xero.DataComparer.Core/CompareResult.cs) holding three slices:

| Slice | Type | What it contains |
|---|---|---|
| `OnlyInReference` | `List<T>` | Whole original objects that exist only on the reference side. |
| `OnlyInTarget` | `List<T>` | Whole original objects that exist only on the target side. |
| `result` itself (flat rows) | `List<PooledDictionary<string, object>>` | One row per pair where the keys match but at least one compared property disagrees. |

Each "in-both-but-diff" row is laid out so a grid, a JSON serializer, or an investigation script can consume it directly:

| Column | Meaning |
|---|---|
| `Key_<KeyProperty>` | The composite-key value that links the two sides. One column per key property. |
| `Reference_<Property>` | The value seen on the reference side, **only for properties that actually differ**. |
| `Target_<Property>` | The value seen on the target side, **only for properties that actually differ**. |
| `ComparedItems` | A `CoupleItem<T>` carrying the two original `T` objects so you can navigate to any other field on demand. |

Because `CompareResult<T>` implements `ITypedList`, those flat rows light up automatically in any data-bound grid (DevExpress `GridControl`, WinForms `DataGridView`, …) no view model, no manual column setup.

### Sample console output (from the `Xero.DataComparer.Console` demo)

The included demo runs an NRT over **10,000,000 risk-indicator rows** and renders the three result slices as three separate tables, each driven by the descriptor-based contract a real DataGrid would walk:

- **In both, differing** columns come from `CompareResult<T>` via `ITypedList.GetItemProperties()`
- **Only in reference** / **Only in target** columns come from `TypeDescriptor.GetProperties(typeof(T))`, i.e. the same default column inference WinForms / WPF DataGrid / DevExpress GridControl do for any typed list.

The console renderer is generic and knows nothing about the diff schema or the domain type:

```
Only in reference  :   20,000   (expected 20,000)
Only in target     :   20,000   (expected 20,000)
In both, differing :   20,000   (expected 20,000)

Three result tables, each rendered through the same descriptor-based path
a real DataGrid would walk:
  · in-both-but-differ  → CompareResult<T> ITypedList descriptors
  · only-in-reference   → TypeDescriptor.GetProperties(typeof(T))
  · only-in-target      → TypeDescriptor.GetProperties(typeof(T))

─── In both, differing top 5 of 20,000 ──────────────────────────────────────
┌────────────────┬────────────┬──────────────┬───────────────┬───────────────┬───────────────┬───────────────┐
│ Key_TradeId    │ Key_Book   │ Key_AsOfDate │ Reference_PnL │ Target_PnL    │ Reference_VaR │ Target_VaR    │
├────────────────┼────────────┼──────────────┼───────────────┼───────────────┼───────────────┼───────────────┤
│ TRD-009962041  │ PWR-FR     │ 2026-04-30   │ -45,123.4567  │ -45,167.8901  │  12,345.6789  │  12,389.1234  │
│ TRD-009962042  │ EQ-DELTA-1 │ 2026-04-30   │  28,401.1023  │  28,422.7124  │   8,907.5512  │   8,944.9301  │
│ TRD-009962043  │ FX-G10     │ 2026-04-30   │  -1,204.8801  │  -1,232.5135  │  34,556.1804  │  34,512.9403  │
│ TRD-009962044  │ IR-EU      │ 2026-04-30   │  72,003.5512  │  72,041.0089  │  19,442.6603  │  19,488.7702  │
│ TRD-009962045  │ GAS-NBP    │ 2026-04-30   │ -88,221.4400  │ -88,180.0021  │   4,902.1190  │   4,950.0080  │
└────────────────┴────────────┴──────────────┴───────────────┴───────────────┴───────────────┴───────────────┘

─── Only in reference top 5 of 20,000 ───────────────────────────────────────
┌────────────────┬────────────┬────────────┬──────────────┬─────────────┬───────────┬──────────┬──────────┬─────────┐
│ TradeId        │ Book       │ AsOfDate   │ PnL          │ VaR         │ Delta     │ Gamma    │ Vega     │ Theta   │
├────────────────┼────────────┼────────────┼──────────────┼─────────────┼───────────┼──────────┼──────────┼─────────┤
│ TRD-009980001  │ PWR-DE     │ 2026-04-30 │  41,228.0144 │  31,990.45… │ -213.7301 │   4.5500 │  37.8801 │  1.4429 │
│ TRD-009980002  │ OIL-BRENT  │ 2026-04-30 │ -89,440.5512 │   2,108.91… │  411.0072 │   8.2317 │ -19.1109 │ -3.2241 │
│ TRD-009980003  │ EQ-VEGA-1  │ 2026-04-30 │  17,902.3344 │  44,005.10… │  -71.4112 │   2.1100 │  10.5532 │  0.7710 │
│ TRD-009980004  │ FX-G10     │ 2026-04-30 │ -32,118.7780 │  19,884.32… │  283.8801 │   6.4429 │ -25.0090 │  4.0021 │
│ TRD-009980005  │ IR-EU      │ 2026-04-30 │  71,015.6611 │   7,330.55… │ -498.2210 │   0.4710 │  44.1188 │ -2.8807 │
└────────────────┴────────────┴────────────┴──────────────┴─────────────┴───────────┴──────────┴──────────┴─────────┘

─── Only in target top 5 of 20,000 ──────────────────────────────────────────
┌────────────────┬────────────┬────────────┬──────────────┬─────────────┬───────────┬──────────┬──────────┬─────────┐
│ TradeId        │ Book       │ AsOfDate   │ PnL          │ VaR         │ Delta     │ Gamma    │ Vega     │ Theta   │
├────────────────┼────────────┼────────────┼──────────────┼─────────────┼───────────┼──────────┼──────────┼─────────┤
│ TRD-009990001  │ GAS-NBP    │ 2026-04-30 │  55,114.2299 │  18,442.65… │  102.4471 │   7.1100 │  -8.5519 │  3.6602 │
│ TRD-009990002  │ EQ-DELTA-1 │ 2026-04-30 │ -19,443.0017 │  29,005.81… │ -334.9912 │   3.2208 │  41.7702 │ -1.9180 │
│ TRD-009990003  │ PWR-FR     │ 2026-04-30 │  88,221.5500 │   4,901.10… │  248.8810 │   9.4471 │ -32.1109 │  4.4471 │
│ TRD-009990004  │ OIL-BRENT  │ 2026-04-30 │ -65,007.4421 │  41,005.55… │  -27.6601 │   1.7710 │   9.5512 │ -3.0091 │
│ TRD-009990005  │ IR-EU      │ 2026-04-30 │  12,003.8810 │  35,114.20… │  457.8801 │   8.6602 │ -47.4471 │  2.2208 │
└────────────────┴────────────┴────────────┴──────────────┴─────────────┴───────────┴──────────┴──────────┴─────────┘
```

The exact same `CompareResult<T>` (and its `OnlyInReference` / `OnlyInTarget` slices) could be assigned to a `DevExpress.XtraGrid.GridControl.DataSource`, a WPF `DataGrid.ItemsSource`, or a WinForms `DataGridView.DataSource` and produce three fully columnar views with zero extra code. The console renderer is just one consumer of the same descriptor contract.

> See [`Xero.DataComparer.Console/Program.cs`](Xero.DataComparer.Console/Program.cs) under 200 lines, including the data generator and the generic `ConsoleGrid` renderer.

---

## Features

- **Composite key matching.** Identify pairs across the two lists by any combination of properties no need to write a custom `IEqualityComparer` per type.
- **Structured diff data not a verdict.** Differences come back as flat rows (`Key_*`, `Reference_*`, `Target_*`, plus the original paired objects). You see *which* row diverged, on *which* properties, with *what* values ready to bind to any `ITypedList`-aware UI control (DevExpress `GridControl`, WinForms `DataGridView`), to serialize, or to pipe into an investigation pipeline.
- **Compiled accessors.** Property getters are built once at construction time using `Expression.Lambda`, removing reflection from the hot path.
- **Parallel execution.** Both reference and target indexes are built concurrently; the three result sets (only-in-reference, only-in-target, in-both-with-diff) are produced in parallel; per-pair property comparison is fan-out via PLINQ.
- **Custom dual-indexed HashSet.** A purpose-built `HashSetComparer<T>` holds left and right hash structures using the same comparator, avoiding allocation churn when a row needs to be looked up in the opposite set.
- **Memory-tight diff dictionary.** `PooledDictionary<TKey, TValue>` is sized exactly to the column count, never resizes during normal use, and uses linear scan (faster than hashing for the small entry counts of a per-row diff).
- **Built-in instrumentation.** Each major phase is timed with a `Stopwatch` and logged; an ASCII Gantt chart is emitted to the logger so you can see the parallelism at work.
- **Runtime type generation.** `DynamicTypeBuilder` can emit a CLR type from a `ColumnDef[]` schema using `System.Reflection.Emit`, useful when comparing query results whose schema is only known at runtime (works with Dapper).
- **Single dependency.** Only `Microsoft.Extensions.Logging.Abstractions` no third-party libraries, no source generators, no analyzers.

---

## Requirements

The package multi-targets and runs on everything from **.NET Framework 4.8 to .NET 10**:

- `net48` .NET Framework 4.8
- `netstandard2.0` .NET Core 2.0–3.1, .NET 5/6/7/9
- `net8.0` .NET 8 (LTS)
- `net10.0` .NET 10

On `net48` / `netstandard2.0` the package brings in `System.Text.Json` and an `IsExternalInit`
polyfill so the same API works unchanged.

---

## Quick start

```csharp
using Xero.DataComparer.Core;

public sealed class Trade
{
    public string   TradeId      { get; set; } = "";
    public string   Book         { get; set; } = "";
    public decimal  Quantity     { get; set; }
    public decimal  Price        { get; set; }
    public DateTime LastModified { get; set; }
}

var reference = LoadFromSystemA();   // List<Trade>
var target    = LoadFromSystemB();   // List<Trade>

var comparer = new ListComparer<Trade>(
    keyProperties:    new List<string> { "TradeId" },
    ignoreProperties: new List<string> { "LastModified" });

CompareResult<Trade> result = await comparer.CompareList(reference, target);

// 1) Counts useful for a high-level NRT pass / fail signal
Console.WriteLine($"Only in reference : {result.OnlyInReference?.Count ?? 0}");
Console.WriteLine($"Only in target    : {result.OnlyInTarget?.Count    ?? 0}");
Console.WriteLine($"In both, differing: {result.Count}");

// 2) Actual diff DATA the value the library brings on top of a hash check
foreach (var diff in result)
{
    var tradeId = diff["Key_TradeId"];
    var refQty  = diff["Reference_Quantity"];
    var tgtQty  = diff["Target_Quantity"];
    Console.WriteLine($"Trade {tradeId}: ref={refQty}  target={tgtQty}");
}
```

`CompareResult<T>` is itself a `List<PooledDictionary<string, object>>` and implements `ITypedList`, so it can be assigned directly to a grid `DataSource` and the columns light up automatically.

---

## How it works

```
                   ┌─────────────────────────────┐
   reference  ──▶  │  HashSetComparer<T> (left)  │
                   └─────────────────────────────┘
                                ▲
                                │ shared key-equality comparer
                                ▼
                   ┌─────────────────────────────┐
   target     ──▶  │  HashSetComparer<T> (right) │
                   └─────────────────────────────┘

   ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐
   │ OnlyInLeft   │   │ OnlyInRight  │   │ InBoth → property    │
   │ enumeration  │ ║ │ enumeration  │ ║ │ comparison via PLINQ │
   └──────────────┘   └──────────────┘   └──────────────────────┘
                              ▼
                        CompareResult<T>
```

1. The user-supplied **key property names** are resolved once into compiled `Func<T, object>` getters.
2. An `EqualityComparerEx<T>` is built around those getters and used as the shared comparer for both the left and the right hash sets.
3. The two indexes are populated in parallel via `Task.WhenAll`.
4. The three result categories are produced with `Parallel.Invoke`; the per-pair property comparison is itself parallelized with `AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount)`.
5. Each phase is timed and rendered as an ASCII Gantt chart through the supplied `ILogger<>`.

---

## Performance design notes

A few choices that matter when the inputs grow into the millions:

- **Reflection is paid once, not per row.** All getters are precompiled with `System.Linq.Expressions`; the per-row path is a delegate invocation.
- **Pre-sized hash buckets.** `HashSetComparer<T>` is constructed with the known left/right counts and uses prime-sized buckets, eliminating the resize-and-rehash cost that a generic `HashSet<T>` would pay when filled in bulk.
- **Linear-scan diff rows.** A diff dictionary holds at most `2 × propsCompared + keys + 1` entries typically 10 to 30. For that range, a linear scan is faster than computing hash codes; `PooledDictionary` exploits that.
- **No allocation in the inner equality loop.** The comparator's `Equals` and `GetHashCode` walk the precompiled getter list with no boxing beyond the `object` returned by the getter itself.
- **Pair enumeration is iterator-based.** `OnlyInLeft`, `OnlyInRight`, and `InBoth` are `IEnumerable<>` yielding pipelines so the parallel consumers can start processing without waiting for full materialization.

---

## API surface

| Type | Purpose |
|---|---|
| `ListComparer<T>` | Entry point. Configured with key + ignored property names. |
| `CompareResult<T>` | Output: differing pairs as flat rows + only-in-reference + only-in-target. Implements `ITypedList`. |
| `HashSetComparer<T>` | Dual hash-indexed structure powering `OnlyIn*` / `InBoth` enumerations. |
| `EqualityComparerEx<T>` | `IEqualityComparer<T>` derived from a list of getters (composite key). |
| `PooledDictionary<TKey, TValue>` | Lightweight, exact-capacity dictionary used for diff rows. |
| `CoupleItem<T>` | Holds the original `(reference, target)` pair alongside the diff row. |
| `DynamicTypeBuilder` / `ColumnDef` | Emit a CLR type at runtime from a column schema, for schema-late comparisons (Dapper, ad-hoc SQL). |
| `DynamicPropertyDescriptor` | Backs the `ITypedList` integration so dynamic columns are visible to UI grids. |

---

## Use cases

- **Non-regression testing of pricing / risk engines.** Compare yesterday's run against today's across thousands of trades, ignoring volatile fields (timestamps, run IDs).
- **Cross-environment reconciliation.** Validate that a UAT environment and production return the same booking output for the same input.
- **ETL validation.** Diff the source and the loaded data to confirm a migration has not corrupted records.
- **Schema-late comparisons.** Compare two query results coming from different databases when the schema is only known at runtime `DynamicTypeBuilder` produces a Dapper-friendly type and `ListComparer<T>` handles the rest.

---

## Building from source

```bash
git clone https://github.com/<your-username>/Xero.DataComparer.git
cd Xero.DataComparer
dotnet build -c Release
```

A NuGet packaging target can be added by setting `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>` in the csproj.

---

## License

Released under the MIT License see [LICENSE](LICENSE) for details.

---

## Author

**Youssef Bouha** Senior software architect, ENSIMAG, 18+ years on .NET front-to-back risk and pricing platforms.

🔗 [LinkedIn](https://www.linkedin.com/in/youssef-bouha-3311946/)

This library is a clean-room implementation of patterns I have used and refined over multiple capital-markets and energy-trading systems. It is shared as open source, written from scratch in modern .NET, with no proprietary code.
